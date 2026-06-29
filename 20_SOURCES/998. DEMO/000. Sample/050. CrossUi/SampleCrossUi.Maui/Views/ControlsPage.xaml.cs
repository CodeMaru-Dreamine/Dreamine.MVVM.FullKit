using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.Maui.Views;

public partial class ControlsPage : ContentView
{
    private readonly ControlsViewModel _viewModel;
    private readonly (string Title, VerticalStackLayout Content)[] _tabs;
    private Button? _selectedTabButton;

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

    private void SelectTab(VerticalStackLayout target, Button tabButton)
    {
        foreach (var (_, content) in _tabs)
            content.IsVisible = content == target;

        if (_selectedTabButton is not null)
            _selectedTabButton.BackgroundColor = Color.FromArgb("#0D1B3E");

        tabButton.BackgroundColor = Color.FromArgb("#0d6efd");
        _selectedTabButton = tabButton;
    }

    private void OnVariantButtonClicked(object? sender, EventArgs e)
    {
        if (sender is Button button)
            ButtonTabStatus.Text = $"[{DateTime.Now:HH:mm:ss}] '{button.Text}' 버튼이 클릭되었습니다.";
    }

    private void OnRadioCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (!e.Value || sender is not RadioButton radio)
            return;

        var option = radio.Content?.ToString() ?? string.Empty;
        _viewModel.SelectRadioCommand.Execute(option);
    }

    private void OnGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        GridSelectionLabel.Text = e.CurrentSelection.FirstOrDefault() is ControlsDemoRow row
            ? $"선택된 행: No.{row.No} {row.Name} ({row.Status})"
            : "(선택된 행 없음)";
    }

    private void OnFruitItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Label { Text: { } text })
            _viewModel.ListBoxActivatedCommand.Execute(text);
    }

    private async void OnLogClickClicked(object? sender, EventArgs e)
    {
        _viewModel.ClickMeCommand.Execute(null);
        await Task.Delay(50);
        if (_viewModel.ActivityLog.Count > 0)
            LogList.ScrollTo(_viewModel.ActivityLog[^1], position: ScrollToPosition.End, animate: true);
    }
}
