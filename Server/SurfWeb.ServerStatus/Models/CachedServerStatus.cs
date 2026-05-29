namespace SurfWeb.ServerStatus.Models;

public sealed class CachedServerStatus
{
    public required int Index { get; init; }

    public bool Online { get; init; }

    public string? Map { get; init; }

    public int? MapTier { get; init; }

    public int Players { get; init; }

    public int MaxPlayers { get; init; }

    public IReadOnlyList<CachedOnlinePlayer> OnlinePlayers { get; init; } = [];
}
