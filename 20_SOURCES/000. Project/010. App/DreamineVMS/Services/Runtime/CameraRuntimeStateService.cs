using DreamineVMS.Models;
using System.Collections.Concurrent;

namespace DreamineVMS.Services.Runtime;

/// <summary>
/// \if KO
/// <para>\brief 메모리 기반 카메라 런타임 상태 관리 서비스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates camera runtime state service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CameraRuntimeStateService : ICameraRuntimeStateService
{
    /// <summary>
    /// \if KO
    /// <para>states 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the states value.</para>
    /// \endif
    /// </summary>
    private readonly ConcurrentDictionary<string, CameraRuntimeState> _states = new();

    /// <summary>
    /// \if KO
    /// <para>State Changed 상황이 발생할 때 알립니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when state changed takes place.</para>
    /// \endif
    /// </summary>
    public event EventHandler<CameraRuntimeState>? StateChanged;

    /// <summary>
    /// \if KO
    /// <para>State 값을 가져옵니다.</para>
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
    public CameraRuntimeState GetState(string cameraId)
    {
        return _states.GetOrAdd(cameraId, id => new CameraRuntimeState { CameraId = id });
    }

    /// <summary>
    /// \if KO
    /// <para>All 값을 가져옵니다.</para>
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
    public IReadOnlyList<CameraRuntimeState> GetAll()
    {
        return _states.Values.OrderBy(state => state.CameraId).ToArray();
    }

    /// <summary>
    /// \if KO
    /// <para>State 값을 설정합니다.</para>
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
    public void SetState(string cameraId, CameraConnectionState state, string message, string? error = null)
    {
        CameraRuntimeState runtimeState = GetState(cameraId);

        // Connecting은 새 HLS 출력 세션의 시작으로 봅니다.
        // StopAll → StartAll 후 playlist URL이 같아도 Blazor/hls.js가 새 세션을 구분할 수 있게
        // RestartCount를 세션 버전으로 함께 사용합니다.
        if (state == CameraConnectionState.Connecting && runtimeState.State != CameraConnectionState.Connecting)
        {
            runtimeState.RestartCount++;
        }

        runtimeState.State = state;
        runtimeState.LastMessage = message;
        runtimeState.LastError = error;
        runtimeState.LastUpdated = DateTimeOffset.Now;
        StateChanged?.Invoke(this, runtimeState);
    }

    /// <summary>
    /// \if KO
    /// <para>Increment Restart 작업을 수행합니다.</para>
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
    public void IncrementRestart(string cameraId, string message)
    {
        CameraRuntimeState runtimeState = GetState(cameraId);
        runtimeState.RestartCount++;
        runtimeState.LastMessage = message;
        runtimeState.LastUpdated = DateTimeOffset.Now;
        StateChanged?.Invoke(this, runtimeState);
    }
}
