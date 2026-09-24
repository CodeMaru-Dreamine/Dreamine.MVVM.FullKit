using System.Collections.Concurrent;
using GamePlatform.Domain;

namespace GamePlatform.Application;

/// <summary>여러 브라우저 회로가 같은 계정의 공격 속도를 중첩하지 못하도록 서버 전체에서 공격 간격을 제한합니다.</summary>
public interface IGameAttackRateLimiter
{
    /// <summary>사용자와 공격 출처에 대해 지정된 최소 간격을 만족하면 이번 공격을 허용합니다.</summary>
    bool TryAcquire(string userId, AttackSource source, TimeSpan minimumInterval);
}

/// <summary>프로세스 내 모든 게임 회로가 공유하는 사용자별 공격 속도 제한기입니다.</summary>
public sealed class GameAttackRateLimiter : IGameAttackRateLimiter
{
    private readonly ConcurrentDictionary<AttackKey, GateState> _gates = new();
    private readonly TimeProvider _timeProvider;

    /// <summary>공격 시간 판정에 사용할 시간 공급자로 제한기를 생성합니다.</summary>
    public GameAttackRateLimiter(TimeProvider? timeProvider = null) =>
        _timeProvider = timeProvider ?? TimeProvider.System;

    /// <inheritdoc />
    public bool TryAcquire(string userId, AttackSource source, TimeSpan minimumInterval)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (minimumInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(minimumInterval));

        var state = _gates.GetOrAdd(new AttackKey(userId, source), static _ => new GateState());
        lock (state.Sync)
        {
            var now = _timeProvider.GetTimestamp();
            if (state.HasTimestamp
                && _timeProvider.GetElapsedTime(state.Timestamp, now) < minimumInterval)
                return false;

            state.Timestamp = now;
            state.HasTimestamp = true;
            return true;
        }
    }

    private readonly record struct AttackKey(string UserId, AttackSource Source);

    private sealed class GateState
    {
        public object Sync { get; } = new();
        public long Timestamp { get; set; }
        public bool HasTimestamp { get; set; }
    }
}
