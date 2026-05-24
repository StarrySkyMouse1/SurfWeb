namespace SurfWeb.Application.Options;



public sealed class SurfWebOptions

{

    public const string SectionName = "SurfWeb";



    /// <summary>由 <see cref="StyleOption.Default"/> 在绑定后解析；也可在配置中直接指定。</summary>

    public byte DefaultStyleId { get; set; }



    public string[] CorsOrigins { get; init; } = [];

    /// <summary>
    /// 读 API（/api/v1，不含 admin）最小响应时间（秒）。
    /// 实际耗时不足时补齐等待；为 0 则禁用。
    /// </summary>
    public double MinResponseDelaySeconds { get; init; } = 0.2;

    public CacheOptions Cache { get; init; } = new();

    public List<StyleOption> Styles { get; init; } = [];



    /// <summary>地图封面图床：前端拼接 BaseUrl + 地图名 + Extension。</summary>

    public MapImageOptions MapImages { get; init; } = new();



    /// <summary>游戏服务器列表（名称/连接串等）；在线状态由 Steam A2S 定时刷新。</summary>

    public List<ServerInfoOptions> Servers { get; init; } = [];

    public ServerQueryOptions ServerQuery { get; init; } = new();

}



public sealed class ServerInfoOptions

{

    public string Name { get; init; } = "";

    /// <summary>如 <c>connect host:port</c> 或 <c>host:port</c>。</summary>
    public string Address { get; init; } = "";

    /// <summary>可选，覆盖从 <see cref="Address"/> 解析的主机。</summary>
    public string? Host { get; init; }

    /// <summary>可选，覆盖从 <see cref="Address"/> 解析的端口。</summary>
    public int? Port { get; init; }

    /// <summary>离线时的占位地图（在线时由 Steam 覆盖）。</summary>
    public string? Map { get; init; }

    public int? Players { get; init; }

    /// <summary>可选上限覆盖；未设则用 Steam 返回值。</summary>
    public int? MaxPlayers { get; init; }

    public string? Note { get; init; }

}



public sealed class ServerQueryOptions

{

    /// <summary>后台刷新 Steam 状态的间隔（秒）。</summary>
    public int RefreshSeconds { get; init; } = 30;

    public int QueryTimeoutMs { get; init; } = 3000;

}



public sealed class CacheOptions

{

    public int MapsMinutes { get; init; } = 5;

    public int LeaderboardSeconds { get; init; } = 60;

    /// <summary>全站排行榜全量缓存（最多 100 条）过期分钟数；过期后由下一次请求懒刷新。</summary>
    public int RankingsRefreshMinutes { get; init; } = 1;

    /// <summary>最新记录全量缓存（最多 100 条）过期分钟数；过期后由下一次请求懒刷新。</summary>
    public int RecentRefreshMinutes { get; init; } = 1;

}



public sealed class StyleOption

{

    public byte Id { get; init; }

    public string Name { get; init; } = "";

    public bool Default { get; init; }

}



public sealed class MapImageOptions

{

    /// <summary>图床根 URL，如 https://cdn.example.com/maps/（末尾斜杠可选）。空则前端不展示图片。</summary>

    public string? BaseUrl { get; init; }



    /// <summary>地图名后的扩展名，如 .jpg；默认空（由图床路径直接以地图名结尾）。</summary>

    public string Extension { get; init; } = "";

}


