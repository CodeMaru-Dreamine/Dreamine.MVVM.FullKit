using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.IoTabs;

/// <summary>
/// \if KO
/// <para>Io Point State 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Represents a 16-point digital I/O sample point.</para>
/// \endif
/// </summary>
public sealed class IoPointState : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>value 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the value value.</para>
    /// \endif
    /// </summary>
    private bool _value;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="IoPointState"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="IoPointState"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="module">
    /// \if KO
    /// <para>module에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The module index.</para>
    /// \endif
    /// </param>
    /// <param name="channel">
    /// \if KO
    /// <para>channel에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The channel index.</para>
    /// \endif
    /// </param>
    /// <param name="name">
    /// \if KO
    /// <para>name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The display name.</para>
    /// \endif
    /// </param>
    public IoPointState(int module, int channel, string name)
    {
        Module = module;
        Channel = channel;
        Name = name;
    }

    /// <summary>
    /// \if KO
    /// <para>Module 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the module index.</para>
    /// \endif
    /// </summary>
    public int Module { get; }

    /// <summary>
    /// \if KO
    /// <para>Channel 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the channel index.</para>
    /// \endif
    /// </summary>
    public int Channel { get; }

    /// <summary>
    /// \if KO
    /// <para>Name 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the display name.</para>
    /// \endif
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// \if KO
    /// <para>Value 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the point value.</para>
    /// \endif
    /// </summary>
    public bool Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(ValueText));
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Value Text 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the point value as ON/OFF text.</para>
    /// \endif
    /// </summary>
    public string ValueText => Value ? "ON" : "OFF";
}
