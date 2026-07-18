using Dreamine.MVVM.Interfaces.Navigation;
using SampleSmart.Pages.WindowSub;
using System.Windows;

namespace SampleSmart.Pages.PageSub
{
    /// <summary>
    /// \if KO
    /// <para>PageSub2 화면 이벤트를 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates page sub2 event functionality and related state.</para>
    /// \endif
    /// </summary>
    public sealed class PageSub2Event
    {
        /// <summary>
        /// \if KO
        /// <para>view Manager 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the view manager value.</para>
        /// \endif
        /// </summary>
        private readonly IViewManager _viewManager;

        /// <summary>
        /// \if KO
        /// <para>PageSub2Event 인스턴스를 생성합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="PageSub2Event"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="viewManager">
        /// \if KO
        /// <para>View 표시 관리자입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IViewManager</c> value used for view manager.</para>
        /// \endif
        /// </param>
        public PageSub2Event(IViewManager viewManager)
        {
            _viewManager = viewManager;
        }

        /// <summary>
        /// \if KO
        /// <para>OK 동작을 실행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the ok operation.</para>
        /// \endif
        /// </summary>
        public void Ok()
        {
            MessageBox.Show("PageSub2 확인");
        }

        /// <summary>
        /// \if KO
        /// <para>Notice 팝업을 엽니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the open notice operation.</para>
        /// \endif
        /// </summary>
        public void OpenNotice()
        {
            _viewManager.Show<PopupNoticeViewModel>();
        }
    }
}