using Dreamine.Hybrid.Messaging;

namespace Sample01.Messages
{
    /// <summary>
    /// \if KO
    /// <para>Counter Changed Message 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sample message published when the shared counter value changes.</para>
    /// \endif
    /// </summary>
    public sealed class CounterChangedMessage : HybridMessageBase
    {
        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="CounterChangedMessage"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="CounterChangedMessage"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="count">
        /// \if KO
        /// <para>count에 사용할 <c>int</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>int</c> value used for count.</para>
        /// \endif
        /// </param>
        public CounterChangedMessage(int count)
        {
            Count = count;
        }

        /// <summary>
        /// \if KO
        /// <para>Count 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the count value.</para>
        /// \endif
        /// </summary>
        public int Count { get; }
    }
}
