using System.Collections.ObjectModel;
using Dreamine.MVVM.ViewModels;

namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// Controls showcase ViewModel. Platform-independent.
/// Reusable across WPF, WinForms, Blazor, and MAUI.
/// </summary>
public class ControlsViewModel : ViewModelBase
{
    // ── Button ────────────────────────────────────────────
    private int _clickCount;
    public int ClickCount { get => _clickCount; set => SetProperty(ref _clickCount, value); }

    public RelayCommand ClickMeCommand { get; }

    // ── CheckBox ──────────────────────────────────────────
    private bool _check1 = true;
    private bool _check2;
    private bool _check3;
    public bool Check1 { get => _check1; set => SetProperty(ref _check1, value); }
    public bool Check2 { get => _check2; set => SetProperty(ref _check2, value); }
    public bool Check3 { get => _check3; set => SetProperty(ref _check3, value); }

    // ── RadioButton ───────────────────────────────────────
    private string _selectedRadio = "Option A";
    public string SelectedRadio { get => _selectedRadio; set => SetProperty(ref _selectedRadio, value); }

    public RelayCommand<string> SelectRadioCommand { get; }

    // ── CheckLed ──────────────────────────────────────────
    private bool _ledIsOn = true;
    private bool _ledIsPulse;
    public bool LedIsOn  { get => _ledIsOn;   set => SetProperty(ref _ledIsOn,   value); }
    public bool LedIsPulse { get => _ledIsPulse; set => SetProperty(ref _ledIsPulse, value); }

    public RelayCommand ToggleLedCommand   { get; }
    public RelayCommand TogglePulseCommand { get; }

    // ── TextBox / PasswordBox ─────────────────────────────
    private string _textInput = string.Empty;
    private string _password  = string.Empty;
    public string TextInput { get => _textInput; set => SetProperty(ref _textInput, value); }
    public string Password  { get => _password;  set => SetProperty(ref _password,  value); }

    public RelayCommand ClearTextCommand     { get; }
    public RelayCommand ClearPasswordCommand { get; }

    // ── ComboBox ──────────────────────────────────────────
    public ObservableCollection<string> FruitItems { get; } =
        new() { "Apple", "Banana", "Cherry", "Grape", "Mango", "Melon" };

    private string? _selectedFruit = "Cherry";
    public string? SelectedFruit { get => _selectedFruit; set => SetProperty(ref _selectedFruit, value); }

    // ── Expander ─────────────────────────────────────────
    private bool _isExpanded = true;
    public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }

    // ── TimeSpinner (WPF용; WinForms는 무시) ─────────────
    private TimeSpan _time = new(9, 30, 0);
    public TimeSpan Time { get => _time; set => SetProperty(ref _time, value); }

    // ── Status ────────────────────────────────────────────
    private string _statusMessage = "Ready";
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public ControlsViewModel()
    {
        ClickMeCommand = new RelayCommand(() =>
        {
            ClickCount++;
            StatusMessage = $"Button clicked {ClickCount} time(s)";
        });

        SelectRadioCommand = new RelayCommand<string>(option =>
        {
            SelectedRadio = option ?? string.Empty;
            StatusMessage = $"Radio selected: {SelectedRadio}";
        });

        ToggleLedCommand = new RelayCommand(() =>
        {
            LedIsOn = !LedIsOn;
            StatusMessage = $"LED is {(LedIsOn ? "ON" : "OFF")}";
        });

        TogglePulseCommand = new RelayCommand(() =>
        {
            LedIsPulse = !LedIsPulse;
            StatusMessage = $"Pulse is {(LedIsPulse ? "ON" : "OFF")}";
        });

        ClearTextCommand = new RelayCommand(() =>
        {
            TextInput = string.Empty;
            StatusMessage = "TextBox cleared";
        });

        ClearPasswordCommand = new RelayCommand(() =>
        {
            Password = string.Empty;
            StatusMessage = "Password cleared";
        });
    }
}
