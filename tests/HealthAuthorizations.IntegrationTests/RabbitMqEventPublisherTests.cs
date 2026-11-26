using System.Text;
using HealthAuthorizations.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace HealthAuthorizations.IntegrationTests;

/// <summary>Publica contra un RabbitMQ real en contenedor y lo consume como lo haría otro sistema.</summary>
[TestFixture]
public sealed class RabbitMqEventPublisherTests
{
    private const string Exchange = "health-authorizations.events";

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:4-alpine").Build();
    private RabbitMqEventPublisher _publisher = null!;
    private IConnection _consumerConnection = null!;
    private IChannel _consumerChannel = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await _rabbit.StartAsync();
        _publisher = new RabbitMqEventPublisher(Options.Create(new RabbitMqOptions
        {
            Enabled = true,
            ConnectionString = _rabbit.GetConnectionString(),
            Exchange = Exchange,
        }));

        var factory = new ConnectionFactory { Uri = new Uri(_rabbit.GetConnectionString()) };
        _consumerConnection = await factory.CreateConnectionAsync();
        _consumerChannel = await _consumerConnection.CreateChannelAsync();
        await _consumerChannel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _consumerChannel.DisposeAsync();
        await _consumerConnection.DisposeAsync();
        await _publisher.DisposeAsync();
        await _rabbit.DisposeAsync();
    }

    private async Task<string> BindQueueAsync(string routingPattern)
    {
        var queue = await _consumerChannel.QueueDeclareAsync(exclusive: true);
        await _consumerChannel.QueueBindAsync(queue.QueueName, Exchange, routingPattern);
        return queue.QueueName;
    }

    private async Task<BasicGetResult?> ReceiveAsync(string queue)
    {
        for (var i = 0; i < 50; i++)
        {
            var result = await _consumerChannel.BasicGetAsync(queue, autoAck: true);
            if (result is not null)
            {
                return result;
            }

            await Task.Delay(100);
        }

        return null;
    }

    [Test]
    public async Task Should_DeliverPersistentMessageWithMetadata_When_EventIsPublished()
    {
        // Arrange
        var queue = await BindQueueAsync("authorization.*");
        var message = new IntegrationMessage(Guid.NewGuid(), "authorization.created", """{"authorizationId":"abc"}""", DateTimeOffset.UtcNow);

        // Act
        await _publisher.PublishAsync(message, CancellationToken.None);

        // Assert
        var received = await ReceiveAsync(queue);
        Assert.That(received, Is.Not.Null, "El mensaje no llegó a la cola");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Encoding.UTF8.GetString(received!.Body.Span), Is.EqualTo(message.Payload));
            Assert.That(received.RoutingKey, Is.EqualTo("authorization.created"));
            Assert.That(received.BasicProperties.MessageId, Is.EqualTo(message.MessageId.ToString()));
            Assert.That(received.BasicProperties.Type, Is.EqualTo("authorization.created"));
            Assert.That(received.BasicProperties.DeliveryMode, Is.EqualTo(DeliveryModes.Persistent));
        }
    }

    [Test]
    public async Task Should_RouteOnlyMatchingEvents_When_ConsumerBindsToSpecificType()
    {
        // Arrange
        var queue = await BindQueueAsync("authorization.deactivated");

        // Act
        await _publisher.PublishAsync(new IntegrationMessage(Guid.NewGuid(), "authorization.created", "{}", DateTimeOffset.UtcNow), CancellationToken.None);
        await _publisher.PublishAsync(new IntegrationMessage(Guid.NewGuid(), "authorization.deactivated", "{}", DateTimeOffset.UtcNow), CancellationToken.None);

        // Assert
        var first = await ReceiveAsync(queue);
        var second = await _consumerChannel.BasicGetAsync(queue, autoAck: true);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(first?.RoutingKey, Is.EqualTo("authorization.deactivated"));
            Assert.That(second, Is.Null);
        }
    }
}
