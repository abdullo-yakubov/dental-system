using DentalLocal.Application;
using DentalLocal.Infrastructure;
using DentalLocal.Persistence;
using DentalLocal.Server.Endpoints;
using DentalLocal.Server.Middleware;
using Serilog;

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
        retainedFileCountLimit: 30));

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

await app.Services.MigrateDatabaseAsync();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors(ClientOrigin);

var filesDir = Path.Combine(app.Environment.ContentRootPath, "Data", "Files");
Directory.CreateDirectory(filesDir);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(filesDir),
    RequestPath = "/api/v1/files"
});

app.UseMiddleware<BearerAuthMiddleware>();

app.MapAuthEndpoints();
app.MapPatientsEndpoints();
app.MapVisitsEndpoints();
app.MapProceduresEndpoints();
app.MapTeethEndpoints();
app.MapPaymentsEndpoints();
app.MapDashboardEndpoints();
app.MapMaterialsEndpoints();
app.MapBackupEndpoints();
app.MapReportEndpoints();
app.MapSettingsEndpoints();

app.MapGet(DentalLocal.Shared.Common.ApiRoutes.System.Health, () => Results.Ok(new { status = "ok" }));

app.Run();
