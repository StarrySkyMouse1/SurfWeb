using SurfWeb.Infrastructure.Steam;
using Xunit;

namespace SurfWeb.Application.Tests.Steam;

/// <summary>Live A2S test. Set SURFWEB_LIVE_STEAM_HOST and SURFWEB_LIVE_STEAM_PORT to enable it.</summary>
public sealed class SteamServerQueryLiveTests
{
    [Fact]
    public void QueryServer_and_players_from_environment()
    {
        var host = Environment.GetEnvironmentVariable("SURFWEB_LIVE_STEAM_HOST");
        var portText = Environment.GetEnvironmentVariable("SURFWEB_LIVE_STEAM_PORT");

        if (string.IsNullOrWhiteSpace(host) || !int.TryParse(portText, out var port))
        {
            return;
        }

        var steam = new SteamServerQueryService();
        var info = steam.QueryServer(host, port, 8000);
        Assert.False(string.IsNullOrWhiteSpace(info.Map));
        Assert.True(info.MaxPlayers > 0);

        var players = steam.QueryPlayers(host, port, 8000);
        Assert.NotNull(players);
    }
}
