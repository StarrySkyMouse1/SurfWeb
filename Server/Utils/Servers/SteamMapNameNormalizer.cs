namespace SurfWeb.Utils.Servers;

public static class SteamMapNameNormalizer
{
    /// <summary>
    /// 将 Steam A2S 返回的地图名规范为 Shavit 地图名（去除路径前缀与 .bsp 后缀）。
    /// </summary>
    /// <param name="map">原始地图名。</param>
    /// <returns>规范化后的地图名；无效时返回 null。</returns>
    public static string? Normalize(string? map)
    {
        if (string.IsNullOrWhiteSpace(map))
            return null;

        var m = map.Trim().Replace('\\', '/');
        var slash = m.LastIndexOf('/');
        if (slash >= 0 && slash < m.Length - 1)
            m = m[(slash + 1)..];

        if (m.EndsWith(".bsp", StringComparison.OrdinalIgnoreCase))
            m = m[..^4];

        return string.IsNullOrWhiteSpace(m) ? null : m;
    }
}