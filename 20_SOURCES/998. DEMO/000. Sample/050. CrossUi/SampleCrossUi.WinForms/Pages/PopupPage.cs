using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;
using Dreamine.UI.WinForms.MessageBox;
using Dreamine.UI.WinForms.Popup;

namespace SampleCrossUi.WinForms.Pages;

/// <summary>
/// \if KO
/// <para>Popup Page 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates popup page functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PopupPage : UserControl
{
    /// <summary>
    /// \if KO
    /// <para>title 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the title value.</para>
    /// \endif
    /// </summary>
    private Label         _title       = null!;
    /// <summary>
    /// \if KO
    /// <para>result Label 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the result label value.</para>
    /// \endif
    /// </summary>
    private Label         _resultLabel = null!;
    /// <summary>
    /// \if KO
    /// <para>btn Msg Box 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the btn msg box value.</para>
    /// \endif
    /// </summary>
    private DreamineButton _btnMsgBox  = null!;
    /// <summary>
    /// \if KO
    /// <para>btn Blink Ok 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the btn blink ok value.</para>
    /// \endif
    /// </summary>
    private DreamineButton _btnBlinkOk = null!;
    /// <summary>
    /// \if KO
    /// <para>btn Blink Alarm 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the btn blink alarm value.</para>
    /// \endif
    /// </summary>
    private DreamineButton _btnBlinkAlarm = null!;
    /// <summary>
    /// \if KO
    /// <para>layout 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the layout value.</para>
    /// \endif
    /// </summary>
    private FlowLayoutPanel _layout    = null!;

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

        // DreamineMessageBox — WPF DreamineMessageBox(5초 후 자동 OK)와 동일한 동작.
        _btnMsgBox.Click += (_, _) =>
        {
            DreamineMessageBox.ShowAsync(
                "DreamineMessageBox 데모입니다.\n버튼을 클릭하거나 기다리면 자동으로 닫힙니다.",
                "MessageBox Demo",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information,
                autoClick: DialogResult.OK,
                autoClickDelaySeconds: 5,
                callback: r => _resultLabel.Text = $"Last result: MessageBox → {r}");
        };

        // DreamineBlinkPopup(완료 알림) — WPF ShowBlinkAsync(UseBlink=false)와 동일한 동작.
        _btnBlinkOk.Click += async (_, _) =>
        {
            var result = await DreamineBlinkPopup.ShowAsync(FindForm(), new BlinkPopupOptions
            {
                Title = "작업 완료",
                Message = "작업이 성공적으로 완료되었습니다.",
                UseBlink = false,
                Color1 = Color.FromArgb(13, 27, 62),
                Color2 = Color.FromArgb(13, 27, 62),
                ForegroundColor = Color.White,
                OkText = "확인",
                IsModal = true
            });
            _resultLabel.Text = $"Last result: BlinkPopup(OK) → {result}";
        };

        // DreamineBlinkPopup(설비 ALARM, 점멸) — WPF ShowBlinkAsync(UseBlink=true)와 동일한 동작.
        _btnBlinkAlarm.Click += async (_, _) =>
        {
            var result = await DreamineBlinkPopup.ShowAsync(FindForm(), new BlinkPopupOptions
            {
                Title = "⚠ ALARM",
                Message = "설비 이상이 감지되었습니다.\n운영자 확인이 필요합니다.",
                UseBlink = true,
                BlinkIntervalMs = 400,
                Color1 = Color.FromArgb(180, 30, 30),
                Color2 = Color.FromArgb(80, 10, 10),
                ForegroundColor = Color.Yellow,
                OkText = "확인",
                CancelText = "취소",
                IsModal = true
            });
            _resultLabel.Text = $"Last result: BlinkPopup(Alarm) → {result}";
        };
    }

    /// <summary>
    /// \if KO
    /// <para>Initialize Component 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the initialize component operation.</para>
    /// \endif
    /// </summary>
    private void InitializeComponent()
    {
        _title = new Label();
        _resultLabel = new Label();
        _btnMsgBox = new DreamineButton();
        _btnBlinkOk = new DreamineButton();
        _btnBlinkAlarm = new DreamineButton();
        _layout = new FlowLayoutPanel();
        _layout.SuspendLayout();
        SuspendLayout();
        //
        // _title
        //
        _title.AutoSize = true;
        _title.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        _title.ForeColor = Color.White;
        _title.Location = new Point(0, 0);
        _title.Margin = new Padding(0, 0, 0, 24);
        _title.Name = "_title";
        _title.Size = new Size(163, 32);
        _title.TabIndex = 0;
        _title.Text = "Popup Demo";
        //
        // _resultLabel
        //
        _resultLabel.AutoSize = true;
        _resultLabel.Font = new Font("Segoe UI", 10F);
        _resultLabel.ForeColor = Color.FromArgb(136, 153, 170);
        _resultLabel.Location = new Point(0, 264);
        _resultLabel.Margin = new Padding(0, 16, 0, 0);
        _resultLabel.Name = "_resultLabel";
        _resultLabel.Size = new Size(93, 19);
        _resultLabel.TabIndex = 4;
        _resultLabel.Text = "Last result: —";
        //
        // _btnMsgBox
        //
        _btnMsgBox.BackColor = Color.FromArgb(13, 27, 62);
        _btnMsgBox.BorderColor = Color.FromArgb(45, 74, 110);
        _btnMsgBox.Command = null;
        _btnMsgBox.CommandParameter = null;
        _btnMsgBox.Content = "DreamineMessageBox (5s auto-close)";
        _btnMsgBox.CornerRadius = 6;
        _btnMsgBox.Font = new Font("Segoe UI", 10F);
        _btnMsgBox.ForeColor = Color.White;
        _btnMsgBox.IsSelected = false;
        _btnMsgBox.Location = new Point(0, 56);
        _btnMsgBox.Margin = new Padding(0, 0, 0, 8);
        _btnMsgBox.Name = "_btnMsgBox";
        _btnMsgBox.ShineColor = Color.Empty;
        _btnMsgBox.Size = new Size(260, 40);
        _btnMsgBox.TabIndex = 1;
        //
        // _btnBlinkOk
        //
        _btnBlinkOk.BackColor = Color.FromArgb(13, 27, 62);
        _btnBlinkOk.BorderColor = Color.FromArgb(45, 74, 110);
        _btnBlinkOk.Command = null;
        _btnBlinkOk.CommandParameter = null;
        _btnBlinkOk.Content = "BlinkPopup — 완료 알림";
        _btnBlinkOk.CornerRadius = 6;
        _btnBlinkOk.Font = new Font("Segoe UI", 10F);
        _btnBlinkOk.ForeColor = Color.White;
        _btnBlinkOk.IsSelected = false;
        _btnBlinkOk.Location = new Point(0, 104);
        _btnBlinkOk.Margin = new Padding(0, 0, 0, 8);
        _btnBlinkOk.Name = "_btnBlinkOk";
        _btnBlinkOk.ShineColor = Color.Empty;
        _btnBlinkOk.Size = new Size(260, 40);
        _btnBlinkOk.TabIndex = 2;
        //
        // _btnBlinkAlarm
        //
        _btnBlinkAlarm.BackColor = Color.FromArgb(13, 27, 62);
        _btnBlinkAlarm.BorderColor = Color.FromArgb(45, 74, 110);
        _btnBlinkAlarm.Command = null;
        _btnBlinkAlarm.CommandParameter = null;
        _btnBlinkAlarm.Content = "BlinkPopup — 설비 ALARM (점멸)";
        _btnBlinkAlarm.CornerRadius = 6;
        _btnBlinkAlarm.Font = new Font("Segoe UI", 10F);
        _btnBlinkAlarm.ForeColor = Color.White;
        _btnBlinkAlarm.IsSelected = false;
        _btnBlinkAlarm.Location = new Point(0, 152);
        _btnBlinkAlarm.Margin = new Padding(0, 0, 0, 8);
        _btnBlinkAlarm.Name = "_btnBlinkAlarm";
        _btnBlinkAlarm.ShineColor = Color.Empty;
        _btnBlinkAlarm.Size = new Size(260, 40);
        _btnBlinkAlarm.TabIndex = 3;
        //
        // _layout
        //
        _layout.AutoSize = true;
        _layout.Controls.Add(_title);
        _layout.Controls.Add(_btnMsgBox);
        _layout.Controls.Add(_btnBlinkOk);
        _layout.Controls.Add(_btnBlinkAlarm);
        _layout.Controls.Add(_resultLabel);
        _layout.Dock = DockStyle.Fill;
        _layout.FlowDirection = FlowDirection.TopDown;
        _layout.Location = new Point(24, 24);
        _layout.Name = "_layout";
        _layout.Size = new Size(1133, 1225);
        _layout.TabIndex = 0;
        _layout.WrapContents = false;
        //
        // PopupPage
        //
        BackColor = Color.FromArgb(26, 26, 46);
        Controls.Add(_layout);
        Name = "PopupPage";
        Padding = new Padding(24);
        Size = new Size(1181, 1273);
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
