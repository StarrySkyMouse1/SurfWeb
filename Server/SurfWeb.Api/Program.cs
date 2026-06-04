using SurfWeb.Realtime;
using SurfWeb.Configurations;
using SurfWeb.ServerStatus;
using SurfWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddSurfWebLocalConfiguration();
builder.AddDefault();
builder.AddSurfWebOptions();
builder.AddSurfWebWebHost();
builder.AddSurfWeb();
builder.AddSurfWebServerStatus();
builder.AddSurfWebRealtime();

var app = builder.Build();

app.UseDefault();
app.UseSurfWebWebHost();
app.MapControllers();
app.MapSurfWebRealtime();
app.MapHealthChecks("/health");

app.Run();
