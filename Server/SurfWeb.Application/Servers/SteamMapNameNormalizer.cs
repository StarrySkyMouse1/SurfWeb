namespace SurfWeb.Application.Servers;

public static class SteamMapNameNormalizer
{
    /// <summary>Steam A2S 返回的地图名可能带 .bsp 或路径前缀，规范为 Shavit 地图名。</summary>
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
