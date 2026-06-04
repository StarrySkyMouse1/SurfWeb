using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;

namespace SurfWeb.Services.IServices;

public interface IApiService
{
    /// <summary>
    /// 查询最新完成记录（最多 100 条）。
    /// </summary>
    /// <param name="type">全部 / 主线 / 奖励 / 阶段；缺省为全部。</param>
    /// <param name="after">完成时间游标（ISO 8601）：仅返回严格晚于该时刻的记录，按时间升序；省略则返回最新若干条（时间降序）。</param>
    /// <param name="limit">条数，默认 100，最大 100。</param>
    Task<IReadOnlyList<ApiLatestRecordDto>> GetLatestRecordsAsync(
        RealtimeRecentRecordScope? type = null,
        DateTimeOffset? after = null,
        int limit = 100,
        CancellationToken ct = default);
}
