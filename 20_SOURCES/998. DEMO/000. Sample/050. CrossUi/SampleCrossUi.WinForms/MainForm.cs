using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;
using SampleCrossUi.Shared.ViewModels;
using SampleCrossUi.WinForms.Pages;
using SampleCrossUi.WinForms.ViewModels;

namespace SampleCrossUi.WinForms;

public sealed class MainForm : Form
{
    private readonly Panel _pageHost;
    private readonly DreamineButton[] _navButtons;
    private UserControl? _currentPage;

    private readonly CounterPage  _counterPage;
    private readonly ControlsPage _controlsPage;
    private readonly PopupPage    _popupPage;

    public MainForm(CounterViewModel counterVm)
    {
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

        // ── Pages ────────────────────────────────────────
        _counterPage  = new CounterPage(counterVm);
        _controlsPage = new ControlsPage(new ControlsViewModel());
        _popupPage    = new PopupPage();

        Controls.Add(_pageHost);
        Controls.Add(nav);

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _counterPage.Dispose();
            _controlsPage.Dispose();
            _popupPage.Dispose();
        }
        base.Dispose(disposing);
    }
}
