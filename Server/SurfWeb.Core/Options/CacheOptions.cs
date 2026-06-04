namespace SurfWeb.Core.Options;

public sealed class CacheOptions
{
    public int MapsMinutes { get; init; } = 5;

    public int LeaderboardSeconds { get; init; } = 60;

    /// <summary>
    /// 全站排行榜全量缓存（最多 100 条）过期分钟数；过期后由下一次请求懒刷新。
    /// </summary>
    public int RankingsRefreshMinutes { get; init; } = 1;

    /// <summary>
    /// 最新记录全量缓存（最多 100 条）过期分钟数；过期后由下一次请求懒刷新。
    /// </summary>
    public int RecentRefreshMinutes { get; init; } = 1;

    /// <summary>
    /// SignalR 推送：后台重建最新记录快照的间隔（秒）。为 0 则禁用后台刷新与推送。
    /// </summary>
    public int RecentPushSeconds { get; init; } = 30;
}
