using System;
using System.Collections.ObjectModel;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Interfaces;
using Dreamine.MVVM.ViewModels;

namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// \if KO
/// <para>Controls Demo Row 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>A single row of sample data for the DataGrid/ListBox demos.</para>
/// \endif
/// </summary>
public class ControlsDemoRow
{
    /// <summary>
    /// \if KO
    /// <para>No 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the no value.</para>
    /// \endif
    /// </summary>
    public int No { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the name value.</para>
    /// \endif
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Status 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the status value.</para>
    /// \endif
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// \if KO
/// <para>Converters 탭(EnumToString/EnumToVisibility/EnumDescription 등)에서 쓰는 샘플 enum.</para>
/// \endif
/// \if EN
/// <para>Encapsulates demo status functionality and related state.</para>
/// \endif
/// </summary>
public enum DemoStatus
{
    /// <summary>
    /// \if KO
    /// <para>Idle 값을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the idle value.</para>
    /// \endif
    /// </summary>
    [System.ComponentModel.Description("대기 중")]
    Idle,
    /// <summary>
    /// \if KO
    /// <para>Running 값을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the running value.</para>
    /// \endif
    /// </summary>
    [System.ComponentModel.Description("실행 중")]
    Running,
    /// <summary>
    /// \if KO
    /// <para>Error 값을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the error value.</para>
    /// \endif
    /// </summary>
    [System.ComponentModel.Description("오류 발생")]
    Error
}

/// <summary>
/// \if KO
/// <para>Controls View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Controls showcase ViewModel. Platform-independent. Reusable across WPF, WinForms, Blazor, and MAUI. Actual behavior lives in <see cref="ControlsEvent"/>; pure UI-only state (no business logic attached) uses [DreamineProperty] directly here.</para>
/// \endif
/// </summary>
public partial class ControlsViewModel : ViewModelBase, IActivatable, IVisibilityAware
{
    // ── ViewSwitcher.NotifyShown/NotifyHidden 데모 ──────────
    /// <summary>
    /// \if KO
    /// <para>activation Log 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the activation log value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty]
    private string _activationLog = "(탭을 떠났다가 다시 들어오면 로그가 쌓입니다)";

    /// <summary>
    /// \if KO
    /// <para>Activate 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the activate operation.</para>
    /// \endif
    /// </summary>
    void IActivatable.Activate() => ActivationLog = $"[{DateTime.Now:HH:mm:ss}] Activate()";
    /// <summary>
    /// \if KO
    /// <para>Deactivate 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the deactivate operation.</para>
    /// \endif
    /// </summary>
    void IActivatable.Deactivate() => ActivationLog = $"[{DateTime.Now:HH:mm:ss}] Deactivate()";
    /// <summary>
    /// \if KO
    /// <para>Shown 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the shown event or state change.</para>
    /// \endif
    /// </summary>
    void IVisibilityAware.OnShown() => ActivationLog = $"[{DateTime.Now:HH:mm:ss}] OnShown()";
    /// <summary>
    /// \if KO
    /// <para>Hidden 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the hidden event or state change.</para>
    /// \endif
    /// </summary>
    void IVisibilityAware.OnHidden() => ActivationLog = $"[{DateTime.Now:HH:mm:ss}] OnHidden()";


    /// <summary>
    /// \if KO
    /// <para>event 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private ControlsEvent _event;

    // ── Button ────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Click Count 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the click count value.</para>
    /// \endif
    /// </summary>
    public int ClickCount => Event.ClickCount;

    /// <summary>
    /// \if KO
    /// <para>Activity Log 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the activity log value.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<string> ActivityLog => Event.ActivityLog;

    /// <summary>
    /// \if KO
    /// <para>Click Me 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the click me operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ClickMe")]
    private partial void ClickMe();

    // ── CheckBox (순수 UI 상태; 커맨드 로직 없음) ───────────
    /// <summary>
    /// \if KO
    /// <para>check1 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the check1 value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty]
    private bool _check1 = true;

    /// <summary>
    /// \if KO
    /// <para>check2 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the check2 value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty]
    private bool _check2;

    /// <summary>
    /// \if KO
    /// <para>check3 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the check3 value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty]
    private bool _check3;

    // ── RadioButton ───────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Selected Radio 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the selected radio value.</para>
    /// \endif
    /// </summary>
    public string SelectedRadio => Event.SelectedRadio;

    /// <summary>
    /// \if KO
    /// <para>RelayCommand&lt;string&gt;는 매개변수가 있어 [DreamineCommand]로 생성할 수 없어 직접 작성한다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the select radio command value.</para>
    /// \endif
    /// </summary>
    public RelayCommand<string> SelectRadioCommand { get; }

    // ── CheckLed ──────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Led Is On 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the led is on value.</para>
    /// \endif
    /// </summary>
    public bool LedIsOn => Event.LedIsOn;
    /// <summary>
    /// \if KO
    /// <para>Led Is Pulse 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the led is pulse value.</para>
    /// \endif
    /// </summary>
    public bool LedIsPulse => Event.LedIsPulse;

    /// <summary>
    /// \if KO
    /// <para>Toggle Led 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the toggle led operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ToggleLed")]
    private partial void ToggleLed();

    /// <summary>
    /// \if KO
    /// <para>Toggle Pulse 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the toggle pulse operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.TogglePulse")]
    private partial void TogglePulse();

    // ── TextBox / PasswordBox ─────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Text Input 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the text input value.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Password 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the password value.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Clear Text 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear text operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ClearText")]
    private partial void ClearText();

    /// <summary>
    /// \if KO
    /// <para>Clear Password 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear password operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ClearPassword")]
    private partial void ClearPassword();

    // ── ComboBox (순수 UI 상태) ─────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Fruit Items 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the fruit items value.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<string> FruitItems { get; } =
        new() { "Apple", "Banana", "Cherry", "Grape", "Mango", "Melon" };

    /// <summary>
    /// \if KO
    /// <para>selected Fruit 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the selected fruit value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty]
    private string _selectedFruit = "Cherry";

    // ── Expander (순수 UI 상태) ─────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>is Expanded 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the is expanded value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty]
    private bool _isExpanded = true;

    // ── TimeSpinner (WPF-only; ignored by WinForms; 순수 UI 상태) ──
    /// <summary>
    /// \if KO
    /// <para>time 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the time value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty]
    private TimeSpan _time = new(9, 30, 0);

    // ── DataGrid (정적 시연 데이터) ─────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Grid Rows 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the grid rows value.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<ControlsDemoRow> GridRows { get; } = new()
    {
        new ControlsDemoRow { No = 1, Name = "Device A", Status = "Running" },
        new ControlsDemoRow { No = 2, Name = "Device B", Status = "Stopped" },
        new ControlsDemoRow { No = 3, Name = "Device C", Status = "Running" },
        new ControlsDemoRow { No = 4, Name = "Device D", Status = "Error" },
    };

    // ── ListBox 활성화(더블클릭) ───────────────────────────
    /// <summary>
    /// \if KO
    /// <para>List Box Activated Command 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the list box activated command value.</para>
    /// \endif
    /// </summary>
    public RelayCommand<string> ListBoxActivatedCommand { get; }

    // ── NumericRangeBehavior (순수 UI 상태) ─────────────────
    /// <summary>
    /// \if KO
    /// <para>numeric Input 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the numeric input value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty]
    private double _numericInput = 50;

    // ── Status ────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Status Message 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the status message value.</para>
    /// \endif
    /// </summary>
    public string StatusMessage => Event.StatusMessage;

    // ── Converters 탭 전용 샘플 데이터 ──────────────────────
    /// <summary>
    /// \if KO
    /// <para>demo Enum 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the demo enum value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty]
    private DemoStatus _demoEnum = DemoStatus.Running;

    /// <summary>
    /// \if KO
    /// <para>고정된 시연용 시각(앱 시작 시점).</para>
    /// \endif
    /// \if EN
    /// <para>Gets the demo date time value.</para>
    /// \endif
    /// </summary>
    public DateTime DemoDateTime { get; } = DateTime.Now;

    /// <summary>
    /// \if KO
    /// <para>BooleanConverter("1"/"0" 문자열) 데모용.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the demo flag string value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty]
    private string _demoFlagString = "1";

    /// <summary>
    /// \if KO
    /// <para>NullableToBoolConverter / NullToVisibilityConverter 데모용(처음엔 null).</para>
    /// \endif
    /// \if EN
    /// <para>Stores the demo nullable value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty]
    private object _demoNullable = null!;

    /// <summary>
    /// \if KO
    /// <para>BoolPickNameConverter(MultiBinding) 데모용 두 이름.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the demo wire name value.</para>
    /// \endif
    /// </summary>
    public string DemoWireName => "Wire-001";
    /// <summary>
    /// \if KO
    /// <para>Demo Em Name 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the demo em name value.</para>
    /// \endif
    /// </summary>
    public string DemoEmName => "EM-205";

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="ControlsViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="ControlsViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>ControlsEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ControlsEvent</c> value used for event.</para>
    /// \endif
    /// </param>
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
