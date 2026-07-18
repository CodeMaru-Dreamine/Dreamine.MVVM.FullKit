using Dreamine.Hybrid.Messaging;

namespace Sample01.Messages
{
    /// <summary>
    /// \if KO
    /// <para>Dashboard Action Requested Message 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sample message requesting a dashboard action from the WPF shell.</para>
    /// \endif
    /// </summary>
    public sealed class DashboardActionRequestedMessage : HybridMessageBase
    {
        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="DashboardActionRequestedMessage"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="DashboardActionRequestedMessage"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="action">
        /// \if KO
        /// <para>action에 사용할 <c>DashboardAction</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>DashboardAction</c> value used for action.</para>
        /// \endif
        /// </param>
        public DashboardActionRequestedMessage(DashboardAction action)
        {
            Action = action;
        }

        /// <summary>
        /// \if KO
        /// <para>Action 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the action value.</para>
        /// \endif
        /// </summary>
        public DashboardAction Action { get; }
    }
}
