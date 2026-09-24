using Dreamine.Identity;
using GamePlatform.Domain;

namespace GamePlatform.Application;

/// <summary>실제 서버 저장 데이터를 기반으로 계산한 채널 순위 한 행입니다.</summary>
public sealed record ChannelRankingEntry(
    int Rank,
    string UserId,
    string DisplayName,
    long AttackPower,
    int HighestClearedStage,
    long TotalDefeated);

/// <summary>채널별 원정대 순위를 조회합니다.</summary>
public interface IGameRankingService
{
    Task<IReadOnlyList<ChannelRankingEntry>> GetAsync(
        string channelKey,
        CancellationToken cancellationToken = default);
}

/// <summary>가상 경쟁자를 만들지 않고 실제 저장된 계정만 집계하는 순위 서비스입니다.</summary>
public sealed class GameRankingService(
    IGameProgressStore progressStore,
    IGameSettingsStore settingsStore,
    IUserStore userStore) : IGameRankingService
{
    public async Task<IReadOnlyList<ChannelRankingEntry>> GetAsync(
        string channelKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelKey);
        var progressRows = await progressStore.ListAsync(cancellationToken).ConfigureAwait(false);
        var candidates = new List<(GameProgress Progress, string DisplayName, long AttackPower)>();

        foreach (var progress in progressRows)
        {
            var settings = await settingsStore.FindAsync(progress.UserId, cancellationToken).ConfigureAwait(false);
            if (settings?.General.ChannelConfirmed != true
                || !string.Equals(settings.General.ChannelKey, channelKey, StringComparison.OrdinalIgnoreCase))
                continue;

            var displayName = GameNicknameRules.Normalize(progress.GameNickname);
            if (string.IsNullOrWhiteSpace(displayName)
                && long.TryParse(progress.UserId, out var identityUserId))
            {
                var identityUser = await userStore.GetByIdAsync(identityUserId, cancellationToken).ConfigureAwait(false);
                displayName = identityUser?.DisplayName?.Trim() ?? string.Empty;
            }

            candidates.Add((progress, displayName, CalculateAttackPower(progress)));
        }

        return candidates
            .OrderByDescending(item => item.AttackPower)
            .ThenByDescending(item => item.Progress.HighestClearedStage)
            .ThenByDescending(item => item.Progress.TotalDefeated)
            .ThenBy(item => item.Progress.UserId, StringComparer.Ordinal)
            .Take(100)
            .Select((item, index) => new ChannelRankingEntry(
                index + 1,
                item.Progress.UserId,
                item.DisplayName,
                item.AttackPower,
                item.Progress.HighestClearedStage,
                item.Progress.TotalDefeated))
            .ToArray();
    }

    private static long CalculateAttackPower(GameProgress progress)
    {
        var inheritedAttackPower = GameRules.AttackPower(
            progress.AttackLevel,
            progress.SwordArtLevel,
            progress.WeaponEquipmentLevel);
        var party = PartyRules.Resolve(
            progress.HighestClearedStage,
            progress.ActiveHeroId,
            progress.SelectedCompanionIds,
            inheritedAttackPower,
            progress.OwnedCompanionIds,
            progress);
        var jadeBonus = party.CompanionCount > 0
            ? GameRules.JadeAssistBonusPercent(progress.JadeEquipmentLevel)
            : 0;
        return GameRules.PartyAttackPower(
            party.InheritedAttackPower,
            party.AssistPowerPercent + jadeBonus);
    }
}
