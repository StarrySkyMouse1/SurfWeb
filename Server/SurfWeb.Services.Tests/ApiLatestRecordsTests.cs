using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Options;
using SurfWeb.Core.Dtos;
using SurfWeb.Core.Enums;
using SurfWeb.Core.Models;
using SurfWeb.Core.Options;
using SurfWeb.Repositories;
using SurfWeb.Services;

namespace SurfWeb.Services.Tests;

public sealed class ApiLatestRecordsTests
{
    [Fact]
    public async Task Bonus_records_include_track_and_gap_from_wr()
    {
        var api = CreateApi(
            playerTimes:
            [
                PlayerRun(id: 1, auth: 1, map: "surf_beginner", track: 1, time: 50, date: 100),
                PlayerRun(id: 2, auth: 1, map: "surf_beginner", track: 1, time: 45, date: 200),
                PlayerRun(id: 3, auth: 1, map: "surf_beginner", track: 1, time: 48, date: 300),
            ]);

        var items = await api.GetLatestRecordsAsync(
            RealtimeRecentRecordScope.Bonus,
            DateTimeOffset.FromUnixTimeSeconds(99));

        var firstRun = Assert.Single(items, i => i.RecordedAt == DateTimeOffset.FromUnixTimeSeconds(100));
        Assert.Equal("bonus", firstRun.Type);
        Assert.Equal((byte)1, firstRun.Track);
        Assert.Null(firstRun.Stage);
        Assert.Equal("奖励 1", firstRun.TypeLabel);
        Assert.Equal("+0.000", firstRun.GapFromWr);

        var improvedRun = Assert.Single(items, i => i.RecordedAt == DateTimeOffset.FromUnixTimeSeconds(200));
        Assert.Equal("+0.000", improvedRun.GapFromWr);

        var slowerRun = Assert.Single(items, i => i.RecordedAt == DateTimeOffset.FromUnixTimeSeconds(300));
        Assert.Equal("+3.000", slowerRun.GapFromWr);
    }

    [Fact]
    public async Task Stage_records_include_track_stage_and_gap_from_wr()
    {
        var api = CreateApi(
            stageTimes:
            [
                StageRun(id: 1, auth: 1, map: "surf_beginner", track: 2, stage: 3, time: 20, date: 100),
                StageRun(id: 2, auth: 1, map: "surf_beginner", track: 2, stage: 3, time: 18, date: 200),
                StageRun(id: 3, auth: 2, map: "surf_beginner", track: 2, stage: 3, time: 15, date: 300),
            ]);

        var items = await api.GetLatestRecordsAsync(
            RealtimeRecentRecordScope.Stage,
            DateTimeOffset.FromUnixTimeSeconds(99));

        var firstRun = Assert.Single(items, i => i.RecordedAt == DateTimeOffset.FromUnixTimeSeconds(100));
        Assert.Equal("stage", firstRun.Type);
        Assert.Equal((byte)2, firstRun.Track);
        Assert.Equal((byte)3, firstRun.Stage);
        Assert.Equal("阶段 3", firstRun.TypeLabel);
        Assert.Equal("+0.000", firstRun.GapFromWr);

        var improvedRun = Assert.Single(items, i => i.RecordedAt == DateTimeOffset.FromUnixTimeSeconds(200));
        Assert.Equal("+0.000", improvedRun.GapFromWr);
    }

    private static ApiService CreateApi(
        IReadOnlyList<PlayerTime>? playerTimes = null,
        IReadOnlyList<StageTime>? stageTimes = null)
    {
        var users = new[]
        {
            new User { Auth = 1, Name = "Alice" },
            new User { Auth = 2, Name = "Bob" },
        };
        var tiers = new[] { new MapTier { Map = "surf_beginner", Tier = 1 } };
        var engine = new ApiLatestRecordsEngine(
            new TestRepository<PlayerTime>(playerTimes ?? []),
            new TestRepository<StageTime>(stageTimes ?? []),
            new TestRepository<User>(users),
            new TestRepository<MapTier>(tiers),
            Options.Create(new SurfWebOptions { DefaultStyleId = 0 }));
        return new ApiService(engine);
    }

    private static PlayerTime PlayerRun(int id, int auth, string map, byte track, float time, int date) =>
        new()
        {
            Id = id,
            Auth = auth,
            Map = map,
            Track = track,
            Style = 0,
            Time = time,
            Date = date,
        };

    private static StageTime StageRun(int id, int auth, string map, byte track, byte stage, float time, int date) =>
        new()
        {
            Id = id,
            Auth = auth,
            Map = map,
            Track = track,
            Stage = stage,
            Style = 0,
            Time = time,
            Date = date,
        };

    private sealed class TestRepository<TEntity> : IQueryable<TEntity>, IAsyncEnumerable<TEntity>, IBaseRepository<TEntity>
        where TEntity : class
    {
        private readonly IQueryable<TEntity> _query;

        public TestRepository(IEnumerable<TEntity> source) =>
            _query = new TestAsyncEnumerable<TEntity>(source);

        public Type ElementType => _query.ElementType;
        public Expression Expression => _query.Expression;
        public IQueryProvider Provider => _query.Provider;
        public IEnumerator<TEntity> GetEnumerator() => _query.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IAsyncEnumerator<TEntity> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            ((IAsyncEnumerable<TEntity>)_query).GetAsyncEnumerator(cancellationToken);
    }

    private sealed class TestAsyncEnumerable<TEntity> :
        EnumerableQuery<TEntity>,
        IAsyncEnumerable<TEntity>,
        IQueryable<TEntity>
    {
        public TestAsyncEnumerable(IEnumerable<TEntity> enumerable) : base(enumerable)
        {
        }

        public TestAsyncEnumerable(Expression expression) : base(expression)
        {
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<TEntity>(this);

        public IAsyncEnumerator<TEntity> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new TestAsyncEnumerator<TEntity>(this.AsEnumerable().GetEnumerator());
    }

    private sealed class TestAsyncEnumerator<TEntity>(IEnumerator<TEntity> inner) : IAsyncEnumerator<TEntity>
    {
        public TEntity Current => inner.Current;

        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());
    }

    private sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
            new TestAsyncEnumerable<TElement>(expression);

        public object? Execute(Expression expression) => inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult).IsGenericType
                ? typeof(TResult).GetGenericArguments()[0]
                : typeof(TResult);

            var result = typeof(IQueryProvider)
                .GetMethod(nameof(IQueryProvider.Execute), [typeof(Expression)])!
                .MakeGenericMethod(resultType)
                .Invoke(inner, [expression]);

            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>))
            {
                return (TResult)typeof(Task)
                    .GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(resultType)
                    .Invoke(null, [result])!;
            }

            return (TResult)result!;
        }
    }
}
