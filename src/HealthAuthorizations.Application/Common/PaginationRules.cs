using FluentValidation;

namespace HealthAuthorizations.Application.Common;

public static class PaginationRules
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static IRuleBuilderOptions<T, int> ValidPageSize<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, MaxPageSize);

    // GreaterThan ignora null, así que el cursor sigue siendo opcional.
    public static IRuleBuilderOptions<T, long?> ValidCursor<T>(this IRuleBuilder<T, long?> rule) =>
        rule.GreaterThan(0);
}
