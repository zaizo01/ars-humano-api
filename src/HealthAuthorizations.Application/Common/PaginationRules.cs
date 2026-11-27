using FluentValidation;

namespace HealthAuthorizations.Application.Common;

/// <summary>Reglas de paginación por cursor compartidas por todas las búsquedas.</summary>
public static class PaginationRules
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static IRuleBuilderOptions<T, int> ValidPageSize<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, MaxPageSize);

    /// <summary>El cursor es opcional; si viene debe ser positivo (los validadores de comparación ignoran null).</summary>
    public static IRuleBuilderOptions<T, long?> ValidCursor<T>(this IRuleBuilder<T, long?> rule) =>
        rule.GreaterThan(0);
}
