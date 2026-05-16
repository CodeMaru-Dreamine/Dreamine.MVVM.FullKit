namespace DreamineVMS.Models;

/// <summary>
/// \brief VMS 로그 항목입니다.
/// </summary>
public sealed class VmsLogItem
{
    /// <summary>\brief 로그 시각입니다.</summary>
    public DateTimeOffset Time { get; init; } = DateTimeOffset.Now;

    /// <summary>\brief 로그 메시지입니다.</summary>
    public required string Message { get; init; }
}
