using HealthAuthorizations.Application.Abstractions;
using HealthAuthorizations.Application.Common;
using HealthAuthorizations.Domain.Authorizations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Authorizations.Deactivate;

/// <summary>Inactivación lógica: el registro y su detalle se conservan para fines históricos.</summary>
public sealed record DeactivateAuthorizationCommand(Guid Id) : IRequest;

public sealed class DeactivateAuthorizationCommandHandler(IApplicationDbContext db, TimeProvider timeProvider)
    : IRequestHandler<DeactivateAuthorizationCommand>
{
    public async Task Handle(DeactivateAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var authorization = await db.Authorizations
            .AsTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Autorización", request.Id);

        // Idempotente: repetir la solicitud (p. ej. un reintento por timeout) no es un error
        // y no altera la fecha de inactivación original.
        if (authorization.Status == AuthorizationStatus.Inactive)
        {
            return;
        }

        authorization.Deactivate(timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
