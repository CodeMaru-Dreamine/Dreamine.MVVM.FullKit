using Dreamine.Hybrid.Messaging;

namespace Sample01.Messages
{
    /// <summary>
    /// Sample message requesting a dashboard action from the WPF shell.
    /// </summary>
    public sealed class DashboardActionRequestedMessage : HybridMessageBase
    {
        public DashboardActionRequestedMessage(DashboardAction action)
        {
            Action = action;
        }

        public DashboardAction Action { get; }
    }
}
