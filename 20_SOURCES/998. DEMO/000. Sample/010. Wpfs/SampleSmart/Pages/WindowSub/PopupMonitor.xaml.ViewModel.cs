using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Interfaces.Windows;
using Dreamine.MVVM.ViewModels;
using SampleSmart.Constants;
using System.Text;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// \if KO
    /// <para>PopupMonitor ViewModel입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates popup monitor view model functionality and related state.</para>
    /// \endif
    /// </summary>
    public partial class PopupMonitorViewModel : ViewModelBase
    {
        /// <summary>
        /// \if KO
        /// <para>window State Service 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the window state service value.</para>
        /// \endif
        /// </summary>
        private readonly IWindowStateService _windowStateService;

        /// <summary>
        /// \if KO
        /// <para>model 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the model value.</para>
        /// \endif
        /// </summary>
        [DreamineModel]
        private PopupMonitorModel _model;

        /// <summary>
        /// \if KO
        /// <para>event 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the event value.</para>
        /// \endif
        /// </summary>
        [DreamineEvent]
        private PopupMonitorEvent _event;

        /// <summary>
        /// \if KO
        /// <para>창 상태 요약 문자열입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the window state summary value.</para>
        /// \endif
        /// </summary>
        [DreamineProperty]
        private string _windowStateSummary = string.Empty;

        /// <summary>
        /// \if KO
        /// <para>PopupMonitorViewModel 인스턴스를 생성합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="PopupMonitorViewModel"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="windowStateService">
        /// \if KO
        /// <para>창 상태 서비스입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IWindowStateService</c> value used for window state service.</para>
        /// \endif
        /// </param>
        public PopupMonitorViewModel(IWindowStateService windowStateService)
        {
            _model = null!;
            _event = null!;

            _windowStateService = windowStateService;
            _windowStateService.StateChanged += OnWindowStateChanged;

            RefreshSummary();
        }

        /// <summary>
        /// \if KO
        /// <para>상태 새로고침 동작을 실행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the refresh operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.Refresh")]
        private partial void Refresh();

        /// <summary>
        /// \if KO
        /// <para>Window State Changed 이벤트 또는 상태 변경을 처리합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Handles the window state changed event or state change.</para>
        /// \endif
        /// </summary>
        /// <param name="sender">
        /// \if KO
        /// <para>이벤트를 발생시킨 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The object that raised the event.</para>
        /// \endif
        /// </param>
        /// <param name="e">
        /// \if KO
        /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Contains data associated with the event.</para>
        /// \endif
        /// </param>
        private void OnWindowStateChanged(object? sender, WindowStateChangedEventArgs e)
        {
            RefreshSummary();
        }

        /// <summary>
        /// \if KO
        /// <para>Refresh Summary 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the refresh summary operation.</para>
        /// \endif
        /// </summary>
        private void RefreshSummary()
        {
            StringBuilder builder = new();

            AppendState(builder, ViewKeys.WindowSub);
            AppendState(builder, ViewKeys.PopupNotice);
            AppendState(builder, ViewKeys.PopupMonitor);
            AppendState(builder, ViewKeys.PopupSetting);

            WindowStateSummary = builder.ToString();
        }

        /// <summary>
        /// \if KO
        /// <para>Append State 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the append state operation.</para>
        /// \endif
        /// </summary>
        /// <param name="builder">
        /// \if KO
        /// <para>builder에 사용할 <c>StringBuilder</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>StringBuilder</c> value used for builder.</para>
        /// \endif
        /// </param>
        /// <param name="windowKey">
        /// \if KO
        /// <para>window Key에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for window key.</para>
        /// \endif
        /// </param>
        private void AppendState(StringBuilder builder, string windowKey)
        {
            string state = _windowStateService.IsOpen(windowKey)
                ? "OPEN"
                : "CLOSED";

            builder.AppendLine($"{windowKey} : {state}");
        }
    }
}
