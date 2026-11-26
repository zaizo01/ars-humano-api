using HealthAuthorizations.Application.Abstractions;
using HealthAuthorizations.Application.Common;
using HealthAuthorizations.Domain.Authorizations;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Authorizations.Create;

public sealed class CreateAuthorizationCommandHandler(
    IApplicationDbContext db,
    IAmountDistributionPolicy distributionPolicy)
    : IRequestHandler<CreateAuthorizationCommand, AuthorizationResponse>
{
    public async Task<AuthorizationResponse> Handle(CreateAuthorizationCommand request, CancellationToken cancellationToken)
    {
        // Idempotencia: un reintento con la misma clave devuelve la autorización ya creada.
        if (request.IdempotencyKey is not null)
        {
            var existing = await FindByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }
        }

        var authorization = Authorization.Create(
            request.ToParties(),
            request.ServiceDate,
            request.Notes,
            request.ToServiceLines(),
            distributionPolicy,
            request.IdempotencyKey);

        db.Authorizations.Add(authorization);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (ConflictException) when (request.IdempotencyKey is not null)
        {
            // Carrera entre dos solicitudes simultáneas con la misma clave: gana la primera.
            return await FindByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken)
                ?? throw new ConflictException("No fue posible registrar la autorización.");
        }

        return authorization.Adapt<AuthorizationResponse>();
    }

    private Task<AuthorizationResponse?> FindByIdempotencyKeyAsync(string key, CancellationToken ct) =>
        db.Authorizations
            .AsNoTracking()
            .Where(a => a.IdempotencyKey == key)
            .ProjectToType<AuthorizationResponse>()
            .FirstOrDefaultAsync(ct);
}
