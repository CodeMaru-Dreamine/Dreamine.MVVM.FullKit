using Codemaru.Models.Certificates;
using Codemaru.Options;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \if KO
/// <para>\brief Caddy 설정을 검증한 뒤 관리 API를 통해 graceful reload를 실행합니다.</para>
/// \endif
/// \if EN
/// <para>Validates a Caddy configuration and then performs a graceful reload through the admin API.</para>
/// \endif
/// </summary>
public sealed class CaddyReloadService : ICaddyReloadService
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(30);
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// \if KO
    /// <para>\brief <see cref="CaddyReloadService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="CaddyReloadService"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="processRunner">외부 프로세스 실행 서비스입니다.</param>
    public CaddyReloadService(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <inheritdoc />
    public async Task<ProcessExecutionResult> ReloadAsync(
        CertificateMonitorOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        ProcessExecutionResult? optionsError = ValidateOptions(options);
        if (optionsError is not null)
        {
            return optionsError;
        }

        IReadOnlyList<string> validateArguments = BuildArguments(
            "validate",
            options,
            includeReloadOptions: false);

        ProcessExecutionResult validateResult = await _processRunner.RunAsync(
            options.CaddyPath,
            validateArguments,
            options.CaddyWorkingDirectory,
            CommandTimeout,
            options.MaxCommandOutputChars,
            cancellationToken).ConfigureAwait(false);

        if (!validateResult.IsSuccess)
        {
            return new ProcessExecutionResult
            {
                IsSuccess = false,
                ExitCode = validateResult.ExitCode,
                Output = validateResult.Output,
                Error = validateResult.Error,
                Message = $"Caddy config validation failed. {validateResult.Message}"
            };
        }

        IReadOnlyList<string> reloadArguments = BuildArguments(
            "reload",
            options,
            includeReloadOptions: true);

        ProcessExecutionResult reloadResult = await _processRunner.RunAsync(
            options.CaddyPath,
            reloadArguments,
            options.CaddyWorkingDirectory,
            CommandTimeout,
            options.MaxCommandOutputChars,
            cancellationToken).ConfigureAwait(false);

        return new ProcessExecutionResult
        {
            IsSuccess = reloadResult.IsSuccess,
            ExitCode = reloadResult.ExitCode,
            Output = JoinSections(
                "[caddy validate]",
                validateResult.Output,
                "[caddy reload]",
                reloadResult.Output),
            Error = JoinSections(
                "[caddy validate]",
                validateResult.Error,
                "[caddy reload]",
                reloadResult.Error),
            Message = reloadResult.IsSuccess
                ? "Caddy config validation passed and graceful reload completed successfully."
                : $"Caddy config validation passed, but graceful reload failed. {reloadResult.Message}"
        };
    }

    private static ProcessExecutionResult? ValidateOptions(CertificateMonitorOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CaddyPath))
        {
            return InvalidOptions("Caddy executable path is empty.");
        }

        if (string.IsNullOrWhiteSpace(options.CaddyConfigPath))
        {
            return InvalidOptions("Caddy configuration path is empty.");
        }

        return null;
    }

    private static ProcessExecutionResult InvalidOptions(string message)
    {
        return new ProcessExecutionResult
        {
            IsSuccess = false,
            ExitCode = -1,
            Message = message
        };
    }

    private static IReadOnlyList<string> BuildArguments(
        string command,
        CertificateMonitorOptions options,
        bool includeReloadOptions)
    {
        List<string> arguments =
        [
            command,
            "--config",
            options.CaddyConfigPath
        ];

        if (!string.IsNullOrWhiteSpace(options.CaddyConfigAdapter))
        {
            arguments.Add("--adapter");
            arguments.Add(options.CaddyConfigAdapter);
        }

        if (includeReloadOptions && !string.IsNullOrWhiteSpace(options.CaddyAdminAddress))
        {
            arguments.Add("--address");
            arguments.Add(options.CaddyAdminAddress);
        }

        if (includeReloadOptions && options.CaddyForceReload)
        {
            arguments.Add("--force");
        }

        return arguments;
    }

    private static string JoinSections(
        string firstTitle,
        string firstValue,
        string secondTitle,
        string secondValue)
    {
        string first = string.IsNullOrWhiteSpace(firstValue)
            ? string.Empty
            : $"{firstTitle}{Environment.NewLine}{firstValue}";
        string second = string.IsNullOrWhiteSpace(secondValue)
            ? string.Empty
            : $"{secondTitle}{Environment.NewLine}{secondValue}";

        return string.Join(
            Environment.NewLine,
            new[] { first, second }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
