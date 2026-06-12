using System.ComponentModel;
using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.WinForms.Pages;

public sealed class ControlsPage : UserControl
{
    // ── top-level ─────────────────────────────────────────
    private Label              _title       = null!;
    private DreamineTabControl _tabs        = null!;
    private Label              _statusLabel = null!;
    private FlowLayoutPanel    _layout      = null!;

    // ── Button tab ────────────────────────────────────────
    private TabPage         _tabButton     = null!;
    private FlowLayoutPanel _tabButtonFlow = null!;
    private Label           _lblButtonCard = null!;
    private DreamineButton  _btnClickMe    = null!;
    private Label           _lblClickCount = null!;

    // ── CheckBox/Radio tab ────────────────────────────────
    private TabPage           _tabCheckRadio     = null!;
    private FlowLayoutPanel   _tabCheckRadioFlow = null!;
    private Label             _lblCbCard         = null!;
    private DreamineCheckBox  _cb1               = null!;
    private DreamineCheckBox  _cb2               = null!;
    private DreamineCheckBox  _cb3               = null!;
    private Label             _lblRbCard         = null!;
    private Panel             _radioPanel        = null!;
    private DreamineRadioButton _rb1             = null!;
    private DreamineRadioButton _rb2             = null!;
    private DreamineRadioButton _rb3             = null!;

    // ── CheckLed tab ──────────────────────────────────────
    private TabPage         _tabLed      = null!;
    private FlowLayoutPanel _tabLedFlow  = null!;
    private Label           _lblLedCard  = null!;
    private DreamineCheckLed _led        = null!;
    private FlowLayoutPanel  _ledBtnRow  = null!;
    private DreamineButton   _btnToggle  = null!;
    private DreamineButton   _btnPulse   = null!;

    // ── TextBox tab ───────────────────────────────────────
    private TabPage         _tabText     = null!;
    private FlowLayoutPanel _tabTextFlow = null!;
    private Label           _lblTbCard   = null!;
    private FlowLayoutPanel _tbRow       = null!;
    private DreamineTextBox _tb          = null!;
    private DreamineButton  _btnClearTb  = null!;
    private Label           _lblPbCard   = null!;
    private FlowLayoutPanel _pbRow       = null!;
    private DreaminePasswordBox _pb      = null!;
    private DreamineButton  _btnClearPb  = null!;

    // ── ComboBox tab ──────────────────────────────────────
    private TabPage         _tabCombo     = null!;
    private FlowLayoutPanel _tabComboFlow = null!;
    private Label           _lblComboCard = null!;
    private DreamineComboBox _combo       = null!;
    private Label           _lblSelected  = null!;

    // ── Misc tab ──────────────────────────────────────────
    private TabPage         _tabMisc     = null!;
    private FlowLayoutPanel _tabMiscFlow = null!;
    private Label           _lblExpCard  = null!;
    private DreamineExpander _expander   = null!;
    private Label           _lblExpInner = null!;

    private readonly ControlsViewModel _vm;

    /// <summary>VS WinForms 디자이너용 기본 생성자.</summary>
    public ControlsPage() : this(new ControlsViewModel()) { }

