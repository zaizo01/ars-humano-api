namespace HealthAuthorizations.Infrastructure.Messaging;

// MessageId permite a los consumidores descartar duplicados. Type es la routing key.
public sealed record IntegrationMessage(Guid MessageId, string Type, string Payload, DateTimeOffset OccurredAt);

public interface IEventPublisher
{
    Task PublishAsync(IntegrationMessage message, CancellationToken cancellationToken);
}
