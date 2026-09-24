using System.Text.Json;
using Dreamine.Database.Abstractions;
using Dreamine.Database.Abstractions.Mapping;
using Dreamine.Database.Sqlite;
using GamePlatform.Application;
using GamePlatform.Domain;
using GamePlatform.Options;

namespace GamePlatform.Infrastructure;

/// <summary>별도 SQLite 테이블에 사용자 설정 JSON을 저장합니다.</summary>
public sealed class GameSettingsStore : IGameSettingsStore, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabaseQueryProvider _queries;
    private readonly IDatabaseRepository _repository;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>설정 저장소를 열고 설정 테이블을 비파괴 생성합니다.</summary>
    public GameSettingsStore(GameOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var provider = new SqliteDatabaseProvider($"Data Source={options.DatabasePath}");
        provider.EnsureDatabaseExists();
        provider.CreateTable<GameSettingsRow>();
        _queries = provider;
        _repository = provider;
    }

    /// <inheritdoc />
    public async Task<GameSettings?> FindAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var rows = await _queries.QueryAsync<GameSettingsRow>(
            "SELECT UserId, SettingsJson, UpdatedUtc FROM MaruIdleSettings WHERE UserId = @UserId LIMIT 1",
            new { UserId = userId }, cancellationToken).ConfigureAwait(false);
        var row = rows.FirstOrDefault();
        if (row is null || string.IsNullOrWhiteSpace(row.SettingsJson)) return null;
        try { return JsonSerializer.Deserialize<GameSettings>(row.SettingsJson, JsonOptions); }
        catch (JsonException) { return null; }
    }

    /// <inheritdoc />
    public async Task SaveAsync(string userId, GameSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(settings);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = await _queries.QueryAsync<GameSettingsRow>(
                "SELECT UserId, SettingsJson, UpdatedUtc FROM MaruIdleSettings WHERE UserId = @UserId LIMIT 1",
                new { UserId = userId }, cancellationToken).ConfigureAwait(false);
            var row = new GameSettingsRow
            {
                UserId = userId,
                SettingsJson = JsonSerializer.Serialize(GameSettingsPolicy.Normalize(settings), JsonOptions),
                UpdatedUtc = DateTime.UtcNow
            };
            if (existing.Count == 0) await _repository.InsertAsync(row, cancellationToken).ConfigureAwait(false);
            else if (!await _repository.UpdateAsync(row, cancellationToken).ConfigureAwait(false))
                await _repository.InsertAsync(row, cancellationToken).ConfigureAwait(false);
        }
        finally { _writeLock.Release(); }
    }

    /// <summary>설정 저장 동시성 잠금을 정리합니다.</summary>
    public void Dispose() => _writeLock.Dispose();
}

[DatabaseTable("MaruIdleSettings")]
internal sealed class GameSettingsRow
{
    [DatabaseKey]
    public string UserId { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = string.Empty;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
