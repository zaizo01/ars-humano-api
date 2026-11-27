using HealthAuthorizations.Application.Abstractions;
using HealthAuthorizations.Application.Common;
using HealthAuthorizations.Domain.Authorizations;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Authorizations.Update;

// Dos controles: If-Match distinto a la versión actual → 412.
// Cambio de otro proceso entre la lectura y el guardado (xmin) → 409.
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
