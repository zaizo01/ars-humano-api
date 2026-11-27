using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace HealthAuthorizations.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    /// <summary>false = los eventos solo se registran en el log (desarrollo sin broker).</summary>
    public bool Enabled { get; init; }

    public string ConnectionString { get; init; } = "amqp://guest:guest@localhost:5672";

    /// <summary>Exchange tipo topic: cada consumidor enlaza su cola con los eventos que le interesan (ej. <c>authorization.*</c>).</summary>
    public string Exchange { get; init; } = "health-authorizations.events";
}

/// <summary>
/// Publica en un exchange topic durable, con mensajes persistentes y confirmación del broker
/// (publisher confirms): si RabbitMQ no confirma, se lanza una excepción y el outbox reintenta.
/// </summary>
public sealed class RabbitMqEventPublisher(IOptions<RabbitMqOptions> options) : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        var channel = await GetChannelAsync(cancellationToken);

        var properties = new BasicProperties
        {
            MessageId = message.MessageId.ToString(),
            Type = message.Type,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds()),
        };

        await channel.BasicPublishAsync(
            _options.Exchange,
            routingKey: message.Type,
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(message.Payload),
            cancellationToken: cancellationToken);
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            await DisposeConnectionAsync();

            var factory = new ConnectionFactory { Uri = new Uri(_options.ConnectionString), ClientProvidedName = "health-authorizations-api" };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(
                new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
                cancellationToken);
            await _channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

            return _channel;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task DisposeConnectionAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeConnectionAsync();
        _lock.Dispose();
    }
}
