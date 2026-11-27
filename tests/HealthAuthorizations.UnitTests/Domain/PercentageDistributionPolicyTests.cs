using HealthAuthorizations.Domain.Authorizations;
using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.UnitTests.Domain;

[TestFixture]
public sealed class PercentageDistributionPolicyTests
{
    private PercentageDistributionPolicy _policy = null!;

    [SetUp]
    public void SetUp() => _policy = new PercentageDistributionPolicy(70m);

    [TestCase(1000.00, 700.00, 300.00)]
    [TestCase(0.01, 0.01, 0.00)]
    [TestCase(100.05, 70.04, 30.01)] // 70.035 redondea hacia arriba; el copago absorbe la diferencia
    [TestCase(0, 0, 0)]
    public void Should_SplitSeventyThirtyAndAddUp_When_ClaimedAmountIsValid(decimal claimed, decimal expectedCovered, decimal expectedCopay)
    {
        // Act
        var result = _policy.Distribute(claimed);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.CoveredAmount, Is.EqualTo(expectedCovered));
            Assert.That(result.CopayAmount, Is.EqualTo(expectedCopay));
            Assert.That(result.CoveredAmount + result.CopayAmount, Is.EqualTo(claimed));
        }
    }

    [Test]
    public void Should_ThrowDomainException_When_ClaimedAmountIsNegative()
    {
        // Arrange
        const decimal negativeAmount = -1m;

        // Act
        var exception = Assert.Throws<DomainException>(() => _policy.Distribute(negativeAmount));

        // Assert
        Assert.That(exception.Code, Is.EqualTo("amount.negative"));
    }

    [TestCase(-0.01)]
    [TestCase(100.01)]
    public void Should_ThrowArgumentOutOfRange_When_PercentageIsOutOfRange(decimal percentage)
    {
        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _ = new PercentageDistributionPolicy(percentage));

        // Assert
        Assert.That(exception.ParamName, Is.EqualTo("coveragePercentage"));
    }

    [Test]
    public void Should_ApplyConfiguredPercentage_When_CoverageIsNotSeventy()
    {
        // Arrange
        var policy = new PercentageDistributionPolicy(80m);

        // Act
        var result = policy.Distribute(500m);

        // Assert
        Assert.That(result, Is.EqualTo(new AmountDistribution(400m, 100m)));
    }
}
