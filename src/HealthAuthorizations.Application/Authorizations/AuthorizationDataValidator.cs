using FluentValidation;
using HealthAuthorizations.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Authorizations;

/// <summary>Datos editables de una autorización, comunes a crear y actualizar.</summary>
public interface IAuthorizationData
{
    int AffiliateId { get; }
    int ProviderId { get; }
    int ContractorId { get; }
    int? IntermediaryId { get; }
    DateOnly ServiceDate { get; }
    string? Notes { get; }
    IReadOnlyList<ServiceLineRequest> Services { get; }
}

public sealed record ServiceLineRequest(int ServiceTypeId, string? Description, decimal ClaimedAmount);

/// <summary>
/// Reglas compartidas por crear y actualizar: formato de los datos y existencia de las
/// entidades referenciadas (no se registran autorizaciones con entidades inexistentes o inactivas).
/// </summary>
public abstract class AuthorizationDataValidator<T> : AbstractValidator<T>
    where T : IAuthorizationData
{
    public const int MaxServices = 50;

    protected AuthorizationDataValidator(IApplicationDbContext db)
    {
        RuleFor(x => x.ServiceDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);

        RuleFor(x => x.Services)
            .NotEmpty().WithMessage("La autorización debe contener al menos un servicio.")
            .Must(s => s is null || s.Count <= MaxServices).WithMessage($"Máximo {MaxServices} servicios por autorización.");

        RuleForEach(x => x.Services).ChildRules(service =>
        {
            service.RuleFor(s => s.ClaimedAmount)
                .GreaterThan(0m)
                .PrecisionScale(18, 2, ignoreTrailingZeros: true);
            service.RuleFor(s => s.Description).MaximumLength(250);
        });

        RuleFor(x => x.AffiliateId)
            .MustAsync((id, ct) => db.Affiliates.AnyAsync(a => a.Id == id && a.IsActive, ct))
            .WithMessage("El afiliado no existe o está inactivo.");

        RuleFor(x => x.ProviderId)
            .MustAsync((id, ct) => db.Providers.AnyAsync(p => p.Id == id && p.IsActive, ct))
            .WithMessage("El prestador no existe o está inactivo.");

        RuleFor(x => x.ContractorId)
            .MustAsync((id, ct) => db.Contractors.AnyAsync(c => c.Id == id && c.IsActive, ct))
            .WithMessage("El contratante no existe o está inactivo.");

        RuleFor(x => x.IntermediaryId)
            .MustAsync(async (id, ct) => await db.Intermediaries.AnyAsync(i => i.Id == id && i.IsActive, ct))
            .When(x => x.IntermediaryId.HasValue)
            .WithMessage("El intermediario no existe o está inactivo.");

        RuleFor(x => x.Services)
            .MustAsync(async (services, ct) =>
            {
                var ids = services.Select(s => s.ServiceTypeId).Distinct().ToList();
                var found = await db.ServiceTypes.CountAsync(t => ids.Contains(t.Id) && t.IsActive, ct);
                return found == ids.Count;
            })
            .When(x => x.Services is { Count: > 0 })
            .WithMessage("Uno o más tipos de servicio no existen o están inactivos.");
    }
}
