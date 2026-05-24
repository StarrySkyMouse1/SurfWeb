# Server Strong DDD Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rework the `Server` backend into a strongly DDD-shaped architecture with visible aggregates, value objects, domain events, command/query separation, and documentation that clearly contrasts it with three-layer architecture.

**Architecture:** Keep the external read APIs working, but split the backend into a write-side DDD core and a read-side query model. `Domain` becomes the business center with aggregates, value objects, domain events, and repository interfaces. `Application` separates commands from queries. `Infrastructure` implements aggregate repositories and read repositories. `Api` remains a thin adapter and composition root.

**Three-Layer Contrast:** This refactor is intentionally teaching-oriented. The result should visibly differ from `Controller -> Service -> Repository -> Table Entity` by introducing `Controller -> Command UseCase -> Aggregate / Domain Service -> Repository Interface`, while queries stay on a separate read side.

**Tech Stack:** .NET 8, ASP.NET Core, EF Core, Pomelo MySQL, xUnit

---

### Task 1: Establish failing characterization tests for the desired DDD shape

**Files:**
- Modify: `Server/SurfWeb.Application.Tests/Queries/PlayerQueryServiceTests.cs`
- Modify: `Server/SurfWeb.Application.Tests/Queries/RecordQueryServiceTests.cs`
- Create: `Server/SurfWeb.Application.Tests/Architecture/DomainShapeTests.cs`

- [ ] **Step 1: Write failing architecture tests**

```csharp
[Fact]
public void Domain_contains_value_objects_for_identity_and_time()
{
    Assert.NotNull(typeof(PlayerId));
    Assert.NotNull(typeof(MapName));
    Assert.NotNull(typeof(RunTime));
}

[Fact]
public void Domain_contains_aggregate_roots_with_behavior()
{
    Assert.NotNull(typeof(Player));
    Assert.NotNull(typeof(Map));
    Assert.NotNull(typeof(RunRecord));
}

[Fact]
public void Application_separates_commands_and_queries()
{
    Assert.NotNull(typeof(RecordRunCommand));
    Assert.NotNull(typeof(IRecordRunUseCase));
    Assert.NotNull(typeof(IMapReadRepository));
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj`
Expected: FAIL because the new domain types and command-side abstractions do not exist yet.

- [ ] **Step 3: Keep the existing read-side regression tests untouched**

```text
Do not rewrite:
- GetPlayerCompletionsAsync_uses_best_time_per_map_and_orders_by_latest_completion
- GetRecentAsync_returns_default_style_rows_only_and_deduplicates_by_player_and_map
```

- [ ] **Step 4: Re-run tests and confirm the failure is specifically about missing DDD types**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj --filter Domain`
Expected: FAIL with missing type or assertion failures tied to new DDD concepts.

- [ ] **Step 5: Commit**

```bash
git add Server/SurfWeb.Application.Tests
git commit -m "test: define strong ddd architecture expectations"
```

### Task 2: Introduce value objects and domain event primitives

**Files:**
- Create: `Server/SurfWeb.Domain/Common/Entity.cs`
- Create: `Server/SurfWeb.Domain/Common/AggregateRoot.cs`
- Create: `Server/SurfWeb.Domain/Events/IDomainEvent.cs`
- Create: `Server/SurfWeb.Domain/Events/RunRecordedDomainEvent.cs`
- Create: `Server/SurfWeb.Domain/Events/WorldRecordBrokenDomainEvent.cs`
- Create: `Server/SurfWeb.Domain/ValueObjects/PlayerId.cs`
- Create: `Server/SurfWeb.Domain/ValueObjects/MapName.cs`
- Create: `Server/SurfWeb.Domain/ValueObjects/StyleId.cs`
- Create: `Server/SurfWeb.Domain/ValueObjects/TrackId.cs`
- Create: `Server/SurfWeb.Domain/ValueObjects/StageId.cs`
- Create: `Server/SurfWeb.Domain/ValueObjects/RunTime.cs`

- [ ] **Step 1: Write failing unit tests for value object invariants**

```csharp
[Fact]
public void MapName_rejects_blank_values()
{
    Assert.Throws<ArgumentException>(() => MapName.Create(" "));
}

