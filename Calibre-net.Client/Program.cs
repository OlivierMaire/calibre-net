using Calibre_net.Client;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using System.Globalization;
using Microsoft.JSInterop;
using Calibre_net.Shared.Resources;
using Microsoft.Extensions.Localization;
using Calibre_net.Client.Services;
using Calibre_net.Shared.Contracts;
using EPubBlazor;
using AudioPlayerBlazor;
using ComicsBlazor;
using MudExtensions.Services;
using MudBlazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
// builder.Configuration.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
// builder.Configuration.AddJsonFile("customsettings.json", optional: true, reloadOnChange: true);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();

// builder.Services.AddAuthorizationCore();
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("Admin",
        policy => policy.RequireClaim("Permissions", "Admin"));
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
// builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

// await builder.Build().RunAsync();

builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});
SupportedCulturesOptions supportedCulturesOptions = new SupportedCulturesOptions();
// builder.Services.Configure<SupportedCulturesOptions>(options => {});
builder.Services.Configure<JsonStringLocalizerOptions>(options =>
{
    options.ResourcesPath = "Resources";
    options.ShowKeyNameIfEmpty = true;
    options.ShowDefaultIfEmpty = true;

});
builder.Services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();

// builder.Services.AddSingleton<IMessageService, MessageService>();
builder.Services.AddScoped<WindowIdService>();



// var apiOptions = builder.Configuration.GetSection(ApiServiceOptions.OptionName);
var baseAddress = builder.HostEnvironment.BaseAddress;
// var baseAddress = apiOptions["Host"];

// System.Diagnostics.Debug.WriteLine("aaa" + baseAddress);

builder.Services.AddHttpClient();

builder.Services.AddHttpClient("AuthenticationApi", client => client.BaseAddress = new Uri(baseAddress));


// builder.Services.AddTransient<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient("calibre-net.Api", client => client.BaseAddress = new Uri(baseAddress));
//  .AddHttpMessageHandler<AuthenticationDelegatingHandler>();

// builder.Services.Configure<CalibreConfiguration>(options => builder.Configuration.GetSection("calibre").Bind(options));

// ComicsBlazor_ServiceCollectionExtensions.AddComicsBlazorServices(builder.Services, ServiceLifetime.Singleton);
builder.Services.AddComicsBlazor(ServiceLifetime.Singleton);
builder.Services.AddEPubBlazor(ServiceLifetime.Singleton);
builder.Services.AddAudioPlayerBlazor(ServiceLifetime.Singleton);
// builder.Services.AddAudioPlayerBlazor(ServiceLifetime.Scoped);


builder.Services.RegisterServices(builder.Configuration);

builder.Services.AddMudExtensions();

var host = builder.Build();

// CultureInfo culture;
// var js = host.Services.GetRequiredService<IJSRuntime>();
// var result = await js.InvokeAsync<string>("blazorCulture.get");

// if (result != null)
// {
//     culture = new CultureInfo(result);
// }
// else
// {
//     culture = new CultureInfo(supportedCulturesOptions.SupportedCultures[0]);
//     await js.InvokeVoidAsync("blazorCulture.set", "en-US");
// }

// CultureInfo.DefaultThreadCurrentCulture = culture;
// CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();