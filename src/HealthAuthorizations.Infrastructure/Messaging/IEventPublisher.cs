namespace HealthAuthorizations.Infrastructure.Messaging;

/// <summary>Mensaje listo para salir hacia otros sistemas.</summary>
/// <param name="MessageId">Identificador único: permite a los consumidores descartar duplicados.</param>
/// <param name="Type">Nombre del evento; se usa como routing key.</param>
public sealed record IntegrationMessage(Guid MessageId, string Type, string Payload, DateTimeOffset OccurredAt);

/// <summary>
/// Canal de salida de eventos. Agregar una integración nueva (otro broker, un webhook) es
/// implementar esta interfaz sin tocar el dominio ni los casos de uso.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(IntegrationMessage message, CancellationToken cancellationToken);
}
