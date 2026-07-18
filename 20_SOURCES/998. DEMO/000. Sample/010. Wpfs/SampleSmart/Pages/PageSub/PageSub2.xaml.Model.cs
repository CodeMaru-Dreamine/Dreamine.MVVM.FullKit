namespace SampleSmart.Pages.PageSub
{
    /// <summary>
    /// \if KO
    /// <para>PageSub2 화면 모델입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates page sub2 model functionality and related state.</para>
    /// \endif
    /// </summary>
    public sealed class PageSub2Model
    {
        /// <summary>
        /// \if KO
        /// <para>화면 메시지를 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the message value.</para>
        /// \endif
        /// </summary>
        public string Message { get; } = "두 번째 SubPage입니다. Region 전환 테스트용 화면입니다.";
    }
}