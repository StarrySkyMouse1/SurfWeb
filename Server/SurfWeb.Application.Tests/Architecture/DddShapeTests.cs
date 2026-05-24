using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SurfWeb.Api.Controllers.V1;
using SurfWeb.Application.Abstractions;
using SurfWeb.Application.Commands.RecordRun;
using SurfWeb.Application.Queries.Abstractions;
using SurfWeb.Infrastructure;
using SurfWeb.Domain.Aggregates.Maps;
using SurfWeb.Domain.Aggregates.Players;
using SurfWeb.Domain.Aggregates.Runs;
using SurfWeb.Domain.Common;
using SurfWeb.Domain.DomainServices;
using SurfWeb.Domain.Events;
using SurfWeb.Domain.Repositories;
using SurfWeb.Domain.ValueObjects;
using Xunit;

namespace SurfWeb.Application.Tests.Architecture;

public sealed class DddShapeTests
{
    [Fact]
    public void Required_value_objects_exist_in_the_domain_core()
    {
        Assert.True(typeof(PlayerId).IsValueType);
        Assert.True(typeof(MapName).IsValueType);
        Assert.True(typeof(StyleId).IsValueType);
        Assert.True(typeof(TrackId).IsValueType);
        Assert.True(typeof(StageId).IsValueType);
        Assert.True(typeof(RunTime).IsValueType);
    }

    [Fact]
    public void Required_aggregates_exist_and_inherit_from_aggregate_root()
    {
        Assert.True(IsAggregateRoot(typeof(Player)));
        Assert.True(IsAggregateRoot(typeof(Map)));
        Assert.True(IsAggregateRoot(typeof(RunRecord)));
    }

    [Fact]
    public void Command_side_types_exist_for_recording_runs()
    {
        Assert.True(typeof(RecordRunCommand).IsClass);
        Assert.True(typeof(IRecordRunUseCase).IsInterface);
        Assert.True(typeof(RecordRunUseCase).IsAssignableTo(typeof(IRecordRunUseCase)));
    }

    [Fact]
    public void Domain_events_exist_for_recorded_runs_and_world_records()
    {
        Assert.True(typeof(RunRecordedDomainEvent).IsAssignableTo(typeof(IDomainEvent)));
        Assert.True(typeof(WorldRecordBrokenDomainEvent).IsAssignableTo(typeof(IDomainEvent)));
    }

    [Fact]
    public void Infrastructure_registers_read_and_write_sides_separately()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Shavit"] = "Server=localhost;Database=shavit;User=readonly;Password=;",
            })
            .Build();

        services.AddSurfWebInfrastructure(configuration);

        var playerRead = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IPlayerReadRepository));
        var mapRead = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IMapReadRepository));
        var userRead = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IUserReadRepository));
        var playerWrite = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IPlayerRepository));
        var mapWrite = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IMapRepository));
        var runWrite = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IRunRecordRepository));
        var worldRecordPolicy = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IWorldRecordPolicy));
        var completionPolicy = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICompletionPolicy));
        var unitOfWork = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IUnitOfWork));

        Assert.Contains(".Repositories.Read", playerRead.ImplementationType?.Namespace);
        Assert.Contains(".Repositories.Read", mapRead.ImplementationType?.Namespace);
        Assert.Contains(".Repositories.Read", userRead.ImplementationType?.Namespace);
        Assert.Contains(".Repositories.Write", playerWrite.ImplementationType?.Namespace);
        Assert.Contains(".Repositories.Write", mapWrite.ImplementationType?.Namespace);
        Assert.Contains(".Repositories.Write", runWrite.ImplementationType?.Namespace);
        Assert.Contains(".Policies", worldRecordPolicy.ImplementationType?.Namespace);
        Assert.Contains(".Policies", completionPolicy.ImplementationType?.Namespace);
        Assert.Contains(".Persistence", unitOfWork.ImplementationType?.Namespace);
    }

    [Fact]
    public void Api_exposes_a_command_controller_that_depends_on_the_command_use_case_only()
    {
        var controllerType = typeof(AdminRunsController);
        var constructorParameterTypes = controllerType
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.Contains(typeof(IRecordRunUseCase), constructorParameterTypes);
        Assert.DoesNotContain(constructorParameterTypes, type => type.Name.EndsWith("QueryService", StringComparison.Ordinal));
    }

    private static bool IsAggregateRoot(Type candidate)
    {
        var current = candidate.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }
}
