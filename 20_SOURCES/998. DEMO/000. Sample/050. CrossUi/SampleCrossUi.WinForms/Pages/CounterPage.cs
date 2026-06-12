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

        SuspendLayout();

        // _title
        _title.Text      = "Counter";
        _title.ForeColor = Color.White;
        _title.Font      = new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Point);
        _title.AutoSize  = true;
        _title.Margin    = new Padding(0, 0, 0, 16);

        // _countLabel
        _countLabel.Text      = "Count: 0";
        _countLabel.ForeColor = DreamineTheme.AccentBlue;
        _countLabel.Font      = new Font("Segoe UI", 36f, FontStyle.Bold, GraphicsUnit.Point);
        _countLabel.AutoSize  = true;
        _countLabel.Margin    = new Padding(0, 0, 0, 16);

        // buttons
        _btnIncrement.Content = "Increment";
        _btnIncrement.Width   = 140;
        _btnIncrement.Height  = 40;
        _btnIncrement.Margin  = new Padding(0, 0, 8, 0);

        _btnReset.Content = "Reset";
        _btnReset.Width   = 100;
        _btnReset.Height  = 40;

        // _btnPanel
        _btnPanel.AutoSize       = true;
        _btnPanel.FlowDirection  = FlowDirection.LeftToRight;
        _btnPanel.Margin         = new Padding(0, 0, 0, 24);
        _btnPanel.Controls.Add(_btnIncrement);
        _btnPanel.Controls.Add(_btnReset);

        // _logTitle
        _logTitle.Text      = "Operation Log";
        _logTitle.ForeColor = DreamineTheme.TextSecondary;
        _logTitle.Font      = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
        _logTitle.AutoSize  = true;
        _logTitle.Margin    = new Padding(0, 0, 0, 4);

        // _logList
        _logList.BackColor   = DreamineTheme.CardBackground;
        _logList.ForeColor   = DreamineTheme.TextPrimary;
        _logList.Font        = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        _logList.Width       = 400;
        _logList.Height      = 200;
        _logList.BorderStyle = BorderStyle.FixedSingle;

        // _layout
        _layout.Dock          = DockStyle.Fill;
        _layout.FlowDirection = FlowDirection.TopDown;
        _layout.AutoSize      = true;
        _layout.WrapContents  = false;
        _layout.Controls.Add(_title);
        _layout.Controls.Add(_countLabel);
        _layout.Controls.Add(_btnPanel);
        _layout.Controls.Add(_logTitle);
        _layout.Controls.Add(_logList);

        // UserControl
        BackColor = DreamineTheme.AppBackground;
        Dock      = DockStyle.Fill;
        Padding   = new Padding(24);
        Name      = "CounterPage";
        Controls.Add(_layout);

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.PropertyChanged -= OnPropertyChanged;
        base.Dispose(disposing);
    }
}
