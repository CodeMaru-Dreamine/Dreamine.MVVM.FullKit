using DreamineVMS.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace DreamineVMS.ViewModels;

/// <summary>
/// \brief WPF 카메라 ListBox 한 행에 대응하는 ViewModel입니다.
/// </summary>
/// <remarks>
/// CameraDevice(설정 데이터)와 CameraRuntimeState(실시간 상태)를 한 데 묶어
/// XAML에서 단일 바인딩 소스로 사용합니다.
/// </remarks>
public sealed class CameraRowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly CameraRuntimeState _runtimeState;
    private bool _isDisposed;

    /// <summary>
    /// \brief CameraRowViewModel 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="device">카메라 장치 정보입니다.</param>
    /// <param name="runtimeState">카메라 런타임 상태입니다.</param>
    public CameraRowViewModel(CameraDevice device, CameraRuntimeState runtimeState)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));

        _runtimeState.PropertyChanged += OnRuntimeStatePropertyChanged;
    }

    /// <summary>\brief 속성 변경 이벤트입니다.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>\brief 원본 카메라 장치 정보입니다.</summary>
    public CameraDevice Device { get; }

    /// <summary>\brief 카메라 ID입니다.</summary>
    public string Id => Device.Id;

    /// <summary>\brief 카메라 이름입니다.</summary>
    public string Name => Device.Name;

    /// <summary>\brief RTSP URL입니다.</summary>
    public string RtspUrl => Device.RtspUrl;

    /// <summary>\brief HLS URL입니다.</summary>
    public string HlsUrl => Device.HlsUrl;

    /// <summary>\brief 현재 연결 상태입니다.</summary>
    public CameraConnectionState State => _runtimeState.State;

    /// <summary>\brief 상태 텍스트(예: Connected)입니다.</summary>
    public string StateText => _runtimeState.State.ToString();

    /// <summary>\brief 상태에 따른 표시 색입니다.</summary>
    public Brush StateBrush => _runtimeState.State switch
    {
        CameraConnectionState.Connected => new SolidColorBrush(Color.FromRgb(0x22, 0xc5, 0x5e)),
        CameraConnectionState.Connecting => new SolidColorBrush(Color.FromRgb(0xf5, 0x9e, 0x0b)),
        CameraConnectionState.Faulted => new SolidColorBrush(Color.FromRgb(0xef, 0x44, 0x44)),
        _ => new SolidColorBrush(Color.FromRgb(0x6b, 0x72, 0x80))
    };

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _runtimeState.PropertyChanged -= OnRuntimeStatePropertyChanged;
    }

    private void OnRuntimeStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // RuntimeState의 어떤 속성이 바뀌어도 행 표시에 영향이 있으므로 일괄 알림.
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(StateText));
        OnPropertyChanged(nameof(StateBrush));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
