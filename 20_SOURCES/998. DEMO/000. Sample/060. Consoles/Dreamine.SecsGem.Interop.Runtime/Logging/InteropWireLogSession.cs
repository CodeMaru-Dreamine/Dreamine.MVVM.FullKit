using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.SecsGem.Interop.Runtime.Profiles;

namespace Dreamine.SecsGem.Interop.Runtime.Logging;

/// <summary>
/// \if KO 공개 샘플과 headless 도구에서 사용할 제한 용량 wire-log 설정입니다. \endif
/// \if EN Defines bounded wire-log settings for public samples and headless tools. \endif
/// </summary>
public sealed class InteropWireLogSessionOptions
{
    /// <summary>\if KO 제품이 소유하는 로그 루트로 설정을 만듭니다. \endif \if EN Creates settings for a product-owned log root. \endif</summary>
    public InteropWireLogSessionOptions(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        RootDirectory = rootDirectory;
    }

    /// <summary>\if KO 제품이 소유하는 로그 루트입니다. \endif \if EN Gets the product-owned log root. \endif</summary>
    public string RootDirectory { get; }

    /// <summary>\if KO transport 관찰 큐 용량입니다. \endif \if EN Gets the transport observation queue capacity. \endif</summary>
    public int ObservationQueueCapacity { get; init; } = 512;

    /// <summary>\if KO recorder 큐 용량입니다. \endif \if EN Gets the recorder queue capacity. \endif</summary>
    public int RecorderQueueCapacity { get; init; } = 512;

    /// <summary>\if KO JSONL segment 최대 크기입니다. \endif \if EN Gets the maximum JSONL segment size. \endif</summary>
    public long MaximumSegmentBytes { get; init; } = 32L * 1024 * 1024;

    /// <summary>\if KO 유지할 제품 소유 segment 수입니다. \endif \if EN Gets the retained product-owned segment count. \endif</summary>
    public int RetainedSegments { get; init; } = 20;

    /// <summary>\if KO 제한 종료 시간입니다. \endif \if EN Gets the bounded shutdown timeout. \endif</summary>
    public TimeSpan ShutdownTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>\if KO 기본 제공 로그 정책 ID입니다. \endif \if EN Gets the built-in log policy ID. \endif</summary>
    public string LogPolicyId { get; init; } = ConnectionLogPolicyIds.HeaderOnlyV1;

    /// <summary>
    /// \if KO session 생성 전에 적용할 bounded wire 관찰 옵션을 만듭니다. \endif
    /// \if EN Creates bounded wire-observation options to apply before session construction. \endif
    /// </summary>
    public HsmsWireObservationOptions CreateObservationOptions()
    {
        Validate();
        return CreatePolicy().CreateObservationOptions(ObservationQueueCapacity);
    }

    internal WireLogPolicy CreatePolicy() => LogPolicyId switch
    {
        ConnectionLogPolicyIds.HeaderOnlyV1 => new WireLogPolicy(WireBodyCaptureMode.HeaderOnly),
        ConnectionLogPolicyIds.ExcludedV1 => new WireLogPolicy(WireBodyCaptureMode.Excluded),
        ConnectionLogPolicyIds.FullBodyExplicitV1 => throw new InvalidOperationException(
            "Full-body logging requires explicit per-S/F template rules; the safe sample facade cannot enable it globally."),
        _ => throw new ArgumentException($"Unknown wire-log policy '{LogPolicyId}'.", nameof(LogPolicyId))
    };

    internal void Validate()
    {
        _ = Path.GetFullPath(RootDirectory);
        if (ObservationQueueCapacity is < 1 or > 65_536)
            throw new ArgumentOutOfRangeException(nameof(ObservationQueueCapacity));
        if (RecorderQueueCapacity is < 1 or > 65_536)
            throw new ArgumentOutOfRangeException(nameof(RecorderQueueCapacity));
        if (MaximumSegmentBytes is < 4096 or > 4L * 1024 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(MaximumSegmentBytes));
        if (RetainedSegments is < 1 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(RetainedSegments));
        if (ShutdownTimeout < TimeSpan.FromMilliseconds(100) || ShutdownTimeout > TimeSpan.FromMinutes(1))
            throw new ArgumentOutOfRangeException(nameof(ShutdownTimeout));
        _ = CreatePolicy();
    }
}

/// <summary>\if KO 완료된 wire-log 실행의 건강 상태입니다. \endif \if EN Describes the health of a completed wire-log run. \endif</summary>
public sealed record InteropWireLogRunHealth(
    long SourceDropped,
    long RecorderDropped,
    long Written,
    bool FlushCompleted,
    string? Failure)
{
    /// <summary>\if KO drop·실패가 없고 flush가 끝났는지 나타냅니다. \endif \if EN Gets whether the run completed without drops or failures and flushed successfully. \endif</summary>
    public bool IsEvidenceEligible => SourceDropped == 0 && RecorderDropped == 0 && FlushCompleted && Failure is null;
}

