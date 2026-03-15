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
		///// <summary>
		///// 이벤트 바인딩 예시입니다. 사용 시 주석 해제 후 활용 가능합니다.
		///// </summary>
		///// <remarks>
		///// [RelayCommand] 속성과 함께 사용하면 버튼 클릭 등과 연계 가능합니다.
		///// </remarks>
		//[DreamineEvent]
		//private MainWindowEvent _event;

		///// <summary>
		///// 확인 버튼 클릭 시 동작할 커맨드입니다.
		///// </summary>
		//[RelayCommand]
		//private void Ok() => Event.Ok();


		[DreamineEvent]
		private WindowSubEvent _event;


        [DreamineCommand("Event.ReadmeCleck", BindTo = nameof(Readme))]
        private partial void ReadmeCleck();

        //[RelayCommand]		
        //private void ReadmeCleck()
        //{
        //	string text = Event.ReadmeCleck();

        //	//Model.Readme = text;
        //	Readme = text;
        //}

        private readonly PageSubViewModel _viewModel;

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
