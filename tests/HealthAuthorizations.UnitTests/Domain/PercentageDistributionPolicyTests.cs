using FluentAssertions;
using HealthAuthorizations.Domain.Authorizations;
using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.UnitTests.Domain;

public sealed class PercentageDistributionPolicyTests
{
    private readonly PercentageDistributionPolicy _policy = new(70m);

    [Theory]
    [InlineData(1000.00, 700.00, 300.00)]
    [InlineData(0.01, 0.01, 0.00)]
    [InlineData(100.05, 70.04, 30.01)] // 70.035 redondea hacia arriba; el copago absorbe la diferencia
    [InlineData(0, 0, 0)]
    public void Distribute_splits_70_30_and_always_adds_up(decimal claimed, decimal expectedCovered, decimal expectedCopay)
    {
        var result = _policy.Distribute(claimed);

        result.CoveredAmount.Should().Be(expectedCovered);
        result.CopayAmount.Should().Be(expectedCopay);
        (result.CoveredAmount + result.CopayAmount).Should().Be(claimed);
    }

    [Fact]
    public void Distribute_rejects_negative_amounts()
    {
        var act = () => _policy.Distribute(-1m);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("amount.negative");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Constructor_rejects_percentages_out_of_range(decimal percentage)
    {
        var act = () => new PercentageDistributionPolicy(percentage);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Coverage_percentage_is_configurable()
    {
        var result = new PercentageDistributionPolicy(80m).Distribute(500m);

        result.Should().Be(new AmountDistribution(400m, 100m));
    }
}
