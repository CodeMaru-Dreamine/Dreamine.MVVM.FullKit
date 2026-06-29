using Dreamine.UI.Maui.Popup;

namespace SampleCrossUi.Maui.Views;

public partial class PopupPage : ContentView
{
    public PopupPage()
    {
        InitializeComponent();
    }

    private async void OnShowMessageBoxClicked(object? sender, EventArgs e)
    {
        var result = await DreamineMessageBox.ShowAsync(
            "DreamineMessageBox 데모입니다.\n버튼을 클릭하거나 기다리면 자동으로 닫힙니다.",
            "MessageBox Demo",
            autoClickDelaySeconds: 5);
        ResultLabel.Text = $"Last result: MessageBox → {result}";
    }

    private async void OnShowBlinkOkClicked(object? sender, EventArgs e)
    {
        var result = await DreamineBlinkPopup.ShowAsync(new BlinkPopupOptions
        {
            Title = "작업 완료",
            Message = "작업이 성공적으로 완료되었습니다.",
            UseBlink = false,
            Color1 = Color.FromArgb("#0D1B3E"),
            Color2 = Color.FromArgb("#0D1B3E"),
            ForegroundColor = Colors.White,
            OkText = "확인"
        });
        ResultLabel.Text = $"Last result: BlinkPopup(OK) → {result}";
    }

    private async void OnShowBlinkAlarmClicked(object? sender, EventArgs e)
    {
        var result = await DreamineBlinkPopup.ShowAsync(new BlinkPopupOptions
        {
            Title = "⚠ ALARM",
            Message = "설비 이상이 감지되었습니다.\n운영자 확인이 필요합니다.",
            UseBlink = true,
            BlinkIntervalMs = 400,
            Color1 = Color.FromArgb("#B41E1E"),
            Color2 = Color.FromArgb("#500A0A"),
            ForegroundColor = Colors.Yellow,
            OkText = "확인",
            CancelText = "취소"
        });
        ResultLabel.Text = $"Last result: BlinkPopup(Alarm) → {result}";
    }
}
