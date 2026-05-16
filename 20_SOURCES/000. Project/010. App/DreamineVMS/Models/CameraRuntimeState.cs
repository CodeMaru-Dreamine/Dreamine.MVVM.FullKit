using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DreamineVMS.Models;

/// <summary>
/// \brief 카메라 런타임 상태입니다.
/// </summary>
public sealed class CameraRuntimeState : INotifyPropertyChanged
{
    private CameraConnectionState _state = CameraConnectionState.Disconnected;
    private string _lastMessage = "Ready.";
    private DateTimeOffset _lastUpdated = DateTimeOffset.Now;
    private string? _lastError;
    private int _restartCount;

    /// <summary>\brief 속성 변경 이벤트입니다.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>\brief 카메라 고유 식별자입니다.</summary>
    public required string CameraId { get; init; }

    /// <summary>\brief 연결 상태입니다.</summary>
    public CameraConnectionState State
    {
        get => _state;
        set => SetField(ref _state, value);
    }

    /// <summary>\brief 마지막 상태 메시지입니다.</summary>
    public string LastMessage
    {
        get => _lastMessage;
        set => SetField(ref _lastMessage, value);
    }

    /// <summary>\brief 마지막 상태 갱신 시각입니다.</summary>
    public DateTimeOffset LastUpdated
    {
        get => _lastUpdated;
        set => SetField(ref _lastUpdated, value);
    }

    /// <summary>\brief 마지막 오류 메시지입니다.</summary>
    public string? LastError
    {
        get => _lastError;
        set => SetField(ref _lastError, value);
    }

    /// <summary>\brief 재시작 횟수입니다.</summary>
    public int RestartCount
    {
        get => _restartCount;
        set => SetField(ref _restartCount, value);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
