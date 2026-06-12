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
        _title = new Label();
        _tabs = new DreamineTabControl();
        _tabButton = new TabPage();
        _tabButtonFlow = new FlowLayoutPanel();
        _lblButtonCard = new Label();
        _btnClickMe = new DreamineButton();
        _lblClickCount = new Label();
        _tabCheckRadio = new TabPage();
        _tabCheckRadioFlow = new FlowLayoutPanel();
        _lblCbCard = new Label();
        _cb1 = new DreamineCheckBox();
        _cb2 = new DreamineCheckBox();
        _cb3 = new DreamineCheckBox();
        _lblRbCard = new Label();
        _radioPanel = new Panel();
        _rb1 = new DreamineRadioButton();
        _rb2 = new DreamineRadioButton();
        _rb3 = new DreamineRadioButton();
        _tabLed = new TabPage();
        _tabLedFlow = new FlowLayoutPanel();
        _lblLedCard = new Label();
        _led = new DreamineCheckLed();
        _ledBtnRow = new FlowLayoutPanel();
        _btnToggle = new DreamineButton();
        _btnPulse = new DreamineButton();
        _tabText = new TabPage();
        _tabTextFlow = new FlowLayoutPanel();
        _lblTbCard = new Label();
        _tbRow = new FlowLayoutPanel();
        _tb = new DreamineTextBox();
        _btnClearTb = new DreamineButton();
        _lblPbCard = new Label();
        _pbRow = new FlowLayoutPanel();
        _pb = new DreaminePasswordBox();
        _btnClearPb = new DreamineButton();
        _tabCombo = new TabPage();
        _tabComboFlow = new FlowLayoutPanel();
        _lblComboCard = new Label();
        _combo = new DreamineComboBox();
        _lblSelected = new Label();
        _tabMisc = new TabPage();
        _tabMiscFlow = new FlowLayoutPanel();
        _lblExpCard = new Label();
        _expander = new DreamineExpander();
        _lblExpInner = new Label();
        _statusLabel = new Label();
        _layout = new FlowLayoutPanel();
        _tabs.SuspendLayout();
        _tabButton.SuspendLayout();
        _tabButtonFlow.SuspendLayout();
        _tabCheckRadio.SuspendLayout();
        _tabCheckRadioFlow.SuspendLayout();
        _radioPanel.SuspendLayout();
        _tabLed.SuspendLayout();
        _tabLedFlow.SuspendLayout();
        _ledBtnRow.SuspendLayout();
        _tabText.SuspendLayout();
        _tabTextFlow.SuspendLayout();
        _tbRow.SuspendLayout();
        _pbRow.SuspendLayout();
        _tabCombo.SuspendLayout();
        _tabComboFlow.SuspendLayout();
        _tabMisc.SuspendLayout();
        _tabMiscFlow.SuspendLayout();
        _layout.SuspendLayout();
        SuspendLayout();
        // 
        // _title
        // 
        _title.AutoSize = true;
        _title.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        _title.ForeColor = Color.White;
        _title.Location = new Point(0, 0);
        _title.Margin = new Padding(0, 0, 0, 8);
        _title.Name = "_title";
        _title.Size = new Size(228, 32);
        _title.TabIndex = 0;
        _title.Text = "Controls Showcase";
        // 
        // _tabs
        // 
        _tabs.Controls.Add(_tabButton);
        _tabs.Controls.Add(_tabCheckRadio);
        _tabs.Controls.Add(_tabLed);
        _tabs.Controls.Add(_tabText);
        _tabs.Controls.Add(_tabCombo);
        _tabs.Controls.Add(_tabMisc);
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabs.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _tabs.ItemSize = new Size(120, 36);
        _tabs.Location = new Point(0, 40);
        _tabs.Margin = new Padding(0, 0, 0, 8);
        _tabs.Name = "_tabs";
        _tabs.Padding = new Point(12, 6);
        _tabs.SelectedIndex = 0;
        _tabs.Size = new Size(680, 420);
        _tabs.TabIndex = 1;
        // 
        // _tabButton
        // 
        _tabButton.BackColor = Color.FromArgb(26, 26, 46);
        _tabButton.Controls.Add(_tabButtonFlow);
        _tabButton.ForeColor = Color.White;
        _tabButton.Location = new Point(4, 40);
        _tabButton.Name = "_tabButton";
        _tabButton.Size = new Size(672, 376);
        _tabButton.TabIndex = 0;
        _tabButton.Text = "Button";
        // 
        // _tabButtonFlow
        // 
        _tabButtonFlow.BackColor = Color.FromArgb(26, 26, 46);
        _tabButtonFlow.Controls.Add(_lblButtonCard);
        _tabButtonFlow.Controls.Add(_btnClickMe);
        _tabButtonFlow.Controls.Add(_lblClickCount);
        _tabButtonFlow.Dock = DockStyle.Fill;
        _tabButtonFlow.FlowDirection = FlowDirection.TopDown;
        _tabButtonFlow.Location = new Point(0, 0);
        _tabButtonFlow.Name = "_tabButtonFlow";
        _tabButtonFlow.Padding = new Padding(16);
        _tabButtonFlow.Size = new Size(672, 376);
        _tabButtonFlow.TabIndex = 0;
        _tabButtonFlow.WrapContents = false;
        // 
        // _lblButtonCard
        // 
        _lblButtonCard.AutoSize = true;
        _lblButtonCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblButtonCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblButtonCard.Location = new Point(16, 24);
        _lblButtonCard.Margin = new Padding(0, 8, 0, 4);
        _lblButtonCard.Name = "_lblButtonCard";
        _lblButtonCard.Size = new Size(118, 19);
        _lblButtonCard.TabIndex = 0;
        _lblButtonCard.Text = "DreamineButton";
        // 
        // _btnClickMe
        // 
        _btnClickMe.BackColor = Color.FromArgb(13, 27, 62);
        _btnClickMe.BorderColor = Color.FromArgb(45, 74, 110);
        _btnClickMe.Command = null;
        _btnClickMe.CommandParameter = null;
        _btnClickMe.Content = "Click Me";
        _btnClickMe.CornerRadius = 6;
        _btnClickMe.Font = new Font("Segoe UI", 10F);
        _btnClickMe.ForeColor = Color.White;
        _btnClickMe.IsSelected = false;
        _btnClickMe.Location = new Point(19, 50);
        _btnClickMe.Name = "_btnClickMe";
        _btnClickMe.ShineColor = Color.Empty;
        _btnClickMe.Size = new Size(140, 40);
        _btnClickMe.TabIndex = 1;
        // 
        // _lblClickCount
        // 
        _lblClickCount.AutoSize = true;
        _lblClickCount.Font = new Font("Segoe UI", 11F);
        _lblClickCount.ForeColor = Color.White;
        _lblClickCount.Location = new Point(19, 93);
        _lblClickCount.Name = "_lblClickCount";
        _lblClickCount.Size = new Size(61, 20);
        _lblClickCount.TabIndex = 2;
        _lblClickCount.Text = "Clicks: 0";
        // 
        // _tabCheckRadio
        // 
        _tabCheckRadio.BackColor = Color.FromArgb(26, 26, 46);
        _tabCheckRadio.Controls.Add(_tabCheckRadioFlow);
        _tabCheckRadio.ForeColor = Color.White;
        _tabCheckRadio.Location = new Point(4, 40);
        _tabCheckRadio.Name = "_tabCheckRadio";
        _tabCheckRadio.Size = new Size(672, 376);
        _tabCheckRadio.TabIndex = 1;
        _tabCheckRadio.Text = "CheckBox / Radio";
        // 
        // _tabCheckRadioFlow
        // 
        _tabCheckRadioFlow.BackColor = Color.FromArgb(26, 26, 46);
        _tabCheckRadioFlow.Controls.Add(_lblCbCard);
        _tabCheckRadioFlow.Controls.Add(_cb1);
        _tabCheckRadioFlow.Controls.Add(_cb2);
        _tabCheckRadioFlow.Controls.Add(_cb3);
        _tabCheckRadioFlow.Controls.Add(_lblRbCard);
        _tabCheckRadioFlow.Controls.Add(_radioPanel);
        _tabCheckRadioFlow.Dock = DockStyle.Fill;
        _tabCheckRadioFlow.FlowDirection = FlowDirection.TopDown;
        _tabCheckRadioFlow.Location = new Point(0, 0);
        _tabCheckRadioFlow.Name = "_tabCheckRadioFlow";
        _tabCheckRadioFlow.Padding = new Padding(16);
        _tabCheckRadioFlow.Size = new Size(672, 376);
        _tabCheckRadioFlow.TabIndex = 0;
        _tabCheckRadioFlow.WrapContents = false;
        // 
        // _lblCbCard
        // 
        _lblCbCard.AutoSize = true;
        _lblCbCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblCbCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblCbCard.Location = new Point(16, 24);
        _lblCbCard.Margin = new Padding(0, 8, 0, 4);
        _lblCbCard.Name = "_lblCbCard";
        _lblCbCard.Size = new Size(140, 19);
        _lblCbCard.TabIndex = 0;
        _lblCbCard.Text = "DreamineCheckBox";
        // 
        // _cb1
        // 
        _cb1.BackColor = Color.Transparent;
        _cb1.Content = "Option 1";
        _cb1.Font = new Font("Segoe UI", 10F);
        _cb1.ForeColor = Color.White;
        _cb1.IsChecked = false;
        _cb1.Location = new Point(19, 50);
        _cb1.Name = "_cb1";
        _cb1.Size = new Size(180, 28);
        _cb1.TabIndex = 1;
        // 
        // _cb2
        // 
        _cb2.BackColor = Color.Transparent;
        _cb2.Content = "Option 2";
        _cb2.Font = new Font("Segoe UI", 10F);
        _cb2.ForeColor = Color.White;
        _cb2.IsChecked = false;
        _cb2.Location = new Point(19, 84);
        _cb2.Name = "_cb2";
        _cb2.Size = new Size(180, 28);
        _cb2.TabIndex = 2;
        // 
        // _cb3
        // 
        _cb3.BackColor = Color.Transparent;
        _cb3.Content = "Option 3";
        _cb3.Font = new Font("Segoe UI", 10F);
        _cb3.ForeColor = Color.White;
        _cb3.IsChecked = false;
        _cb3.Location = new Point(19, 118);
        _cb3.Name = "_cb3";
        _cb3.Size = new Size(180, 28);
        _cb3.TabIndex = 3;
        // 
        // _lblRbCard
        // 
        _lblRbCard.AutoSize = true;
        _lblRbCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblRbCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblRbCard.Location = new Point(16, 157);
        _lblRbCard.Margin = new Padding(0, 8, 0, 4);
        _lblRbCard.Name = "_lblRbCard";
        _lblRbCard.Size = new Size(157, 19);
        _lblRbCard.TabIndex = 4;
        _lblRbCard.Text = "DreamineRadioButton";
        // 
        // _radioPanel
        // 
        _radioPanel.BackColor = Color.FromArgb(15, 30, 58);
        _radioPanel.Controls.Add(_rb1);
        _radioPanel.Controls.Add(_rb2);
        _radioPanel.Controls.Add(_rb3);
        _radioPanel.Location = new Point(19, 183);
        _radioPanel.Name = "_radioPanel";
        _radioPanel.Size = new Size(500, 80);
        _radioPanel.TabIndex = 5;
        // 
        // _rb1
        // 
        _rb1.BackColor = Color.Transparent;
        _rb1.Content = "Option A";
        _rb1.Font = new Font("Segoe UI", 10F);
        _rb1.ForeColor = Color.White;
        _rb1.GroupName = "demo";
        _rb1.IsChecked = false;
        _rb1.Location = new Point(0, 0);
        _rb1.Name = "_rb1";
        _rb1.Size = new Size(160, 28);
        _rb1.TabIndex = 0;
        // 
        // _rb2
        // 
        _rb2.BackColor = Color.Transparent;
        _rb2.Content = "Option B";
        _rb2.Font = new Font("Segoe UI", 10F);
        _rb2.ForeColor = Color.White;
        _rb2.GroupName = "demo";
        _rb2.IsChecked = false;
        _rb2.Location = new Point(160, 0);
        _rb2.Name = "_rb2";
        _rb2.Size = new Size(160, 28);
        _rb2.TabIndex = 1;
        // 
        // _rb3
        // 
        _rb3.BackColor = Color.Transparent;
        _rb3.Content = "Option C";
        _rb3.Font = new Font("Segoe UI", 10F);
        _rb3.ForeColor = Color.White;
        _rb3.GroupName = "demo";
        _rb3.IsChecked = false;
        _rb3.Location = new Point(320, 0);
        _rb3.Name = "_rb3";
        _rb3.Size = new Size(160, 28);
        _rb3.TabIndex = 2;
        // 
        // _tabLed
        // 
        _tabLed.BackColor = Color.FromArgb(26, 26, 46);
        _tabLed.Controls.Add(_tabLedFlow);
        _tabLed.ForeColor = Color.White;
        _tabLed.Location = new Point(4, 40);
        _tabLed.Name = "_tabLed";
        _tabLed.Size = new Size(672, 376);
        _tabLed.TabIndex = 2;
        _tabLed.Text = "CheckLed";
        // 
        // _tabLedFlow
        // 
        _tabLedFlow.BackColor = Color.FromArgb(26, 26, 46);
        _tabLedFlow.Controls.Add(_lblLedCard);
        _tabLedFlow.Controls.Add(_led);
        _tabLedFlow.Controls.Add(_ledBtnRow);
        _tabLedFlow.Dock = DockStyle.Fill;
        _tabLedFlow.FlowDirection = FlowDirection.TopDown;
        _tabLedFlow.Location = new Point(0, 0);
        _tabLedFlow.Name = "_tabLedFlow";
        _tabLedFlow.Padding = new Padding(16);
        _tabLedFlow.Size = new Size(672, 376);
        _tabLedFlow.TabIndex = 0;
        _tabLedFlow.WrapContents = false;
        // 
        // _lblLedCard
        // 
        _lblLedCard.AutoSize = true;
        _lblLedCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblLedCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblLedCard.Location = new Point(16, 24);
        _lblLedCard.Margin = new Padding(0, 8, 0, 4);
        _lblLedCard.Name = "_lblLedCard";
        _lblLedCard.Size = new Size(138, 19);
        _lblLedCard.TabIndex = 0;
        _lblLedCard.Text = "DreamineCheckLed";
        // 
        // _led
        // 
        _led.BackColor = Color.Transparent;
        _led.Corner = LedCorner.TopRight;
        _led.Diameter = 16F;
        _led.IsOn = true;
        _led.IsPulse = false;
        _led.Location = new Point(19, 50);
        _led.Name = "_led";
        _led.Size = new Size(60, 60);
        _led.TabIndex = 1;
        // 
        // _ledBtnRow
        // 
        _ledBtnRow.AutoSize = true;
        _ledBtnRow.Controls.Add(_btnToggle);
        _ledBtnRow.Controls.Add(_btnPulse);
        _ledBtnRow.Location = new Point(19, 116);
        _ledBtnRow.Name = "_ledBtnRow";
        _ledBtnRow.Size = new Size(314, 42);
        _ledBtnRow.TabIndex = 2;
        // 
        // _btnToggle
        // 
        _btnToggle.BackColor = Color.FromArgb(13, 27, 62);
        _btnToggle.BorderColor = Color.FromArgb(45, 74, 110);
        _btnToggle.Command = null;
        _btnToggle.CommandParameter = null;
        _btnToggle.Content = "Toggle ON/OFF";
        _btnToggle.CornerRadius = 6;
        _btnToggle.Font = new Font("Segoe UI", 10F);
        _btnToggle.ForeColor = Color.White;
        _btnToggle.IsSelected = false;
        _btnToggle.Location = new Point(0, 0);
        _btnToggle.Margin = new Padding(0, 0, 8, 0);
        _btnToggle.Name = "_btnToggle";
        _btnToggle.ShineColor = Color.Empty;
        _btnToggle.Size = new Size(160, 36);
        _btnToggle.TabIndex = 0;
        // 
        // _btnPulse
        // 
        _btnPulse.BackColor = Color.FromArgb(13, 27, 62);
        _btnPulse.BorderColor = Color.FromArgb(45, 74, 110);
        _btnPulse.Command = null;
        _btnPulse.CommandParameter = null;
        _btnPulse.Content = "Toggle Pulse";
        _btnPulse.CornerRadius = 6;
        _btnPulse.Font = new Font("Segoe UI", 10F);
        _btnPulse.ForeColor = Color.White;
        _btnPulse.IsSelected = false;
        _btnPulse.Location = new Point(171, 3);
        _btnPulse.Name = "_btnPulse";
        _btnPulse.ShineColor = Color.Empty;
        _btnPulse.Size = new Size(140, 36);
        _btnPulse.TabIndex = 1;
        // 
        // _tabText
        // 
        _tabText.BackColor = Color.FromArgb(26, 26, 46);
        _tabText.Controls.Add(_tabTextFlow);
        _tabText.ForeColor = Color.White;
        _tabText.Location = new Point(4, 40);
        _tabText.Name = "_tabText";
        _tabText.Size = new Size(672, 376);
        _tabText.TabIndex = 3;
        _tabText.Text = "TextBox";
        // 
        // _tabTextFlow
        // 
        _tabTextFlow.BackColor = Color.FromArgb(26, 26, 46);
        _tabTextFlow.Controls.Add(_lblTbCard);
        _tabTextFlow.Controls.Add(_tbRow);
        _tabTextFlow.Controls.Add(_lblPbCard);
        _tabTextFlow.Controls.Add(_pbRow);
        _tabTextFlow.Dock = DockStyle.Fill;
        _tabTextFlow.FlowDirection = FlowDirection.TopDown;
        _tabTextFlow.Location = new Point(0, 0);
        _tabTextFlow.Name = "_tabTextFlow";
        _tabTextFlow.Padding = new Padding(16);
        _tabTextFlow.Size = new Size(672, 376);
        _tabTextFlow.TabIndex = 0;
        _tabTextFlow.WrapContents = false;
        // 
        // _lblTbCard
        // 
        _lblTbCard.AutoSize = true;
        _lblTbCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblTbCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblTbCard.Location = new Point(16, 24);
        _lblTbCard.Margin = new Padding(0, 8, 0, 4);
        _lblTbCard.Name = "_lblTbCard";
        _lblTbCard.Size = new Size(128, 19);
        _lblTbCard.TabIndex = 0;
        _lblTbCard.Text = "DreamineTextBox";
        // 
        // _tbRow
        // 
        _tbRow.AutoSize = true;
        _tbRow.Controls.Add(_tb);
        _tbRow.Controls.Add(_btnClearTb);
        _tbRow.Location = new Point(19, 50);
        _tbRow.Name = "_tbRow";
        _tbRow.Size = new Size(454, 42);
        _tbRow.TabIndex = 1;
        // 
        // _tb
        // 
        _tb.BackColor = Color.FromArgb(22, 32, 64);
        _tb.Font = new Font("Segoe UI", 10F);
        _tb.ForeColor = Color.White;
        _tb.Hint = "텍스트를 입력하세요…";
        _tb.IsReadOnly = false;
        _tb.Location = new Point(3, 3);
        _tb.Name = "_tb";
        _tb.Padding = new Padding(2);
        _tb.Size = new Size(360, 36);
        _tb.TabIndex = 0;
        // 
        // _btnClearTb
        // 
        _btnClearTb.BackColor = Color.FromArgb(13, 27, 62);
        _btnClearTb.BorderColor = Color.FromArgb(45, 74, 110);
        _btnClearTb.Command = null;
        _btnClearTb.CommandParameter = null;
        _btnClearTb.Content = "Clear";
        _btnClearTb.CornerRadius = 6;
        _btnClearTb.Font = new Font("Segoe UI", 10F);
        _btnClearTb.ForeColor = Color.White;
        _btnClearTb.IsSelected = false;
        _btnClearTb.Location = new Point(374, 0);
        _btnClearTb.Margin = new Padding(8, 0, 0, 0);
        _btnClearTb.Name = "_btnClearTb";
        _btnClearTb.ShineColor = Color.Empty;
        _btnClearTb.Size = new Size(80, 36);
        _btnClearTb.TabIndex = 1;
        // 
        // _lblPbCard
        // 
        _lblPbCard.AutoSize = true;
        _lblPbCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblPbCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblPbCard.Location = new Point(16, 103);
        _lblPbCard.Margin = new Padding(0, 8, 0, 4);
        _lblPbCard.Name = "_lblPbCard";
        _lblPbCard.Size = new Size(164, 19);
        _lblPbCard.TabIndex = 2;
        _lblPbCard.Text = "DreaminePasswordBox";
        // 
        // _pbRow
        // 
        _pbRow.AutoSize = true;
        _pbRow.Controls.Add(_pb);
        _pbRow.Controls.Add(_btnClearPb);
        _pbRow.Location = new Point(19, 129);
        _pbRow.Name = "_pbRow";
        _pbRow.Size = new Size(454, 42);
        _pbRow.TabIndex = 3;
        // 
        // _pb
        // 
        _pb.BackColor = Color.FromArgb(22, 32, 64);
        _pb.Font = new Font("Segoe UI", 10F);
        _pb.ForeColor = Color.White;
        _pb.Hint = "비밀번호 입력…";
        _pb.Location = new Point(3, 3);
        _pb.Name = "_pb";
        _pb.Password = "";
        _pb.Size = new Size(360, 36);
        _pb.TabIndex = 0;
        // 
        // _btnClearPb
        // 
        _btnClearPb.BackColor = Color.FromArgb(13, 27, 62);
        _btnClearPb.BorderColor = Color.FromArgb(45, 74, 110);
        _btnClearPb.Command = null;
        _btnClearPb.CommandParameter = null;
        _btnClearPb.Content = "Clear";
        _btnClearPb.CornerRadius = 6;
        _btnClearPb.Font = new Font("Segoe UI", 10F);
        _btnClearPb.ForeColor = Color.White;
        _btnClearPb.IsSelected = false;
        _btnClearPb.Location = new Point(374, 0);
        _btnClearPb.Margin = new Padding(8, 0, 0, 0);
        _btnClearPb.Name = "_btnClearPb";
        _btnClearPb.ShineColor = Color.Empty;
        _btnClearPb.Size = new Size(80, 36);
        _btnClearPb.TabIndex = 1;
        // 
        // _tabCombo
        // 
        _tabCombo.BackColor = Color.FromArgb(26, 26, 46);
        _tabCombo.Controls.Add(_tabComboFlow);
        _tabCombo.ForeColor = Color.White;
        _tabCombo.Location = new Point(4, 40);
        _tabCombo.Name = "_tabCombo";
        _tabCombo.Size = new Size(672, 376);
        _tabCombo.TabIndex = 4;
        _tabCombo.Text = "ComboBox";
        // 
        // _tabComboFlow
        // 
        _tabComboFlow.BackColor = Color.FromArgb(26, 26, 46);
        _tabComboFlow.Controls.Add(_lblComboCard);
        _tabComboFlow.Controls.Add(_combo);
        _tabComboFlow.Controls.Add(_lblSelected);
        _tabComboFlow.Dock = DockStyle.Fill;
        _tabComboFlow.FlowDirection = FlowDirection.TopDown;
        _tabComboFlow.Location = new Point(0, 0);
        _tabComboFlow.Name = "_tabComboFlow";
        _tabComboFlow.Padding = new Padding(16);
        _tabComboFlow.Size = new Size(672, 376);
        _tabComboFlow.TabIndex = 0;
        _tabComboFlow.WrapContents = false;
        // 
        // _lblComboCard
        // 
        _lblComboCard.AutoSize = true;
        _lblComboCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblComboCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblComboCard.Location = new Point(16, 24);
        _lblComboCard.Margin = new Padding(0, 8, 0, 4);
        _lblComboCard.Name = "_lblComboCard";
        _lblComboCard.Size = new Size(149, 19);
        _lblComboCard.TabIndex = 0;
        _lblComboCard.Text = "DreamineComboBox";
        // 
        // _combo
        // 
        _combo.BackColor = Color.FromArgb(22, 32, 64);
        _combo.Font = new Font("Segoe UI", 10F);
        _combo.ForeColor = Color.White;
        _combo.Location = new Point(19, 50);
        _combo.Name = "_combo";
        _combo.SelectedIndex = -1;
        _combo.SelectedItem = null;
        _combo.Size = new Size(240, 36);
        _combo.TabIndex = 1;
        // 
        // _lblSelected
        // 
        _lblSelected.AutoSize = true;
        _lblSelected.Font = new Font("Segoe UI", 11F);
        _lblSelected.ForeColor = Color.White;
        _lblSelected.Location = new Point(19, 89);
        _lblSelected.Name = "_lblSelected";
        _lblSelected.Size = new Size(88, 20);
        _lblSelected.TabIndex = 2;
        _lblSelected.Text = "Selected: —";
        // 
        // _tabMisc
        // 
        _tabMisc.BackColor = Color.FromArgb(26, 26, 46);
        _tabMisc.Controls.Add(_tabMiscFlow);
        _tabMisc.ForeColor = Color.White;
        _tabMisc.Location = new Point(4, 40);
        _tabMisc.Name = "_tabMisc";
        _tabMisc.Size = new Size(672, 376);
        _tabMisc.TabIndex = 5;
        _tabMisc.Text = "Misc";
        // 
        // _tabMiscFlow
        // 
        _tabMiscFlow.BackColor = Color.FromArgb(26, 26, 46);
        _tabMiscFlow.Controls.Add(_lblExpCard);
        _tabMiscFlow.Controls.Add(_expander);
        _tabMiscFlow.Dock = DockStyle.Fill;
        _tabMiscFlow.FlowDirection = FlowDirection.TopDown;
        _tabMiscFlow.Location = new Point(0, 0);
        _tabMiscFlow.Name = "_tabMiscFlow";
        _tabMiscFlow.Padding = new Padding(16);
        _tabMiscFlow.Size = new Size(672, 376);
        _tabMiscFlow.TabIndex = 0;
        _tabMiscFlow.WrapContents = false;
        // 
        // _lblExpCard
        // 
        _lblExpCard.AutoSize = true;
        _lblExpCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblExpCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblExpCard.Location = new Point(16, 24);
        _lblExpCard.Margin = new Padding(0, 8, 0, 4);
        _lblExpCard.Name = "_lblExpCard";
        _lblExpCard.Size = new Size(137, 19);
        _lblExpCard.TabIndex = 0;
        _lblExpCard.Text = "DreamineExpander";
        // 
        // _expander
        // 
        _expander.BackColor = Color.FromArgb(15, 30, 58);
        _expander.Font = new Font("Segoe UI", 10F);
        _expander.ForeColor = Color.White;
        _expander.Header = "Expander 섹션";
        _expander.IsExpanded = true;
        _expander.Location = new Point(19, 50);
        _expander.Name = "_expander";
        _expander.Size = new Size(500, 120);
        _expander.TabIndex = 1;
        // 
        // _lblExpInner
        // 
        _lblExpInner.AutoSize = true;
        _lblExpInner.Font = new Font("Segoe UI", 9F);
        _lblExpInner.ForeColor = Color.White;
        _lblExpInner.Location = new Point(0, 0);
        _lblExpInner.Name = "_lblExpInner";
        _lblExpInner.Size = new Size(244, 15);
        _lblExpInner.TabIndex = 0;
        _lblExpInner.Text = "이 안에 어떤 컨트롤이든 배치할 수 있습니다.";
        // 
        // _statusLabel
        // 
        _statusLabel.AutoSize = true;
        _statusLabel.Font = new Font("Segoe UI", 9F);
        _statusLabel.ForeColor = Color.FromArgb(136, 153, 170);
        _statusLabel.Location = new Point(3, 468);
        _statusLabel.Name = "_statusLabel";
        _statusLabel.Size = new Size(57, 15);
        _statusLabel.TabIndex = 2;
        _statusLabel.Text = "Status: —";
        // 
        // _layout
        // 
        _layout.AutoSize = true;
        _layout.Controls.Add(_title);
        _layout.Controls.Add(_tabs);
        _layout.Controls.Add(_statusLabel);
        _layout.Dock = DockStyle.Fill;
        _layout.FlowDirection = FlowDirection.TopDown;
        _layout.Location = new Point(16, 16);
        _layout.Name = "_layout";
        _layout.Size = new Size(1149, 1241);
        _layout.TabIndex = 0;
        _layout.WrapContents = false;
        // 
        // ControlsPage
        // 
        BackColor = Color.FromArgb(26, 26, 46);
        Controls.Add(_layout);
        Name = "ControlsPage";
        Padding = new Padding(16);
        Size = new Size(1181, 1273);
        _tabs.ResumeLayout(false);
        _tabButton.ResumeLayout(false);
        _tabButtonFlow.ResumeLayout(false);
        _tabButtonFlow.PerformLayout();
        _tabCheckRadio.ResumeLayout(false);
        _tabCheckRadioFlow.ResumeLayout(false);
        _tabCheckRadioFlow.PerformLayout();
        _radioPanel.ResumeLayout(false);
        _tabLed.ResumeLayout(false);
        _tabLedFlow.ResumeLayout(false);
        _tabLedFlow.PerformLayout();
        _ledBtnRow.ResumeLayout(false);
        _tabText.ResumeLayout(false);
        _tabTextFlow.ResumeLayout(false);
        _tabTextFlow.PerformLayout();
        _tbRow.ResumeLayout(false);
        _pbRow.ResumeLayout(false);
        _tabCombo.ResumeLayout(false);
        _tabComboFlow.ResumeLayout(false);
        _tabComboFlow.PerformLayout();
        _tabMisc.ResumeLayout(false);
        _tabMiscFlow.ResumeLayout(false);
        _tabMiscFlow.PerformLayout();
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.PropertyChanged -= OnPropertyChanged;
        base.Dispose(disposing);
    }
}
