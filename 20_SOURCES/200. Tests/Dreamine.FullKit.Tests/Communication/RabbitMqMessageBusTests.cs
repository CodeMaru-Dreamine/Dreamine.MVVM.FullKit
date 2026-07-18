using System.Text.Json;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.RabbitMQ.Buses;
using Dreamine.Communication.RabbitMQ.Infrastructure;
using Dreamine.Communication.RabbitMQ.Options;

namespace Dreamine.FullKit.Tests.Communication;

/// <summary>
/// \if KO
/// <para>Rabbit Mq Message Bus Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates rabbit mq message bus tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class RabbitMqMessageBusTests
{
    /// <summary>
    /// \if KO
    /// <para>Connect Async Declares Configured Topology 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect async declares configured topology operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Connect Async Declares Configured Topology 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the connect async declares configured topology operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Publish Async Uses Message Route And Serializes Envelope 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the publish async uses message route and serializes envelope operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Publish Async Uses Message Route And Serializes Envelope 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the publish async uses message route and serializes envelope operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Publish Async Uses Configured Routing Key When Message Route Is Empty 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the publish async uses configured routing key when message route is empty operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Publish Async Uses Configured Routing Key When Message Route Is Empty 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the publish async uses configured routing key when message route is empty operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Subscribe Async Binds Route Consumes And Acknowledges Handled Delivery 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the subscribe async binds route consumes and acknowledges handled delivery operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Subscribe Async Binds Route Consumes And Acknowledges Handled Delivery 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the subscribe async binds route consumes and acknowledges handled delivery operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Subscribe Async Allows Multiple Handlers For Same Route 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the subscribe async allows multiple handlers for same route operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Subscribe Async Allows Multiple Handlers For Same Route 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the subscribe async allows multiple handlers for same route operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task SubscribeAsync_AllowsMultipleHandlersForSameRoute()
    {
        var fixture = new RabbitMqFixture(new RabbitMqMessageBusOptions
        {
            ExchangeName = "dreamine.exchange",
            QueueName = "dreamine.queue"
        });
        var calls = new List<string>();

        await fixture.Bus.ConnectAsync();
        await fixture.Bus.SubscribeAsync(
            "custom.route",
            (_, _) =>
            {
                calls.Add("first");
                return Task.CompletedTask;
            });
        await fixture.Bus.SubscribeAsync(
            "custom.route",
            (_, _) =>
            {
                calls.Add("second");
                return Task.CompletedTask;
            });

        var delivery = new RabbitMqDelivery(
            13,
            "custom.route",
            JsonSerializer.SerializeToUtf8Bytes(new MessageEnvelope
            {
                Name = "Rabbit.Receive",
                Route = "custom.route"
            }));

        await fixture.Channel.DeliverAsync(delivery);

        Assert.Equal(["first", "second"], calls);
        Assert.Equal([13UL], fixture.Channel.AckedDeliveryTags);
        Assert.Empty(fixture.Channel.NackedDeliveryTags);
    }

    /// <summary>
    /// \if KO
    /// <para>Subscribe Async Nacks Delivery When Handler Fails 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the subscribe async nacks delivery when handler fails operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Subscribe Async Nacks Delivery When Handler Fails 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the subscribe async nacks delivery when handler fails operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Disconnect Async Cancels Consumer And Closes Resources 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect async cancels consumer and closes resources operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Disconnect Async Cancels Consumer And Closes Resources 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the disconnect async cancels consumer and closes resources operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Rabbit Mq Fixture 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates rabbit mq fixture functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class RabbitMqFixture
    {
        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="RabbitMqFixture"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="RabbitMqFixture"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="options">
        /// \if KO
        /// <para>동작을 구성하는 설정입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The options that configure the operation.</para>
        /// \endif
        /// </param>
        public RabbitMqFixture(RabbitMqMessageBusOptions options)
        {
            Channel = new FakeRabbitMqChannel();
            Connection = new FakeRabbitMqConnection(Channel);
            Bus = new RabbitMqMessageBus(options, new FakeRabbitMqConnectionFactory(Connection));
        }

        /// <summary>
        /// \if KO
        /// <para>Bus 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the bus value.</para>
        /// \endif
        /// </summary>
        public RabbitMqMessageBus Bus { get; }

        /// <summary>
        /// \if KO
        /// <para>Connection 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the connection value.</para>
        /// \endif
        /// </summary>
        public FakeRabbitMqConnection Connection { get; }

        /// <summary>
        /// \if KO
        /// <para>Channel 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the channel value.</para>
        /// \endif
        /// </summary>
        public FakeRabbitMqChannel Channel { get; }
    }

    /// <summary>
    /// \if KO
    /// <para>Fake Rabbit Mq Connection Factory 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates fake rabbit mq connection factory functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class FakeRabbitMqConnectionFactory : IRabbitMqConnectionFactory
    {
        /// <summary>
        /// \if KO
        /// <para>connection 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the connection value.</para>
        /// \endif
        /// </summary>
        private readonly FakeRabbitMqConnection _connection;

        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="FakeRabbitMqConnectionFactory"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="FakeRabbitMqConnectionFactory"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="connection">
        /// \if KO
        /// <para>connection에 사용할 <c>FakeRabbitMqConnection</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>FakeRabbitMqConnection</c> value used for connection.</para>
        /// \endif
        /// </param>
        public FakeRabbitMqConnectionFactory(FakeRabbitMqConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// \if KO
        /// <para>Connection 값을 생성합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Creates the connection value.</para>
        /// \endif
        /// </summary>
        /// <param name="options">
        /// \if KO
        /// <para>동작을 구성하는 설정입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The options that configure the operation.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Create Connection 작업에서 생성한 <c>IRabbitMqConnection</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IRabbitMqConnection</c> result produced by the create connection operation.</para>
        /// \endif
        /// </returns>
        public IRabbitMqConnection CreateConnection(RabbitMqMessageBusOptions options)
        {
            return _connection;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Fake Rabbit Mq Connection 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates fake rabbit mq connection functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class FakeRabbitMqConnection : IRabbitMqConnection
    {
        /// <summary>
        /// \if KO
        /// <para>channel 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the channel value.</para>
        /// \endif
        /// </summary>
        private readonly FakeRabbitMqChannel _channel;

        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="FakeRabbitMqConnection"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="FakeRabbitMqConnection"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="channel">
        /// \if KO
        /// <para>channel에 사용할 <c>FakeRabbitMqChannel</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>FakeRabbitMqChannel</c> value used for channel.</para>
        /// \endif
        /// </param>
        public FakeRabbitMqConnection(FakeRabbitMqChannel channel)
        {
            _channel = channel;
        }

        /// <summary>
        /// \if KO
        /// <para>Is Open 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the is open value.</para>
        /// \endif
        /// </summary>
        public bool IsOpen => !Closed;

        /// <summary>
        /// \if KO
        /// <para>Closed 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the closed value.</para>
        /// \endif
        /// </summary>
        public bool Closed { get; private set; }

        /// <summary>
        /// \if KO
        /// <para>Channel 값을 생성합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Creates the channel value.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Create Channel 작업에서 생성한 <c>IRabbitMqChannel</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IRabbitMqChannel</c> result produced by the create channel operation.</para>
        /// \endif
        /// </returns>
        public IRabbitMqChannel CreateChannel()
        {
            return _channel;
        }

        /// <summary>
        /// \if KO
        /// <para>Close 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the close operation.</para>
        /// \endif
        /// </summary>
        public void Close()
        {
            Closed = true;
        }

        /// <summary>
        /// \if KO
        /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Releases resources owned by this instance.</para>
        /// \endif
        /// </summary>
        public void Dispose()
        {
            Closed = true;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Fake Rabbit Mq Channel 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates fake rabbit mq channel functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class FakeRabbitMqChannel : IRabbitMqChannel
    {
        /// <summary>
        /// \if KO
        /// <para>on Received 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the on received value.</para>
        /// \endif
        /// </summary>
        private Func<RabbitMqDelivery, CancellationToken, Task>? _onReceived;

        /// <summary>
        /// \if KO
        /// <para>Is Open 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the is open value.</para>
        /// \endif
        /// </summary>
        public bool IsOpen => !Closed;

        /// <summary>
        /// \if KO
        /// <para>Closed 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the closed value.</para>
        /// \endif
        /// </summary>
        public bool Closed { get; private set; }

        /// <summary>
        /// \if KO
        /// <para>Last Nack Requeue 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the last nack requeue value.</para>
        /// \endif
        /// </summary>
        public bool LastNackRequeue { get; private set; }

        /// <summary>
        /// \if KO
        /// <para>Operations 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the operations value.</para>
        /// \endif
        /// </summary>
        public List<string> Operations { get; } = [];

        /// <summary>
        /// \if KO
        /// <para>Published Messages 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the published messages value.</para>
        /// \endif
        /// </summary>
        public List<PublishedMessage> PublishedMessages { get; } = [];

        /// <summary>
        /// \if KO
        /// <para>Acked Delivery Tags 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the acked delivery tags value.</para>
        /// \endif
        /// </summary>
        public List<ulong> AckedDeliveryTags { get; } = [];

        /// <summary>
        /// \if KO
        /// <para>Nacked Delivery Tags 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the nacked delivery tags value.</para>
        /// \endif
        /// </summary>
        public List<ulong> NackedDeliveryTags { get; } = [];

        /// <summary>
        /// \if KO
        /// <para>Rejected Delivery Tags 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the rejected delivery tags value.</para>
        /// \endif
        /// </summary>
        public List<ulong> RejectedDeliveryTags { get; } = [];

        /// <summary>
        /// \if KO
        /// <para>Cancelled Consumer Tags 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the cancelled consumer tags value.</para>
        /// \endif
        /// </summary>
        public List<string> CancelledConsumerTags { get; } = [];

        /// <summary>
        /// \if KO
        /// <para>Exchange Declare 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the exchange declare operation.</para>
        /// \endif
        /// </summary>
        /// <param name="exchange">
        /// \if KO
        /// <para>exchange에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for exchange.</para>
        /// \endif
        /// </param>
        /// <param name="type">
        /// \if KO
        /// <para>type에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for type.</para>
        /// \endif
        /// </param>
        /// <param name="durable">
        /// \if KO
        /// <para>durable에 사용할 <c>bool</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>bool</c> value used for durable.</para>
        /// \endif
        /// </param>
        /// <param name="autoDelete">
        /// \if KO
        /// <para>auto Delete에 사용할 <c>bool</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>bool</c> value used for auto delete.</para>
        /// \endif
        /// </param>
        public void ExchangeDeclare(
            string exchange,
            string type,
            bool durable,
            bool autoDelete)
        {
            Operations.Add($"exchange:{exchange}:{type}:{durable}:{autoDelete}");
        }

        /// <summary>
        /// \if KO
        /// <para>Queue Declare 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the queue declare operation.</para>
        /// \endif
        /// </summary>
        /// <param name="queue">
        /// \if KO
        /// <para>queue에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for queue.</para>
        /// \endif
        /// </param>
        /// <param name="durable">
        /// \if KO
        /// <para>durable에 사용할 <c>bool</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>bool</c> value used for durable.</para>
        /// \endif
        /// </param>
        /// <param name="exclusive">
        /// \if KO
        /// <para>exclusive에 사용할 <c>bool</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>bool</c> value used for exclusive.</para>
        /// \endif
        /// </param>
        /// <param name="autoDelete">
        /// \if KO
        /// <para>auto Delete에 사용할 <c>bool</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>bool</c> value used for auto delete.</para>
        /// \endif
        /// </param>
        public void QueueDeclare(
            string queue,
            bool durable,
            bool exclusive,
            bool autoDelete)
        {
            Operations.Add($"queue:{queue}:{durable}:{exclusive}:{autoDelete}");
        }

        /// <summary>
        /// \if KO
        /// <para>Queue Bind 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the queue bind operation.</para>
        /// \endif
        /// </summary>
        /// <param name="queue">
        /// \if KO
        /// <para>queue에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for queue.</para>
        /// \endif
        /// </param>
        /// <param name="exchange">
        /// \if KO
        /// <para>exchange에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for exchange.</para>
        /// \endif
        /// </param>
        /// <param name="routingKey">
        /// \if KO
        /// <para>routing Key에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for routing key.</para>
        /// \endif
        /// </param>
        public void QueueBind(
            string queue,
            string exchange,
            string routingKey)
        {
            Operations.Add($"bind:{queue}:{exchange}:{routingKey}");
        }

        /// <summary>
        /// \if KO
        /// <para>Basic Properties 값을 생성합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Creates the basic properties value.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Create Basic Properties 작업에서 생성한 <c>IRabbitMqBasicProperties</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IRabbitMqBasicProperties</c> result produced by the create basic properties operation.</para>
        /// \endif
        /// </returns>
        public IRabbitMqBasicProperties CreateBasicProperties()
        {
            return new FakeRabbitMqBasicProperties();
        }

        /// <summary>
        /// \if KO
        /// <para>Basic Publish 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the basic publish operation.</para>
        /// \endif
        /// </summary>
        /// <param name="exchange">
        /// \if KO
        /// <para>exchange에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for exchange.</para>
        /// \endif
        /// </param>
        /// <param name="routingKey">
        /// \if KO
        /// <para>routing Key에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for routing key.</para>
        /// \endif
        /// </param>
        /// <param name="mandatory">
        /// \if KO
        /// <para>mandatory에 사용할 <c>bool</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>bool</c> value used for mandatory.</para>
        /// \endif
        /// </param>
        /// <param name="properties">
        /// \if KO
        /// <para>properties에 사용할 <c>IRabbitMqBasicProperties</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IRabbitMqBasicProperties</c> value used for properties.</para>
        /// \endif
        /// </param>
        /// <param name="body">
        /// \if KO
        /// <para>body에 사용할 <c>ReadOnlyMemory&lt;byte&gt;</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>ReadOnlyMemory&lt;byte&gt;</c> value used for body.</para>
        /// \endif
        /// </param>
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

        /// <summary>
        /// \if KO
        /// <para>Basic Consume 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the basic consume operation.</para>
        /// \endif
        /// </summary>
        /// <param name="queue">
        /// \if KO
        /// <para>queue에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for queue.</para>
        /// \endif
        /// </param>
        /// <param name="autoAck">
        /// \if KO
        /// <para>auto Ack에 사용할 <c>bool</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>bool</c> value used for auto ack.</para>
        /// \endif
        /// </param>
        /// <param name="onReceived">
        /// \if KO
        /// <para>on Received에 사용할 <c>Func&lt;RabbitMqDelivery, CancellationToken, Task&gt;</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Func&lt;RabbitMqDelivery, CancellationToken, Task&gt;</c> value used for on received.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Basic Consume 작업에서 생성한 <c>string</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> result produced by the basic consume operation.</para>
        /// \endif
        /// </returns>
        public string BasicConsume(
            string queue,
            bool autoAck,
            Func<RabbitMqDelivery, CancellationToken, Task> onReceived)
        {
            Operations.Add($"consume:{queue}:{autoAck}");
            _onReceived = onReceived;
            return "consumer-1";
        }

        /// <summary>
        /// \if KO
        /// <para>Deliver Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the deliver async operation.</para>
        /// \endif
        /// </summary>
        /// <param name="delivery">
        /// \if KO
        /// <para>delivery에 사용할 <c>RabbitMqDelivery</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>RabbitMqDelivery</c> value used for delivery.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Deliver Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Task</c> result produced by the deliver async operation.</para>
        /// \endif
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// \if KO
        /// <para>현재 객체 상태에서 Deliver Async 작업을 수행할 수 없는 경우 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when the deliver async operation is not valid for the current object state.</para>
        /// \endif
        /// </exception>
        public Task DeliverAsync(RabbitMqDelivery delivery)
        {
            return _onReceived?.Invoke(delivery, CancellationToken.None)
                ?? throw new InvalidOperationException("Consumer is not registered.");
        }

        /// <summary>
        /// \if KO
        /// <para>Basic Ack 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the basic ack operation.</para>
        /// \endif
        /// </summary>
        /// <param name="deliveryTag">
        /// \if KO
        /// <para>delivery Tag에 사용할 <c>ulong</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>ulong</c> value used for delivery tag.</para>
        /// \endif
        /// </param>
        /// <param name="multiple">
        /// \if KO
        /// <para>multiple에 사용할 <c>bool</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>bool</c> value used for multiple.</para>
        /// \endif
        /// </param>
        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            AckedDeliveryTags.Add(deliveryTag);
        }

        /// <summary>
        /// \if KO
        /// <para>Basic Nack 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the basic nack operation.</para>
        /// \endif
        /// </summary>
        /// <param name="deliveryTag">
        /// \if KO
        /// <para>delivery Tag에 사용할 <c>ulong</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>ulong</c> value used for delivery tag.</para>
        /// \endif
        /// </param>
        /// <param name="multiple">
        /// \if KO
        /// <para>multiple에 사용할 <c>bool</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>bool</c> value used for multiple.</para>
        /// \endif
        /// </param>
        /// <param name="requeue">
        /// \if KO
        /// <para>requeue에 사용할 <c>bool</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>bool</c> value used for requeue.</para>
        /// \endif
        /// </param>
        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            NackedDeliveryTags.Add(deliveryTag);
            LastNackRequeue = requeue;
        }

        /// <summary>
        /// \if KO
        /// <para>Basic Reject 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the basic reject operation.</para>
        /// \endif
        /// </summary>
        /// <param name="deliveryTag">
        /// \if KO
        /// <para>delivery Tag에 사용할 <c>ulong</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>ulong</c> value used for delivery tag.</para>
        /// \endif
        /// </param>
        /// <param name="requeue">
        /// \if KO
        /// <para>requeue에 사용할 <c>bool</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>bool</c> value used for requeue.</para>
        /// \endif
        /// </param>
        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            RejectedDeliveryTags.Add(deliveryTag);
        }

        /// <summary>
        /// \if KO
        /// <para>Basic Cancel 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the basic cancel operation.</para>
        /// \endif
        /// </summary>
        /// <param name="consumerTag">
        /// \if KO
        /// <para>consumer Tag에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for consumer tag.</para>
        /// \endif
        /// </param>
        public void BasicCancel(string consumerTag)
        {
            CancelledConsumerTags.Add(consumerTag);
        }

        /// <summary>
        /// \if KO
        /// <para>Close 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the close operation.</para>
        /// \endif
        /// </summary>
        public void Close()
        {
            Closed = true;
        }

        /// <summary>
        /// \if KO
        /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Releases resources owned by this instance.</para>
        /// \endif
        /// </summary>
        public void Dispose()
        {
            Closed = true;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Fake Rabbit Mq Basic Properties 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates fake rabbit mq basic properties functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class FakeRabbitMqBasicProperties : IRabbitMqBasicProperties
    {
        /// <summary>
        /// \if KO
        /// <para>Persistent 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the persistent value.</para>
        /// \endif
        /// </summary>
        public bool Persistent { get; set; }

        /// <summary>
        /// \if KO
        /// <para>Content Type 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the content type value.</para>
        /// \endif
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// \if KO
        /// <para>Type 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the type value.</para>
        /// \endif
        /// </summary>
        public string? Type { get; set; }
    }

    /// <summary>
    /// \if KO
    /// <para>Published Message 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates published message functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed record PublishedMessage(
        string Exchange,
        string RoutingKey,
        bool Mandatory,
        FakeRabbitMqBasicProperties Properties,
        ReadOnlyMemory<byte> Body);
}
