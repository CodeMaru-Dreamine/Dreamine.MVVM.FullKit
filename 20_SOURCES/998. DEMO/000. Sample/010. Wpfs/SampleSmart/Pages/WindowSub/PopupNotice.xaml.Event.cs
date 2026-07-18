using System.Windows;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// \if KO
    /// <para>PopupNotice 이벤트를 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates popup notice event functionality and related state.</para>
    /// \endif
    /// </summary>
    public sealed class PopupNoticeEvent
    {
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
            MessageBox.Show("Notice 확인");
        }
    }
}
