using Codemaru.Models.Certificates;
using Codemaru.Options;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \if KO
/// <para>\brief win-acme 갱신 작업 상태 조회와 갱신 명령 실행을 담당합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates win acme service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class WinAcmeService : IWinAcmeService
{
    /// <summary>
    /// \if KO
    /// <para>Power Shell Path 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the power shell path value.</para>
    /// \endif
    /// </summary>
    private const string PowerShellPath = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";
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
    /// <para>\brief WinAcmeService 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="WinAcmeService"/> class with the specified settings.</para>
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
    public WinAcmeService(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <summary>
    /// \if KO
    /// <para>Renewal Task Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the renewal task async value.</para>
    /// \endif
    /// </summary>
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
    /// <para>Get Renewal Task Async 작업에서 생성한 <c>Task&lt;WinAcmeTaskInfo&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;WinAcmeTaskInfo&gt;</c> result produced by the get renewal task async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Run Renew Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run renew async operation.</para>
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
    /// <param name="force">
    /// \if KO
    /// <para>force에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for force.</para>
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
    /// <para>Run Renew Async 작업에서 생성한 <c>Task&lt;ProcessExecutionResult&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ProcessExecutionResult&gt;</c> result produced by the run renew async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Task Detail Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the task detail async value.</para>
    /// \endif
    /// </summary>
    /// <param name="taskName">
    /// \if KO
    /// <para>task Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for task name.</para>
    /// \endif
    /// </param>
    /// <param name="taskPath">
    /// \if KO
    /// <para>task Path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for task path.</para>
    /// \endif
    /// </param>
    /// <param name="state">
    /// \if KO
    /// <para>state에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for state.</para>
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
    /// <para>Get Task Detail Async 작업에서 생성한 <c>Task&lt;WinAcmeTaskInfo&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;WinAcmeTaskInfo&gt;</c> result produced by the get task detail async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Run Power Shell Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run power shell async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="command">
    /// \if KO
    /// <para>command에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for command.</para>
    /// \endif
    /// </param>
    /// <param name="timeout">
    /// \if KO
    /// <para>timeout에 사용할 <c>TimeSpan</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TimeSpan</c> value used for timeout.</para>
    /// \endif
    /// </param>
    /// <param name="maxOutputChars">
    /// \if KO
    /// <para>max Output Chars에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for max output chars.</para>
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
    /// <para>Run Power Shell Async 작업에서 생성한 <c>Task&lt;ProcessExecutionResult&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ProcessExecutionResult&gt;</c> result produced by the run power shell async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Escape Power Shell Single Quoted String 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the escape power shell single quoted string operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Escape Power Shell Single Quoted String 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the escape power shell single quoted string operation.</para>
    /// \endif
    /// </returns>
    private static string EscapePowerShellSingleQuotedString(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Task Path 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize task path operation.</para>
    /// \endif
    /// </summary>
    /// <param name="taskPath">
    /// \if KO
    /// <para>task Path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for task path.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Task Path 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize task path operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeTaskPath(string taskPath)
    {
        if (string.IsNullOrWhiteSpace(taskPath))
        {
            return @"\";
        }

        return taskPath;
    }

    /// <summary>
    /// \if KO
    /// <para>String 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the string value.</para>
    /// \endif
    /// </summary>
    /// <param name="root">
    /// \if KO
    /// <para>root에 사용할 <c>JsonElement</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>JsonElement</c> value used for root.</para>
    /// \endif
    /// </param>
    /// <param name="propertyName">
    /// \if KO
    /// <para>property Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for property name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get String 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get string operation.</para>
    /// \endif
    /// </returns>
    private static string GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out JsonElement element)
            ? element.ToString()
            : string.Empty;
    }
}
