namespace SurfWeb.Core.Enums;

/// <summary>
/// 实时最新记录推送筛选（SignalR），与 REST <see cref="RecentRecordFilter"/> 中 all/main/bonus/stage 一致。
/// </summary>
public enum RealtimeRecentRecordScope
{
    All,
    Main,
    Bonus,
    Stage,
}
