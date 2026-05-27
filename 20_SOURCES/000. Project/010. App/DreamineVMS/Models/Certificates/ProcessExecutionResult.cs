namespace DreamineVMS.Models.Certificates;

/// <summary>
/// \brief 외부 프로세스 실행 결과입니다.
/// </summary>
public sealed class ProcessExecutionResult
{
    /// <summary>\brief 실행 성공 여부입니다.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>\brief 프로세스 종료 코드입니다.</summary>
    public int ExitCode { get; init; }

    /// <summary>\brief 표준 출력입니다.</summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>\brief 표준 오류입니다.</summary>
    public string Error { get; init; } = string.Empty;

    /// <summary>\brief 결과 메시지입니다.</summary>
    public string Message { get; init; } = string.Empty;
}
