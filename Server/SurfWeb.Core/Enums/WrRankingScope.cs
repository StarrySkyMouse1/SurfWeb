namespace SurfWeb.Core.Enums;

/// <summary>
/// WR 排行榜子范围，与 GET /rankings?type=wr&amp;wrScope= 一致（大小写不敏感）。
/// </summary>
public enum WrRankingScope
{
    Main,
    Bonus,
    Stage,
}
