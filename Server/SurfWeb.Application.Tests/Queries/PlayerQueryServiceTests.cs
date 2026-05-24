using SurfWeb.Application.Queries;
using SurfWeb.Application.Queries.Abstractions;
using SurfWeb.Domain.Entities;
using SurfWeb.Domain.ReadModels;
using Xunit;

namespace SurfWeb.Application.Tests.Queries;

public sealed class PlayerQueryServiceTests
{
    [Fact]
    public void Constructor_uses_application_read_contracts_instead_of_domain_repositories()
    {
        var constructor = typeof(PlayerQueryService).GetConstructors().Single();
        var parameterTypes = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        Assert.Contains(typeof(IUserReadRepository), parameterTypes);
        Assert.Contains(typeof(IPlayerReadRepository), parameterTypes);
        Assert.Contains(typeof(IMapReadRepository), parameterTypes);
        Assert.DoesNotContain(parameterTypes, type => type.Namespace == "SurfWeb.Domain.Repositories");
    }

    [Fact]
    public async Task GetPlayerCompletionsAsync_uses_best_time_per_map_and_orders_by_latest_completion()
    {
        var service = new PlayerQueryService(
            new FakeUserReadRepository(),
            new FakePlayerReadRepository(
                completionRows:
                [
                    new PlayerTime { Id = 1, Auth = 7, Map = "surf_alpha", Style = 4, Track = 0, Time = 42.123f, Date = 150 },
                    new PlayerTime { Id = 2, Auth = 7, Map = "surf_alpha", Style = 4, Track = 0, Time = 41.500f, Date = 100 },
                    new PlayerTime { Id = 3, Auth = 7, Map = "surf_bravo", Style = 4, Track = 0, Time = 50.250f, Date = 200 },
                ],
                completionWorldRecords:
                [
                    ("surf_alpha", 40.000f),
                    ("surf_bravo", 49.500f),
                ]),
            new FakeMapReadRepository(new Dictionary<string, int>
            {
                ["surf_alpha"] = 2,
                ["surf_bravo"] = 3,
            }));

        var (items, total) = await service.GetPlayerCompletionsAsync(7, page: 1, pageSize: 20);

        Assert.Equal(2, total);
        Assert.Collection(
            items,
            first =>
            {
                Assert.Equal("surf_bravo", first.Map);
                Assert.Equal(3, first.Tier);
                Assert.Equal(50.250f, first.Time);
                Assert.Equal(49.500f, first.WorldRecordTime);
                Assert.NotNull(first.GapFromWr);
                Assert.True(Math.Abs(first.GapFromWr.Value - 0.750f) < 0.001f);
            },
            second =>
            {
                Assert.Equal("surf_alpha", second.Map);
                Assert.Equal(2, second.Tier);
                Assert.Equal(41.500f, second.Time);
                Assert.Equal(40.000f, second.WorldRecordTime);
                Assert.NotNull(second.GapFromWr);
                Assert.True(Math.Abs(second.GapFromWr.Value - 1.500f) < 0.001f);
            });
    }

    [Fact]
    public async Task GetPlayerTimesAsync_uses_player_time_repository_without_style_parameter()
    {
        var playerReadRepository = new FakePlayerReadRepository(
            pagedRows:
            [
                new PlayerTime { Id = 8, Auth = 7, Map = "surf_alpha", Style = 4, Track = 0, Time = 38.500f, Date = 100 },
            ]);

        var service = new PlayerQueryService(
            new FakeUserReadRepository(),
            playerReadRepository,
            new FakeMapReadRepository(new Dictionary<string, int>()));

        var (items, total) = await service.GetPlayerTimesAsync(7, "surf_alpha", page: 1, pageSize: 10);

        Assert.Single(items);
        Assert.Equal(1, total);
        Assert.Equal((7, "surf_alpha"), playerReadRepository.LastCountRequest);
        Assert.Equal((7, "surf_alpha", 0, 10), playerReadRepository.LastPageRequest);
    }

    private sealed class FakeUserReadRepository : IUserReadRepository
    {
        public Task<User?> FindByAuthAsync(int auth, CancellationToken ct = default) =>
            Task.FromResult<User?>(new User { Auth = auth, Name = $"Player {auth}", Points = 100, Playtime = 50 });

        public Task<int> CountAllAsync(CancellationToken ct = default) => Task.FromResult(1);

