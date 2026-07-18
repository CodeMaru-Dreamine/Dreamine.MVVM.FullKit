namespace Codemaru.Models.Certificates;

/// <summary>
/// \if KO
/// <para>\brief win-acme 자동 갱신 예약 작업 정보입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates win acme task info functionality and related state.</para>
/// \endif
/// </summary>
public sealed class WinAcmeTaskInfo
{
    /// <summary>
    /// \if KO
    /// <para>\brief 예약 작업 조회 성공 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is success value.</para>
    /// \endif
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 예약 작업 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the task name value.</para>
    /// \endif
    /// </summary>
    public string TaskName { get; init; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 예약 작업 경로입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the task path value.</para>
    /// \endif
    /// </summary>
    public string TaskPath { get; init; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 예약 작업 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the state value.</para>
    /// \endif
    /// </summary>
    public string State { get; init; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 마지막 실행 시간입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the last run time value.</para>
    /// \endif
    /// </summary>
    public string LastRunTime { get; init; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 다음 실행 시간입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the next run time value.</para>
    /// \endif
    /// </summary>
    public string NextRunTime { get; init; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 마지막 실행 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the last task result value.</para>
    /// \endif
    /// </summary>
    public string LastTaskResult { get; init; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 상태 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the message value.</para>
    /// \endif
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
