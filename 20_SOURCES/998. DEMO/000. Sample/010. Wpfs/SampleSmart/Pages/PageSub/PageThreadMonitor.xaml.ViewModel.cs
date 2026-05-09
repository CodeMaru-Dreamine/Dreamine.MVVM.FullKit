using Dreamine.MVVM.ViewModels;
using Dreamine.Threading.Wpf.ViewModels;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// Provides the ViewModel for the sample thread monitor page.
/// </summary>
public sealed class PageThreadMonitorViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the Dreamine thread monitor ViewModel.
    /// </summary>
    public DreamineThreadMonitorViewModel ThreadMonitor { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PageThreadMonitorViewModel"/> class.
    /// </summary>
    /// <param name="threadMonitor">The Dreamine thread monitor ViewModel.</param>
    public PageThreadMonitorViewModel(DreamineThreadMonitorViewModel threadMonitor)
    {
        ThreadMonitor = threadMonitor;
    }
}