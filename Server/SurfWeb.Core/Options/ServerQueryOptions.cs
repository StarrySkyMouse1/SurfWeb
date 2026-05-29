namespace SurfWeb.Core.Options;

public sealed class ServerQueryOptions
{
    /// <summary>
    /// 后台刷新 Steam 服务器状态的间隔（秒）。
    /// </summary>
    public int RefreshSeconds { get; init; } = 30;

    public int QueryTimeoutMs { get; init; } = 3000;
}
