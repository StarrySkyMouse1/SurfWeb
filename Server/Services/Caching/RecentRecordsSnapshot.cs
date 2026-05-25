using SurfWeb.Data.Dtos;

namespace SurfWeb.Data.Caching;

/// <summary>
/// 最新记录缓存：各筛选各保留 Top 100，按完成时间降序。
/// </summary>
public sealed record RecentRecordsSnapshot(
    IReadOnlyList<RecentRecordDto> All,
    IReadOnlyList<RecentRecordDto> Main,
    IReadOnlyList<RecentRecordDto> Bonus,
    IReadOnlyList<RecentRecordDto> Wr);
