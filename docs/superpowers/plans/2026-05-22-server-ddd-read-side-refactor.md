# Server DDD Read-Side Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** This plan has been superseded by the strong DDD teaching-oriented refactor. Keep it only as the historical intermediate step from page-shaped repositories toward query-side contracts.

**Architecture:** Historical intermediate state only: query-side contracts in `Application`, read implementations in `Infrastructure`, before the later strong DDD split introduced aggregates, value objects, command use cases, and read/write separation.

**Tech Stack:** .NET 8, ASP.NET Core, EF Core, Pomelo MySQL, xUnit

---

### Task 1: Add backend test project and cover current application rules

**Files:**
- Create: `Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj`
- Create: `Server/SurfWeb.Application.Tests/Queries/PlayerQueryServiceTests.cs`
- Create: `Server/SurfWeb.Application.Tests/Queries/RecordQueryServiceTests.cs`
- Modify: `Server/SurfWeb.slnx`

- [ ] **Step 1: Add the failing tests**

```csharp
[Fact]
public async Task GetPlayerCompletionsAsync_uses_best_time_per_map_and_orders_by_latest_completion()
{
    // Arrange repositories with duplicate map rows and distinct dates.
    // Act.
    // Assert returned map order and chosen rows.
}

[Fact]
public async Task GetRecentAsync_returns_default_style_rows_only_and_deduplicates_by_player_and_map()
{
    // Arrange mixed rows and verify only the newest row per (auth, map) survives.
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj`
Expected: FAIL because the project and test scaffolding do not exist yet.

- [ ] **Step 3: Create the test project and minimal test doubles**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\SurfWeb.Application\SurfWeb.Application.csproj" />
    <ProjectReference Include="..\SurfWeb.Domain\SurfWeb.Domain.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Run tests to verify the new tests now fail for the expected behavior gap**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj`
Expected: FAIL on assertions until the refactor is implemented.

- [ ] **Step 5: Commit**

```bash
git add Server/SurfWeb.Application.Tests Server/SurfWeb.slnx
git commit -m "test: add application query regression coverage"
```

### Task 2: Move read query contracts from Domain to Application

**Files:**
- Create: `Server/SurfWeb.Application/Queries/Abstractions/IMapReadRepository.cs`
- Create: `Server/SurfWeb.Application/Queries/Abstractions/IPlayerReadRepository.cs`
- Create: `Server/SurfWeb.Application/Queries/Abstractions/IUserReadRepository.cs`
- Modify: `Server/SurfWeb.Application/Queries/MapQueryService.cs`
- Modify: `Server/SurfWeb.Application/Queries/PlayerQueryService.cs`
- Modify: `Server/SurfWeb.Application/Queries/RankingQueryService.cs`
- Modify: `Server/SurfWeb.Application/Queries/RecordQueryService.cs`
- Delete: `Server/SurfWeb.Domain/Repositories/IMapRepository.cs`
- Delete: `Server/SurfWeb.Domain/Repositories/IPlayerTimeRepository.cs`
- Delete: `Server/SurfWeb.Domain/Repositories/IUserRepository.cs`

- [ ] **Step 1: Write the failing test expectation for the architectural move**

```csharp
[Fact]
public void Application_services_depend_on_application_read_contracts_not_domain_repositories()
{
    var ctor = typeof(PlayerQueryService).GetConstructors().Single();
    var parameterTypes = ctor.GetParameters().Select(p => p.ParameterType).ToArray();

    Assert.Contains(typeof(IPlayerReadRepository), parameterTypes);
    Assert.DoesNotContain(parameterTypes, t => t.Namespace == "SurfWeb.Domain.Repositories");
}
```

- [ ] **Step 2: Run tests to verify it fails**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj --filter Application_services_depend_on_application_read_contracts_not_domain_repositories`
Expected: FAIL because current constructors still depend on `SurfWeb.Domain.Repositories`.

- [ ] **Step 3: Create application-side read contracts and switch query services**

```csharp
namespace SurfWeb.Application.Queries.Abstractions;

public interface IUserReadRepository
{
    Task<User?> FindByAuthAsync(int auth, CancellationToken ct = default);
    Task<int> CountAllAsync(CancellationToken ct = default);
    // ...
}
```

- [ ] **Step 4: Remove the old domain repository interfaces**

```text
Delete:
- Server/SurfWeb.Domain/Repositories/IMapRepository.cs
- Server/SurfWeb.Domain/Repositories/IPlayerTimeRepository.cs
- Server/SurfWeb.Domain/Repositories/IUserRepository.cs
```

- [ ] **Step 5: Run tests to verify the new dependency boundary passes**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj`
Expected: PASS for constructor-boundary assertions, with remaining failures only where behavior still needs updates.

- [ ] **Step 6: Commit**

```bash
git add Server/SurfWeb.Application Server/SurfWeb.Domain
git commit -m "refactor: move read repository contracts to application"
```

### Task 3: Rewire infrastructure to implement application read contracts

