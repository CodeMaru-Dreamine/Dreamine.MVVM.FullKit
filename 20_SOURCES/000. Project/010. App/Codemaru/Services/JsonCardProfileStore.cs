using Codemaru.Models;
using System.IO;
using System.Text.Json;

namespace Codemaru.Services;

/// <summary>
/// \brief CardHybrid 스냅샷을 프로젝트의 <c>App_Data/Cards/{userId}/snapshot.json</c> 에 저장합니다.
/// </summary>
/// <remarks>
/// Guest 사용자의 데이터는 파일로 저장하지 않습니다 (서킷 메모리만 사용).
/// </remarks>
public sealed class JsonCardProfileStore : ICardProfileStore
{
    private const string SnapshotFileName = "snapshot.json";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _rootDirectory;

    public JsonCardProfileStore()
    {
        _rootDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data", "Cards");
        Directory.CreateDirectory(_rootDirectory);
    }

    /// <inheritdoc />
    public async Task<CardHybridSnapshot?> LoadAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (IsGuest(userId))
        {
            return null;
        }

        var filePath = GetSnapshotPath(userId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        var snapshot = await JsonSerializer.DeserializeAsync<CardHybridSnapshot>(
            stream, SerializerOptions, cancellationToken).ConfigureAwait(false);

        return snapshot?.UserId == userId ? snapshot : snapshot;
    }

    /// <inheritdoc />
    public async Task SaveAsync(string userId, CardHybridSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (IsGuest(userId))
        {
            return;
        }

        var filePath = GetSnapshotPath(userId);
        var directory = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(directory);

        var tempPath = filePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, snapshot, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        File.Copy(tempPath, filePath, overwrite: true);
        File.Delete(tempPath);
    }

    /// <inheritdoc />
    public async Task<CardHybridSnapshot?> LoadBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalizedSlug = slug.Trim().Trim('/').ToLowerInvariant();

        foreach (var userDirectory in Directory.EnumerateDirectories(_rootDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = Path.Combine(userDirectory, SnapshotFileName);
            if (!File.Exists(filePath))
            {
                continue;
            }

            try
            {
                await using var stream = File.OpenRead(filePath);
                var snapshot = await JsonSerializer.DeserializeAsync<CardHybridSnapshot>(
                    stream, SerializerOptions, cancellationToken).ConfigureAwait(false);

                if (snapshot?.Profile is null)
                {
                    continue;
                }

                var profileSlug = snapshot.Profile.LandingSlug.Trim().Trim('/').ToLowerInvariant();
                if (profileSlug == $"card/{normalizedSlug}" || profileSlug == normalizedSlug)
                {
                    return snapshot;
                }
            }
            catch
            {
                // skip corrupt files
            }
        }

        return null;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (IsGuest(userId))
        {
            return Task.CompletedTask;
        }

        var filePath = GetSnapshotPath(userId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private string GetSnapshotPath(string userId)
    {
        return Path.Combine(_rootDirectory, SanitizeUserId(userId), SnapshotFileName);
    }

    private static bool IsGuest(string userId) =>
        string.IsNullOrWhiteSpace(userId) ||
        string.Equals(userId, CardHybridUser.Guest.Id, StringComparison.OrdinalIgnoreCase);

    private static string SanitizeUserId(string userId)
    {
        var safe = new string((string.IsNullOrWhiteSpace(userId) ? "guest" : userId)
            .Select(static c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_')
            .ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "guest" : safe;
    }
}
