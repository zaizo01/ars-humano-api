using FluentAssertions;
using HealthAuthorizations.Domain.Authorizations;
using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.UnitTests.Domain;

public sealed class AuthorizationTests
{
    private static readonly AuthorizationParties Parties = new(AffiliateId: 1, ProviderId: 1, ContractorId: 1, IntermediaryId: null);
    private static readonly DateOnly Today = new(2026, 11, 24);
    private readonly IAmountDistributionPolicy _policy = new PercentageDistributionPolicy(70m);

    [Fact]
    public void Create_calculates_detail_and_totals()
    {
        var authorization = Authorization.Create(Parties, Today, null,
            [new ServiceLine(1, "Consulta general", 1000m), new ServiceLine(4, "Hemograma", 500m)], _policy);

        authorization.Status.Should().Be(AuthorizationStatus.Active);
        authorization.Details.Should().HaveCount(2);
        authorization.TotalClaimed.Should().Be(1500m);
        authorization.TotalCovered.Should().Be(1050m);
        authorization.TotalCopay.Should().Be(450m);
        authorization.Details.Should().OnlyContain(d => d.AuthorizationId == authorization.Id);
    }

    [Fact]
    public void Create_requires_at_least_one_service()
    {
        var act = () => Authorization.Create(Parties, Today, null, [], _policy);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("authorization.no_services");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_rejects_non_positive_amounts(decimal amount)
    {
        var act = () => Authorization.Create(Parties, Today, null, [new ServiceLine(1, null, amount)], _policy);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("amount.not_positive");
    }

    [Fact]
    public void Update_replaces_services_and_recalculates_totals()
    {
        var authorization = Authorization.Create(Parties, Today, null, [new ServiceLine(1, null, 1000m)], _policy);

        authorization.Update(Parties, Today, "Ajuste", [new ServiceLine(2, null, 200m)], _policy);

        authorization.Details.Should().ContainSingle().Which.ServiceTypeId.Should().Be(2);
        authorization.TotalClaimed.Should().Be(200m);
        authorization.Notes.Should().Be("Ajuste");
    }

    [Fact]
    public void Deactivated_authorization_cannot_be_modified_or_deactivated_again()
    {
        var authorization = Authorization.Create(Parties, Today, null, [new ServiceLine(1, null, 1000m)], _policy);
        authorization.Deactivate(DateTimeOffset.UtcNow);

        authorization.Status.Should().Be(AuthorizationStatus.Inactive);
        authorization.Invoking(a => a.Update(Parties, Today, null, [new ServiceLine(1, null, 1m)], _policy))
            .Should().Throw<DomainException>().Which.Code.Should().Be("authorization.inactive");
        authorization.Invoking(a => a.Deactivate(DateTimeOffset.UtcNow))
            .Should().Throw<DomainException>();
    }
}
