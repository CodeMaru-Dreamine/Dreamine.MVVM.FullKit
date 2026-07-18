using System.IO;
using Microsoft.AspNetCore.Components.Forms;

namespace PortfolioApp.Services;

/// <summary>
/// \if KO
/// <para>Local Media Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates local media service functionality and related state.</para>
/// \endif
/// </summary>
public class LocalMediaService : IMediaService
{
    /// <summary>
    /// \if KO
    /// <para>root 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the root value.</para>
    /// \endif
    /// </summary>
    private readonly string _root;
    /// <summary>
    /// \if KO
    /// <para>Max Image Bytes 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the max image bytes value.</para>
    /// \endif
    /// </summary>
    private const long MaxImageBytes = 20 * 1024 * 1024;
    /// <summary>
    /// \if KO
    /// <para>Max Video Bytes 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the max video bytes value.</para>
    /// \endif
    /// </summary>
    private const long MaxVideoBytes = 500 * 1024 * 1024;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="LocalMediaService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="LocalMediaService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>PortfolioOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    public LocalMediaService(PortfolioOptions opts) => _root = opts.ResolvedDataPath;

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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
    /// \endif
    /// </param>
    /// <param name="file">
    /// \if KO
    /// <para>file에 사용할 <c>IBrowserFile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IBrowserFile</c> value used for file.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Save Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the save async operation.</para>
    /// \endif
    /// </returns>
    public async Task<string> SaveAsync(string slug, string projectId, IBrowserFile file)
    {
        var dir = Path.Combine(_root, slug, "media", projectId);
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        var safe = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(dir, safe);
        await using var fs = new FileStream(path, FileMode.Create);
        await file.OpenReadStream(MaxImageBytes).CopyToAsync(fs);
        return safe;
    }

    /// <summary>
    /// \if KO
    /// <para>Video Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves video async data.</para>
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
    /// \endif
    /// </param>
    /// <param name="file">
    /// \if KO
    /// <para>file에 사용할 <c>IBrowserFile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IBrowserFile</c> value used for file.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Save Video Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the save video async operation.</para>
    /// \endif
    /// </returns>
    public async Task<string> SaveVideoAsync(string slug, string projectId, IBrowserFile file)
    {
        var dir = Path.Combine(_root, slug, "media", projectId);
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        var safe = $"vid_{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(dir, safe);
        await using var fs = new FileStream(path, FileMode.Create);
        await file.OpenReadStream(MaxVideoBytes).CopyToAsync(fs);
        return safe;
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
    /// \endif
    /// </param>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
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
    public Task DeleteAsync(string slug, string projectId, string fileName)
    {
        var path = Path.Combine(_root, slug, "media", projectId, fileName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    /// <summary>
    /// \if KO
    /// <para>Profile Image Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves profile image async data.</para>
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
    /// <param name="file">
    /// \if KO
    /// <para>file에 사용할 <c>IBrowserFile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IBrowserFile</c> value used for file.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Save Profile Image Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the save profile image async operation.</para>
    /// \endif
    /// </returns>
    public async Task<string> SaveProfileImageAsync(string slug, IBrowserFile file)
    {
        var dir = Path.Combine(_root, slug, "media", "_profile");
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        var safe = $"profile{ext}";
        var path = Path.Combine(dir, safe);
        await using var fs = new FileStream(path, FileMode.Create);
        await file.OpenReadStream(MaxImageBytes).CopyToAsync(fs);
        return safe;
    }

    /// <summary>
    /// \if KO
    /// <para>Media Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the media url value.</para>
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
    /// \endif
    /// </param>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Media Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get media url operation.</para>
    /// \endif
    /// </returns>
    public string GetMediaUrl(string slug, string projectId, string fileName) =>
        $"/portfolio-data/{slug}/media/{projectId}/{Uri.EscapeDataString(fileName)}";

    /// <summary>
    /// \if KO
    /// <para>Profile Image Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the profile image url value.</para>
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
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Profile Image Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get profile image url operation.</para>
    /// \endif
    /// </returns>
    public string GetProfileImageUrl(string slug, string fileName) =>
        $"/portfolio-data/{slug}/media/_profile/{Uri.EscapeDataString(fileName)}";
}
