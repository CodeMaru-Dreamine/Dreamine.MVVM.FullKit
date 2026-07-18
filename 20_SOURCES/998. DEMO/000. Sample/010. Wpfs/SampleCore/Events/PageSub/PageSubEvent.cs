using System.Windows;

namespace SampleCore.Events.PageSub
{
    /// <summary>
    /// \if KO
    /// <para>Page Sub Event 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates page sub event functionality and related state.</para>
    /// \endif
    /// </summary>
    public class PageSubEvent
    {
        /// <summary>
        /// \if KO
        /// <para>Ok 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the ok operation.</para>
        /// \endif
        /// </summary>
        public void Ok() => MessageBox.Show("확인 클릭됨!");
        /// <summary>
        /// \if KO
        /// <para>Cancel 조건을 확인합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Determines whether cancel.</para>
        /// \endif
        /// </summary>
        public void Cancel() => MessageBox.Show("취소 클릭됨!");
    }
}