/// <summary>
/// \if KO provider-neutral session의 exact wire·진단·상태를 bounded JSONL로 기록합니다. 호출자는 session을 먼저 종료한 뒤 이 객체를 종료합니다. \endif
/// \if EN Records exact wire, diagnostics, and state from a provider-neutral session to bounded JSONL. The caller stops the session before stopping this object. \endif
/// </summary>
public sealed class InteropWireLogSession : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly ISecsMessageSession _session;
    private readonly WireLogRecorder _recorder;
    private readonly JsonlWireLogSink _sink;
    private Task? _stopTask;
    private InteropWireLogRunHealth _health = new(0, 0, 0, false, "The wire-log run has not stopped.");
    private IReadOnlyList<string> _finalizedSegments = [];

    private InteropWireLogSession(ISecsMessageSession session, InteropWireLogSessionOptions options)
    {
        _session = session;
        options.Validate();
        if (!session.IsWireObservationEnabled)
            throw new InvalidOperationException(
                "Wire observation is disabled. Apply options.CreateObservationOptions() before creating the session.");

        _sink = new JsonlWireLogSink(new WireLogStorageOptions
        {
            RootDirectory = options.RootDirectory,
            MaximumSegmentBytes = options.MaximumSegmentBytes,
            RetainedSegments = options.RetainedSegments
        });
        try
        {
            var identity = session.ConnectionIdentity;
            _recorder = new WireLogRecorder(
                session,
                new WireLogIdentity(
                    identity.Role.ToString(),
                    $"{identity.ProviderKey}:{identity.SessionInstanceId:N}",
                    "redacted",
                    identity.SessionId.Value),
                new WireLogRecorderOptions
                {
                    QueueCapacity = options.RecorderQueueCapacity,
                    ShutdownTimeout = options.ShutdownTimeout,
                    Policy = options.CreatePolicy()
                },
                _sink);
        }
        catch
        {
            _sink.DisposeAsync().AsTask().GetAwaiter().GetResult();
            throw;
        }

        session.DiagnosticReceived += OnDiagnosticReceived;
        session.StateChanged += OnStateChanged;
    }

    /// <summary>\if KO 이미 wire 관찰이 활성화된 session의 기록을 시작합니다. \endif \if EN Starts recording a session whose wire observation is already enabled. \endif</summary>
    public static InteropWireLogSession Start(
        ISecsMessageSession session,
        InteropWireLogSessionOptions options)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(options);
        return new InteropWireLogSession(session, options);
    }

    /// <summary>\if KO 마지막 완료 상태입니다. \endif \if EN Gets the last completed health state. \endif</summary>
    public InteropWireLogRunHealth Health
    {
        get { lock (_gate) return _health; }
    }

    /// <summary>\if KO 완료된 제품 소유 JSONL segment 경로입니다. \endif \if EN Gets finalized product-owned JSONL segment paths. \endif</summary>
    public IReadOnlyList<string> FinalizedSegments
    {
        get { lock (_gate) return _finalizedSegments; }
    }

    /// <summary>\if KO 구독을 해제하고 bounded recorder를 종료합니다. \endif \if EN Unsubscribes and stops the bounded recorder. \endif</summary>
    public Task StopAsync()
    {
        lock (_gate) return _stopTask ??= StopCoreAsync();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => new(StopAsync());

    private async Task StopCoreAsync()
    {
        _session.DiagnosticReceived -= OnDiagnosticReceived;
        _session.StateChanged -= OnStateChanged;
        await _recorder.DisposeAsync().ConfigureAwait(false);
        var health = _recorder.Health;
        lock (_gate)
        {
            _health = new InteropWireLogRunHealth(
                health.SourceDropped,
                health.RecorderDropped,
                health.Written,
                health.FlushCompleted,
                health.WriterFailure);
            _finalizedSegments = _sink.FinalizedSegments.ToArray();
        }
    }

    private void OnDiagnosticReceived(object? sender, SecsDiagnosticEvent diagnostic)
    {
        var epoch = sender is ISecsMessageSession source
            ? source.ConnectionIdentity.ConnectionEpoch
            : _session.ConnectionIdentity.ConnectionEpoch;
        _recorder.TryRecordDiagnostic(diagnostic, epoch);
    }

    private void OnStateChanged(object? sender, SecsSessionStateChangedEventArgs transition) =>
        _recorder.TryRecordState(transition);
}
