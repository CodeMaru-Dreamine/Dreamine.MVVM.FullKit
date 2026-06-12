using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SampleCrossUi.WinForms.ViewModels;

public class ControlsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ── Button ────────────────────────────────────────────
    private int _clickCount;
    public int ClickCount { get => _clickCount; set => Set(ref _clickCount, value); }

    // ── CheckBox ──────────────────────────────────────────
    private bool _check1 = true;
    private bool _check2;
    private bool _check3;
    public bool Check1 { get => _check1; set => Set(ref _check1, value); }
    public bool Check2 { get => _check2; set => Set(ref _check2, value); }
    public bool Check3 { get => _check3; set => Set(ref _check3, value); }

    // ── RadioButton ───────────────────────────────────────
    private string _selectedRadio = "Option A";
    public string SelectedRadio { get => _selectedRadio; set => Set(ref _selectedRadio, value); }

    // ── CheckLed ──────────────────────────────────────────
    private bool _ledIsOn = true;
    private bool _ledIsPulse;
    public bool LedIsOn { get => _ledIsOn; set => Set(ref _ledIsOn, value); }
    public bool LedIsPulse { get => _ledIsPulse; set => Set(ref _ledIsPulse, value); }

    // ── TextBox / PasswordBox ─────────────────────────────
    private string _textInput = string.Empty;
    private string _password = string.Empty;
    public string TextInput { get => _textInput; set => Set(ref _textInput, value); }
    public string Password  { get => _password;  set => Set(ref _password, value); }

    // ── ComboBox ──────────────────────────────────────────
    public string[] FruitItems { get; } =
        ["Apple", "Banana", "Cherry", "Grape", "Mango", "Melon"];

    private string _selectedFruit = "Cherry";
    public string SelectedFruit { get => _selectedFruit; set => Set(ref _selectedFruit, value); }

    // ── Expander ─────────────────────────────────────────
    private bool _isExpanded = true;
    public bool IsExpanded { get => _isExpanded; set => Set(ref _isExpanded, value); }

    // ── Status ────────────────────────────────────────────
    private string _statusMessage = "Ready";
    public string StatusMessage { get => _statusMessage; set => Set(ref _statusMessage, value); }

    // ── Actions ───────────────────────────────────────────
    public void ClickMe()
    {
        ClickCount++;
        StatusMessage = $"Button clicked {ClickCount} time(s)";
    }

    public void SelectRadio(string option)
    {
        SelectedRadio = option;
        StatusMessage = $"Radio selected: {SelectedRadio}";
    }

    public void ToggleLed()
    {
        LedIsOn = !LedIsOn;
        StatusMessage = $"LED is {(LedIsOn ? "ON" : "OFF")}";
    }

    public void TogglePulse()
    {
        LedIsPulse = !LedIsPulse;
        StatusMessage = $"Pulse is {(LedIsPulse ? "ON" : "OFF")}";
    }

    public void ClearText()
    {
        TextInput = string.Empty;
        StatusMessage = "TextBox cleared";
    }

    public void ClearPassword()
    {
        Password = string.Empty;
        StatusMessage = "Password cleared";
    }
}
