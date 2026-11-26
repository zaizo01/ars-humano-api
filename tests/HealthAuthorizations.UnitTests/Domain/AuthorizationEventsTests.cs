using HealthAuthorizations.Domain.Authorizations;

namespace HealthAuthorizations.UnitTests.Domain;

[TestFixture]
public sealed class AuthorizationEventsTests
{
    private static readonly AuthorizationParties Parties = new(AffiliateId: 1, ProviderId: 3, ContractorId: 1, IntermediaryId: 2);
    private static readonly DateOnly ServiceDate = new(2026, 11, 24);
    private static readonly IAmountDistributionPolicy Policy = new PercentageDistributionPolicy(70m);

    private static Authorization CreateAuthorization() =>
        Authorization.Create(42, Parties, ServiceDate, null, [new ServiceLine(1, null, 1000m)], Policy);

    [Test]
    public void Should_RaiseCreatedEventWithNumberAndAmounts_When_AuthorizationIsCreated()
    {
        // Arrange
        // (datos de la autorización definidos en la clase)

        // Act
        var authorization = CreateAuthorization();

        // Assert
        var created = authorization.DomainEvents.OfType<AuthorizationCreated>().Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(created.AuthorizationId, Is.EqualTo(authorization.Id));
            Assert.That(created.Number, Is.EqualTo(42));
            Assert.That(created.IntermediaryId, Is.EqualTo(2));
            Assert.That(created.TotalCovered, Is.EqualTo(700m));
            Assert.That(created.TotalCopay, Is.EqualTo(300m));
        }
    }

    [Test]
    public void Should_RaiseUpdatedEventWithNewTotals_When_AuthorizationIsUpdated()
    {
        // Arrange
        var authorization = CreateAuthorization();
        authorization.ClearDomainEvents();

        // Act
        authorization.Update(Parties, ServiceDate, null, [new ServiceLine(1, null, 200m)], Policy);

        // Assert
        var updated = authorization.DomainEvents.OfType<AuthorizationUpdated>().Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(authorization.DomainEvents, Has.Count.EqualTo(1));
            Assert.That(updated.TotalClaimed, Is.EqualTo(200m));
            Assert.That(updated.TotalCovered, Is.EqualTo(140m));
        }
    }

    [Test]
    public void Should_RaiseDeactivatedEvent_When_AuthorizationIsDeactivated()
    {
        // Arrange
        var authorization = CreateAuthorization();
        authorization.ClearDomainEvents();
        var deactivatedAt = new DateTimeOffset(2026, 11, 26, 9, 30, 0, TimeSpan.Zero);

        // Act
        authorization.Deactivate(deactivatedAt);

        // Assert
        var deactivated = authorization.DomainEvents.OfType<AuthorizationDeactivated>().Single();
        Assert.That(deactivated, Is.EqualTo(new AuthorizationDeactivated(authorization.Id, 42, deactivatedAt)));
    }

    [Test]
    public void Should_NotRaiseEvent_When_OperationViolatesARule()
    {
        // Arrange
        var authorization = CreateAuthorization();
        authorization.Deactivate(DateTimeOffset.UtcNow);
        authorization.ClearDomainEvents();

        // Act
        Assert.Throws<HealthAuthorizations.Domain.Common.DomainException>(() => authorization.Deactivate(DateTimeOffset.UtcNow));

        // Assert
        Assert.That(authorization.DomainEvents, Is.Empty);
    }

    [Test]
    public void Should_RemoveAllEvents_When_EventsAreCleared()
    {
        // Arrange
        var authorization = CreateAuthorization();

        // Act
        authorization.ClearDomainEvents();

        // Assert
        Assert.That(authorization.DomainEvents, Is.Empty);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Should_ThrowArgumentOutOfRange_When_NumberIsNotPositive(long number)
    {
        // Arrange
        ServiceLine[] services = [new(1, null, 100m)];

        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Authorization.Create(number, Parties, ServiceDate, null, services, Policy));

        // Assert
        Assert.That(exception.ParamName, Is.EqualTo("number"));
    }
}
