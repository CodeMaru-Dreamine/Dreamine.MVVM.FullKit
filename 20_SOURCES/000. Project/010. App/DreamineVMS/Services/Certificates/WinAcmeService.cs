using DreamineVMS.Models.Certificates;
using DreamineVMS.Options;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DreamineVMS.Services.Certificates;

/// <summary>
/// \brief win-acme 갱신 작업 상태 조회와 갱신 명령 실행을 담당합니다.
/// </summary>
public sealed class WinAcmeService : IWinAcmeService
{
    private const string PowerShellPath = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// \brief WinAcmeService 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="processRunner">외부 프로세스 실행 서비스입니다.</param>
    public WinAcmeService(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <inheritdoc />
    public async Task<WinAcmeTaskInfo> GetRenewalTaskAsync(CancellationToken cancellationToken)
    {
        string command = """
$task = Get-ScheduledTask -ErrorAction SilentlyContinue |
    Where-Object {
        $_.TaskName -like '*win-acme*' -or
        $_.TaskPath -like '*win-acme*' -or
        $_.Description -like '*win-acme*'
    } |
    Select-Object -First 1 TaskName, TaskPath, State

if ($null -eq $task) {
    return
}

$task | ConvertTo-Json -Compress
""";

        ProcessExecutionResult result = await RunPowerShellAsync(
            command,
            TimeSpan.FromSeconds(20),
            4000,
            cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return new WinAcmeTaskInfo
            {
                IsSuccess = false,
                Message = $"Failed to query scheduled task. {result.Message} {result.Error}".Trim()
            };
        }

        if (string.IsNullOrWhiteSpace(result.Output))
        {
            return new WinAcmeTaskInfo
            {
                IsSuccess = false,
                Message = "win-acme scheduled task was not found."
            };
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(result.Output);
            JsonElement root = document.RootElement;
            string taskName = GetString(root, "TaskName");
            string taskPath = NormalizeTaskPath(GetString(root, "TaskPath"));
            string state = GetString(root, "State");

            return await GetTaskDetailAsync(taskName, taskPath, state, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new WinAcmeTaskInfo
            {
                IsSuccess = false,
                Message = $"Failed to parse scheduled task information. {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public Task<ProcessExecutionResult> RunRenewAsync(CertificateMonitorOptions options, bool force, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        string arguments = force ? "--renew --force" : "--renew";
        return _processRunner.RunAsync(
            options.WacsPath,
            arguments,
            Path.GetDirectoryName(options.WacsPath),
            TimeSpan.FromMinutes(5),
            options.MaxCommandOutputChars,
            cancellationToken);
    }

    private async Task<WinAcmeTaskInfo> GetTaskDetailAsync(
        string taskName,
        string taskPath,
        string state,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            return new WinAcmeTaskInfo
            {
                IsSuccess = false,
                State = state,
                Message = "win-acme scheduled task name is empty."
            };
        }

        string escapedTaskName = EscapePowerShellSingleQuotedString(taskName);
        string escapedTaskPath = EscapePowerShellSingleQuotedString(taskPath);
        string command = $$"""
$t = Get-ScheduledTask -TaskName '{{escapedTaskName}}' -TaskPath '{{escapedTaskPath}}' -ErrorAction SilentlyContinue
if ($null -eq $t) {
    return
}

$i = $t | Get-ScheduledTaskInfo -ErrorAction SilentlyContinue

$lastRun = '-'
$nextRun = '-'
$lastResult = '-'

if ($null -ne $i) {
    if ($i.LastRunTime -ne [DateTime]::MinValue) {
        $lastRun = $i.LastRunTime.ToString('yyyy-MM-dd HH:mm:ss')
    }

    if ($i.NextRunTime -ne [DateTime]::MinValue) {
        $nextRun = $i.NextRunTime.ToString('yyyy-MM-dd HH:mm:ss')
    }

    $lastResult = $i.LastTaskResult.ToString()
}

[PSCustomObject]@{
    TaskName=$t.TaskName
    TaskPath=$t.TaskPath
    State=$t.State.ToString()
    LastRunTime=$lastRun
    NextRunTime=$nextRun
    LastTaskResult=$lastResult
} | ConvertTo-Json -Compress
""";

        ProcessExecutionResult result = await RunPowerShellAsync(
            command,
            TimeSpan.FromSeconds(20),
            4000,
            cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Output))
        {
            return new WinAcmeTaskInfo
            {
                IsSuccess = true,
                TaskName = taskName,
                TaskPath = taskPath,
                State = state,
                Message = "win-acme scheduled task was found, but detailed task info could not be loaded."
            };
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(result.Output);
            JsonElement root = document.RootElement;

            return new WinAcmeTaskInfo
            {
                IsSuccess = true,
                TaskName = GetString(root, "TaskName"),
                TaskPath = NormalizeTaskPath(GetString(root, "TaskPath")),
                State = GetString(root, "State"),
                LastRunTime = GetString(root, "LastRunTime"),
                NextRunTime = GetString(root, "NextRunTime"),
                LastTaskResult = GetString(root, "LastTaskResult"),
                Message = "win-acme scheduled task is registered."
            };
        }
        catch (Exception ex)
        {
            return new WinAcmeTaskInfo
            {
                IsSuccess = false,
                TaskName = taskName,
                TaskPath = taskPath,
                State = state,
                Message = $"Failed to parse task detail. {ex.Message}"
            };
        }
    }

    private Task<ProcessExecutionResult> RunPowerShellAsync(
        string command,
        TimeSpan timeout,
        int maxOutputChars,
        CancellationToken cancellationToken)
    {
        string encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(command));
        return _processRunner.RunAsync(
            PowerShellPath,
            $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}",
            null,
            timeout,
            maxOutputChars,
            cancellationToken);
    }

    private static string EscapePowerShellSingleQuotedString(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private static string NormalizeTaskPath(string taskPath)
    {
        if (string.IsNullOrWhiteSpace(taskPath))
        {
            return @"\";
        }

        return taskPath;
    }

    private static string GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out JsonElement element)
            ? element.ToString()
            : string.Empty;
    }
}
