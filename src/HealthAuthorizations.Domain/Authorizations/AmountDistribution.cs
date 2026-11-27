using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Authorizations;

public readonly record struct AmountDistribution(decimal CoveredAmount, decimal CopayAmount);

public interface IAmountDistributionPolicy
{
    AmountDistribution Distribute(decimal claimedAmount);
}

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

        // El copago va por diferencia para que la suma cuadre al centavo.
        var covered = Math.Round(claimedAmount * _coverageRate, 2, MidpointRounding.AwayFromZero);
        return new AmountDistribution(covered, claimedAmount - covered);
    }
}
