using HealthAuthorizations.Application.Common;
using HealthAuthorizations.Domain.Authorizations;
using MediatR;

namespace HealthAuthorizations.Application.Authorizations.Search;

/// <summary>Búsqueda de autorizaciones. Todos los filtros son opcionales y se combinan con AND.</summary>
public sealed record SearchAuthorizationsQuery : IRequest<CursorPage<AuthorizationSummaryResponse>>
{
    public const int DefaultPageSize = PaginationRules.DefaultPageSize;
    public const int MaxPageSize = PaginationRules.MaxPageSize;

    public long? Number { get; init; }
    public int? AffiliateId { get; init; }
    public int? ProviderId { get; init; }
    public int? ContractorId { get; init; }
    public int? IntermediaryId { get; init; }
    public int? ServiceTypeId { get; init; }
    public AuthorizationStatus? Status { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }

    /// <summary>Valor <c>nextCursor</c> de la página anterior.</summary>
    public long? Cursor { get; init; }

    public int PageSize { get; init; } = DefaultPageSize;
}

/// <summary>Vista resumida para listados: sin el detalle de servicios, para que la respuesta sea liviana.</summary>
public sealed record AuthorizationSummaryResponse
{
    public Guid Id { get; init; }
    public long Number { get; init; }
    public int AffiliateId { get; init; }
    public required string AffiliateName { get; init; }
    public int ProviderId { get; init; }
    public required string ProviderName { get; init; }
    public int ContractorId { get; init; }
    public DateOnly ServiceDate { get; init; }
    public AuthorizationStatus Status { get; init; }
    public int ServicesCount { get; init; }
    public decimal TotalClaimed { get; init; }
    public decimal TotalCovered { get; init; }
    public decimal TotalCopay { get; init; }
}
