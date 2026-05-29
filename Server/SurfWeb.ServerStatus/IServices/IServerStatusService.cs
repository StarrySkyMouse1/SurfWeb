using SurfWeb.Core.Dtos;

namespace SurfWeb.ServerStatus.IServices;

public interface IServerStatusService
{
    Task<IReadOnlyList<ServerStatusDto>> GetStatusesAsync(CancellationToken ct = default);

    Task RefreshAsync(CancellationToken ct = default);
}
