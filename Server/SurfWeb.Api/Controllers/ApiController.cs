using Microsoft.AspNetCore.Mvc;
using SurfWeb.Configurations.Common;
using SurfWeb.Configurations.Security;
using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;
using SurfWeb.Services.IServices;

namespace SurfWeb.Api.Controllers;

[ApiController]
[Route("api/v1/api")]
public sealed class ApiController(IApiService api, IExternalApiTokenValidator tokenValidator) : ControllerBase
{
    /// <summary>
    /// 查询最新完成记录。
    /// </summary>
    /// <param name="token">访问令牌，须与配置 <c>SurfWeb:ExternalApi:LatestRecordsToken</c> 一致。</param>
    /// <param name="after">完成时间游标（ISO 8601）：仅返回严格晚于该时刻的记录（最多 50 条，升序）；省略则仅返回最新 1 条（降序）。</param>
    [HttpGet("records/latest")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ApiLatestRecordDto>>>> LatestRecords(
        [FromQuery] string? token,
        [FromQuery] string? type = null,
        [FromQuery] DateTimeOffset? after = null,
        CancellationToken ct = default)
    {
        if (!tokenValidator.ValidateLatestRecordsToken(token))
            return Unauthorized(ApiResponse<IReadOnlyList<ApiLatestRecordDto>>.Fail(ApiErrorCode.Unauthorized));

        if (!RealtimeRecentRecordScopeParser.TryParse(type, out var scope, out var typeError))
            return BadRequest(ApiResponse<IReadOnlyList<ApiLatestRecordDto>>.Fail(ApiErrorCode.BadRequest, typeError));

        var items = await api.GetLatestRecordsAsync(scope, after, ct);
        return Ok(ApiResponse<IReadOnlyList<ApiLatestRecordDto>>.Ok(items));
    }
}
