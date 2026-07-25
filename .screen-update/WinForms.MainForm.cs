using Dreamine.WinForms.Empty.ViewModels;

namespace Dreamine.WinForms.Empty.Views;

public partial class MainForm : Form
{
    private readonly MainFormViewModel _viewModel;
    private readonly Label _messageLabel;
    private readonly Label _statusLabel;

    public MainForm(MainFormViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();

        _messageLabel = new Label { AutoSize = true, Font = new Font(Font.FontFamily, 20, FontStyle.Bold), Text = _viewModel.Message };
        _statusLabel = new Label { AutoSize = true, MaximumSize = new Size(420, 0), TextAlign = ContentAlignment.MiddleCenter, Text = _viewModel.StatusMessage };
        var productLabel = new Label { AutoSize = true, Font = new Font(Font.FontFamily, 10, FontStyle.Bold), ForeColor = Color.DimGray, Text = "DREAMINE WINFORMS" };
        var okButton = new Button { AutoSize = true, MinimumSize = new Size(110, 36), Text = "확인" };
        var cancelButton = new Button { AutoSize = true, MinimumSize = new Size(110, 36), Text = "취소" };
        okButton.Click += (_, _) => _viewModel.OkCommand.Execute(null);
        cancelButton.Click += (_, _) => _viewModel.CancelCommand.Execute(null);
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainFormViewModel.StatusMessage)) _statusLabel.Text = _viewModel.StatusMessage;
        };

        var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        actions.Controls.Add(okButton);
        actions.Controls.Add(cancelButton);
        var card = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(40), BackColor = Color.White };
        card.Controls.Add(productLabel);
        card.Controls.Add(_messageLabel);
        card.Controls.Add(_statusLabel);
        card.Controls.Add(actions);
        Controls.Add(card);
        card.Location = new Point((ClientSize.Width - card.Width) / 2, (ClientSize.Height - card.Height) / 2);
    }
}
