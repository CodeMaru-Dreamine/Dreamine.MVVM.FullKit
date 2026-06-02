using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.IoTabs;

/// <summary>
/// Represents a 16-point digital I/O sample point.
/// </summary>
public sealed class IoPointState : ViewModelBase
{
    private bool _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="IoPointState"/> class.
    /// </summary>
    /// <param name="module">The module index.</param>
    /// <param name="channel">The channel index.</param>
    /// <param name="name">The display name.</param>
    public IoPointState(int module, int channel, string name)
    {
        Module = module;
        Channel = channel;
        Name = name;
    }

    /// <summary>
    /// Gets the module index.
    /// </summary>
    public int Module { get; }

    /// <summary>
    /// Gets the channel index.
    /// </summary>
    public int Channel { get; }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the point value.
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
    /// Gets the point value as ON/OFF text.
    /// </summary>
    public string ValueText => Value ? "ON" : "OFF";
}
