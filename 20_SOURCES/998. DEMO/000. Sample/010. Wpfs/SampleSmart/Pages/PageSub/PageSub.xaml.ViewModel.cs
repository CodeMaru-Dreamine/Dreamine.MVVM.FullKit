using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Interfaces.Windows;
using Dreamine.MVVM.ViewModels;
using SampleSmart.Constants;

namespace SampleSmart.Pages.PageSub
{
    /// <summary>
    /// PageSub 화면의 ViewModel입니다.
    /// </summary>
    public partial class PageSubViewModel : ViewModelBase
    {
        private readonly IWindowStateService _windowStateService;

        [DreamineModel]
        private PageSubModel _model;

        [DreamineEvent]
        private PageSubEvent _event;

        /// <summary>
        /// 화면에 표시할 메시지입니다.
        /// </summary>
        [DreamineProperty]
        private string _message = string.Empty;

        /// <summary>
        /// WindowSub 창이 열려 있는지 여부입니다.
        /// </summary>
        [DreamineProperty]
        private bool _isWindowSubOpen;

        /// <summary>
        /// PageSubViewModel 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="windowStateService">창 상태 서비스입니다.</param>
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
        /// OK 동작 실행.
        /// </summary>
        [DreamineCommand("Event.Ok")]
        private partial void Ok();

        /// <summary>
        /// Cancel 동작 실행.
        /// </summary>
        [DreamineCommand("Event.Cancel")]
        private partial void Cancel();

        private void OnWindowStateChanged(object? sender, WindowStateChangedEventArgs e)
        {
            if (e.WindowKey != ViewKeys.WindowSub)
            {
                return;
            }

            IsWindowSubOpen = e.IsOpen;
            ApplyWindowSubMessage(e.IsOpen);
        }

        private void ApplyWindowSubMessage(bool isOpen)
        {
            Message = isOpen
                ? "WindowSub 창이 열려 있습니다."
                : "WindowSub 창이 닫혔습니다.";
        }
    }
}