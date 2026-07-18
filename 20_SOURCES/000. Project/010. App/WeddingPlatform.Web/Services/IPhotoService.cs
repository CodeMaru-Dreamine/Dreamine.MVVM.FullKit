using Microsoft.AspNetCore.Components.Forms;
using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// \if KO
/// <para>I Photo Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i photo service functionality and related state.</para>
/// \endif
/// </summary>
public interface IPhotoService
{
    /// <summary>
    /// \if KO
    /// <para>Gallery Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the gallery async value.</para>
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
    /// <para>Get Gallery Async 작업에서 생성한 <c>Task&lt;IReadOnlyList&lt;PhotoInfo&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;IReadOnlyList&lt;PhotoInfo&gt;&gt;</c> result produced by the get gallery async operation.</para>
    /// \endif
    /// </returns>
    Task<IReadOnlyList<PhotoInfo>> GetGalleryAsync(string slug, CancellationToken ct = default);
    /// <summary>
    /// \if KO
    /// <para>Upload Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload async operation.</para>
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
    /// <para>Upload Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload async operation.</para>
    /// \endif
    /// </returns>
    Task<string> UploadAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    /// <summary>
    /// \if KO
    /// <para>Upload Hero Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload hero async operation.</para>
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
    /// <para>Upload Hero Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload hero async operation.</para>
    /// \endif
    /// </returns>
    Task<string> UploadHeroAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    /// <summary>
    /// \if KO
    /// <para>Upload Road Map Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload road map async operation.</para>
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
    /// <para>Upload Road Map Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload road map async operation.</para>
    /// \endif
    /// </returns>
    Task<string> UploadRoadMapAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    /// <summary>
    /// \if KO
    /// <para>Upload Music Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload music async operation.</para>
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
    /// <para>Upload Music Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload music async operation.</para>
    /// \endif
    /// </returns>
    Task<string> UploadMusicAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    /// <summary>
    /// \if KO
    /// <para>Upload Og Image Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload og image async operation.</para>
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
    /// <para>Upload Og Image Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload og image async operation.</para>
    /// \endif
    /// </returns>
    Task<string> UploadOgImageAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    /// <summary>
    /// \if KO
    /// <para>Upload Thank You Og Image Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload thank you og image async operation.</para>
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
    /// <para>Upload Thank You Og Image Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload thank you og image async operation.</para>
    /// \endif
    /// </returns>
    Task<string> UploadThankYouOgImageAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    /// <summary>
    /// \if KO
    /// <para>Upload Video Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload video async operation.</para>
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
    /// <para>Upload Video Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload video async operation.</para>
    /// \endif
    /// </returns>
    Task<string> UploadVideoAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    /// <summary>
    /// \if KO
    /// <para>Delete Video Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete video async operation.</para>
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
    /// <para>Delete Video Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete video async operation.</para>
    /// \endif
    /// </returns>
    Task DeleteVideoAsync(string slug, string fileName, CancellationToken ct = default);
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
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
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
    Task DeleteAsync(string slug, string fileName, CancellationToken ct = default);
    /// <summary>
    /// \if KO
    /// <para>Photo Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the photo url value.</para>
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
    /// <para>Get Photo Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get photo url operation.</para>
    /// \endif
    /// </returns>
    string GetPhotoUrl(string slug, string fileName);
    /// <summary>
    /// \if KO
    /// <para>Thumb Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the thumb url value.</para>
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
    /// <para>Get Thumb Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get thumb url operation.</para>
    /// \endif
    /// </returns>
    string GetThumbUrl(string slug, string fileName);
    /// <summary>
    /// \if KO
    /// <para>Hero Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the hero url value.</para>
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
    /// <para>Get Hero Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get hero url operation.</para>
    /// \endif
    /// </returns>
    string GetHeroUrl(string slug, string fileName);
    /// <summary>
    /// \if KO
    /// <para>Road Map Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the road map url value.</para>
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
    /// <para>Get Road Map Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get road map url operation.</para>
    /// \endif
    /// </returns>
    string GetRoadMapUrl(string slug, string fileName);
    /// <summary>
    /// \if KO
    /// <para>Music Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the music url value.</para>
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
    /// <para>Get Music Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get music url operation.</para>
    /// \endif
    /// </returns>
    string GetMusicUrl(string slug, string fileName);
    /// <summary>
    /// \if KO
    /// <para>Video Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the video url value.</para>
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
    /// <para>Get Video Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get video url operation.</para>
    /// \endif
    /// </returns>
    string GetVideoUrl(string slug, string fileName);
}
