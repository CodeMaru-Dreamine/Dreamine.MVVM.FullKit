using Dreamine.Database.Abstractions;
using Dreamine.Database.Abstractions.Mapping;
using Dreamine.Database.Sqlite;
using GamePlatform.Application;
using GamePlatform.Options;

namespace GamePlatform.Infrastructure;

public sealed class GameAdminGrantAuditStore : IGameAdminGrantAuditStore
{
    private readonly IDatabaseQueryProvider _queries;
    private readonly IDatabaseRepository _repository;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public GameAdminGrantAuditStore(GameOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var provider = new SqliteDatabaseProvider($"Data Source={options.DatabasePath}");
        provider.EnsureDatabaseExists();
        provider.CreateTable<GameAdminGrantRow>();
        _queries = provider;
        _repository = provider;
    }

    public async Task AppendAsync(GameAdminGrantRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { await _repository.InsertAsync(GameAdminGrantRow.FromDomain(record), cancellationToken).ConfigureAwait(false); }
        finally { _writeLock.Release(); }
    }

    public async Task<IReadOnlyList<GameAdminGrantRecord>> LoadRecentAsync(
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        var rows = await _queries.QueryAsync<GameAdminGrantRow>(
            """
            SELECT GrantId, Administrator, Target, TargetUserId, RewardKind, Amount,
                   RecipientCount, FailureCount, Note, CreatedUtc
            FROM MaruIdleAdminGrants
            ORDER BY CreatedUtc DESC
            LIMIT @Count
            """,
            new { Count = Math.Clamp(count, 1, 200) },
            cancellationToken).ConfigureAwait(false);
        return rows.Select(row => row.ToDomain()).ToArray();
    }
}

[DatabaseTable("MaruIdleAdminGrants")]
internal sealed class GameAdminGrantRow
{
    [DatabaseKey]
    public string GrantId { get; set; } = string.Empty;
    public string Administrator { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string? TargetUserId { get; set; }
    public string RewardKind { get; set; } = string.Empty;
    public long Amount { get; set; }
    public int RecipientCount { get; set; }
    public int FailureCount { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }

    public GameAdminGrantRecord ToDomain() => new(
        GrantId,
        Administrator,
        Enum.TryParse<GameAdminGrantTarget>(Target, out var target) ? target : GameAdminGrantTarget.Individual,
        TargetUserId,
        Enum.TryParse<GameAdminRewardKind>(RewardKind, out var rewardKind) ? rewardKind : GameAdminRewardKind.Gold,
        Amount,
        RecipientCount,
        FailureCount,
        Note,
        CreatedUtc);

    public static GameAdminGrantRow FromDomain(GameAdminGrantRecord value) => new()
    {
        GrantId = value.GrantId,
        Administrator = value.Administrator,
        Target = value.Target.ToString(),
        TargetUserId = value.TargetUserId,
        RewardKind = value.RewardKind.ToString(),
        Amount = value.Amount,
        RecipientCount = value.RecipientCount,
        FailureCount = value.FailureCount,
        Note = value.Note,
        CreatedUtc = value.CreatedUtc
    };
}
