namespace GamePlatform.Application;

/// <summary>중복 타이머 없이 자동공격 콜백을 실행하고 취소하는 작업입니다.</summary>
public sealed class AutoAttackLoop : IAsyncDisposable
{
    private static readonly AsyncLocal<AutoAttackLoop?> ExecutingLoop = new();
    private readonly TimeProvider _timeProvider;
    private readonly object _sync = new();
    private CancellationTokenSource? _cancellation;
    private Task? _loopTask;
    private TimeSpan _interval;

    /// <summary>지정한 시간 공급자로 자동공격 루프를 만듭니다.</summary>
    public AutoAttackLoop(TimeProvider? timeProvider = null) =>
        _timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>현재 자동공격 작업이 실행 중인지 반환합니다.</summary>
    public bool IsRunning
    {
        get { lock (_sync) return _loopTask is { IsCompleted: false }; }
    }

    /// <summary>자동공격 루프를 시작하거나 실행 중인 루프의 공격 간격을 갱신합니다.</summary>
    public void Start(
        Func<CancellationToken, Task> attack,
        TimeSpan interval,
        CancellationToken lifetimeToken = default)
    {
        ArgumentNullException.ThrowIfNull(attack);
        if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));

        lock (_sync)
        {
            _interval = interval;
            // StopAsync가 취소 신호를 보낸 직후 기존 작업이 아직 빠져나오는 중일 수 있습니다.
            // 이때 기존 작업만 보고 실행 중으로 판단하면 재시작 요청이 유실됩니다.
            if (_loopTask is { IsCompleted: false }
                && _cancellation is { IsCancellationRequested: false }) return;
            _cancellation?.Dispose();
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);
            _loopTask = RunAsync(attack, _cancellation.Token);
        }
    }

    /// <summary>실행 중인 자동공격 작업을 취소하고 종료를 기다립니다.</summary>
    public async Task StopAsync()
    {
        Task? loop;
        var calledFromOwnCallback = ReferenceEquals(ExecutingLoop.Value, this);
        lock (_sync)
        {
            _cancellation?.Cancel();
            loop = _loopTask;
        }

        // 공격 콜백 안에서 자기 루프의 완료를 기다리면 영원히 끝나지 않습니다.
        // 이 경로에서는 취소 신호만 보내고 RunAsync가 콜백 반환 뒤 종료하게 둡니다.
        if (loop is not null && !calledFromOwnCallback)
        {
            try { await loop.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }
    }

    private async Task RunAsync(
        Func<CancellationToken, Task> attack,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TimeSpan interval;
                lock (_sync) interval = _interval;
                await Task.Delay(interval, _timeProvider, cancellationToken).ConfigureAwait(false);
                try
                {
                    var previousLoop = ExecutingLoop.Value;
                    ExecutingLoop.Value = this;
                    try { await attack(cancellationToken).ConfigureAwait(false); }
                    finally { ExecutingLoop.Value = previousLoop; }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception)
                {
                    // 브라우저 연결, 오디오, 전환 연출 등의 일시 오류 한 번이
                    // 계정의 AUTO 설정 자체를 죽이지 않도록 다음 틱에서 복구합니다.
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    /// <summary>자동공격 작업과 취소 리소스를 정리합니다.</summary>
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        lock (_sync)
        {
            _cancellation?.Dispose();
            _cancellation = null;
            _loopTask = null;
        }
    }
}
