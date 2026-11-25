using HealthAuthorizations.Domain.Authorizations;
using HealthAuthorizations.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Abstractions;

/// <summary>
/// Puerto de persistencia. Se expone DbSet directamente (sin repositorio genérico)
/// para que las consultas CQRS puedan proyectar y paginar en SQL.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Authorization> Authorizations { get; }
    DbSet<Affiliate> Affiliates { get; }
    DbSet<Intermediary> Intermediaries { get; }
    DbSet<Provider> Providers { get; }
    DbSet<Contractor> Contractors { get; }
    DbSet<ProviderType> ProviderTypes { get; }
    DbSet<ServiceType> ServiceTypes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
