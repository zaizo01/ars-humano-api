using System.Net;
using System.Net.Http.Json;
using HealthAuthorizations.Application.Authorizations.Search;
using HealthAuthorizations.Application.Common;
using static HealthAuthorizations.IntegrationTests.TestApi;

namespace HealthAuthorizations.IntegrationTests;

// La base es compartida: afiliado 4 / contratante 3 son exclusivos de estas pruebas.
[TestFixture]
public sealed class SearchAuthorizationsTests
{
    private const int AffiliateId = 4;
    private const int ContractorId = 3;

    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _client = await CreateClientAsync();

        // 5 autorizaciones: 3 con el prestador 1 (consulta) y 2 con el prestador 4 (laboratorio), en fechas distintas.
        await CreateAuthorizationAsync(_client, AuthorizationRequest(AffiliateId, providerId: 1, ContractorId, "2026-01-10", serviceTypeId: 1));
        await CreateAuthorizationAsync(_client, AuthorizationRequest(AffiliateId, providerId: 1, ContractorId, "2026-02-10", serviceTypeId: 1));
        await CreateAuthorizationAsync(_client, AuthorizationRequest(AffiliateId, providerId: 1, ContractorId, "2026-03-10", serviceTypeId: 1));
        await CreateAuthorizationAsync(_client, AuthorizationRequest(AffiliateId, providerId: 4, ContractorId, "2026-04-10", serviceTypeId: 4));
        await CreateAuthorizationAsync(_client, AuthorizationRequest(AffiliateId, providerId: 4, ContractorId, "2026-05-10", serviceTypeId: 4));
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _client.Dispose();

    private async Task<CursorPage<AuthorizationSummaryResponse>> SearchAsync(string queryString)
    {
        var page = await _client.GetFromJsonAsync<CursorPage<AuthorizationSummaryResponse>>($"{AuthorizationsUrl}?{queryString}", Json);
        return page!;
    }

    [Test]
    public async Task Should_ReturnOnlyAffiliateAuthorizations_When_FilteringByAffiliate()
    {
        // Arrange
        var query = $"affiliateId={AffiliateId}";

        // Act
        var page = await SearchAsync(query);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.Items, Has.Count.EqualTo(5));
            Assert.That(page.Items, Has.All.Property(nameof(AuthorizationSummaryResponse.AffiliateId)).EqualTo(AffiliateId));
            Assert.That(page.Items[0].AffiliateName, Is.EqualTo("Luis Gómez"));
            Assert.That(page.HasMore, Is.False);
        }
    }

    [Test]
    public async Task Should_ReturnMostRecentFirst_When_Searching()
    {
        // Arrange
        var query = $"affiliateId={AffiliateId}";

        // Act
        var page = await SearchAsync(query);

        // Assert
        Assert.That(page.Items.Select(i => i.Number), Is.Ordered.Descending);
    }

    [Test]
    public async Task Should_TraverseAllPagesWithoutDuplicates_When_FollowingNextCursor()
    {
        // Arrange
        var collected = new List<long>();
        long? cursor = null;
        var pages = 0;

        // Act
        do
        {
            var page = await SearchAsync($"affiliateId={AffiliateId}&pageSize=2" + (cursor is null ? "" : $"&cursor={cursor}"));
            collected.AddRange(page.Items.Select(i => i.Number));
            cursor = page.NextCursor;
            pages++;
        }
        while (cursor is not null);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(pages, Is.EqualTo(3));
            Assert.That(collected, Has.Count.EqualTo(5));
            Assert.That(collected, Is.Unique);
        }
    }

    [Test]
    public async Task Should_CombineFilters_When_FilteringByProviderAndDateRange()
    {
        // Arrange
        var query = $"affiliateId={AffiliateId}&providerId=1&fromDate=2026-02-01&toDate=2026-03-31";

        // Act
        var page = await SearchAsync(query);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.Items, Has.Count.EqualTo(2));
            Assert.That(page.Items, Has.All.Property(nameof(AuthorizationSummaryResponse.ProviderId)).EqualTo(1));
            Assert.That(page.Items.Select(i => i.ServiceDate), Has.All.InRange(new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 31)));
        }
    }

    [Test]
    public async Task Should_ReturnAuthorizationsContainingService_When_FilteringByServiceType()
    {
        // Arrange
        var query = $"affiliateId={AffiliateId}&serviceTypeId=4";

        // Act
        var page = await SearchAsync(query);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.Items, Has.Count.EqualTo(2));
            Assert.That(page.Items, Has.All.Property(nameof(AuthorizationSummaryResponse.ProviderName)).EqualTo("Laboratorio Referencia"));
        }
    }

    [Test]
    public async Task Should_ReturnSingleAuthorization_When_FilteringByNumber()
    {
        // Arrange
        var target = (await SearchAsync($"affiliateId={AffiliateId}&pageSize=1")).Items[0];

        // Act
        var page = await SearchAsync($"number={target.Number}");

        // Assert
        Assert.That(page.Items.Single().Id, Is.EqualTo(target.Id));
    }

    [Test]
    public async Task Should_ReturnEmptyPage_When_NoAuthorizationMatches()
    {
        // Arrange
        var query = $"affiliateId={AffiliateId}&fromDate=2030-01-01";

        // Act
        var page = await SearchAsync(query);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.Items, Is.Empty);
            Assert.That(page.NextCursor, Is.Null);
        }
    }

    [Test]
    public async Task Should_AllowSearch_When_UserIsReadOnly()
    {
        // Arrange
        using var readOnlyClient = await CreateClientAsync("consulta", "Consulta123!");

        // Act
        var response = await readOnlyClient.GetAsync($"{AuthorizationsUrl}?affiliateId={AffiliateId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Should_ReturnBadRequest_When_PageSizeExceedsMaximum()
    {
        // Arrange
        var query = $"pageSize={SearchAuthorizationsQuery.MaxPageSize + 1}";

        // Act
        var response = await _client.GetAsync($"{AuthorizationsUrl}?{query}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
