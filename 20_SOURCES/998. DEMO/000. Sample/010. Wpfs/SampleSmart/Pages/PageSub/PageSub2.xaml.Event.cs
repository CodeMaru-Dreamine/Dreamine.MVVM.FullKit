using Dreamine.MVVM.Interfaces.Navigation;
using SampleSmart.Pages.WindowSub;
using System.Windows;

namespace SampleSmart.Pages.PageSub
{
    /// <summary>
    /// PageSub2 화면 이벤트를 처리합니다.
    /// </summary>
    public sealed class PageSub2Event
    {
        private readonly IViewManager _viewManager;

        /// <summary>
        /// PageSub2Event 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="viewManager">View 표시 관리자입니다.</param>
        public PageSub2Event(IViewManager viewManager)
        {
            _viewManager = viewManager;
        }

        /// <summary>
        /// OK 동작을 실행합니다.
        /// </summary>
        public void Ok()
        {
            MessageBox.Show("PageSub2 확인");
        }

        /// <summary>
        /// Notice 팝업을 엽니다.
        /// </summary>
        public void OpenNotice()
        {
            _viewManager.Show<PopupNoticeViewModel>();
        }
    }
}