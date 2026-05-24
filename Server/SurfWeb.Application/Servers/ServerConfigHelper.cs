using SurfWeb.Application.Options;

namespace SurfWeb.Application.Servers;

internal static class ServerConfigHelper
{
    public static List<ServerInfoOptions> GetActiveServers(SurfWebOptions options) =>
        options.Servers
            .Where(s => !string.IsNullOrWhiteSpace(s.Name) || !string.IsNullOrWhiteSpace(s.Address))
            .ToList();
}
