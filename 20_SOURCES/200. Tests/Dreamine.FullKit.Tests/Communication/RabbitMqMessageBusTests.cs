using System.Text.Json;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.RabbitMQ.Buses;
using Dreamine.Communication.RabbitMQ.Infrastructure;
using Dreamine.Communication.RabbitMQ.Options;

namespace Dreamine.FullKit.Tests.Communication;

public sealed class RabbitMqMessageBusTests
{
    [Fact]
    public async Task ConnectAsync_DeclaresConfiguredTopology()
    {
        var fixture = new RabbitMqFixture(new RabbitMqMessageBusOptions
        {
            ExchangeName = "dreamine.exchange",
            QueueName = "dreamine.queue",
            RoutingKey = "dreamine.route",
            ExchangeType = "topic",
            Durable = true,
            AutoDelete = true,
            Exclusive = true
        });

        await fixture.Bus.ConnectAsync();

        Assert.Equal(ConnectionState.Connected, fixture.Bus.State);
        Assert.Equal("exchange:dreamine.exchange:topic:True:True", fixture.Channel.Operations[0]);
        Assert.Equal("queue:dreamine.queue:True:True:True", fixture.Channel.Operations[1]);
        Assert.Equal("bind:dreamine.queue:dreamine.exchange:dreamine.route", fixture.Channel.Operations[2]);
    }

    [Fact]
    public async Task PublishAsync_UsesMessageRouteAndSerializesEnvelope()
    {
        var fixture = new RabbitMqFixture(new RabbitMqMessageBusOptions
        {
            ExchangeName = "dreamine.exchange",
            RoutingKey = "fallback.route",
            PersistentMessages = true
        });

        await fixture.Bus.ConnectAsync();

        var message = new MessageEnvelope
        {
            Name = "Rabbit.Publish",
            Route = "message.route",
            Payload = [1, 2, 3],
            Headers = new Dictionary<string, string> { ["Protocol"] = "RabbitMQ" }
        };

        await fixture.Bus.PublishAsync(message);

        Assert.Single(fixture.Channel.PublishedMessages);

        var published = fixture.Channel.PublishedMessages[0];
        var roundTripped = JsonSerializer.Deserialize<MessageEnvelope>(published.Body.Span);

        Assert.Equal("dreamine.exchange", published.Exchange);
        Assert.Equal("message.route", published.RoutingKey);
        Assert.True(published.Properties.Persistent);
        Assert.Equal("application/json", published.Properties.ContentType);
        Assert.Equal(nameof(MessageEnvelope), published.Properties.Type);
        Assert.NotNull(roundTripped);
        Assert.Equal(message.Name, roundTripped.Name);
        Assert.Equal(message.Route, roundTripped.Route);
        Assert.Equal(message.Payload, roundTripped.Payload);
    }

    [Fact]
    public async Task PublishAsync_UsesConfiguredRoutingKeyWhenMessageRouteIsEmpty()
    {
        var fixture = new RabbitMqFixture(new RabbitMqMessageBusOptions
        {
            RoutingKey = "fallback.route"
        });

        await fixture.Bus.ConnectAsync();

        await fixture.Bus.PublishAsync(new MessageEnvelope { Name = "Rabbit.Publish" });

        Assert.Equal("fallback.route", fixture.Channel.PublishedMessages[0].RoutingKey);
    }

    [Fact]
    public async Task SubscribeAsync_BindsRouteConsumesAndAcknowledgesHandledDelivery()
    {
        var fixture = new RabbitMqFixture(new RabbitMqMessageBusOptions
        {
            ExchangeName = "dreamine.exchange",
            QueueName = "dreamine.queue"
        });

        await fixture.Bus.ConnectAsync();

        MessageEnvelope? handled = null;
        await fixture.Bus.SubscribeAsync(
            "custom.route",
            (message, _) =>
            {
                handled = message;
                return Task.CompletedTask;
            });

        var delivery = new RabbitMqDelivery(
            7,
            "custom.route",
            JsonSerializer.SerializeToUtf8Bytes(new MessageEnvelope
            {
                Name = "Rabbit.Receive",
                Route = "custom.route",
                Payload = [9, 8, 7]
            }));

        await fixture.Channel.DeliverAsync(delivery);

        Assert.NotNull(handled);
        Assert.Equal("Rabbit.Receive", handled.Name);
        Assert.Contains("bind:dreamine.queue:dreamine.exchange:custom.route", fixture.Channel.Operations);
        Assert.Equal("consume:dreamine.queue:False", fixture.Channel.Operations.Last(x => x.StartsWith("consume:", StringComparison.Ordinal)));
        Assert.Equal([7UL], fixture.Channel.AckedDeliveryTags);
        Assert.Empty(fixture.Channel.NackedDeliveryTags);
    }

