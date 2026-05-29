namespace SurfWeb.Core.Enums;

/// <summary>
/// 最新记录筛选，与 GET /records/recent?filter= 查询参数一致（大小写不敏感；缺省为全部）。
/// </summary>
public enum RecentRecordFilter
{
    All,
    Main,
    Stage,
    Bonus,
    Wr,
}
