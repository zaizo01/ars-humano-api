using HealthAuthorizations.Domain.Authorizations;

namespace HealthAuthorizations.Application.Authorizations;

public sealed record AuthorizationResponse
{
    public Guid Id { get; init; }
    public long Number { get; init; }
    public int AffiliateId { get; init; }
    public int ProviderId { get; init; }
    public int ContractorId { get; init; }
    public int? IntermediaryId { get; init; }
    public DateOnly ServiceDate { get; init; }
    public string? Notes { get; init; }
    public AuthorizationStatus Status { get; init; }
    public decimal TotalClaimed { get; init; }
    public decimal TotalCovered { get; init; }
    public decimal TotalCopay { get; init; }
    public uint Version { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyList<AuthorizationDetailResponse> Details { get; init; } = [];
}

public sealed record AuthorizationDetailResponse
{
    public long Id { get; init; }
    public int ServiceTypeId { get; init; }
    public string? Description { get; init; }
    public decimal ClaimedAmount { get; init; }
    public decimal CoveredAmount { get; init; }
    public decimal CopayAmount { get; init; }
}
