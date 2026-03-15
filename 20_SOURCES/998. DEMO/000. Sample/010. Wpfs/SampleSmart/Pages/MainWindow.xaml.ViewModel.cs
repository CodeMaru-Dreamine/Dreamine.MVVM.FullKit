using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Core;

namespace SampleSmart.Pages
{
	/// <summary>
	/// MainWindow에 대한 ViewModel 클래스입니다.
	/// Model과 Event 사이의 바인딩을 담당합니다.
	/// </summary>
	public partial class MainWindowViewModel
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

        /// <summary>
        /// \brief SubWindow 오픈 동작 실행.
        /// </summary>
        [DreamineCommand("Event.SubWindow")]
        private partial void SubWindow();

		public MainWindowViewModel()
		{
            _model = null!;
            _event = null!;
            _ = _model;
            _ = _event;
        }
	}
}
