using SurfWeb.Application.Abstractions;

namespace SurfWeb.Infrastructure.Persistence;

/// <summary>
/// The backing Shavit database remains read-only for this project.
/// This unit of work exists to keep the command side explicitly modeled
/// without pretending we can persist write-side changes into that source.
/// </summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
