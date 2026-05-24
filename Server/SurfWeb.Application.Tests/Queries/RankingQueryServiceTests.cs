using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SurfWeb.Application.Caching;
using SurfWeb.Application.Options;
using SurfWeb.Application.Queries;
using SurfWeb.Application.Queries.Abstractions;
using SurfWeb.Domain.Entities;
using SurfWeb.Domain.ReadModels;
using Xunit;

namespace SurfWeb.Application.Tests.Queries;

public sealed class RankingQueryServiceTests
{
    [Fact]
    public async Task GetRankingsAsync_reuses_snapshot_within_ttl()
    {
        var users = new FakeUserReadRepository(userCount: 3);
        var cache = new QueryCache(new MemoryCache(new MemoryCacheOptions()));
        var options = Microsoft.Extensions.Options.Options.Create(new SurfWebOptions
        {
            Cache = new CacheOptions { RankingsRefreshMinutes = 10 },
        });
        var service = new RankingQueryService(users, new FakePlayerReadRepository(), cache, options);

        await service.GetRankingsAsync("points", page: 1, pageSize: 2);
        await service.GetRankingsAsync("points", page: 2, pageSize: 2);

        Assert.Equal(1, users.CountAllCalls);
        Assert.Equal(1, users.ListByPointsCalls);
    }

    private sealed class FakeUserReadRepository(int userCount) : IUserReadRepository
    {
        public int CountAllCalls { get; private set; }
        public int ListByPointsCalls { get; private set; }

        public Task<User?> FindByAuthAsync(int auth, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<int> CountAllAsync(CancellationToken ct = default)
        {
            CountAllCalls++;
            return Task.FromResult(userCount);
        }

        public Task<IReadOnlyList<User>> ListOrderedByPointsAsync(int skip, int take, CancellationToken ct = default)
        {
            ListByPointsCalls++;
            var users = Enumerable.Range(1, userCount)
                .Select(i => new User { Auth = i, Name = $"P{i}", Points = 100 - i, Playtime = i })
                .OrderByDescending(u => u.Points)
                .Skip(skip)
                .Take(take)
                .ToList();
            return Task.FromResult<IReadOnlyList<User>>(users);
        }

        public Task<IReadOnlyList<User>> ListOrderedByPlaytimeAsync(int skip, int take, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<int> CountAheadByPointsAsync(float points, int auth, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<int> CountAheadByPlaytimeAsync(float playtime, int auth, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Dictionary<int, string?>> GetNamesByAuthIdsAsync(IReadOnlyList<int> authIds, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string?> GetNameAsync(int auth, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Dictionary<string, int>> GetAuthsByNamesAsync(IReadOnlyList<string> names, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakePlayerReadRepository : IPlayerReadRepository
    {
        public Task<int> CountDistinctMapCompletionsAsync(int auth, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<int> CountPlayerTimesAsync(int auth, string? map, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<PlayerTime>> ListPlayerTimesPageAsync(int auth, string? map, int skip, int take, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<PlayerTime>> ListPlayerTimesForCompletionsAsync(int auth, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<PlayerTime>> ScanRecentPlayerTimesAsync(int take, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<(string Map, byte Track, float MinTime)>> GetMinTimesByMapTrackAsync(IReadOnlyList<string> maps, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<(string Map, float MinTime)>> GetMinTimesByMapForCompletionsAsync(IReadOnlyList<string> maps, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<CompletionRankEntry>> ListCompletionRankingsAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<int> CountCompletionRankingsAheadAsync(int completions, int auth, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
