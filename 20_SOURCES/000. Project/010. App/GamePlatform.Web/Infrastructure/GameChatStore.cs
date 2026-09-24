using Dreamine.Database.Abstractions;
using Dreamine.Database.Abstractions.Mapping;
using Dreamine.Database.Sqlite;
using GamePlatform.Application;
using GamePlatform.Domain;
using GamePlatform.Options;

namespace GamePlatform.Infrastructure;

/// <summary>Dreamine SQLite 공급자로 월드 채팅 기록을 저장합니다.</summary>
public sealed class GameChatStore : IGameChatStore
{
    private readonly IDatabaseQueryProvider _queries;
    private readonly IDatabaseRepository _repository;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>채팅 데이터베이스와 테이블을 준비합니다.</summary>
    public GameChatStore(GameOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var provider = new SqliteDatabaseProvider($"Data Source={options.DatabasePath}");
        provider.EnsureDatabaseExists();
        provider.CreateTable<ChannelGameChatMessageRow>();
        _queries = provider;
        _repository = provider;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameChatMessage>> LoadRecentAsync(
        string channelKey,
        int count,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelKey);
        var rows = await _queries.QueryAsync<ChannelGameChatMessageRow>(
            """
            SELECT MessageId, ChannelKey, UserId, DisplayName, Text, SentUtc
            FROM MaruIdleChannelChatMessages
            WHERE ChannelKey = @ChannelKey
            ORDER BY SentUtc DESC
            LIMIT @Count
            """,
            new { ChannelKey = channelKey, Count = Math.Clamp(count, 1, 100) },
            cancellationToken).ConfigureAwait(false);
        return rows
            .OrderBy(row => row.SentUtc)
            .Select(row => row.ToDomain())
            .ToArray();
    }

    /// <inheritdoc />
    public async Task AppendAsync(GameChatMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { await _repository.InsertAsync(ChannelGameChatMessageRow.FromDomain(message), cancellationToken).ConfigureAwait(false); }
        finally { _writeLock.Release(); }
    }
}

[DatabaseTable("MaruIdleChannelChatMessages")]
internal sealed class ChannelGameChatMessageRow
{
    [DatabaseKey]
    public string MessageId { get; set; } = string.Empty;
    public string ChannelKey { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime SentUtc { get; set; }

    public GameChatMessage ToDomain() => new(MessageId, ChannelKey, UserId, DisplayName, Text, SentUtc);
    public static ChannelGameChatMessageRow FromDomain(GameChatMessage value) => new()
    {
        MessageId = value.MessageId,
        ChannelKey = value.ChannelKey,
        UserId = value.UserId,
        DisplayName = value.DisplayName,
        Text = value.Text,
        SentUtc = value.SentUtc
    };
}
