using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.ViewModels;
using SampleSmart.Pages.PageSub;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// WindowSub에 대한 ViewModel 클래스입니다.
    /// Model과 Event 사이의 바인딩을 담당합니다.
    /// </summary>
    public partial class WindowSubViewModel : ViewModelBase
    {
        [DreamineModel]
        private WindowSubModel _model;

        [DreamineProperty]
        private string _readme = string.Empty;

        [DreamineEvent]
        private WindowSubEvent _event;

        /// <summary>
        /// ReadmeClick 동작 예제입니다.
        /// 기존 RelayCommand 방식에서는 Event.ReadmeClick() 호출 후 결과를 Readme에 반영하는 형태로 구현할 수 있습니다.
        /// 현재 샘플은 동일 동작을 DreamineCommand 선언 한 줄로 대체하는 방식을 보여줍니다.
        /// </summary>
        [DreamineCommand("Event.ReadmeClick", BindTo = nameof(Readme))]
        private partial void ReadmeClick();

        private PageSubViewModel _viewModel;

        public WindowSubViewModel(PageSubViewModel viewModel)
        {
            _model = null!;
            _event = null!;
            _viewModel = viewModel;

            _ = _model;
            _ = _event;

            Readme = Model.Readme;
            _viewModel.Message = "WindowSub 창 활성화 감지";
        }
    }
}