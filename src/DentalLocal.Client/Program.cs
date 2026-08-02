using System.Globalization;
using DentalLocal.Client;
using DentalLocal.Client.Auth;
using DentalLocal.Client.Http;
using DentalLocal.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// TODO(Фаза 6): при переходе на hosted-модель (Client раздаётся Server'ом) адрес API станет same-origin;
// сейчас Client и Server — отдельные процессы разработки (см. CORS в DentalLocal.Server/Program.cs).
const string ApiBaseAddress = "http://127.0.0.1:5005";

builder.Services.AddSingleton<AuthState>();
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
builder.Services.AddAuthorizationCore();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddTransient<RetryHandler>();

builder.Services.AddLocalization();
builder.Services.AddMudServices();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<DraftAutosaveService>();
builder.Services.AddScoped<SessionLockService>();

// Обычные запросы — 15 сек (см. промпт 42 аудита): без явного таймаута зависший backend держит
// UI в состоянии загрузки бесконечно, поскольку HttpClient по умолчанию ждёт 100 секунд.
builder.Services.AddHttpClient<ApiClient>(client =>
    {
        client.BaseAddress = new Uri(ApiBaseAddress);
        client.Timeout = TimeSpan.FromSeconds(15);
    })
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddHttpMessageHandler<RetryHandler>();

// Отдельный именованный клиент для загрузки файлов пациентов — 120 сек, крупные снимки/КТ не должны
// упираться в общий 15-секундный таймаут.
builder.Services.AddHttpClient(ApiClient.FileUploadClientName, client =>
    {
        client.BaseAddress = new Uri(ApiBaseAddress);
        client.Timeout = TimeSpan.FromSeconds(120);
    })
    .AddHttpMessageHandler<BearerTokenHandler>();

var host = builder.Build();

var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();
var languageCode = await CultureService.GetStoredCultureAsync(jsRuntime);
var cultureName = languageCode is not null && CultureService.SupportedCultures.TryGetValue(languageCode, out var mapped)
    ? mapped
    : "ru-RU";
var culture = new CultureInfo(cultureName);
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();
