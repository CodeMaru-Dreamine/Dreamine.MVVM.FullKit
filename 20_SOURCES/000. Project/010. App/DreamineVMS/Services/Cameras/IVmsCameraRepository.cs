using DreamineVMS.Models;

namespace DreamineVMS.Services.Cameras;

/// <summary>
/// \brief VMS 카메라 저장소 인터페이스입니다.
/// </summary>
public interface IVmsCameraRepository
{
    /// <summary>
    /// \brief 등록된 모든 카메라를 반환합니다.
    /// </summary>
    IReadOnlyList<CameraDevice> GetAll();
}
