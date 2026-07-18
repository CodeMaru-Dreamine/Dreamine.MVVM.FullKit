using System.ComponentModel;
using System.Linq;
using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;
using SampleCrossUi.Shared.ViewModels;
using SampleCrossUi.WinForms.Pages;

namespace SampleCrossUi.WinForms;

/// <summary>
/// \if KO
/// <para>Main Form 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates main form functionality and related state.</para>
/// \endif
/// </summary>
public sealed class MainForm : Form
{
    // ── Designer-visible fields ──────────────────────────
    /// <summary>
    /// \if KO
    /// <para>page Host 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the page host value.</para>
    /// \endif
    /// </summary>
    private Panel _pageHost = null!;
    /// <summary>
    /// \if KO
    /// <para>nav Panel 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the nav panel value.</para>
    /// \endif
    /// </summary>
    private Panel _navPanel = null!;
    /// <summary>
    /// \if KO
    /// <para>nav Flow 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the nav flow value.</para>
    /// \endif
    /// </summary>
    private FlowLayoutPanel _navFlow = null!;
    /// <summary>
    /// \if KO
    /// <para>btn Counter 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the btn counter value.</para>
    /// \endif
    /// </summary>
    private DreamineButton _btnCounter = null!;
    /// <summary>
    /// \if KO
    /// <para>btn Light Bulb 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the btn light bulb value.</para>
    /// \endif
    /// </summary>
    private DreamineButton _btnLightBulb = null!;
    /// <summary>
    /// \if KO
    /// <para>btn Controls 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the btn controls value.</para>
    /// \endif
    /// </summary>
    private DreamineButton _btnControls = null!;
    /// <summary>
    /// \if KO
    /// <para>btn Popup 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the btn popup value.</para>
    /// \endif
    /// </summary>
    private DreamineButton _btnPopup = null!;

    // ── Runtime-only fields ──────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>nav Buttons 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the nav buttons value.</para>
    /// \endif
    /// </summary>
    private DreamineButton[] _navButtons = null!;
    /// <summary>
    /// \if KO
    /// <para>current Page 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current page value.</para>
    /// \endif
    /// </summary>
    private UserControl? _currentPage;
    /// <summary>
    /// \if KO
    /// <para>counter Page 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the counter page value.</para>
    /// \endif
    /// </summary>
    private CounterPage _counterPage = null!;
    /// <summary>
    /// \if KO
    /// <para>light Bulb Page 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the light bulb page value.</para>
    /// \endif
    /// </summary>
    private LightBulbPage _lightBulbPage = null!;
    /// <summary>
    /// \if KO
    /// <para>controls Page 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the controls page value.</para>
    /// \endif
    /// </summary>
    private ControlsPage _controlsPage = null!;
    /// <summary>
    /// \if KO
    /// <para>popup Page 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the popup page value.</para>
    /// \endif
    /// </summary>
    private PopupPage _popupPage = null!;

