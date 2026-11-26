using HealthAuthorizations.Application.Abstractions;
using HealthAuthorizations.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Infrastructure.Persistence;

/// <summary>
/// Toma el siguiente valor de la secuencia de PostgreSQL. nextval es atómico y no participa de
/// transacciones, así que dos solicitudes simultáneas nunca obtienen el mismo número
/// (puede haber huecos si una solicitud falla, lo cual es aceptable para un correlativo).
/// </summary>
internal sealed class AuthorizationNumberGenerator(AppDbContext db) : IAuthorizationNumberGenerator
{
    public Task<long> NextAsync(CancellationToken cancellationToken) =>
        db.Database
            .SqlQueryRaw<long>($"SELECT nextval('\"{AuthorizationConfiguration.NumberSequence}\"') AS \"Value\"")
            .SingleAsync(cancellationToken);
}
