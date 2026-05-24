using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SurfWeb.Application.Caching;
using SurfWeb.Application.Options;
using SurfWeb.Application.Queries;
using SurfWeb.Application.Queries.Abstractions;
using SurfWeb.Domain.Constants;
using SurfWeb.Domain.Entities;
using SurfWeb.Domain.ReadModels;
using SurfWeb.Infrastructure;
using Xunit;

namespace SurfWeb.Application.Tests.Queries;

public sealed class RecordQueryServiceTests
{
    [Fact]
    public async Task GetRecentAsync_deduplicates_by_player_and_map()
    {
        var playerReadRepository = new FakePlayerReadRepository(
            recentRows:
            [
                new PlayerTime { Id = 1, Auth = 7, Map = "surf_alpha", Style = 4, Track = 0, Time = 40.000f, Date = 120 },
                new PlayerTime { Id = 2, Auth = 7, Map = "surf_alpha", Style = 4, Track = 1, Time = 39.500f, Date = 150 },
                new PlayerTime { Id = 3, Auth = 8, Map = "surf_bravo", Style = 4, Track = 0, Time = 50.000f, Date = 130 },
            ],
            minTimes:
            [
                ("surf_alpha", (byte)1, 39.500f),
                ("surf_bravo", (byte)0, 49.000f),
            ]);

        var service = CreateService(playerReadRepository);

        var (items, total) = await service.GetRecentAsync(page: 1, pageSize: 10);

        Assert.Equal(2, total);
        Assert.Collection(
            items,
            first =>
            {
                Assert.Equal(7, first.Auth);
                Assert.Equal("surf_alpha", first.Map);
                Assert.Equal((byte)1, first.Track);
                Assert.Equal(39.500f, first.Time);
                Assert.Null(first.GapFromWr);
            },
            second =>
            {
                Assert.Equal(8, second.Auth);
                Assert.Equal("surf_bravo", second.Map);
                Assert.NotNull(second.GapFromWr);
                Assert.True(Math.Abs(second.GapFromWr.Value - 1.000f) < 0.001f);
            });

        Assert.Equal(SiteLimits.RecentScanBatch, playerReadRepository.LastRecentScanTake);
    }

    [Fact]
    public async Task GetRecentAsync_reuses_snapshot_within_ttl()
    {
        var playerReadRepository = new FakePlayerReadRepository(
            recentRows:
            [
                new PlayerTime { Id = 1, Auth = 7, Map = "surf_alpha", Style = 4, Track = 0, Time = 40f, Date = 120 },
            ],
            minTimes: []);

        var service = CreateService(playerReadRepository);

        await service.GetRecentAsync(page: 1, pageSize: 5);
        await service.GetRecentAsync(page: 1, pageSize: 5);

        Assert.Equal(1, playerReadRepository.RecentScanCalls);
    }

    private static RecordQueryService CreateService(FakePlayerReadRepository playerReadRepository)
    {
        var cache = new QueryCache(new MemoryCache(new MemoryCacheOptions()));
        var options = Microsoft.Extensions.Options.Options.Create(new SurfWebOptions
        {
            Cache = new CacheOptions { RecentRefreshMinutes = 10 },
        });
        return new RecordQueryService(playerReadRepository, new FakeUserReadRepository(), cache, options);
    }

    [Fact]
    public void Infrastructure_registers_application_read_contracts()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Shavit"] = "Server=localhost;Database=shavit;User=readonly;Password=;",
            })
            .Build();

        services.AddSurfWebInfrastructure(configuration);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IMapReadRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IUserReadRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IPlayerReadRepository));
    }

    private sealed class FakeUserReadRepository : IUserReadRepository
    {
        public Task<User?> FindByAuthAsync(int auth, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> CountAllAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> ListOrderedByPointsAsync(int skip, int take, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> ListOrderedByPlaytimeAsync(int skip, int take, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> CountAheadByPointsAsync(float points, int auth, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> CountAheadByPlaytimeAsync(float playtime, int auth, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<Dictionary<int, string?>> GetNamesByAuthIdsAsync(IReadOnlyList<int> authIds, CancellationToken ct = default) =>
            Task.FromResult(authIds.ToDictionary<int, int, string?>(id => id, id => $"Player {id}"));

        public Task<string?> GetNameAsync(int auth, CancellationToken ct = default) =>
            Task.FromResult<string?>($"Player {auth}");

        public Task<Dictionary<string, int>> GetAuthsByNamesAsync(IReadOnlyList<string> names, CancellationToken ct = default) =>
            Task.FromResult<Dictionary<string, int>>([]);
    }

    private sealed class FakePlayerReadRepository(
        IReadOnlyList<PlayerTime> recentRows,
        IReadOnlyList<(string Map, byte Track, float MinTime)> minTimes) : IPlayerReadRepository
    {
        public int? LastRecentScanTake { get; private set; }
        public int RecentScanCalls { get; private set; }

        public Task<int> CountDistinctMapCompletionsAsync(int auth, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> CountPlayerTimesAsync(int auth, string? map, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlayerTime>> ListPlayerTimesPageAsync(int auth, string? map, int skip, int take, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlayerTime>> ListPlayerTimesForCompletionsAsync(int auth, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<PlayerTime>> ScanRecentPlayerTimesAsync(int take, CancellationToken ct = default)
        {
            LastRecentScanTake = take;
            RecentScanCalls++;
            return Task.FromResult<IReadOnlyList<PlayerTime>>(recentRows);
        }

        public Task<IReadOnlyList<(string Map, byte Track, float MinTime)>> GetMinTimesByMapTrackAsync(IReadOnlyList<string> maps, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<(string Map, byte Track, float MinTime)>>(minTimes);

        public Task<IReadOnlyList<(string Map, float MinTime)>> GetMinTimesByMapForCompletionsAsync(IReadOnlyList<string> maps, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CompletionRankEntry>> ListCompletionRankingsAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> CountCompletionRankingsAheadAsync(int completions, int auth, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
