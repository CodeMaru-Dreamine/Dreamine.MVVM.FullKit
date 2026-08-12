using System.IO;
using System.Net;
using System.Net.Sockets;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;
using Dreamine.SecsGem.Interop.Runtime.Logging;
using Dreamine.SecsGem.Interop.Runtime.Profiles;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class PublicRuntimeSampleLoggingTests
{
    [Fact]
    public async Task PublicFacadeRecordsHeaderOnlyTrafficAndTerminalState()
    {
        var root = Path.Combine(Path.GetTempPath(), $"dreamine-public-wire-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var options = new InteropWireLogSessionOptions(root)
        {
            LogPolicyId = ConnectionLogPolicyIds.HeaderOnlyV1,
            ObservationQueueCapacity = 32,
            RecorderQueueCapacity = 32,
            MaximumSegmentBytes = 4096,
            RetainedSegments = 2
        };
        var port = ReservePort();
        var equipment = CreateSession(port, SecsConnectionMode.Passive, SecsRole.Equipment, null);
        var host = CreateSession(
            port,
            SecsConnectionMode.Active,
            SecsRole.Host,
            options.CreateObservationOptions());
        var capture = InteropWireLogSession.Start(host, options);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var passiveConnect = equipment.ConnectAsync(timeout.Token);
            await WaitUntilAsync(() => equipment.State == ConnectionState.Listening, timeout.Token);
            await host.ConnectAsync(timeout.Token);
            await passiveConnect;
            await host.SelectAsync(timeout.Token);
            await host.SendAsync(
                new SecsStream(127),
                new SecsFunction(1),
                new SecsAsciiItem("SAMPLE-SECRET-MUST-NOT-BE-LOGGED"),
                timeout.Token);
            await host.DisconnectAsync(timeout.Token);
            await host.DisposeAsync();
            await capture.StopAsync();

            Assert.True(capture.Health.IsEvidenceEligible, capture.Health.Failure);
            Assert.NotEmpty(capture.FinalizedSegments);
            var records = new List<WireLogRecord>();
            foreach (var segment in capture.FinalizedSegments)
            {
                await foreach (var record in WireLogReader.ReadAsync(segment)) records.Add(record);
                var serialized = await File.ReadAllTextAsync(segment);
                Assert.DoesNotContain("SAMPLE-SECRET-MUST-NOT-BE-LOGGED", serialized, StringComparison.Ordinal);
                Assert.DoesNotContain("127.0.0.1", serialized, StringComparison.Ordinal);
                Assert.Contains("\"endpoint\":\"redacted\"", serialized, StringComparison.Ordinal);
            }
            Assert.Contains(records, record => record.Kind == WireLogRecordKind.Frame &&
                                               record.Direction == HsmsWireDirection.Outbound &&
                                               record.Stream == 127 && record.Function == 1 &&
                                               record.BodyBytes is null && record.DecodedItem is null);
            Assert.Contains(records, record => record.Kind == WireLogRecordKind.StateTransition &&
                                               record.CurrentConnectionState == ConnectionState.Disconnected);
        }
        finally
        {
            await capture.DisposeAsync();
            await host.DisposeAsync();
            await equipment.DisposeAsync();
            try { Directory.Delete(root, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static HsmsSession CreateSession(
        int port,
        SecsConnectionMode mode,
        SecsRole role,
        HsmsWireObservationOptions? wireObservation) => new(new HsmsSessionOptions
        {
            Host = "127.0.0.1",
            Port = port,
            Mode = mode,
            Role = role,
            SessionId = new SecsSessionId(37),
            WireObservation = wireObservation
        });

    private static int ReservePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, CancellationToken cancellationToken)
    {
        while (!predicate()) await Task.Delay(10, cancellationToken);
    }
}
