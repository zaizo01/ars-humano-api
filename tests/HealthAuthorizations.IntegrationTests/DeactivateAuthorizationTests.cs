using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HealthAuthorizations.Application.Authorizations;
using HealthAuthorizations.Application.Authorizations.Search;
using HealthAuthorizations.Application.Common;
using HealthAuthorizations.Domain.Authorizations;
using static HealthAuthorizations.IntegrationTests.TestApi;

namespace HealthAuthorizations.IntegrationTests;

// Afiliado 3 / contratante 2: no los usa ninguna otra prueba.
[TestFixture]
public sealed class DeactivateAuthorizationTests
{
    private const int AffiliateId = 3;
    private const int ContractorId = 2;

    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() => _client = await CreateClientAsync();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _client.Dispose();

    private Task<AuthorizationResponse> CreateAsync() =>
        CreateAuthorizationAsync(_client, AuthorizationRequest(AffiliateId, contractorId: ContractorId));

    private Task<HttpResponseMessage> DeactivateAsync(Guid id, HttpClient? client = null) =>
        (client ?? _client).DeleteAsync($"{AuthorizationsUrl}/{id}");

    [Test]
    public async Task Should_ReturnNoContent_When_DeactivatingActiveAuthorization()
    {
        // Arrange
        var created = await CreateAsync();

        // Act
        var response = await DeactivateAsync(created.Id);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task Should_KeepRecordWithDetailsAsInactive_When_Deactivated()
    {
        // Arrange
        var created = await CreateAsync();
        await DeactivateAsync(created.Id);

        // Act
        var fetched = await _client.GetFromJsonAsync<AuthorizationResponse>($"{AuthorizationsUrl}/{created.Id}", Json);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fetched!.Status, Is.EqualTo(AuthorizationStatus.Inactive));
            Assert.That(fetched.Details, Has.Count.EqualTo(created.Details.Count));
            Assert.That(fetched.TotalClaimed, Is.EqualTo(created.TotalClaimed));
        }
    }

    [Test]
    public async Task Should_ReturnNoContent_When_DeactivatingAlreadyInactiveAuthorization()
    {
        // Arrange
        var created = await CreateAsync();
        await DeactivateAsync(created.Id);

        // Act
        var response = await DeactivateAsync(created.Id);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task Should_ReturnUnprocessableEntity_When_UpdatingInactiveAuthorization()
    {
        // Arrange
        var created = await CreateAsync();
        await DeactivateAsync(created.Id);
        var current = await _client.GetAsync($"{AuthorizationsUrl}/{created.Id}");
        using var request = new HttpRequestMessage(HttpMethod.Put, $"{AuthorizationsUrl}/{created.Id}")
        {
            Content = JsonContent.Create(AuthorizationRequest(AffiliateId, contractorId: ContractorId, amount: 50m)),
        };
        request.Headers.TryAddWithoutValidation("If-Match", current.Headers.ETag!.Tag);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
            Assert.That(problem.GetProperty("code").GetString(), Is.EqualTo("authorization.inactive"));
        }
    }

    [Test]
    public async Task Should_AppearInSearch_When_FilteringByInactiveStatus()
    {
        // Arrange
        var created = await CreateAsync();
        await DeactivateAsync(created.Id);

        // Act
        var page = await _client.GetFromJsonAsync<CursorPage<AuthorizationSummaryResponse>>(
            $"{AuthorizationsUrl}?affiliateId={AffiliateId}&status=Inactive&pageSize=100", Json);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(page!.Items.Select(i => i.Id), Does.Contain(created.Id));
            Assert.That(page.Items, Has.All.Property(nameof(AuthorizationSummaryResponse.Status)).EqualTo(AuthorizationStatus.Inactive));
        }
    }

    [Test]
    public async Task Should_ReturnNotFound_When_DeactivatingUnknownAuthorization()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await DeactivateAsync(unknownId);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Should_ReturnForbidden_When_ReadOnlyUserDeactivates()
    {
        // Arrange
        var created = await CreateAsync();
        using var readOnlyClient = await CreateClientAsync("consulta", "Consulta123!");

        // Act
        var response = await DeactivateAsync(created.Id, readOnlyClient);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
}
