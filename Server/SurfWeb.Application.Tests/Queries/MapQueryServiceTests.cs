using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SurfWeb.Application.Caching;
using SurfWeb.Application.Dtos;
using SurfWeb.Application.Options;
using SurfWeb.Application.Queries;
using SurfWeb.Application.Queries.Abstractions;
using SurfWeb.Domain.Entities;
using SurfWeb.Domain.ReadModels;
using Xunit;

namespace SurfWeb.Application.Tests.Queries;

public sealed class MapQueryServiceTests
{
    [Fact]
    public async Task GetMapsAsync_reuses_query_cache_within_ttl()
    {
        var maps = new FakeMapReadRepository(
        [
            new MapTier { Map = "surf_a", Tier = 2 },
        ]);
        var cache = new QueryCache(new MemoryCache(new MemoryCacheOptions()));
        var options = Microsoft.Extensions.Options.Options.Create(new SurfWebOptions
        {
            Cache = new CacheOptions { MapsMinutes = 10 },
        });
        var service = new MapQueryService(maps, new FakeUserReadRepository(), cache, options);

        await service.GetMapsAsync(tier: 2, search: null, page: 1, pageSize: 24);
        await service.GetMapsAsync(tier: 2, search: null, page: 1, pageSize: 24);

        Assert.Equal(1, maps.ListMapTiersCalls);
    }

    private sealed class FakeMapReadRepository(IReadOnlyList<MapTier> mapTiers) : IMapReadRepository
    {
        public int ListMapTiersCalls { get; private set; }

        public Task<(IReadOnlyList<MapTier> Maps, int Total)> ListMapTiersAsync(
            int? tier, string? search, int skip, int take, CancellationToken ct = default)
        {
            ListMapTiersCalls++;
            return Task.FromResult<(IReadOnlyList<MapTier>, int)>((mapTiers, mapTiers.Count));
        }

        public Task<MapTier?> FindMapTierAsync(string mapName, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Dictionary<string, int>> GetTiersByMapsAsync(
            IReadOnlyList<string> mapNames, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<int> CountDistinctCompletionsAsync(string mapName, CancellationToken ct = default) =>
            Task.FromResult(0);

        public Task<(float Time, int Auth)?> GetMainWorldRecordAsync(
            string mapName, CancellationToken ct = default) =>
            Task.FromResult<(float Time, int Auth)?>(null);

        public Task<IReadOnlyList<byte>> GetBonusTrackIdsAsync(
            string mapName, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<byte>>([]);

        public Task<Dictionary<string, int>> GetCompletionCountsByMapsAsync(
            IReadOnlyList<string> mapNames, CancellationToken ct = default) =>
            Task.FromResult(mapNames.ToDictionary(m => m, _ => 0));

        public Task<IReadOnlyList<MapWorldRecord>> GetWorldRecordsByMapsAsync(
            IReadOnlyList<string> mapNames, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MapWorldRecord>>([]);

        public Task<int> CountLeaderboardPlayerTimesAsync(
            string mapName, byte track, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<(int Auth, float MinTime)>> GetLeaderboardPlayerTimePageAsync(
            string mapName, byte track, int skip, int take, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlayerTime>> GetPlayerTimeRowsForLeaderboardAsync(
            string mapName, byte track, IReadOnlyList<int> authIds, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<int> CountLeaderboardStageTimesAsync(
            string mapName, byte track, byte stage, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<(int Auth, float MinTime)>> GetLeaderboardStageTimePageAsync(
            string mapName, byte track, byte stage, int skip, int take, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StageTime>> GetStageTimeRowsForLeaderboardAsync(
            string mapName, byte track, byte stage, IReadOnlyList<int> authIds, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeUserReadRepository : IUserReadRepository
    {
        public Task<User?> FindByAuthAsync(int auth, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<int> CountAllAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<User>> ListOrderedByPointsAsync(int skip, int take, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<User>> ListOrderedByPlaytimeAsync(int skip, int take, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<int> CountAheadByPointsAsync(float points, int auth, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<int> CountAheadByPlaytimeAsync(float playtime, int auth, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Dictionary<int, string?>> GetNamesByAuthIdsAsync(IReadOnlyList<int> authIds, CancellationToken ct = default) =>
            Task.FromResult<Dictionary<int, string?>>([]);
        public Task<Dictionary<string, int>> GetAuthsByNamesAsync(IReadOnlyList<string> names, CancellationToken ct = default) =>
            Task.FromResult<Dictionary<string, int>>([]);

        public Task<string?> GetNameAsync(int auth, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
