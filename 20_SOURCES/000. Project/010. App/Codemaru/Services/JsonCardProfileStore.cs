using Codemaru.Models;
using System.IO;
using System.Text.Json;

namespace Codemaru.Services;

/// <summary>
/// \if KO
/// <para>\brief CardHybrid 스냅샷을 프로젝트의 <c>App_Data/Cards/{userId}/snapshot.json</c> 에 저장합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates json card profile store functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>Guest 사용자의 데이터는 파일로 저장하지 않습니다 (서킷 메모리만 사용).</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
public sealed class JsonCardProfileStore : ICardProfileStore
{
    /// <summary>
    /// \if KO
    /// <para>Snapshot File Name 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the snapshot file name value.</para>
    /// \endif
    /// </summary>
    private const string SnapshotFileName = "snapshot.json";

    /// <summary>
    /// \if KO
    /// <para>Serializer Options 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the serializer options value.</para>
    /// \endif
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    /// <summary>
    /// \if KO
    /// <para>root Directory 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the root directory value.</para>
    /// \endif
    /// </summary>
    private readonly string _rootDirectory;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="JsonCardProfileStore"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="JsonCardProfileStore"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public JsonCardProfileStore()
    {
        _rootDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data", "Cards");
        Directory.CreateDirectory(_rootDirectory);
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads async data.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Load Async 작업에서 생성한 <c>Task&lt;CardHybridSnapshot?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;CardHybridSnapshot?&gt;</c> result produced by the load async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <param name="snapshot">
    /// \if KO
    /// <para>snapshot에 사용할 <c>CardHybridSnapshot</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridSnapshot</c> value used for snapshot.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Save Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>By Slug Async 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads by slug async data.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Load By Slug Async 작업에서 생성한 <c>Task&lt;CardHybridSnapshot?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;CardHybridSnapshot?&gt;</c> result produced by the load by slug async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Delete Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Delete Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Snapshot Path 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the snapshot path value.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Snapshot Path 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get snapshot path operation.</para>
    /// \endif
    /// </returns>
    private string GetSnapshotPath(string userId)
    {
        return Path.Combine(_rootDirectory, SanitizeUserId(userId), SnapshotFileName);
    }

    /// <summary>
    /// \if KO
    /// <para>Is Guest 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is guest.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Guest 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is guest condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsGuest(string userId) =>
        string.IsNullOrWhiteSpace(userId) ||
        string.Equals(userId, CardHybridUser.Guest.Id, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// \if KO
    /// <para>Sanitize User Id 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sanitize user id operation.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Sanitize User Id 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the sanitize user id operation.</para>
    /// \endif
    /// </returns>
    private static string SanitizeUserId(string userId)
    {
        var safe = new string((string.IsNullOrWhiteSpace(userId) ? "guest" : userId)
            .Select(static c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_')
            .ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "guest" : safe;
    }
}
