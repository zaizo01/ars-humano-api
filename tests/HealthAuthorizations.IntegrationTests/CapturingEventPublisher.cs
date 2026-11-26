using System.Collections.Concurrent;
using System.Text.Json;
using HealthAuthorizations.Infrastructure.Messaging;

namespace HealthAuthorizations.IntegrationTests;

/// <summary>Reemplaza al broker en las pruebas de la API: guarda lo que el outbox publica.</summary>
public sealed class CapturingEventPublisher : IEventPublisher
{
    private readonly ConcurrentQueue<IntegrationMessage> _messages = new();

    public Task PublishAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<IntegrationMessage> For(Guid authorizationId) =>
        _messages.Where(m => JsonDocument.Parse(m.Payload).RootElement.GetProperty("authorizationId").GetGuid() == authorizationId).ToList();

    /// <summary>Espera (sin sleeps fijos) hasta que el outbox publique el evento o se agote el tiempo.</summary>
    public async Task<IntegrationMessage?> WaitForAsync(Guid authorizationId, string type, TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
        while (!cts.IsCancellationRequested)
        {
            var match = For(authorizationId).FirstOrDefault(m => m.Type == type);
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(100, CancellationToken.None);
        }

        return null;
    }
}