    /// <summary>
    /// \if KO
    /// <para>VS WinForms 디자이너용 기본 생성자.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="MainForm"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public MainForm() : this(
        new CounterViewModel(new CounterEvent(new SampleCrossUi.Shared.Services.CounterService())),
        new LightBulbViewModel(new LightBulbEvent(new LightBulbModel()))) { }

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="MainForm"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="MainForm"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="counterVm">
    /// \if KO
    /// <para>counter Vm에 사용할 <c>CounterViewModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CounterViewModel</c> value used for counter vm.</para>
    /// \endif
    /// </param>
    /// <param name="lightBulbVm">
    /// \if KO
    /// <para>light Bulb Vm에 사용할 <c>LightBulbViewModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>LightBulbViewModel</c> value used for light bulb vm.</para>
    /// \endif
    /// </param>
    public MainForm(CounterViewModel counterVm, LightBulbViewModel lightBulbVm)
    {
        InitializeComponent();

        Resize += (_, _) => CenterNavFlow();
        Load += (_, _) => CenterNavFlow();

        _navButtons = [_btnCounter, _btnLightBulb, _btnControls, _btnPopup];
        for (int i = 0; i < _navButtons.Length; i++)
        {
            var idx = i;
            _navButtons[i].Click += (_, _) => Navigate(idx);
        }

        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;

        _counterPage   = new CounterPage(counterVm);
        _lightBulbPage = new LightBulbPage(lightBulbVm);
        _controlsPage  = new ControlsPage(new ControlsViewModel(new ControlsEvent()));
        _popupPage     = new PopupPage();

        Navigate(0);
    }

    /// <summary>
    /// \if KO
    /// <para>Navigate 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the navigate operation.</para>
    /// \endif
    /// </summary>
    /// <param name="index">
    /// \if KO
    /// <para>index에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for index.</para>
    /// \endif
    /// </param>
    private void Navigate(int index)
    {
        for (int i = 0; i < _navButtons.Length; i++)
            _navButtons[i].IsSelected = i == index;

        _currentPage?.Hide();

        _currentPage = index switch
        {
            0 => _counterPage,
            1 => _lightBulbPage,
            2 => _controlsPage,
            3 => _popupPage,
            _ => _counterPage,
        };

        if (!_pageHost.Controls.Contains(_currentPage))
        {
            _currentPage.Dock = DockStyle.Fill;
            _pageHost.Controls.Add(_currentPage);
        }
        _currentPage.Show();
        _currentPage.BringToFront();
    }

    /// <summary>
    /// \if KO
    /// <para>Initialize Component 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the initialize component operation.</para>
    /// \endif
    /// </summary>
    private void InitializeComponent()
    {
        _btnCounter  = new DreamineButton();
        _btnLightBulb = new DreamineButton();
        _btnControls = new DreamineButton();
        _btnPopup    = new DreamineButton();
        _navFlow     = new FlowLayoutPanel();
        _navPanel    = new Panel();
        _pageHost    = new Panel();

        SuspendLayout();

        // _btnCounter — WPF DreamineNavigationBar 버튼과 동일한 크기/배치(가로 나열)
        _btnCounter.Content      = "Counter";
        _btnCounter.Width        = 140;
        _btnCounter.Height       = 40;
        _btnCounter.CornerRadius = 6;
        _btnCounter.Margin       = new Padding(4);
        _btnCounter.Name         = "_btnCounter";

        // _btnLightBulb
        _btnLightBulb.Content      = "Light Bulb";
        _btnLightBulb.Width        = 140;
        _btnLightBulb.Height       = 40;
        _btnLightBulb.CornerRadius = 6;
        _btnLightBulb.Margin       = new Padding(4);
        _btnLightBulb.Name         = "_btnLightBulb";

        // _btnControls
        _btnControls.Content      = "Controls";
        _btnControls.Width        = 140;
        _btnControls.Height       = 40;
        _btnControls.CornerRadius = 6;
        _btnControls.Margin       = new Padding(4);
        _btnControls.Name         = "_btnControls";

        // _btnPopup
        _btnPopup.Content      = "Popup";
        _btnPopup.Width        = 140;
        _btnPopup.Height       = 40;
        _btnPopup.CornerRadius = 6;
        _btnPopup.Margin       = new Padding(4);
        _btnPopup.Name         = "_btnPopup";

        // _navFlow — WPF처럼 가로로 나열되는 네비게이션 버튼들
        _navFlow.Dock            = DockStyle.Fill;
        _navFlow.FlowDirection   = FlowDirection.LeftToRight;
        _navFlow.WrapContents    = false;
        _navFlow.Anchor          = AnchorStyles.None;
        _navFlow.AutoSize        = true;
        _navFlow.Controls.Add(_btnCounter);
        _navFlow.Controls.Add(_btnLightBulb);
        _navFlow.Controls.Add(_btnControls);
        _navFlow.Controls.Add(_btnPopup);

        // _navPanel — 상단 가로 네비게이션 바(WPF MainWindow의 DreamineNavigationBar 행과 동일한 위치)
        _navPanel.Dock      = DockStyle.Top;
        _navPanel.Height    = 64;
        _navPanel.BackColor = DreamineTheme.NavBackground;
        _navPanel.Padding   = new Padding(5);
        _navPanel.Controls.Add(_navFlow);

        // _pageHost
        _pageHost.Dock      = DockStyle.Fill;
        _pageHost.BackColor = DreamineTheme.AppBackground;

        // MainForm
        Text             = "Dreamine Cross-UI — WinForms";
        ClientSize       = new Size(1100, 860);
        MinimumSize      = new Size(960, 700);
        StartPosition    = FormStartPosition.CenterScreen;
        BackColor        = DreamineTheme.AppBackground;
        Name             = "MainForm";
        Controls.Add(_pageHost);
        Controls.Add(_navPanel);

        ResumeLayout(false);
    }

    /// <summary>
    /// \if KO
    /// <para>네비게이션 바 버튼들을 가운데로 정렬한다(WPF DreamineNavigationBar는 자체적으로 처리하지만, WinForms FlowLayoutPanel은 가운데 정렬을 직접 지원하지 않아 패딩으로 흉내낸다).</para>
    /// \endif
    /// \if EN
    /// <para>Performs the center nav flow operation.</para>
    /// \endif
    /// </summary>
    private void CenterNavFlow()
    {
        var totalWidth = _navFlow.Controls.Cast<Control>().Sum(c => c.Width + c.Margin.Horizontal);
        var offset = Math.Max(0, (_navPanel.Width - totalWidth) / 2);
        _navFlow.Padding = new Padding(offset, 0, 0, 0);
    }

    /// <summary>
    /// \if KO
    /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Releases resources owned by this instance.</para>
    /// \endif
    /// </summary>
    /// <param name="disposing">
    /// \if KO
    /// <para>disposing에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for disposing.</para>
    /// \endif
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _counterPage?.Dispose();
            _lightBulbPage?.Dispose();
            _controlsPage?.Dispose();
            _popupPage?.Dispose();
        }
        base.Dispose(disposing);
    }
}
