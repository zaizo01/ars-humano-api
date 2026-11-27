using HealthAuthorizations.Domain.Authorizations;
using HealthAuthorizations.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Abstractions;

// DbSet directo (sin repositorio genérico) para proyectar y paginar en SQL.
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
