using DentalLocal.Client;
using DentalLocal.Client.Auth;
using DentalLocal.Client.Http;
using DentalLocal.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
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

builder.Services.AddMudServices();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<DraftAutosaveService>();

builder.Services.AddHttpClient<ApiClient>(client => client.BaseAddress = new Uri(ApiBaseAddress))
    .AddHttpMessageHandler<BearerTokenHandler>();

await builder.Build().RunAsync();
