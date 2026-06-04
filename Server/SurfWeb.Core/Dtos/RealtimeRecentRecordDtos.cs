using SurfWeb.Core.Enums;

namespace SurfWeb.Core.Dtos;

/// <summary>实时查询/推送用的最新完成记录（含与全服第一、个人最快的差距）。</summary>
public sealed record RealtimeRecentRecordDto(
    int Id,
    int Auth,
    string? PlayerName,
    string Map,
    byte Style,
    byte Track,
    byte? Stage,
    float Time,
    string TimeFormatted,
    DateTimeOffset? Date,
    int? Tier,
    float? FirstPlaceTime,
    string? FirstPlaceTimeFormatted,
    /// <summary>与全服最快（第一名）之差（秒）；持 WR 或差距 ≤0.001 时为 0。</summary>
    float? GapFromFirst,
    float? PersonalBestTime,
    string? PersonalBestTimeFormatted,
    /// <summary>当前成绩与完成前个人最快之差（秒）；刷新 PB 时为负数，首次无历史 PB 时为 null。</summary>
    float? GapFromPersonalBest);

public sealed record RealtimeRecentRecordsSnapshotMessage(
    string Revision,
    RealtimeRecentRecordScope Scope,
    IReadOnlyList<RealtimeRecentRecordDto> Items,
    int Total);

public sealed record RealtimeRecentRecordsUpdatedMessage(
    string Revision,
    RealtimeRecentRecordScope Scope,
    IReadOnlyList<RealtimeRecentRecordDto> Added);

/// <summary>手动或后台执行一轮「查新记录 + SignalR 推送」的结果（开发/运维用）。</summary>
public sealed record RealtimePushCycleResult(
    bool CursorWasInitialized,
    bool CursorInitializedThisCycle,
    int AfterPlayerTimeId,
    int AfterStageTimeId,
    int NewRecordCount,
    string Revision,
    bool BroadcastSent);
