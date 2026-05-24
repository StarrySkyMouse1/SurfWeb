using SurfWeb.Application.Dtos;

namespace SurfWeb.Application.Abstractions;

public interface IServerQueryService
{
    Task<IReadOnlyList<ServerStatusDto>> GetStatusesAsync(CancellationToken ct = default);
}
