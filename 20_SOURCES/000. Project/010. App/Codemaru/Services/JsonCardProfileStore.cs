using Codemaru.Models;
using System.Text.Json;
using System.IO;

namespace Codemaru.Services;

/// <summary>
/// \brief Stores CardHybrid profile data as a JSON file under the user's application data folder.
/// </summary>
public sealed class JsonCardProfileStore : ICardProfileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _directory;
    private readonly string _lastUserPath;

    /// <summary>
    /// \brief Initializes a new instance of the <see cref="JsonCardProfileStore" /> class.
    /// </summary>
    public JsonCardProfileStore()
    {
        _directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Dreamine",
            "CardHybrid");

        Directory.CreateDirectory(_directory);
        _lastUserPath = Path.Combine(_directory, "last-user.json");
    }

    /// <inheritdoc />
    public async Task<CardHybridUser?> LoadLastUserAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_lastUserPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(_lastUserPath);
        return await JsonSerializer.DeserializeAsync<CardHybridUser>(stream, SerializerOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveLastUserAsync(CardHybridUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        await SaveJsonAsync(_lastUserPath, user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CardHybridUser?> LoadUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filePath = GetUserPath(userId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<CardHybridUser>(stream, SerializerOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveUserAsync(CardHybridUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        await SaveJsonAsync(GetUserPath(user.Id), user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CardHybridSnapshot?> LoadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filePath = GetSnapshotPath(userId);
        if (!File.Exists(filePath))
        {
            filePath = userId.Equals(CardHybridUser.Guest.Id, StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(_directory, "cardhybrid-profile.json")
                : filePath;

            if (!File.Exists(filePath))
            {
                return null;
            }
        }

        await using var stream = File.OpenRead(filePath);
        var snapshot = await JsonSerializer.DeserializeAsync<CardHybridSnapshot>(stream, SerializerOptions, cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        if (userId.Equals(CardHybridUser.Guest.Id, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(snapshot.UserId))
        {
            return snapshot;
        }

        return snapshot.UserId == userId ? snapshot : null;
    }

    /// <inheritdoc />
    public async Task SaveAsync(string userId, CardHybridSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await SaveJsonAsync(GetSnapshotPath(userId), snapshot, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CardHybridSnapshot?> LoadBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalizedSlug = slug.Trim().Trim('/').ToLowerInvariant();
        var files = Directory.GetFiles(_directory, "cardhybrid-profile-*.json");

        foreach (var filePath in files)
        {
            try
            {
                await using var stream = File.OpenRead(filePath);
                var snapshot = await JsonSerializer.DeserializeAsync<CardHybridSnapshot>(stream, SerializerOptions, cancellationToken);
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

    private async Task SaveJsonAsync<T>(string filePath, T value, CancellationToken cancellationToken)
    {
        var tempPath = $"{filePath}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, SerializerOptions, cancellationToken);
        }

        File.Copy(tempPath, filePath, overwrite: true);
        File.Delete(tempPath);
    }

    private string GetSnapshotPath(string userId)
    {
        return Path.Combine(_directory, $"cardhybrid-profile-{SanitizeUserId(userId)}.json");
    }

    private string GetUserPath(string userId)
    {
        return Path.Combine(_directory, $"cardhybrid-user-{SanitizeUserId(userId)}.json");
    }

    private static string SanitizeUserId(string userId)
    {
        var safe = new string((string.IsNullOrWhiteSpace(userId) ? "guest" : userId)
            .Select(static c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_')
            .ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "guest" : safe;
    }
}
