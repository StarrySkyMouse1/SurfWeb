using Microsoft.Extensions.DependencyInjection;
using SurfWeb.Application;
using SurfWeb.Application.Abstractions;
using SurfWeb.Application.Commands.RecordRun;
using SurfWeb.Domain.Aggregates.Maps;
using SurfWeb.Domain.Aggregates.Players;
using SurfWeb.Domain.Aggregates.Runs;
using SurfWeb.Domain.DomainServices;
using SurfWeb.Domain.Events;
using SurfWeb.Domain.Repositories;
using SurfWeb.Domain.ValueObjects;
using Xunit;

namespace SurfWeb.Application.Tests.Commands;

public sealed class RecordRunUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_records_run_persists_aggregates_and_commits_once()
    {
        var playerRepository = new FakePlayerRepository();
        var mapRepository = new FakeMapRepository();
        var runRecordRepository = new FakeRunRecordRepository();
        var unitOfWork = new FakeUnitOfWork();
        var useCase = new RecordRunUseCase(
            playerRepository,
            mapRepository,
            runRecordRepository,
            new FakeWorldRecordPolicy(isWorldRecord: true),
            new FakeCompletionPolicy(RunKind.Completion),
            unitOfWork);

        var result = await useCase.ExecuteAsync(
            new RecordRunCommand(
                new PlayerId(7),
                "Player 7",
                new MapName("surf_alpha"),
                new StyleId(4),
                new TrackId(0),
                StageId: null,
                new RunTime(39.5),
                new DateTime(2026, 05, 22, 0, 0, 0, DateTimeKind.Utc)));

        Assert.True(result.IsWorldRecord);
        Assert.Equal(RunKind.Completion, result.RunKind);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        Assert.Single(playerRepository.SavedPlayers);
        Assert.Single(mapRepository.SavedMaps);
        Assert.Single(runRecordRepository.SavedRuns);
        Assert.Contains(mapRepository.SavedMaps.Single().DomainEvents, evt => evt is WorldRecordBrokenDomainEvent);
    }

    [Fact]
    public async Task ExecuteAsync_reuses_existing_aggregates_and_skips_world_record_event_when_policy_rejects()
    {
        var existingPlayer = Player.Create(new PlayerId(7), "Existing Player");
        var existingMap = Map.Create(new MapName("surf_alpha"));
        var previousBest = RunRecord.Create(
            new PlayerId(99),
            new MapName("surf_alpha"),
            new StyleId(4),
            new TrackId(0),
            null,
            new RunTime(38.5),
            RunKind.Completion,
            DateTime.UtcNow.AddMinutes(-10));
        previousBest.ClearDomainEvents();

        var playerRepository = new FakePlayerRepository(existingPlayer);
        var mapRepository = new FakeMapRepository(existingMap);
        var runRecordRepository = new FakeRunRecordRepository(previousBest);
        var unitOfWork = new FakeUnitOfWork();
        var useCase = new RecordRunUseCase(
            playerRepository,
            mapRepository,
            runRecordRepository,
            new FakeWorldRecordPolicy(isWorldRecord: false),
            new FakeCompletionPolicy(RunKind.Stage),
            unitOfWork);

        var result = await useCase.ExecuteAsync(
            new RecordRunCommand(
                new PlayerId(7),
                "Ignored Name",
                new MapName("surf_alpha"),
                new StyleId(4),
                new TrackId(0),
                new StageId(3),
                new RunTime(41.1),
                new DateTime(2026, 05, 22, 0, 0, 0, DateTimeKind.Utc)));

        Assert.False(result.IsWorldRecord);
        Assert.Equal(RunKind.Stage, result.RunKind);
        Assert.Same(existingPlayer, playerRepository.SavedPlayers.Single());
        Assert.Same(existingMap, mapRepository.SavedMaps.Single());
        Assert.DoesNotContain(existingMap.DomainEvents, evt => evt is WorldRecordBrokenDomainEvent);
    }

    [Fact]
    public void AddSurfWebApplication_registers_record_run_use_case()
    {
        var services = new ServiceCollection();

        services.AddSurfWebApplication();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecordRunUseCase));
    }

    private sealed class FakePlayerRepository(Player? existing = null) : IPlayerRepository
    {
        public List<Player> SavedPlayers { get; } = [];

        public Task<Player?> GetByIdAsync(PlayerId playerId, CancellationToken ct = default) =>
            Task.FromResult(existing);

        public Task SaveAsync(Player player, CancellationToken ct = default)
        {
            SavedPlayers.Add(player);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMapRepository(Map? existing = null) : IMapRepository
    {
        public List<Map> SavedMaps { get; } = [];

        public Task<Map?> GetByIdAsync(MapName mapName, CancellationToken ct = default) =>
            Task.FromResult(existing);

        public Task SaveAsync(Map map, CancellationToken ct = default)
        {
            SavedMaps.Add(map);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRunRecordRepository(RunRecord? existingBest = null) : IRunRecordRepository
    {
        public List<RunRecord> SavedRuns { get; } = [];

        public Task<RunRecord?> GetBestAsync(
            MapName mapName,
            StyleId styleId,
            TrackId trackId,
            StageId? stageId,
            RunKind kind,
            CancellationToken ct = default) =>
            Task.FromResult(existingBest);

        public Task AddAsync(RunRecord runRecord, CancellationToken ct = default)
        {
            SavedRuns.Add(runRecord);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWorldRecordPolicy(bool isWorldRecord) : IWorldRecordPolicy
    {
        public bool IsWorldRecord(RunRecord candidate, RunRecord? currentRecord) => isWorldRecord;
    }

    private sealed class FakeCompletionPolicy(RunKind runKind) : ICompletionPolicy
    {
        public RunKind ResolveRunKind(TrackId trackId, StageId? stageId) => runKind;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }
}
