using Codemaru.Models.Certificates;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \brief 외부 프로세스를 실행하고 표준 출력/오류를 수집합니다.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    /// <inheritdoc />
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

    private static string ResolveWorkingDirectory(string fileName, string? workingDirectory)
    {
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            return workingDirectory;
        }

        return Path.GetDirectoryName(fileName) ?? Environment.CurrentDirectory;
    }

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
