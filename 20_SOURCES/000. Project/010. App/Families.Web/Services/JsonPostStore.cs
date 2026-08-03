using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dreamine.AppSecurity;
using FamiliesApp.Models;

namespace FamiliesApp.Services;

/// <summary>
/// \if KO
/// <para>Json Post Store 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates json post store functionality and related state.</para>
/// \endif
/// </summary>
public sealed class JsonPostStore : IPostStore
{
    /// <summary>
    /// \if KO
    /// <para>tenants 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tenants value.</para>
    /// \endif
    /// </summary>
    private readonly IFamilyTenantStore _tenants;
    /// <summary>
    /// \if KO
    /// <para>gate 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the gate value.</para>
    /// \endif
    /// </summary>
    private static readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// \if KO
    /// <para>json Opts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the json opts value.</para>
    /// \endif
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="JsonPostStore"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="JsonPostStore"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="tenants">
    /// \if KO
    /// <para>tenants에 사용할 <c>IFamilyTenantStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IFamilyTenantStore</c> value used for tenants.</para>
    /// \endif
    /// </param>
    public JsonPostStore(IFamilyTenantStore tenants) => _tenants = tenants;

    /// <summary>
    /// \if KO
    /// <para>Posts Dir 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the posts dir operation.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Posts Dir 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the posts dir operation.</para>
    /// \endif
    /// </returns>
    private string PostsDir(string slug) =>
        StoragePathGuard.ResolveUnderRoot(_tenants.GetTenantDataPath(slug), "posts");

    /// <summary>
    /// \if KO
    /// <para>Post Path 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the post path operation.</para>
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
    /// <param name="postId">
    /// \if KO
    /// <para>post Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for post id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Post Path 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the post path operation.</para>
    /// \endif
    /// </returns>
    private string PostPath(string slug, string postId) =>
        StoragePathGuard.ResolveIdentifierFile(PostsDir(slug), postId, ".json", nameof(postId));

