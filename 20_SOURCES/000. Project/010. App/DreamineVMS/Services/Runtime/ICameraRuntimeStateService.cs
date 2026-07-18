using DreamineVMS.Models;

namespace DreamineVMS.Services.Runtime;

/// <summary>
/// \if KO
/// <para>\brief 카메라 런타임 상태 관리 서비스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i camera runtime state service functionality and related state.</para>
/// \endif
/// </summary>
public interface ICameraRuntimeStateService
{
    /// <summary>
    /// \if KO
    /// <para>\brief 상태 변경 이벤트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when state changed takes place.</para>
    /// \endif
    /// </summary>
    event EventHandler<CameraRuntimeState>? StateChanged;

    /// <summary>
    /// \if KO
    /// <para>\brief 지정한 카메라 상태를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the state value.</para>
    /// \endif
    /// </summary>
    /// <param name="cameraId">
    /// \if KO
    /// <para>camera Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for camera id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get State 작업에서 생성한 <c>CameraRuntimeState</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraRuntimeState</c> result produced by the get state operation.</para>
    /// \endif
    /// </returns>
    CameraRuntimeState GetState(string cameraId);

    /// <summary>
    /// \if KO
    /// <para>\brief 모든 카메라 상태를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the all value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get All 작업에서 생성한 <c>IReadOnlyList&lt;CameraRuntimeState&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;CameraRuntimeState&gt;</c> result produced by the get all operation.</para>
    /// \endif
    /// </returns>
    IReadOnlyList<CameraRuntimeState> GetAll();

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라 상태를 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the state value.</para>
    /// \endif
    /// </summary>
    /// <param name="cameraId">
    /// \if KO
    /// <para>camera Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for camera id.</para>
    /// \endif
    /// </param>
    /// <param name="state">
    /// \if KO
    /// <para>state에 사용할 <c>CameraConnectionState</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraConnectionState</c> value used for state.</para>
    /// \endif
    /// </param>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
    /// <param name="error">
    /// \if KO
    /// <para>error에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for error.</para>
    /// \endif
    /// </param>
    void SetState(string cameraId, CameraConnectionState state, string message, string? error = null);

    /// <summary>
    /// \if KO
    /// <para>\brief 재시작 횟수를 증가시킵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the increment restart operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cameraId">
    /// \if KO
    /// <para>camera Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for camera id.</para>
    /// \endif
    /// </param>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
    void IncrementRestart(string cameraId, string message);
}
