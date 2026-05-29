namespace SurfWeb.ServerStatus.Models;

public sealed class CachedOnlinePlayer
{
    public required string Name { get; init; }

    public int? Auth { get; init; }

    public float DurationSeconds { get; init; }
}