[Fact]
public void RunTime_rejects_non_positive_values()
{
    Assert.Throws<ArgumentOutOfRangeException>(() => RunTime.FromSeconds(0));
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj --filter MapName_rejects_blank_values|RunTime_rejects_non_positive_values`
Expected: FAIL because the value objects do not exist yet.

- [ ] **Step 3: Add minimal value objects and domain event interfaces**

```csharp
public readonly record struct PlayerId(int Value)
{
    public static PlayerId Create(int value)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
        return new PlayerId(value);
    }
}
```

- [ ] **Step 4: Re-run tests to verify the invariants pass**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj`
Expected: PASS for the new value object tests; other DDD-shape tests may still fail.

- [ ] **Step 5: Commit**

```bash
git add Server/SurfWeb.Domain Server/SurfWeb.Application.Tests
git commit -m "feat: add domain value objects and events"
```

### Task 3: Replace table-shaped domain entities with aggregate-centered domain models

**Files:**
- Create: `Server/SurfWeb.Domain/Aggregates/Players/Player.cs`
- Create: `Server/SurfWeb.Domain/Aggregates/Maps/Map.cs`
- Create: `Server/SurfWeb.Domain/Aggregates/Runs/RunRecord.cs`
- Create: `Server/SurfWeb.Domain/Aggregates/Runs/RunKind.cs`
- Create: `Server/SurfWeb.Domain/DomainServices/IWorldRecordPolicy.cs`
- Create: `Server/SurfWeb.Domain/DomainServices/ICompletionPolicy.cs`
- Create: `Server/SurfWeb.Domain/Repositories/IPlayerRepository.cs`
- Create: `Server/SurfWeb.Domain/Repositories/IMapRepository.cs`
- Create: `Server/SurfWeb.Domain/Repositories/IRunRecordRepository.cs`
- Keep: existing `Entities` and `ReadModels` as persistence/read-side types for now, but stop treating them as the domain core

- [ ] **Step 1: Add failing aggregate behavior tests**

```csharp
[Fact]
public void Player_register_completion_updates_completion_count()
{
    var player = Player.Create(PlayerId.Create(7), "Alice");
    player.RegisterCompletion(MapName.Create("surf_alpha"), StyleId.Create(0));
    Assert.Equal(1, player.CompletionCountFor(StyleId.Create(0)));
}

[Fact]
public void RunRecord_mark_as_world_record_raises_domain_event()
{
    var run = RunRecord.CreateMain(...);
    run.MarkAsWorldRecord();
    Assert.Contains(run.DomainEvents, e => e is WorldRecordBrokenDomainEvent);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj --filter Player_register_completion_updates_completion_count|RunRecord_mark_as_world_record_raises_domain_event`
Expected: FAIL because aggregates and behaviors do not exist yet.

- [ ] **Step 3: Add aggregate roots and repository interfaces**

```csharp
public sealed class Player : AggregateRoot<PlayerId>
{
    private readonly Dictionary<StyleId, HashSet<MapName>> _completedMaps = new();

    public void RegisterCompletion(MapName map, StyleId style)
    {
        if (!_completedMaps.TryGetValue(style, out var maps))
        {
            maps = new HashSet<MapName>();
            _completedMaps[style] = maps;
        }
        maps.Add(map);
    }
}
```

