using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Abstractions.Validation;
using Dreamine.Secs.Com.Hsms;
using Dreamine.SecsGem.FactoryScale.Infrastructure;

namespace Dreamine.SecsGem.FactoryScale.Scenarios;

/// <summary>
/// Exercises receive-loop and timer failure paths with real loopback TCP peers.
/// Each injection owns an independent session; a healthy Factory host probe is
/// executed after every failure so a local fault cannot be mistaken for fleet
/// isolation merely because the faulty connection closed as expected.
/// </summary>
internal sealed class ProtocolFaultInjectionSuite
{
    private readonly ValidatedPortRangeManager _ports;

    internal ProtocolFaultInjectionSuite(ValidatedPortRangeManager ports) =>
        _ports = ports ?? throw new ArgumentNullException(nameof(ports));

    internal async Task<IReadOnlyList<string>> RunAsync(
        Func<CancellationToken, Task> healthyProbe,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(healthyProbe);
        var checks = new List<string>();

        await VerifyT6Async(cancellationToken).ConfigureAwait(false);
        checks.Add("T6: an unanswered Select.req expired and only its independent TCP session faulted.");
        await healthyProbe(cancellationToken).ConfigureAwait(false);

        await VerifyT7Async(cancellationToken).ConfigureAwait(false);
        checks.Add("T7: an unselected TCP session expired without affecting the healthy Factory fleet.");
        await healthyProbe(cancellationToken).ConfigureAwait(false);

        await VerifyT8Async(cancellationToken).ConfigureAwait(false);
        checks.Add("T8: a partial length prefix expired without blocking another equipment context.");
        await healthyProbe(cancellationToken).ConfigureAwait(false);

        await VerifyInvalidFrameLengthAsync(cancellationToken).ConfigureAwait(false);
        checks.Add("Invalid frame length was rejected on its own connection.");
        await healthyProbe(cancellationToken).ConfigureAwait(false);

        await VerifyUnknownSTypeAsync(cancellationToken).ConfigureAwait(false);
        checks.Add("Unknown HSMS SType was rejected on its own connection.");
        await healthyProbe(cancellationToken).ConfigureAwait(false);

        await VerifyTruncatedFrameAsync(cancellationToken).ConfigureAwait(false);
        checks.Add("Truncated HSMS frame was contained to its source connection.");
        await healthyProbe(cancellationToken).ConfigureAwait(false);

        await VerifyCloseImmediatelyAfterConnectAsync(cancellationToken).ConfigureAwait(false);
        checks.Add("Connection close immediately after TCP establishment was observed and cleaned up.");
        await healthyProbe(cancellationToken).ConfigureAwait(false);

        await VerifyCloseDuringMessageSendAsync(cancellationToken).ConfigureAwait(false);
        checks.Add("Connection close during a large message send was observed and cleaned up.");
        await healthyProbe(cancellationToken).ConfigureAwait(false);

        return checks;
    }

    private static async Task VerifyT6Async(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start(1);
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var accept = listener.AcceptTcpClientAsync(cancellationToken).AsTask();
            await using var session = CreateSession(port, SecsConnectionMode.Active, t6Seconds: 1);
            await session.ConnectAsync(cancellationToken).ConfigureAwait(false);
            using var peer = await accept.ConfigureAwait(false);
            Exception? failure = null;
            try { await session.SelectAsync(cancellationToken).ConfigureAwait(false); }
            catch (Exception exception) { failure = exception; }
            Require(failure is HsmsTimerExpiredException { TimerName: "T6" },
                $"Expected T6 expiration, observed {failure?.GetType().Name ?? "success"}.");
            await WaitUntilAsync(() => session.State == ConnectionState.Faulted,
                TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
        }
        finally { listener.Stop(); }
    }

