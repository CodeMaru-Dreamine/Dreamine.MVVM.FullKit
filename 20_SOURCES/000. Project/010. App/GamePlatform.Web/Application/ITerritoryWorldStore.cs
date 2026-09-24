using GamePlatform.Domain;
namespace GamePlatform.Application;

public interface ITerritoryWorldStore
{
    Task<T> MutateWorldAsync<T>(string channel, string userId, Func<TerritorySharedWorld, IDictionary<string, GameProgress>, T> mutation, CancellationToken cancellationToken = default);
}
public sealed record TerritorySharedResult(bool Success, string Message, string Channel, string UserId, TerritorySharedWorld World, TerritoryState Territory, long Coins);
