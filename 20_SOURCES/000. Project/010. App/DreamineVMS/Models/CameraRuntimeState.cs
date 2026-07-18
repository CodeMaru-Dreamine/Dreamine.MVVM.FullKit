using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DreamineVMS.Models;

/// <summary>
/// \if KO
/// <para>\brief 카메라 런타임 상태입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates camera runtime state functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CameraRuntimeState : INotifyPropertyChanged
{
    /// <summary>
    /// \if KO
    /// <para>state 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the state value.</para>
    /// \endif
    /// </summary>
    private CameraConnectionState _state = CameraConnectionState.Disconnected;
    /// <summary>
    /// \if KO
    /// <para>last Message 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the last message value.</para>
    /// \endif
    /// </summary>
    private string _lastMessage = "Ready.";
    /// <summary>
    /// \if KO
    /// <para>last Updated 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the last updated value.</para>
    /// \endif
    /// </summary>
    private DateTimeOffset _lastUpdated = DateTimeOffset.Now;
    /// <summary>
    /// \if KO
    /// <para>last Error 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the last error value.</para>
    /// \endif
    /// </summary>
    private string? _lastError;
    /// <summary>
    /// \if KO
    /// <para>restart Count 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the restart count value.</para>
    /// \endif
    /// </summary>
    private int _restartCount;

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
    /// <para>\brief 카메라 고유 식별자입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the camera id value.</para>
    /// \endif
    /// </summary>
    public required string CameraId { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 연결 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the state value.</para>
    /// \endif
    /// </summary>
    public CameraConnectionState State
    {
        get => _state;
        set => SetField(ref _state, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 마지막 상태 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the last message value.</para>
    /// \endif
    /// </summary>
    public string LastMessage
    {
        get => _lastMessage;
        set => SetField(ref _lastMessage, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 마지막 상태 갱신 시각입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the last updated value.</para>
    /// \endif
    /// </summary>
    public DateTimeOffset LastUpdated
    {
        get => _lastUpdated;
        set => SetField(ref _lastUpdated, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 마지막 오류 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the last error value.</para>
    /// \endif
    /// </summary>
    public string? LastError
    {
        get => _lastError;
        set => SetField(ref _lastError, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 재시작 횟수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the restart count value.</para>
    /// \endif
    /// </summary>
    public int RestartCount
    {
        get => _restartCount;
        set => SetField(ref _restartCount, value);
    }

    /// <summary>
    /// \if KO
    /// <para>Field 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the field value.</para>
    /// \endif
    /// </summary>
    /// <typeparam name="T">
    /// \if KO
    /// <para>T 형식 매개변수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The T type parameter.</para>
    /// \endif
    /// </typeparam>
    /// <param name="field">
    /// \if KO
    /// <para>field에 사용할 <c>T</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>T</c> value used for field.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <param name="propertyName">
    /// \if KO
    /// <para>property Name에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for property name.</para>
    /// \endif
    /// </param>
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
