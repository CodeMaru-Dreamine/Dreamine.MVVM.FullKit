using Dreamine.Hybrid.Messaging;

namespace Sample01.Messages
{
    /// <summary>
    /// Sample message published when the shared counter value changes.
    /// </summary>
    public sealed class CounterChangedMessage : HybridMessageBase
    {
        public CounterChangedMessage(int count)
        {
            Count = count;
        }

        public int Count { get; }
    }
}
