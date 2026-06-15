using Codemaru.Models.Certificates;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \brief 외부 프로세스를 실행하는 서비스입니다.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// \brief 외부 프로세스를 실행하고 출력 결과를 반환합니다.
    /// </summary>
    /// <param name="fileName">실행 파일 경로입니다.</param>
    /// <param name="arguments">실행 인자입니다.</param>
    /// <param name="workingDirectory">작업 폴더입니다.</param>
    /// <param name="timeout">실행 제한 시간입니다.</param>
    /// <param name="maxOutputChars">출력 최대 길이입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>프로세스 실행 결과입니다.</returns>
    Task<ProcessExecutionResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory,
        TimeSpan timeout,
        int maxOutputChars,
        CancellationToken cancellationToken);
}
