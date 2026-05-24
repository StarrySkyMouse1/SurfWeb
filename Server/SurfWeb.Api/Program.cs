using SurfWeb.Application;
using SurfWeb.Application.Web;
using SurfWeb.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true, reloadOnChange: true);

builder.Services.AddSurfWebOptions(builder.Configuration);
builder.Services.AddSurfWebApplication();
builder.Services.AddSurfWebInfrastructure(builder.Configuration);
builder.Services.AddSurfWebWebHost(builder.Configuration, builder.Environment);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSurfWebWebHost();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
