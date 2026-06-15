using DreamineVMS.Web.Models;

namespace DreamineVMS.Web.Services.Cameras;

/// <summary>
/// \brief VMS 카메라 저장소 인터페이스입니다.
/// </summary>
public interface IVmsCameraRepository
{
    /// <summary>\brief 등록된 모든 카메라를 반환합니다.</summary>
    IReadOnlyList<CameraDevice> GetAll();

    /// <summary>\brief 카메라를 추가합니다.</summary>
    Task<CameraDevice> AddAsync(CameraDevice camera);

    /// <summary>\brief 카메라 정보를 업데이트합니다.</summary>
    Task UpdateAsync(CameraDevice camera);

    /// <summary>\brief 카메라를 삭제합니다.</summary>
    Task DeleteAsync(string id);

    /// <summary>\brief 카메라 목록이 변경될 때 발생합니다.</summary>
    event EventHandler<CameraRepositoryChangedEventArgs>? Changed;
}