- [ ] **Step 4: Re-run tests to verify aggregate behavior passes**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj`
Expected: PASS for aggregate behavior tests; read-side tests remain green.

- [ ] **Step 5: Commit**

```bash
git add Server/SurfWeb.Domain Server/SurfWeb.Application.Tests
git commit -m "feat: add aggregate-centered domain model"
```

### Task 4: Introduce command-side application use cases

**Files:**
- Create: `Server/SurfWeb.Application/Commands/RecordRun/RecordRunCommand.cs`
- Create: `Server/SurfWeb.Application/Commands/RecordRun/RecordRunResult.cs`
- Create: `Server/SurfWeb.Application/Commands/RecordRun/IRecordRunUseCase.cs`
- Create: `Server/SurfWeb.Application/Commands/RecordRun/RecordRunUseCase.cs`
- Create: `Server/SurfWeb.Application/Abstractions/IUnitOfWork.cs`
- Modify: `Server/SurfWeb.Application/DependencyInjection.cs`

- [ ] **Step 1: Write failing use-case tests**

```csharp
[Fact]
public async Task RecordRunUseCase_loads_aggregates_applies_policies_and_saves()
{
    // Arrange repositories and policies.
    // Act with a RecordRunCommand.
    // Assert aggregate repository SaveAsync and unit of work commit are called.
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj --filter RecordRunUseCase`
Expected: FAIL because the command-side use case does not exist yet.

- [ ] **Step 3: Implement the command-side application service**

```csharp
public sealed class RecordRunUseCase : IRecordRunUseCase
{
    public async Task<RecordRunResult> ExecuteAsync(RecordRunCommand command, CancellationToken ct = default)
    {
        // Load player/map aggregates, create run aggregate, apply policies, save, commit.
    }
}
```

- [ ] **Step 4: Re-run tests to verify the command path passes**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj`
Expected: PASS for command-side tests and existing read-side tests.

- [ ] **Step 5: Commit**

```bash
git add Server/SurfWeb.Application Server/SurfWeb.Application.Tests
git commit -m "feat: add command-side application use case"
```

### Task 5: Split infrastructure into write repositories and read repositories

**Files:**
- Create: `Server/SurfWeb.Infrastructure/Persistence/Configurations/*.cs`
- Create: `Server/SurfWeb.Infrastructure/Repositories/Write/PlayerRepository.cs`
- Create: `Server/SurfWeb.Infrastructure/Repositories/Write/MapRepository.cs`
- Create: `Server/SurfWeb.Infrastructure/Repositories/Write/RunRecordRepository.cs`
- Create: `Server/SurfWeb.Infrastructure/Persistence/EfUnitOfWork.cs`
- Create: `Server/SurfWeb.Infrastructure/Queries/MapReadRepository.cs`
- Create: `Server/SurfWeb.Infrastructure/Queries/PlayerReadRepository.cs`
- Create: `Server/SurfWeb.Infrastructure/Queries/UserReadRepository.cs`
- Modify: `Server/SurfWeb.Infrastructure/DependencyInjection.cs`
- Modify: `Server/SurfWeb.Infrastructure/Persistence/ShavitDbContext.cs`
- Delete or move: current mixed repositories under `Server/SurfWeb.Infrastructure/Repositories/*.cs`

- [ ] **Step 1: Add failing DI tests**

```csharp
[Fact]
public void Infrastructure_registers_write_repositories_and_read_repositories_separately()
{
    // Assert IPlayerRepository binds to write repo and IPlayerReadRepository binds to read repo.
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj --filter Infrastructure_registers_write_repositories_and_read_repositories_separately`
Expected: FAIL because DI still points to the mixed repository set.

- [ ] **Step 3: Implement separate read/write infrastructure types**

```csharp
services.AddScoped<IPlayerRepository, PlayerRepository>();
services.AddScoped<IMapRepository, MapRepository>();
services.AddScoped<IRunRecordRepository, RunRecordRepository>();
services.AddScoped<IPlayerReadRepository, PlayerReadRepository>();
services.AddScoped<IMapReadRepository, MapReadRepository>();
services.AddScoped<IUserReadRepository, UserReadRepository>();
services.AddScoped<IUnitOfWork, EfUnitOfWork>();
```

- [ ] **Step 4: Re-run tests to verify the split passes**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj`
Expected: PASS for read-side, write-side, and DI tests.

- [ ] **Step 5: Commit**

```bash
git add Server/SurfWeb.Infrastructure Server/SurfWeb.Application.Tests
git commit -m "refactor: split infrastructure read and write paths"
```

### Task 6: Keep API read behavior stable and optionally expose a teaching command endpoint

**Files:**
- Modify: `Server/SurfWeb.Api/Program.cs`
- Modify: `Server/SurfWeb.Api/Controllers/V1/*.cs`
- Optionally create: `Server/SurfWeb.Api/Controllers/V1/AdminRunsController.cs`

- [ ] **Step 1: Add failing API-focused tests if a command endpoint is exposed**

```csharp
[Fact]
public async Task Post_run_command_returns_world_record_metadata_when_policy_marks_it_as_wr()
{
    // Use a lightweight application fake to verify HTTP mapping only.
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj --filter Post_run_command`
Expected: FAIL if the teaching endpoint is added; skip this step if the endpoint remains internal-only.

- [ ] **Step 3: Keep read endpoints thin and wire command use case**

```csharp
app.MapControllers();
// Read controllers continue to call query services only.
// Optional command controller calls IRecordRunUseCase.
```

- [ ] **Step 4: Re-run backend tests**

Run: `dotnet test Server/SurfWeb.slnx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Server/SurfWeb.Api Server/SurfWeb.Application.Tests
git commit -m "refactor: keep api thin over ddd application layer"
```

### Task 7: Update design documents with explicit Three-Layer vs DDD comparison

**Files:**
- Modify: `doc/design.md`
- Modify: `docs/superpowers/plans/2026-05-22-surfweb.md`
- Modify: `docs/superpowers/plans/2026-05-22-server-ddd-read-side-refactor.md`

- [ ] **Step 1: Add the architecture comparison section**

```markdown
## DDD 对照说明

- 三层：Controller -> Service -> Repository -> Table Entity
- 本实现：Controller -> Application Use Case -> Aggregate / Domain Service -> Repository Interface
- 查询另走 Query Side，不参与聚合建模
```

- [ ] **Step 2: Document the new domain core**

```markdown
- 聚合根：Player / Map / RunRecord
- 值对象：PlayerId / MapName / StyleId / TrackId / StageId / RunTime
- 领域事件：RunRecordedDomainEvent / WorldRecordBrokenDomainEvent
```

- [ ] **Step 3: Run a docs drift scan**

Run: `rg -n "三层|DDD-lite|仓储接口（`IMapRepository` 等）|style 参数忽略" doc/design.md docs/superpowers/plans`
Expected: only the new comparison language remains; stale wording is removed.

- [ ] **Step 4: Commit**

```bash
git add doc/design.md docs/superpowers/plans
git commit -m "docs: explain strong ddd architecture"
```

### Task 8: Final verification and delivery commit

**Files:**
- Modify: all touched `Server/*` files
- Modify: `doc/design.md`
- Modify: `docs/superpowers/plans/*.md`

- [ ] **Step 1: Run the full backend test suite**

Run: `dotnet test Server/SurfWeb.slnx`
Expected: PASS

- [ ] **Step 2: Run the full backend build**

Run: `dotnet build Server/SurfWeb.slnx`
Expected: PASS with 0 errors

- [ ] **Step 3: Inspect the final diff**

Run: `git diff -- Server doc/design.md docs/superpowers/plans`
Expected: visible DDD concepts (aggregates, value objects, domain events, commands, read/write split) and matching documentation updates.

- [ ] **Step 4: Create the delivery commit**

```bash
git add Server doc/design.md docs/superpowers/plans
git commit -m "refactor: introduce strong ddd backend structure"
```
