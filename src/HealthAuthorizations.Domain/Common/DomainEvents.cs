namespace HealthAuthorizations.Domain.Common;

/// <summary>Algo relevante que ocurrió en el dominio y que otros sistemas pueden necesitar saber.</summary>
public interface IDomainEvent;

/// <summary>
/// Agregado que acumula eventos mientras se modifica. Al persistir, la infraestructura los
/// convierte en mensajes del outbox dentro de la misma transacción y luego los limpia.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
