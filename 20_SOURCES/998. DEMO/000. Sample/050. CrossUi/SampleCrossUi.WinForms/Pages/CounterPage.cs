using System.ComponentModel;
using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;
using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.WinForms.Pages;

public sealed class CounterPage : UserControl
{
    private readonly CounterViewModel _vm;
    private readonly Label _countLabel = null!;
    private readonly ListBox _logList = null!;

    /// <summary>VS WinForms 디자이너용 기본 생성자.</summary>
    public CounterPage() : this(new CounterViewModel(new CounterService())) { }

    public CounterPage(CounterViewModel vm)
    {
        _vm = vm;
        BackColor = DreamineTheme.AppBackground;
        Dock = DockStyle.Fill;
        Padding = new Padding(24);

        // Title
        var title = new Label
        {
            Text = "Counter",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Point),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16),
        };

        // Count display
        _countLabel = new Label
        {
            Text = $"Count: {_vm.Count}",
            ForeColor = DreamineTheme.AccentBlue,
            Font = new Font("Segoe UI", 36f, FontStyle.Bold, GraphicsUnit.Point),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16),
        };

        // Buttons
        var btnIncrement = new DreamineButton { Content = "Increment", Width = 140, Height = 40, Margin = new Padding(0, 0, 8, 0) };
        var btnReset     = new DreamineButton { Content = "Reset",     Width = 100, Height = 40 };

        var btnPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, 24),
        };
        btnPanel.Controls.Add(btnIncrement);
        btnPanel.Controls.Add(btnReset);

        // Log
        var logTitle = new Label
        {
            Text = "Operation Log",
            ForeColor = DreamineTheme.TextSecondary,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4),
        };

        _logList = new ListBox
        {
            BackColor = DreamineTheme.CardBackground,
            ForeColor = DreamineTheme.TextPrimary,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
            Width = 400,
            Height = 200,
            BorderStyle = BorderStyle.FixedSingle,
        };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
        };
        layout.Controls.Add(title);
        layout.Controls.Add(_countLabel);
        layout.Controls.Add(btnPanel);
        layout.Controls.Add(logTitle);
        layout.Controls.Add(_logList);
        Controls.Add(layout);

        // Wire
        btnIncrement.Click += (_, _) => _vm.IncrementCommand.Execute(null);
        btnReset.Click     += (_, _) => _vm.ResetCommand.Execute(null);

        _vm.PropertyChanged += OnPropertyChanged;
        _vm.Logs.CollectionChanged += (_, _) => RefreshLog();
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

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.PropertyChanged -= OnPropertyChanged;
        base.Dispose(disposing);
    }
}
