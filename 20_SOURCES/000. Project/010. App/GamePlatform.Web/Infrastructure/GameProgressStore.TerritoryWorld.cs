using System.Text.Json;
using Dapper;
using GamePlatform.Application;
using GamePlatform.Domain;
using Microsoft.Data.Sqlite;
namespace GamePlatform.Infrastructure;

public sealed partial class GameProgressStore
{
    // The same write gate as all solo mutations, and one SQLite transaction for
    // the shared world, troop reservations, defender damage, rewards and coins.
    public async Task<T> MutateWorldAsync<T>(string channel, string userId, Func<TerritorySharedWorld, IDictionary<string, GameProgress>, T> mutation, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();
            await connection.ExecuteAsync(new CommandDefinition("CREATE TABLE IF NOT EXISTS MaruTerritoryWorlds (Channel TEXT PRIMARY KEY, StateJson TEXT NOT NULL)", transaction: transaction, cancellationToken: cancellationToken));
            var json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition("SELECT StateJson FROM MaruTerritoryWorlds WHERE Channel=@Channel", new { Channel = channel }, transaction, cancellationToken: cancellationToken));
            var world = json is null ? new TerritorySharedWorld() : JsonSerializer.Deserialize<TerritorySharedWorld>(json) ?? throw new InvalidOperationException("월드 저장 데이터가 올바르지 않습니다.");
            var ids = world.Members.Keys.Append(userId).Distinct().ToArray();
            var rows = await connection.QueryAsync<GamePlayerRow>(new CommandDefinition("SELECT * FROM MaruIdlePlayers WHERE UserId IN @Ids", new { Ids = ids }, transaction, cancellationToken: cancellationToken));
            var players = rows.Select(r => r.ToDomain()).ToDictionary(p => p.UserId);
            if (!players.ContainsKey(userId)) throw new InvalidOperationException("게임 계정을 먼저 불러오세요.");
            foreach (var p in players.Values) Normalize(p);
            var result = mutation(world, players);
            foreach (var p in players.Values)
            {
                await connection.ExecuteAsync(new CommandDefinition("UPDATE MaruIdlePlayers SET TerritoryStateJson=@Territory, PremiumCurrency=@Coins WHERE UserId=@UserId", new { Territory = JsonSerializer.Serialize(p.Territory), Coins = p.PremiumCurrency, p.UserId }, transaction, cancellationToken: cancellationToken));
            }
            await connection.ExecuteAsync(new CommandDefinition("INSERT INTO MaruTerritoryWorlds (Channel,StateJson) VALUES (@Channel,@Json) ON CONFLICT(Channel) DO UPDATE SET StateJson=excluded.StateJson", new { Channel = channel, Json = JsonSerializer.Serialize(world) }, transaction, cancellationToken: cancellationToken));
            transaction.Commit();
            return result;
        }
        finally { _writeLock.Release(); }
    }
}
