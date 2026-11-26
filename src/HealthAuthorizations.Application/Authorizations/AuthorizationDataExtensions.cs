using HealthAuthorizations.Domain.Authorizations;

namespace HealthAuthorizations.Application.Authorizations;

internal static class AuthorizationDataExtensions
{
    public static AuthorizationParties ToParties(this IAuthorizationData data) =>
        new(data.AffiliateId, data.ProviderId, data.ContractorId, data.IntermediaryId);

    public static IReadOnlyCollection<ServiceLine> ToServiceLines(this IAuthorizationData data) =>
        data.Services.Select(s => new ServiceLine(s.ServiceTypeId, s.Description, s.ClaimedAmount)).ToList();
}
