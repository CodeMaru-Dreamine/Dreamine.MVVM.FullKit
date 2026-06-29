using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dreamine.UI.Abstractions.Popup;
using Dreamine.UI.Wpf.Behaviors;
using Dreamine.UI.Wpf.Controls.MessageBox;
using Dreamine.UI.Wpf.Controls.ViewRegion;
using Microsoft.Xaml.Behaviors;
using SampleCrossUi.Wpf.Views;

namespace SampleCrossUi.Wpf.ViewModels;

/// <summary>
/// Popup 화면의 실제 동작(메시지박스/블링크 팝업/별도 창 열기)을 처리합니다.
/// PopupViewModel은 [DreamineCommand]를 통해 이 클래스의 메서드를 호출만 합니다.
/// </summary>
public sealed class PopupEvent
{
    private readonly IPopupService _popupService;

    public PopupEvent(IPopupService popupService)
    {
        _popupService = popupService ?? throw new ArgumentNullException(nameof(popupService));
    }

    public string LastResult { get; private set; } = "-";

    /// <summary>LastResult가 갱신될 때마다 발생합니다. ViewModel이 구독해서 OnPropertyChanged를 전달합니다.</summary>
    public event EventHandler? Changed;

    private void SetLastResult(string value)
    {
        LastResult = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ShowMessageBox()
    {
        DreamineMessageBox.ShowAsync(
            "DreamineMessageBox 데모입니다.\n버튼을 클릭하거나 기다리면 자동으로 닫힙니다.",
            "MessageBox Demo",
            autoClick: MessageBoxResult.OK,
            autoClickDelaySeconds: 5,
            callback: r => SetLastResult($"MessageBox → {r}"));
    }

    public void ShowBlinkOk()
    {
        _ = ShowBlinkOkAsync();
    }

    public void ShowBlinkAlarm()
    {
        _ = ShowBlinkAlarmAsync();
    }

    /// <summary>
    /// Navigation 서브시스템(ViewLoader + PopupWindowManager) 데모.
    /// 이 화면(PopupView/PopupViewModel)을 타입명 규칙으로 다시 로드해서
    /// 메인 윈도우와 별개인 독립 팝업 창으로 띄운다.
    /// </summary>
    public void OpenAsWindow()
    {
        var info = ViewLoader.LoadViewWithViewModel(
            typeof(PopupView).FullName!,
            useSingletonView: true);

        PopupWindowManager.Instance.CreatePopup(
            "PopupViewWindow",
            info,
            popupWidth: 420,
            popupHeight: 460,
            centerOnScreen: true);

        if (PopupWindowManager.Instance.Windows.TryGetValue("PopupViewWindow", out var popup))
        {
            // PopupWindowManager가 만드는 창은 WindowStyle=None이라 제목줄/닫기 버튼이 없다.
            // 매번 새 Window+View가 만들어지므로, 매 호출마다 직접 타이틀바(드래그+닫기)를 씌워준다.
            WrapWithTitleBar(popup);

            PopupWindowManager.Instance.Show("PopupViewWindow");
            popup.Activate();
            popup.Focus();
        }
        else
        {
            PopupWindowManager.Instance.Show("PopupViewWindow");
        }
    }

    /// <summary>
    /// 팝업 창의 기존 Content(View) 위에 드래그 가능한 타이틀바와 닫기(✕) 버튼을 추가한다.
    /// </summary>
    private static void WrapWithTitleBar(Window popup)
    {
        if (popup.Content is not UIElement originalContent)
            return;

        popup.Content = null;

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var titleBar = new Border { Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x1B, 0x3E)) };
        Interaction.GetBehaviors(titleBar).Add(new WindowDragBehavior());

        var closeButton = new Button
        {
            Content = "✕",
            Width = 32,
            Height = 32,
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = Brushes.White
        };
        closeButton.Click += (_, _) => popup.Close();
        titleBar.Child = closeButton;

        Grid.SetRow(titleBar, 0);
        Grid.SetRow(originalContent, 1);
        root.Children.Add(titleBar);
        root.Children.Add(originalContent);

        popup.Content = root;
    }

    private async System.Threading.Tasks.Task ShowBlinkOkAsync()
    {
        var result = await _popupService.ShowBlinkAsync(
            Application.Current.MainWindow,
            new BlinkPopupOptions
            {
                Title = "작업 완료",
                Message = "작업이 성공적으로 완료되었습니다.",
                UseBlink = false,
                OkText = "확인",
                IsModal = true
            });
        SetLastResult($"BlinkPopup(OK) → {result}");
    }

    private async System.Threading.Tasks.Task ShowBlinkAlarmAsync()
    {
        var result = await _popupService.ShowBlinkAsync(
            Application.Current.MainWindow,
            new BlinkPopupOptions
            {
                Title = "⚠ ALARM",
                Message = "설비 이상이 감지되었습니다.\n운영자 확인이 필요합니다.",
                UseBlink = true,
                BlinkIntervalMs = 400,
                Color1 = Color.FromRgb(180, 30, 30),
                Color2 = Color.FromRgb(80, 10, 10),
                Opacity1 = 1.0,
                Opacity2 = 0.6,
                OkText = "확인",
                CancelText = "취소",
                IsModal = true
            });
        SetLastResult($"BlinkPopup(Alarm) → {result}");
    }
}
