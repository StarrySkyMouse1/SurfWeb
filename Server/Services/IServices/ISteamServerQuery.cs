namespace SurfWeb.Services.IServices;

public interface ISteamServerQuery
{
    SteamQueryServerInfo QueryServer(string host, int port, int timeoutMs);

    IReadOnlyList<SteamQueryPlayerInfo> QueryPlayers(string host, int port, int timeoutMs);
}

public sealed record SteamQueryServerInfo(
    string Name,
    string Map,
    int Players,
    int MaxPlayers);

public sealed record SteamQueryPlayerInfo(
    string Name,
    float DurationSeconds);
