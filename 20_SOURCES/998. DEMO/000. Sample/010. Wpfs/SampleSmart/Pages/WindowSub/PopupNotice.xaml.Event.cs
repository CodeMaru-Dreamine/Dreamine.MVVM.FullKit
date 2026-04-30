using System.Windows;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// PopupNotice 이벤트를 처리합니다.
    /// </summary>
    public sealed class PopupNoticeEvent
    {
        /// <summary>
        /// OK 동작을 실행합니다.
        /// </summary>
        public void Ok()
        {
            MessageBox.Show("Notice 확인");
        }
    }
}
