using FluentValidation;
using HealthAuthorizations.Application.Common;

namespace HealthAuthorizations.Application.Authorizations.Search;

public sealed class SearchAuthorizationsQueryValidator : AbstractValidator<SearchAuthorizationsQuery>
{
    public SearchAuthorizationsQueryValidator()
    {
        RuleFor(x => x.PageSize).ValidPageSize();
        RuleFor(x => x.Cursor).ValidCursor();
        RuleFor(x => x.Number).GreaterThan(0).When(x => x.Number.HasValue);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("La fecha final debe ser mayor o igual que la fecha inicial.");
    }
}
