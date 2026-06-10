using Dreamine.Logging.Interfaces;
using Dreamine.MVVM.Interfaces.Navigation;
using SampleSmart.Pages.PageSub;
using SampleSmart.Pages.WindowSub;
using System.Windows;

namespace SampleSmart.Pages
{
    /// <summary>
    /// Handles MainWindow events.
    /// </summary>
    public class MainWindowEvent
    {
        private readonly IViewManager _viewManager;
        private readonly IDreamineLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowEvent"/> class.
        /// </summary>
        /// <param name="viewManager">The view manager.</param>
        /// <param name="logger">The logger.</param>
        public MainWindowEvent(IViewManager viewManager, IDreamineLogger logger)
        {
            _viewManager = viewManager;
            _logger = logger;
        }

        public void Ok()
        {
            _logger.Info("OK clicked.");
            MessageBox.Show("확인 클릭됨!");
        }

        public void Cancel()
        {
            _logger.Info("Cancel clicked.");
            MessageBox.Show("취소 클릭됨!");
        }

        public void SubPage()
        {
            _logger.Info("PageSub requested.");
            _viewManager.Show<PageSubViewModel>();
        }

        public void SubPage2()
        {
            _logger.Info("PageSub2 requested.");
            _viewManager.Show<PageSub2ViewModel>();
        }

        public void LogPage()
        {
            _logger.Info("PageLog requested.");
            _viewManager.Show<PageLogViewModel>();
        }

        public void ThreadPage()
        {
            _logger.Info("Thread monitor page requested.");
            _viewManager.Show<PageThreadMonitorViewModel>();
        }        
        public void CommunicationPage()
        {
            _logger.Info("Communication monitor page requested.");
            _viewManager.Show<PageCommunicationMonitorViewModel>();
        }

        public void PlcPage()
        {
            _logger.Info("PLC monitor page requested.");
            _viewManager.Show<PagePlcMonitorViewModel>();
        }

        public void IoPage()
        {
            _logger.Info("I/O monitor page requested.");
            _viewManager.Show<PageIoMonitorViewModel>();
        }

        public void DatabasePage()
        {
            _logger.Info("Database sample page requested.");
            _viewManager.Show<PageDatabaseViewModel>();
        }

        public void SubWindow()
        {
            _logger.Info("WindowSub requested.");
            _viewManager.Show<WindowSubViewModel>();
        }

        public void NoticeWindow()
        {
            _logger.Info("NoticeWindow requested.");
            _viewManager.Show<PopupNoticeViewModel>();
        }

        public void MonitorWindow()
        {
            _logger.Info("MonitorWindow requested.");
            _viewManager.Show<PopupMonitorViewModel>();
        }

        public void SettingWindow()
        {
            _logger.Info("SettingWindow requested.");
            _viewManager.Show<PopupSettingViewModel>();
        }

        public void Minimize()
        {
            _logger.Debug("Minimize requested.");

            var w = GetActiveWindow();
            if (w != null)
            {
                w.WindowState = WindowState.Minimized;
            }
        }

        public void Maximize()
        {
            _logger.Debug("Maximize requested.");

            var w = GetActiveWindow();
            if (w != null)
            {
                w.WindowState = w.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
        }

        public void Close()
        {
            _logger.Info("Close requested.");
            GetActiveWindow()?.Close();
        }

        private Window? GetActiveWindow()
        {
            return Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive);
        }
    }
}
