using System.Text.Json;
using GamePlatform.Domain;
namespace GamePlatform.Application;

public sealed partial class GameSession
{
    public async Task<TerritorySharedResult> SharedWorldActionAsync(string action = "load", string? target = null, int option = 0, TerritoryFormation? formation = null, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var id = RequireUserId();
            var settings = _settingsStore is null ? null : await _settingsStore.FindAsync(id, cancellationToken);
            if (settings?.General.ChannelConfirmed != true) throw new InvalidOperationException("서버 선택을 먼저 완료하세요.");
            if (_store is not ITerritoryWorldStore worlds) throw new InvalidOperationException("공유 월드 저장소를 사용할 수 없습니다.");
            var channel = settings.General.ChannelKey.ToLowerInvariant();
            var result = await worlds.MutateWorldAsync(channel, id, (world, players) =>
            {
                var applied = TerritorySharedRules.Execute(world, players, id, action, target, option, formation, _timeProvider.GetUtcNow().UtcDateTime);
                return new TerritorySharedResult(applied.Success, applied.Message, channel, id,
                    JsonSerializer.Deserialize<TerritorySharedWorld>(JsonSerializer.Serialize(world))!, TerritoryRules.Clone(players[id].Territory), players[id].PremiumCurrency);
            }, cancellationToken);
            Snapshot = Snapshot with { PremiumCurrency = result.Coins };
            return result;
        }
        finally { _gate.Release(); }
    }
}
