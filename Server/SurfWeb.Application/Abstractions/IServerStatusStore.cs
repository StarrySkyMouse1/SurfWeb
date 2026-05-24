namespace SurfWeb.Application.Abstractions;

public interface IServerStatusStore
{
    IReadOnlyList<CachedServerStatus> Snapshot { get; }

    DateTimeOffset? LastUpdatedUtc { get; }

    void Update(IReadOnlyList<CachedServerStatus> items);
}

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

public sealed class CachedOnlinePlayer
{
    public required string Name { get; init; }

    public int? Auth { get; init; }

    public float DurationSeconds { get; init; }
}
