using System.Windows;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// 드리마인 페이지 전용 이벤트 클래스입니다.
    /// </summary>
    /// <remarks>
    /// 기본 제공 이벤트(예: 닫기, 최소화, 최대화)는 필요 시 별도 창 제어 이벤트 클래스로 확장할 수 있습니다.
    /// 현재 샘플은 ReadmeClick 예제만 포함합니다.
    /// </remarks>
    public class WindowSubEvent
    {
        /// <summary>
        /// Readme 클릭 예제 동작입니다.
        /// </summary>
        /// <returns>샘플 메시지</returns>
        public string ReadmeClick()
        {
            return "Readme 눌림";
        }
    }
}