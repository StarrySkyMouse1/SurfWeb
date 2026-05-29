using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;

namespace SurfWeb.Services.IServices;

public interface IRecordService
{
    Task<(IReadOnlyList<RecentRecordDto> Items, int Total)> GetRecentAsync(
        int page,
        int pageSize,
        RecentRecordFilter filter = RecentRecordFilter.All,
        WrRankingScope wrScope = WrRankingScope.Main,
        CancellationToken ct = default);
}
