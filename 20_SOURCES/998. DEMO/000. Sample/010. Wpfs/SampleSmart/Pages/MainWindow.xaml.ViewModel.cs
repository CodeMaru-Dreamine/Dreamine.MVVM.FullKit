using Dreamine.Logging.Interfaces;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.ViewModels;
using Dreamine.Threading.Interfaces;
using Dreamine.Threading.Models;

namespace SampleSmart.Pages
{
	/// <summary>
	/// MainWindow에 대한 ViewModel 클래스입니다.
	/// Model과 Event 사이의 바인딩을 담당합니다.
	/// </summary>
	public partial class MainWindowViewModel : ViewModelBase
	{
		[DreamineModel]
		private MainWindowModel _model;
		[DreamineEvent]
		private MainWindowEvent _event;
		public string Title => Model.Title;
		public string Message => Model.Message;
        /// <summary>
        /// \brief OK 동작 실행.
        /// </summary>
        [DreamineCommand("Event.Ok")]
        private partial void Ok();

        /// <summary>
        /// \brief Cancel 동작 실행.
        /// </summary>
        [DreamineCommand("Event.Cancel")]
        private partial void Cancel();

        /// <summary>
        /// \brief Minimize 동작 실행.
        /// </summary>
        [DreamineCommand("Event.Minimize")]
        private partial void Minimize();

        /// <summary>
        /// \brief Maximize 동작 실행.
        /// </summary>
        [DreamineCommand("Event.Maximize")]
        private partial void Maximize();

        /// <summary>
        /// \brief Close 동작 실행.
        /// </summary>
        [DreamineCommand("Event.Close")]
        private partial void Close();

        /// <summary>
        /// \brief SubPage 이동 동작 실행.
        /// </summary>
        [DreamineCommand("Event.SubPage")]
        private partial void SubPage();

        [DreamineCommand("Event.SubPage2")]
        private partial void SubPage2();

        [DreamineCommand("Event.LogPage")]
        private partial void LogPage();

        /// <summary>
        /// \brief Thread Monitor 페이지 이동 동작 실행.
        /// </summary>
        [DreamineCommand("Event.ThreadPage")]
        private partial void ThreadPage();

        /// <summary>
        /// \brief Communication Monitor 페이지 이동 동작 실행.
        /// </summary>
        [DreamineCommand("Event.CommunicationPage")]
        private partial void CommunicationPage();

        /// <summary>
        /// \brief PLC Monitor 페이지 이동 동작 실행.
        /// </summary>
        [DreamineCommand("Event.PlcPage")]
        private partial void PlcPage();

        /// <summary>
        /// \brief I/O Monitor 페이지 이동 동작 실행.
        /// </summary>
        [DreamineCommand("Event.IoPage")]
        private partial void IoPage();

        /// <summary>
        /// \brief SubWindow 오픈 동작 실행.
        /// </summary>
        [DreamineCommand("Event.SubWindow")]
        private partial void SubWindow();

        [DreamineCommand("Event.NoticeWindow")]
        private partial void NoticeWindow();

        [DreamineCommand("Event.MonitorWindow")]
        private partial void MonitorWindow();

        [DreamineCommand("Event.SettingWindow")]
        private partial void SettingWindow();

        private const string SampleMonitoringJobName = "Sample-MonitoringJob";

        private readonly IDreamineLogger _logger;
        private readonly IDreamineThreadManager _threadManager;

        private bool _isInitialized;
        private bool _isSampleMonitoringJobRegistered;
        private int _sampleMonitoringTickCount;

        public MainWindowViewModel(IDreamineLogger logger, IDreamineThreadManager threadManager)
		{
            _model = null!;
            _event = null!;
            _ = _model;
            _ = _event;

            _logger = logger;
            _threadManager = threadManager;

            Initialize();
        }

        /// <summary>
        /// Initializes the ViewModel and registers sample monitoring jobs.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            RegisterSampleMonitoringJob();
        }

        private void RegisterSampleMonitoringJob()
        {
            if (_isSampleMonitoringJobRegistered)
            {
                _logger.Warning($"{SampleMonitoringJobName} is already registered.");
                return;
            }

            _isSampleMonitoringJobRegistered = true;

            _threadManager.Register(
                new DreamineThreadOptions
                {
                    Name = SampleMonitoringJobName,
                    Priority = DreamineThreadPriority.Normal,
                    IntervalMs = 10,
                    CoreMode = DreamineThreadCoreMode.Auto,
                    AutoThreadsPerCore = 2,
                    OverflowPollingIntervalMs = 20,
                    AutoStart = true,
                    UseHighPrecisionTimer = true,
                    YieldWhenIntervalIsZero = true,
                    UseAdaptiveCpuDelay = true
                },
                ExecuteSampleMonitoringJobAsync);

            _logger.Info($"{SampleMonitoringJobName} registered from MainWindowViewModel.");
        }

        private ValueTask ExecuteSampleMonitoringJobAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return ValueTask.CompletedTask;
            }

            _sampleMonitoringTickCount++;

            if (_sampleMonitoringTickCount % 100 == 0)
            {
                _logger.Debug(
                    $"Sample monitoring thread tick. Count={_sampleMonitoringTickCount}");
            }

            return ValueTask.CompletedTask;
        }
    }
}
