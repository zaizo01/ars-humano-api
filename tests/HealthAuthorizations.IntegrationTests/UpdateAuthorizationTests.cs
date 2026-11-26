using System.Net;
using System.Net.Http.Json;
using HealthAuthorizations.Application.Authorizations;
using static HealthAuthorizations.IntegrationTests.TestApi;

namespace HealthAuthorizations.IntegrationTests;

/// <summary>Usa el afiliado 2, que no participa en las demás pruebas.</summary>
[TestFixture]
public sealed class UpdateAuthorizationTests
{
    private const int AffiliateId = 2;

    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() => _client = await CreateClientAsync();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _client.Dispose();

    private static object UpdateRequest(decimal firstAmount, decimal secondAmount) => new
    {
        affiliateId = AffiliateId,
        providerId = 4,
        contractorId = 1,
        intermediaryId = (int?)null,
        serviceDate = "2026-11-20",
        notes = "Ajuste de servicios",
        services = new[]
        {
            new { serviceTypeId = 4, description = "Hemograma", claimedAmount = firstAmount },
            new { serviceTypeId = 5, description = "Radiografía", claimedAmount = secondAmount },
        },
    };

    private async Task<(AuthorizationResponse Authorization, string ETag)> CreateAndGetETagAsync()
    {
        var created = await CreateAuthorizationAsync(_client, AuthorizationRequest(AffiliateId));
        var response = await _client.GetAsync($"{AuthorizationsUrl}/{created.Id}");
        return (created, response.Headers.ETag!.Tag);
    }

    private Task<HttpResponseMessage> PutAsync(Guid id, object body, string? ifMatch, HttpClient? client = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"{AuthorizationsUrl}/{id}") { Content = JsonContent.Create(body) };
        if (ifMatch is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        return (client ?? _client).SendAsync(request);
    }

    [Test]
    public async Task Should_ReturnETagHeader_When_GettingAuthorizationById()
    {
        // Arrange
        var created = await CreateAuthorizationAsync(_client, AuthorizationRequest(AffiliateId));

        // Act
        var response = await _client.GetAsync($"{AuthorizationsUrl}/{created.Id}");

        // Assert
        Assert.That(response.Headers.ETag?.Tag, Is.EqualTo($"\"{created.Version}\""));
    }

    [Test]
    public async Task Should_ReplaceServicesAndRecalculateAmounts_When_IfMatchIsCurrent()
    {
        // Arrange
        var (created, etag) = await CreateAndGetETagAsync();

        // Act
        var response = await PutAsync(created.Id, UpdateRequest(500m, 1500m), etag);

        // Assert
        var updated = await response.Content.ReadFromJsonAsync<AuthorizationResponse>(Json);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(updated!.Number, Is.EqualTo(created.Number));
            Assert.That(updated.ProviderId, Is.EqualTo(4));
            Assert.That(updated.Notes, Is.EqualTo("Ajuste de servicios"));
            Assert.That(updated.Details, Has.Count.EqualTo(2));
            Assert.That(updated.TotalClaimed, Is.EqualTo(2000m));
            Assert.That(updated.TotalCovered, Is.EqualTo(1400m));
            Assert.That(updated.TotalCopay, Is.EqualTo(600m));
            Assert.That(response.Headers.ETag!.Tag, Is.Not.EqualTo(etag));
        }
    }

    [Test]
    public async Task Should_PersistChanges_When_UpdateSucceeds()
    {
        // Arrange
        var (created, etag) = await CreateAndGetETagAsync();
        await PutAsync(created.Id, UpdateRequest(100m, 200m), etag);

        // Act
        var fetched = await _client.GetFromJsonAsync<AuthorizationResponse>($"{AuthorizationsUrl}/{created.Id}", Json);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fetched!.Details.Select(d => d.ServiceTypeId), Is.EquivalentTo(new[] { 4, 5 }));
            Assert.That(fetched.TotalClaimed, Is.EqualTo(300m));
        }
    }

    [Test]
    public async Task Should_AcceptWeakETag_When_IfMatchHasWeakPrefix()
    {
        // Arrange
        var (created, etag) = await CreateAndGetETagAsync();

        // Act
        var response = await PutAsync(created.Id, UpdateRequest(100m, 100m), $"W/{etag}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Should_ReturnPreconditionFailed_When_IfMatchIsStale()
    {
        // Arrange
        var (created, staleETag) = await CreateAndGetETagAsync();
        await PutAsync(created.Id, UpdateRequest(100m, 100m), staleETag);

        // Act
        var response = await PutAsync(created.Id, UpdateRequest(999m, 999m), staleETag);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.PreconditionFailed));
    }

    [Test]
    public async Task Should_AcceptOnlyOneUpdate_When_SameETagIsUsedConcurrently()
    {
        // Arrange
        var (created, etag) = await CreateAndGetETagAsync();

        // Act
        var responses = await Task.WhenAll(
            Enumerable.Range(1, 5).Select(i => PutAsync(created.Id, UpdateRequest(100m * i, 100m), etag)));

        // Assert
        var statuses = responses.Select(r => r.StatusCode).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(statuses.Count(s => s == HttpStatusCode.OK), Is.EqualTo(1));
            Assert.That(statuses.Where(s => s != HttpStatusCode.OK),
                Has.All.AnyOf(HttpStatusCode.PreconditionFailed, HttpStatusCode.Conflict));
        }
    }

    [Test]
    public async Task Should_ReturnPreconditionRequired_When_IfMatchIsMissing()
    {
        // Arrange
        var (created, _) = await CreateAndGetETagAsync();

        // Act
        var response = await PutAsync(created.Id, UpdateRequest(100m, 100m), ifMatch: null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)428));
    }

    [Test]
    public async Task Should_ReturnBadRequest_When_IfMatchIsNotAValidETag()
    {
        // Arrange
        var (created, _) = await CreateAndGetETagAsync();

        // Act
        var response = await PutAsync(created.Id, UpdateRequest(100m, 100m), "\"not-a-version\"");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Should_ReturnBadRequest_When_UpdateHasNoServices()
    {
        // Arrange
        var (created, etag) = await CreateAndGetETagAsync();
        var invalid = new { affiliateId = AffiliateId, providerId = 4, contractorId = 1, serviceDate = "2026-11-20", services = Array.Empty<object>() };

        // Act
        var response = await PutAsync(created.Id, invalid, etag);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Should_ReturnNotFound_When_UpdatingUnknownAuthorization()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await PutAsync(unknownId, UpdateRequest(100m, 100m), "\"1\"");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Should_ReturnForbidden_When_ReadOnlyUserUpdates()
    {
        // Arrange
        var (created, etag) = await CreateAndGetETagAsync();
        using var readOnlyClient = await CreateClientAsync("consulta", "Consulta123!");

        // Act
        var response = await PutAsync(created.Id, UpdateRequest(100m, 100m), etag, readOnlyClient);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
}
