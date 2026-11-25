using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using HealthAuthorizations.Application.Authorizations;

namespace HealthAuthorizations.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AuthorizationsEndpointsTests(ApiFactory factory)
{
    private const string Url = "/api/v1/authorizations";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private static object ValidRequest(decimal amount = 1000m) => new
    {
        affiliateId = 1,
        providerId = 3,
        contractorId = 1,
        intermediaryId = 1,
        serviceDate = "2026-11-24",
        services = new[] { new { serviceTypeId = 1, description = "Consulta", claimedAmount = amount } },
    };

    private async Task<HttpClient> CreateClientAsync(string user = "operador", string password = "Operador123!")
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/token", new { userName = user, password });
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());
        return client;
    }

    [Fact]
    public async Task Requests_without_token_are_rejected()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(Url, ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Read_only_user_cannot_create()
    {
        var client = await CreateClientAsync("consulta", "Consulta123!");

        var response = await client.PostAsJsonAsync(Url, ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_then_get_returns_distributed_amounts()
    {
        var client = await CreateClientAsync();

        var created = await client.PostAsJsonAsync(Url, ValidRequest(1000m));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await created.Content.ReadFromJsonAsync<AuthorizationResponse>(Json);
        body!.TotalCovered.Should().Be(700m);
        body.TotalCopay.Should().Be(300m);
        body.Number.Should().BePositive();

        var fetched = await client.GetFromJsonAsync<AuthorizationResponse>(created.Headers.Location, Json);
        fetched!.Id.Should().Be(body.Id);
        fetched.Details.Should().ContainSingle().Which.CoveredAmount.Should().Be(700m);
    }

    [Fact]
    public async Task Same_idempotency_key_does_not_duplicate()
    {
        var client = await CreateClientAsync();
        var key = Guid.NewGuid().ToString();

        async Task<AuthorizationResponse> SendAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Url) { Content = JsonContent.Create(ValidRequest()) };
            request.Headers.Add("Idempotency-Key", key);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AuthorizationResponse>(Json))!;
        }

        var results = await Task.WhenAll(SendAsync(), SendAsync(), SendAsync());

        results.Select(r => r.Id).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task Invalid_request_returns_validation_problem()
    {
        var client = await CreateClientAsync();
        var request = new { affiliateId = 999, providerId = 3, contractorId = 1, serviceDate = "2026-11-24", services = Array.Empty<object>() };

        var response = await client.PostAsJsonAsync(Url, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").EnumerateObject().Select(p => p.Name)
            .Should().Contain(["AffiliateId", "Services"]);
    }

    [Fact]
    public async Task Unknown_id_returns_404()
    {
        var client = await CreateClientAsync();

        var response = await client.GetAsync($"{Url}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
