namespace SurfWeb.Core.Options;

public sealed class MapImageOptions
{
    /// <summary>
    /// 图床根 URL，如 https://cdn.example.com/maps/（末尾斜杠可选）。为空则前端不展示图片。
    /// </summary>
    public string? BaseUrl { get; init; }

    /// <summary>
    /// 地图名后的扩展名，如 .jpg；默认为空（图床路径直接以地图名结尾）。
    /// </summary>
    public string Extension { get; init; } = "";
}
