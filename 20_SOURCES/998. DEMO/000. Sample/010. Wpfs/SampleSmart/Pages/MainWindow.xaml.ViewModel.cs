using Dreamine.Logging.Interfaces;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.ViewModels;
using Dreamine.Threading.Interfaces;
using Dreamine.Threading.Models;

namespace SampleSmart.Pages
{
	/// <summary>
	/// \if KO
	/// <para>MainWindow에 대한 ViewModel 클래스입니다. Model과 Event 사이의 바인딩을 담당합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Encapsulates main window view model functionality and related state.</para>
	/// \endif
	/// </summary>
	public partial class MainWindowViewModel : ViewModelBase
	{
		/// <summary>
		/// \if KO
		/// <para>model 값을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the model value.</para>
		/// \endif
		/// </summary>
		[DreamineModel]
		private MainWindowModel _model;
		/// <summary>
		/// \if KO
		/// <para>event 값을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the event value.</para>
		/// \endif
		/// </summary>
		[DreamineEvent]
		private MainWindowEvent _event;
		/// <summary>
		/// \if KO
		/// <para>Title 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the title value.</para>
		/// \endif
		/// </summary>
		public string Title => Model.Title;
		/// <summary>
		/// \if KO
		/// <para>Message 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the message value.</para>
		/// \endif
		/// </summary>
		public string Message => Model.Message;
        /// <summary>
        /// \if KO
        /// <para>\brief OK 동작 실행.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the ok operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.Ok")]
        private partial void Ok();

        /// <summary>
        /// \if KO
        /// <para>\brief Cancel 동작 실행.</para>
        /// \endif
        /// \if EN
        /// <para>Determines whether cancel.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.Cancel")]
        private partial void Cancel();

        /// <summary>
        /// \if KO
        /// <para>\brief Minimize 동작 실행.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the minimize operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.Minimize")]
        private partial void Minimize();

        /// <summary>
        /// \if KO
        /// <para>\brief Maximize 동작 실행.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the maximize operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.Maximize")]
        private partial void Maximize();

        /// <summary>
        /// \if KO
        /// <para>\brief Close 동작 실행.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the close operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.Close")]
        private partial void Close();

        /// <summary>
        /// \if KO
        /// <para>\brief SubPage 이동 동작 실행.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the sub page operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.SubPage")]
        private partial void SubPage();

        /// <summary>
        /// \if KO
        /// <para>Sub Page2 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the sub page2 operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.SubPage2")]
        private partial void SubPage2();

        /// <summary>
        /// \if KO
        /// <para>Log Page 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the log page operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.LogPage")]
        private partial void LogPage();

        /// <summary>
        /// \if KO
        /// <para>\brief Thread Monitor 페이지 이동 동작 실행.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the thread page operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.ThreadPage")]
        private partial void ThreadPage();

        /// <summary>
        /// \if KO
        /// <para>\brief Communication Monitor 페이지 이동 동작 실행.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the communication page operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.CommunicationPage")]
        private partial void CommunicationPage();

        /// <summary>
        /// \if KO
        /// <para>\brief PLC Monitor 페이지 이동 동작 실행.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the plc page operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.PlcPage")]
        private partial void PlcPage();

        /// <summary>
        /// \if KO
        /// <para>\brief I/O Monitor 페이지 이동 동작 실행.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the io page operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.IoPage")]
        private partial void IoPage();

        /// <summary>
        /// \if KO
        /// <para>\brief Database 샘플 페이지 이동 동작 실행.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the database page operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.DatabasePage")]
        private partial void DatabasePage();

        /// <summary>
        /// \if KO
        /// <para>\brief SubWindow 오픈 동작 실행.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the sub window operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.SubWindow")]
        private partial void SubWindow();

        /// <summary>
        /// \if KO
        /// <para>Notice Window 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the notice window operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.NoticeWindow")]
        private partial void NoticeWindow();

        /// <summary>
        /// \if KO
        /// <para>Monitor Window 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the monitor window operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.MonitorWindow")]
        private partial void MonitorWindow();

        /// <summary>
        /// \if KO
        /// <para>ting Window 값을 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Sets the ting window value.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.SettingWindow")]
        private partial void SettingWindow();

        /// <summary>
        /// \if KO
        /// <para>Sample Monitoring Job Name 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the sample monitoring job name value.</para>
        /// \endif
        /// </summary>
        private const string SampleMonitoringJobName = "Sample-MonitoringJob";

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
        /// <para>thread Manager 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the thread manager value.</para>
        /// \endif
        /// </summary>
        private readonly IDreamineThreadManager _threadManager;

        /// <summary>
        /// \if KO
        /// <para>is Initialized 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the is initialized value.</para>
        /// \endif
        /// </summary>
        private bool _isInitialized;
        /// <summary>
        /// \if KO
        /// <para>is Sample Monitoring Job Registered 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the is sample monitoring job registered value.</para>
        /// \endif
        /// </summary>
        private bool _isSampleMonitoringJobRegistered;
        /// <summary>
        /// \if KO
        /// <para>sample Monitoring Tick Count 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the sample monitoring tick count value.</para>
        /// \endif
        /// </summary>
        private int _sampleMonitoringTickCount;

        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="MainWindowViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="MainWindowViewModel"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="logger">
        /// \if KO
        /// <para>logger에 사용할 <c>IDreamineLogger</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IDreamineLogger</c> value used for logger.</para>
        /// \endif
        /// </param>
        /// <param name="threadManager">
        /// \if KO
        /// <para>thread Manager에 사용할 <c>IDreamineThreadManager</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IDreamineThreadManager</c> value used for thread manager.</para>
        /// \endif
        /// </param>
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
        /// \if KO
        /// <para>Initialize 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes the ViewModel and registers sample monitoring jobs.</para>
        /// \endif
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

        /// <summary>
        /// \if KO
        /// <para>Register Sample Monitoring Job 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the register sample monitoring job operation.</para>
        /// \endif
        /// </summary>
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

        /// <summary>
        /// \if KO
        /// <para>Execute Sample Monitoring Job Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the execute sample monitoring job async operation.</para>
        /// \endif
        /// </summary>
        /// <param name="token">
        /// \if KO
        /// <para>token에 사용할 <c>CancellationToken</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>CancellationToken</c> value used for token.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Execute Sample Monitoring Job Async 작업에서 생성한 <c>ValueTask</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>ValueTask</c> result produced by the execute sample monitoring job async operation.</para>
        /// \endif
        /// </returns>
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
