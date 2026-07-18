using Codemaru.Models.Certificates;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \if KO
/// <para>\brief 외부 프로세스를 실행하고 표준 출력/오류를 수집합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates process runner functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    /// <summary>
    /// \if KO
    /// <para>Run Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <param name="arguments">
    /// \if KO
    /// <para>arguments에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for arguments.</para>
    /// \endif
    /// </param>
    /// <param name="workingDirectory">
    /// \if KO
    /// <para>working Directory에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for working directory.</para>
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
    /// <para>Run Async 작업에서 생성한 <c>Task&lt;ProcessExecutionResult&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ProcessExecutionResult&gt;</c> result produced by the run async operation.</para>
    /// \endif
    /// </returns>
    public async Task<ProcessExecutionResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory,
        TimeSpan timeout,
        int maxOutputChars,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return new ProcessExecutionResult
            {
                IsSuccess = false,
                ExitCode = -1,
                Message = "Executable path is empty."
            };
        }

        if (!File.Exists(fileName))
        {
            return new ProcessExecutionResult
            {
                IsSuccess = false,
                ExitCode = -1,
                Message = $"Executable not found: {fileName}"
            };
        }

        string resolvedWorkingDirectory = ResolveWorkingDirectory(fileName, workingDirectory);
        if (!Directory.Exists(resolvedWorkingDirectory))
        {
            return new ProcessExecutionResult
            {
                IsSuccess = false,
                ExitCode = -1,
                Message = $"Working directory not found: {resolvedWorkingDirectory}"
            };
        }

        StringBuilder output = new();
        StringBuilder error = new();
        int outputLimit = maxOutputChars <= 0 ? 6000 : maxOutputChars;

        ProcessStartInfo startInfo = new()
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = resolvedWorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using Process process = new() { StartInfo = startInfo, EnableRaisingEvents = true };

        try
        {
            process.OutputDataReceived += (_, e) => AppendLine(output, e.Data, outputLimit);
            process.ErrorDataReceived += (_, e) => AppendLine(error, e.Data, outputLimit);

            if (!process.Start())
            {
                return new ProcessExecutionResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    Message = $"Failed to start process: {fileName}"
                };
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using CancellationTokenSource timeoutSource = new(timeout);
            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token);

            try
            {
                await process.WaitForExitAsync(linkedSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);

                return new ProcessExecutionResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    Output = output.ToString(),
                    Error = error.ToString(),
                    Message = timeoutSource.IsCancellationRequested
                        ? $"Process timed out: {fileName} {arguments}"
                        : $"Process canceled: {fileName} {arguments}"
                };
            }

            return new ProcessExecutionResult
            {
                IsSuccess = process.ExitCode == 0,
                ExitCode = process.ExitCode,
                Output = output.ToString(),
                Error = error.ToString(),
                Message = process.ExitCode == 0
                    ? "Process completed successfully."
                    : $"Process failed with exit code {process.ExitCode}."
            };
        }
        catch (Exception ex)
        {
            return new ProcessExecutionResult
            {
                IsSuccess = false,
                ExitCode = -1,
                Output = output.ToString(),
                Error = error.ToString(),
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Resolve Working Directory 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve working directory operation.</para>
    /// \endif
    /// </summary>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <param name="workingDirectory">
    /// \if KO
    /// <para>working Directory에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for working directory.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Resolve Working Directory 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the resolve working directory operation.</para>
    /// \endif
    /// </returns>
    private static string ResolveWorkingDirectory(string fileName, string? workingDirectory)
    {
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            return workingDirectory;
        }

        return Path.GetDirectoryName(fileName) ?? Environment.CurrentDirectory;
    }

    /// <summary>
    /// \if KO
    /// <para>Append Line 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the append line operation.</para>
    /// \endif
    /// </summary>
    /// <param name="builder">
    /// \if KO
    /// <para>builder에 사용할 <c>StringBuilder</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>StringBuilder</c> value used for builder.</para>
    /// \endif
    /// </param>
    /// <param name="line">
    /// \if KO
    /// <para>line에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for line.</para>
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
    private static void AppendLine(StringBuilder builder, string? line, int maxOutputChars)
    {
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        if (builder.Length >= maxOutputChars)
        {
            return;
        }

        builder.AppendLine(line);

        if (builder.Length > maxOutputChars)
        {
            builder.Length = maxOutputChars;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Kill 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to kill and returns whether the operation succeeds.</para>
    /// \endif
    /// </summary>
    /// <param name="process">
    /// \if KO
    /// <para>process에 사용할 <c>Process</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Process</c> value used for process.</para>
    /// \endif
    /// </param>
    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Process cleanup best effort.
        }
    }
}
