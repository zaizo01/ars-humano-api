using System.Text.Json.Serialization;
using MediatR;

namespace HealthAuthorizations.Application.Authorizations.Create;

public sealed record CreateAuthorizationCommand(
    int AffiliateId,
    int ProviderId,
    int ContractorId,
    int? IntermediaryId,
    DateOnly ServiceDate,
    string? Notes,
    IReadOnlyList<ServiceLineRequest> Services) : IRequest<AuthorizationResponse>
{
    /// <summary>Viene del header <c>Idempotency-Key</c>, no del body.</summary>
    [JsonIgnore]
    public string? IdempotencyKey { get; init; }
}

public sealed record ServiceLineRequest(int ServiceTypeId, string? Description, decimal ClaimedAmount);
