using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Interfaces.Windows;
using Dreamine.MVVM.ViewModels;
using SampleSmart.Constants;

namespace SampleSmart.Pages.PageSub
{
    /// <summary>
    /// \if KO
    /// <para>PageSub 화면의 ViewModel입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates page sub view model functionality and related state.</para>
    /// \endif
    /// </summary>
    public partial class PageSubViewModel : ViewModelBase
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
        private PageSubModel _model;

        /// <summary>
        /// \if KO
        /// <para>event 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the event value.</para>
        /// \endif
        /// </summary>
        [DreamineEvent]
        private PageSubEvent _event;

        /// <summary>
        /// \if KO
        /// <para>화면에 표시할 메시지입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the message value.</para>
        /// \endif
        /// </summary>
        [DreamineProperty]
        private string _message = string.Empty;

        /// <summary>
        /// \if KO
        /// <para>WindowSub 창이 열려 있는지 여부입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the is window sub open value.</para>
        /// \endif
        /// </summary>
        [DreamineProperty]
        private bool _isWindowSubOpen;

        /// <summary>
        /// \if KO
        /// <para>PageSubViewModel 인스턴스를 생성합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="PageSubViewModel"/> class with the specified settings.</para>
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
        public PageSubViewModel(IWindowStateService windowStateService)
        {
            _model = null!;
            _event = null!;

            _windowStateService = windowStateService;

            Message = Model.Message;

            IsWindowSubOpen = _windowStateService.IsOpen(ViewKeys.WindowSub);
            ApplyWindowSubMessage(IsWindowSubOpen);

            _windowStateService.StateChanged += OnWindowStateChanged;
        }

        /// <summary>
        /// \if KO
        /// <para>OK 동작 실행.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the ok operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.Ok")]
        private partial void Ok();

        /// <summary>
        /// \if KO
        /// <para>Cancel 동작 실행.</para>
        /// \endif
        /// \if EN
        /// <para>Determines whether cancel.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.Cancel")]
        private partial void Cancel();

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
            if (e.WindowKey != ViewKeys.WindowSub)
            {
                return;
            }

            IsWindowSubOpen = e.IsOpen;
            ApplyWindowSubMessage(e.IsOpen);
        }

        /// <summary>
        /// \if KO
        /// <para>Apply Window Sub Message 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the apply window sub message operation.</para>
        /// \endif
        /// </summary>
        /// <param name="isOpen">
        /// \if KO
        /// <para>is Open에 사용할 <c>bool</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>bool</c> value used for is open.</para>
        /// \endif
        /// </param>
        private void ApplyWindowSubMessage(bool isOpen)
        {
            Message = isOpen
                ? "WindowSub 창이 열려 있습니다."
                : "WindowSub 창이 닫혔습니다.";
        }
    }
}