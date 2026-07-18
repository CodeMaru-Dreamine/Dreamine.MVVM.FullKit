using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        Path.Combine(_tenants.GetTenantDataPath(slug), "posts");

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
        Path.Combine(PostsDir(slug), $"{Sanitize(postId)}.json");

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
        var dir = PostsDir(slug);
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(_tenants.GetTenantDataPath(slug), "media", post.Id));

        var path = PostPath(slug, post.Id);
        var tmp = path + ".tmp";

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using (var fs = File.Create(tmp))
                await JsonSerializer.SerializeAsync(fs, post, _jsonOpts, ct).ConfigureAwait(false);

            File.Copy(tmp, path, overwrite: true);
            File.Delete(tmp);
        }
        finally { _gate.Release(); }
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
    public Task DeleteAsync(string slug, string postId, CancellationToken ct = default)
    {
        var path = PostPath(slug, postId);
        if (File.Exists(path)) File.Delete(path);

        var mediaDir = Path.Combine(_tenants.GetTenantDataPath(slug), "media", postId);
        if (Directory.Exists(mediaDir)) Directory.Delete(mediaDir, recursive: true);

        return Task.CompletedTask;
    }

    /// <summary>
    /// \if KO
    /// <para>Sanitize 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sanitize operation.</para>
    /// \endif
    /// </summary>
    /// <param name="id">
    /// \if KO
    /// <para>id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Sanitize 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the sanitize operation.</para>
    /// \endif
    /// </returns>
    private static string Sanitize(string id) =>
        string.Concat(id.Split(Path.GetInvalidFileNameChars()));
}
