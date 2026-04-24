using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Odasoft.XBOL.Business.Configs;
using Odasoft.XBOL.Business.Extensions;
using Odasoft.XBOL.Business.Messages;
using Odasoft.XBOL.ClientAPI.Extensions;
using Odasoft.XBOL.ClientAPI.Filters;
using Odasoft.XBOL.ClientAPI.Schema;
using Odasoft.XBOL.Commons.Options;
using Odasoft.XBOL.Data;
using Odasoft.XBOL.Data.Extensions;
using Odasoft.XBOL.Models;
using System.Reflection;
using Wolverine;

if (args.Contains("--generate-schema"))
{
    var outputPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "appsettings.schema.json"));
    AppSettingsSchemaGenerator.GenerateAndWrite(outputPath);
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.ConfigureOptions();

builder.Services.AddDbContext<XBOLDbContext>((sp, options) =>
{
    var database = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    options.UseNpgsql(database.Database);
});

#region AppSettings
SearchSettings searchSettings = builder.Configuration.GetSection("SearchSettings").Get<SearchSettings>()!;
EventsTrackingSettings eventsTrackingSettings = builder.Configuration.GetSection("EventsTrackingSettings").Get<EventsTrackingSettings>()!;
#endregion

// Identity + EF Core store
builder.Services.AddDataProtection();

builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<Role>()
    .AddEntityFrameworkStores<XBOLDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

var corsOptions = builder.Configuration
    .GetSection(CorsOptions.SectionName)
    .Get<CorsOptions>() ?? new CorsOptions();

builder.Services.AddCors(o => o.AddPolicy(corsOptions.PolicyName, policy =>
{
    policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithOrigins(corsOptions.AcceptedOrigins)
        .AllowCredentials();
}));

// Add services to the container.
builder.Services.ConfigureServices();
builder.Services.ConfigureRepositories();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
});
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

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(CreateEventBookingCommand).Assembly);
});

builder.Services.AddSingleton(searchSettings);
builder.Services.AddSingleton(eventsTrackingSettings);

// Add Http Clients
builder.Services.ConfigureHttpClients();

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
app.UseCors(corsOptions.PolicyName);
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
