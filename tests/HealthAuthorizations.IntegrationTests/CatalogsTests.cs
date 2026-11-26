using System.Net;
using System.Net.Http.Json;
using HealthAuthorizations.Application.Catalogs;
using HealthAuthorizations.Application.Common;
using static HealthAuthorizations.IntegrationTests.TestApi;

namespace HealthAuthorizations.IntegrationTests;

/// <summary>Verifica los catálogos contra los datos iniciales de la migración.</summary>
[TestFixture]
public sealed class CatalogsTests
{
    private const string Url = "/api/v1/catalogs";

    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() => _client = await CreateClientAsync("consulta", "Consulta123!");

    [OneTimeTearDown]
    public void OneTimeTearDown() => _client.Dispose();

    private async Task<T> GetAsync<T>(string path) => (await _client.GetFromJsonAsync<T>($"{Url}/{path}", Json))!;

    [Test]
    public async Task Should_ReturnAllProviderTypes_When_GettingProviderTypes()
    {
        // Arrange
        var expected = new[] { "Clínica", "Farmacia", "Laboratorio", "Médico" };

        // Act
        var items = await GetAsync<List<CatalogItemResponse>>("provider-types");

        // Assert
        Assert.That(items.Select(i => i.Name), Is.EqualTo(expected));
    }

    [Test]
    public async Task Should_ReturnFiveServiceTypes_When_GettingServiceTypes()
    {
        // Arrange
        var expectedCodes = new[] { "CON", "EME", "HOS", "LAB", "IMG" };

        // Act
        var items = await GetAsync<List<CatalogItemResponse>>("service-types");

        // Assert
        Assert.That(items.Select(i => i.Code), Is.EquivalentTo(expectedCodes));
    }

    [Test]
    public async Task Should_ReturnSameData_When_CatalogIsServedFromCache()
    {
        // Arrange
        var first = await GetAsync<List<CatalogItemResponse>>("intermediaries");

        // Act
        var second = await GetAsync<List<CatalogItemResponse>>("intermediaries");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Has.Count.EqualTo(2));
            Assert.That(second, Is.EqualTo(first));
        }
    }

    [Test]
    public async Task Should_ReturnContractorsWithIntermediary_When_GettingContractors()
    {
        // Arrange
        const string contractorWithIntermediary = "CTR-001";

        // Act
        var items = await GetAsync<List<ContractorResponse>>("contractors");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(items, Has.Count.EqualTo(3));
            Assert.That(items.Single(c => c.Code == contractorWithIntermediary).IntermediaryId, Is.EqualTo(1));
        }
    }

    [TestCase("AF-0003", "Ana Martínez")]
    [TestCase("00100000003", "Ana Martínez")]
    [TestCase("martínez", "Ana Martínez")]
    public async Task Should_FindAffiliate_When_SearchingByNumberDocumentOrName(string search, string expectedName)
    {
        // Arrange
        var query = $"affiliates?search={Uri.EscapeDataString(search)}";

        // Act
        var page = await GetAsync<CursorPage<AffiliateResponse>>(query);

        // Assert
        Assert.That(page.Items.Single().FullName, Is.EqualTo(expectedName));
    }

    [Test]
    public async Task Should_ReturnOnlyInactiveAffiliates_When_FilteringByIsActiveFalse()
    {
        // Arrange
        const string query = "affiliates?isActive=false";

        // Act
        var page = await GetAsync<CursorPage<AffiliateResponse>>(query);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.Items, Has.Count.EqualTo(1));
            Assert.That(page.Items[0].AffiliateNumber, Is.EqualTo("AF-0005"));
        }
    }

    [Test]
    public async Task Should_TraverseAllAffiliates_When_FollowingNextCursor()
    {
        // Arrange
        var ids = new List<int>();
        long? cursor = null;

        // Act
        do
        {
            var page = await GetAsync<CursorPage<AffiliateResponse>>("affiliates?pageSize=2" + (cursor is null ? "" : $"&cursor={cursor}"));
            ids.AddRange(page.Items.Select(a => a.Id));
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        // Assert
        Assert.That(ids, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public async Task Should_ReturnProvidersOfType_When_FilteringByProviderType()
    {
        // Arrange
        const int clinicTypeId = 3;

        // Act
        var page = await GetAsync<CursorPage<ProviderResponse>>($"providers?providerTypeId={clinicTypeId}");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.Items, Has.Count.EqualTo(2));
            Assert.That(page.Items, Has.All.Property(nameof(ProviderResponse.ProviderTypeName)).EqualTo("Clínica"));
        }
    }

    [Test]
    public async Task Should_FindProvider_When_SearchingByPartialName()
    {
        // Arrange
        const string query = "providers?search=farmacia";

        // Act
        var page = await GetAsync<CursorPage<ProviderResponse>>(query);

        // Assert
        Assert.That(page.Items.Single().Code, Is.EqualTo("PRE-002"));
    }

    [Test]
    public async Task Should_ReturnBadRequest_When_CatalogPageSizeIsInvalid()
    {
        // Arrange
        const string query = "providers?pageSize=0";

        // Act
        var response = await _client.GetAsync($"{Url}/{query}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Should_ReturnUnauthorized_When_CatalogRequestHasNoToken()
    {
        // Arrange
        using var anonymous = Factory.CreateClient();

        // Act
        var response = await anonymous.GetAsync($"{Url}/service-types");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
