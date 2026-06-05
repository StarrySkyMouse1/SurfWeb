namespace SurfWeb.Core.Enums;

/// <summary>
/// 实时最新记录推送筛选（SignalR），与 REST <see cref="RecentRecordFilter"/> 中 all/main/bonus/stage 一致。
/// </summary>
public enum RealtimeRecentRecordScope
{
    /// <summary>全部（0）</summary>
    All,

    /// <summary>主线 track=0（1）</summary>
    Main,

    /// <summary>奖励 track&gt;0（2）</summary>
    Bonus,

    /// <summary>阶段 stagetimes（3）</summary>
    Stage,
}
