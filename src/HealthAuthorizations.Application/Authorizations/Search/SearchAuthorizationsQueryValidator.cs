using FluentValidation;

namespace HealthAuthorizations.Application.Authorizations.Search;

public sealed class SearchAuthorizationsQueryValidator : AbstractValidator<SearchAuthorizationsQuery>
{
    public SearchAuthorizationsQueryValidator()
    {
        RuleFor(x => x.PageSize).InclusiveBetween(1, SearchAuthorizationsQuery.MaxPageSize);
        RuleFor(x => x.Cursor).GreaterThan(0).When(x => x.Cursor.HasValue);
        RuleFor(x => x.Number).GreaterThan(0).When(x => x.Number.HasValue);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("La fecha final debe ser mayor o igual que la fecha inicial.");
    }
}
