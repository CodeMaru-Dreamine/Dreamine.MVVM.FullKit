using System.Windows;
using Dreamine.MVVM.Core;
using Dreamine.UI.Wpf.Controls.Navigation;
using Dreamine.UI.Wpf.Localization;
using SampleCrossUi.Wpf.ViewModels;

namespace SampleCrossUi.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = DMContainer.Resolve<MainViewModel>();

        // DreamineNavigationBar.AutoLogoutOccurred is static — subscribe once per window.
        DreamineNavigationBar.AutoLogoutOccurred += OnAutoLogoutOccurred;
        Closed += (_, _) => DreamineNavigationBar.AutoLogoutOccurred -= OnAutoLogoutOccurred;
    }

    private void OnToggleLanguageClick(object sender, RoutedEventArgs e)
    {
        StaticLocalizationProxy.Instance.CurrentLanguage =
            StaticLocalizationProxy.Instance.CurrentLanguage == Dreamine.UI.Wpf.Localization.Language.Korean
                ? Dreamine.UI.Wpf.Localization.Language.English
                : Dreamine.UI.Wpf.Localization.Language.Korean;
    }

    /// <summary>
    /// DreamineNavigationBar.AutoLogoutTimeout 데모. 8초간 마우스/키보드 입력이 없으면
    /// AutoLogoutOccurred가 발생하고(소유 창 자동 Hide + 안내 메시지), 다시 입력하면 타이머가 리셋된다.
    /// 평소엔 0(끔)으로 두고, 이 버튼을 눌렀을 때만 데모용으로 활성화한다.
    /// </summary>
    private void OnStartAutoLogoutTestClick(object sender, RoutedEventArgs e)
    {
        MainNavBar.AutoLogoutTimeout = 8;
    }

    private void OnAutoLogoutOccurred(object? sender, System.EventArgs e)
    {
        MainNavBar.AutoLogoutTimeout = 0; // 한 번 시연했으면 다시 끔(반복 트리거 방지)
    }
}
