using SurfWeb.Domain.Aggregates.Maps;
using SurfWeb.Domain.Aggregates.Players;
using SurfWeb.Domain.Aggregates.Runs;
using SurfWeb.Domain.Events;
using SurfWeb.Domain.ValueObjects;
using Xunit;

namespace SurfWeb.Application.Tests.Domain;

public sealed class RunDomainBehaviorTests
{
    [Fact]
    public void Value_objects_enforce_basic_invariants()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerId(0));
        Assert.Throws<ArgumentException>(() => new MapName("   "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StyleId(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StageId(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RunTime(0));
    }

    [Fact]
    public void Map_name_normalizes_input_for_domain_identity()
    {
        var mapName = new MapName("  Surf_Utopia  ");

        Assert.Equal("surf_utopia", mapName.Value);
    }

    [Fact]
    public void Run_record_collects_a_run_recorded_domain_event_when_created()
    {
        var run = RunRecord.Create(
            playerId: new PlayerId(7),
            mapName: new MapName("surf_alpha"),
            styleId: new StyleId(4),
            trackId: new TrackId(0),
            stageId: null,
            time: new RunTime(39.5),
            kind: RunKind.Completion,
            recordedAtUtc: new DateTime(2026, 05, 22, 0, 0, 0, DateTimeKind.Utc));

        var evt = Assert.Single(run.DomainEvents);
        var recorded = Assert.IsType<RunRecordedDomainEvent>(evt);

        Assert.Equal(run.Id, recorded.RunId);
        Assert.Equal(run.PlayerId, recorded.PlayerId);
        Assert.Equal(run.MapName, recorded.MapName);
        Assert.Equal(run.Time, recorded.Time);
    }

    [Fact]
    public void Map_collects_a_world_record_event_when_a_faster_run_is_applied()
    {
        var map = Map.Create(new MapName("surf_alpha"));
        var previous = RunRecord.Create(
            new PlayerId(7),
            new MapName("surf_alpha"),
            new StyleId(4),
            new TrackId(0),
            null,
            new RunTime(40.0),
            RunKind.Completion,
            DateTime.UtcNow.AddMinutes(-5));
        previous.ClearDomainEvents();

        var candidate = RunRecord.Create(
            new PlayerId(8),
            new MapName("surf_alpha"),
            new StyleId(4),
            new TrackId(0),
            null,
            new RunTime(39.5),
            RunKind.Completion,
            DateTime.UtcNow);
        candidate.ClearDomainEvents();

        map.RecordWorldRecord(candidate, previous);

        var evt = Assert.Single(map.DomainEvents);
        var broken = Assert.IsType<WorldRecordBrokenDomainEvent>(evt);

        Assert.Equal(candidate.Id, broken.RunId);
        Assert.Equal(candidate.PlayerId, broken.PlayerId);
        Assert.Equal(previous.PlayerId, broken.PreviousPlayerId);
        Assert.Equal(previous.Time, broken.PreviousTime);
    }

    [Fact]
    public void Player_is_modeled_as_an_aggregate_root()
    {
        var player = Player.Create(new PlayerId(7), "Player 7");

        Assert.Equal(new PlayerId(7), player.Id);
        Assert.Equal("Player 7", player.DisplayName);
    }

    [Fact]
    public void Player_tracks_unique_completions_per_style()
    {
        var player = Player.Create(new PlayerId(7), "Player 7");
        var style = new StyleId(4);

        player.RegisterCompletion(new MapName("surf_alpha"), style);
        player.RegisterCompletion(new MapName("surf_alpha"), style);
        player.RegisterCompletion(new MapName("surf_bravo"), style);

        Assert.Equal(2, player.CompletionCountFor(style));
    }
}
