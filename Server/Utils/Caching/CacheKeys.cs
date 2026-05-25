namespace SurfWeb.Utils.Caching;

public static class CacheKeys
{
    public const string RankingsPoints = "surfweb:rankings:points";
    public const string RankingsPlaytime = "surfweb:rankings:playtime";
    public const string RankingsCompletions = "surfweb:rankings:completions";
    public const string RankingsWr = "surfweb:rankings:wr";
    public const string RecordsRecent = "surfweb:records:recent";

    public static string Rankings(string type) =>
        type.ToLowerInvariant() switch
        {
            "playtime" => RankingsPlaytime,
            "completions" => RankingsCompletions,
            "wr" => RankingsWr,
            _ => RankingsPoints,
        };

    public static string MapsList(int? tier, string? search, int page, int pageSize) =>
        $"surfweb:maps:list:{tier?.ToString() ?? "all"}:{NormalizeSearch(search)}:{page}:{pageSize}";

    public static string MapDetail(string mapName) =>
        $"surfweb:maps:detail:{mapName.Trim().ToLowerInvariant()}";

    public static string MapLeaderboard(
        string mapName,
        byte track,
        byte? stage,
        int page,
        int pageSize) =>
        $"surfweb:maps:lb:{mapName.Trim().ToLowerInvariant()}:{track}:{stage?.ToString() ?? "main"}:{page}:{pageSize}";

    private static string NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim().ToLowerInvariant();
}