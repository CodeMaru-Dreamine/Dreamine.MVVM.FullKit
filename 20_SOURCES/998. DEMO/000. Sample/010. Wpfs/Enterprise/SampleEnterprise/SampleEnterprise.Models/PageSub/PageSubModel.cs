using Dreamine.MVVM.Attributes;

namespace SampleEnterprise.Models.PageSub
{
    /// <summary>
    /// \if KO
    /// <para>Page Sub Model 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates page sub model functionality and related state.</para>
    /// \endif
    /// </summary>
    public partial class PageSubModel
    {
        /// <summary>
        /// \if KO
        /// <para>Message 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the message value.</para>
        /// \endif
        /// </summary>
        public string Message { get; set; } = "이것은 Sub View Sample 입니다.";
    }
}
