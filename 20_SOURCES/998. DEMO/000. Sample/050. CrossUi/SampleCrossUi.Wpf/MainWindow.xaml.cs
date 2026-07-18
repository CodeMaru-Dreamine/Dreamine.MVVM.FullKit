using System.Windows;
using Dreamine.MVVM.Core;
using Dreamine.UI.Wpf.Controls.Navigation;
using Dreamine.UI.Wpf.Localization;
using SampleCrossUi.Wpf.ViewModels;

namespace SampleCrossUi.Wpf;

/// <summary>
/// \if KO
/// <para>Main Window 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates main window functionality and related state.</para>
/// \endif
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="MainWindow"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="MainWindow"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = DMContainer.Resolve<MainViewModel>();

        // DreamineNavigationBar.AutoLogoutOccurred is static — subscribe once per window.
        DreamineNavigationBar.AutoLogoutOccurred += OnAutoLogoutOccurred;
        Closed += (_, _) => DreamineNavigationBar.AutoLogoutOccurred -= OnAutoLogoutOccurred;
    }

    /// <summary>
    /// \if KO
    /// <para>Toggle Language Click 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the toggle language click event or state change.</para>
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
    private void OnToggleLanguageClick(object sender, RoutedEventArgs e)
    {
        StaticLocalizationProxy.Instance.CurrentLanguage =
            StaticLocalizationProxy.Instance.CurrentLanguage == Dreamine.UI.Wpf.Localization.Language.Korean
                ? Dreamine.UI.Wpf.Localization.Language.English
                : Dreamine.UI.Wpf.Localization.Language.Korean;
    }

    /// <summary>
    /// \if KO
    /// <para>DreamineNavigationBar.AutoLogoutTimeout 데모. 8초간 마우스/키보드 입력이 없으면 AutoLogoutOccurred가 발생하고(소유 창 자동 Hide + 안내 메시지), 다시 입력하면 타이머가 리셋된다. 평소엔 0(끔)으로 두고, 이 버튼을 눌렀을 때만 데모용으로 활성화한다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the start auto logout test click event or state change.</para>
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
    private void OnStartAutoLogoutTestClick(object sender, RoutedEventArgs e)
    {
        MainNavBar.AutoLogoutTimeout = 8;
    }

    /// <summary>
    /// \if KO
    /// <para>Auto Logout Occurred 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the auto logout occurred event or state change.</para>
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
    private void OnAutoLogoutOccurred(object? sender, System.EventArgs e)
    {
        MainNavBar.AutoLogoutTimeout = 0; // 한 번 시연했으면 다시 끔(반복 트리거 방지)
    }
}
