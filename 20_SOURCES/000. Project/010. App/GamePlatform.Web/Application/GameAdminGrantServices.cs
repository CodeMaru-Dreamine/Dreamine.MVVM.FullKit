using GamePlatform.Domain;

namespace GamePlatform.Application;

public enum GameAdminGrantTarget { Individual, AllPlayers }
public enum GameAdminRewardKind { PremiumCurrency, Gold, HeroSummonTickets, AllCurrencies }

public sealed record GameAdminGrantRequest(
    GameAdminGrantTarget Target,
    string? UserId,
    GameAdminRewardKind RewardKind,
    long Amount,
    string Note);

public sealed record GameAdminGrantRecord(
    string GrantId,
    string Administrator,
    GameAdminGrantTarget Target,
    string? TargetUserId,
    GameAdminRewardKind RewardKind,
    long Amount,
    int RecipientCount,
    int FailureCount,
    string Note,
    DateTime CreatedUtc);

public sealed record GameAdminGrantResult(
    bool Accepted,
    int RecipientCount,
    int FailureCount,
    string Message,
    GameAdminGrantRecord? Record);

public interface IGameAdminGrantAuditStore
{
    Task AppendAsync(GameAdminGrantRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameAdminGrantRecord>> LoadRecentAsync(int count = 50, CancellationToken cancellationToken = default);
}

public interface IGameAdminGrantService
{
    Task<GameAdminGrantResult> GrantAsync(
        GameAdminGrantRequest request,
        string administrator,
        CancellationToken cancellationToken = default);

    Task<GameAdminGrantResult> ResetBalanceAsync(
        string userId,
        GameAdminRewardKind rewardKind,
        string note,
        string administrator,
        CancellationToken cancellationToken = default);

    Task<GameAdminGrantResult> ReclaimBalanceAsync(
        string userId,
        GameAdminRewardKind rewardKind,
        long amount,
        string note,
        string administrator,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GameAdminGrantRecord>> LoadRecentAsync(
        int count = 50,
        CancellationToken cancellationToken = default);
}

public sealed class GameAdminGrantService(
    IGameProgressStore progressStore,
    IGameAdminGrantAuditStore auditStore,
    TimeProvider timeProvider,
    ILogger<GameAdminGrantService> logger) : IGameAdminGrantService
{
    private const long MaximumGrantAmount = 1_000_000_000_000;

    public async Task<GameAdminGrantResult> GrantAsync(
        GameAdminGrantRequest request,
        string administrator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(administrator))
            return Rejected("관리자 계정을 확인할 수 없습니다.");
        if (request.Amount is < 1 or > MaximumGrantAmount)
            return Rejected($"지급 수량은 1~{MaximumGrantAmount:N0} 범위여야 합니다.");
        if (!Enum.IsDefined(request.RewardKind))
            return Rejected("지원하지 않는 지급 품목입니다.");
        if (request.RewardKind == GameAdminRewardKind.AllCurrencies)
            return Rejected("전체 재화는 지급할 수 없습니다. 초기화 기능에서만 선택해 주세요.");

        var players = await progressStore.ListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<GameProgress> recipients;
        if (request.Target == GameAdminGrantTarget.AllPlayers)
        {
            recipients = players;
        }
        else
        {
            var userId = request.UserId?.Trim();
            var player = players.FirstOrDefault(item =>
                string.Equals(item.UserId, userId, StringComparison.OrdinalIgnoreCase));
            if (player is null) return Rejected("지급할 게임 계정을 찾지 못했습니다.");
            recipients = [player];
        }

        if (recipients.Count == 0) return Rejected("지급 대상 게임 계정이 없습니다.");

        var succeeded = 0;
        var failed = 0;
        foreach (var recipient in recipients)
        {
            try
            {
                await progressStore.MutateAsync(
                    recipient.UserId,
                    progress =>
                    {
                        Apply(progress, request.RewardKind, request.Amount);
                        return true;
                    },
                    cancellationToken).ConfigureAwait(false);
                succeeded++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                failed++;
                logger.LogError(exception, "Admin grant failed for player {UserId}.", recipient.UserId);
            }
        }

        var record = new GameAdminGrantRecord(
            Guid.NewGuid().ToString("N"),
            administrator.Trim(),
            request.Target,
            request.Target == GameAdminGrantTarget.Individual ? recipients[0].UserId : null,
            request.RewardKind,
            request.Amount,
            succeeded,
            failed,
            NormalizeNote(request.Note),
            timeProvider.GetUtcNow().UtcDateTime);
        await auditStore.AppendAsync(record, cancellationToken).ConfigureAwait(false);

        var targetText = request.Target == GameAdminGrantTarget.AllPlayers ? "전체 계정" : recipients[0].UserId;
        var message = failed == 0
            ? $"{targetText}에 {RewardLabel(request.RewardKind)} {request.Amount:N0} 지급을 완료했습니다."
            : $"{succeeded:N0}개 계정 지급 완료, {failed:N0}개 계정 실패. 감사 기록을 확인해 주세요.";
        return new(succeeded > 0, succeeded, failed, message, record);
    }

