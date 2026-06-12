using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;

namespace SampleCrossUi.WinForms.Pages;

public sealed class PopupPage : UserControl
{
    private Label         _title       = null!;
    private Label         _resultLabel = null!;
    private DreamineButton _btnMsgBox  = null!;
    private DreamineButton _btnInfo    = null!;
    private DreamineButton _btnWarn    = null!;
    private DreamineButton _btnError   = null!;
    private FlowLayoutPanel _layout    = null!;

    public PopupPage()
    {
        InitializeComponent();

        _btnMsgBox.Click += (_, _) =>
        {
            var r = MessageBox.Show(
                "DreamineButton으로 트리거된 MessageBox 데모입니다.\n확인 또는 취소를 클릭하세요.",
                "MessageBox Demo",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);
            _resultLabel.Text = $"Last result: MessageBox → {r}";
        };

        _btnInfo.Click += (_, _) =>
        {
            MessageBox.Show("작업이 성공적으로 완료되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _resultLabel.Text = "Last result: Info dialog closed";
        };

        _btnWarn.Click += (_, _) =>
        {
            var r = MessageBox.Show("주의가 필요한 작업입니다.\n계속 진행할까요?", "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            _resultLabel.Text = $"Last result: Warning → {r}";
        };

        _btnError.Click += (_, _) =>
        {
            MessageBox.Show("오류가 발생했습니다.\n운영자에게 문의하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _resultLabel.Text = "Last result: Error dialog closed";
        };
    }

    private void InitializeComponent()
    {
        _title       = new Label();
        _resultLabel = new Label();
        _btnMsgBox   = new DreamineButton();
        _btnInfo     = new DreamineButton();
        _btnWarn     = new DreamineButton();
        _btnError    = new DreamineButton();
        _layout      = new FlowLayoutPanel();

        SuspendLayout();

        // _title
        _title.Text      = "Popup Demo";
        _title.ForeColor = Color.White;
        _title.Font      = new Font("Segoe UI", 18f, FontStyle.Bold, GraphicsUnit.Point);
        _title.AutoSize  = true;
        _title.Margin    = new Padding(0, 0, 0, 24);

        // _resultLabel
        _resultLabel.Text      = "Last result: —";
        _resultLabel.ForeColor = DreamineTheme.TextSecondary;
        _resultLabel.Font      = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
        _resultLabel.AutoSize  = true;
        _resultLabel.Margin    = new Padding(0, 16, 0, 0);

        // buttons
        _btnMsgBox.Content = "Show MessageBox";
        _btnInfo.Content   = "Show Info Dialog";
        _btnWarn.Content   = "Show Warning";
        _btnError.Content  = "Show Error";

        foreach (var btn in new[] { _btnMsgBox, _btnInfo, _btnWarn, _btnError })
        {
            btn.Width  = 200;
            btn.Height = 40;
            btn.Margin = new Padding(0, 0, 0, 8);
        }

        // _layout
        _layout.Dock          = DockStyle.Fill;
        _layout.FlowDirection = FlowDirection.TopDown;
        _layout.AutoSize      = true;
        _layout.WrapContents  = false;
        _layout.Controls.Add(_title);
        _layout.Controls.Add(_btnMsgBox);
        _layout.Controls.Add(_btnInfo);
        _layout.Controls.Add(_btnWarn);
        _layout.Controls.Add(_btnError);
        _layout.Controls.Add(_resultLabel);

        // UserControl
        BackColor = DreamineTheme.AppBackground;
        Dock      = DockStyle.Fill;
        Padding   = new Padding(24);
        Name      = "PopupPage";
        Controls.Add(_layout);

        ResumeLayout(false);
    }
}
