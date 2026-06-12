using System.ComponentModel;
using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;
using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.WinForms.Pages;

public sealed class CounterPage : UserControl
{
    private Label           _title        = null!;
    private Label           _countLabel   = null!;
    private DreamineButton  _btnIncrement = null!;
    private DreamineButton  _btnReset     = null!;
    private FlowLayoutPanel _btnPanel     = null!;
    private Label           _logTitle     = null!;
    private ListBox         _logList      = null!;
    private FlowLayoutPanel _layout       = null!;

    private readonly CounterViewModel _vm;

    /// <summary>VS WinForms 디자이너용 기본 생성자.</summary>
    public CounterPage() : this(new CounterViewModel(new CounterService())) { }

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

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CounterViewModel.Count))
            _countLabel.Text = $"Count: {_vm.Count}";
    }

    private void RefreshLog()
    {
        _logList.BeginUpdate();
        _logList.Items.Clear();
        foreach (var item in _vm.Logs)
            _logList.Items.Add($"[{item.CreatedAt:HH:mm:ss}] {item.Message}");
        _logList.EndUpdate();
    }

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

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.PropertyChanged -= OnPropertyChanged;
        base.Dispose(disposing);
    }
}