    public async Task<GameAdminGrantResult> ResetBalanceAsync(
        string userId,
        GameAdminRewardKind rewardKind,
        string note,
        string administrator,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(administrator)) return Rejected("관리자 계정을 확인할 수 없습니다.");
        if (!Enum.IsDefined(rewardKind)) return Rejected("지원하지 않는 초기화 항목입니다.");
        var normalizedUserId = userId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedUserId)) return Rejected("초기화할 게임 계정을 선택해 주세요.");

        var players = await progressStore.ListAsync(cancellationToken).ConfigureAwait(false);
        var player = players.FirstOrDefault(item => string.Equals(item.UserId, normalizedUserId, StringComparison.OrdinalIgnoreCase));
        if (player is null) return Rejected("초기화할 게임 계정을 찾지 못했습니다.");

        try
        {
            await progressStore.MutateAsync(
                player.UserId,
                progress =>
                {
                    Reset(progress, rewardKind);
                    return true;
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Admin balance reset failed for player {UserId}.", player.UserId);
            return Rejected("재화 초기화 중 오류가 발생했습니다.");
        }

        var record = new GameAdminGrantRecord(
            Guid.NewGuid().ToString("N"), administrator.Trim(), GameAdminGrantTarget.Individual,
            player.UserId, rewardKind, 0, 1, 0, NormalizeNote(note), timeProvider.GetUtcNow().UtcDateTime);
        await auditStore.AppendAsync(record, cancellationToken).ConfigureAwait(false);
        return new(true, 1, 0, $"{player.UserId}의 {RewardLabel(rewardKind)} 잔액을 0으로 초기화했습니다.", record);
    }

    public async Task<GameAdminGrantResult> ReclaimBalanceAsync(
        string userId,
        GameAdminRewardKind rewardKind,
        long amount,
        string note,
        string administrator,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(administrator)) return Rejected("관리자 계정을 확인할 수 없습니다.");
        if (!Enum.IsDefined(rewardKind) || rewardKind == GameAdminRewardKind.AllCurrencies)
            return Rejected("회수할 재화 한 종류를 선택해 주세요.");
        if (amount is < 1 or > MaximumGrantAmount)
            return Rejected($"회수 수량은 1~{MaximumGrantAmount:N0} 범위여야 합니다.");
        var normalizedUserId = userId?.Trim();
        var players = await progressStore.ListAsync(cancellationToken).ConfigureAwait(false);
        var player = players.FirstOrDefault(item => string.Equals(item.UserId, normalizedUserId, StringComparison.OrdinalIgnoreCase));
        if (player is null) return Rejected("회수할 게임 계정을 찾지 못했습니다.");

        var reclaimed = 0L;
        await progressStore.MutateAsync(
            player.UserId,
            progress =>
            {
                var current = CurrentBalance(progress, rewardKind);
                reclaimed = Math.Min(current, amount);
                SetBalance(progress, rewardKind, current - reclaimed);
                return true;
            }, cancellationToken).ConfigureAwait(false);
        if (reclaimed == 0) return Rejected($"{player.UserId}에 회수할 {RewardLabel(rewardKind)} 잔액이 없습니다.");

        var record = new GameAdminGrantRecord(
            Guid.NewGuid().ToString("N"), administrator.Trim(), GameAdminGrantTarget.Individual,
            player.UserId, rewardKind, -reclaimed, 1, 0, NormalizeNote(note), timeProvider.GetUtcNow().UtcDateTime);
        await auditStore.AppendAsync(record, cancellationToken).ConfigureAwait(false);
        return new(true, 1, 0, $"{player.UserId}에서 {RewardLabel(rewardKind)} {reclaimed:N0}을 회수했습니다.", record);
    }

    public Task<IReadOnlyList<GameAdminGrantRecord>> LoadRecentAsync(
        int count = 50,
        CancellationToken cancellationToken = default) =>
        auditStore.LoadRecentAsync(Math.Clamp(count, 1, 200), cancellationToken);

    private static void Apply(GameProgress progress, GameAdminRewardKind kind, long amount)
    {
        switch (kind)
        {
            case GameAdminRewardKind.PremiumCurrency:
                progress.PremiumCurrency = GameRules.SaturatingAdd(progress.PremiumCurrency, amount);
                break;
            case GameAdminRewardKind.Gold:
                progress.Gold = GameRules.SaturatingAdd(progress.Gold, amount);
                break;
            case GameAdminRewardKind.HeroSummonTickets:
                progress.HeroSummonTickets = GameRules.SaturatingAdd(progress.HeroSummonTickets, amount);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static void Reset(GameProgress progress, GameAdminRewardKind kind)
    {
        if (kind is GameAdminRewardKind.PremiumCurrency or GameAdminRewardKind.AllCurrencies) progress.PremiumCurrency = 0;
        if (kind is GameAdminRewardKind.Gold or GameAdminRewardKind.AllCurrencies) progress.Gold = 0;
        if (kind is GameAdminRewardKind.HeroSummonTickets or GameAdminRewardKind.AllCurrencies) progress.HeroSummonTickets = 0;
    }

    private static long CurrentBalance(GameProgress progress, GameAdminRewardKind kind) => kind switch
    {
        GameAdminRewardKind.PremiumCurrency => progress.PremiumCurrency,
        GameAdminRewardKind.Gold => progress.Gold,
        GameAdminRewardKind.HeroSummonTickets => progress.HeroSummonTickets,
        _ => 0
    };

    private static void SetBalance(GameProgress progress, GameAdminRewardKind kind, long value)
    {
        value = Math.Max(0, value);
        if (kind == GameAdminRewardKind.PremiumCurrency) progress.PremiumCurrency = value;
        else if (kind == GameAdminRewardKind.Gold) progress.Gold = value;
        else if (kind == GameAdminRewardKind.HeroSummonTickets) progress.HeroSummonTickets = value;
    }

    private static string NormalizeNote(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length <= 200 ? normalized : normalized[..200];
    }

    private static string RewardLabel(GameAdminRewardKind kind) => kind switch
    {
        GameAdminRewardKind.PremiumCurrency => "CodeMaru C",
        GameAdminRewardKind.Gold => "금화",
        GameAdminRewardKind.HeroSummonTickets => "소환패",
        GameAdminRewardKind.AllCurrencies => "전체 재화",
        _ => "보상"
    };

    private static GameAdminGrantResult Rejected(string message) => new(false, 0, 0, message, null);
}
