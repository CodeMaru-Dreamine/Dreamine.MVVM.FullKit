using System.ComponentModel;
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
    private Label _appTitle = null!;
    private Panel _navDivider = null!;
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
    public MainForm() : this(new CounterViewModel(new SampleCrossUi.Shared.Services.CounterService())) { }

    public MainForm(CounterViewModel counterVm)
    {
        InitializeComponent();

        _navButtons = [_btnCounter, _btnControls, _btnPopup];
        for (int i = 0; i < _navButtons.Length; i++)
        {
            var idx = i;
            _navButtons[i].Click += (_, _) => Navigate(idx);
        }

        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;

        _counterPage  = new CounterPage(counterVm);
        _controlsPage = new ControlsPage(new ControlsViewModel());
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
        _appTitle    = new Label();
        _navDivider  = new Panel();
        _btnCounter  = new DreamineButton();
        _btnControls = new DreamineButton();
        _btnPopup    = new DreamineButton();
        _navFlow     = new FlowLayoutPanel();
        _navPanel    = new Panel();
        _pageHost    = new Panel();

        SuspendLayout();

        // _appTitle
        _appTitle.Text      = "Dreamine\nCross-UI";
        _appTitle.ForeColor = Color.White;
        _appTitle.Font      = new Font("Segoe UI", 13f, FontStyle.Bold, GraphicsUnit.Point);
        _appTitle.Dock      = DockStyle.Top;
        _appTitle.Height    = 64;
        _appTitle.TextAlign = ContentAlignment.MiddleCenter;
        _appTitle.Padding   = new Padding(8, 8, 8, 0);

        // _navDivider
        _navDivider.Dock      = DockStyle.Top;
        _navDivider.Height    = 1;
        _navDivider.BackColor = DreamineTheme.BorderNormal;

        // _btnCounter
        _btnCounter.Content      = "Counter";
        _btnCounter.Width        = 172;
        _btnCounter.Height       = 44;
        _btnCounter.CornerRadius = 6;
        _btnCounter.Margin       = new Padding(4, 2, 4, 2);
        _btnCounter.Name         = "_btnCounter";

        // _btnControls
        _btnControls.Content      = "Controls";
        _btnControls.Width        = 172;
        _btnControls.Height       = 44;
        _btnControls.CornerRadius = 6;
        _btnControls.Margin       = new Padding(4, 2, 4, 2);
        _btnControls.Name         = "_btnControls";

        // _btnPopup
        _btnPopup.Content      = "Popup";
        _btnPopup.Width        = 172;
        _btnPopup.Height       = 44;
        _btnPopup.CornerRadius = 6;
        _btnPopup.Margin       = new Padding(4, 2, 4, 2);
        _btnPopup.Name         = "_btnPopup";

        // _navFlow
        _navFlow.Dock            = DockStyle.Fill;
        _navFlow.FlowDirection   = FlowDirection.TopDown;
        _navFlow.WrapContents    = false;
        _navFlow.BackColor       = DreamineTheme.CardBackground;
        _navFlow.Controls.Add(_btnCounter);
        _navFlow.Controls.Add(_btnControls);
        _navFlow.Controls.Add(_btnPopup);

        // _navPanel
        _navPanel.Dock      = DockStyle.Left;
        _navPanel.Width     = 180;
        _navPanel.BackColor = DreamineTheme.CardBackground;
        _navPanel.Padding   = new Padding(0, 8, 0, 8);
        _navPanel.Controls.Add(_navFlow);
        _navPanel.Controls.Add(_navDivider);
        _navPanel.Controls.Add(_appTitle);

        // _pageHost
        _pageHost.Dock      = DockStyle.Fill;
        _pageHost.BackColor = DreamineTheme.AppBackground;

        // MainForm
        Text             = "Dreamine Cross-UI — WinForms";
        ClientSize       = new Size(900, 660);
        MinimumSize      = new Size(900, 600);
        StartPosition    = FormStartPosition.CenterScreen;
        BackColor        = DreamineTheme.AppBackground;
        Name             = "MainForm";
        Controls.Add(_pageHost);
        Controls.Add(_navPanel);

        ResumeLayout(false);
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
