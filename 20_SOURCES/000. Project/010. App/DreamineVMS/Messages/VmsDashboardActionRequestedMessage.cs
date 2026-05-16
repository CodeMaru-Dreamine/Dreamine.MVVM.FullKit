using Dreamine.Hybrid.Messaging;

namespace DreamineVMS.Messages;

/// <summary>
/// \brief Blazor Dashboard에서 WPF Shell로 요청하는 VMS 동작 메시지입니다.
/// </summary>
public sealed class VmsDashboardActionRequestedMessage : HybridMessageBase
{
    /// <summary>
    /// \brief 요청 동작 이름입니다.
    /// </summary>
    public required string Action { get; init; }
}
