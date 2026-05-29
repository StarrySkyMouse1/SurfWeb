using SurfWeb.Core.Dtos;

namespace SurfWeb.Utils.Caching;

/// <summary>
/// 最新记录缓存：各筛选各保留 Top 100，按完成时间降序。
/// </summary>
public sealed record RecentRecordsSnapshot(
    IReadOnlyList<RecentRecordDto> All,
    IReadOnlyList<RecentRecordDto> Main,
    IReadOnlyList<RecentRecordDto> Stage,
    IReadOnlyList<RecentRecordDto> Bonus,
    IReadOnlyList<RecentRecordDto> WrMain,
    IReadOnlyList<RecentRecordDto> WrBonus,
    IReadOnlyList<RecentRecordDto> WrStage);
