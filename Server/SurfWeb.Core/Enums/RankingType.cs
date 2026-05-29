namespace SurfWeb.Core.Enums;

/// <summary>
/// 全站排行榜类型，与 GET /rankings?type= 查询参数一致（大小写不敏感）。
/// </summary>
public enum RankingType
{
    Points,
    Playtime,
    Completions,
    Wr,
}
