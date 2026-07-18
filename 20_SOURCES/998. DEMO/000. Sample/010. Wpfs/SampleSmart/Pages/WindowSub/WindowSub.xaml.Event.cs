using System.Windows;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// \if KO
    /// <para>드리마인 페이지 전용 이벤트 클래스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates window sub event functionality and related state.</para>
    /// \endif
    /// </summary>
    /// <remarks>
    /// \if KO
    /// <para>기본 제공 이벤트(예: 닫기, 최소화, 최대화)는 필요 시 별도 창 제어 이벤트 클래스로 확장할 수 있습니다. 현재 샘플은 ReadmeClick 예제만 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Describes behavior and usage considerations for this member.</para>
    /// \endif
    /// </remarks>
    public class WindowSubEvent
    {
        /// <summary>
        /// \if KO
        /// <para>Readme 클릭 예제 동작입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Reads me click data.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>샘플 메시지</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> result produced by the readme click operation.</para>
        /// \endif
        /// </returns>
        public string ReadmeClick()
        {
            return "Readme 눌림";
        }
    }
}