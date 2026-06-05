using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;
using SurfWeb.Services.IServices;

namespace SurfWeb.Services;

public sealed class ApiService(ApiLatestRecordsEngine engine) : IApiService
{
    public Task<IReadOnlyList<ApiLatestRecordDto>> GetLatestRecordsAsync(
        RealtimeRecentRecordScope? type = null,
        DateTimeOffset? after = null,
        CancellationToken ct = default) =>
        engine.QueryAsync(
            type ?? RealtimeRecentRecordScope.All,
            after,
            ct);
}
