using DreamineVMS.Models;
using System.Collections.Concurrent;

namespace DreamineVMS.Services.Runtime;

/// <summary>
/// \brief 메모리 기반 카메라 런타임 상태 관리 서비스입니다.
/// </summary>
public sealed class CameraRuntimeStateService : ICameraRuntimeStateService
{
    private readonly ConcurrentDictionary<string, CameraRuntimeState> _states = new();

    /// <inheritdoc />
    public event EventHandler<CameraRuntimeState>? StateChanged;

    /// <inheritdoc />
    public CameraRuntimeState GetState(string cameraId)
    {
        return _states.GetOrAdd(cameraId, id => new CameraRuntimeState { CameraId = id });
    }

    /// <inheritdoc />
    public IReadOnlyList<CameraRuntimeState> GetAll()
    {
        return _states.Values.OrderBy(state => state.CameraId).ToArray();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void IncrementRestart(string cameraId, string message)
    {
        CameraRuntimeState runtimeState = GetState(cameraId);
        runtimeState.RestartCount++;
        runtimeState.LastMessage = message;
        runtimeState.LastUpdated = DateTimeOffset.Now;
        StateChanged?.Invoke(this, runtimeState);
    }
}
