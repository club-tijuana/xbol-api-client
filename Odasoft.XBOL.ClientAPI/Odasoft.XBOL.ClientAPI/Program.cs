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

CorsSettings corsSettings = builder.Configuration.GetSection("Cors").Get<CorsSettings>();
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<XBOLDbContext>(options =>
    options.UseNpgsql(connectionString));

#region AppSettings
Authentication authenticationConfig = builder.Configuration.GetSection("Authentication").Get<Authentication>()!;
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
app.MapHealthChecks("/healthz");

app.Run();
