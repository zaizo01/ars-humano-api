using HealthAuthorizations.Application.Abstractions;
using HealthAuthorizations.Application.Common;
using HealthAuthorizations.Domain.Authorizations;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Authorizations.Update;

/// <summary>
/// Concurrencia en dos niveles:
/// 1. If-Match: si la versión que el cliente leyó ya cambió → 412 (no se pisa el cambio de otro usuario).
/// 2. xmin: si otro proceso modifica la fila entre la lectura y el guardado → 409.
/// </summary>
public sealed class UpdateAuthorizationCommandHandler(
    IApplicationDbContext db,
    IAmountDistributionPolicy distributionPolicy)
    : IRequestHandler<UpdateAuthorizationCommand, AuthorizationResponse>
{
    public async Task<AuthorizationResponse> Handle(UpdateAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var authorization = await db.Authorizations
            .AsTracking()
            .Include(a => a.Details)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Autorización", request.Id);

        if (authorization.Version != request.ExpectedVersion)
        {
            throw new PreconditionFailedException(
                "La autorización fue modificada después de consultarla. Obtenga la versión actual e intente de nuevo.");
        }

        authorization.Update(
            request.ToParties(),
            request.ServiceDate,
            request.Notes,
            request.ToServiceLines(),
            distributionPolicy);

        await db.SaveChangesAsync(cancellationToken);

        return authorization.Adapt<AuthorizationResponse>();
    }
}
