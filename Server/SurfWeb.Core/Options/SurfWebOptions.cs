namespace SurfWeb.Core.Options;

public sealed class SurfWebOptions
{
    public const string SectionName = "SurfWeb";

    /// <summary>成绩库提供程序：<see cref="DatabaseOptions.MySql"/>（默认）或 <see cref="DatabaseOptions.Sqlite"/>。</summary>
    public DatabaseOptions Database { get; init; } = new();

    /// <summary>
    /// 默认 style 编号；绑定后由 <see cref="StyleOption.Default"/> 解析，也可在配置中直接指定。
    /// </summary>
    public byte DefaultStyleId { get; set; }

    public string[] CorsOrigins { get; init; } = [];

    /// <summary>
    /// 读 API（/api/v1，不含 /api/v1/admin）最小响应时间（秒）。
    /// 实际耗时不足时补齐等待；为 0 则禁用。
    /// </summary>
    public double MinResponseDelaySeconds { get; init; } = 0.2;

    public CacheOptions Cache { get; init; } = new();

    public List<StyleOption> Styles { get; init; } = [];

    /// <summary>
    /// 地图封面图床：前端拼接 BaseUrl + 地图名 + Extension。
    /// </summary>
    public MapImageOptions MapImages { get; init; } = new();

    /// <summary>
    /// 游戏服务器列表（名称、连接串等）；在线状态由 Steam A2S 定时刷新。
    /// </summary>
    public List<ServerInfoOptions> Servers { get; init; } = [];

    public ServerQueryOptions ServerQuery { get; init; } = new();

    /// <summary>对外 REST（如最新记录 API）的 token 等。</summary>
    public ExternalApiOptions ExternalApi { get; init; } = new();
}
