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
    private Panel              _layout      = null!;

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
    private Label            _lblCornerCard = null!;
    private FlowLayoutPanel  _cornerRow     = null!;
    private DreamineCheckLed _ledTopLeft     = null!;
    private DreamineCheckLed _ledTopRight    = null!;
    private DreamineCheckLed _ledBottomLeft  = null!;
    private DreamineCheckLed _ledBottomRight = null!;
    private Label            _lblDiameterCard = null!;
    private FlowLayoutPanel  _diameterRow     = null!;
    private DreamineCheckLed _ledD10 = null!;
    private DreamineCheckLed _ledD16 = null!;
    private DreamineCheckLed _ledD22 = null!;
    private DreamineCheckLed _ledD30 = null!;
    private DreamineCheckLed _ledD42 = null!;

    // ── TextBox tab ───────────────────────────────────────
    private TabPage         _tabText     = null!;
    private FlowLayoutPanel _tabTextFlow = null!;
    private Label           _lblTbCard   = null!;
    private FlowLayoutPanel _tbRow       = null!;
    private DreamineTextBox _tb          = null!;
    private DreamineButton  _btnClearTb  = null!;
    private Label           _lblTbReadOnlyCard = null!;
    private DreamineTextBox _tbReadOnly        = null!;
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
    private Label           _lblComboDisabledCard = null!;
    private DreamineComboBox _comboDisabled = null!;

    // ── DataGrid / ListBox tab ─────────────────────────────
    private TabPage         _tabGrid      = null!;
    private FlowLayoutPanel _tabGridFlow  = null!;
    private Label           _lblGridCard  = null!;
    private DreamineDataGrid _grid        = null!;
    private Label           _lblListCard  = null!;
    private DreamineListBox _list         = null!;
    private Label           _lblLogCard   = null!;
    private DreamineButton  _btnLogClick  = null!;
    private DreamineListBox _logList      = null!;

    // ── Misc tab ──────────────────────────────────────────
    private TabPage         _tabMisc     = null!;
    private FlowLayoutPanel _tabMiscFlow = null!;
    private Label           _lblExpCard  = null!;
    private DreamineExpander _expander   = null!;
    private Label           _lblExpInner = null!;
    private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1 = null!;
    private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = null!;
    private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3 = null!;
    private readonly ControlsViewModel _vm;

    /// <summary>VS WinForms 디자이너용 기본 생성자.</summary>
    public ControlsPage() : this(new ControlsViewModel(new ControlsEvent())) { }

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
        // 디자이너가 InitializeComponent를 재생성할 때 Content.Controls.Add 같은 비표준 호출을
        // 지워버리므로, 생성자 쪽에 둬서 디자이너가 다시 열려도 유지되게 한다.
        _expander.Content.Controls.Add(_lblExpInner);
        foreach (var item in _vm.FruitItems) _combo.Items.Add(item);
        foreach (var item in _vm.FruitItems) _comboDisabled.Items.Add(item);
        if (_comboDisabled.Items.Count > 0) _comboDisabled.SelectedIndex = 0;
        _combo.SelectedItem  = _vm.SelectedFruit;
        _lblSelected.Text    = $"Selected: {_vm.SelectedFruit}";
        _lblClickCount.Text  = $"Clicks: {_vm.ClickCount}";

        // Button tab events
        _btnClickMe.Click += (_, _) => { _vm.ClickMeCommand.Execute(null); _lblClickCount.Text = $"Clicks: {_vm.ClickCount}"; };
        BuildButtonShowcaseRows();

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

        // DreamineVirtualKeyboard — WPF의 vk:DreamineVirtualKeyboardAssist.UseVirtualKeyBoard="True"와 동일한 데모.
        Dreamine.UI.WinForms.VirtualKeyboard.DreamineVirtualKeyboardAssist.Attach(_tb);

        // ComboBox events
        _combo.SelectedIndexChanged += (_, _) =>
        {
            _vm.SelectedFruit = _combo.SelectedItem?.ToString() ?? string.Empty;
            _lblSelected.Text = $"Selected: {_vm.SelectedFruit}";
        };

        // Expander events
        _expander.ExpandedChanged += (_, _) => _vm.IsExpanded = _expander.IsExpanded;

        // DataGrid / ListBox tab
        foreach (var row in _vm.GridRows)
            _grid.Rows.Add(row.No, row.Name, row.Status);

        foreach (var fruit in _vm.FruitItems)
            _list.Items.Add(fruit);
        _list.DoubleClick += (_, _) => _vm.ListBoxActivatedCommand.Execute(_list.SelectedItem?.ToString());

        foreach (var entry in _vm.ActivityLog)
            _logList.Items.Add(entry);
        _logList.AutoScrollToEnd = true;
        _btnLogClick.Click += (_, _) =>
        {
            _vm.ClickMeCommand.Execute(null);
            _logList.Items.Clear();
            foreach (var entry in _vm.ActivityLog)
                _logList.Items.Add(entry);
            _logList.NotifyItemAdded();
            _lblClickCount.Text = $"Clicks: {_vm.ClickCount}";
        };

        _vm.PropertyChanged += OnPropertyChanged;
    }

    /// <summary>
    /// WPF Button 탭(색상/ShineColor/크기/비활성+그림자 변형)과 동등한 데모 줄을 동적으로 만든다.
    /// 개별 버튼마다 디자이너 필드를 두지 않고 코드로 생성한다(WinForms 디자이너가
    /// InitializeComponent를 재생성할 때 비표준 코드를 지워버리는 문제를 피하기 위함).
    /// </summary>
    private void BuildButtonShowcaseRows()
    {
        var colorRow = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 16, 0, 0) };
        var colorLabel = MakeCardLabel("기본 버튼 — Background 변형");
        AddColorButton(colorRow, "Primary", Color.FromArgb(30, 144, 255));
        AddColorButton(colorRow, "Secondary", Color.FromArgb(45, 74, 110));
        AddColorButton(colorRow, "Success", Color.FromArgb(27, 122, 62));
        AddColorButton(colorRow, "Warning", Color.FromArgb(184, 92, 0));
        AddColorButton(colorRow, "Danger", Color.FromArgb(139, 26, 26));

        var shineLabel = MakeCardLabel("ShineColor — 글로우 효과");
        var shineRow = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 0, 0, 0) };
        AddShineButton(shineRow, "Blue Glow", Color.FromArgb(13, 27, 62), Color.FromArgb(30, 144, 255));
        AddShineButton(shineRow, "Cyan Glow", Color.FromArgb(13, 27, 62), Color.FromArgb(0, 188, 212));
        AddShineButton(shineRow, "Green Glow", Color.FromArgb(13, 27, 62), Color.FromArgb(76, 175, 80));

        var sizeLabel = MakeCardLabel("크기 변형");
        var sizeRow = new FlowLayoutPanel { AutoSize = true };
        AddSizeButton(sizeRow, "XS", 50, 24, 8f);
        AddSizeButton(sizeRow, "Small", 72, 28, 9f);
        AddSizeButton(sizeRow, "Medium", 90, 34, 10f);
        AddSizeButton(sizeRow, "Large", 110, 42, 11f);

        var stateLabel = MakeCardLabel("비활성 / Disabled");
        var stateRow = new FlowLayoutPanel { AutoSize = true };
        AddColorButton(stateRow, "Enabled", Color.FromArgb(30, 144, 255));
        var disabledBtn = AddColorButton(stateRow, "Disabled", Color.FromArgb(30, 144, 255));
        disabledBtn.Enabled = false;

        _tabButtonFlow.Controls.Add(colorLabel);
        _tabButtonFlow.Controls.Add(colorRow);
        _tabButtonFlow.Controls.Add(shineLabel);
        _tabButtonFlow.Controls.Add(shineRow);
        _tabButtonFlow.Controls.Add(sizeLabel);
        _tabButtonFlow.Controls.Add(sizeRow);
        _tabButtonFlow.Controls.Add(stateLabel);
        _tabButtonFlow.Controls.Add(stateRow);
    }

    private static Label MakeCardLabel(string text) => new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
        ForeColor = Color.FromArgb(30, 144, 255),
        Margin = new Padding(0, 16, 0, 4),
        Text = text
    };

    private DreamineButton AddColorButton(FlowLayoutPanel row, string text, Color background)
    {
        var btn = new DreamineButton
        {
            Content = text,
            BackColor = background,
            ForeColor = Color.White,
            Width = 100,
            Height = 34,
            CornerRadius = 6,
            Margin = new Padding(0, 0, 8, 8)
        };
        btn.Click += (_, _) => { _vm.ClickMeCommand.Execute(null); _lblClickCount.Text = $"Clicks: {_vm.ClickCount}"; };
        row.Controls.Add(btn);
        return btn;
    }

    private void AddShineButton(FlowLayoutPanel row, string text, Color background, Color shine)
    {
        var btn = new DreamineButton
        {
            Content = text,
            BackColor = background,
            ShineColor = shine,
            ForeColor = Color.White,
            Width = 110,
            Height = 36,
            CornerRadius = 6,
            Margin = new Padding(0, 0, 8, 8)
        };
        btn.Click += (_, _) => { _vm.ClickMeCommand.Execute(null); _lblClickCount.Text = $"Clicks: {_vm.ClickCount}"; };
        row.Controls.Add(btn);
    }

    private const int MaxShowcaseButtonHeight = 42;

    private void AddSizeButton(FlowLayoutPanel row, string text, int width, int height, float fontSize)
    {
        // FlowLayoutPanel은 기본적으로 위쪽 정렬이라, 높이가 다른 버튼들을 그대로 두면
        // 줄이 들쭐날쭐해 보인다. 가장 큰 버튼(Large) 기준으로 위쪽 여백을 줘서 아래쪽을 맞춘다.
        var topOffset = MaxShowcaseButtonHeight - height;
        var btn = new DreamineButton
        {
            Content = text,
            BackColor = Color.FromArgb(30, 144, 255),
            ForeColor = Color.White,
            Width = width,
            Height = height,
            Font = new Font("Segoe UI", fontSize),
            CornerRadius = 6,
            Margin = new Padding(0, topOffset, 8, 0)
        };
        btn.Click += (_, _) => { _vm.ClickMeCommand.Execute(null); _lblClickCount.Text = $"Clicks: {_vm.ClickCount}"; };
        row.Controls.Add(btn);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ControlsViewModel.StatusMessage))
            _statusLabel.Text = _vm.StatusMessage;
    }

    private void InitializeComponent()
    {
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
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
        _lblCornerCard = new Label();
        _cornerRow = new FlowLayoutPanel();
        _ledTopLeft = new DreamineCheckLed();
        _ledTopRight = new DreamineCheckLed();
        _ledBottomLeft = new DreamineCheckLed();
        _ledBottomRight = new DreamineCheckLed();
        _lblDiameterCard = new Label();
        _diameterRow = new FlowLayoutPanel();
        _ledD10 = new DreamineCheckLed();
        _ledD16 = new DreamineCheckLed();
        _ledD22 = new DreamineCheckLed();
        _ledD30 = new DreamineCheckLed();
        _ledD42 = new DreamineCheckLed();
        _tabText = new TabPage();
        _tabTextFlow = new FlowLayoutPanel();
        _lblTbCard = new Label();
        _tbRow = new FlowLayoutPanel();
        _tb = new DreamineTextBox();
        _btnClearTb = new DreamineButton();
        _lblTbReadOnlyCard = new Label();
        _tbReadOnly = new DreamineTextBox();
        _lblPbCard = new Label();
        _pbRow = new FlowLayoutPanel();
        _pb = new DreaminePasswordBox();
        _btnClearPb = new DreamineButton();
        _tabCombo = new TabPage();
        _tabComboFlow = new FlowLayoutPanel();
        _lblComboCard = new Label();
        _combo = new DreamineComboBox();
        _lblSelected = new Label();
        _lblComboDisabledCard = new Label();
        _comboDisabled = new DreamineComboBox();
        _tabGrid = new TabPage();
        _tabGridFlow = new FlowLayoutPanel();
        _lblGridCard = new Label();
        _grid = new DreamineDataGrid();
        _lblListCard = new Label();
        _list = new DreamineListBox();
        _lblLogCard = new Label();
        _btnLogClick = new DreamineButton();
        _logList = new DreamineListBox();
        dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
        dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
        dataGridViewTextBoxColumn3 = new DataGridViewTextBoxColumn();
        _tabMisc = new TabPage();
        _tabMiscFlow = new FlowLayoutPanel();
        _lblExpCard = new Label();
        _expander = new DreamineExpander();
        _lblExpInner = new Label();
        _statusLabel = new Label();
        _layout = new Panel();
        _tabs.SuspendLayout();
        _tabButton.SuspendLayout();
        _tabButtonFlow.SuspendLayout();
        _tabCheckRadio.SuspendLayout();
        _tabCheckRadioFlow.SuspendLayout();
        _radioPanel.SuspendLayout();
        _tabLed.SuspendLayout();
        _tabLedFlow.SuspendLayout();
        _ledBtnRow.SuspendLayout();
        _cornerRow.SuspendLayout();
        _diameterRow.SuspendLayout();
        _tabText.SuspendLayout();
        _tabTextFlow.SuspendLayout();
        _tbRow.SuspendLayout();
        _pbRow.SuspendLayout();
        _tabCombo.SuspendLayout();
        _tabComboFlow.SuspendLayout();
        _tabGrid.SuspendLayout();
        _tabGridFlow.SuspendLayout();
        ((ISupportInitialize)_grid).BeginInit();
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
        _tabs.Controls.Add(_tabGrid);
        _tabs.Controls.Add(_tabMisc);
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabs.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _tabs.ItemSize = new Size(120, 36);
        _tabs.Location = new Point(0, 40);
        _tabs.Margin = new Padding(0, 0, 0, 8);
        _tabs.Name = "_tabs";
        _tabs.Padding = new Point(12, 6);
        _tabs.SelectedIndex = 0;
        _tabs.Size = new Size(887, 420);
        _tabs.TabIndex = 1;
        // 
        // _tabButton
        // 
        _tabButton.BackColor = Color.FromArgb(26, 26, 46);
        _tabButton.Controls.Add(_tabButtonFlow);
        _tabButton.ForeColor = Color.White;
        _tabButton.Location = new Point(4, 40);
        _tabButton.Name = "_tabButton";
        _tabButton.Size = new Size(879, 376);
        _tabButton.AutoScroll = true;
        _tabButton.TabIndex = 0;
        _tabButton.Text = "Button";
        // 
        // _tabButtonFlow
        // 
        _tabButtonFlow.BackColor = Color.FromArgb(26, 26, 46);
        _tabButtonFlow.Controls.Add(_lblButtonCard);
        _tabButtonFlow.Controls.Add(_btnClickMe);
        _tabButtonFlow.Controls.Add(_lblClickCount);
        _tabButtonFlow.Dock = DockStyle.Top;
        _tabButtonFlow.AutoSize = true;
        _tabButtonFlow.FlowDirection = FlowDirection.TopDown;
        _tabButtonFlow.Location = new Point(0, 0);
        _tabButtonFlow.Name = "_tabButtonFlow";
        _tabButtonFlow.Padding = new Padding(16);
        _tabButtonFlow.Size = new Size(879, 376);
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
        _tabCheckRadio.AutoScroll = true;
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
        _tabCheckRadioFlow.Dock = DockStyle.Top;
        _tabCheckRadioFlow.AutoSize = true;
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
        _tabLed.AutoScroll = true;
        _tabLed.TabIndex = 2;
        _tabLed.Text = "CheckLed";
        // 
        // _tabLedFlow
        // 
        _tabLedFlow.BackColor = Color.FromArgb(26, 26, 46);
        _tabLedFlow.Controls.Add(_lblLedCard);
        _tabLedFlow.Controls.Add(_led);
        _tabLedFlow.Controls.Add(_ledBtnRow);
        _tabLedFlow.Controls.Add(_lblCornerCard);
        _tabLedFlow.Controls.Add(_cornerRow);
        _tabLedFlow.Controls.Add(_lblDiameterCard);
        _tabLedFlow.Controls.Add(_diameterRow);
        _tabLedFlow.Dock = DockStyle.Top;
        _tabLedFlow.AutoSize = true;
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
        // _lblCornerCard
        //
        _lblCornerCard.AutoSize = true;
        _lblCornerCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblCornerCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblCornerCard.Margin = new Padding(0, 16, 0, 4);
        _lblCornerCard.Name = "_lblCornerCard";
        _lblCornerCard.Size = new Size(140, 19);
        _lblCornerCard.TabIndex = 3;
        _lblCornerCard.Text = "Corner 위치 (4가지)";
        //
        // _cornerRow
        //
        _cornerRow.AutoSize = true;
        _cornerRow.Controls.Add(_ledTopLeft);
        _cornerRow.Controls.Add(_ledTopRight);
        _cornerRow.Controls.Add(_ledBottomLeft);
        _cornerRow.Controls.Add(_ledBottomRight);
        _cornerRow.Name = "_cornerRow";
        _cornerRow.Size = new Size(280, 70);
        _cornerRow.TabIndex = 4;
        //
        // _ledTopLeft
        //
        _ledTopLeft.BackColor = Color.FromArgb(13, 27, 62);
        _ledTopLeft.Corner = LedCorner.TopLeft;
        _ledTopLeft.Diameter = 16F;
        _ledTopLeft.IsOn = true;
        _ledTopLeft.Margin = new Padding(0, 0, 8, 0);
        _ledTopLeft.Name = "_ledTopLeft";
        _ledTopLeft.Size = new Size(60, 60);
        _ledTopLeft.TabIndex = 0;
        //
        // _ledTopRight
        //
        _ledTopRight.BackColor = Color.FromArgb(13, 27, 62);
        _ledTopRight.Corner = LedCorner.TopRight;
        _ledTopRight.Diameter = 16F;
        _ledTopRight.IsOn = true;
        _ledTopRight.Margin = new Padding(0, 0, 8, 0);
        _ledTopRight.Name = "_ledTopRight";
        _ledTopRight.Size = new Size(60, 60);
        _ledTopRight.TabIndex = 1;
        //
        // _ledBottomLeft
        //
        _ledBottomLeft.BackColor = Color.FromArgb(13, 27, 62);
        _ledBottomLeft.Corner = LedCorner.BottomLeft;
        _ledBottomLeft.Diameter = 16F;
        _ledBottomLeft.IsOn = true;
        _ledBottomLeft.Margin = new Padding(0, 0, 8, 0);
        _ledBottomLeft.Name = "_ledBottomLeft";
        _ledBottomLeft.Size = new Size(60, 60);
        _ledBottomLeft.TabIndex = 2;
        //
        // _ledBottomRight
        //
        _ledBottomRight.BackColor = Color.FromArgb(13, 27, 62);
        _ledBottomRight.Corner = LedCorner.BottomRight;
        _ledBottomRight.Diameter = 16F;
        _ledBottomRight.IsOn = false;
        _ledBottomRight.Name = "_ledBottomRight";
        _ledBottomRight.Size = new Size(60, 60);
        _ledBottomRight.TabIndex = 3;
        //
        // _lblDiameterCard
        //
        _lblDiameterCard.AutoSize = true;
        _lblDiameterCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblDiameterCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblDiameterCard.Margin = new Padding(0, 16, 0, 4);
        _lblDiameterCard.Name = "_lblDiameterCard";
        _lblDiameterCard.Size = new Size(160, 19);
        _lblDiameterCard.TabIndex = 5;
        _lblDiameterCard.Text = "Diameter — 크기 변형";
        //
        // _diameterRow
        //
        _diameterRow.AutoSize = true;
        _diameterRow.Controls.Add(_ledD10);
        _diameterRow.Controls.Add(_ledD16);
        _diameterRow.Controls.Add(_ledD22);
        _diameterRow.Controls.Add(_ledD30);
        _diameterRow.Controls.Add(_ledD42);
        _diameterRow.Name = "_diameterRow";
        _diameterRow.Size = new Size(280, 50);
        _diameterRow.TabIndex = 6;
        //
        // _ledD10
        //
        _ledD10.BackColor = Color.Transparent;
        _ledD10.Diameter = 10F;
        _ledD10.IsOn = true;
        _ledD10.Margin = new Padding(0, 0, 14, 0);
        _ledD10.Name = "_ledD10";
        _ledD10.Size = new Size(22, 22);
        _ledD10.TabIndex = 0;
        //
        // _ledD16
        //
        _ledD16.BackColor = Color.Transparent;
        _ledD16.Diameter = 16F;
        _ledD16.IsOn = true;
        _ledD16.Margin = new Padding(0, 0, 14, 0);
        _ledD16.Name = "_ledD16";
        _ledD16.Size = new Size(28, 28);
        _ledD16.TabIndex = 1;
        //
        // _ledD22
        //
        _ledD22.BackColor = Color.Transparent;
        _ledD22.Diameter = 22F;
        _ledD22.IsOn = true;
        _ledD22.Margin = new Padding(0, 0, 14, 0);
        _ledD22.Name = "_ledD22";
        _ledD22.Size = new Size(34, 34);
        _ledD22.TabIndex = 2;
        //
        // _ledD30
        //
        _ledD30.BackColor = Color.Transparent;
        _ledD30.Diameter = 30F;
        _ledD30.IsOn = true;
        _ledD30.Margin = new Padding(0, 0, 14, 0);
        _ledD30.Name = "_ledD30";
        _ledD30.Size = new Size(42, 42);
        _ledD30.TabIndex = 3;
        //
        // _ledD42
        //
        _ledD42.BackColor = Color.Transparent;
        _ledD42.Diameter = 42F;
        _ledD42.IsOn = true;
        _ledD42.Name = "_ledD42";
        _ledD42.Size = new Size(54, 54);
        _ledD42.TabIndex = 4;
        //
        // _tabText
        //
        _tabText.BackColor = Color.FromArgb(26, 26, 46);
        _tabText.Controls.Add(_tabTextFlow);
        _tabText.ForeColor = Color.White;
        _tabText.Location = new Point(4, 40);
        _tabText.Name = "_tabText";
        _tabText.Size = new Size(672, 376);
        _tabText.AutoScroll = true;
        _tabText.TabIndex = 3;
        _tabText.Text = "TextBox";
        // 
        // _tabTextFlow
        // 
        _tabTextFlow.BackColor = Color.FromArgb(26, 26, 46);
        _tabTextFlow.Controls.Add(_lblTbCard);
        _tabTextFlow.Controls.Add(_tbRow);
        _tabTextFlow.Controls.Add(_lblTbReadOnlyCard);
        _tabTextFlow.Controls.Add(_tbReadOnly);
        _tabTextFlow.Controls.Add(_lblPbCard);
        _tabTextFlow.Controls.Add(_pbRow);
        _tabTextFlow.Dock = DockStyle.Top;
        _tabTextFlow.AutoSize = true;
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
        _tb.Hint = "클릭하면 가상 키보드가 열립니다…";
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
        // _lblTbReadOnlyCard
        //
        _lblTbReadOnlyCard.AutoSize = true;
        _lblTbReadOnlyCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblTbReadOnlyCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblTbReadOnlyCard.Margin = new Padding(0, 16, 0, 4);
        _lblTbReadOnlyCard.Name = "_lblTbReadOnlyCard";
        _lblTbReadOnlyCard.Size = new Size(220, 19);
        _lblTbReadOnlyCard.TabIndex = 4;
        _lblTbReadOnlyCard.Text = "DreamineTextBox — 읽기 전용";
        //
        // _tbReadOnly
        //
        _tbReadOnly.BackColor = Color.FromArgb(10, 21, 37);
        _tbReadOnly.Font = new Font("Segoe UI", 10F);
        _tbReadOnly.ForeColor = Color.FromArgb(136, 153, 170);
        _tbReadOnly.IsReadOnly = true;
        _tbReadOnly.Name = "_tbReadOnly";
        _tbReadOnly.Size = new Size(360, 36);
        _tbReadOnly.TabIndex = 5;
        _tbReadOnly.Text = "이 텍스트는 수정할 수 없습니다.";
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
        _tabCombo.AutoScroll = true;
        _tabCombo.TabIndex = 4;
        _tabCombo.Text = "ComboBox";
        // 
        // _tabComboFlow
        // 
        _tabComboFlow.BackColor = Color.FromArgb(26, 26, 46);
        _tabComboFlow.Controls.Add(_lblComboCard);
        _tabComboFlow.Controls.Add(_combo);
        _tabComboFlow.Controls.Add(_lblSelected);
        _tabComboFlow.Controls.Add(_lblComboDisabledCard);
        _tabComboFlow.Controls.Add(_comboDisabled);
        _tabComboFlow.Dock = DockStyle.Top;
        _tabComboFlow.AutoSize = true;
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
        // _lblComboDisabledCard
        //
        _lblComboDisabledCard.AutoSize = true;
        _lblComboDisabledCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblComboDisabledCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblComboDisabledCard.Margin = new Padding(0, 16, 0, 4);
        _lblComboDisabledCard.Name = "_lblComboDisabledCard";
        _lblComboDisabledCard.Size = new Size(200, 19);
        _lblComboDisabledCard.TabIndex = 3;
        _lblComboDisabledCard.Text = "IsEnabled=False — 비활성 상태";
        //
        // _comboDisabled
        //
        _comboDisabled.BackColor = Color.FromArgb(10, 21, 37);
        _comboDisabled.Enabled = false;
        _comboDisabled.Font = new Font("Segoe UI", 10F);
        _comboDisabled.ForeColor = Color.FromArgb(136, 153, 170);
        _comboDisabled.Name = "_comboDisabled";
        _comboDisabled.Size = new Size(240, 36);
        _comboDisabled.TabIndex = 4;
        //
        // _tabGrid
        // 
        _tabGrid.BackColor = Color.FromArgb(26, 26, 46);
        _tabGrid.Controls.Add(_tabGridFlow);
        _tabGrid.ForeColor = Color.White;
        _tabGrid.Location = new Point(4, 40);
        _tabGrid.Name = "_tabGrid";
        _tabGrid.Size = new Size(672, 376);
        _tabGrid.AutoScroll = true;
        _tabGrid.TabIndex = 5;
        _tabGrid.Text = "DataGrid / ListBox";
        // 
        // _tabGridFlow
        // 
        _tabGridFlow.BackColor = Color.FromArgb(26, 26, 46);
        _tabGridFlow.Controls.Add(_lblGridCard);
        _tabGridFlow.Controls.Add(_grid);
        _tabGridFlow.Controls.Add(_lblListCard);
        _tabGridFlow.Controls.Add(_list);
        _tabGridFlow.Controls.Add(_lblLogCard);
        _tabGridFlow.Controls.Add(_btnLogClick);
        _tabGridFlow.Controls.Add(_logList);
        _tabGridFlow.Dock = DockStyle.Top;
        _tabGridFlow.AutoSize = true;
        _tabGridFlow.FlowDirection = FlowDirection.TopDown;
        _tabGridFlow.Location = new Point(0, 0);
        _tabGridFlow.Name = "_tabGridFlow";
        _tabGridFlow.Padding = new Padding(16);
        _tabGridFlow.Size = new Size(672, 376);
        _tabGridFlow.TabIndex = 0;
        _tabGridFlow.WrapContents = false;
        // 
        // _lblGridCard
        // 
        _lblGridCard.AutoSize = true;
        _lblGridCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblGridCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblGridCard.Location = new Point(16, 24);
        _lblGridCard.Margin = new Padding(0, 8, 0, 4);
        _lblGridCard.Name = "_lblGridCard";
        _lblGridCard.Size = new Size(134, 19);
        _lblGridCard.TabIndex = 0;
        _lblGridCard.Text = "DreamineDataGrid + EnableClickToDeselect";
        // 
        // _grid
        // 
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(15, 30, 58);
        dataGridViewCellStyle1.ForeColor = Color.White;
        dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(30, 144, 255);
        dataGridViewCellStyle1.SelectionForeColor = Color.White;
        _grid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = Color.FromArgb(22, 32, 64);
        _grid.BorderStyle = BorderStyle.None;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = Color.FromArgb(13, 27, 62);
        dataGridViewCellStyle2.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        dataGridViewCellStyle2.ForeColor = Color.White;
        dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(13, 27, 62);
        dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
        _grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
        _grid.ColumnHeadersHeight = 32;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn1, dataGridViewTextBoxColumn2, dataGridViewTextBoxColumn3 });
        dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle3.BackColor = Color.FromArgb(22, 32, 64);
        dataGridViewCellStyle3.Font = new Font("Segoe UI", 9.5F);
        dataGridViewCellStyle3.ForeColor = Color.White;
        dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(30, 144, 255);
        dataGridViewCellStyle3.SelectionForeColor = Color.White;
        dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
        _grid.DefaultCellStyle = dataGridViewCellStyle3;
        _grid.EnableClickToDeselect = true;
        _grid.EnableHeadersVisualStyles = false;
        _grid.Font = new Font("Segoe UI", 9.5F);
        _grid.GridColor = Color.FromArgb(45, 74, 110);
        _grid.Location = new Point(16, 47);
        _grid.Margin = new Padding(0, 0, 0, 16);
        _grid.MultiSelect = false;
        _grid.Name = "_grid";
        _grid.ReadOnly = true;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.Size = new Size(560, 150);
        _grid.TabIndex = 1;
        // 
        // _lblListCard
        // 
        _lblListCard.AutoSize = true;
        _lblListCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblListCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblListCard.Location = new Point(16, 213);
        _lblListCard.Margin = new Padding(0, 0, 0, 4);
        _lblListCard.Name = "_lblListCard";
        _lblListCard.Size = new Size(264, 19);
        _lblListCard.TabIndex = 2;
        _lblListCard.Text = "DreamineListBox — 더블클릭 시 활성화";
        // 
        // _list
        // 
        _list.AutoScrollToEnd = false;
        _list.BackColor = Color.FromArgb(22, 32, 64);
        _list.DataSource = null;
        _list.Font = new Font("Segoe UI", 10F);
        _list.ForeColor = Color.White;
        _list.Location = new Point(16, 236);
        _list.Margin = new Padding(0, 0, 0, 16);
        _list.Name = "_list";
        _list.Padding = new Padding(2);
        _list.SelectedIndex = -1;
        _list.SelectedItem = null;
        _list.Size = new Size(300, 100);
        _list.TabIndex = 3;
        // 
        // _lblLogCard
        // 
        _lblLogCard.AutoSize = true;
        _lblLogCard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblLogCard.ForeColor = Color.FromArgb(30, 144, 255);
        _lblLogCard.Location = new Point(16, 352);
        _lblLogCard.Margin = new Padding(0, 0, 0, 4);
        _lblLogCard.Name = "_lblLogCard";
        _lblLogCard.Size = new Size(381, 19);
        _lblLogCard.TabIndex = 4;
        _lblLogCard.Text = "AutoScrollToEnd — 클릭마다 로그가 추가되며 자동 스크롤";
        // 
        // _btnLogClick
        // 
        _btnLogClick.BackColor = Color.FromArgb(13, 27, 62);
        _btnLogClick.BorderColor = Color.FromArgb(45, 74, 110);
        _btnLogClick.Command = null;
        _btnLogClick.CommandParameter = null;
        _btnLogClick.Content = "Click Me";
        _btnLogClick.CornerRadius = 6;
        _btnLogClick.Font = new Font("Segoe UI", 10F);
        _btnLogClick.ForeColor = Color.White;
        _btnLogClick.IsSelected = false;
        _btnLogClick.Location = new Point(16, 375);
        _btnLogClick.Margin = new Padding(0, 0, 0, 8);
        _btnLogClick.Name = "_btnLogClick";
        _btnLogClick.ShineColor = Color.Empty;
        _btnLogClick.Size = new Size(120, 36);
        _btnLogClick.TabIndex = 5;
        // 
        // _logList
        // 
        _logList.AutoScrollToEnd = false;
        _logList.BackColor = Color.FromArgb(22, 32, 64);
        _logList.DataSource = null;
        _logList.Font = new Font("Segoe UI", 10F);
        _logList.ForeColor = Color.White;
        _logList.Location = new Point(19, 422);
        _logList.Name = "_logList";
        _logList.Padding = new Padding(2);
        _logList.SelectedIndex = -1;
        _logList.SelectedItem = null;
        _logList.Size = new Size(560, 100);
        _logList.TabIndex = 6;
        // 
        // dataGridViewTextBoxColumn1
        // 
        dataGridViewTextBoxColumn1.HeaderText = "No";
        dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
        dataGridViewTextBoxColumn1.ReadOnly = true;
        // 
        // dataGridViewTextBoxColumn2
        // 
        dataGridViewTextBoxColumn2.HeaderText = "Name";
        dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
        dataGridViewTextBoxColumn2.ReadOnly = true;
        // 
        // dataGridViewTextBoxColumn3
        // 
        dataGridViewTextBoxColumn3.HeaderText = "Status";
        dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
        dataGridViewTextBoxColumn3.ReadOnly = true;
        // 
        // _tabMisc
        // 
        _tabMisc.BackColor = Color.FromArgb(26, 26, 46);
        _tabMisc.Controls.Add(_tabMiscFlow);
        _tabMisc.ForeColor = Color.White;
        _tabMisc.Location = new Point(4, 40);
        _tabMisc.Name = "_tabMisc";
        _tabMisc.Size = new Size(672, 376);
        _tabMisc.AutoScroll = true;
        _tabMisc.TabIndex = 5;
        _tabMisc.Text = "Misc";
        // 
        // _tabMiscFlow
        // 
        _tabMiscFlow.BackColor = Color.FromArgb(26, 26, 46);
        _tabMiscFlow.Controls.Add(_lblExpCard);
        _tabMiscFlow.Controls.Add(_expander);
        _tabMiscFlow.Dock = DockStyle.Top;
        _tabMiscFlow.AutoSize = true;
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
        _lblExpInner.Location = new Point(12, 12);
        _lblExpInner.Margin = new Padding(12);
        _lblExpInner.Name = "_lblExpInner";
        _lblExpInner.Size = new Size(244, 15);
        _lblExpInner.TabIndex = 0;
        _lblExpInner.Text = "이 안에 어떤 컨트롤이든 배치할 수 있습니다.";
        //
        // _statusLabel
        //
        _statusLabel.AutoSize = true;
        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.Font = new Font("Segoe UI", 9F);
        _statusLabel.ForeColor = Color.FromArgb(136, 153, 170);
        _statusLabel.Margin = new Padding(0, 8, 0, 0);
        _statusLabel.Name = "_statusLabel";
        _statusLabel.Padding = new Padding(0, 4, 0, 0);
        _statusLabel.Size = new Size(57, 15);
        _statusLabel.TabIndex = 2;
        _statusLabel.Text = "Status: —";
        //
        // _layout
        //
        // Panel + Dock 조합으로 바꿔서, 창 크기가 커지면 _tabs(Fill)도 같이 늘어나게 한다.
        // (이전엔 FlowLayoutPanel이라 _tabs 크기가 고정값으로 굳어 있어서 창만 커지고
        //  내용은 그대로인 채 빈 공간만 늘어났다.)
        _title.Dock = DockStyle.Top;
        _tabs.Dock = DockStyle.Fill;
        // WinForms는 "나중에 추가된 컨트롤이 먼저 도킹된다" — Fill을 가장 먼저 추가해야
        // Top/Bottom이 나중에(즉, 먼저 처리되어) 자기 영역을 확보하고 남은 공간을 Fill이 채운다.
        _layout.Controls.Add(_tabs);
        _layout.Controls.Add(_title);
        _layout.Controls.Add(_statusLabel);
        _layout.Dock = DockStyle.Fill;
        _layout.Name = "_layout";
        _layout.TabIndex = 0;
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
        _cornerRow.ResumeLayout(false);
        _diameterRow.ResumeLayout(false);
        _tabText.ResumeLayout(false);
        _tabTextFlow.ResumeLayout(false);
        _tabTextFlow.PerformLayout();
        _tbRow.ResumeLayout(false);
        _pbRow.ResumeLayout(false);
        _tabCombo.ResumeLayout(false);
        _tabComboFlow.ResumeLayout(false);
        _tabComboFlow.PerformLayout();
        _tabGrid.ResumeLayout(false);
        _tabGridFlow.ResumeLayout(false);
        _tabGridFlow.PerformLayout();
        ((ISupportInitialize)_grid).EndInit();
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
