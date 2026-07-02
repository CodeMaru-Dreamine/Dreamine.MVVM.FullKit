using System.ComponentModel;
using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.WinForms.Pages;

public sealed class LightBulbPage : UserControl
{
    private readonly LightBulbViewModel _vm;
    private readonly Label _title;
    private readonly Label _description;
    private readonly DreamineLightBulb _bulb;
    private readonly Label _status;
    private readonly DreamineButton _toggleButton;
    private readonly CheckBox _powerCheckBox;
    private readonly Label _count;
    private bool _refreshing;

    public LightBulbPage() : this(new LightBulbViewModel(new LightBulbEvent(new LightBulbModel()))) { }

    public LightBulbPage(LightBulbViewModel vm)
    {
        _vm = vm;

        BackColor = DreamineTheme.AppBackground;
        Dock = DockStyle.Fill;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(24),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _title = new Label
        {
            Text = "Light Bulb",
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 0, 0, 8)
        };

        _description = new Label
        {
            Text = "A button command and a checkbox binding drive the same shared state.",
            AutoSize = true,
            ForeColor = Color.FromArgb(184, 198, 221),
            Font = new Font("Segoe UI", 10f),
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 0, 0, 18)
        };

        _bulb = new DreamineLightBulb
        {
            Width = 160,
            Height = 170,
            Diameter = 112,
            Anchor = AnchorStyles.None,
            BackColor = DreamineTheme.AppBackground,
            Margin = new Padding(0, 0, 0, 6)
        };

        _status = new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(141, 203, 255),
            Font = new Font("Consolas", 10f, FontStyle.Bold),
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 0, 0, 18)
        };

        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            Anchor = AnchorStyles.None,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };

        _toggleButton = new DreamineButton
        {
            Content = "Toggle",
            Width = 110,
            Height = 40,
            Margin = new Padding(0, 0, 14, 0),
        };

        _powerCheckBox = new CheckBox
        {
            Text = "Power",
            AutoSize = true,
            ForeColor = Color.White,
            BackColor = DreamineTheme.AppBackground,
            Margin = new Padding(0, 10, 14, 0),
        };

        _count = new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(217, 227, 241),
            Font = new Font("Consolas", 9f),
            Margin = new Padding(0, 12, 0, 0),
        };

        row.Controls.Add(_toggleButton);
        row.Controls.Add(_powerCheckBox);
        row.Controls.Add(_count);

        layout.Controls.Add(_title, 0, 0);
        layout.Controls.Add(_description, 0, 1);
        layout.Controls.Add(_bulb, 0, 2);
        layout.Controls.Add(_status, 0, 3);
        layout.Controls.Add(row, 0, 4);
        Controls.Add(layout);

        _toggleButton.Click += (_, _) => _vm.ToggleCommand.Execute(null);
        _powerCheckBox.CheckedChanged += (_, _) =>
        {
            if (_refreshing || _vm.IsOn == _powerCheckBox.Checked) return;
            _vm.ToggleCommand.Execute(null);
        };
        _vm.PropertyChanged += OnViewModelPropertyChanged;

        RefreshState();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(RefreshState);
        else RefreshState();
    }

    private void RefreshState()
    {
        _refreshing = true;
        _powerCheckBox.Checked = _vm.IsOn;
        _bulb.IsOn = _vm.IsOn;
        _status.Text = _vm.StatusText;
        _count.Text = $"Toggled {_vm.ToggleCount}x";
        _refreshing = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
        base.Dispose(disposing);
    }
}
