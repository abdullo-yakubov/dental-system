using DentalLocal.Application;
using DentalLocal.Infrastructure;
using DentalLocal.Persistence;
using DentalLocal.Server.Common;
using DentalLocal.Server.Endpoints;
using DentalLocal.Server.Middleware;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Single-instance guard: двойной клик по ярлыку лаунчера (или повторный ручной запуск сервера
// поверх уже работающего) не должен поднимать второй Kestrel/Postgres-бутстрап на тех же портах.
// Дополняет pg_advisory_lock вокруг Database.MigrateAsync (см. Persistence/MigrationExtensions.cs) —
// тот защищает миграции от гонки, этот не даёт второму процессу вообще стартовать.
using var singleInstanceMutex = new Mutex(initiallyOwned: true, name: "Global\\DentalLocal.Server.SingleInstance", createdNew: out var isFirstServerInstance);
if (!isFirstServerInstance)
{
    Console.Error.WriteLine("DentalLocal.Server уже запущен в другом процессе. Повторный запуск отменён.");
    return;
}

var builder = WebApplication.CreateBuilder(args);

// TODO(Фаза 6): при переходе на итоговую раскладку лаунчера (см. п.19 ТЗ) путь логов должен указывать
// на Data/Logs относительно корня установки, а не ContentRootPath разработческого запуска.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(context.HostingEnvironment.ContentRootPath, "Logs", "log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        // Раньше ограничение было только по количеству файлов — один аномально шумный день
        // (например, цикл ретраев к недоступной БД) мог залить диск ещё до полуночного роллинга.
        fileSizeLimitBytes: 50 * 1024 * 1024,
        rollOnFileSizeLimit: true));

const string ClientOrigin = "ClientDevServer";
builder.Services.AddCors(options => options.AddPolicy(ClientOrigin, policy => policy
    .WithOrigins("http://localhost:5171", "https://localhost:7046")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:Default.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddPersistence(connectionString);

// Rate limiting (см. промпт 17 аудита): без него подбор PIN через curl не ограничен ничем, кроме
// клиентской блокировки, которую curl просто не видит.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy(RateLimiterPolicies.Login, httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
    options.AddPolicy(RequestTimeoutPolicies.Long, TimeSpan.FromSeconds(120));
});

// Лимит файлов пациента — 200МБ (PatientFile.MaxFileSizeBytes) + запас на служебные поля формы.
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 210L * 1024 * 1024;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var postgresBootstrap = scope.ServiceProvider.GetRequiredService<DentalLocal.Application.Common.IPostgresBootstrapService>();
    await postgresBootstrap.BootstrapAsync();

    var lifetime = scope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopping.Register(() =>
    {
        postgresBootstrap.ShutdownAsync().GetAwaiter().GetResult();
    });
}

await app.Services.MigrateDatabaseAsync(seedTestPatients: app.Environment.IsDevelopment());

// Успешные опросы /health/*, а также раздача статики Blazor-бандла на каждый F5 — это шум, а не
// сигнал; понижаем их до Debug (ниже порога Information по умолчанию), чтобы не засорять лог,
// но ошибки на этих же путях по-прежнему логируются как обычно.
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex is not null || httpContext.Response.StatusCode >= 500)
            return Serilog.Events.LogEventLevel.Error;

        var path = httpContext.Request.Path.Value ?? string.Empty;
        var isNoiseEndpoint = path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
            || path.Equals(DentalLocal.Shared.Common.ApiRoutes.System.Health, StringComparison.OrdinalIgnoreCase);

        return isNoiseEndpoint ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information;
    };
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors(ClientOrigin);

// Файлы пациентов больше НЕ раздаются анонимно через PhysicalFileProvider (см. промпт 11 аудита —
// это было утечкой медданных: <img src> не шлёт Bearer-заголовок, поэтому либо файлы не грузились,
// либо middleware их пропускал без проверки). Теперь отдача идёт через FilesEndpoints.cs —
// GET /api/v1/files/{id}/link (обычная авторизация) выдаёт короткоживущую подписанную ссылку,
// GET /api/v1/files/{id}?exp=&sig= проверяет подпись сама (см. BearerAuthMiddleware).
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "Data", "Files"));

app.UseRateLimiter();
app.UseMiddleware<BearerAuthMiddleware>();
app.UseRequestTimeouts();

// Раздача опубликованного Blazor WASM Client (единый .exe из раздела 2 ТЗ — установщик копирует
// публикацию Client в Server/wwwroot). В dev-режиме wwwroot пуст, Client запускается отдельным
// процессом (см. TODO выше) — эти вызовы в таком случае просто ничего не находят и не мешают CORS-режиму.
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapAuthEndpoints();
app.MapPatientsEndpoints();
app.MapVisitsEndpoints();
app.MapProceduresEndpoints();
app.MapTeethEndpoints();
app.MapIcdEndpoints();
app.MapPaymentsEndpoints();
app.MapDashboardEndpoints();
app.MapMaterialsEndpoints();
app.MapBackupEndpoints();
app.MapReportEndpoints();
app.MapSettingsEndpoints();
app.MapRemindersEndpoints();
app.MapTreatmentPlansEndpoints();
app.MapDiscountsEndpoints();
app.MapWaitingListEndpoints();
app.MapPrescriptionsEndpoints();
app.MapWarrantiesEndpoints();
app.MapDoctorScheduleEndpoints();
app.MapAnalyticsEndpoints();
app.MapJobsEndpoints();
app.MapFilesEndpoints();

app.MapGet(DentalLocal.Shared.Common.ApiRoutes.System.Health, () => Results.Ok(new { status = "ok" }));

// /health/live — процесс жив и Kestrel принимает запросы (используется Launcher'ом для опроса
// готовности вместо бесконечного ожидания — см. промпт 9 аудита зависаний).
app.MapGet(DentalLocal.Shared.Common.ApiRoutes.System.Live, () => Results.Ok(new { status = "ok" }));

// /health/ready — БД доступна и миграции применены. Оба пути вне /api/v1, поэтому не требуют
// Bearer-токена (см. BearerAuthMiddleware).
app.MapGet(DentalLocal.Shared.Common.ApiRoutes.System.Ready, async (DentalDbContext db, CancellationToken ct) =>
{
    try
    {
        if (!await db.Database.CanConnectAsync(ct))
        {
            return Results.Json(
                new { status = "starting", stage = "database", detail = "PostgreSQL ещё не готов принимать соединения." },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var pending = await db.Database.GetPendingMigrationsAsync(ct);
        if (pending.Any())
        {
            return Results.Json(
                new { status = "starting", stage = "migrations", detail = "Применяются миграции базы данных." },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Ok(new { status = "ready", stage = "ready", detail = (string?)null });
    }
    catch (Exception ex)
    {
        return Results.Json(
            new { status = "error", stage = "database", detail = ex.Message },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapFallbackToFile("index.html");

// Пишем фактический порт в runtime.json рядом с исполняемым файлом, чтобы Launcher не полагался
// на захардкоженный http://localhost:5000 (см. промпт 10 аудита зависаний) — если порт когда-либо
// станет настраиваемым/динамическим, Launcher подхватит актуальное значение без пересборки.
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var addressesFeature = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
        var address = addressesFeature?.Addresses.FirstOrDefault();
        if (address is null) return;

        var port = new Uri(address).Port;
        var runtimeInfoPath = Path.Combine(AppContext.BaseDirectory, "runtime.json");
        File.WriteAllText(runtimeInfoPath, System.Text.Json.JsonSerializer.Serialize(new { port }));
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Не удалось записать runtime.json с фактическим портом сервера.");
    }
});

app.Run();
