using Dreamine.Logging.Interfaces;
using Dreamine.MVVM.Interfaces.Navigation;
using SampleSmart.Pages.PageSub;
using SampleSmart.Pages.WindowSub;
using System.Windows;

namespace SampleSmart.Pages
{
    /// <summary>
    /// \if KO
    /// <para>Main Window Event 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles MainWindow events.</para>
    /// \endif
    /// </summary>
    public class MainWindowEvent
    {
        /// <summary>
        /// \if KO
        /// <para>view Manager 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the view manager value.</para>
        /// \endif
        /// </summary>
        private readonly IViewManager _viewManager;
        /// <summary>
        /// \if KO
        /// <para>logger 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the logger value.</para>
        /// \endif
        /// </summary>
        private readonly IDreamineLogger _logger;

        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="MainWindowEvent"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="MainWindowEvent"/> class.</para>
        /// \endif
        /// </summary>
        /// <param name="viewManager">
        /// \if KO
        /// <para>view Manager에 사용할 <c>IViewManager</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The view manager.</para>
        /// \endif
        /// </param>
        /// <param name="logger">
        /// \if KO
        /// <para>logger에 사용할 <c>IDreamineLogger</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The logger.</para>
        /// \endif
        /// </param>
        public MainWindowEvent(IViewManager viewManager, IDreamineLogger logger)
        {
            _viewManager = viewManager;
            _logger = logger;
        }

        /// <summary>
        /// \if KO
        /// <para>Ok 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the ok operation.</para>
        /// \endif
        /// </summary>
        public void Ok()
        {
            _logger.Info("OK clicked.");
            MessageBox.Show("확인 클릭됨!");
        }

        /// <summary>
        /// \if KO
        /// <para>Cancel 조건을 확인합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Determines whether cancel.</para>
        /// \endif
        /// </summary>
        public void Cancel()
        {
            _logger.Info("Cancel clicked.");
            MessageBox.Show("취소 클릭됨!");
        }

        /// <summary>
        /// \if KO
        /// <para>Sub Page 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the sub page operation.</para>
        /// \endif
        /// </summary>
        public void SubPage()
        {
            _logger.Info("PageSub requested.");
            _viewManager.Show<PageSubViewModel>();
        }

        /// <summary>
        /// \if KO
        /// <para>Sub Page2 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the sub page2 operation.</para>
        /// \endif
        /// </summary>
        public void SubPage2()
        {
            _logger.Info("PageSub2 requested.");
            _viewManager.Show<PageSub2ViewModel>();
        }

        /// <summary>
        /// \if KO
        /// <para>Log Page 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the log page operation.</para>
        /// \endif
        /// </summary>
        public void LogPage()
        {
            _logger.Info("PageLog requested.");
            _viewManager.Show<PageLogViewModel>();
        }

        /// <summary>
        /// \if KO
        /// <para>Thread Page 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the thread page operation.</para>
        /// \endif
        /// </summary>
        public void ThreadPage()
        {
            _logger.Info("Thread monitor page requested.");
            _viewManager.Show<PageThreadMonitorViewModel>();
        }        
        /// <summary>
        /// \if KO
        /// <para>Communication Page 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the communication page operation.</para>
        /// \endif
        /// </summary>
        public void CommunicationPage()
        {
            _logger.Info("Communication monitor page requested.");
            _viewManager.Show<PageCommunicationMonitorViewModel>();
        }

        /// <summary>
        /// \if KO
        /// <para>Plc Page 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the plc page operation.</para>
        /// \endif
        /// </summary>
        public void PlcPage()
        {
            _logger.Info("PLC monitor page requested.");
            _viewManager.Show<PagePlcMonitorViewModel>();
        }

        /// <summary>
        /// \if KO
        /// <para>Io Page 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the io page operation.</para>
        /// \endif
        /// </summary>
        public void IoPage()
        {
            _logger.Info("I/O monitor page requested.");
            _viewManager.Show<PageIoMonitorViewModel>();
        }

        /// <summary>
        /// \if KO
        /// <para>Database Page 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the database page operation.</para>
        /// \endif
        /// </summary>
        public void DatabasePage()
        {
            _logger.Info("Database sample page requested.");
            _viewManager.Show<PageDatabaseViewModel>();
        }

        /// <summary>
        /// \if KO
        /// <para>Sub Window 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the sub window operation.</para>
        /// \endif
        /// </summary>
        public void SubWindow()
        {
            _logger.Info("WindowSub requested.");
            _viewManager.Show<WindowSubViewModel>();
        }

        /// <summary>
        /// \if KO
        /// <para>Notice Window 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the notice window operation.</para>
        /// \endif
        /// </summary>
        public void NoticeWindow()
        {
            _logger.Info("NoticeWindow requested.");
            _viewManager.Show<PopupNoticeViewModel>();
        }

        /// <summary>
        /// \if KO
        /// <para>Monitor Window 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the monitor window operation.</para>
        /// \endif
        /// </summary>
        public void MonitorWindow()
        {
            _logger.Info("MonitorWindow requested.");
            _viewManager.Show<PopupMonitorViewModel>();
        }

        /// <summary>
        /// \if KO
        /// <para>ting Window 값을 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Sets the ting window value.</para>
        /// \endif
        /// </summary>
        public void SettingWindow()
        {
            _logger.Info("SettingWindow requested.");
            _viewManager.Show<PopupSettingViewModel>();
        }

        /// <summary>
        /// \if KO
        /// <para>Minimize 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the minimize operation.</para>
        /// \endif
        /// </summary>
        public void Minimize()
        {
            _logger.Debug("Minimize requested.");

            var w = GetActiveWindow();
            if (w != null)
            {
                w.WindowState = WindowState.Minimized;
            }
        }

        /// <summary>
        /// \if KO
        /// <para>Maximize 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the maximize operation.</para>
        /// \endif
        /// </summary>
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

        /// <summary>
        /// \if KO
        /// <para>Close 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the close operation.</para>
        /// \endif
        /// </summary>
        public void Close()
        {
            _logger.Info("Close requested.");
            GetActiveWindow()?.Close();
        }

        /// <summary>
        /// \if KO
        /// <para>Active Window 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the active window value.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Get Active Window 작업에서 생성한 <c>Window?</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Window?</c> result produced by the get active window operation.</para>
        /// \endif
        /// </returns>
        private Window? GetActiveWindow()
        {
            return Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive);
        }
    }
}
