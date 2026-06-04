namespace SurfWeb.Core.Dtos;

/// <summary>对外 API：最新完成记录（含与 WR 的差距文案）。</summary>
public sealed record ApiLatestRecordDto(
    string? PlayerName,
    string Map,
    int? Tier,
    /// <summary>记录类型：<c>main</c> / <c>bonus</c> / <c>stage</c>。</summary>
    string Type,
    /// <summary>赛道编号：主线为 0，奖励为奖励编号，阶段记录保留原始 track。</summary>
    byte Track,
    /// <summary>阶段编号；非阶段记录为 <c>null</c>。</summary>
    byte? Stage,
    /// <summary>面向展示的类型文案，如「主线」「奖励 1」「阶段 3」。</summary>
    string TypeLabel,
    DateTimeOffset? RecordedAt,
    /// <summary>与全服最快之差，如 <c>+0.050</c>；持 WR 时为 <c>+0.000</c>。</summary>
    string GapFromWr);