    private async Task VerifyT7Async(CancellationToken cancellationToken)
    {
        await using var fixture = await StartPassiveAsync(t7Seconds: 1, t8Seconds: 5, cancellationToken)
            .ConfigureAwait(false);
        using var peer = new TcpClient();
        await peer.ConnectAsync(IPAddress.Loopback, fixture.Port, cancellationToken).ConfigureAwait(false);
        await fixture.ConnectTask.ConfigureAwait(false);
        await WaitUntilAsync(() => fixture.Session.State == ConnectionState.Faulted,
            TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
        RequireDiagnostic(fixture.Diagnostics, SecsDiagnosticKind.Timeout, "T7");
    }

    private async Task VerifyT8Async(CancellationToken cancellationToken)
    {
        await using var fixture = await StartPassiveAsync(t7Seconds: 5, t8Seconds: 1, cancellationToken)
            .ConfigureAwait(false);
        using var peer = new TcpClient();
        await peer.ConnectAsync(IPAddress.Loopback, fixture.Port, cancellationToken).ConfigureAwait(false);
        await fixture.ConnectTask.ConfigureAwait(false);
        await peer.GetStream().WriteAsync(new byte[] { 0 }, cancellationToken).ConfigureAwait(false);
        await WaitUntilAsync(() => fixture.Session.State == ConnectionState.Faulted,
            TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
        RequireDiagnostic(fixture.Diagnostics, SecsDiagnosticKind.ConnectionClosed, "T8");
    }

    private async Task VerifyInvalidFrameLengthAsync(CancellationToken cancellationToken) =>
        await SendInvalidFrameAsync(BuildLengthOnly(9), closeAfterWrite: false,
            "HSMS length must be at least", cancellationToken).ConfigureAwait(false);

    private async Task VerifyUnknownSTypeAsync(CancellationToken cancellationToken)
    {
        var frame = new byte[14];
        BinaryPrimitives.WriteInt32BigEndian(frame, 10);
        frame[4] = 0xFF;
        frame[5] = 0xFF;
        frame[9] = 0x08;
        BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(10), 1);
        await SendInvalidFrameAsync(frame, closeAfterWrite: false,
            "Unsupported SType", cancellationToken).ConfigureAwait(false);
    }

    private async Task VerifyTruncatedFrameAsync(CancellationToken cancellationToken)
    {
        var frame = new byte[9];
        BinaryPrimitives.WriteInt32BigEndian(frame, 10);
        await SendInvalidFrameAsync(frame, closeAfterWrite: true,
            "stream ended inside an HSMS frame", cancellationToken).ConfigureAwait(false);
    }

    private async Task SendInvalidFrameAsync(
        byte[] frame,
        bool closeAfterWrite,
        string expectedDiagnostic,
        CancellationToken cancellationToken)
    {
        await using var fixture = await StartPassiveAsync(t7Seconds: 5, t8Seconds: 2, cancellationToken)
            .ConfigureAwait(false);
        using var peer = new TcpClient();
        await peer.ConnectAsync(IPAddress.Loopback, fixture.Port, cancellationToken).ConfigureAwait(false);
        await fixture.ConnectTask.ConfigureAwait(false);
        await peer.GetStream().WriteAsync(frame, cancellationToken).ConfigureAwait(false);
        if (closeAfterWrite) peer.Close();
        await WaitUntilAsync(
            () => fixture.Session.State is ConnectionState.Faulted or ConnectionState.Disconnected,
            TimeSpan.FromSeconds(4), cancellationToken).ConfigureAwait(false);
        RequireDiagnostic(fixture.Diagnostics, SecsDiagnosticKind.ConnectionClosed, expectedDiagnostic);
    }

    private static async Task VerifyCloseImmediatelyAfterConnectAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start(1);
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var accept = listener.AcceptTcpClientAsync(cancellationToken).AsTask();
            await using var session = CreateSession(port, SecsConnectionMode.Active, t6Seconds: 2);
            await session.ConnectAsync(cancellationToken).ConfigureAwait(false);
            using var peer = await accept.ConfigureAwait(false);
            peer.Close();
            await WaitUntilAsync(
                () => session.State is ConnectionState.Faulted or ConnectionState.Disconnected,
                TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
        }
        finally { listener.Stop(); }
    }

    private static async Task VerifyCloseDuringMessageSendAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        // Set the accepted socket window before the handshake so loopback autotuning cannot
        // acknowledge the entire large frame before the reset is injected.
        listener.Server.ReceiveBufferSize = 1_024;
        listener.Start(1);
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var accept = listener.AcceptTcpClientAsync(cancellationToken).AsTask();
            await using var session = CreateSession(port, SecsConnectionMode.Active, t6Seconds: 2);
            await session.ConnectAsync(cancellationToken).ConfigureAwait(false);
            using var peer = await accept.ConfigureAwait(false);
            peer.ReceiveBufferSize = 1_024;
            var codec = new HsmsFrameCodec();
            var select = session.SelectAsync(cancellationToken);
            var selectRequest = await codec.ReadAsync(
                peer.GetStream(), TimeSpan.FromSeconds(2), TimeProvider.System, cancellationToken).ConfigureAwait(false);
            if (selectRequest is not HsmsControlMessage { SType: HsmsSType.SelectRequest } request)
                throw new InvalidOperationException("Raw peer did not receive Select.req.");
            await codec.WriteAsync(peer.GetStream(), new HsmsControlMessage(HsmsHeader.CreateControl(
                HsmsSType.SelectResponse, request.Header.SystemBytes)), cancellationToken).ConfigureAwait(false);
            await select.ConfigureAwait(false);

