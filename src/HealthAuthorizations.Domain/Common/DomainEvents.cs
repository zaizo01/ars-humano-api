namespace HealthAuthorizations.Domain.Common;

public interface IDomainEvent;

// AppDbContext los pasa al outbox en el mismo SaveChanges.
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
