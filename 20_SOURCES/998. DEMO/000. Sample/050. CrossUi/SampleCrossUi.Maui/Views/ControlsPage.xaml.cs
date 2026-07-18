using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.Maui.Views;

/// <summary>
/// \if KO
/// <para>Controls Page 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates controls page functionality and related state.</para>
/// \endif
/// </summary>
public partial class ControlsPage : ContentView
{
    /// <summary>
    /// \if KO
    /// <para>view Model 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the view model value.</para>
    /// \endif
    /// </summary>
    private readonly ControlsViewModel _viewModel;
    /// <summary>
    /// \if KO
    /// <para>tabs 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tabs value.</para>
    /// \endif
    /// </summary>
    private readonly (string Title, VerticalStackLayout Content)[] _tabs;
    /// <summary>
    /// \if KO
    /// <para>selected Tab Button 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the selected tab button value.</para>
    /// \endif
    /// </summary>
    private Button? _selectedTabButton;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="ControlsPage"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="ControlsPage"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="viewModel">
    /// \if KO
    /// <para>view Model에 사용할 <c>ControlsViewModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ControlsViewModel</c> value used for view model.</para>
    /// \endif
    /// </param>
    public ControlsPage(ControlsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        RadioOptionA.IsChecked = _viewModel.SelectedRadio == "Option A";
        RadioOptionB.IsChecked = _viewModel.SelectedRadio == "Option B";

        VirtualKeyboard.Attach(MainEntry);

        _tabs = new (string, VerticalStackLayout)[]
        {
            ("Button", TabButton),
            ("CheckBox / Radio", TabCheckRadio),
            ("CheckLed", TabCheckLed),
            ("TextBox", TabTextBox),
            ("ComboBox", TabComboBox),
            ("DataGrid / ListBox", TabGridList),
            ("Misc", TabMisc),
        };

        foreach (var (title, content) in _tabs)
        {
            var tabButton = new Button
            {
                Text = title,
                BackgroundColor = Color.FromArgb("#0D1B3E"),
                TextColor = Colors.White,
                CornerRadius = 6,
                FontSize = 12,
                Padding = new Thickness(10, 4)
            };
            tabButton.Clicked += (_, _) => SelectTab(content, tabButton);
            TabRow.Children.Add(tabButton);

            if (content == TabButton)
            {
                _selectedTabButton = tabButton;
                tabButton.BackgroundColor = Color.FromArgb("#0d6efd");
            }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Select Tab 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the select tab operation.</para>
    /// \endif
    /// </summary>
    /// <param name="target">
    /// \if KO
    /// <para>target에 사용할 <c>VerticalStackLayout</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>VerticalStackLayout</c> value used for target.</para>
    /// \endif
    /// </param>
    /// <param name="tabButton">
    /// \if KO
    /// <para>tab Button에 사용할 <c>Button</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Button</c> value used for tab button.</para>
    /// \endif
    /// </param>
    private void SelectTab(VerticalStackLayout target, Button tabButton)
    {
        foreach (var (_, content) in _tabs)
            content.IsVisible = content == target;

        if (_selectedTabButton is not null)
            _selectedTabButton.BackgroundColor = Color.FromArgb("#0D1B3E");

        tabButton.BackgroundColor = Color.FromArgb("#0d6efd");
        _selectedTabButton = tabButton;
    }

    /// <summary>
    /// \if KO
    /// <para>Variant Button Clicked 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the variant button clicked event or state change.</para>
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
    private void OnVariantButtonClicked(object? sender, EventArgs e)
    {
        if (sender is Button button)
            ButtonTabStatus.Text = $"[{DateTime.Now:HH:mm:ss}] '{button.Text}' 버튼이 클릭되었습니다.";
    }

    /// <summary>
    /// \if KO
    /// <para>Radio Checked Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the radio checked changed event or state change.</para>
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
    private void OnRadioCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (!e.Value || sender is not RadioButton radio)
            return;

        var option = radio.Content?.ToString() ?? string.Empty;
        _viewModel.SelectRadioCommand.Execute(option);
    }

    /// <summary>
    /// \if KO
    /// <para>Grid Selection Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the grid selection changed event or state change.</para>
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
    private void OnGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        GridSelectionLabel.Text = e.CurrentSelection.FirstOrDefault() is ControlsDemoRow row
            ? $"선택된 행: No.{row.No} {row.Name} ({row.Status})"
            : "(선택된 행 없음)";
    }

    /// <summary>
    /// \if KO
    /// <para>Fruit Item Double Tapped 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the fruit item double tapped event or state change.</para>
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
    private void OnFruitItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Label { Text: { } text })
            _viewModel.ListBoxActivatedCommand.Execute(text);
    }

    /// <summary>
    /// \if KO
    /// <para>Log Click Clicked 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the log click clicked event or state change.</para>
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
    private async void OnLogClickClicked(object? sender, EventArgs e)
    {
        _viewModel.ClickMeCommand.Execute(null);
        await Task.Delay(50);
        if (_viewModel.ActivityLog.Count > 0)
            LogList.ScrollTo(_viewModel.ActivityLog[^1], position: ScrollToPosition.End, animate: true);
    }
}
