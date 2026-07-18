using Dreamine.Hybrid.Messaging;

namespace DreamineVMS.Messages;

/// <summary>
/// \if KO
/// <para>\brief Blazor Dashboard에서 WPF Shell로 요청하는 VMS 동작 메시지입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates vms dashboard action requested message functionality and related state.</para>
/// \endif
/// </summary>
public sealed class VmsDashboardActionRequestedMessage : HybridMessageBase
{
    /// <summary>
    /// \if KO
    /// <para>\brief 요청 동작 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the action value.</para>
    /// \endif
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라 단위 동작 대상 ID입니다. 전체 동작이면 null입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the camera id value.</para>
    /// \endif
    /// </summary>
    public string? CameraId { get; init; }
}
