using System.Text.Json.Serialization;
using FluentValidation;
using HealthAuthorizations.Application.Abstractions;
using MediatR;

namespace HealthAuthorizations.Application.Authorizations.Update;

public sealed record UpdateAuthorizationCommand(
    int AffiliateId,
    int ProviderId,
    int ContractorId,
    int? IntermediaryId,
    DateOnly ServiceDate,
    string? Notes,
    IReadOnlyList<ServiceLineRequest> Services) : IRequest<AuthorizationResponse>, IAuthorizationData
{
    [JsonIgnore]
    public Guid Id { get; init; }

    // Header If-Match
    [JsonIgnore]
    public uint ExpectedVersion { get; init; }
}

public sealed class UpdateAuthorizationCommandValidator : AuthorizationDataValidator<UpdateAuthorizationCommand>
{
    public UpdateAuthorizationCommandValidator(IApplicationDbContext db)
        : base(db)
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
