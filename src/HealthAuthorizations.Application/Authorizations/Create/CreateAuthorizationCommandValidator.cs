using FluentValidation;
using HealthAuthorizations.Application.Abstractions;

namespace HealthAuthorizations.Application.Authorizations.Create;

public sealed class CreateAuthorizationCommandValidator : AuthorizationDataValidator<CreateAuthorizationCommand>
{
    public CreateAuthorizationCommandValidator(IApplicationDbContext db)
        : base(db)
    {
        RuleFor(x => x.IdempotencyKey).MaximumLength(100);
    }
}
