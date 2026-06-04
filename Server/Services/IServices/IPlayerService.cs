using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;

namespace SurfWeb.Services.IServices;

public interface IPlayerService
{
    Task<PlayerSummaryDto?> GetPlayerAsync(int auth, CancellationToken ct = default);

    Task<(PlayerRecordsPageDto Page, int Total)?> GetPlayerRecordsAsync(
        int auth,
        PlayerRecordCategory category,
        PlayerRecordScope scope,
        int page,
        int pageSize,
        int? tier = null,
        CancellationToken ct = default);
}
