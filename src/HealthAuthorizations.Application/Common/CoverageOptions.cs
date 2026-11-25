namespace HealthAuthorizations.Application.Common;

/// <summary>
/// Parámetros de la regla de distribución de montos. Cambiar el porcentaje
/// solo requiere cambiar configuración (appsettings / variables de entorno).
/// </summary>
public sealed class CoverageOptions
{
    public const string SectionName = "Coverage";

    public decimal CoveragePercentage { get; init; } = 70m;
}
