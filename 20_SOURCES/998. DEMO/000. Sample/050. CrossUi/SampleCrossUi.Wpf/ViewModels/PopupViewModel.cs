using System.Windows;
using System.Windows.Media;
using Dreamine.MVVM.ViewModels;
using Dreamine.UI.Abstractions.Popup;
using Dreamine.UI.Wpf.Controls.MessageBox;

namespace SampleCrossUi.Wpf.ViewModels;

public class PopupViewModel : ViewModelBase
{
    private readonly IPopupService _popupService;

    private string _lastResult = "-";
    public string LastResult { get => _lastResult; set => SetProperty(ref _lastResult, value); }

    public RelayCommand ShowMessageBoxCommand { get; }
    public AsyncRelayCommand ShowBlinkOkCommand { get; }
    public AsyncRelayCommand ShowBlinkAlarmCommand { get; }

    public PopupViewModel(IPopupService popupService)
    {
        _popupService = popupService;

        ShowMessageBoxCommand = new RelayCommand(() =>
        {
            DreamineMessageBox.ShowAsync(
                "DreamineMessageBox 데모입니다.\n버튼을 클릭하거나 기다리면 자동으로 닫힙니다.",
                "MessageBox Demo",
                autoClick: MessageBoxResult.OK,
                autoClickDelaySeconds: 5,
                callback: r => LastResult = $"MessageBox → {r}");
        });

        ShowBlinkOkCommand = new AsyncRelayCommand(async () =>
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
            LastResult = $"BlinkPopup(OK) → {result}";
        });

        ShowBlinkAlarmCommand = new AsyncRelayCommand(async () =>
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
            LastResult = $"BlinkPopup(Alarm) → {result}";
        });
    }
}
