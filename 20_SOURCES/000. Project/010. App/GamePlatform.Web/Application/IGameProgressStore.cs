using GamePlatform.Domain;

namespace GamePlatform.Application;

/// <summary>사용자 게임 진행 데이터를 원자적으로 읽고 변경하는 저장 경계입니다.</summary>
public interface IGameProgressStore
{
    /// <summary>사용자의 진행 데이터를 읽고 없으면 Stage 1 데이터로 만듭니다.</summary>
    Task<GameLoadResult> LoadOrCreateAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>실제 서버 순위 집계를 위해 저장된 모든 계정 진행 요약을 읽습니다.</summary>
    Task<IReadOnlyList<GameProgress>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>최신 사용자 데이터를 저장소 잠금 안에서 변경하고 한 번에 저장합니다.</summary>
    Task<TResult> MutateAsync<TResult>(
        string userId,
        Func<GameProgress, TResult> mutation,
        CancellationToken cancellationToken = default);
}

/// <summary>게임 진행 데이터의 최초 로드 결과입니다.</summary>
public sealed record GameLoadResult(GameProgress Progress, bool IsNewPlayer);

/// <summary>서버 데미지 난수의 교체 가능한 경계입니다.</summary>
public interface IAttackRollSource
{
    /// <summary>0 이상 100 미만의 서버 롤을 반환합니다.</summary>
    int NextRoll();
}