        public Task<IReadOnlyList<User>> ListOrderedByPointsAsync(int skip, int take, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<User>>([]);

        public Task<IReadOnlyList<User>> ListOrderedByPlaytimeAsync(int skip, int take, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<User>>([]);

        public Task<int> CountAheadByPointsAsync(float points, int auth, CancellationToken ct = default) => Task.FromResult(0);

        public Task<int> CountAheadByPlaytimeAsync(float playtime, int auth, CancellationToken ct = default) => Task.FromResult(0);

        public Task<Dictionary<int, string?>> GetNamesByAuthIdsAsync(IReadOnlyList<int> authIds, CancellationToken ct = default) =>
            Task.FromResult(authIds.ToDictionary<int, int, string?>(id => id, id => $"Player {id}"));

        public Task<Dictionary<string, int>> GetAuthsByNamesAsync(IReadOnlyList<string> names, CancellationToken ct = default) =>
            Task.FromResult<Dictionary<string, int>>([]);

        public Task<string?> GetNameAsync(int auth, CancellationToken ct = default) =>
            Task.FromResult<string?>($"Player {auth}");
    }

    private sealed class FakeMapReadRepository(Dictionary<string, int> tiersByMap) : IMapReadRepository
    {
        public Task<(IReadOnlyList<MapTier> Maps, int Total)> ListMapTiersAsync(int? tier, string? search, int skip, int take, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<MapTier?> FindMapTierAsync(string mapName, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Dictionary<string, int>> GetTiersByMapsAsync(IReadOnlyList<string> mapNames, CancellationToken ct = default) =>
            Task.FromResult(mapNames.Where(tiersByMap.ContainsKey).ToDictionary(name => name, name => tiersByMap[name]));

        public Task<int> CountDistinctCompletionsAsync(string mapName, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<(float Time, int Auth)?> GetMainWorldRecordAsync(string mapName, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<byte>> GetBonusTrackIdsAsync(string mapName, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Dictionary<string, int>> GetCompletionCountsByMapsAsync(IReadOnlyList<string> mapNames, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MapWorldRecord>> GetWorldRecordsByMapsAsync(IReadOnlyList<string> mapNames, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<int> CountLeaderboardPlayerTimesAsync(string mapName, byte track, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<(int Auth, float MinTime)>> GetLeaderboardPlayerTimePageAsync(string mapName, byte track, int skip, int take, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlayerTime>> GetPlayerTimeRowsForLeaderboardAsync(string mapName, byte track, IReadOnlyList<int> authIds, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<int> CountLeaderboardStageTimesAsync(string mapName, byte track, byte stage, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<(int Auth, float MinTime)>> GetLeaderboardStageTimePageAsync(string mapName, byte track, byte stage, int skip, int take, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StageTime>> GetStageTimeRowsForLeaderboardAsync(string mapName, byte track, byte stage, IReadOnlyList<int> authIds, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakePlayerReadRepository(
        IReadOnlyList<PlayerTime>? completionRows = null,
        IReadOnlyList<(string Map, float MinTime)>? completionWorldRecords = null,
        IReadOnlyList<PlayerTime>? pagedRows = null) : IPlayerReadRepository
    {
        public (int Auth, string? Map)? LastCountRequest { get; private set; }
        public (int Auth, string? Map, int Skip, int Take)? LastPageRequest { get; private set; }

        public Task<int> CountDistinctMapCompletionsAsync(int auth, CancellationToken ct = default) =>
            Task.FromResult(0);

        public Task<int> CountPlayerTimesAsync(int auth, string? map, CancellationToken ct = default)
        {
            LastCountRequest = (auth, map);
            return Task.FromResult(pagedRows?.Count ?? 0);
        }

        public Task<IReadOnlyList<PlayerTime>> ListPlayerTimesPageAsync(int auth, string? map, int skip, int take, CancellationToken ct = default)
        {
            LastPageRequest = (auth, map, skip, take);
            return Task.FromResult<IReadOnlyList<PlayerTime>>(pagedRows ?? []);
        }

        public Task<IReadOnlyList<PlayerTime>> ListPlayerTimesForCompletionsAsync(int auth, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PlayerTime>>(completionRows ?? []);

        public Task<IReadOnlyList<PlayerTime>> ScanRecentPlayerTimesAsync(int take, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<(string Map, byte Track, float MinTime)>> GetMinTimesByMapTrackAsync(IReadOnlyList<string> maps, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<(string Map, float MinTime)>> GetMinTimesByMapForCompletionsAsync(IReadOnlyList<string> maps, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<(string Map, float MinTime)>>(completionWorldRecords ?? []);

        public Task<IReadOnlyList<CompletionRankEntry>> ListCompletionRankingsAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<int> CountCompletionRankingsAheadAsync(int completions, int auth, CancellationToken ct = default) =>
            Task.FromResult(0);
    }
}
