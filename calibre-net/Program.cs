using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using calibre_net.Components.Account;
using calibre_net.Data;
using MudBlazor.Services;
using Microsoft.Extensions.Localization;
using calibre_net.Middleware;
using MudExtensions.Services;
using calibre_net.Services;
using calibre_net.Components;
using HeimGuard;
using calibre_net.Shared.Resources;
using calibre_net.Client.Services;
using Namotion.Reflection;
using calibre_net.Migrations;
using calibre_net.Shared.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
builder.Configuration.AddJsonFile("customsettings.json", optional: true, reloadOnChange: true);

var listeningPort = builder.Configuration["calibre:basic:server:port"];
if (!string.IsNullOrEmpty(listeningPort))
    builder.WebHost.UseUrls($"https://localhost:{listeningPort}");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddControllers();


builder.Services.AddMudServices();
builder.Services.AddMudExtensions();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, CalibreNetClaimsPrincipalFactory>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddHeimGuard<UserPolicyHandler>()
    .AutomaticallyCheckPermissions()
    .MapAuthorizationPolicies();

// builder.Services.AddAuthorizationCore(options =>
// {
//     options.AddPolicy("Admin",
//         policy => policy.RequireClaim("Permissions", "Admin"));
// });

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// builder.Services.AddLocalization();

builder.Services.AddLocalization();
SupportedCulturesOptions supportedCulturesOptions = new SupportedCulturesOptions();
// builder.Services.Configure<SupportedCulturesOptions>(options =>
// options.SupportedCultures = supportedCultures);
builder.Services.Configure<JsonStringLocalizerOptions>(options =>
{
    options.ResourcesPath = "Resources";
    options.ShowKeyNameIfEmpty = true;
    options.ShowDefaultIfEmpty = true;

});
builder.Services.AddSingleton
     <IStringLocalizerFactory, JsonStringLocalizerFactory>();

// builder.Services.AddSingleton
//      <IMessageService, MessageService>();
builder.Services.AddScoped<WindowIdService>();


builder.Services.AddScoped<CalibreNetAuthenticationService>();
builder.Services.AddScoped<PasskeyService>();

// builder.Services.AddScoped<Fido2NetLib.Fido2Configuration>();
// builder.Services.AddScoped<Fido2NetLib.Fido2>();

builder.Services.RegisterServices(builder.Configuration);

// builder.Services.AddOptions<CalibreConfiguration>().BindConfiguration(CalibreConfiguration.SectionName);
builder.Services.Configure<CalibreConfiguration>(builder.Configuration.GetSection(CalibreConfiguration.SectionName));


builder.Services.AddFido2(options =>
      {
          options.ServerDomain = "localhost";
          options.ServerName = "Calibre.Net";
          options.ServerIcon = "https://static-00.iconduck.com/assets.00/apps-calibre-icon-512x512-qox1oz2k.png";
          options.Origins = new HashSet<string> { "https://localhost:7046/" };

          //   options.ServerDomain = Configuration["fido2:serverDomain"];
          //   options.ServerName = "FIDO2 Test";
          //   options.Origins = Configuration.GetSection("fido2:origins").Get<HashSet<string>>();
          //   options.TimestampDriftTolerance = Configuration.GetValue<int>("fido2:timestampDriftTolerance");
          //   options.MDSCacheDirPath = Configuration["fido2:MDSCacheDirPath"];
          //   options.BackupEligibleCredentialPolicy = Configuration.GetValue<Fido2Configuration.CredentialBackupPolicy>("fido2:backupEligibleCredentialPolicy");
          //   options.BackedUpCredentialPolicy = Configuration.GetValue<Fido2Configuration.CredentialBackupPolicy>("fido2:backedUpCredentialPolicy");
      });


// builder.Services.AddOpenApiDocument(); 

builder.Services.AddApiVersioning(o =>
        {
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            o.ReportApiVersions = true;
            o.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
                new Asp.Versioning.UrlSegmentApiVersionReader(),
                new Asp.Versioning.QueryStringApiVersionReader("api-version"),
                new Asp.Versioning.HeaderApiVersionReader("X-Version")
            // new MediaTypeApiVersionReader("ver")
            );
        })
        //.AddMvc()
        .AddApiExplorer(
        options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

var versionDescriptionProvider = builder.Services.BuildServiceProvider().GetService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
if (versionDescriptionProvider != null)
    foreach (var description in versionDescriptionProvider.ApiVersionDescriptions)
    {
        builder.Services.AddOpenApiDocument(config =>
            {
                config.DocumentName = description.GroupName;
                config.PostProcess = document =>
                {
                    document.Info.Version = description.GroupName;
                    document.Info.Title = "Calibre.Net Api";
                    document.Info.Description = "Calibre.Net Api Services.";
                    document.Info.TermsOfService = "None";
                    document.Info.Contact = new NSwag.OpenApiContact
                    {
                        Name = "Olivier Maire",
                        Email = string.Empty,
                        Url = ""
                    };
                };
                // config.OperationProcessors.Add(new OperationSecurityScopeProcessor("JWT token"));
                // config.AddSecurity("JWT token", new OpenApiSecurityScheme
                // {
                //     Type = OpenApiSecuritySchemeType.ApiKey,
                //     Name = "Authorization",
                //     Description = "Copy 'Bearer ' + valid JWT token into field",
                //     In = OpenApiSecurityApiKeyLocation.Header
                // });
                config.ApiGroupNames = new[] { description.GroupName };
            });
    }
var baseAddress = builder.Configuration["Calibre:ApiHost"];
if (string.IsNullOrEmpty(baseAddress))
{
    baseAddress = $"https://localhost:{listeningPort}";
}

builder.Services.AddHttpClient();


builder.Services.AddHttpClient("AuthenticationApi", client => client.BaseAddress = new Uri(baseAddress));


// builder.Services.AddTransient<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient("calibre-net.Api", client => client.BaseAddress = new Uri(baseAddress));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();

    // Add OpenAPI 3.0 document serving middleware
    // Available at: http://localhost:<port>/swagger/v1/swagger.json
    app.UseOpenApi();

    // Add web UIs to interact with the document
    // Available at: http://localhost:<port>/swagger
    app.UseSwaggerUi();

    // Add ReDoc UI to interact with the document
    // Available at: http://localhost:<port>/redoc
    app.UseReDoc(options =>
    {
        options.Path = "/redoc";
    });
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCulturesOptions.SupportedCultures[0])
    .AddSupportedCultures(supportedCulturesOptions.SupportedCultures)
    .AddSupportedUICultures(supportedCulturesOptions.SupportedCultures);

app.UseRequestLocalization(localizationOptions);

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseMiddleware<BlazorCookieAuthenticationMiddleware<ApplicationUser>>();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(calibre_net.Client.Pages.Counter).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();


app.Run();
