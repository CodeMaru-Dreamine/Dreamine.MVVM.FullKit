using Codemaru.Models.Certificates;
using Codemaru.Options;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \if KO
/// <para>\brief nginx 설정 검증 후 reload 명령을 실행합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates nginx reload service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class NginxReloadService : INginxReloadService
{
    /// <summary>
    /// \if KO
    /// <para>process Runner 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the process runner value.</para>
    /// \endif
    /// </summary>
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// \if KO
    /// <para>\brief NginxReloadService 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="NginxReloadService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="processRunner">
    /// \if KO
    /// <para>외부 프로세스 실행 서비스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IProcessRunner</c> value used for process runner.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public NginxReloadService(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <summary>
    /// \if KO
    /// <para>Reload Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reload async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>동작을 구성하는 설정입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Reload Async 작업에서 생성한 <c>Task&lt;ProcessExecutionResult&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ProcessExecutionResult&gt;</c> result produced by the reload async operation.</para>
    /// \endif
    /// </returns>
    public async Task<ProcessExecutionResult> ReloadAsync(CertificateMonitorOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        ProcessExecutionResult testResult = await _processRunner.RunAsync(
            options.NginxPath,
            "-t",
            options.NginxWorkingDirectory,
            TimeSpan.FromSeconds(30),
            options.MaxCommandOutputChars,
            cancellationToken).ConfigureAwait(false);

        if (!testResult.IsSuccess)
        {
            return new ProcessExecutionResult
            {
                IsSuccess = false,
                ExitCode = testResult.ExitCode,
                Output = testResult.Output,
                Error = testResult.Error,
                Message = $"nginx config test failed. {testResult.Message}"
            };
        }

        ProcessExecutionResult reloadResult = await _processRunner.RunAsync(
            options.NginxPath,
            options.NginxReloadArguments,
            options.NginxWorkingDirectory,
            TimeSpan.FromSeconds(30),
            options.MaxCommandOutputChars,
            cancellationToken).ConfigureAwait(false);

        return new ProcessExecutionResult
        {
            IsSuccess = reloadResult.IsSuccess,
            ExitCode = reloadResult.ExitCode,
            Output = JoinSections("[nginx -t]", testResult.Output, "[nginx reload]", reloadResult.Output),
            Error = JoinSections("[nginx -t]", testResult.Error, "[nginx reload]", reloadResult.Error),
            Message = reloadResult.IsSuccess
                ? "nginx config test passed and reload completed successfully."
                : $"nginx config test passed, but reload failed. {reloadResult.Message}"
        };
    }

    /// <summary>
    /// \if KO
    /// <para>Join Sections 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the join sections operation.</para>
    /// \endif
    /// </summary>
    /// <param name="firstTitle">
    /// \if KO
    /// <para>first Title에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for first title.</para>
    /// \endif
    /// </param>
    /// <param name="firstValue">
    /// \if KO
    /// <para>first Value에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for first value.</para>
    /// \endif
    /// </param>
    /// <param name="secondTitle">
    /// \if KO
    /// <para>second Title에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for second title.</para>
    /// \endif
    /// </param>
    /// <param name="secondValue">
    /// \if KO
    /// <para>second Value에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for second value.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Join Sections 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the join sections operation.</para>
    /// \endif
    /// </returns>
    private static string JoinSections(string firstTitle, string firstValue, string secondTitle, string secondValue)
    {
        string first = string.IsNullOrWhiteSpace(firstValue) ? string.Empty : $"{firstTitle}{Environment.NewLine}{firstValue}";
        string second = string.IsNullOrWhiteSpace(secondValue) ? string.Empty : $"{secondTitle}{Environment.NewLine}{secondValue}";
        return string.Join(Environment.NewLine, new[] { first, second }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}
