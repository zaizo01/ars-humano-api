using HealthAuthorizations.Application.Abstractions;
using HealthAuthorizations.Application.Common;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Authorizations.GetById;

public sealed record GetAuthorizationByIdQuery(Guid Id) : IRequest<AuthorizationResponse>;

public sealed class GetAuthorizationByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAuthorizationByIdQuery, AuthorizationResponse>
{
    public async Task<AuthorizationResponse> Handle(GetAuthorizationByIdQuery request, CancellationToken cancellationToken) =>
        await db.Authorizations
            .AsNoTracking()
            .Where(a => a.Id == request.Id)
            .ProjectToType<AuthorizationResponse>()
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Autorización", request.Id);
}
