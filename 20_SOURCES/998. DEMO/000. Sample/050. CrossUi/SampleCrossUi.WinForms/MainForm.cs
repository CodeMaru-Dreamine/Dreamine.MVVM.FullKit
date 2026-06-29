using System.ComponentModel;
using System.Linq;
using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;
using SampleCrossUi.Shared.ViewModels;
using SampleCrossUi.WinForms.Pages;

namespace SampleCrossUi.WinForms;

public sealed class MainForm : Form
{
    // ── Designer-visible fields ──────────────────────────
    private Panel _pageHost = null!;
    private Panel _navPanel = null!;
    private FlowLayoutPanel _navFlow = null!;
    private DreamineButton _btnCounter = null!;
    private DreamineButton _btnControls = null!;
    private DreamineButton _btnPopup = null!;

    // ── Runtime-only fields ──────────────────────────────
    private DreamineButton[] _navButtons = null!;
    private UserControl? _currentPage;
    private CounterPage _counterPage = null!;
    private ControlsPage _controlsPage = null!;
    private PopupPage _popupPage = null!;

    /// <summary>VS WinForms 디자이너용 기본 생성자.</summary>
    public MainForm() : this(new CounterViewModel(new CounterEvent(new SampleCrossUi.Shared.Services.CounterService()))) { }

    public MainForm(CounterViewModel counterVm)
    {
        InitializeComponent();

        Resize += (_, _) => CenterNavFlow();
        Load += (_, _) => CenterNavFlow();

        _navButtons = [_btnCounter, _btnControls, _btnPopup];
        for (int i = 0; i < _navButtons.Length; i++)
        {
            var idx = i;
            _navButtons[i].Click += (_, _) => Navigate(idx);
        }

        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;

        _counterPage  = new CounterPage(counterVm);
        _controlsPage = new ControlsPage(new ControlsViewModel(new ControlsEvent()));
        _popupPage    = new PopupPage();

        Navigate(0);
    }

    private void Navigate(int index)
    {
        for (int i = 0; i < _navButtons.Length; i++)
            _navButtons[i].IsSelected = i == index;

        _currentPage?.Hide();

        _currentPage = index switch
        {
            0 => _counterPage,
            1 => _controlsPage,
            2 => _popupPage,
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

    private void InitializeComponent()
    {
        _btnCounter  = new DreamineButton();
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

    /// <summary>네비게이션 바 버튼들을 가운데로 정렬한다(WPF DreamineNavigationBar는 자체적으로 처리하지만,
    /// WinForms FlowLayoutPanel은 가운데 정렬을 직접 지원하지 않아 패딩으로 흉내낸다).</summary>
    private void CenterNavFlow()
    {
        var totalWidth = _navFlow.Controls.Cast<Control>().Sum(c => c.Width + c.Margin.Horizontal);
        var offset = Math.Max(0, (_navPanel.Width - totalWidth) / 2);
        _navFlow.Padding = new Padding(offset, 0, 0, 0);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _counterPage?.Dispose();
            _controlsPage?.Dispose();
            _popupPage?.Dispose();
        }
        base.Dispose(disposing);
    }
}
