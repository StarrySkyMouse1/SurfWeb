namespace SurfWeb.Core.Dtos;

public sealed record ServerStatusDto(
    string Name,
    string Address,
    bool Online,
    string? Map,
    int? MapTier,
    int Players,
    int MaxPlayers,
    string? Note,
    int SteamAppId,
    IReadOnlyList<ServerOnlinePlayerDto> OnlinePlayers);

public sealed record ServerOnlinePlayerDto(
    string Name,
    int? Auth,
    float DurationSeconds,
    string DurationDisplay);
