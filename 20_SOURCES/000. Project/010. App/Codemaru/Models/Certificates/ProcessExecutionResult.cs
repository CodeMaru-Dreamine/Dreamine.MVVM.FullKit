namespace Codemaru.Models.Certificates;

/// <summary>
/// \if KO
/// <para>\brief 외부 프로세스 실행 결과입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates process execution result functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ProcessExecutionResult
{
    /// <summary>
    /// \if KO
    /// <para>\brief 실행 성공 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is success value.</para>
    /// \endif
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 프로세스 종료 코드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the exit code value.</para>
    /// \endif
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 표준 출력입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the output value.</para>
    /// \endif
    /// </summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 표준 오류입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the error value.</para>
    /// \endif
    /// </summary>
    public string Error { get; init; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 결과 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the message value.</para>
    /// \endif
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
