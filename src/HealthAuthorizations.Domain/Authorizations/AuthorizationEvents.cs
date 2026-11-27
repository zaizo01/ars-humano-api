using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Authorizations;

// Se publican tal cual: cambiar un campo rompe a los consumidores.

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
