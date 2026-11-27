using System.Text.Json;
using HealthAuthorizations.Domain.Authorizations;
using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Infrastructure.Outbox;

/// <summary>
/// Evento pendiente de publicar. Se inserta en la misma transacción que el cambio que lo origina,
/// así nunca se pierde un evento ni se publica uno de un cambio que no se guardó.
/// </summary>
public sealed class OutboxMessage
{
    public const int MaxErrorLength = 2000;

    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Nombre público del evento, usado también como routing key (ej. <c>authorization.created</c>).</summary>
    public string Type { get; private set; } = null!;

    public string Payload { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    public static OutboxMessage From(IDomainEvent domainEvent, DateTimeOffset occurredAt) => new()
    {
        Id = Guid.CreateVersion7(),
        Type = EventNameOf(domainEvent),
        Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions),
        OccurredAt = occurredAt,
    };

    public void MarkProcessed(DateTimeOffset at) => ProcessedAt = at;

    public void MarkFailed(string error)
    {
        Attempts++;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
    }

    public static string EventNameOf(IDomainEvent domainEvent) => domainEvent switch
    {
        AuthorizationCreated => "authorization.created",
        AuthorizationUpdated => "authorization.updated",
        AuthorizationDeactivated => "authorization.deactivated",
        _ => domainEvent.GetType().Name,
    };
}
