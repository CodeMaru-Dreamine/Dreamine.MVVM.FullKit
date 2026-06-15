namespace Codemaru.Models.Certificates;

/// <summary>
/// \brief win-acme 자동 갱신 예약 작업 정보입니다.
/// </summary>
public sealed class WinAcmeTaskInfo
{
    /// <summary>\brief 예약 작업 조회 성공 여부입니다.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>\brief 예약 작업 이름입니다.</summary>
    public string TaskName { get; init; } = string.Empty;

    /// <summary>\brief 예약 작업 경로입니다.</summary>
    public string TaskPath { get; init; } = string.Empty;

    /// <summary>\brief 예약 작업 상태입니다.</summary>
    public string State { get; init; } = string.Empty;

    /// <summary>\brief 마지막 실행 시간입니다.</summary>
    public string LastRunTime { get; init; } = string.Empty;

    /// <summary>\brief 다음 실행 시간입니다.</summary>
    public string NextRunTime { get; init; } = string.Empty;

    /// <summary>\brief 마지막 실행 결과입니다.</summary>
    public string LastTaskResult { get; init; } = string.Empty;

    /// <summary>\brief 상태 메시지입니다.</summary>
    public string Message { get; init; } = string.Empty;
}
