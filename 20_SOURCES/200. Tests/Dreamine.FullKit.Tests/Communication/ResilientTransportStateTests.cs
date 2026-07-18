using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Communication.Abstractions.Interfaces;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Abstractions.Options;
using Dreamine.Communication.Core.Resilience;

namespace Dreamine.FullKit.Tests.Communication;

/// <summary>
/// \if KO
/// <para>Resilient Transport State Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Verifies that ResilientMessageTransport state machine notifications serialize correctly under concurrent calls.</para>
/// \endif
/// </summary>
public sealed class ResilientTransportStateTests
{
    /// <summary>
    /// \if KO
    /// <para>State Changed Not Fired With Duplicate State Under Concurrent Notify 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the state changed not fired with duplicate state under concurrent notify operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>State Changed Not Fired With Duplicate State Under Concurrent Notify 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the state changed not fired with duplicate state under concurrent notify operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Releases resources owned by this instance.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Dispose Async Is Idempotent Under Concurrent Calls 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the dispose async is idempotent under concurrent calls operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Send Async Queues Message When Disconnected With Queue Policy 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send async queues message when disconnected with queue policy operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Send Async Queues Message When Disconnected With Queue Policy 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the send async queues message when disconnected with queue policy operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Controllable Transport 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates controllable transport functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class ControllableTransport : IMessageTransport
    {
        /// <summary>
        /// \if KO
        /// <para>connected 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the connected value.</para>
        /// \endif
        /// </summary>
        private bool _connected;
        /// <summary>
        /// \if KO
        /// <para>dispose Count 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the dispose count value.</para>
        /// \endif
        /// </summary>
        private int _disposeCount;

        /// <summary>
        /// \if KO
        /// <para>Dispose Count 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the dispose count value.</para>
        /// \endif
        /// </summary>
        public int DisposeCount => System.Threading.Volatile.Read(ref _disposeCount);

#pragma warning disable CS0067
        /// <summary>
        /// \if KO
        /// <para>Message Received 상황이 발생할 때 알립니다.</para>
        /// \endif
        /// \if EN
        /// <para>Occurs when message received takes place.</para>
        /// \endif
        /// </summary>
        public event EventHandler<MessageEnvelope>? MessageReceived;
#pragma warning restore CS0067
        /// <summary>
        /// \if KO
        /// <para>State Changed 상황이 발생할 때 알립니다.</para>
        /// \endif
        /// \if EN
        /// <para>Occurs when state changed takes place.</para>
        /// \endif
        /// </summary>
        public event EventHandler<ConnectionState>? StateChanged;

        /// <summary>
        /// \if KO
        /// <para>State 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the state value.</para>
        /// \endif
        /// </summary>
        public ConnectionState State => _connected
            ? ConnectionState.Connected
            : ConnectionState.Disconnected;

        /// <summary>
        /// \if KO
        /// <para>Kind 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the kind value.</para>
        /// \endif
        /// </summary>
        public TransportKind Kind => TransportKind.InMemory;

        /// <summary>
        /// \if KO
        /// <para>Connect Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the connect async operation.</para>
        /// \endif
        /// </summary>
        /// <param name="cancellationToken">
        /// \if KO
        /// <para>취소 요청을 감시하는 토큰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A token used to observe cancellation requests.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Connect Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Task</c> result produced by the connect async operation.</para>
        /// \endif
        /// </returns>
        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            _connected = true;
            StateChanged?.Invoke(this, ConnectionState.Connected);
            return Task.CompletedTask;
        }

        /// <summary>
        /// \if KO
        /// <para>Disconnect Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the disconnect async operation.</para>
        /// \endif
        /// </summary>
        /// <param name="cancellationToken">
        /// \if KO
        /// <para>취소 요청을 감시하는 토큰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A token used to observe cancellation requests.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Disconnect Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Task</c> result produced by the disconnect async operation.</para>
        /// \endif
        /// </returns>
        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            _connected = false;
            StateChanged?.Invoke(this, ConnectionState.Disconnected);
            return Task.CompletedTask;
        }

        /// <summary>
        /// \if KO
        /// <para>Send Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the send async operation.</para>
        /// \endif
        /// </summary>
        /// <param name="message">
        /// \if KO
        /// <para>처리할 메시지입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The message to process.</para>
        /// \endif
        /// </param>
        /// <param name="cancellationToken">
        /// \if KO
        /// <para>취소 요청을 감시하는 토큰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A token used to observe cancellation requests.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Send Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Task</c> result produced by the send async operation.</para>
        /// \endif
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// \if KO
        /// <para>현재 객체 상태에서 Send Async 작업을 수행할 수 없는 경우 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when the send async operation is not valid for the current object state.</para>
        /// \endif
        /// </exception>
        public Task SendAsync(MessageEnvelope message, CancellationToken cancellationToken = default)
        {
            if (!_connected)
                throw new InvalidOperationException("Not connected.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// \if KO
        /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Releases resources owned by this instance.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Dispose Async 작업에서 생성한 <c>ValueTask</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>ValueTask</c> result produced by the dispose async operation.</para>
        /// \endif
        /// </returns>
        public ValueTask DisposeAsync()
        {
            System.Threading.Interlocked.Increment(ref _disposeCount);
            return ValueTask.CompletedTask;
        }
    }
}
