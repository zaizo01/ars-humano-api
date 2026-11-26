using Microsoft.Extensions.Logging;

namespace HealthAuthorizations.Infrastructure.Messaging;

/// <summary>Publicador para entornos sin broker (desarrollo local): solo registra el evento en el log.</summary>
internal sealed class LoggingEventPublisher(ILogger<LoggingEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Evento {EventType} ({MessageId}) publicado (sin broker): {Payload}",
            message.Type, message.MessageId, message.Payload);
        return Task.CompletedTask;
    }
}
