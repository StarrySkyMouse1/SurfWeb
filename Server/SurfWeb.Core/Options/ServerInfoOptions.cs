namespace SurfWeb.Core.Options;

public sealed class ServerInfoOptions
{
    public string Name { get; init; } = "";

    /// <summary>
    /// 连接地址，如 <c>connect host:port</c> 或 <c>host:port</c>。
    /// </summary>
    public string Address { get; init; } = "";

    /// <summary>
    /// 可选，覆盖从 <see cref="Address"/> 解析的主机。
    /// </summary>
    public string? Host { get; init; }

    /// <summary>
    /// 可选，覆盖从 <see cref="Address"/> 解析的端口。
    /// </summary>
    public int? Port { get; init; }

    /// <summary>
    /// 离线时的占位地图（在线时由 Steam 覆盖）。
    /// </summary>
    public string? Map { get; init; }

    public int? Players { get; init; }

    /// <summary>
    /// 可选人数上限覆盖；未设置则使用 Steam 返回值。
    /// </summary>
    public int? MaxPlayers { get; init; }

    public string? Note { get; init; }

    /// <summary>
    /// 可选 Steam AppID。「加入」行为：
    /// 未填或 <c>0</c> → <c>steam://connect</c>；
    /// 大于 0 → <c>steam://run/{AppId}//+connect …</c>。
    /// </summary>
    public int SteamAppId { get; init; }
}
