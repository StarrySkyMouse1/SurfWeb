using Microsoft.AspNetCore.SignalR;
using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;
using SurfWeb.Services.IServices;

namespace SurfWeb.Realtime.Hubs;

/// <summary>
/// 对外最新记录实时推送 Hub（数据来自 <see cref="IRealtimeRecentRecordsService"/> 直查数据库）。
/// </summary>
public sealed class RecordsHub(
    IRealtimeRecentRecordsService records,
    RealtimeRecordsPushState pushState) : Hub
{
    public const string RecordsUpdatedMethod = "RecordsUpdated";
    public const string RecentSnapshotMethod = "RecentSnapshot";

    /// <param name="scope"><see cref="RealtimeRecentRecordScope"/>；客户端传枚举名（如 <c>Main</c>）或整型值。</param>
    /// <param name="snapshotPageSize">订阅时附带的第 1 页条数，默认 10，最大 50。</param>
    public async Task SubscribeRecent(
        RealtimeRecentRecordScope scope = RealtimeRecentRecordScope.All,
        int snapshotPageSize = 10)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(scope));

        var pageSize = Math.Clamp(snapshotPageSize, 1, 50);
        var (items, total) = await records.GetRecentPageAsync(
            scope,
            1,
            pageSize,
            Context.ConnectionAborted);

        await Clients.Caller.SendAsync(
            RecentSnapshotMethod,
            new RealtimeRecentRecordsSnapshotMessage(pushState.Revision, scope, items, total),
            Context.ConnectionAborted);
    }

    public async Task UnsubscribeRecent(RealtimeRecentRecordScope scope = RealtimeRecentRecordScope.All)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupFor(scope));
    }

    /// <summary>将新记录按 scope 广播到对应订阅组（供 <see cref="RealtimeRecentRecordsPushWorker"/> 调用）。</summary>
    internal static async Task BroadcastNewRecordsAsync(
        IHubContext<RecordsHub> hub,
        string revision,
        IReadOnlyList<RealtimeRecentRecordDto> items,
        CancellationToken ct)
    {
        foreach (var targetScope in AllScopes())
        {
            var added = items.Where(item => MatchesScope(item, targetScope)).ToList();
            if (added.Count == 0)
                continue;

            var message = new RealtimeRecentRecordsUpdatedMessage(revision, targetScope, added);
            await hub.Clients
                .Group(GroupFor(targetScope))
                .SendAsync(RecordsUpdatedMethod, message, ct);
        }
    }

    private static string GroupFor(RealtimeRecentRecordScope scope) =>
        scope switch
        {
            RealtimeRecentRecordScope.Main => "recent:main",
            RealtimeRecentRecordScope.Bonus => "recent:bonus",
            RealtimeRecentRecordScope.Stage => "recent:stage",
            _ => "recent:all",
        };

    private static IEnumerable<RealtimeRecentRecordScope> AllScopes() =>
    [
        RealtimeRecentRecordScope.All,
        RealtimeRecentRecordScope.Main,
        RealtimeRecentRecordScope.Bonus,
        RealtimeRecentRecordScope.Stage,
    ];

    private static bool MatchesScope(RealtimeRecentRecordDto item, RealtimeRecentRecordScope scope) =>
        scope switch
        {
            RealtimeRecentRecordScope.Main => item.Stage is null && item.Track == 0,
            RealtimeRecentRecordScope.Bonus => item.Stage is null && item.Track > 0,
            RealtimeRecentRecordScope.Stage => item.Stage is not null,
            _ => true,
        };
}