    [Fact]
    public async Task SubscribeAsync_NacksDeliveryWhenHandlerFails()
    {
        var fixture = new RabbitMqFixture(new RabbitMqMessageBusOptions
        {
            RequeueOnHandlerError = true
        });

        await fixture.Bus.ConnectAsync();
        await fixture.Bus.SubscribeAsync(
            "failing.route",
            (_, _) => throw new InvalidOperationException("handler failed"));

        var delivery = new RabbitMqDelivery(
            11,
            "failing.route",
            JsonSerializer.SerializeToUtf8Bytes(new MessageEnvelope
            {
                Name = "Rabbit.Receive",
                Route = "failing.route"
            }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Channel.DeliverAsync(delivery));

        Assert.Equal([11UL], fixture.Channel.NackedDeliveryTags);
        Assert.True(fixture.Channel.LastNackRequeue);
        Assert.Empty(fixture.Channel.AckedDeliveryTags);
    }

    [Fact]
    public async Task DisconnectAsync_CancelsConsumerAndClosesResources()
    {
        var fixture = new RabbitMqFixture(new RabbitMqMessageBusOptions());

        await fixture.Bus.ConnectAsync();
        await fixture.Bus.SubscribeAsync("route", (_, _) => Task.CompletedTask);
        await fixture.Bus.DisconnectAsync();

        Assert.Equal(ConnectionState.Disconnected, fixture.Bus.State);
        Assert.Contains("consumer-1", fixture.Channel.CancelledConsumerTags);
        Assert.True(fixture.Channel.Closed);
        Assert.True(fixture.Connection.Closed);
    }

    private sealed class RabbitMqFixture
    {
        public RabbitMqFixture(RabbitMqMessageBusOptions options)
        {
            Channel = new FakeRabbitMqChannel();
            Connection = new FakeRabbitMqConnection(Channel);
            Bus = new RabbitMqMessageBus(options, new FakeRabbitMqConnectionFactory(Connection));
        }

        public RabbitMqMessageBus Bus { get; }

        public FakeRabbitMqConnection Connection { get; }

        public FakeRabbitMqChannel Channel { get; }
    }

    private sealed class FakeRabbitMqConnectionFactory : IRabbitMqConnectionFactory
    {
        private readonly FakeRabbitMqConnection _connection;

        public FakeRabbitMqConnectionFactory(FakeRabbitMqConnection connection)
        {
            _connection = connection;
        }

        public IRabbitMqConnection CreateConnection(RabbitMqMessageBusOptions options)
        {
            return _connection;
        }
    }

    private sealed class FakeRabbitMqConnection : IRabbitMqConnection
    {
        private readonly FakeRabbitMqChannel _channel;

        public FakeRabbitMqConnection(FakeRabbitMqChannel channel)
        {
            _channel = channel;
        }

        public bool IsOpen => !Closed;

        public bool Closed { get; private set; }

        public IRabbitMqChannel CreateChannel()
        {
            return _channel;
        }

        public void Close()
        {
            Closed = true;
        }

        public void Dispose()
        {
            Closed = true;
        }
    }

    private sealed class FakeRabbitMqChannel : IRabbitMqChannel
    {
        private Func<RabbitMqDelivery, CancellationToken, Task>? _onReceived;

        public bool IsOpen => !Closed;

        public bool Closed { get; private set; }

        public bool LastNackRequeue { get; private set; }

        public List<string> Operations { get; } = [];

        public List<PublishedMessage> PublishedMessages { get; } = [];

        public List<ulong> AckedDeliveryTags { get; } = [];

        public List<ulong> NackedDeliveryTags { get; } = [];

        public List<ulong> RejectedDeliveryTags { get; } = [];

        public List<string> CancelledConsumerTags { get; } = [];

        public void ExchangeDeclare(
            string exchange,
            string type,
            bool durable,
            bool autoDelete)
        {
            Operations.Add($"exchange:{exchange}:{type}:{durable}:{autoDelete}");
        }

        public void QueueDeclare(
            string queue,
            bool durable,
            bool exclusive,
            bool autoDelete)
        {
            Operations.Add($"queue:{queue}:{durable}:{exclusive}:{autoDelete}");
        }

        public void QueueBind(
            string queue,
            string exchange,
            string routingKey)
        {
            Operations.Add($"bind:{queue}:{exchange}:{routingKey}");
        }

        public IRabbitMqBasicProperties CreateBasicProperties()
        {
            return new FakeRabbitMqBasicProperties();
        }

        public void BasicPublish(
            string exchange,
            string routingKey,
            bool mandatory,
            IRabbitMqBasicProperties properties,
            ReadOnlyMemory<byte> body)
        {
            PublishedMessages.Add(new PublishedMessage(
                exchange,
                routingKey,
                mandatory,
                new FakeRabbitMqBasicProperties
                {
                    Persistent = properties.Persistent,
                    ContentType = properties.ContentType,
                    Type = properties.Type
                },
                body.ToArray()));
        }

        public string BasicConsume(
            string queue,
            bool autoAck,
            Func<RabbitMqDelivery, CancellationToken, Task> onReceived)
        {
            Operations.Add($"consume:{queue}:{autoAck}");
            _onReceived = onReceived;
            return "consumer-1";
        }

        public Task DeliverAsync(RabbitMqDelivery delivery)
        {
            return _onReceived?.Invoke(delivery, CancellationToken.None)
                ?? throw new InvalidOperationException("Consumer is not registered.");
        }

        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            AckedDeliveryTags.Add(deliveryTag);
        }

        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            NackedDeliveryTags.Add(deliveryTag);
            LastNackRequeue = requeue;
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            RejectedDeliveryTags.Add(deliveryTag);
        }

        public void BasicCancel(string consumerTag)
        {
            CancelledConsumerTags.Add(consumerTag);
        }

        public void Close()
        {
            Closed = true;
        }

        public void Dispose()
        {
            Closed = true;
        }
    }

    private sealed class FakeRabbitMqBasicProperties : IRabbitMqBasicProperties
    {
        public bool Persistent { get; set; }

        public string? ContentType { get; set; }

        public string? Type { get; set; }
    }

    private sealed record PublishedMessage(
        string Exchange,
        string RoutingKey,
        bool Mandatory,
        FakeRabbitMqBasicProperties Properties,
        ReadOnlyMemory<byte> Body);
}
