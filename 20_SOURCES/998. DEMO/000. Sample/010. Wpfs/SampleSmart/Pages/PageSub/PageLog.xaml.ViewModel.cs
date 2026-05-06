using Dreamine.Logging.Wpf.ViewModels;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub
{
    /// <summary>
    /// Provides the ViewModel for the PageLog view.
    /// </summary>
    public sealed class PageLogViewModel : ViewModelBase
    {
        /// <summary>
        /// Gets the Dreamine log panel ViewModel.
        /// </summary>
        public DreamineLogPanelViewModel LogPanel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageLogViewModel"/> class.
        /// </summary>
        /// <param name="logPanel">The Dreamine log panel ViewModel.</param>
        public PageLogViewModel(DreamineLogPanelViewModel logPanel)
        {
            LogPanel = logPanel;
        }
    }
}