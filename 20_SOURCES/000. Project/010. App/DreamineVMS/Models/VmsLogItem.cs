namespace DreamineVMS.Models;

/// <summary>
/// \if KO
/// <para>\brief VMS 로그 항목입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates vms log item functionality and related state.</para>
/// \endif
/// </summary>
public sealed class VmsLogItem
{
    /// <summary>
    /// \if KO
    /// <para>\brief 로그 시각입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the time value.</para>
    /// \endif
    /// </summary>
    public DateTimeOffset Time { get; init; } = DateTimeOffset.Now;

    /// <summary>
    /// \if KO
    /// <para>\brief 로그 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the message value.</para>
    /// \endif
    /// </summary>
    public required string Message { get; init; }
}
