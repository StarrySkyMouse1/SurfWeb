namespace SurfWeb.Core.Options;

/// <summary>对外集成 API（<c>/api/v1/api/*</c>）访问控制。</summary>
public sealed class ExternalApiOptions
{
    /// <summary>
    /// <c>GET /api/v1/api/records/latest</c> 查询参数 <c>token</c>，须与此完全一致；未配置或为空时拒绝所有请求。
    /// </summary>
    public string LatestRecordsToken { get; init; } = "";
}
