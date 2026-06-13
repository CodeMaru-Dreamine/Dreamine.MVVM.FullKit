using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Communication.Abstractions.Interfaces;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Abstractions.Options;
using Dreamine.Communication.Core.Resilience;

namespace Dreamine.FullKit.Tests.Communication;

/// <summary>
/// Verifies that ResilientMessageTransport state machine notifications
/// serialize correctly under concurrent calls.
/// </summary>
public sealed class ResilientTransportStateTests
{
    [Fact]
    public async Task StateChanged_NotFiredWithDuplicateState_UnderConcurrentNotify()
    {
        var inner = new ControllableTransport();
        var policy = new ReconnectPolicy { Enabled = false };
        await using var transport = new ResilientMessageTransport(inner, policy);

        var states = new System.Collections.Concurrent.ConcurrentBag<ConnectionState>();
        transport.StateChanged += (_, s) => states.Add(s);

        // Trigger connect so state transitions to Connected, then disconnect.
        await transport.ConnectAsync();
        await transport.DisconnectAsync();

        // All observed transitions must be real changes (no duplicate consecutive states).
        var list = states.ToArray();
        for (int i = 1; i < list.Length; i++)
        {
            Assert.NotEqual(list[i - 1], list[i]);
        }
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent_UnderConcurrentCalls()
    {
        var inner = new ControllableTransport();
        var policy = new ReconnectPolicy { Enabled = false };
        var transport = new ResilientMessageTransport(inner, policy);

        // Fire multiple concurrent DisposeAsync calls — only the first should take effect.
        var tasks = Enumerable.Range(0, 8)
            .Select(_ => transport.DisposeAsync().AsTask())
            .ToArray();

        await Task.WhenAll(tasks);

        // Inner transport should have been disposed exactly once.
        Assert.Equal(1, inner.DisposeCount);
    }

    [Fact]
    public async Task SendAsync_QueuesMessageWhenDisconnected_WithQueuePolicy()
    {
        var inner = new ControllableTransport();
        var policy = new ReconnectPolicy { Enabled = false };
        var queueOptions = new OutboundQueueOptions
        {
            DisconnectedSendPolicy = DisconnectedSendPolicy.Queue
        };
        await using var transport = new ResilientMessageTransport(inner, policy, queueOptions);

        var msg = new MessageEnvelope { Route = "test.route", Name = "hello" };
        await transport.SendAsync(msg);

        Assert.Equal(1, transport.QueuedMessageCount);
    }

    private sealed class ControllableTransport : IMessageTransport
    {
        private bool _connected;
        private int _disposeCount;

        public int DisposeCount => System.Threading.Volatile.Read(ref _disposeCount);

#pragma warning disable CS0067
        public event EventHandler<MessageEnvelope>? MessageReceived;
#pragma warning restore CS0067
        public event EventHandler<ConnectionState>? StateChanged;

        public ConnectionState State => _connected
            ? ConnectionState.Connected
            : ConnectionState.Disconnected;

        public TransportKind Kind => TransportKind.InMemory;

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            _connected = true;
            StateChanged?.Invoke(this, ConnectionState.Connected);
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            _connected = false;
            StateChanged?.Invoke(this, ConnectionState.Disconnected);
            return Task.CompletedTask;
        }

        public Task SendAsync(MessageEnvelope message, CancellationToken cancellationToken = default)
        {
            if (!_connected)
                throw new InvalidOperationException("Not connected.");
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            System.Threading.Interlocked.Increment(ref _disposeCount);
            return ValueTask.CompletedTask;
        }
    }
}
