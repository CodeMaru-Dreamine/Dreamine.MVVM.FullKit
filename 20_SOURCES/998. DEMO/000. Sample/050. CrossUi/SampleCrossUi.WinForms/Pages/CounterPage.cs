using System.ComponentModel;
using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;
using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.WinForms.Pages;

/// <summary>
/// \if KO
/// <para>Counter Page 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates counter page functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CounterPage : UserControl
{
    /// <summary>
    /// \if KO
    /// <para>title 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the title value.</para>
    /// \endif
    /// </summary>
    private Label           _title        = null!;
    /// <summary>
    /// \if KO
    /// <para>count Label 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the count label value.</para>
    /// \endif
    /// </summary>
    private Label           _countLabel   = null!;
    /// <summary>
    /// \if KO
    /// <para>btn Increment 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the btn increment value.</para>
    /// \endif
    /// </summary>
    private DreamineButton  _btnIncrement = null!;
    /// <summary>
    /// \if KO
    /// <para>btn Reset 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the btn reset value.</para>
    /// \endif
    /// </summary>
    private DreamineButton  _btnReset     = null!;
    /// <summary>
    /// \if KO
    /// <para>btn Panel 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the btn panel value.</para>
    /// \endif
    /// </summary>
    private FlowLayoutPanel _btnPanel     = null!;
    /// <summary>
    /// \if KO
    /// <para>log Title 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the log title value.</para>
    /// \endif
    /// </summary>
    private Label           _logTitle     = null!;
    /// <summary>
    /// \if KO
    /// <para>log List 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the log list value.</para>
    /// \endif
    /// </summary>
    private ListBox         _logList      = null!;
    /// <summary>
    /// \if KO
    /// <para>layout 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the layout value.</para>
    /// \endif
    /// </summary>
    private FlowLayoutPanel _layout       = null!;

    /// <summary>
    /// \if KO
    /// <para>vm 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the vm value.</para>
    /// \endif
    /// </summary>
    private readonly CounterViewModel _vm;

    /// <summary>
    /// \if KO
    /// <para>VS WinForms 디자이너용 기본 생성자.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="CounterPage"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public CounterPage() : this(new CounterViewModel(new CounterEvent(new CounterService()))) { }

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="CounterPage"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="CounterPage"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="vm">
    /// \if KO
    /// <para>vm에 사용할 <c>CounterViewModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CounterViewModel</c> value used for vm.</para>
    /// \endif
    /// </param>
    public CounterPage(CounterViewModel vm)
    {
        _vm = vm;
        InitializeComponent();

        _btnIncrement.Click += (_, _) => _vm.IncrementCommand.Execute(null);
        _btnReset.Click     += (_, _) => _vm.ResetCommand.Execute(null);

        _vm.PropertyChanged              += OnPropertyChanged;
        _vm.Logs.CollectionChanged       += (_, _) => RefreshLog();
        RefreshLog();
    }

    /// <summary>
    /// \if KO
    /// <para>Property Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the property changed event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CounterViewModel.Count))
            _countLabel.Text = $"Count: {_vm.Count}";
    }

    /// <summary>
    /// \if KO
    /// <para>Refresh Log 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh log operation.</para>
    /// \endif
    /// </summary>
    private void RefreshLog()
    {
        _logList.BeginUpdate();
        _logList.Items.Clear();
        foreach (var item in _vm.Logs)
            _logList.Items.Add($"[{item.CreatedAt:HH:mm:ss}] {item.Message}");
        _logList.EndUpdate();
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
        _title        = new Label();
        _countLabel   = new Label();
        _btnIncrement = new DreamineButton();
        _btnReset     = new DreamineButton();
        _btnPanel     = new FlowLayoutPanel();
        _logTitle     = new Label();
        _logList      = new ListBox();
        _layout       = new FlowLayoutPanel();
        _layout.SuspendLayout();
        SuspendLayout();
        //
        // _title
        //
        _title.AutoSize = true;
        _title.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        _title.ForeColor = Color.White;
        _title.Location = new Point(0, 0);
        _title.Margin = new Padding(0, 0, 0, 16);
        _title.Name = "_title";
        _title.Size = new Size(120, 37);
        _title.TabIndex = 0;
        _title.Text = "Counter";
        //
        // _countLabel
        //
        _countLabel.AutoSize = true;
        _countLabel.Font = new Font("Segoe UI", 36F, FontStyle.Bold);
        _countLabel.ForeColor = DreamineTheme.AccentBlue;
        _countLabel.Location = new Point(0, 53);
        _countLabel.Margin = new Padding(0, 0, 0, 16);
        _countLabel.Name = "_countLabel";
        _countLabel.Size = new Size(218, 65);
        _countLabel.TabIndex = 1;
        _countLabel.Text = "Count: 0";
        //
        // _btnIncrement
        //
        _btnIncrement.Content = "Increment";
        _btnIncrement.Width   = 140;
        _btnIncrement.Height  = 40;
        _btnIncrement.Margin  = new Padding(0, 0, 8, 0);
        _btnIncrement.Name    = "_btnIncrement";
        _btnIncrement.TabIndex = 0;
        //
        // _btnReset
        //
        _btnReset.Content  = "Reset";
        _btnReset.Width    = 100;
        _btnReset.Height   = 40;
        _btnReset.Name     = "_btnReset";
        _btnReset.TabIndex = 1;
        //
        // _btnPanel
        //
        _btnPanel.AutoSize = true;
        _btnPanel.FlowDirection = FlowDirection.LeftToRight;
        _btnPanel.Location = new Point(0, 134);
        _btnPanel.Margin = new Padding(0, 0, 0, 24);
        _btnPanel.Name = "_btnPanel";
        _btnPanel.Size = new Size(0, 0);
        _btnPanel.TabIndex = 2;
        _btnPanel.Controls.Add(_btnIncrement);
        _btnPanel.Controls.Add(_btnReset);
        //
        // _logTitle
        //
        _logTitle.AutoSize = true;
        _logTitle.Font = new Font("Segoe UI", 10F);
        _logTitle.Location = new Point(0, 158);
        _logTitle.Margin = new Padding(0, 0, 0, 4);
        _logTitle.Name = "_logTitle";
        _logTitle.Size = new Size(98, 19);
        _logTitle.TabIndex = 3;
        _logTitle.Text = "Operation Log";
        // 
        // _logList
        // 
        _logList.BorderStyle = BorderStyle.FixedSingle;
        _logList.Font = new Font("Segoe UI", 9F);
        _logList.ItemHeight = 15;
        _logList.Location = new Point(3, 184);
        _logList.Name = "_logList";
        _logList.Size = new Size(400, 197);
        _logList.TabIndex = 4;
        // 
        // _layout
        // 
        _layout.AutoSize = true;
        _layout.Controls.Add(_title);
        _layout.Controls.Add(_countLabel);
        _layout.Controls.Add(_btnPanel);
        _layout.Controls.Add(_logTitle);
        _layout.Controls.Add(_logList);
        _layout.Dock = DockStyle.Fill;
        _layout.FlowDirection = FlowDirection.TopDown;
        _layout.Location = new Point(24, 24);
        _layout.Name = "_layout";
        _layout.Size = new Size(1133, 1225);
        _layout.TabIndex = 0;
        _layout.WrapContents = false;
        // 
        // CounterPage
        // 
        Controls.Add(_layout);
        Name = "CounterPage";
        Padding = new Padding(24);
        Size = new Size(1181, 1273);
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
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
        if (disposing) _vm.PropertyChanged -= OnPropertyChanged;
        base.Dispose(disposing);
    }
}
