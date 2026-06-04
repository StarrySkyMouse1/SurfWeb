using Microsoft.AspNetCore.Mvc;
using SurfWeb.Configurations.Common;
using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;
using SurfWeb.Services.IServices;

namespace SurfWeb.Api.Controllers;

[ApiController]
[Route("api/v1/api")]
public sealed class ApiController(IApiService api) : ControllerBase
{
    /// <summary>
    /// 查询最新完成记录。
    /// </summary>
    /// <param name="type">类型：all / main / bonus / stage；缺省为全部。</param>
    /// <param name="after">完成时间游标（ISO 8601）：仅返回严格晚于该时刻的记录；省略则返回最新若干条（时间降序）。</param>
    /// <param name="limit">条数，默认 100，最大 100。</param>
    [HttpGet("records/latest")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ApiLatestRecordDto>>>> LatestRecords(
        [FromQuery] RealtimeRecentRecordScope? type = null,
        [FromQuery] DateTimeOffset? after = null,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var items = await api.GetLatestRecordsAsync(type, after, limit, ct);
        return Ok(ApiResponse<IReadOnlyList<ApiLatestRecordDto>>.Ok(items));
    }
}
