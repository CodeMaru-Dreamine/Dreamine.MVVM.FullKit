namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// Contains the UI-independent behavior for the light bulb sample.
/// </summary>
public sealed class LightBulbEvent
{
    private readonly LightBulbModel _model;

    public event EventHandler? Changed;

    public LightBulbEvent(LightBulbModel model)
    {
        _model = model;
    }

    public bool IsOn => _model.IsOn;
    public int ToggleCount => _model.ToggleCount;

    public void Toggle()
    {
        _model.IsOn = !_model.IsOn;
        _model.ToggleCount++;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
