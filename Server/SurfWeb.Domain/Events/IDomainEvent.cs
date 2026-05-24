namespace SurfWeb.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
