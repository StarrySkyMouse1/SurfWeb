using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;

namespace SurfWeb.Services.IServices;

public interface IRealtimeRecentRecordsService
{
    /// <summary>当前表最大 Id，用于推送游标初始化（不推送历史）。</summary>
    Task<(int PlayerTimeId, int StageTimeId)> GetHighWaterMarksAsync(CancellationToken ct = default);

    /// <summary>自给定游标之后的新完成记录（实时查库，含差距字段）。</summary>
    Task<RealtimeRecentRecordsPollResult> PollNewSinceAsync(
        int afterPlayerTimeId,
        int afterStageTimeId,
        CancellationToken ct = default);

    /// <summary>按筛选分页查询最新记录（实时查库）。</summary>
    Task<(IReadOnlyList<RealtimeRecentRecordDto> Items, int Total)> GetRecentPageAsync(
        RealtimeRecentRecordScope scope,
        int page,
        int pageSize,
        CancellationToken ct = default);
}

public sealed record RealtimeRecentRecordsPollResult(
    IReadOnlyList<RealtimeRecentRecordDto> Items,
    int LastPlayerTimeId,
    int LastStageTimeId);
