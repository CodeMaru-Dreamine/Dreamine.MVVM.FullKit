using DreamineVMS.Models;

namespace DreamineVMS.Services.Cameras;

/// <summary>
/// \if KO
/// <para>\brief VMS 카메라 저장소 인터페이스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i vms camera repository functionality and related state.</para>
/// \endif
/// </summary>
public interface IVmsCameraRepository
{
    /// <summary>
    /// \if KO
    /// <para>\brief 등록된 모든 카메라를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the all value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get All 작업에서 생성한 <c>IReadOnlyList&lt;CameraDevice&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;CameraDevice&gt;</c> result produced by the get all operation.</para>
    /// \endif
    /// </returns>
    IReadOnlyList<CameraDevice> GetAll();

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라를 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the async item.</para>
    /// \endif
    /// </summary>
    /// <param name="camera">
    /// \if KO
    /// <para>camera에 사용할 <c>CameraDevice</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraDevice</c> value used for camera.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Add Async 작업에서 생성한 <c>Task&lt;CameraDevice&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;CameraDevice&gt;</c> result produced by the add async operation.</para>
    /// \endif
    /// </returns>
    Task<CameraDevice> AddAsync(CameraDevice camera);

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라 정보를 업데이트합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the update async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="camera">
    /// \if KO
    /// <para>camera에 사용할 <c>CameraDevice</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraDevice</c> value used for camera.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Update Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the update async operation.</para>
    /// \endif
    /// </returns>
    Task UpdateAsync(CameraDevice camera);

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라를 삭제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete async operation.</para>
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
    /// <para>Delete Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete async operation.</para>
    /// \endif
    /// </returns>
    Task DeleteAsync(string id);

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라 목록이 변경될 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when changed takes place.</para>
    /// \endif
    /// </summary>
    event EventHandler<CameraRepositoryChangedEventArgs>? Changed;
}
