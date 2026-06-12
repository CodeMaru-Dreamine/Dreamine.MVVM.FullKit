using System.ComponentModel;
using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;
using SampleCrossUi.Shared.ViewModels;
using SampleCrossUi.WinForms.Pages;

namespace SampleCrossUi.WinForms;

public sealed class MainForm : Form
{
    private readonly Panel _pageHost;
    private readonly DreamineButton[] _navButtons;
    private UserControl? _currentPage;

    private readonly CounterPage _counterPage = null!;
    private readonly ControlsPage _controlsPage = null!;
    private readonly PopupPage _popupPage = null!;

    /// <summary>VS WinForms 디자이너용 기본 생성자.</summary>
    public MainForm() : this(new CounterViewModel(new SampleCrossUi.Shared.Services.CounterService())) { }

    public MainForm(CounterViewModel counterVm)
    {
        InitializeComponent();   // 디자이너 필수 — 없으면 디자이너가 폼을 인식하지 못함

        Text = "Dreamine Cross-UI — WinForms";
        ClientSize = new Size(900, 660);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = DreamineTheme.AppBackground;

        // ── Nav sidebar ─────────────────────────────────
        var nav = new Panel
        {
            Dock = DockStyle.Left,
            Width = 180,
            BackColor = DreamineTheme.CardBackground,
            Padding = new Padding(0, 8, 0, 8),
        };

        var appTitle = new Label
        {
            Text = "Dreamine\nCross-UI",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold, GraphicsUnit.Point),
            Dock = DockStyle.Top,
            Height = 64,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(8, 8, 8, 0),
        };

        var divider = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = DreamineTheme.BorderNormal,
            Margin = new Padding(0, 4, 0, 4),
        };

        string[] navLabels = ["Counter", "Controls", "Popup"];
        _navButtons = new DreamineButton[3];

        for (int i = 0; i < navLabels.Length; i++)
        {
            var btn = new DreamineButton
            {
                Content = navLabels[i],
                Width = 172,
                Height = 44,
                CornerRadius = 6,
                Margin = new Padding(4, 2, 4, 2),
                Tag = i,
            };
            _navButtons[i] = btn;
            var idx = i;
            btn.Click += (_, _) => Navigate(idx);
        }

        var navFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = DreamineTheme.CardBackground,
        };
        foreach (var b in _navButtons) navFlow.Controls.Add(b);

        nav.Controls.Add(navFlow);
        nav.Controls.Add(divider);
        nav.Controls.Add(appTitle);

        // ── Page host ────────────────────────────────────
        _pageHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = DreamineTheme.AppBackground,
        };

        // 레이아웃은 항상 추가 — 디자이너에서 폼 골격을 볼 수 있도록
        Controls.Add(_pageHost);
        Controls.Add(nav);

        // 디자이너에서는 페이지 생성 생략 (복잡한 초기화가 디자인 타임에 실패할 수 있음)
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;

        // ── Pages ────────────────────────────────────────
        _counterPage = new CounterPage(counterVm);
        _controlsPage = new ControlsPage(new ControlsViewModel());
        _popupPage = new PopupPage();

        Navigate(0);
    }

    private void Navigate(int index)
    {
        // update selection highlight
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
        SuspendLayout();
        // 
        // MainForm
        // 
        ClientSize = new Size(284, 261);
        Name = "MainForm";
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
