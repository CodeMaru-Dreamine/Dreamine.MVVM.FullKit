using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Abstractions.Options;

namespace Dreamine.FullKit.Tests.Communication;

public sealed class CommunicationAbstractionsTests
{
    [Fact]
    public void OptionTypes_ExposeDocumentedDefaults()
    {
        var communication = new CommunicationOptions();
        var bus = new MessageBusOptions();
        var queue = new OutboundQueueOptions();
        var reconnect = new ReconnectPolicy();
        var transport = new TransportOptions();

        Assert.Equal("DefaultCommunication", communication.Name);
        Assert.False(communication.AutoConnect);
        Assert.Equal(TransportKind.InMemory, bus.Kind);
        Assert.True(bus.ThrowOnHandlerError);
        Assert.Equal(DisconnectedSendPolicy.Queue, queue.DisconnectedSendPolicy);
        Assert.Equal(10_000, queue.MaxQueueSize);
        Assert.True(reconnect.Enabled);
        Assert.Equal(TimeSpan.FromSeconds(1), reconnect.InitialDelay);
        Assert.Equal(TransportKind.Tcp, transport.Kind);
        Assert.Equal("127.0.0.1", transport.Host);
    }

    [Fact]
    public void MessageEnvelope_CreatesReasonableDefaults()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var envelope = new MessageEnvelope();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        Assert.False(string.IsNullOrWhiteSpace(envelope.MessageId));
        Assert.Equal(string.Empty, envelope.Name);
        Assert.Equal(string.Empty, envelope.Route);
        Assert.Empty(envelope.Payload);
        Assert.Empty(envelope.Headers);
        Assert.InRange(envelope.CreatedAt, before, after);
    }
}
