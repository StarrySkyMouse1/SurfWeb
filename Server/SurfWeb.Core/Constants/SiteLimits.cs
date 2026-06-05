namespace SurfWeb.Core.Constants;

public static class SiteLimits
{
    public const int MaxRankingsTotal = 100;

    public const int MaxRecentTotal = 100;

    /// <summary>提供 <c>after</c> 游标时单次最多返回条数。</summary>
    public const int ApiLatestRecordsCount = 50;

    /// <summary>未提供 <c>after</c> 时仅返回最新条数。</summary>
    public const int ApiLatestRecordsInitialCount = 1;

    public const int RecentScanBatch = 3000;
}