    public ControlsPage(ControlsViewModel vm)
    {
        _vm = vm;
        InitializeComponent();

        // ViewModel 초기값 반영
        _cb1.IsChecked  = _vm.Check1;
        _cb2.IsChecked  = _vm.Check2;
        _cb3.IsChecked  = _vm.Check3;
        _rb1.IsChecked  = _vm.SelectedRadio == "Option A";
        _rb2.IsChecked  = _vm.SelectedRadio == "Option B";
        _rb3.IsChecked  = _vm.SelectedRadio == "Option C";
        _led.IsOn       = _vm.LedIsOn;
        _led.IsPulse    = _vm.LedIsPulse;
        _expander.IsExpanded = _vm.IsExpanded;
        foreach (var item in _vm.FruitItems) _combo.Items.Add(item);
        _combo.SelectedItem  = _vm.SelectedFruit;
        _lblSelected.Text    = $"Selected: {_vm.SelectedFruit}";
        _lblClickCount.Text  = $"Clicks: {_vm.ClickCount}";

        // Button tab events
        _btnClickMe.Click += (_, _) => { _vm.ClickMeCommand.Execute(null); _lblClickCount.Text = $"Clicks: {_vm.ClickCount}"; };

        // CheckBox events
        _cb1.CheckedChanged += (_, _) => _vm.Check1 = _cb1.IsChecked;
        _cb2.CheckedChanged += (_, _) => _vm.Check2 = _cb2.IsChecked;
        _cb3.CheckedChanged += (_, _) => _vm.Check3 = _cb3.IsChecked;

        // RadioButton events
        _rb1.CheckedChanged += (_, _) => { if (_rb1.IsChecked) _vm.SelectRadioCommand.Execute("Option A"); };
        _rb2.CheckedChanged += (_, _) => { if (_rb2.IsChecked) _vm.SelectRadioCommand.Execute("Option B"); };
        _rb3.CheckedChanged += (_, _) => { if (_rb3.IsChecked) _vm.SelectRadioCommand.Execute("Option C"); };

        // LED events
        _btnToggle.Click += (_, _) => { _vm.ToggleLedCommand.Execute(null);   _led.IsOn    = _vm.LedIsOn;    };
        _btnPulse.Click  += (_, _) => { _vm.TogglePulseCommand.Execute(null); _led.IsPulse = _vm.LedIsPulse; };

        // TextBox events
        _tb.TextChanged    += (_, _) => _vm.TextInput = _tb.Text;
        _btnClearTb.Click  += (_, _) => { _vm.ClearTextCommand.Execute(null);     _tb.Text       = string.Empty; };
        _btnClearPb.Click  += (_, _) => { _vm.ClearPasswordCommand.Execute(null); _pb.Password   = string.Empty; };

        // ComboBox events
        _combo.SelectedIndexChanged += (_, _) =>
        {
            _vm.SelectedFruit = _combo.SelectedItem?.ToString() ?? string.Empty;
            _lblSelected.Text = $"Selected: {_vm.SelectedFruit}";
        };

        // Expander events
        _expander.ExpandedChanged += (_, _) => _vm.IsExpanded = _expander.IsExpanded;

        _vm.PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ControlsViewModel.StatusMessage))
            _statusLabel.Text = _vm.StatusMessage;
    }

    private void InitializeComponent()
    {
        // ── instantiate all ───────────────────────────────
        _title            = new Label();
        _tabs             = new DreamineTabControl();
        _statusLabel      = new Label();
        _layout           = new FlowLayoutPanel();

        _tabButton        = new TabPage("Button");
        _tabButtonFlow    = new FlowLayoutPanel();
        _lblButtonCard    = new Label();
        _btnClickMe       = new DreamineButton();
        _lblClickCount    = new Label();

        _tabCheckRadio     = new TabPage("CheckBox / Radio");
        _tabCheckRadioFlow = new FlowLayoutPanel();
        _lblCbCard         = new Label();
        _cb1               = new DreamineCheckBox();
        _cb2               = new DreamineCheckBox();
        _cb3               = new DreamineCheckBox();
        _lblRbCard         = new Label();
        _radioPanel        = new Panel();
        _rb1               = new DreamineRadioButton();
        _rb2               = new DreamineRadioButton();
        _rb3               = new DreamineRadioButton();

        _tabLed     = new TabPage("CheckLed");
        _tabLedFlow = new FlowLayoutPanel();
        _lblLedCard = new Label();
        _led        = new DreamineCheckLed();
        _ledBtnRow  = new FlowLayoutPanel();
        _btnToggle  = new DreamineButton();
        _btnPulse   = new DreamineButton();

        _tabText     = new TabPage("TextBox");
        _tabTextFlow = new FlowLayoutPanel();
        _lblTbCard   = new Label();
        _tbRow       = new FlowLayoutPanel();
        _tb          = new DreamineTextBox();
        _btnClearTb  = new DreamineButton();
        _lblPbCard   = new Label();
        _pbRow       = new FlowLayoutPanel();
        _pb          = new DreaminePasswordBox();
        _btnClearPb  = new DreamineButton();

        _tabCombo     = new TabPage("ComboBox");
        _tabComboFlow = new FlowLayoutPanel();
        _lblComboCard = new Label();
        _combo        = new DreamineComboBox();
        _lblSelected  = new Label();

        _tabMisc     = new TabPage("Misc");
        _tabMiscFlow = new FlowLayoutPanel();
        _lblExpCard  = new Label();
        _expander    = new DreamineExpander();
        _lblExpInner = new Label();

        SuspendLayout();

        // ── _title ────────────────────────────────────────
        _title.Text      = "Controls Showcase";
        _title.ForeColor = Color.White;
        _title.Font      = new Font("Segoe UI", 18f, FontStyle.Bold, GraphicsUnit.Point);
        _title.AutoSize  = true;
        _title.Margin    = new Padding(0, 0, 0, 8);
        _title.Name      = "_title";

        // ── _statusLabel ──────────────────────────────────
        _statusLabel.Text      = "Status: —";
        _statusLabel.ForeColor = DreamineTheme.TextSecondary;
        _statusLabel.Font      = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        _statusLabel.AutoSize  = true;
        _statusLabel.Name      = "_statusLabel";

        // ══ Button tab ════════════════════════════════════
        _tabButton.BackColor = DreamineTheme.AppBackground;
        _tabButton.ForeColor = Color.White;
        _tabButton.Name      = "_tabButton";

        _tabButtonFlow.Dock          = DockStyle.Fill;
        _tabButtonFlow.FlowDirection = FlowDirection.TopDown;
        _tabButtonFlow.WrapContents  = false;
        _tabButtonFlow.Padding       = new Padding(16);
        _tabButtonFlow.BackColor     = DreamineTheme.AppBackground;
        _tabButtonFlow.Name          = "_tabButtonFlow";

        _lblButtonCard.Text      = "DreamineButton";
        _lblButtonCard.ForeColor = DreamineTheme.AccentBlue;
        _lblButtonCard.Font      = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
        _lblButtonCard.AutoSize  = true;
        _lblButtonCard.Margin    = new Padding(0, 8, 0, 4);
        _lblButtonCard.Name      = "_lblButtonCard";

        _btnClickMe.Content = "Click Me";
        _btnClickMe.Width   = 140;
        _btnClickMe.Height  = 40;
        _btnClickMe.Name    = "_btnClickMe";

        _lblClickCount.Text      = "Clicks: 0";
        _lblClickCount.ForeColor = DreamineTheme.TextPrimary;
        _lblClickCount.Font      = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point);
        _lblClickCount.AutoSize  = true;
        _lblClickCount.Name      = "_lblClickCount";

        _tabButtonFlow.Controls.Add(_lblButtonCard);
        _tabButtonFlow.Controls.Add(_btnClickMe);
        _tabButtonFlow.Controls.Add(_lblClickCount);
        _tabButton.Controls.Add(_tabButtonFlow);

        // ══ CheckBox/Radio tab ════════════════════════════
        _tabCheckRadio.BackColor = DreamineTheme.AppBackground;
        _tabCheckRadio.ForeColor = Color.White;
        _tabCheckRadio.Name      = "_tabCheckRadio";

        _tabCheckRadioFlow.Dock          = DockStyle.Fill;
        _tabCheckRadioFlow.FlowDirection = FlowDirection.TopDown;
        _tabCheckRadioFlow.WrapContents  = false;
        _tabCheckRadioFlow.Padding       = new Padding(16);
        _tabCheckRadioFlow.BackColor     = DreamineTheme.AppBackground;
        _tabCheckRadioFlow.Name          = "_tabCheckRadioFlow";

        _lblCbCard.Text      = "DreamineCheckBox";
        _lblCbCard.ForeColor = DreamineTheme.AccentBlue;
        _lblCbCard.Font      = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
        _lblCbCard.AutoSize  = true;
        _lblCbCard.Margin    = new Padding(0, 8, 0, 4);
        _lblCbCard.Name      = "_lblCbCard";

        _cb1.Content = "Option 1";
        _cb1.Width   = 180;
        _cb1.Height  = 28;
        _cb1.Name    = "_cb1";

        _cb2.Content = "Option 2";
        _cb2.Width   = 180;
        _cb2.Height  = 28;
        _cb2.Name    = "_cb2";

        _cb3.Content = "Option 3";
        _cb3.Width   = 180;
        _cb3.Height  = 28;
        _cb3.Name    = "_cb3";

        _lblRbCard.Text      = "DreamineRadioButton";
        _lblRbCard.ForeColor = DreamineTheme.AccentBlue;
        _lblRbCard.Font      = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
        _lblRbCard.AutoSize  = true;
        _lblRbCard.Margin    = new Padding(0, 8, 0, 4);
        _lblRbCard.Name      = "_lblRbCard";

        _radioPanel.Width     = 500;
        _radioPanel.Height    = 80;
        _radioPanel.BackColor = DreamineTheme.CardBackground;
        _radioPanel.Name      = "_radioPanel";

        _rb1.Content   = "Option A";
        _rb1.GroupName = "demo";
        _rb1.Width     = 160;
        _rb1.Height    = 28;
        _rb1.Left      = 0;
        _rb1.Name      = "_rb1";

        _rb2.Content   = "Option B";
        _rb2.GroupName = "demo";
        _rb2.Width     = 160;
        _rb2.Height    = 28;
        _rb2.Left      = 160;
        _rb2.Name      = "_rb2";

        _rb3.Content   = "Option C";
        _rb3.GroupName = "demo";
        _rb3.Width     = 160;
        _rb3.Height    = 28;
        _rb3.Left      = 320;
        _rb3.Name      = "_rb3";

        _radioPanel.Controls.Add(_rb1);
        _radioPanel.Controls.Add(_rb2);
        _radioPanel.Controls.Add(_rb3);

        _tabCheckRadioFlow.Controls.Add(_lblCbCard);
        _tabCheckRadioFlow.Controls.Add(_cb1);
        _tabCheckRadioFlow.Controls.Add(_cb2);
        _tabCheckRadioFlow.Controls.Add(_cb3);
        _tabCheckRadioFlow.Controls.Add(_lblRbCard);
        _tabCheckRadioFlow.Controls.Add(_radioPanel);
        _tabCheckRadio.Controls.Add(_tabCheckRadioFlow);

        // ══ CheckLed tab ══════════════════════════════════
        _tabLed.BackColor = DreamineTheme.AppBackground;
        _tabLed.ForeColor = Color.White;
        _tabLed.Name      = "_tabLed";

        _tabLedFlow.Dock          = DockStyle.Fill;
        _tabLedFlow.FlowDirection = FlowDirection.TopDown;
        _tabLedFlow.WrapContents  = false;
        _tabLedFlow.Padding       = new Padding(16);
        _tabLedFlow.BackColor     = DreamineTheme.AppBackground;
        _tabLedFlow.Name          = "_tabLedFlow";

        _lblLedCard.Text      = "DreamineCheckLed";
        _lblLedCard.ForeColor = DreamineTheme.AccentBlue;
        _lblLedCard.Font      = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
        _lblLedCard.AutoSize  = true;
        _lblLedCard.Margin    = new Padding(0, 8, 0, 4);
        _lblLedCard.Name      = "_lblLedCard";

        _led.Width  = 60;
        _led.Height = 60;
        _led.Name   = "_led";

        _btnToggle.Content = "Toggle ON/OFF";
        _btnToggle.Width   = 160;
        _btnToggle.Height  = 36;
        _btnToggle.Margin  = new Padding(0, 0, 8, 0);
        _btnToggle.Name    = "_btnToggle";

        _btnPulse.Content = "Toggle Pulse";
        _btnPulse.Width   = 140;
        _btnPulse.Height  = 36;
        _btnPulse.Name    = "_btnPulse";

        _ledBtnRow.AutoSize      = true;
        _ledBtnRow.FlowDirection = FlowDirection.LeftToRight;
        _ledBtnRow.Name          = "_ledBtnRow";
        _ledBtnRow.Controls.Add(_btnToggle);
        _ledBtnRow.Controls.Add(_btnPulse);

        _tabLedFlow.Controls.Add(_lblLedCard);
        _tabLedFlow.Controls.Add(_led);
        _tabLedFlow.Controls.Add(_ledBtnRow);
        _tabLed.Controls.Add(_tabLedFlow);

        // ══ TextBox tab ═══════════════════════════════════
        _tabText.BackColor = DreamineTheme.AppBackground;
        _tabText.ForeColor = Color.White;
        _tabText.Name      = "_tabText";

        _tabTextFlow.Dock          = DockStyle.Fill;
        _tabTextFlow.FlowDirection = FlowDirection.TopDown;
        _tabTextFlow.WrapContents  = false;
        _tabTextFlow.Padding       = new Padding(16);
        _tabTextFlow.BackColor     = DreamineTheme.AppBackground;
        _tabTextFlow.Name          = "_tabTextFlow";

        _lblTbCard.Text      = "DreamineTextBox";
        _lblTbCard.ForeColor = DreamineTheme.AccentBlue;
        _lblTbCard.Font      = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
        _lblTbCard.AutoSize  = true;
        _lblTbCard.Margin    = new Padding(0, 8, 0, 4);
        _lblTbCard.Name      = "_lblTbCard";

        _tb.Hint   = "텍스트를 입력하세요…";
        _tb.Width  = 360;
        _tb.Height = 36;
        _tb.Name   = "_tb";

        _btnClearTb.Content = "Clear";
        _btnClearTb.Width   = 80;
        _btnClearTb.Height  = 36;
        _btnClearTb.Margin  = new Padding(8, 0, 0, 0);
        _btnClearTb.Name    = "_btnClearTb";

        _tbRow.AutoSize      = true;
        _tbRow.FlowDirection = FlowDirection.LeftToRight;
        _tbRow.Name          = "_tbRow";
        _tbRow.Controls.Add(_tb);
        _tbRow.Controls.Add(_btnClearTb);

        _lblPbCard.Text      = "DreaminePasswordBox";
        _lblPbCard.ForeColor = DreamineTheme.AccentBlue;
        _lblPbCard.Font      = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
        _lblPbCard.AutoSize  = true;
        _lblPbCard.Margin    = new Padding(0, 8, 0, 4);
        _lblPbCard.Name      = "_lblPbCard";

        _pb.Hint   = "비밀번호 입력…";
        _pb.Width  = 360;
        _pb.Height = 36;
        _pb.Name   = "_pb";

        _btnClearPb.Content = "Clear";
        _btnClearPb.Width   = 80;
        _btnClearPb.Height  = 36;
        _btnClearPb.Margin  = new Padding(8, 0, 0, 0);
        _btnClearPb.Name    = "_btnClearPb";

        _pbRow.AutoSize      = true;
        _pbRow.FlowDirection = FlowDirection.LeftToRight;
        _pbRow.Name          = "_pbRow";
        _pbRow.Controls.Add(_pb);
        _pbRow.Controls.Add(_btnClearPb);

        _tabTextFlow.Controls.Add(_lblTbCard);
        _tabTextFlow.Controls.Add(_tbRow);
        _tabTextFlow.Controls.Add(_lblPbCard);
        _tabTextFlow.Controls.Add(_pbRow);
        _tabText.Controls.Add(_tabTextFlow);

        // ══ ComboBox tab ══════════════════════════════════
        _tabCombo.BackColor = DreamineTheme.AppBackground;
        _tabCombo.ForeColor = Color.White;
        _tabCombo.Name      = "_tabCombo";

        _tabComboFlow.Dock          = DockStyle.Fill;
        _tabComboFlow.FlowDirection = FlowDirection.TopDown;
        _tabComboFlow.WrapContents  = false;
        _tabComboFlow.Padding       = new Padding(16);
        _tabComboFlow.BackColor     = DreamineTheme.AppBackground;
        _tabComboFlow.Name          = "_tabComboFlow";

        _lblComboCard.Text      = "DreamineComboBox";
        _lblComboCard.ForeColor = DreamineTheme.AccentBlue;
        _lblComboCard.Font      = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
        _lblComboCard.AutoSize  = true;
        _lblComboCard.Margin    = new Padding(0, 8, 0, 4);
        _lblComboCard.Name      = "_lblComboCard";

        _combo.Width  = 240;
        _combo.Height = 36;
        _combo.Name   = "_combo";

        _lblSelected.Text      = "Selected: —";
        _lblSelected.ForeColor = DreamineTheme.TextPrimary;
        _lblSelected.Font      = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point);
        _lblSelected.AutoSize  = true;
        _lblSelected.Name      = "_lblSelected";

        _tabComboFlow.Controls.Add(_lblComboCard);
        _tabComboFlow.Controls.Add(_combo);
        _tabComboFlow.Controls.Add(_lblSelected);
        _tabCombo.Controls.Add(_tabComboFlow);

        // ══ Misc tab ══════════════════════════════════════
        _tabMisc.BackColor = DreamineTheme.AppBackground;
        _tabMisc.ForeColor = Color.White;
        _tabMisc.Name      = "_tabMisc";

        _tabMiscFlow.Dock          = DockStyle.Fill;
        _tabMiscFlow.FlowDirection = FlowDirection.TopDown;
        _tabMiscFlow.WrapContents  = false;
        _tabMiscFlow.Padding       = new Padding(16);
        _tabMiscFlow.BackColor     = DreamineTheme.AppBackground;
        _tabMiscFlow.Name          = "_tabMiscFlow";

        _lblExpCard.Text      = "DreamineExpander";
        _lblExpCard.ForeColor = DreamineTheme.AccentBlue;
        _lblExpCard.Font      = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
        _lblExpCard.AutoSize  = true;
        _lblExpCard.Margin    = new Padding(0, 8, 0, 4);
        _lblExpCard.Name      = "_lblExpCard";

        _expander.Header = "Expander 섹션";
        _expander.Width  = 500;
        _expander.Height = 120;
        _expander.Name   = "_expander";

        _lblExpInner.Text      = "이 안에 어떤 컨트롤이든 배치할 수 있습니다.";
        _lblExpInner.ForeColor = DreamineTheme.TextPrimary;
        _lblExpInner.Font      = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        _lblExpInner.AutoSize  = true;
        _lblExpInner.Name      = "_lblExpInner";

        _expander.Content.Controls.Add(_lblExpInner);

        _tabMiscFlow.Controls.Add(_lblExpCard);
        _tabMiscFlow.Controls.Add(_expander);
        _tabMisc.Controls.Add(_tabMiscFlow);

        // ══ _tabs ═════════════════════════════════════════
        _tabs.Width  = 680;
        _tabs.Height = 420;
        _tabs.Margin = new Padding(0, 0, 0, 8);
        _tabs.Name   = "_tabs";
        _tabs.TabPages.Add(_tabButton);
        _tabs.TabPages.Add(_tabCheckRadio);
        _tabs.TabPages.Add(_tabLed);
        _tabs.TabPages.Add(_tabText);
        _tabs.TabPages.Add(_tabCombo);
        _tabs.TabPages.Add(_tabMisc);

        // ══ _layout ═══════════════════════════════════════
        _layout.Dock          = DockStyle.Fill;
        _layout.FlowDirection = FlowDirection.TopDown;
        _layout.AutoSize      = true;
        _layout.WrapContents  = false;
        _layout.Name          = "_layout";
        _layout.Controls.Add(_title);
        _layout.Controls.Add(_tabs);
        _layout.Controls.Add(_statusLabel);

        // ══ UserControl ═══════════════════════════════════
        BackColor = DreamineTheme.AppBackground;
        Dock      = DockStyle.Fill;
        Padding   = new Padding(16);
        Name      = "ControlsPage";
        Controls.Add(_layout);

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.PropertyChanged -= OnPropertyChanged;
        base.Dispose(disposing);
    }
}
