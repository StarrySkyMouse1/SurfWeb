using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;

namespace SurfWeb.Services.IServices;

public interface IApiService
{
    /// <summary>
    /// 查询最新完成记录（固定 <see cref="SurfWeb.Core.Constants.SiteLimits.ApiLatestRecordsCount"/> 条）。
    /// </summary>
    /// <param name="type">全部 / 主线 / 奖励 / 阶段；缺省为全部。</param>
    /// <param name="after">完成时间游标（ISO 8601）：仅返回严格晚于该时刻的记录，按时间升序（最多 50 条）；省略则仅返回最新 1 条，按时间降序。</param>
    Task<IReadOnlyList<ApiLatestRecordDto>> GetLatestRecordsAsync(
        RealtimeRecentRecordScope? type = null,
        DateTimeOffset? after = null,
        CancellationToken ct = default);
}