            // A single loopback write can be copied into the Windows socket stack before a peer
            // reset is scheduled. Keep a bounded batch outstanding behind the session send gate;
            // observing the first frame prefix proves transmission started, while unfinished tasks
            // prove the reset is injected before the batch has completed.
            var body = new SecsBinaryItem(new byte[1024 * 1024]);
            var sends = Enumerable.Range(0, 16).Select(_ => session.SendAsync(
                new SecsMessage(new SecsSessionId(0), new SecsStream(6), new SecsFunction(11),
                    false, session.AllocateSystemBytes(), body), cancellationToken)).ToArray();
            var prefix = new byte[4];
            await ReadExactlyAsync(peer.GetStream(), prefix, cancellationToken).ConfigureAwait(false);
            Require(sends.Any(send => !send.IsCompleted),
                "The large-send batch completed before the peer-close injection was applied.");
            peer.Client.LingerState = new LingerOption(true, 0);
            peer.Close();
            var failedSends = 0;
            foreach (var send in sends)
            {
                try { await send.ConfigureAwait(false); }
                catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException)
                {
                    failedSends++;
                }
            }
            Require(failedSends > 0, "No outstanding large send observed the injected peer reset.");
            await WaitUntilAsync(
                () => session.State is ConnectionState.Faulted or ConnectionState.Disconnected,
                TimeSpan.FromSeconds(4), cancellationToken).ConfigureAwait(false);
        }
        finally { listener.Stop(); }
    }

    private Task<PassiveSessionFixture> StartPassiveAsync(
        int t7Seconds,
        int t8Seconds,
        CancellationToken cancellationToken) =>
        _ports.StartBoundAsync(async (port, startToken) =>
        {
            var diagnostics = new RecordingDiagnosticSink();
            var session = CreateSession(port, SecsConnectionMode.Passive,
                t6Seconds: 2, t7Seconds: t7Seconds, t8Seconds: t8Seconds, diagnostics);
            var connect = session.ConnectAsync(startToken);
            try
            {
                await WaitUntilAsync(
                    () => session.State is ConnectionState.Listening or ConnectionState.Faulted,
                    TimeSpan.FromSeconds(3), startToken).ConfigureAwait(false);
                if (session.State == ConnectionState.Faulted) await connect.ConfigureAwait(false);
                return new PassiveSessionFixture(port, session, connect, diagnostics);
            }
            catch
            {
                try { await session.DisposeAsync().ConfigureAwait(false); } catch { }
                Observe(connect);
                throw;
            }
        }, cancellationToken);

    private static HsmsSession CreateSession(
        int port,
        SecsConnectionMode mode,
        int t6Seconds,
        int t7Seconds = 5,
        int t8Seconds = 5,
        ISecsDiagnosticSink? diagnostics = null) =>
        new(new HsmsSessionOptions
        {
            Host = "127.0.0.1",
            Port = port,
            Mode = mode,
            Role = SecsRole.Host,
            SessionId = new SecsSessionId(0),
            AutoReconnect = false,
            Timers = new HsmsTimerOptions
            {
                T3 = TimeSpan.FromSeconds(2),
                T5 = TimeSpan.FromSeconds(1),
                T6 = TimeSpan.FromSeconds(t6Seconds),
                T7 = TimeSpan.FromSeconds(t7Seconds),
                T8 = TimeSpan.FromSeconds(t8Seconds)
            }
        }, diagnostics: diagnostics);

    private static async Task ReadExactlyAsync(
        Stream stream,
        Memory<byte> destination,
        CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < destination.Length)
        {
            var read = await stream.ReadAsync(destination[offset..], cancellationToken).ConfigureAwait(false);
            if (read == 0) throw new EndOfStreamException("Peer closed before the expected bytes arrived.");
            offset += read;
        }
    }

    private static void RequireDiagnostic(
        RecordingDiagnosticSink diagnostics,
        SecsDiagnosticKind kind,
        string messageFragment)
    {
        Require(diagnostics.Events.Any(value =>
                value.Kind == kind && value.Message.Contains(messageFragment, StringComparison.OrdinalIgnoreCase)),
            $"Expected {kind} diagnostic containing '{messageFragment}'.");
    }

    private static byte[] BuildLengthOnly(int length)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, length);
        return bytes;
    }

    private static async Task WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.StartNew();
        while (!condition())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (started.Elapsed >= timeout)
                throw new TimeoutException($"Fault injection condition was not reached within {timeout}.");
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Observe(Task task) => _ = task.ContinueWith(
        static completed => _ = completed.Exception,
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
        TaskScheduler.Default);

    private sealed class PassiveSessionFixture(
        int port,
        HsmsSession session,
        Task connectTask,
        RecordingDiagnosticSink diagnostics) : IAsyncDisposable
    {
        internal int Port { get; } = port;
        internal HsmsSession Session { get; } = session;
        internal Task ConnectTask { get; } = connectTask;
        internal RecordingDiagnosticSink Diagnostics { get; } = diagnostics;

        public async ValueTask DisposeAsync()
        {
            try { await Session.DisposeAsync().ConfigureAwait(false); }
            finally { Observe(ConnectTask); }
        }
    }

    private sealed class RecordingDiagnosticSink : ISecsDiagnosticSink
    {
        private readonly object _gate = new();
        private readonly List<SecsDiagnosticEvent> _events = [];

        internal IReadOnlyList<SecsDiagnosticEvent> Events
        {
            get { lock (_gate) return _events.ToArray(); }
        }

        public void Emit(SecsDiagnosticEvent diagnosticEvent)
        {
            lock (_gate) _events.Add(diagnosticEvent);
        }
    }
}
