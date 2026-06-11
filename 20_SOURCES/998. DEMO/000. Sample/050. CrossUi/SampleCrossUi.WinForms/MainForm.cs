using System.ComponentModel;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.WinForms;

/// <summary>
/// Main WinForms window demonstrating CounterViewModel reuse without full MVVM.
/// The ViewModel is injected via constructor; UI subscribes to PropertyChanged
/// and delegates button clicks to ViewModel commands.
/// </summary>
public sealed class MainForm : Form
{
    private readonly CounterViewModel _viewModel;
    private readonly Label _countLabel;
    private readonly ListBox _logListBox;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainForm"/> class.
    /// </summary>
    /// <param name="viewModel">The counter ViewModel.</param>
    public MainForm(CounterViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;

        Text = "Dreamine Cross-UI — WinForms";
        ClientSize = new Size(480, 540);
        StartPosition = FormStartPosition.CenterScreen;

        // --- Title ---
        var titleLabel = new Label
        {
            Text = "Dreamine Cross-UI Sample — WinForms",
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // --- Count ---
        _countLabel = new Label
        {
            Text = $"Count: {_viewModel.Count}",
            Font = new Font(Font.FontFamily, 24),
            Dock = DockStyle.Top,
            Height = 50,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // --- Buttons ---
        var incrementButton = new Button
        {
            Text = "Increment",
            Width = 100, Height = 34
        };
        var resetButton = new Button
        {
            Text = "Reset",
            Width = 100, Height = 34,
            Left = 112
        };

        var buttonPanel = new Panel { Dock = DockStyle.Top, Height = 44 };
        buttonPanel.Controls.Add(incrementButton);
        buttonPanel.Controls.Add(resetButton);

        // --- Log list ---
        var logLabel = new Label
        {
            Text = "Operation Log:",
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 22
        };

        _logListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            HorizontalScrollbar = true
        };

        // Layout (reverse order for DockStyle.Top stacking)
        Controls.Add(_logListBox);
        Controls.Add(logLabel);
        Controls.Add(buttonPanel);
        Controls.Add(_countLabel);
        Controls.Add(titleLabel);

        // Wire up
        incrementButton.Click += (_, _) => _viewModel.IncrementCommand.Execute(null);
        resetButton.Click    += (_, _) => _viewModel.ResetCommand.Execute(null);

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.Logs.CollectionChanged += (_, _) => RefreshLog();

        RefreshLog();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CounterViewModel.Count))
        {
            _countLabel.Text = $"Count: {_viewModel.Count}";
        }
    }

    private void RefreshLog()
    {
        _logListBox.BeginUpdate();
        _logListBox.Items.Clear();
        foreach (var item in _viewModel.Logs)
        {
            _logListBox.Items.Add($"[{item.CreatedAt:HH:mm:ss}] {item.Message}");
        }
        _logListBox.EndUpdate();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        base.Dispose(disposing);
    }
}
