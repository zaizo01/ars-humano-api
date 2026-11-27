using HealthAuthorizations.Application.Abstractions;
using HealthAuthorizations.Application.Common;
using HealthAuthorizations.Domain.Authorizations;
using HealthAuthorizations.Domain.Catalogs;
using HealthAuthorizations.Domain.Common;
using HealthAuthorizations.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HealthAuthorizations.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : DbContext(options), IApplicationDbContext
{
    public DbSet<Authorization> Authorizations => Set<Authorization>();
    public DbSet<Affiliate> Affiliates => Set<Affiliate>();
    public DbSet<Intermediary> Intermediaries => Set<Intermediary>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Contractor> Contractors => Set<Contractor>();
    public DbSet<ProviderType> ProviderTypes => Set<ProviderType>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>(Configurations.AuthorizationConfiguration.NumberSequence).StartsAt(1);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        ConvertDomainEventsToOutboxMessages();

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException("El registro fue modificado por otro usuario. Recargue e intente de nuevo.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("Ya existe un registro con los mismos datos únicos.", ex);
        }
    }

    // Outbox: los eventos se guardan en la misma transacción que el cambio.
    private void ConvertDomainEventsToOutboxMessages()
    {
        var now = timeProvider.GetUtcNow();
        var aggregates = ChangeTracker.Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            OutboxMessages.AddRange(aggregate.DomainEvents.Select(e => OutboxMessage.From(e, now)));
            aggregate.ClearDomainEvents();
        }
    }

    private void ApplyAuditInfo()
    {
        var now = timeProvider.GetUtcNow();
        var user = currentUser.UserName ?? "system";

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = user;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = user;
                    break;
            }
        }
    }
}
