using System.Windows;
using Dreamine.MVVM.Core;
using SampleCrossUi.Wpf.ViewModels;

namespace SampleCrossUi.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = DMContainer.Resolve<MainViewModel>();
    }
}
