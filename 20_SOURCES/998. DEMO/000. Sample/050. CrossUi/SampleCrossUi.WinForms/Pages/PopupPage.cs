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
        _title = new Label();
        _resultLabel = new Label();
        _btnMsgBox = new DreamineButton();
        _btnInfo = new DreamineButton();
        _btnWarn = new DreamineButton();
        _btnError = new DreamineButton();
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
        _resultLabel.TabIndex = 5;
        _resultLabel.Text = "Last result: —";
        // 
        // _btnMsgBox
        // 
        _btnMsgBox.BackColor = Color.FromArgb(13, 27, 62);
        _btnMsgBox.BorderColor = Color.FromArgb(45, 74, 110);
        _btnMsgBox.Command = null;
        _btnMsgBox.CommandParameter = null;
        _btnMsgBox.Content = "Show MessageBox";
        _btnMsgBox.CornerRadius = 6;
        _btnMsgBox.Font = new Font("Segoe UI", 10F);
        _btnMsgBox.ForeColor = Color.White;
        _btnMsgBox.IsSelected = false;
        _btnMsgBox.Location = new Point(0, 56);
        _btnMsgBox.Margin = new Padding(0, 0, 0, 8);
        _btnMsgBox.Name = "_btnMsgBox";
        _btnMsgBox.ShineColor = Color.Empty;
        _btnMsgBox.Size = new Size(200, 40);
        _btnMsgBox.TabIndex = 1;
        // 
        // _btnInfo
        // 
        _btnInfo.BackColor = Color.FromArgb(13, 27, 62);
        _btnInfo.BorderColor = Color.FromArgb(45, 74, 110);
        _btnInfo.Command = null;
        _btnInfo.CommandParameter = null;
        _btnInfo.Content = "Show Info Dialog";
        _btnInfo.CornerRadius = 6;
        _btnInfo.Font = new Font("Segoe UI", 10F);
        _btnInfo.ForeColor = Color.White;
        _btnInfo.IsSelected = false;
        _btnInfo.Location = new Point(0, 104);
        _btnInfo.Margin = new Padding(0, 0, 0, 8);
        _btnInfo.Name = "_btnInfo";
        _btnInfo.ShineColor = Color.Empty;
        _btnInfo.Size = new Size(200, 40);
        _btnInfo.TabIndex = 2;
        // 
        // _btnWarn
        // 
        _btnWarn.BackColor = Color.FromArgb(13, 27, 62);
        _btnWarn.BorderColor = Color.FromArgb(45, 74, 110);
        _btnWarn.Command = null;
        _btnWarn.CommandParameter = null;
        _btnWarn.Content = "Show Warning";
        _btnWarn.CornerRadius = 6;
        _btnWarn.Font = new Font("Segoe UI", 10F);
        _btnWarn.ForeColor = Color.White;
        _btnWarn.IsSelected = false;
        _btnWarn.Location = new Point(0, 152);
        _btnWarn.Margin = new Padding(0, 0, 0, 8);
        _btnWarn.Name = "_btnWarn";
        _btnWarn.ShineColor = Color.Empty;
        _btnWarn.Size = new Size(200, 40);
        _btnWarn.TabIndex = 3;
        // 
        // _btnError
        // 
        _btnError.BackColor = Color.FromArgb(13, 27, 62);
        _btnError.BorderColor = Color.FromArgb(45, 74, 110);
        _btnError.Command = null;
        _btnError.CommandParameter = null;
        _btnError.Content = "Show Error";
        _btnError.CornerRadius = 6;
        _btnError.Font = new Font("Segoe UI", 10F);
        _btnError.ForeColor = Color.White;
        _btnError.IsSelected = false;
        _btnError.Location = new Point(0, 200);
        _btnError.Margin = new Padding(0, 0, 0, 8);
        _btnError.Name = "_btnError";
        _btnError.ShineColor = Color.Empty;
        _btnError.Size = new Size(200, 40);
        _btnError.TabIndex = 4;
        // 
        // _layout
        // 
        _layout.AutoSize = true;
        _layout.Controls.Add(_title);
        _layout.Controls.Add(_btnMsgBox);
        _layout.Controls.Add(_btnInfo);
        _layout.Controls.Add(_btnWarn);
        _layout.Controls.Add(_btnError);
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
