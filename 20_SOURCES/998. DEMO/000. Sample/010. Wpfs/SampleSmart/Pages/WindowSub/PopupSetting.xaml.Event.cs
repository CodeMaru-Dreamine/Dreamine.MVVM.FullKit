using System.Windows;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// \if KO
    /// <para>PopupSetting 이벤트를 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates popup setting event functionality and related state.</para>
    /// \endif
    /// </summary>
    public sealed class PopupSettingEvent
    {
        /// <summary>
        /// \if KO
        /// <para>설정 적용 동작을 실행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the apply operation.</para>
        /// \endif
        /// </summary>
        public void Apply()
        {
            MessageBox.Show("설정 확인");
        }
    }
}
