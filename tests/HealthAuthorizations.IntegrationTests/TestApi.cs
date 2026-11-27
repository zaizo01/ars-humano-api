using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HealthAuthorizations.Application.Authorizations;

namespace HealthAuthorizations.IntegrationTests;

/// <summary>Utilidades compartidas por las pruebas de integración.</summary>
internal static class TestApi
{
    public const string AuthorizationsUrl = "/api/v1/authorizations";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static ApiFactory Factory => IntegrationTestSetup.Factory;

    public static object AuthorizationRequest(
        int affiliateId = 1,
        int providerId = 3,
        int contractorId = 1,
        string serviceDate = "2026-11-24",
        int serviceTypeId = 1,
        decimal amount = 1000m) => new
    {
        affiliateId,
        providerId,
        contractorId,
        intermediaryId = (int?)null,
        serviceDate,
        services = new[] { new { serviceTypeId, description = "Servicio", claimedAmount = amount } },
    };

    public static async Task<HttpClient> CreateClientAsync(string user = "operador", string password = "Operador123!")
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/token", new { userName = user, password });
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());
        return client;
    }

    public static async Task<AuthorizationResponse> CreateAuthorizationAsync(HttpClient client, object request)
    {
        var response = await client.PostAsJsonAsync(AuthorizationsUrl, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthorizationResponse>(Json))!;
    }
}
