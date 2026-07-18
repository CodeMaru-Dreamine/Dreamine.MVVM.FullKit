namespace SampleCrossUi.Maui;

/// <summary>
/// \if KO
/// <para>App 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates app functionality and related state.</para>
/// \endif
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// \if KO
    /// <para>main Page 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the main page value.</para>
    /// \endif
    /// </summary>
    private readonly MainPage _mainPage;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="App"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="App"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="mainPage">
    /// \if KO
    /// <para>main Page에 사용할 <c>MainPage</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MainPage</c> value used for main page.</para>
    /// \endif
    /// </param>
    public App(MainPage mainPage)
    {
        InitializeComponent();
        _mainPage = mainPage;
    }

    /// <summary>
    /// \if KO
    /// <para>Window 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the window value.</para>
    /// \endif
    /// </summary>
    /// <param name="activationState">
    /// \if KO
    /// <para>activation State에 사용할 <c>IActivationState?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IActivationState?</c> value used for activation state.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Window 작업에서 생성한 <c>Window</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Window</c> result produced by the create window operation.</para>
    /// \endif
    /// </returns>
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
