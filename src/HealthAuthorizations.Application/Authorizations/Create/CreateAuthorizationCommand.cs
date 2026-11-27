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
    IReadOnlyList<ServiceLineRequest> Services) : IRequest<AuthorizationResponse>, IAuthorizationData
{
    // Header Idempotency-Key
    [JsonIgnore]
    public string? IdempotencyKey { get; init; }
}
