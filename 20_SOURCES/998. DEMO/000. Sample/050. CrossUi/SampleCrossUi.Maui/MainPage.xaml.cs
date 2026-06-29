using SampleCrossUi.Maui.Views;

namespace SampleCrossUi.Maui;

public partial class MainPage : ContentPage
{
    private readonly CounterPage _counterPage;
    private readonly ControlsPage _controlsPage;
    private readonly PopupPage _popupPage;

    public MainPage(CounterPage counterPage, ControlsPage controlsPage, PopupPage popupPage)
    {
        InitializeComponent();
        _counterPage = counterPage;
        _controlsPage = controlsPage;
        _popupPage = popupPage;

        Navigate(_counterPage);
    }

    private void OnNavCounterClicked(object? sender, EventArgs e) => Navigate(_counterPage);

    private void OnNavControlsClicked(object? sender, EventArgs e) => Navigate(_controlsPage);

    private void OnNavPopupClicked(object? sender, EventArgs e) => Navigate(_popupPage);

    private void Navigate(View page) => PageHost.Content = page;
}
