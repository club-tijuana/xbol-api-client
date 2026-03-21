using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Business.Extensions;
using Odasoft.XBOL.ClientAPI.Configs;
using Odasoft.XBOL.Commons.Settings;
using Odasoft.XBOL.Data;
using Odasoft.XBOL.Data.Extensions;
using Odasoft.XBOL.Models;
using System.Reflection;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

CorsSettings? corsSettings = builder.Configuration.GetSection("Cors").Get<CorsSettings>();
if (corsSettings is null)
    throw new InvalidOperationException(
        "Missing 'Cors' configuration. Add a Cors section (appsettings or env in GKE), e.g. " +
        "Cors__PolicyName and Cors__AcceptedOrigins__0, Cors__AcceptedOrigins__1, ...");
if (string.IsNullOrWhiteSpace(corsSettings.PolicyName))
    throw new InvalidOperationException(
        "Cors:PolicyName is required (e.g. env Cors__PolicyName=XBOLPolicy).");
corsSettings.AcceptedOrigins ??= Array.Empty<string>();
if (corsSettings.AcceptedOrigins.Length == 0)
    throw new InvalidOperationException(
        "Cors:AcceptedOrigins must include at least one origin (e.g. Cors__AcceptedOrigins__0=https://your-app.example).");

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Connection string 'Default' is missing. Set ConnectionStrings__Default (e.g. from External Secrets).");
builder.Services.AddDbContext<XBOLDbContext>(options =>
    options.UseNpgsql(connectionString));

#region AppSettings
Authentication? authenticationConfig = builder.Configuration.GetSection("Authentication").Get<Authentication>();
if (authenticationConfig is null)
    throw new InvalidOperationException(
        "Missing or invalid 'Authentication' configuration. " +
        "Ensure the 'Authentication' section exists (e.g. in appsettings.Production.json or via ConfigMap/Env in GKE) with at least 'AllowedUsers'.");
#endregion

builder.Services.AddCors(o => o.AddPolicy(corsSettings.PolicyName, builder =>
{
    builder
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithOrigins(corsSettings.AcceptedOrigins)
    .AllowCredentials();
}));

// Identity + EF Core store
builder.Services.AddDataProtection();

builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<Odasoft.XBOL.Models.Role>()
    .AddEntityFrameworkStores<XBOLDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Add services to the container.
builder.Services.ConfigureServices();
builder.Services.ConfigureRepositories();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add health check services
builder.Services.AddHealthChecks();

#region Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "es" };

    options.SetDefaultCulture("es");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
});
#endregion

// Add OpenAPI services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.UseInlineDefinitionsForEnums();
    c.SwaggerDoc("v1", new() { Title = "XBOL Client API", Version = "v1" });

    // Include XML comments if available
    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddSingleton(authenticationConfig);

var app = builder.Build();

// Enable middleware to serve generated OpenAPI as a JSON endpoint and the Swagger UI.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/client-api.json";
    });

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/client-api.json", "Client API");
    });

    app.MapGet(
        "/",
        context =>
        {
            context.Response.Redirect("/swagger/index.html");
            return Task.CompletedTask;
        }
    );
}

// Configure the HTTP request pipeline.

// Only use HTTPS redirection when running directly (Visual Studio, dotnet run)
// Containers handle TLS at load balancer/reverse proxy level
if (
    !app.Environment.IsProduction()
    || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"))
)
{
    app.UseHttpsRedirection();
}

app.UseRequestLocalization();
app.UseCors(corsSettings.PolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map health check endpoint for container health monitoring
app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var appName = app.Environment.ApplicationName ?? "unknown";
        var environment = app.Environment.EnvironmentName ?? "unknown";
        var dockerImageVersion = Environment.GetEnvironmentVariable("DOCKER_IMAGE_VERSION") ?? "unknown";
        var response = new
        {
            appName,
            environment,
            status = report.Status.ToString(),
            dockerImageVersion
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

app.Run();
