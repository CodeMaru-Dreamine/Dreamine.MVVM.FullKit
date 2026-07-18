using Codemaru.Models.Certificates;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \if KO
/// <para>\brief 외부 프로세스를 실행하는 서비스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i process runner functionality and related state.</para>
/// \endif
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// \if KO
    /// <para>\brief 외부 프로세스를 실행하고 출력 결과를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="fileName">
    /// \if KO
    /// <para>실행 파일 경로입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <param name="arguments">
    /// \if KO
    /// <para>실행 인자입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for arguments.</para>
    /// \endif
    /// </param>
    /// <param name="workingDirectory">
    /// \if KO
    /// <para>작업 폴더입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for working directory.</para>
    /// \endif
    /// </param>
    /// <param name="timeout">
    /// \if KO
    /// <para>실행 제한 시간입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TimeSpan</c> value used for timeout.</para>
    /// \endif
    /// </param>
    /// <param name="maxOutputChars">
    /// \if KO
    /// <para>출력 최대 길이입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for max output chars.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>프로세스 실행 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ProcessExecutionResult&gt;</c> result produced by the run async operation.</para>
    /// \endif
    /// </returns>
    Task<ProcessExecutionResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory,
        TimeSpan timeout,
        int maxOutputChars,
        CancellationToken cancellationToken);
}
