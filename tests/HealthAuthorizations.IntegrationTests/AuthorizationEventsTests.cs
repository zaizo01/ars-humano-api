using System.Net.Http.Json;
using System.Text.Json;
using HealthAuthorizations.Infrastructure.Outbox;
using HealthAuthorizations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HealthAuthorizations.IntegrationTests.TestApi;

namespace HealthAuthorizations.IntegrationTests;

/// <summary>Recorrido completo: API → tabla OutboxMessages → procesador → publicador.</summary>
[TestFixture]
public sealed class AuthorizationEventsTests
{
    private HttpClient _client = null!;

    private static CapturingEventPublisher Events => Factory.Events;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() => _client = await CreateClientAsync();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _client.Dispose();

    private static async Task<List<OutboxMessage>> OutboxMessagesForAsync(Guid authorizationId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.OutboxMessages
            .FromSql($"""SELECT * FROM "OutboxMessages" WHERE "Payload" ->> 'authorizationId' = {authorizationId.ToString()}""")
            .ToListAsync();
    }

    [Test]
    public async Task Should_PublishCreatedEventWithAuthorizationData_When_AuthorizationIsCreated()
    {
        // Arrange
        var request = AuthorizationRequest(amount: 1000m);

        // Act
        var created = await CreateAuthorizationAsync(_client, request);

        // Assert
        var message = await Events.WaitForAsync(created.Id, "authorization.created");
        Assert.That(message, Is.Not.Null, "El evento no se publicó a tiempo");
        var payload = JsonDocument.Parse(message!.Payload).RootElement;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(payload.GetProperty("number").GetInt64(), Is.EqualTo(created.Number));
            Assert.That(payload.GetProperty("totalCovered").GetDecimal(), Is.EqualTo(700m));
            Assert.That(message.MessageId, Is.Not.EqualTo(Guid.Empty));
        }
    }

    [Test]
    public async Task Should_MarkOutboxMessageAsProcessed_When_EventIsPublished()
    {
        // Arrange
        var created = await CreateAuthorizationAsync(_client, AuthorizationRequest());

        // Act
        await Events.WaitForAsync(created.Id, "authorization.created");

        // Assert
        var stored = (await OutboxMessagesForAsync(created.Id)).Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored.Type, Is.EqualTo("authorization.created"));
            Assert.That(stored.ProcessedAt, Is.Not.Null);
            Assert.That(stored.Attempts, Is.Zero);
        }
    }

    [Test]
    public async Task Should_PublishUpdatedEvent_When_AuthorizationIsUpdated()
    {
        // Arrange
        var created = await CreateAuthorizationAsync(_client, AuthorizationRequest());
        var current = await _client.GetAsync($"{AuthorizationsUrl}/{created.Id}");
        using var request = new HttpRequestMessage(HttpMethod.Put, $"{AuthorizationsUrl}/{created.Id}")
        {
            Content = JsonContent.Create(AuthorizationRequest(amount: 2000m)),
        };
        request.Headers.TryAddWithoutValidation("If-Match", current.Headers.ETag!.Tag);

        // Act
        (await _client.SendAsync(request)).EnsureSuccessStatusCode();

        // Assert
        var message = await Events.WaitForAsync(created.Id, "authorization.updated");
        Assert.That(JsonDocument.Parse(message!.Payload).RootElement.GetProperty("totalClaimed").GetDecimal(), Is.EqualTo(2000m));
    }

    [Test]
    public async Task Should_PublishDeactivatedEvent_When_AuthorizationIsDeactivated()
    {
        // Arrange
        var created = await CreateAuthorizationAsync(_client, AuthorizationRequest());

        // Act
        (await _client.DeleteAsync($"{AuthorizationsUrl}/{created.Id}")).EnsureSuccessStatusCode();

        // Assert
        var message = await Events.WaitForAsync(created.Id, "authorization.deactivated");
        Assert.That(message, Is.Not.Null, "El evento no se publicó a tiempo");
    }

    [Test]
    public async Task Should_PublishSingleCreatedEvent_When_RequestIsRetriedWithSameIdempotencyKey()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();

        async Task<Guid> SendAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, AuthorizationsUrl) { Content = JsonContent.Create(AuthorizationRequest()) };
            request.Headers.Add("Idempotency-Key", key);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        }

        // Act
        var id = await SendAsync();
        await SendAsync();
        await SendAsync();

        // Assert
        await Events.WaitForAsync(id, "authorization.created");
        Assert.That(await OutboxMessagesForAsync(id), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Should_NotStoreEvent_When_DeactivatingAlreadyInactiveAuthorization()
    {
        // Arrange
        var created = await CreateAuthorizationAsync(_client, AuthorizationRequest());
        await _client.DeleteAsync($"{AuthorizationsUrl}/{created.Id}");

        // Act
        await _client.DeleteAsync($"{AuthorizationsUrl}/{created.Id}");

        // Assert
        var types = (await OutboxMessagesForAsync(created.Id)).Select(m => m.Type);
        Assert.That(types.Count(t => t == "authorization.deactivated"), Is.EqualTo(1));
    }
}