    /// <inheritdoc />
    public async Task<PostEntry?> MutateAsync(
        string slug,
        string postId,
        Func<PostEntry?, PostEntry?> mutation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        var path = PostPath(slug, postId);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            PostEntry? current = null;
            if (File.Exists(path))
            {
                await using var input = File.OpenRead(path);
                current = await JsonSerializer.DeserializeAsync<PostEntry>(input, _jsonOpts, ct)
                    .ConfigureAwait(false);
            }

            var updated = mutation(current);
            if (updated is null)
            {
                return current;
            }

            var dir = PostsDir(slug);
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(MediaDirectory(slug, updated.Id));
            var tmp = StoragePathGuard.ResolveUnderRoot(
                Path.GetDirectoryName(path)!,
                $"{Path.GetFileName(path)}.tmp");

            await using (var output = File.Create(tmp))
            {
                await JsonSerializer.SerializeAsync(output, updated, _jsonOpts, ct).ConfigureAwait(false);
            }

            File.Copy(tmp, path, overwrite: true);
            File.Delete(tmp);
            return updated;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the async value.</para>
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
    /// <param name="postId">
    /// \if KO
    /// <para>post Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for post id.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Async 작업에서 생성한 <c>Task&lt;PostEntry?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;PostEntry?&gt;</c> result produced by the get async operation.</para>
    /// \endif
    /// </returns>
    public async Task<PostEntry?> GetAsync(string slug, string postId, CancellationToken ct = default)
    {
        var path = PostPath(slug, postId);
        if (!File.Exists(path)) return null;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<PostEntry>(fs, _jsonOpts, ct).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// \if KO
    /// <para>All Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the all async value.</para>
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
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get All Async 작업에서 생성한 <c>Task&lt;IReadOnlyList&lt;PostEntry&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;IReadOnlyList&lt;PostEntry&gt;&gt;</c> result produced by the get all async operation.</para>
    /// \endif
    /// </returns>
    public async Task<IReadOnlyList<PostEntry>> GetAllAsync(string slug, CancellationToken ct = default)
    {
        var dir = PostsDir(slug);
        if (!Directory.Exists(dir)) return [];

        var list = new List<PostEntry>();
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await using var fs = File.OpenRead(file);
                var p = await JsonSerializer.DeserializeAsync<PostEntry>(fs, _jsonOpts, ct).ConfigureAwait(false);
                if (p != null) list.Add(p);
            }
            catch { /* 깨진 파일 하나로 전체 목록이 죽지 않도록 */ }
            finally { _gate.Release(); }
        }
        return list.OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.PostedAt).ToList();
    }

    /// <summary>
    /// \if KO
    /// <para>Page Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the page async value.</para>
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
    /// <param name="page">
    /// \if KO
    /// <para>page에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for page.</para>
    /// \endif
    /// </param>
    /// <param name="pageSize">
    /// \if KO
    /// <para>page Size에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for page size.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Page Async 작업에서 생성한 <c>Task&lt;(IReadOnlyList&lt;PostEntry&gt; Items, int TotalCount)&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;(IReadOnlyList&lt;PostEntry&gt; Items, int TotalCount)&gt;</c> result produced by the get page async operation.</para>
    /// \endif
    /// </returns>
    public async Task<(IReadOnlyList<PostEntry> Items, int TotalCount)> GetPageAsync(
        string slug, int page, int pageSize, CancellationToken ct = default)
    {
        var all = await GetAllAsync(slug, ct).ConfigureAwait(false);
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, all.Count);
    }

    /// <summary>
    /// \if KO
    /// <para>By Album Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the by album async value.</para>
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
    /// <param name="albumId">
    /// \if KO
    /// <para>album Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for album id.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get By Album Async 작업에서 생성한 <c>Task&lt;IReadOnlyList&lt;PostEntry&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;IReadOnlyList&lt;PostEntry&gt;&gt;</c> result produced by the get by album async operation.</para>
    /// \endif
    /// </returns>
    public async Task<IReadOnlyList<PostEntry>> GetByAlbumAsync(string slug, string albumId, CancellationToken ct = default)
    {
        var all = await GetAllAsync(slug, ct).ConfigureAwait(false);
        return all.Where(p => p.AlbumId == albumId).ToList();
    }

    /// <summary>
    /// \if KO
    /// <para>By Album Page Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the by album page async value.</para>
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
    /// <param name="albumId">
    /// \if KO
    /// <para>album Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for album id.</para>
    /// \endif
    /// </param>
    /// <param name="page">
    /// \if KO
    /// <para>page에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for page.</para>
    /// \endif
    /// </param>
    /// <param name="pageSize">
    /// \if KO
    /// <para>page Size에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for page size.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get By Album Page Async 작업에서 생성한 <c>Task&lt;(IReadOnlyList&lt;PostEntry&gt; Items, int TotalCount)&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;(IReadOnlyList&lt;PostEntry&gt; Items, int TotalCount)&gt;</c> result produced by the get by album page async operation.</para>
    /// \endif
    /// </returns>
    public async Task<(IReadOnlyList<PostEntry> Items, int TotalCount)> GetByAlbumPageAsync(
        string slug, string albumId, int page, int pageSize, CancellationToken ct = default)
    {
        var filtered = await GetByAlbumAsync(slug, albumId, ct).ConfigureAwait(false);
        var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, filtered.Count);
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
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
    /// <param name="post">
    /// \if KO
    /// <para>post에 사용할 <c>PostEntry</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PostEntry</c> value used for post.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
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
    public async Task SaveAsync(string slug, PostEntry post, CancellationToken ct = default)
    {
        await MutateAsync(slug, post.Id, _ => post, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// \if KO
    /// <para>Delete Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete async operation.</para>
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
    /// <param name="postId">
    /// \if KO
    /// <para>post Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for post id.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
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
    public async Task DeleteAsync(string slug, string postId, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var path = PostPath(slug, postId);
            var mediaDir = MediaDirectory(slug, postId);
            var deleteId = Guid.NewGuid().ToString("N");
            var stagedPostPath = StoragePathGuard.ResolveUnderRoot(
                Path.GetDirectoryName(path)!,
                $"{Path.GetFileName(path)}.deleting-{deleteId}");
            var stagedMediaDir = StoragePathGuard.ResolveUnderRoot(
                Path.GetDirectoryName(mediaDir)!,
                $"{Path.GetFileName(mediaDir)}.deleting-{deleteId}");
            var postStaged = false;
            var mediaStaged = false;

            try
            {
                // Rename both active paths before destructive cleanup. A move either succeeds
                // as one filesystem operation or leaves the source untouched, so an open media
                // handle cannot first remove the JSON and then strand a half-deleted post.
                if (Directory.Exists(mediaDir))
                {
                    Directory.Move(mediaDir, stagedMediaDir);
                    mediaStaged = true;
                }

                if (File.Exists(path))
                {
                    File.Move(path, stagedPostPath);
                    postStaged = true;
                }
            }
            catch
            {
                TryRestoreFile(stagedPostPath, path, postStaged);
                TryRestoreDirectory(stagedMediaDir, mediaDir, mediaStaged);
                throw;
            }

            // The post is no longer visible to readers. Cleanup is best-effort because a
            // transient antivirus/preview handle must not roll an already-consistent delete
            // back into a partially visible state.
            TryDeleteFile(stagedPostPath, postStaged);
            TryDeleteDirectory(stagedMediaDir, mediaStaged);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void TryDeleteFile(string path, bool shouldDelete)
    {
        if (!shouldDelete) return;
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void TryDeleteDirectory(string path, bool shouldDelete)
    {
        if (!shouldDelete) return;
        try { Directory.Delete(path, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void TryRestoreFile(string stagedPath, string activePath, bool shouldRestore)
    {
        if (!shouldRestore || !File.Exists(stagedPath) || File.Exists(activePath)) return;
        try { File.Move(stagedPath, activePath); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void TryRestoreDirectory(string stagedPath, string activePath, bool shouldRestore)
    {
        if (!shouldRestore || !Directory.Exists(stagedPath) || Directory.Exists(activePath)) return;
        try { Directory.Move(stagedPath, activePath); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private string MediaDirectory(string slug, string postId)
    {
        var mediaRoot = StoragePathGuard.ResolveUnderRoot(_tenants.GetTenantDataPath(slug), "media");
        return StoragePathGuard.ResolveIdentifierDirectory(mediaRoot, postId, nameof(postId));
    }
}
