using System.Collections.Concurrent;
using System.Text.Json;
using HealthAuthorizations.Infrastructure.Messaging;

namespace HealthAuthorizations.IntegrationTests;

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
