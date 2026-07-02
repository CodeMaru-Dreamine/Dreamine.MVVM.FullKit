using SampleCrossUi.Maui.Views;

namespace SampleCrossUi.Maui;

public partial class MainPage : ContentPage
{
    private readonly CounterPage _counterPage;
    private readonly LightBulbPage _lightBulbPage;
    private readonly ControlsPage _controlsPage;
    private readonly PopupPage _popupPage;

    public MainPage(CounterPage counterPage, LightBulbPage lightBulbPage, ControlsPage controlsPage, PopupPage popupPage)
    {
        InitializeComponent();
        _counterPage = counterPage;
        _lightBulbPage = lightBulbPage;
        _controlsPage = controlsPage;
        _popupPage = popupPage;

        Navigate(_counterPage);
    }

    private void OnNavCounterClicked(object? sender, EventArgs e) => Navigate(_counterPage);

    private void OnNavLightBulbClicked(object? sender, EventArgs e) => Navigate(_lightBulbPage);

    private void OnNavControlsClicked(object? sender, EventArgs e) => Navigate(_controlsPage);

    private void OnNavPopupClicked(object? sender, EventArgs e) => Navigate(_popupPage);

    private void Navigate(View page) => PageHost.Content = page;
}
