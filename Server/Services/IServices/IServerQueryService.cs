using SurfWeb.Data.Dtos;

namespace SurfWeb.Services.IServices;

public interface IServerService
{
    Task<IReadOnlyList<ServerStatusDto>> GetStatusesAsync(CancellationToken ct = default);
}
