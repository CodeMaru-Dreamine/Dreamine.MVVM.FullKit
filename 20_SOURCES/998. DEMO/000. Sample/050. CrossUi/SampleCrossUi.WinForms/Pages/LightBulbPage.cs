using System.ComponentModel;
using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.WinForms.Pages;

/// <summary>
/// \if KO
/// <para>Light Bulb Page 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates light bulb page functionality and related state.</para>
/// \endif
/// </summary>
public sealed class LightBulbPage : UserControl
{
    /// <summary>
    /// \if KO
    /// <para>vm 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the vm value.</para>
    /// \endif
    /// </summary>
    private readonly LightBulbViewModel _vm;
    /// <summary>
    /// \if KO
    /// <para>title 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the title value.</para>
    /// \endif
    /// </summary>
    private readonly Label _title;
    /// <summary>
    /// \if KO
    /// <para>description 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the description value.</para>
    /// \endif
    /// </summary>
    private readonly Label _description;
    /// <summary>
    /// \if KO
    /// <para>bulb 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the bulb value.</para>
    /// \endif
    /// </summary>
    private readonly DreamineLightBulb _bulb;
    /// <summary>
    /// \if KO
    /// <para>status 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the status value.</para>
    /// \endif
    /// </summary>
    private readonly Label _status;
    /// <summary>
    /// \if KO
    /// <para>toggle Button 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the toggle button value.</para>
    /// \endif
    /// </summary>
    private readonly DreamineButton _toggleButton;
    /// <summary>
    /// \if KO
    /// <para>power Check Box 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the power check box value.</para>
    /// \endif
    /// </summary>
    private readonly CheckBox _powerCheckBox;
    /// <summary>
    /// \if KO
    /// <para>count 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the count value.</para>
    /// \endif
    /// </summary>
    private readonly Label _count;
    /// <summary>
    /// \if KO
    /// <para>refreshing 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the refreshing value.</para>
    /// \endif
    /// </summary>
    private bool _refreshing;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="LightBulbPage"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="LightBulbPage"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public LightBulbPage() : this(new LightBulbViewModel(new LightBulbEvent(new LightBulbModel()))) { }

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="LightBulbPage"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="LightBulbPage"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="vm">
    /// \if KO
    /// <para>vm에 사용할 <c>LightBulbViewModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>LightBulbViewModel</c> value used for vm.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>View Model Property Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the view model property changed event or state change.</para>
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
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(RefreshState);
        else RefreshState();
    }

    /// <summary>
    /// \if KO
    /// <para>Refresh State 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh state operation.</para>
    /// \endif
    /// </summary>
    private void RefreshState()
    {
        _refreshing = true;
        _powerCheckBox.Checked = _vm.IsOn;
        _bulb.IsOn = _vm.IsOn;
        _status.Text = _vm.StatusText;
        _count.Text = $"Toggled {_vm.ToggleCount}x";
        _refreshing = false;
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
        if (disposing)
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
        base.Dispose(disposing);
    }
}
