using Codemaru.Models.Certificates;
using Codemaru.Options;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \brief nginx 설정 검증 후 reload 명령을 실행합니다.
/// </summary>
public sealed class NginxReloadService : INginxReloadService
{
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// \brief NginxReloadService 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="processRunner">외부 프로세스 실행 서비스입니다.</param>
    public NginxReloadService(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <inheritdoc />
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

    private static string JoinSections(string firstTitle, string firstValue, string secondTitle, string secondValue)
    {
        string first = string.IsNullOrWhiteSpace(firstValue) ? string.Empty : $"{firstTitle}{Environment.NewLine}{firstValue}";
        string second = string.IsNullOrWhiteSpace(secondValue) ? string.Empty : $"{secondTitle}{Environment.NewLine}{secondValue}";
        return string.Join(Environment.NewLine, new[] { first, second }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}
