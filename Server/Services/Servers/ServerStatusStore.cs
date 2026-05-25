using SurfWeb.Services.IServices;

namespace SurfWeb.Data.Servers;

public sealed class ServerStatusStore : IServerStatusStore
{
    private IReadOnlyList<CachedServerStatus> _snapshot = [];

    public IReadOnlyList<CachedServerStatus> Snapshot => _snapshot;

    public DateTimeOffset? LastUpdatedUtc { get; private set; }

    public void Update(IReadOnlyList<CachedServerStatus> items)
    {
        _snapshot = items;
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }
}
