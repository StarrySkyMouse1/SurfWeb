using SurfWeb.Data.Dtos;

namespace SurfWeb.Services.IServices;

public interface IRecordService
{
    Task<(IReadOnlyList<RecentRecordDto> Items, int Total)> GetRecentAsync(
        int page, int pageSize, string? filter = null, CancellationToken ct = default);
}
