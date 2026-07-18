using Dreamine.UI.Maui.Popup;

namespace SampleCrossUi.Maui.Views;

/// <summary>
/// \if KO
/// <para>Popup Page 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates popup page functionality and related state.</para>
/// \endif
/// </summary>
public partial class PopupPage : ContentView
{
    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="PopupPage"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PopupPage"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public PopupPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// \if KO
    /// <para>Show Message Box Clicked 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the show message box clicked event or state change.</para>
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
    private async void OnShowMessageBoxClicked(object? sender, EventArgs e)
    {
        var result = await DreamineMessageBox.ShowAsync(
            "DreamineMessageBox 데모입니다.\n버튼을 클릭하거나 기다리면 자동으로 닫힙니다.",
            "MessageBox Demo",
            autoClickDelaySeconds: 5);
        ResultLabel.Text = $"Last result: MessageBox → {result}";
    }

    /// <summary>
    /// \if KO
    /// <para>Show Blink Ok Clicked 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the show blink ok clicked event or state change.</para>
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

    /// <summary>
    /// \if KO
    /// <para>Show Blink Alarm Clicked 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the show blink alarm clicked event or state change.</para>
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
