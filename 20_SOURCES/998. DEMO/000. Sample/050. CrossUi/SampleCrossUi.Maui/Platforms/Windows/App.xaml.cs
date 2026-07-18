using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace SampleCrossUi.Maui.WinUI;

/// <summary>
/// \if KO
/// <para>App 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates app functionality and related state.</para>
/// \endif
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="App"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="App"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// \if KO
    /// <para>Maui App 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the maui app value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Create Maui App 작업에서 생성한 <c>MauiApp</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MauiApp</c> result produced by the create maui app operation.</para>
    /// \endif
    /// </returns>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
