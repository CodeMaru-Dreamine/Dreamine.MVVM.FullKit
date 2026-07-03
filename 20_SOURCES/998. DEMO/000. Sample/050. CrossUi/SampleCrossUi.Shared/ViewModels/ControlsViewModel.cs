using System;
using System.Collections.ObjectModel;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Interfaces;
using Dreamine.MVVM.ViewModels;

namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// A single row of sample data for the DataGrid/ListBox demos.
/// </summary>
public class ControlsDemoRow
{
    public int No { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>Converters 탭(EnumToString/EnumToVisibility/EnumDescription 등)에서 쓰는 샘플 enum.</summary>
public enum DemoStatus
{
    [System.ComponentModel.Description("대기 중")]
    Idle,
    [System.ComponentModel.Description("실행 중")]
    Running,
    [System.ComponentModel.Description("오류 발생")]
    Error
}

/// <summary>
/// Controls showcase ViewModel. Platform-independent.
/// Reusable across WPF, WinForms, Blazor, and MAUI.
/// Actual behavior lives in <see cref="ControlsEvent"/>; pure UI-only state
/// (no business logic attached) uses [DreamineProperty] directly here.
/// </summary>
public partial class ControlsViewModel : ViewModelBase, IActivatable, IVisibilityAware
{
    // ── ViewSwitcher.NotifyShown/NotifyHidden 데모 ──────────
    [DreamineProperty]
    private string _activationLog = "(탭을 떠났다가 다시 들어오면 로그가 쌓입니다)";

    void IActivatable.Activate() => ActivationLog = $"[{DateTime.Now:HH:mm:ss}] Activate()";
    void IActivatable.Deactivate() => ActivationLog = $"[{DateTime.Now:HH:mm:ss}] Deactivate()";
    void IVisibilityAware.OnShown() => ActivationLog = $"[{DateTime.Now:HH:mm:ss}] OnShown()";
    void IVisibilityAware.OnHidden() => ActivationLog = $"[{DateTime.Now:HH:mm:ss}] OnHidden()";


    [DreamineEvent]
    private ControlsEvent _event;

    // ── Button ────────────────────────────────────────────
    public int ClickCount => Event.ClickCount;

    public ObservableCollection<string> ActivityLog => Event.ActivityLog;

    [DreamineCommand("Event.ClickMe")]
    private partial void ClickMe();

    // ── CheckBox (순수 UI 상태; 커맨드 로직 없음) ───────────
    [DreamineProperty]
    private bool _check1 = true;

    [DreamineProperty]
    private bool _check2;

    [DreamineProperty]
    private bool _check3;

    // ── RadioButton ───────────────────────────────────────
    public string SelectedRadio => Event.SelectedRadio;

    /// <summary>RelayCommand&lt;string&gt;는 매개변수가 있어 [DreamineCommand]로 생성할 수 없어 직접 작성한다.</summary>
    public RelayCommand<string> SelectRadioCommand { get; }

    // ── CheckLed ──────────────────────────────────────────
    public bool LedIsOn => Event.LedIsOn;
    public bool LedIsPulse => Event.LedIsPulse;

    [DreamineCommand("Event.ToggleLed")]
    private partial void ToggleLed();

    [DreamineCommand("Event.TogglePulse")]
    private partial void TogglePulse();

    // ── TextBox / PasswordBox ─────────────────────────────
    public string TextInput
    {
        get => Event.TextInput;
        set
        {
            if (Event.TextInput == value) return;
            Event.TextInput = value;
            OnPropertyChanged(nameof(TextInput));
        }
    }

    public string Password
    {
        get => Event.Password;
        set
        {
            if (Event.Password == value) return;
            Event.Password = value;
            OnPropertyChanged(nameof(Password));
        }
    }

    [DreamineCommand("Event.ClearText")]
    private partial void ClearText();

    [DreamineCommand("Event.ClearPassword")]
    private partial void ClearPassword();

    // ── ComboBox (순수 UI 상태) ─────────────────────────────
    public ObservableCollection<string> FruitItems { get; } =
        new() { "Apple", "Banana", "Cherry", "Grape", "Mango", "Melon" };

    [DreamineProperty]
    private string _selectedFruit = "Cherry";

    // ── Expander (순수 UI 상태) ─────────────────────────────
    [DreamineProperty]
    private bool _isExpanded = true;

    // ── TimeSpinner (WPF-only; ignored by WinForms; 순수 UI 상태) ──
    [DreamineProperty]
    private TimeSpan _time = new(9, 30, 0);

    // ── DataGrid (정적 시연 데이터) ─────────────────────────
    public ObservableCollection<ControlsDemoRow> GridRows { get; } = new()
    {
        new ControlsDemoRow { No = 1, Name = "Device A", Status = "Running" },
        new ControlsDemoRow { No = 2, Name = "Device B", Status = "Stopped" },
        new ControlsDemoRow { No = 3, Name = "Device C", Status = "Running" },
        new ControlsDemoRow { No = 4, Name = "Device D", Status = "Error" },
    };

    // ── ListBox 활성화(더블클릭) ───────────────────────────
    public RelayCommand<string> ListBoxActivatedCommand { get; }

    // ── NumericRangeBehavior (순수 UI 상태) ─────────────────
    [DreamineProperty]
    private double _numericInput = 50;

    // ── Status ────────────────────────────────────────────
    public string StatusMessage => Event.StatusMessage;

    // ── Converters 탭 전용 샘플 데이터 ──────────────────────
    [DreamineProperty]
    private DemoStatus _demoEnum = DemoStatus.Running;

    /// <summary>고정된 시연용 시각(앱 시작 시점).</summary>
    public DateTime DemoDateTime { get; } = DateTime.Now;

    /// <summary>BooleanConverter("1"/"0" 문자열) 데모용.</summary>
    [DreamineProperty]
    private string _demoFlagString = "1";

    /// <summary>NullableToBoolConverter / NullToVisibilityConverter 데모용(처음엔 null).</summary>
    [DreamineProperty]
    private object _demoNullable = null!;

    /// <summary>BoolPickNameConverter(MultiBinding) 데모용 두 이름.</summary>
    public string DemoWireName => "Wire-001";
    public string DemoEmName => "EM-205";

    public ControlsViewModel(ControlsEvent @event)
    {
        _event = @event;

        Event.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(ClickCount));
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(SelectedRadio));
            OnPropertyChanged(nameof(LedIsOn));
            OnPropertyChanged(nameof(LedIsPulse));
            OnPropertyChanged(nameof(TextInput));
            OnPropertyChanged(nameof(Password));
        };

        SelectRadioCommand = new RelayCommand<string>(option => Event.SelectRadio(option));
        ListBoxActivatedCommand = new RelayCommand<string>(item => Event.ListBoxActivated(item));
    }
}
