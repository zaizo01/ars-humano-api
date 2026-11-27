using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Authorizations;

/// <summary>
/// Reparto de un monto reclamado entre la ARS (cubierto) y el afiliado (copago).
/// </summary>
public readonly record struct AmountDistribution(decimal CoveredAmount, decimal CopayAmount);

/// <summary>
/// Regla de distribución de montos. Se abstrae para poder cambiarla
/// (por plan, tipo de servicio, topes, etc.) sin tocar el agregado.
/// </summary>
public interface IAmountDistributionPolicy
{
    AmountDistribution Distribute(decimal claimedAmount);
}

/// <summary>
/// Distribución porcentual: la ARS cubre <c>coveragePercentage</c> % y el afiliado el resto.
/// El copago se calcula por diferencia para que la suma siempre cuadre al centavo.
/// </summary>
public sealed class PercentageDistributionPolicy : IAmountDistributionPolicy
{
    private readonly decimal _coverageRate;

    public PercentageDistributionPolicy(decimal coveragePercentage)
    {
        if (coveragePercentage is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(coveragePercentage), "El porcentaje de cobertura debe estar entre 0 y 100.");
        }

        _coverageRate = coveragePercentage / 100m;
    }

    public AmountDistribution Distribute(decimal claimedAmount)
    {
        if (claimedAmount < 0m)
        {
            throw new DomainException("amount.negative", "El monto reclamado no puede ser negativo.");
        }

        var covered = Math.Round(claimedAmount * _coverageRate, 2, MidpointRounding.AwayFromZero);
        return new AmountDistribution(covered, claimedAmount - covered);
    }
}
