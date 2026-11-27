using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Authorizations;

// Contratos de los eventos que se publican hacia otros sistemas. Llevan los datos que un
// consumidor típico necesita (facturación, notificaciones, analítica) sin obligarlo a
// consultar la API; cambiar su forma es un cambio de contrato.

public sealed record AuthorizationCreated(
    Guid AuthorizationId,
    long Number,
    int AffiliateId,
    int ProviderId,
    int ContractorId,
    int? IntermediaryId,
    DateOnly ServiceDate,
    decimal TotalClaimed,
    decimal TotalCovered,
    decimal TotalCopay) : IDomainEvent;

public sealed record AuthorizationUpdated(
    Guid AuthorizationId,
    long Number,
    int ProviderId,
    DateOnly ServiceDate,
    decimal TotalClaimed,
    decimal TotalCovered,
    decimal TotalCopay) : IDomainEvent;

public sealed record AuthorizationDeactivated(
    Guid AuthorizationId,
    long Number,
    DateTimeOffset DeactivatedAt) : IDomainEvent;
