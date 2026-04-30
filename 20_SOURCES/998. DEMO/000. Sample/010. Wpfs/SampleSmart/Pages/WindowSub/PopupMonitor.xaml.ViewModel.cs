using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Interfaces.Windows;
using Dreamine.MVVM.ViewModels;
using SampleSmart.Constants;
using System.Text;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// PopupMonitor ViewModel입니다.
    /// </summary>
    public partial class PopupMonitorViewModel : ViewModelBase
    {
        private readonly IWindowStateService _windowStateService;

        [DreamineModel]
        private PopupMonitorModel _model;

        [DreamineEvent]
        private PopupMonitorEvent _event;

        /// <summary>
        /// 창 상태 요약 문자열입니다.
        /// </summary>
        [DreamineProperty]
        private string _windowStateSummary = string.Empty;

        /// <summary>
        /// PopupMonitorViewModel 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="windowStateService">창 상태 서비스입니다.</param>
        public PopupMonitorViewModel(IWindowStateService windowStateService)
        {
            _model = null!;
            _event = null!;

            _windowStateService = windowStateService;
            _windowStateService.StateChanged += OnWindowStateChanged;

            RefreshSummary();
        }

        /// <summary>
        /// 상태 새로고침 동작을 실행합니다.
        /// </summary>
        [DreamineCommand("Event.Refresh")]
        private partial void Refresh();

        private void OnWindowStateChanged(object? sender, WindowStateChangedEventArgs e)
        {
            RefreshSummary();
        }

        private void RefreshSummary()
        {
            StringBuilder builder = new();

            AppendState(builder, ViewKeys.WindowSub);
            AppendState(builder, ViewKeys.PopupNotice);
            AppendState(builder, ViewKeys.PopupMonitor);
            AppendState(builder, ViewKeys.PopupSetting);

            WindowStateSummary = builder.ToString();
        }

        private void AppendState(StringBuilder builder, string windowKey)
        {
            string state = _windowStateService.IsOpen(windowKey)
                ? "OPEN"
                : "CLOSED";

            builder.AppendLine($"{windowKey} : {state}");
        }
    }
}
