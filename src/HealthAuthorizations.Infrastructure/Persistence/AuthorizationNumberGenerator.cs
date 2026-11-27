using HealthAuthorizations.Application.Abstractions;
using HealthAuthorizations.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Infrastructure.Persistence;

// nextval no participa de la transacción: nunca repite, pero deja huecos si algo falla. Aceptable.
internal sealed class AuthorizationNumberGenerator(AppDbContext db) : IAuthorizationNumberGenerator
{
    public Task<long> NextAsync(CancellationToken cancellationToken) =>
        db.Database
            .SqlQueryRaw<long>($"SELECT nextval('\"{AuthorizationConfiguration.NumberSequence}\"') AS \"Value\"")
            .SingleAsync(cancellationToken);
}
