using HealthAuthorizations.Application.Abstractions;
using HealthAuthorizations.Application.Common;
using HealthAuthorizations.Domain.Authorizations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Authorizations.Deactivate;

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

        // Idempotente: un reintento no es error ni cambia la fecha original.
        if (authorization.Status == AuthorizationStatus.Inactive)
        {
            return;
        }

        authorization.Deactivate(timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
