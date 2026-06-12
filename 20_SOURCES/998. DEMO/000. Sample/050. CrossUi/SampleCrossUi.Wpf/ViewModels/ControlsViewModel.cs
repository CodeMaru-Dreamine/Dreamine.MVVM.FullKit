using Dreamine.MVVM.ViewModels;
using Dreamine.UI.Wpf.Controls;

namespace SampleCrossUi.Wpf.ViewModels;

public class ControlsViewModel : ViewModelBase
{
    // CheckBox / CheckLed state
    private bool _isChecked = true;
    public bool IsChecked { get => _isChecked; set => SetProperty(ref _isChecked, value); }

    // LedCorner selection
    private LedCorner _ledCorner = LedCorner.TopRight;
    public LedCorner LedCorner { get => _ledCorner; set => SetProperty(ref _ledCorner, value); }

    // TextBox value
    private string _inputText = string.Empty;
    public string InputText { get => _inputText; set => SetProperty(ref _inputText, value); }

    // Numeric input
    private decimal _numericValue = 42m;
    public decimal NumericValue { get => _numericValue; set => SetProperty(ref _numericValue, value); }

    // Expander open/close
    private bool _isExpanded = true;
    public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }

    // Status message
    private string _statusMessage = "Ready";
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public RelayCommand ToggleCheckCommand { get; }
    public RelayCommand ClearTextCommand { get; }

    public ControlsViewModel()
    {
        ToggleCheckCommand = new RelayCommand(() =>
        {
            IsChecked = !IsChecked;
            StatusMessage = IsChecked ? "Checked ON" : "Checked OFF";
        });

        ClearTextCommand = new RelayCommand(() =>
        {
            InputText = string.Empty;
            NumericValue = 0m;
            StatusMessage = "Cleared";
        });
    }
}
