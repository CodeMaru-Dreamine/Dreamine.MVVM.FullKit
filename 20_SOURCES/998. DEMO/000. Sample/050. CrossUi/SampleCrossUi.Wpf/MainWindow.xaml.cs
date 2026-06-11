using System.Windows;
using Dreamine.MVVM.Core;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.Wpf;

/// <summary>
/// WPF main window — binds to <see cref="CounterViewModel"/> resolved from DMContainer.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = DMContainer.Resolve<CounterViewModel>();
    }
}
