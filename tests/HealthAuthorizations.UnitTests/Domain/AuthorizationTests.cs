using HealthAuthorizations.Domain.Authorizations;
using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.UnitTests.Domain;

[TestFixture]
public sealed class AuthorizationTests
{
    private static readonly AuthorizationParties Parties = new(AffiliateId: 1, ProviderId: 1, ContractorId: 1, IntermediaryId: null);
    private static readonly DateOnly ServiceDate = new(2026, 11, 24);
    private static readonly IAmountDistributionPolicy Policy = new PercentageDistributionPolicy(70m);

    private static Authorization CreateAuthorization(params ServiceLine[] services) =>
        Authorization.Create(Parties, ServiceDate, notes: null, services, Policy);

    [Test]
    public void Should_CalculateDetailAndTotals_When_CreatedWithValidServices()
    {
        // Arrange
        ServiceLine[] services = [new(1, "Consulta general", 1000m), new(4, "Hemograma", 500m)];

        // Act
        var authorization = CreateAuthorization(services);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(authorization.Status, Is.EqualTo(AuthorizationStatus.Active));
            Assert.That(authorization.Details, Has.Count.EqualTo(2));
            Assert.That(authorization.TotalClaimed, Is.EqualTo(1500m));
            Assert.That(authorization.TotalCovered, Is.EqualTo(1050m));
            Assert.That(authorization.TotalCopay, Is.EqualTo(450m));
            Assert.That(authorization.Details, Has.All.Property(nameof(AuthorizationDetail.AuthorizationId)).EqualTo(authorization.Id));
        }
    }

    [Test]
    public void Should_ThrowDomainException_When_CreatedWithoutServices()
    {
        // Arrange
        ServiceLine[] noServices = [];

        // Act
        var exception = Assert.Throws<DomainException>(() => CreateAuthorization(noServices));

        // Assert
        Assert.That(exception.Code, Is.EqualTo("authorization.no_services"));
    }

    [TestCase(0)]
    [TestCase(-10)]
    public void Should_ThrowDomainException_When_ServiceAmountIsNotPositive(decimal amount)
    {
        // Arrange
        var service = new ServiceLine(1, null, amount);

        // Act
        var exception = Assert.Throws<DomainException>(() => CreateAuthorization(service));

        // Assert
        Assert.That(exception.Code, Is.EqualTo("amount.not_positive"));
    }

    [Test]
    public void Should_ReplaceServicesAndRecalculateTotals_When_Updated()
    {
        // Arrange
        var authorization = CreateAuthorization(new ServiceLine(1, null, 1000m));

        // Act
        authorization.Update(Parties, ServiceDate, "Ajuste", [new ServiceLine(2, null, 200m)], Policy);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(authorization.Details, Has.Count.EqualTo(1));
            Assert.That(authorization.Details.Single().ServiceTypeId, Is.EqualTo(2));
            Assert.That(authorization.TotalClaimed, Is.EqualTo(200m));
            Assert.That(authorization.Notes, Is.EqualTo("Ajuste"));
        }
    }

    [Test]
    public void Should_MarkAsInactiveAndKeepRecord_When_Deactivated()
    {
        // Arrange
        var authorization = CreateAuthorization(new ServiceLine(1, null, 1000m));
        var deactivatedAt = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);

        // Act
        authorization.Deactivate(deactivatedAt);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(authorization.Status, Is.EqualTo(AuthorizationStatus.Inactive));
            Assert.That(authorization.DeactivatedAt, Is.EqualTo(deactivatedAt));
            Assert.That(authorization.Details, Is.Not.Empty);
        }
    }

    [Test]
    public void Should_ThrowDomainException_When_UpdatingInactiveAuthorization()
    {
        // Arrange
        var authorization = CreateAuthorization(new ServiceLine(1, null, 1000m));
        authorization.Deactivate(DateTimeOffset.UtcNow);

        // Act
        var exception = Assert.Throws<DomainException>(() =>
            authorization.Update(Parties, ServiceDate, null, [new ServiceLine(1, null, 1m)], Policy));

        // Assert
        Assert.That(exception.Code, Is.EqualTo("authorization.inactive"));
    }

    [Test]
    public void Should_ThrowDomainException_When_DeactivatingAlreadyInactiveAuthorization()
    {
        // Arrange
        var authorization = CreateAuthorization(new ServiceLine(1, null, 1000m));
        authorization.Deactivate(DateTimeOffset.UtcNow);

        // Act
        var exception = Assert.Throws<DomainException>(() => authorization.Deactivate(DateTimeOffset.UtcNow));

        // Assert
        Assert.That(exception.Code, Is.EqualTo("authorization.inactive"));
    }
}
