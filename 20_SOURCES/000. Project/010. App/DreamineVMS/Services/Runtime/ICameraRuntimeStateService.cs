using DreamineVMS.Models;

namespace DreamineVMS.Services.Runtime;

/// <summary>
/// \brief 카메라 런타임 상태 관리 서비스입니다.
/// </summary>
public interface ICameraRuntimeStateService
{
    /// <summary>\brief 상태 변경 이벤트입니다.</summary>
    event EventHandler<CameraRuntimeState>? StateChanged;

    /// <summary>\brief 지정한 카메라 상태를 반환합니다.</summary>
    CameraRuntimeState GetState(string cameraId);

    /// <summary>\brief 모든 카메라 상태를 반환합니다.</summary>
    IReadOnlyList<CameraRuntimeState> GetAll();

    /// <summary>\brief 카메라 상태를 설정합니다.</summary>
    void SetState(string cameraId, CameraConnectionState state, string message, string? error = null);

    /// <summary>\brief 재시작 횟수를 증가시킵니다.</summary>
    void IncrementRestart(string cameraId, string message);
}
