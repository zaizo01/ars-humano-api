using System.Text.Json.Serialization;
using FluentValidation;
using HealthAuthorizations.Application.Abstractions;
using MediatR;

namespace HealthAuthorizations.Application.Authorizations.Update;

/// <summary>Reemplaza los datos y servicios de una autorización activa (semántica PUT).</summary>
public sealed record UpdateAuthorizationCommand(
    int AffiliateId,
    int ProviderId,
    int ContractorId,
    int? IntermediaryId,
    DateOnly ServiceDate,
    string? Notes,
    IReadOnlyList<ServiceLineRequest> Services) : IRequest<AuthorizationResponse>, IAuthorizationData
{
    /// <summary>Viene de la ruta.</summary>
    [JsonIgnore]
    public Guid Id { get; init; }

    /// <summary>Versión que el cliente leyó; viene del header <c>If-Match</c>.</summary>
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
