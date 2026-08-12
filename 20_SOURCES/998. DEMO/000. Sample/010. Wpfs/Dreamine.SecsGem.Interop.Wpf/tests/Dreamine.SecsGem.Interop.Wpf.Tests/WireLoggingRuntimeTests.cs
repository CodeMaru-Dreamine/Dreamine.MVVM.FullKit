using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;
using Dreamine.SecsGem.Interop.Runtime.Evidence;
using Dreamine.SecsGem.Interop.Runtime.Logging;
using Dreamine.SecsGem.Interop.Runtime.Persistence;
using Dreamine.SecsGem.Interop.Runtime.Profiles;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class WireLoggingRuntimeTests
{
    [Fact]
    public void HeaderOnlyLargeBodyDoesNotCopyDecodeOrFormatTheBody()
    {
        var decoder = new CountingDecoder();
        var frame = EncodeData(6, 11, new SecsBinaryItem(new byte[1024 * 1024]));
        var observation = Observe(frame);
        var record = new WireLogRecordFactory(new WireLogPolicy(), decoder)
            .Create(observation, Identity());

        Assert.Equal(WireBodyCaptureMode.HeaderOnly, record.CaptureMode);
        Assert.Equal(WireLogPolicy.HsmsPrefixAndHeaderLength, record.HeaderBytes?.Length);
        Assert.Null(record.BodyBytes);
        Assert.Null(record.DecodedItem);
        Assert.Equal(0, decoder.CallCount);
        Assert.Equal(frame.Length, record.ActualFrameBytes);
    }

    [Fact]
    public void FullBodyIsExplicitPerDialogueAndDecodedTextIsBounded()
    {
        var frame = EncodeData(6, 11, new SecsBinaryItem(Enumerable.Range(0, 512).Select(value => (byte)value).ToArray()));
        var policy = new WireLogPolicy(rules:
        [
            new WireBodyCaptureRule(6, 11, HsmsWireDirection.Inbound, WireBodyCaptureMode.FullBody, 1024)
        ], maximumDecodedCharacters: 128);

        var record = new WireLogRecordFactory(policy).Create(Observe(frame), Identity());

        Assert.Equal(WireBodyCaptureMode.FullBody, record.CaptureMode);
        Assert.Equal(frame.Length - WireLogPolicy.HsmsPrefixAndHeaderLength, record.BodyBytes?.Length);
        Assert.NotNull(record.DecodedItem);
        Assert.InRange(record.DecodedItem!.Length, 1, 128);
        Assert.EndsWith("…", record.DecodedItem, StringComparison.Ordinal);
        Assert.Null(record.DecodeError);
    }

    [Fact]
    public void RuntimePolicyCompilesToCoreCaptureRulesBeforeSessionCreation()
    {
        var policy = new WireLogPolicy(rules:
        [
            new WireBodyCaptureRule(6, 11, HsmsWireDirection.Inbound, WireBodyCaptureMode.FullBody, 1024),
            new WireBodyCaptureRule(10, 3, null, WireBodyCaptureMode.Excluded)
        ]);

        var options = policy.CreateObservationOptions(queueCapacity: 32);

        Assert.Equal(HsmsWireCaptureMode.HeaderOnly, options.DefaultCaptureMode);
        Assert.Equal(WireLogPolicy.HsmsPrefixAndHeaderLength + 1024, options.MaximumCapturedBytes);
        Assert.Contains(options.CaptureRules, rule => rule.Stream == 6 && rule.Function == 11 &&
            rule.Mode == HsmsWireCaptureMode.FullFrame &&
            rule.MaximumCapturedBytes == WireLogPolicy.HsmsPrefixAndHeaderLength + 1024);
        Assert.Contains(options.CaptureRules, rule => rule.Stream == 10 && rule.Function == 3 &&
            rule.Mode == HsmsWireCaptureMode.Excluded);
    }

    [Fact]
    public void FullBodyLimitPreventsDecodeOfAnOversizedBody()
    {
        var decoder = new CountingDecoder();
        var frame = EncodeData(6, 11, new SecsBinaryItem(new byte[4096]));
        var policy = new WireLogPolicy(rules:
        [
            new WireBodyCaptureRule(6, 11, HsmsWireDirection.Inbound, WireBodyCaptureMode.FullBody, 128)
        ]);

        var record = new WireLogRecordFactory(policy, decoder).Create(Observe(frame), Identity());

        Assert.Equal(128, record.BodyBytes?.Length);
        Assert.Equal(0, decoder.CallCount);
        Assert.Null(record.DecodedItem);
        Assert.Contains("exceeds", record.DecodeError, StringComparison.Ordinal);
    }

    [Fact]
    public void ExcludedPolicyRetainsOnlyStructuredMetadata()
    {
        var frame = EncodeData(10, 3, new SecsAsciiItem("sensitive"));
        var policy = new WireLogPolicy(WireBodyCaptureMode.Excluded);

        var record = new WireLogRecordFactory(policy).Create(Observe(frame), Identity());

        Assert.Equal((byte)10, record.Stream);
        Assert.Equal((byte)3, record.Function);
        Assert.Equal((uint)1, record.SystemBytes);
        Assert.Null(record.HeaderBytes);
        Assert.Null(record.BodyBytes);
        Assert.Null(record.DecodedItem);
        Assert.Null(record.DecodeError);
    }

    [Fact]
    public void ControlFrameDoesNotPretendToBeStreamZeroFunctionZero()
    {
        var frame = new HsmsFrameCodec().Encode(new HsmsControlMessage(
            HsmsHeader.CreateControl(HsmsSType.LinktestRequest, new SecsSystemBytes(3))));

        var record = new WireLogRecordFactory(new WireLogPolicy()).Create(Observe(frame), Identity());

        Assert.Null(record.Stream);
        Assert.Null(record.Function);
        Assert.Null(record.ReplyExpected);
        Assert.Equal((byte)HsmsSType.LinktestRequest, record.SType);
    }

    [Fact]
    public async Task BoundedRecorderDropsNewestWithoutBlockingObservationDrain()
    {
        var source = new FakeObservationSource();
        var sink = new BlockingSink();
        await using var recorder = new WireLogRecorder(source, Identity(), new WireLogRecorderOptions
        {
            QueueCapacity = 1,
            Policy = new WireLogPolicy()
        }, sink);
        var frame = EncodeData(1, 1, null);

        source.Publish(Observe(frame, 1));
        await sink.Entered.WaitAsync(TimeSpan.FromSeconds(5));
        for (var sequence = 2; sequence <= 20; sequence++) source.Publish(Observe(frame, sequence));
        source.Complete();
        await source.ReadAll.WaitAsync(TimeSpan.FromSeconds(5));
        sink.Release();

        await recorder.DisposeAsync();

        Assert.True(recorder.Health.RecorderDropped > 0);
        Assert.True(recorder.Health.Written >= 1);
        Assert.False(recorder.Health.IsEvidenceEligible);
    }

    [Fact]
    public async Task WriterFailureIsObservedAndDoesNotStopSourceDrain()
    {
        var source = new FakeObservationSource();
        await using var recorder = new WireLogRecorder(source, Identity(), new WireLogRecorderOptions(), new ThrowingSink());
        var frame = EncodeData(1, 1, null);
        for (var sequence = 1; sequence <= 5; sequence++) source.Publish(Observe(frame, sequence));
        source.Complete();
        await source.ReadAll.WaitAsync(TimeSpan.FromSeconds(5));

        await recorder.DisposeAsync();

        Assert.NotNull(recorder.Health.WriterFailure);
        Assert.False(recorder.Health.IsEvidenceEligible);
    }

    [Fact]
    public async Task StoppingBeforeSourceCompletionMakesEvidenceIneligible()
    {
        var source = new FakeObservationSource();
        await using var recorder = new WireLogRecorder(source, Identity(), new WireLogRecorderOptions(), new CollectingSink());

        await recorder.DisposeAsync();

        Assert.NotNull(recorder.Health.WriterFailure);
        Assert.False(recorder.Health.IsEvidenceEligible);
    }

    [Fact]
    public async Task NonCooperativeWriterCannotExceedTheRecorderShutdownDeadline()
    {
        var source = new FakeObservationSource();
        var sink = new NonCooperativeSink();
        await using var recorder = new WireLogRecorder(source, Identity(), new WireLogRecorderOptions
        {
            ShutdownTimeout = TimeSpan.FromMilliseconds(100)
        }, sink);
        source.Publish(Observe(EncodeData(1, 1, null)));
        source.Complete();
        await source.ReadAll.WaitAsync(TimeSpan.FromSeconds(5));
        await sink.Entered.WaitAsync(TimeSpan.FromSeconds(5));

        var started = DateTimeOffset.UtcNow;
        await recorder.DisposeAsync();

        Assert.True(DateTimeOffset.UtcNow - started < TimeSpan.FromSeconds(2));
        Assert.Contains("deadline", recorder.Health.WriterFailure, StringComparison.Ordinal);
        Assert.False(recorder.Health.IsEvidenceEligible);
        sink.Release();
        await sink.Exited.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task JsonlSinkFinalizesRollsAndRetainsOnlyOwnedSegments()
    {
        using var directory = new TemporaryDirectory();
        var foreign = Path.Combine(directory.Path, "keep-me.jsonl");
        await File.WriteAllTextAsync(foreign, "foreign");
        var sink = new JsonlWireLogSink(new WireLogStorageOptions
        {
            RootDirectory = directory.Path,
            MaximumSegmentBytes = 4096,
            MaximumRecordBytes = 8192,
            RetainedSegments = 2
        });
        var record = Record() with { Error = new string('E', 1200) };
        for (var index = 0; index < 10; index++)
            await sink.AppendAsync(record with { Sequence = index + 1 }, CancellationToken.None);

        await sink.CompleteAsync(CancellationToken.None);
        await sink.DisposeAsync();

        Assert.InRange(sink.FinalizedSegments.Count, 1, 2);
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.active"));
        Assert.True(File.Exists(foreign));
    }

    [Fact]
    public async Task JsonlSinkRecoversOnlyCompleteRecordsFromAnOwnedActiveSegment()
    {
        using var directory = new TemporaryDirectory();
        var active = Path.Combine(directory.Path, $"{JsonlWireLogSink.OwnedPrefix}crash.active");
        var valid = JsonSerializer.Serialize(Record());
        await File.WriteAllTextAsync(active, valid + "\n{partial");

        await using var sink = new JsonlWireLogSink(new WireLogStorageOptions
        {
            RootDirectory = directory.Path
        });

        var recovered = Assert.Single(sink.FinalizedSegments);
        var records = new List<WireLogRecord>();
        await foreach (var record in WireLogReader.ReadAsync(recovered)) records.Add(record);
        Assert.Single(records);
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.active"));
    }

    [Fact]
    public async Task WorkbenchWireLogManagerPersistsLiveRecordsAndIncrementallyReopensThem()
    {
        using var directory = new TemporaryDirectory();
        var viewLog = new InteropLogManager();
        await using var manager = new WireLogManager(viewLog, directory.Path);
        var source = new FakeObservationSource();

        var observationOptions = manager.CreateObservationOptions();
        Assert.Equal(HsmsWireCaptureMode.HeaderOnly, observationOptions.DefaultCaptureMode);
        manager.Start(source, Identity());
        source.Publish(Observe(EncodeData(1, 1, null)));
        source.Complete();
        await source.ReadAll.WaitAsync(TimeSpan.FromSeconds(5));
        await manager.StopAsync();

        Assert.True(manager.LastHealth.IsEvidenceEligible);
        var segment = Assert.Single(manager.FinalizedSegments);
        Assert.Single(viewLog.Secs1Entries);
        viewLog.Clear();

        var loaded = await manager.OpenAsync(segment, new WireLogFilter(Stream: 1), CancellationToken.None);

        Assert.Equal(1, loaded);
        Assert.Single(viewLog.Secs1Entries);
        Assert.Equal((byte)1, viewLog.Secs1Entries[0].SxFy == "S1F1" ? (byte)1 : (byte)0);
    }

    [Fact]
    public async Task RecorderAllowsACompletedObservationSourceToDrainBeforeClassifyingEarlyStop()
    {
        var source = new FakeObservationSource();
        await using var recorder = new WireLogRecorder(
            source,
            Identity(),
            new WireLogRecorderOptions { ShutdownTimeout = TimeSpan.FromSeconds(1) },
            new CollectingSink());
        source.Publish(Observe(EncodeData(1, 1, null)));
        source.Complete();

        await recorder.DisposeAsync();

        Assert.True(recorder.Health.IsEvidenceEligible);
        Assert.Null(recorder.Health.WriterFailure);
    }

    [Fact]
    public async Task RecorderPersistsTypedDiagnosticsAndStateTransitionsInTheSameBoundedStream()
    {
        var source = new FakeObservationSource();
        var sink = new CollectingSink();
        await using var recorder = new WireLogRecorder(
            source,
            Identity(),
            new WireLogRecorderOptions(),
            sink);
        var connectionIdentity = new SecsConnectionIdentity(
            "provider",
            Guid.NewGuid(),
            3,
            new SecsSessionId(7),
            SecsRole.Host,
            SecsConnectionMode.Active);

        source.Publish(Observe(EncodeData(1, 1, null)));
        source.Complete();
        await source.ReadAll.WaitAsync(TimeSpan.FromSeconds(5));

        // A session can finish its exact-wire source before its terminal diagnostic/state callbacks return.
        // Those already-owned callbacks must remain recordable until the facade unsubscribes and disposes us.
        Assert.True(recorder.TryRecordDiagnostic(new SecsDiagnosticEvent(
            SecsDiagnosticKind.Timeout,
            "T3 expired.",
            HsmsConnectionState.Selected)));
        Assert.True(recorder.TryRecordState(new SecsSessionStateChangedEventArgs(
            ConnectionState.Connecting,
            ConnectionState.Connected,
            HsmsConnectionState.NotConnected,
            HsmsConnectionState.ConnectedNotSelected,
            connectionIdentity)));
        await recorder.DisposeAsync();

        Assert.Equal(3, sink.Records.Count);
        Assert.Equal([1L, 2L, 3L], sink.Records.Select(static record => record.Sequence));
        Assert.Contains(sink.Records, record => record.Kind == WireLogRecordKind.Diagnostic &&
            record.DiagnosticKind == SecsDiagnosticKind.Timeout && record.Error is not null);
        Assert.Contains(sink.Records, record => record.Kind == WireLogRecordKind.StateTransition &&
            record.CurrentConnectionState == ConnectionState.Connected &&
            record.CurrentHsmsState == HsmsConnectionState.ConnectedNotSelected);
        Assert.Contains(sink.Records, record => record.Kind == WireLogRecordKind.Frame &&
            record.SourceSequence == 1);
    }

    [Fact]
    public void WorkbenchWireLogManagerResolvesProfilePoliciesBeforeCoreCapture()
    {
        using var directory = new TemporaryDirectory();
        var viewLog = new InteropLogManager();
        var manager = new WireLogManager(viewLog, directory.Path);

        Assert.Equal(
            HsmsWireCaptureMode.HeaderOnly,
            manager.CreateObservationOptions(ConnectionLogPolicyIds.HeaderOnlyV1).DefaultCaptureMode);
        Assert.Equal(
            HsmsWireCaptureMode.Excluded,
            manager.CreateObservationOptions(ConnectionLogPolicyIds.ExcludedV1).DefaultCaptureMode);
        Assert.Throws<InvalidOperationException>(() =>
            manager.CreateObservationOptions(ConnectionLogPolicyIds.FullBodyExplicitV1));
        Assert.Throws<ArgumentException>(() => manager.CreateObservationOptions("unknown-policy"));
    }

    [Fact]
    public async Task IncrementalReaderYieldsBeforeAFollowingMalformedRecord()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "incremental.jsonl");
        var valid = JsonSerializer.Serialize(Record());
        await File.WriteAllTextAsync(path, valid + Environment.NewLine + "{ malformed" + Environment.NewLine);
        await using var enumerator = WireLogReader.ReadAsync(path).GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(1, enumerator.Current.Sequence);
        await Assert.ThrowsAsync<JsonException>(async () => await enumerator.MoveNextAsync().AsTask());
    }

    [Fact]
    public async Task IncrementalReaderRejectsOversizedRecordAndSupportsFiltering()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "filter.jsonl");
        var records = new[]
        {
            Record() with { Sequence = 1, Stream = 1, Function = 1, TimestampUtc = DateTimeOffset.UnixEpoch },
            Record() with { Sequence = 2, Stream = 6, Function = 11, TimestampUtc = DateTimeOffset.UnixEpoch.AddMinutes(1) },
            Record() with { Sequence = 3, Stream = 6, Function = 11, TimestampUtc = DateTimeOffset.UnixEpoch.AddMinutes(2) }
        };
        await File.WriteAllLinesAsync(path, records.Select(record => JsonSerializer.Serialize(record)));
        var selected = new List<WireLogRecord>();
        await foreach (var record in WireLogReader.ReadAsync(path, new WireLogFilter(
            Stream: 6,
            FromUtc: DateTimeOffset.UnixEpoch.AddSeconds(30),
            ToUtc: DateTimeOffset.UnixEpoch.AddMinutes(1)))) selected.Add(record);
        Assert.Single(selected);
        Assert.Equal(2, selected[0].Sequence);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in WireLogReader.ReadAsync(path, new WireLogFilter(
                FromUtc: DateTimeOffset.UnixEpoch.AddMinutes(2),
                ToUtc: DateTimeOffset.UnixEpoch))) { }
        });

        await File.WriteAllTextAsync(path, new string('X', 2048));
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await foreach (var _ in WireLogReader.ReadAsync(path, maximumRecordBytes: 1024)) { }
        });
    }

    [Fact]
    public void EvidenceRequiresDualArtifactsManualVerificationAndHealthyWireLog()
    {
        var hash = new string('A', 64);
        var baseline = new InteropEvidenceManifest(
            InteropEvidenceManifest.CurrentSchemaVersion,
            "RUN-1",
            "operator",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            "tool",
            "1.0",
            hash,
            EvidenceReviewState.EvidenceRecorded,
            new WireLogHealth(0, 0, 10, true, null),
            [new EvidenceArtifact("Dreamine", EvidenceArtifactKind.DreamineLog, hash)],
            [new EvidenceChecklistItem("connect", "Connection verified", true)]);

        Assert.False(baseline.EvaluateExternalEligibility().EligibleForExternalPassReview);

        var verified = baseline with
        {
            ReviewState = EvidenceReviewState.Verified,
            Artifacts =
            [
                new EvidenceArtifact("Dreamine", EvidenceArtifactKind.DreamineLog, hash),
                new EvidenceArtifact("Counterpart", EvidenceArtifactKind.CounterpartLog, hash)
            ]
        };
        Assert.True(verified.EvaluateExternalEligibility().EligibleForExternalPassReview);
        Assert.False((verified with { WireLogHealth = verified.WireLogHealth with { RecorderDropped = 1 } })
            .EvaluateExternalEligibility().EligibleForExternalPassReview);
    }

    [Fact]
    public void CorruptEvidenceCollectionsAndHealthAreRejectedWithoutThrowing()
    {
        var manifest = new InteropEvidenceManifest(
            InteropEvidenceManifest.CurrentSchemaVersion,
            "RUN-CORRUPT",
            "operator",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            "tool",
            "1.0",
            new string('A', 64),
            EvidenceReviewState.Verified,
            null!,
            null!,
            null!);

        var eligibility = manifest.EvaluateExternalEligibility();

        Assert.False(eligibility.EligibleForExternalPassReview);
        Assert.Contains(eligibility.Reasons, reason => reason.Contains("health", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(eligibility.Reasons, reason => reason.Contains("artifact", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(eligibility.Reasons, reason => reason.Contains("checklist", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvidenceManifestStoreRoundTripsAtomicallyAndRejectsUnknownVersions()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "evidence.json");
        var hash = new string('B', 64);
        var manifest = new InteropEvidenceManifest(
            InteropEvidenceManifest.CurrentSchemaVersion,
            "RUN-STORE",
            "operator",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            "tool",
            "1.0",
            hash,
            EvidenceReviewState.EvidenceRecorded,
            new WireLogHealth(0, 0, 1, true, null),
            [new EvidenceArtifact("Dreamine", EvidenceArtifactKind.DreamineLog, hash)],
            [new EvidenceChecklistItem("connect", "Connection verified", true)]);
        var store = new EvidenceManifestStore();

        await store.SaveAsync(path, manifest);
        var loaded = await store.LoadAsync(path);

        Assert.Equal(manifest.SchemaVersion, loaded.SchemaVersion);
        Assert.Equal(manifest.RunId, loaded.RunId);
        Assert.Equal(manifest.WireLogHealth, loaded.WireLogHealth);
        Assert.Equal(manifest.Artifacts.ToArray(), loaded.Artifacts.ToArray());
        Assert.Equal(manifest.Checklist.ToArray(), loaded.Checklist.ToArray());
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.tmp"));

        await File.WriteAllTextAsync(path,
            "{\"schema\":\"dreamine.interop-evidence\",\"version\":2,\"manifest\":{}}");
        await Assert.ThrowsAsync<JsonSchemaVersionException>(() => store.LoadAsync(path));
    }

    [Fact]
    public void PrivacyExportDoesNotRetainEndpointPathRawOrDecodedPayload()
    {
        var source = Record() with
        {
            Endpoint = "10.20.30.40:5000",
            EquipmentId = "private-equipment",
            ConnectionId = "C:\\secret\\connection",
            HeaderBytes = [1, 2, 3],
            BodyBytes = [4, 5, 6],
            DecodedItem = "PRIVATE-BODY",
            DecodeError = "C:\\secret\\decode",
            TransactionStatus = "host.internal",
            Error = "10.20.30.40"
        };

        var safe = EvidencePrivacySanitizer.Sanitize(source);
        var json = JsonSerializer.Serialize(safe);

        Assert.DoesNotContain("10.20.30.40", json, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PRIVATE-BODY", json, StringComparison.Ordinal);
        Assert.DoesNotContain("private-equipment", json, StringComparison.Ordinal);
        Assert.DoesNotContain(Convert.ToBase64String(source.BodyBytes!), json, StringComparison.Ordinal);
    }

    private static byte[] EncodeData(byte stream, byte function, SecsItem? item) => new HsmsFrameCodec().Encode(
        new HsmsDataMessage(new SecsMessage(new SecsSessionId(7), new SecsStream(stream),
            new SecsFunction(function), false, new SecsSystemBytes(1), item)));

    private static HsmsWireObservation Observe(byte[] frame, long sequence = 1) => new(
        sequence,
        1,
        DateTimeOffset.UnixEpoch,
        HsmsWireDirection.Inbound,
        frame.Length,
        frame.Length - 4,
        frame);

    private static WireLogIdentity Identity() => new("EQ-1", "CONNECTION-1", "127.0.0.1:5000", 7);

    private static WireLogRecord Record() => new(
        WireLogRecord.CurrentSchemaVersion,
        1,
        1,
        DateTimeOffset.UnixEpoch,
        HsmsWireDirection.Inbound,
        "EQ-1",
        "CONNECTION-1",
        "127.0.0.1:5000",
        7,
        14,
        10,
        7,
        1,
        1,
        false,
        0,
        0,
        1,
        WireBodyCaptureMode.HeaderOnly,
        0,
        false,
        new byte[14],
        null,
        null,
        null);

    private sealed class CountingDecoder : IWireBodyDecoder
    {
        public int CallCount { get; private set; }
        public string? Decode(ReadOnlyMemory<byte> completeFrame, int maximumCharacters)
        {
            CallCount++;
            return "decoded";
        }
    }

    private sealed class FakeObservationSource : IHsmsWireObservationSource
    {
        private readonly Channel<HsmsWireObservation> _observations = Channel.CreateUnbounded<HsmsWireObservation>();
        private readonly TaskCompletionSource _readAll = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsWireObservationEnabled => true;
        public long DroppedWireObservationCount => 0;
        public Task ReadAll => _readAll.Task;
        public void Publish(HsmsWireObservation observation) => Assert.True(_observations.Writer.TryWrite(observation));
        public void Complete() => _observations.Writer.TryComplete();
        public async IAsyncEnumerable<HsmsWireObservation> ReadWireObservationsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                await foreach (var observation in _observations.Reader.ReadAllAsync(cancellationToken)) yield return observation;
            }
            finally { _readAll.TrySetResult(); }
        }
    }

    private sealed class BlockingSink : IWireLogRecordSink
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _calls;
        public Task Entered => _entered.Task;
        public void Release() => _release.TrySetResult();
        public async ValueTask AppendAsync(WireLogRecord record, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _calls) == 1)
            {
                _entered.TrySetResult();
                await _release.Task.WaitAsync(cancellationToken);
            }
        }
        public ValueTask CompleteAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class ThrowingSink : IWireLogRecordSink
    {
        public ValueTask AppendAsync(WireLogRecord record, CancellationToken cancellationToken) =>
            ValueTask.FromException(new IOException("Injected writer failure."));
        public ValueTask CompleteAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class NonCooperativeSink : IWireLogRecordSink
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Entered => _entered.Task;
        public Task Exited => _exited.Task;
        public void Release() => _release.TrySetResult();
        public async ValueTask AppendAsync(WireLogRecord record, CancellationToken cancellationToken)
        {
            _entered.TrySetResult();
            await _release.Task;
            _exited.TrySetResult();
        }
        public ValueTask CompleteAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class CollectingSink : IWireLogRecordSink
    {
        public List<WireLogRecord> Records { get; } = [];
        public ValueTask AppendAsync(WireLogRecord record, CancellationToken cancellationToken)
        {
            Records.Add(record);
            return ValueTask.CompletedTask;
        }
        public ValueTask CompleteAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dreamine-wire-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }
        internal string Path { get; }
        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }
}
