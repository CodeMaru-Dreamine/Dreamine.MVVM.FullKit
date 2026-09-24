using Dreamine.Database.Abstractions;
using Dreamine.Database.Abstractions.Mapping;
using Dreamine.Database.Sqlite;
using GamePlatform.Application;
using GamePlatform.Domain;
using GamePlatform.Options;

namespace GamePlatform.Infrastructure;

public sealed class GameAdminMessageStore : IGameAdminMessageStore
{
    private readonly IDatabaseQueryProvider _queries;
    private readonly IDatabaseCommandExecutor _commands;
    private readonly IDatabaseRepository _repository;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public GameAdminMessageStore(GameOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var provider = new SqliteDatabaseProvider($"Data Source={options.DatabasePath}");
        provider.EnsureDatabaseExists();
        provider.CreateTable<GameAdminMessageRow>();
        _queries = provider;
        _commands = provider;
        _repository = provider;
        EnsureSchema();
    }

    public async Task<GameAdminMessage?> FindForUserAsync(
        string messageId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _queries.QueryAsync<GameAdminMessageRow>(
            """
            SELECT MessageId, Administrator, Kind, Target, TargetUserId, Title, Text,
                   AttachmentKind, AttachmentAmount, CreatedUtc
            FROM MaruIdleAdminMessages
            WHERE MessageId = @MessageId
              AND (Target = @AllPlayers OR TargetUserId = @UserId)
            LIMIT 1
            """,
            new { MessageId = messageId, AllPlayers = GameAdminMessageTarget.AllPlayers.ToString(), UserId = userId },
            cancellationToken).ConfigureAwait(false);
        return rows.FirstOrDefault()?.ToDomain();
    }

    public async Task AppendAsync(GameAdminMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { await _repository.InsertAsync(GameAdminMessageRow.FromDomain(message), cancellationToken).ConfigureAwait(false); }
        finally { _writeLock.Release(); }
    }

    public async Task<IReadOnlyList<GameAdminMessage>> LoadForUserAsync(
        string userId,
        GameAdminMessageKind kind,
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        var rows = await _queries.QueryAsync<GameAdminMessageRow>(
            """
            SELECT MessageId, Administrator, Kind, Target, TargetUserId, Title, Text,
                   AttachmentKind, AttachmentAmount, CreatedUtc
            FROM MaruIdleAdminMessages
            WHERE Kind = @Kind
              AND (Target = @AllPlayers OR TargetUserId = @UserId)
            ORDER BY CreatedUtc DESC
            LIMIT @Count
            """,
            new
            {
                Kind = kind.ToString(),
                AllPlayers = GameAdminMessageTarget.AllPlayers.ToString(),
                UserId = userId,
                Count = Math.Clamp(count, 1, 200)
            },
            cancellationToken).ConfigureAwait(false);
        return rows.Select(row => row.ToDomain()).ToArray();
    }

    public async Task<IReadOnlyList<GameAdminMessage>> LoadRecentAsync(
        int count = 100,
        CancellationToken cancellationToken = default)
    {
        var rows = await _queries.QueryAsync<GameAdminMessageRow>(
            """
            SELECT MessageId, Administrator, Kind, Target, TargetUserId, Title, Text,
                   AttachmentKind, AttachmentAmount, CreatedUtc
            FROM MaruIdleAdminMessages
            ORDER BY CreatedUtc DESC
            LIMIT @Count
            """,
            new { Count = Math.Clamp(count, 1, 200) },
            cancellationToken).ConfigureAwait(false);
        return rows.Select(row => row.ToDomain()).ToArray();
    }

    private void EnsureSchema()
    {
        var columns = _queries.Query<AdminMessageColumnInfo>("PRAGMA table_info(MaruIdleAdminMessages)")
            .Select(column => column.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!columns.Contains("AttachmentKind"))
            _commands.ExecuteNonQuery("ALTER TABLE MaruIdleAdminMessages ADD COLUMN AttachmentKind TEXT NOT NULL DEFAULT 'None'");
        if (!columns.Contains("AttachmentAmount"))
            _commands.ExecuteNonQuery("ALTER TABLE MaruIdleAdminMessages ADD COLUMN AttachmentAmount INTEGER NOT NULL DEFAULT 0");
    }
}

internal sealed class AdminMessageColumnInfo { public string Name { get; set; } = string.Empty; }

[DatabaseTable("MaruIdleAdminMessages")]
internal sealed class GameAdminMessageRow
{
    [DatabaseKey]
    public string MessageId { get; set; } = string.Empty;
    public string Administrator { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string? TargetUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string AttachmentKind { get; set; } = nameof(GameAdminMailAttachmentKind.None);
    public long AttachmentAmount { get; set; }
    public DateTime CreatedUtc { get; set; }

    public GameAdminMessage ToDomain() => new(
        MessageId,
        Administrator,
        Enum.TryParse<GameAdminMessageKind>(Kind, out var kind) ? kind : GameAdminMessageKind.Mail,
        Enum.TryParse<GameAdminMessageTarget>(Target, out var target) ? target : GameAdminMessageTarget.Individual,
        TargetUserId,
        Title,
        Text,
        Enum.TryParse<GameAdminMailAttachmentKind>(AttachmentKind, out var attachmentKind) ? attachmentKind : GameAdminMailAttachmentKind.None,
        AttachmentAmount,
        CreatedUtc);

    public static GameAdminMessageRow FromDomain(GameAdminMessage value) => new()
    {
        MessageId = value.MessageId,
        Administrator = value.Administrator,
        Kind = value.Kind.ToString(),
        Target = value.Target.ToString(),
        TargetUserId = value.TargetUserId,
        Title = value.Title,
        Text = value.Text,
        AttachmentKind = value.AttachmentKind.ToString(),
        AttachmentAmount = value.AttachmentAmount,
        CreatedUtc = value.CreatedUtc
    };
}
