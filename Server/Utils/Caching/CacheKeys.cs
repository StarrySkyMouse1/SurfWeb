using SurfWeb.Core.Enums;

namespace SurfWeb.Utils.Caching;

public static class CacheKeys
{
    public const string RankingsPoints = "surfweb:rankings:points";

    public const string RankingsPlaytime = "surfweb:rankings:playtime";

    public const string RankingsCompletionsMain = "surfweb:rankings:completions:main";

    public const string RankingsCompletionsBonus = "surfweb:rankings:completions:bonus";

    public const string RankingsWrMain = "surfweb:rankings:wr:main";

    public const string RankingsWrBonus = "surfweb:rankings:wr:bonus";

    public const string RankingsWrStage = "surfweb:rankings:wr:stage";

    public const string RecordsRecent = "surfweb:records:recent";

    public static string Rankings(
        RankingType type,
        WrRankingScope? wrScope = null,
        TrackRankingScope? completionScope = null) =>
        type switch
        {
            RankingType.Playtime => RankingsPlaytime,
            RankingType.Completions => completionScope switch
            {
                TrackRankingScope.Bonus => RankingsCompletionsBonus,
                _ => RankingsCompletionsMain,
            },
            RankingType.Wr => wrScope switch
            {
                WrRankingScope.Bonus => RankingsWrBonus,
                WrRankingScope.Stage => RankingsWrStage,
                _ => RankingsWrMain,
            },
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

    public static string MapCheckpoints(string mapName, byte track, int limit) =>
        $"surfweb:maps:cp:{mapName.Trim().ToLowerInvariant()}:{track}:{limit}";

    private static string NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim().ToLowerInvariant();
}
