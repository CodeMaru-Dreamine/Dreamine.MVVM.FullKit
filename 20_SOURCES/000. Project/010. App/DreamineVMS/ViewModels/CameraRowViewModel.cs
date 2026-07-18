using DreamineVMS.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace DreamineVMS.ViewModels;

/// <summary>
/// \if KO
/// <para>\brief WPF 카메라 ListBox 한 행에 대응하는 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates camera row view model functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>CameraDevice(설정 데이터)와 CameraRuntimeState(실시간 상태)를 한 데 묶어 XAML에서 단일 바인딩 소스로 사용합니다.</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
public sealed class CameraRowViewModel : INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// \if KO
    /// <para>runtime State 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the runtime state value.</para>
    /// \endif
    /// </summary>
    private readonly CameraRuntimeState _runtimeState;
    /// <summary>
    /// \if KO
    /// <para>is Disposed 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the is disposed value.</para>
    /// \endif
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    /// \if KO
    /// <para>\brief CameraRowViewModel 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="CameraRowViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="device">
    /// \if KO
    /// <para>카메라 장치 정보입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraDevice</c> value used for device.</para>
    /// \endif
    /// </param>
    /// <param name="runtimeState">
    /// \if KO
    /// <para>카메라 런타임 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraRuntimeState</c> value used for runtime state.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public CameraRowViewModel(CameraDevice device, CameraRuntimeState runtimeState)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));

        _runtimeState.PropertyChanged += OnRuntimeStatePropertyChanged;
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 속성 변경 이벤트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when property changed takes place.</para>
    /// \endif
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// \if KO
    /// <para>\brief 원본 카메라 장치 정보입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the device value.</para>
    /// \endif
    /// </summary>
    public CameraDevice Device { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라 ID입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the id value.</para>
    /// \endif
    /// </summary>
    public string Id => Device.Id;

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the name value.</para>
    /// \endif
    /// </summary>
    public string Name => Device.Name;

    /// <summary>
    /// \if KO
    /// <para>\brief RTSP URL입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the rtsp url value.</para>
    /// \endif
    /// </summary>
    public string RtspUrl => Device.RtspUrl;

    /// <summary>
    /// \if KO
    /// <para>\brief HLS URL입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the hls url value.</para>
    /// \endif
    /// </summary>
    public string HlsUrl => Device.HlsUrl;

    /// <summary>
    /// \if KO
    /// <para>\brief 현재 연결 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the state value.</para>
    /// \endif
    /// </summary>
    public CameraConnectionState State => _runtimeState.State;

    /// <summary>
    /// \if KO
    /// <para>\brief 상태 텍스트(예: Connected)입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the state text value.</para>
    /// \endif
    /// </summary>
    public string StateText => _runtimeState.State.ToString();

    /// <summary>
    /// \if KO
    /// <para>\brief 상태에 따른 표시 색입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the state brush value.</para>
    /// \endif
    /// </summary>
    public Brush StateBrush => _runtimeState.State switch
    {
        CameraConnectionState.Connected => new SolidColorBrush(Color.FromRgb(0x22, 0xc5, 0x5e)),
        CameraConnectionState.Connecting => new SolidColorBrush(Color.FromRgb(0xf5, 0x9e, 0x0b)),
        CameraConnectionState.Faulted => new SolidColorBrush(Color.FromRgb(0xef, 0x44, 0x44)),
        _ => new SolidColorBrush(Color.FromRgb(0x6b, 0x72, 0x80))
    };

    /// <summary>
    /// \if KO
    /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Releases resources owned by this instance.</para>
    /// \endif
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _runtimeState.PropertyChanged -= OnRuntimeStatePropertyChanged;
    }

    /// <summary>
    /// \if KO
    /// <para>Runtime State Property Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the runtime state property changed event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnRuntimeStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // RuntimeState의 어떤 속성이 바뀌어도 행 표시에 영향이 있으므로 일괄 알림.
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(StateText));
        OnPropertyChanged(nameof(StateBrush));
    }

    /// <summary>
    /// \if KO
    /// <para>Property Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the property changed event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="propertyName">
    /// \if KO
    /// <para>property Name에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for property name.</para>
    /// \endif
    /// </param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