**Files:**
- Modify: `Server/SurfWeb.Infrastructure/DependencyInjection.cs`
- Modify: `Server/SurfWeb.Infrastructure/Repositories/MapRepository.cs`
- Modify: `Server/SurfWeb.Infrastructure/Repositories/PlayerTimeRepository.cs`
- Modify: `Server/SurfWeb.Infrastructure/Repositories/UserRepository.cs`

- [ ] **Step 1: Write the failing integration-oriented assertion**

```csharp
[Fact]
public void Infrastructure_registers_application_read_contracts()
{
    // Build a service collection, call AddSurfWebInfrastructure, and assert the registrations
    // bind application abstractions to infrastructure repositories.
}
```

- [ ] **Step 2: Run tests to verify it fails**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj --filter Infrastructure_registers_application_read_contracts`
Expected: FAIL until DI switches to application contracts.

- [ ] **Step 3: Update repository implementations and dependency injection**

```csharp
services.AddScoped<IMapReadRepository, MapRepository>();
services.AddScoped<IUserReadRepository, UserRepository>();
services.AddScoped<IPlayerReadRepository, PlayerTimeRepository>();
```

- [ ] **Step 4: Run tests to verify registration and behavior pass**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj`
Expected: PASS for DI and service behavior tests.

- [ ] **Step 5: Commit**

```bash
git add Server/SurfWeb.Infrastructure
git commit -m "refactor: wire infrastructure to application read contracts"
```

### Task 4: Tighten API semantics around default-style-only endpoints

**Files:**
- Modify: `Server/SurfWeb.Application/Abstractions/IQueryServices.cs`
- Modify: `Server/SurfWeb.Application/Queries/PlayerQueryService.cs`
- Modify: `Server/SurfWeb.Application/Queries/RecordQueryService.cs`
- Modify: `Server/SurfWeb.Api/Controllers/V1/PlayersController.cs`
- Modify: `Server/SurfWeb.Api/Controllers/V1/RecordsController.cs`

- [ ] **Step 1: Add failing tests for semantic cleanup**

```csharp
[Fact]
public async Task GetPlayerTimesAsync_uses_default_style_contract_without_style_parameter() { }

[Fact]
public async Task GetRecentAsync_uses_default_style_contract_without_style_parameter() { }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj --filter GetPlayerTimesAsync_uses_default_style_contract_without_style_parameter|GetRecentAsync_uses_default_style_contract_without_style_parameter`
Expected: FAIL because current contracts still accept ignored `style` parameters.

- [ ] **Step 3: Remove ignored `style` parameters from default-style-only query methods and controllers**

```csharp
Task<(IReadOnlyList<PlayerTimeDto> Items, int Total)> GetPlayerTimesAsync(
    int auth, string? map, int page, int pageSize, CancellationToken ct = default);

Task<(IReadOnlyList<RecentRecordDto> Items, int Total)> GetRecentAsync(
    int page, int pageSize, CancellationToken ct = default);
```

- [ ] **Step 4: Run tests to verify contracts and behavior pass**

Run: `dotnet test Server/SurfWeb.Application.Tests/SurfWeb.Application.Tests.csproj`
Expected: PASS with no references to ignored `style` parameters remaining.

- [ ] **Step 5: Commit**

```bash
git add Server/SurfWeb.Api Server/SurfWeb.Application
git commit -m "refactor: clarify default-style query semantics"
```

### Task 5: Update design documentation and implementation plan notes

**Files:**
- Modify: `doc/design.md`
- Modify: `docs/superpowers/plans/2026-05-22-surfweb.md`

- [ ] **Step 1: Document the architecture shift**

```markdown
- Backend remains four projects, but query-oriented read contracts now live in `Application`.
- `Domain` is narrowed to stable concepts and rules rather than page-shaped repository APIs.
- `/players/{auth}/times` and `/records/recent` are documented as default-style-only endpoints.
```

- [ ] **Step 2: Run a docs sanity check**

Run: `rg -n "IMapRepository|IPlayerTimeRepository|IUserRepository|style.*忽略" doc/design.md docs/superpowers/plans/2026-05-22-surfweb.md`
Expected: no stale architecture text remains.

- [ ] **Step 3: Commit**

```bash
git add doc/design.md docs/superpowers/plans/2026-05-22-surfweb.md
git commit -m "docs: align backend architecture documentation"
```

### Task 6: Final verification and delivery commit

**Files:**
- Modify: `Server/SurfWeb.slnx`
- Modify: all touched files from Tasks 1-5

- [ ] **Step 1: Run backend tests**

Run: `dotnet test Server/SurfWeb.slnx`
Expected: PASS

- [ ] **Step 2: Run backend build**

Run: `dotnet build Server/SurfWeb.slnx`
Expected: PASS

- [ ] **Step 3: Review the final diff**

Run: `git diff -- Server doc/design.md docs/superpowers/plans/2026-05-22-surfweb.md`
Expected: only the planned refactor, tests, and docs updates appear.

- [ ] **Step 4: Create the delivery commit**

```bash
git add Server doc/design.md docs/superpowers/plans/2026-05-22-surfweb.md
git commit -m "refactor: align backend read-side architecture"
```
