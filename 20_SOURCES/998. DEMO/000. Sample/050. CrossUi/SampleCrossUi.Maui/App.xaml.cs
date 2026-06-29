namespace SampleCrossUi.Maui;

public partial class App : Application
{
    private readonly MainPage _mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();
        _mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // WPF/WinForms 샘플과 비슷한 크기로 시작하게 한다(기본값은 OS가 화면을 거의 다 채움).
        var window = new Window(new NavigationPage(_mainPage))
        {
            Width = 1100,
            Height = 860,
            MinimumWidth = 900,
            MinimumHeight = 650
        };
        return window;
    }
}
