using HealthAuthorizations.Application.Common;
using HealthAuthorizations.Domain.Authorizations;
using MediatR;

namespace HealthAuthorizations.Application.Authorizations.Search;

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

    public long? Cursor { get; init; }

    public int PageSize { get; init; } = DefaultPageSize;
}

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
