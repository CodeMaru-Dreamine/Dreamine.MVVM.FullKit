using System.Windows;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// PopupSetting 이벤트를 처리합니다.
    /// </summary>
    public sealed class PopupSettingEvent
    {
        /// <summary>
        /// 설정 적용 동작을 실행합니다.
        /// </summary>
        public void Apply()
        {
            MessageBox.Show("설정 확인");
        }
    }
}
