using GamePlatform.Domain;

namespace GamePlatform.Application;

public sealed record TerritoryActionResult(bool Success, string Message, TerritoryState State);

public sealed partial class GameSession
{
    public async Task<TerritoryState> LoadTerritoryAsync(CancellationToken cancellationToken = default)
    {
        if (_settingsStore is not null && _store is ITerritoryWorldStore)
            await SharedWorldActionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var loaded = await _store.MutateAsync(RequireUserId(), value =>
            {
                TerritoryRules.Settle(value.Territory, _timeProvider.GetUtcNow().UtcDateTime);
                return (State: TerritoryRules.Clone(value.Territory), Coins: value.PremiumCurrency);
            }, cancellationToken).ConfigureAwait(false);
            Snapshot = Snapshot with { PremiumCurrency = loaded.Coins };
            return loaded.State;
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<TerritoryCommander>> LoadTerritoryCommandersAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await _store.MutateAsync(RequireUserId(), value => TerritoryRules.Commanders(value), cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task<TerritoryActionResult> TerritoryActionAsync(string action, int target = 0, int count = 1, CancellationToken cancellationToken = default,
        TerritoryFormation? formation = null, string? expectedJobId = null)
    {
        if (_settingsStore is not null && _store is ITerritoryWorldStore)
            await SharedWorldActionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var applied = await _store.MutateAsync(RequireUserId(), value =>
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var result = TerritoryRules.Execute(value.Territory, action, target, count, now, value, formation, expectedJobId);
                value.UpdatedUtc = now;
                return (Result: new TerritoryActionResult(result.Success, result.Message, TerritoryRules.Clone(value.Territory)), Coins: value.PremiumCurrency);
            }, cancellationToken).ConfigureAwait(false);
            Snapshot = Snapshot with { PremiumCurrency = applied.Coins };
            return applied.Result;
        }
        finally { _gate.Release(); }
    }
}
