using System.ComponentModel;
using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.WinForms.Pages;

public sealed class ControlsPage : UserControl
{
    private readonly ControlsViewModel _vm;
    private readonly Label _statusLabel = null!;

    /// <summary>VS WinForms 디자이너용 기본 생성자.</summary>
    public ControlsPage() : this(new ControlsViewModel()) { }

    public ControlsPage(ControlsViewModel vm)
    {
        _vm = vm;
        BackColor = DreamineTheme.AppBackground;
        Dock = DockStyle.Fill;
        Padding = new Padding(16);

        var title = new Label
        {
            Text = "Controls Showcase",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold, GraphicsUnit.Point),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
        };

        // Tab control
        var tabs = new DreamineTabControl
        {
            Width = 680,
            Height = 420,
            Margin = new Padding(0, 0, 0, 8),
        };

        tabs.TabPages.Add(BuildButtonTab());
        tabs.TabPages.Add(BuildCheckRadioTab());
        tabs.TabPages.Add(BuildLedTab());
        tabs.TabPages.Add(BuildTextBoxTab());
        tabs.TabPages.Add(BuildComboBoxTab());
        tabs.TabPages.Add(BuildMiscTab());

        // Status bar
        _statusLabel = new Label
        {
            Text = _vm.StatusMessage,
            ForeColor = DreamineTheme.TextSecondary,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
            AutoSize = true,
        };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
        };
        layout.Controls.Add(title);
        layout.Controls.Add(tabs);
        layout.Controls.Add(_statusLabel);
        Controls.Add(layout);

        _vm.PropertyChanged += OnPropertyChanged;
    }

    // ── Tab builders ─────────────────────────────────────

    private TabPage BuildButtonTab()
    {
        var page = DarkTab("Button");
        var panel = DarkFlow(page);

        CardTitle(panel, "DreamineButton");
        var btn = new DreamineButton { Content = "Click Me", Width = 140, Height = 40 };
        var countLabel = new Label
        {
            Text = $"Clicks: {_vm.ClickCount}",
            ForeColor = DreamineTheme.TextPrimary,
            Font = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point),
            AutoSize = true,
        };
        btn.Click += (_, _) => { _vm.ClickMeCommand.Execute(null); countLabel.Text = $"Clicks: {_vm.ClickCount}"; };
        panel.Controls.Add(btn);
        Spacer(panel);
        panel.Controls.Add(countLabel);
        return page;
    }

    private TabPage BuildCheckRadioTab()
    {
        var page = DarkTab("CheckBox / Radio");
        var panel = DarkFlow(page);

        CardTitle(panel, "DreamineCheckBox");
        var cb1 = new DreamineCheckBox { Content = "Option 1", IsChecked = _vm.Check1, Width = 180, Height = 28 };
        var cb2 = new DreamineCheckBox { Content = "Option 2", IsChecked = _vm.Check2, Width = 180, Height = 28 };
        var cb3 = new DreamineCheckBox { Content = "Option 3", IsChecked = _vm.Check3, Width = 180, Height = 28 };
        cb1.CheckedChanged += (_, _) => _vm.Check1 = cb1.IsChecked;
        cb2.CheckedChanged += (_, _) => _vm.Check2 = cb2.IsChecked;
        cb3.CheckedChanged += (_, _) => _vm.Check3 = cb3.IsChecked;
        panel.Controls.Add(cb1); panel.Controls.Add(cb2); panel.Controls.Add(cb3);

        Spacer(panel);
        CardTitle(panel, "DreamineRadioButton");
        var radioPanel = new Panel { Width = 500, Height = 80, BackColor = DreamineTheme.CardBackground };
        var rb1 = new DreamineRadioButton { Content = "Option A", GroupName = "demo", IsChecked = _vm.SelectedRadio == "Option A", Width = 160, Height = 28 };
        var rb2 = new DreamineRadioButton { Content = "Option B", GroupName = "demo", IsChecked = _vm.SelectedRadio == "Option B", Width = 160, Height = 28, Left = 160 };
        var rb3 = new DreamineRadioButton { Content = "Option C", GroupName = "demo", IsChecked = _vm.SelectedRadio == "Option C", Width = 160, Height = 28, Left = 320 };
        radioPanel.Controls.Add(rb1); radioPanel.Controls.Add(rb2); radioPanel.Controls.Add(rb3);
        rb1.CheckedChanged += (_, _) => { if (rb1.IsChecked) _vm.SelectRadioCommand.Execute("Option A"); };
        rb2.CheckedChanged += (_, _) => { if (rb2.IsChecked) _vm.SelectRadioCommand.Execute("Option B"); };
        rb3.CheckedChanged += (_, _) => { if (rb3.IsChecked) _vm.SelectRadioCommand.Execute("Option C"); };
        panel.Controls.Add(radioPanel);
        return page;
    }

    private TabPage BuildLedTab()
    {
        var page = DarkTab("CheckLed");
        var panel = DarkFlow(page);

        CardTitle(panel, "DreamineCheckLed");
        var led = new DreamineCheckLed { IsOn = _vm.LedIsOn, IsPulse = _vm.LedIsPulse, Width = 60, Height = 60 };

        var btnToggle = new DreamineButton { Content = "Toggle ON/OFF", Width = 160, Height = 36, Margin = new Padding(0, 0, 8, 0) };
        var btnPulse = new DreamineButton { Content = "Toggle Pulse", Width = 140, Height = 36 };

        btnToggle.Click += (_, _) => { _vm.ToggleLedCommand.Execute(null); led.IsOn = _vm.LedIsOn; };
        btnPulse.Click += (_, _) => { _vm.TogglePulseCommand.Execute(null); led.IsPulse = _vm.LedIsPulse; };

        panel.Controls.Add(led);
        Spacer(panel);
        var btnRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        btnRow.Controls.Add(btnToggle); btnRow.Controls.Add(btnPulse);
        panel.Controls.Add(btnRow);
        return page;
    }

    private TabPage BuildTextBoxTab()
    {
        var page = DarkTab("TextBox");
        var panel = DarkFlow(page);

        CardTitle(panel, "DreamineTextBox");
        var tb = new DreamineTextBox { Hint = "텍스트를 입력하세요…", Width = 360, Height = 36 };
        var btnClear = new DreamineButton { Content = "Clear", Width = 80, Height = 36, Margin = new Padding(8, 0, 0, 0) };
        var tbRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        tbRow.Controls.Add(tb); tbRow.Controls.Add(btnClear);
        tb.TextChanged += (_, _) => _vm.TextInput = tb.Text;
        btnClear.Click += (_, _) => { _vm.ClearTextCommand.Execute(null); tb.Text = string.Empty; };
        panel.Controls.Add(tbRow);

        Spacer(panel);
        CardTitle(panel, "DreaminePasswordBox");
        var pb = new DreaminePasswordBox { Hint = "비밀번호 입력…", Width = 360, Height = 36 };
        var btnClrPw = new DreamineButton { Content = "Clear", Width = 80, Height = 36, Margin = new Padding(8, 0, 0, 0) };
        var pbRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        pbRow.Controls.Add(pb); pbRow.Controls.Add(btnClrPw);
        btnClrPw.Click += (_, _) => { _vm.ClearPasswordCommand.Execute(null); pb.Password = string.Empty; };
        panel.Controls.Add(pbRow);
        return page;
    }

    private TabPage BuildComboBoxTab()
    {
        var page = DarkTab("ComboBox");
        var panel = DarkFlow(page);

        CardTitle(panel, "DreamineComboBox");
        var combo = new DreamineComboBox { Width = 240, Height = 36 };
        foreach (var item in _vm.FruitItems) combo.Items.Add(item);
        combo.SelectedItem = _vm.SelectedFruit;

        var selLabel = new Label
        {
            Text = $"Selected: {_vm.SelectedFruit}",
            ForeColor = DreamineTheme.TextPrimary,
            Font = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point),
            AutoSize = true,
        };
        combo.SelectedIndexChanged += (_, _) =>
        {
            _vm.SelectedFruit = combo.SelectedItem?.ToString() ?? string.Empty;
            selLabel.Text = $"Selected: {_vm.SelectedFruit}";
        };
        panel.Controls.Add(combo);
        Spacer(panel);
        panel.Controls.Add(selLabel);
        return page;
    }

    private TabPage BuildMiscTab()
    {
        var page = DarkTab("Misc");
        var panel = DarkFlow(page);

        CardTitle(panel, "DreamineExpander");
        var exp = new DreamineExpander { Header = "Expander 섹션", IsExpanded = _vm.IsExpanded, Width = 500, Height = 120 };
        var inner = new Label
        {
            Text = "이 안에 어떤 컨트롤이든 배치할 수 있습니다.",
            ForeColor = DreamineTheme.TextPrimary,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
            AutoSize = true,
        };
        exp.Content.Controls.Add(inner);
        exp.ExpandedChanged += (_, _) => _vm.IsExpanded = exp.IsExpanded;
        panel.Controls.Add(exp);
        return page;
    }

    // ── Helpers ──────────────────────────────────────────

    private static TabPage DarkTab(string text)
        => new(text) { BackColor = DreamineTheme.AppBackground, ForeColor = Color.White };

    private static FlowLayoutPanel DarkFlow(TabPage page)
    {
        var p = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = false,
            WrapContents = false,
            Padding = new Padding(16),
            BackColor = DreamineTheme.AppBackground,
        };
        page.Controls.Add(p);
        return p;
    }

    private static void CardTitle(FlowLayoutPanel panel, string text)
        => panel.Controls.Add(new Label
        {
            Text = text,
            ForeColor = DreamineTheme.AccentBlue,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point),
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 4),
        });

    private static void Spacer(FlowLayoutPanel panel)
        => panel.Controls.Add(new Label { Height = 12, Width = 1, AutoSize = false });

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ControlsViewModel.StatusMessage))
            _statusLabel.Text = _vm.StatusMessage;
    }

    private void InitializeComponent()
    {

    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.PropertyChanged -= OnPropertyChanged;
        base.Dispose(disposing);
    }
}
