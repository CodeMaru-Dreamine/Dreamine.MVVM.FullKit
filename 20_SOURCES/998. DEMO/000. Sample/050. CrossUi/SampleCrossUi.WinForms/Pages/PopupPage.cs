using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;

namespace SampleCrossUi.WinForms.Pages;

public sealed class PopupPage : UserControl
{
    private readonly Label _resultLabel;

    public PopupPage()
    {
        BackColor = DreamineTheme.AppBackground;
        Dock = DockStyle.Fill;
        Padding = new Padding(24);

        var title = new Label
        {
            Text = "Popup Demo",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold, GraphicsUnit.Point),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 24),
        };

        _resultLabel = new Label
        {
            Text = "Last result: —",
            ForeColor = DreamineTheme.TextSecondary,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point),
            AutoSize = true,
            Margin = new Padding(0, 16, 0, 0),
        };

        // MessageBox demo
        var btnMsgBox = new DreamineButton { Content = "Show MessageBox",   Width = 200, Height = 40, Margin = new Padding(0, 0, 0, 8) };
        var btnInfo   = new DreamineButton { Content = "Show Info Dialog",  Width = 200, Height = 40, Margin = new Padding(0, 0, 0, 8) };
        var btnWarn   = new DreamineButton { Content = "Show Warning",      Width = 200, Height = 40, Margin = new Padding(0, 0, 0, 8) };
        var btnError  = new DreamineButton { Content = "Show Error",        Width = 200, Height = 40 };

        btnMsgBox.Click += (_, _) =>
        {
            var r = MessageBox.Show(
                "DreamineButton으로 트리거된 MessageBox 데모입니다.\n확인 또는 취소를 클릭하세요.",
                "MessageBox Demo",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);
            _resultLabel.Text = $"Last result: MessageBox → {r}";
        };

        btnInfo.Click += (_, _) =>
        {
            MessageBox.Show("작업이 성공적으로 완료되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _resultLabel.Text = "Last result: Info dialog closed";
        };

        btnWarn.Click += (_, _) =>
        {
            var r = MessageBox.Show("주의가 필요한 작업입니다.\n계속 진행할까요?", "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            _resultLabel.Text = $"Last result: Warning → {r}";
        };

        btnError.Click += (_, _) =>
        {
            MessageBox.Show("오류가 발생했습니다.\n운영자에게 문의하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _resultLabel.Text = "Last result: Error dialog closed";
        };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
        };
        layout.Controls.Add(title);
        layout.Controls.Add(btnMsgBox);
        layout.Controls.Add(btnInfo);
        layout.Controls.Add(btnWarn);
        layout.Controls.Add(btnError);
        layout.Controls.Add(_resultLabel);
        Controls.Add(layout);
    }
}
