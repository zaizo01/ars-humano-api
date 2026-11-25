using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HealthAuthorizations.Application.Authorizations;
using static HealthAuthorizations.IntegrationTests.TestApi;

namespace HealthAuthorizations.IntegrationTests;

[TestFixture]
public sealed class AuthorizationsEndpointsTests
{
    private const string Url = AuthorizationsUrl;

    private static object ValidRequest(decimal amount = 1000m) => AuthorizationRequest(amount: amount);
    [Test]
    public async Task Should_ReturnUnauthorized_When_RequestHasNoToken()
    {
        // Arrange
        using var client = Factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(Url, ValidRequest());

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Should_ReturnForbidden_When_ReadOnlyUserCreatesAuthorization()
    {
        // Arrange
        using var client = await CreateClientAsync("consulta", "Consulta123!");

        // Act
        var response = await client.PostAsJsonAsync(Url, ValidRequest());

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Should_CreateWithDistributedAmounts_When_RequestIsValid()
    {
        // Arrange
        using var client = await CreateClientAsync();

        // Act
        var response = await client.PostAsJsonAsync(Url, ValidRequest(1000m));

        // Assert
        var body = await response.Content.ReadFromJsonAsync<AuthorizationResponse>(Json);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers.Location, Is.Not.Null);
            Assert.That(body!.TotalCovered, Is.EqualTo(700m));
            Assert.That(body.TotalCopay, Is.EqualTo(300m));
            Assert.That(body.Number, Is.Positive);
        }
    }

    [Test]
    public async Task Should_ReturnAuthorizationWithDetails_When_GettingExistingId()
    {
        // Arrange
        using var client = await CreateClientAsync();
        var created = await client.PostAsJsonAsync(Url, ValidRequest(1000m));
        var createdBody = await created.Content.ReadFromJsonAsync<AuthorizationResponse>(Json);

        // Act
        var fetched = await client.GetFromJsonAsync<AuthorizationResponse>($"{Url}/{createdBody!.Id}", Json);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fetched!.Id, Is.EqualTo(createdBody.Id));
            Assert.That(fetched.Details, Has.Count.EqualTo(1));
            Assert.That(fetched.Details[0].CoveredAmount, Is.EqualTo(700m));
        }
    }

    [Test]
    public async Task Should_CreateOnlyOneAuthorization_When_SameIdempotencyKeyIsSentConcurrently()
    {
        // Arrange
        using var client = await CreateClientAsync();
        var key = Guid.NewGuid().ToString();

        async Task<AuthorizationResponse> SendAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Url) { Content = JsonContent.Create(ValidRequest()) };
            request.Headers.Add("Idempotency-Key", key);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AuthorizationResponse>(Json))!;
        }

        // Act
        var results = await Task.WhenAll(SendAsync(), SendAsync(), SendAsync());

        // Assert
        Assert.That(results.Select(r => r.Id).Distinct(), Has.Exactly(1).Items);
    }

    [Test]
    public async Task Should_ReturnValidationProblem_When_RequestIsInvalid()
    {
        // Arrange
        using var client = await CreateClientAsync();
        var request = new { affiliateId = 999, providerId = 3, contractorId = 1, serviceDate = "2026-11-24", services = Array.Empty<object>() };

        // Act
        var response = await client.PostAsJsonAsync(Url, request);

        // Assert
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var invalidFields = problem.GetProperty("errors").EnumerateObject().Select(p => p.Name).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(invalidFields, Does.Contain("AffiliateId").And.Contain("Services"));
        }
    }

    [Test]
    public async Task Should_ReturnNotFound_When_AuthorizationDoesNotExist()
    {
        // Arrange
        using var client = await CreateClientAsync();
        var unknownId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"{Url}/{unknownId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
