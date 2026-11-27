using HealthAuthorizations.Infrastructure.Messaging;
using HealthAuthorizations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthAuthorizations.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(5);
    public int BatchSize { get; init; } = 50;

    // Después de esto el mensaje queda para revisión manual.
    public int MaxAttempts { get; init; } = 10;
}

// SKIP LOCKED: con varias réplicas cada una toma un lote distinto.
// Entrega al menos una vez: si falla el marcado se republica (el consumidor deduplica por MessageId).
internal sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.PollingInterval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // No tumbar el servicio; se reintenta en el siguiente ciclo.
                logger.LogError(ex, "Error procesando el outbox");
            }
        }
    }

    internal async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        // EnableRetryOnFailure exige ejecutar las transacciones explícitas dentro de la estrategia.
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async ct =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var messages = await db.OutboxMessages
                .FromSql($"""
                    SELECT * FROM "OutboxMessages"
                    WHERE "ProcessedAt" IS NULL AND "Attempts" < {_options.MaxAttempts}
                    ORDER BY "OccurredAt"
                    LIMIT {_options.BatchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .AsTracking()
                .ToListAsync(ct);

            foreach (var message in messages)
            {
                try
                {
                    await publisher.PublishAsync(new IntegrationMessage(message.Id, message.Type, message.Payload, message.OccurredAt), ct);
                    message.MarkProcessed(timeProvider.GetUtcNow());
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    message.MarkFailed(ex.Message);
                    logger.LogWarning(ex, "No se pudo publicar el evento {EventType} ({MessageId}), intento {Attempt}",
                        message.Type, message.Id, message.Attempts);
                }
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return messages.Count;
        }, cancellationToken);
    }
}
