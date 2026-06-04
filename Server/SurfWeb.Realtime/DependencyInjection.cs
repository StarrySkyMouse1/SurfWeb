using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SurfWeb.Realtime.Hubs;

namespace SurfWeb.Realtime;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddSurfWebRealtime(this WebApplicationBuilder builder)
    {
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<RealtimeRecordsPushState>();
        builder.Services.AddSingleton<RealtimeRecentRecordsPushOrchestrator>();
        builder.Services.AddHostedService<RealtimeRecentRecordsPushWorker>();
        return builder;
    }

    public static WebApplication MapSurfWebRealtime(this WebApplication app)
    {
        app.MapHub<RecordsHub>("/hubs/records");
        return app;
    }
}
