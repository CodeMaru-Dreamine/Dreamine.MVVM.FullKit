using System.Diagnostics;
using System.IO;
using System.Text;
using DreamineWeb.KnowledgeQa.Application;
using DreamineWeb.KnowledgeQa.Domain;

namespace DreamineWeb.KnowledgeQa.Infrastructure;

/// <summary>Runs one schema-constrained, read-only Codex turn.</summary>
public interface ICodexCliProcessRunner
{
    Task<CodexCliProcessResult> RunAsync(
        string instruction,
        string inputJson,
        string outputSchema,
        CancellationToken cancellationToken);
    Task<CodexCliProcessResult> RunInRepositoryAsync(
        string instruction,
        string inputJson,
        string outputSchema,
        CancellationToken cancellationToken);
    string ResolveRepositoryRoot();
}

public sealed record CodexCliProcessResult(
    bool IsSuccess,
    string Output,
    string FailureKind,
    int? ExitCode,
    bool TimedOut,
    string StandardOutput,
    string StandardError,
    long ElapsedMilliseconds)
{
    public CodexInvocationDiagnostics ToDiagnostics(
        bool jsonParseSucceeded,
        bool includeDetails,
        string? rawOutput = null) => new()
    {
        Attempted = true,
        Succeeded = IsSuccess && jsonParseSucceeded,
        ExitCode = ExitCode,
        TimedOut = TimedOut,
        JsonParseSucceeded = jsonParseSucceeded,
        ElapsedMilliseconds = ElapsedMilliseconds,
        FailureKind = FailureKind,
        StandardOutput = includeDetails ? StandardOutput : string.Empty,
        StandardError = includeDetails ? StandardError : string.Empty,
        RawOutput = includeDetails ? Limit(rawOutput ?? Output, 32 * 1024) : string.Empty
    };

    private static string Limit(string value, int maximum) =>
        value.Length <= maximum ? value : value[..maximum] + "\n[truncated]";
}

/// <summary>Serializes public web requests into bounded non-interactive Codex executions.</summary>
public sealed class CodexCliProcessRunner : ICodexCliProcessRunner, IDisposable
{
    private const int MaximumOutputCharacters = 256 * 1024;
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);
    private readonly KnowledgeQaOptions _options;
    private readonly SemaphoreSlim _concurrency;

    public CodexCliProcessRunner(KnowledgeQaOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _concurrency = new SemaphoreSlim(Math.Clamp(options.CodexMaxConcurrency, 1, 4));
    }

    public async Task<CodexCliProcessResult> RunAsync(
        string instruction,
        string inputJson,
        string outputSchema,
        CancellationToken cancellationToken) => await RunCoreAsync(
            instruction,
            inputJson,
            outputSchema,
            workingDirectory: null,
            cancellationToken).ConfigureAwait(false);

    public async Task<CodexCliProcessResult> RunInRepositoryAsync(
        string instruction,
        string inputJson,
        string outputSchema,
        CancellationToken cancellationToken) => await RunCoreAsync(
            instruction,
            inputJson,
            outputSchema,
            ResolveRepositoryRoot(),
            cancellationToken).ConfigureAwait(false);

    public string ResolveRepositoryRoot()
    {
        if (!string.IsNullOrWhiteSpace(_options.RepositoryRoot))
        {
            string configured = Path.GetFullPath(_options.RepositoryRoot.Trim());
            if (Directory.Exists(configured))
                return configured;
            throw new DirectoryNotFoundException("KnowledgeQa:RepositoryRoot does not exist.");
        }

        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            DirectoryInfo? current = new(Path.GetFullPath(start));
            while (current is not null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".git"))
                    || File.Exists(Path.Combine(current.FullName, "Dreamine.MVVM.FullKit.sln")))
                {
                    return current.FullName;
                }
                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Dreamine repository root.");
    }

    private async Task<CodexCliProcessResult> RunCoreAsync(
        string instruction,
        string inputJson,
        string outputSchema,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        await _concurrency.WaitAsync(cancellationToken).ConfigureAwait(false);
        string directory = Path.Combine(Path.GetTempPath(), $"dreamine-codex-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string schemaPath = Path.Combine(directory, "schema.json");
        string outputPath = Path.Combine(directory, "answer.json");
        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            await File.WriteAllTextAsync(schemaPath, outputSchema, cancellationToken).ConfigureAwait(false);
            using Process process = new()
            {
                StartInfo = CreateStartInfo(workingDirectory ?? directory, schemaPath, outputPath, instruction)
            };
            try
            {
                if (!process.Start())
                    return Failure("start-failed", null, false, string.Empty, string.Empty, stopwatch);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Failure(
                    "start-failed",
                    null,
                    false,
                    string.Empty,
                    SafeDiagnostic(exception),
                    stopwatch);
            }

            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.StandardInput.WriteAsync(inputJson.AsMemory(), cancellationToken).ConfigureAwait(false);
            process.StandardInput.Close();

            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.RequestTimeoutSeconds, 15, 600)));
            try
            {
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                await ObserveAsync(stdoutTask, stderrTask).ConfigureAwait(false);
                return Failure(
                    "timeout",
                    TryExitCode(process),
                    true,
                    CompletedResult(stdoutTask),
                    CompletedResult(stderrTask),
                    stopwatch);
            }
            catch
            {
                TryKill(process);
                throw;
            }

            string stdout = await stdoutTask.ConfigureAwait(false);
            string stderr = await stderrTask.ConfigureAwait(false);
            if (process.ExitCode != 0)
                return Failure("nonzero-exit", process.ExitCode, false, stdout, stderr, stopwatch);
            string output = File.Exists(outputPath)
                ? await File.ReadAllTextAsync(outputPath, cancellationToken).ConfigureAwait(false)
                : stdout;
            if (string.IsNullOrWhiteSpace(output))
                return Failure("empty-output", process.ExitCode, false, stdout, stderr, stopwatch);
            if (output.Length > MaximumOutputCharacters)
                return Failure("oversized-output", process.ExitCode, false, stdout, stderr, stopwatch);
            stopwatch.Stop();
            return new CodexCliProcessResult(
                true,
                output,
                string.Empty,
                process.ExitCode,
                false,
                LimitAndSanitize(stdout),
                LimitAndSanitize(stderr),
                stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            TryDelete(directory);
            _concurrency.Release();
        }
    }

    private static CodexCliProcessResult Failure(
        string kind,
        int? exitCode,
        bool timedOut,
        string stdout,
        string stderr,
        Stopwatch stopwatch)
    {
        stopwatch.Stop();
        return new CodexCliProcessResult(
            false,
            string.Empty,
            kind,
            exitCode,
            timedOut,
            LimitAndSanitize(stdout),
            LimitAndSanitize(stderr),
            stopwatch.ElapsedMilliseconds);
    }

    private static int? TryExitCode(Process process)
    {
        try { return process.HasExited ? process.ExitCode : null; }
        catch { return null; }
    }

    private static string CompletedResult(Task<string> task)
    {
        if (!task.IsCompletedSuccessfully)
            return string.Empty;
        return task.Result;
    }

    private static string SafeDiagnostic(Exception exception) =>
        $"{exception.GetType().Name}: {exception.Message}";

    private static string LimitAndSanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        string clean = value.Replace(Path.GetTempPath(), "[temp]", StringComparison.OrdinalIgnoreCase);
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
            clean = clean.Replace(userProfile, "[user]", StringComparison.OrdinalIgnoreCase);
        string repositoryRoot = Directory.GetCurrentDirectory();
        if (!string.IsNullOrWhiteSpace(repositoryRoot))
            clean = clean.Replace(repositoryRoot, "[repository]", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(AppContext.BaseDirectory))
            clean = clean.Replace(AppContext.BaseDirectory, "[application]", StringComparison.OrdinalIgnoreCase);
        const int maximum = 16 * 1024;
        return clean.Length <= maximum ? clean : clean[..maximum] + "\n[truncated]";
    }

    private ProcessStartInfo CreateStartInfo(
        string workingDirectory,
        string schemaPath,
        string outputPath,
        string instruction)
    {
        string configuredExecutable = string.IsNullOrWhiteSpace(_options.CodexExecutable)
            ? "codex"
            : _options.CodexExecutable.Trim();
        string executable = ResolveCodexExecutable(configuredExecutable);
        bool commandScript = OperatingSystem.IsWindows()
            && (executable.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)
                || executable.EndsWith(".bat", StringComparison.OrdinalIgnoreCase));
        ProcessStartInfo startInfo = new()
        {
            FileName = commandScript
                ? Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe"
                : executable,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Utf8NoBom,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom
        };
        if (commandScript)
        {
            startInfo.ArgumentList.Add("/d");
            startInfo.ArgumentList.Add("/s");
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add(executable);
        }
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("--sandbox");
        startInfo.ArgumentList.Add("read-only");
        startInfo.ArgumentList.Add("--ephemeral");
        startInfo.ArgumentList.Add("--ignore-user-config");
        startInfo.ArgumentList.Add("--ignore-rules");
        startInfo.ArgumentList.Add("--skip-git-repo-check");
        if (!string.IsNullOrWhiteSpace(_options.CodexModel))
        {
            startInfo.ArgumentList.Add("--model");
            startInfo.ArgumentList.Add(_options.CodexModel.Trim());
        }
        startInfo.ArgumentList.Add("--output-schema");
        startInfo.ArgumentList.Add(schemaPath);
        startInfo.ArgumentList.Add("--output-last-message");
        startInfo.ArgumentList.Add(outputPath);
        startInfo.ArgumentList.Add(instruction);
        return startInfo;
    }

    private static string ResolveCodexExecutable(string configuredExecutable)
    {
        if (!OperatingSystem.IsWindows()
            || Path.IsPathRooted(configuredExecutable)
            || !Path.GetFileNameWithoutExtension(configuredExecutable)
                .Equals("codex", StringComparison.OrdinalIgnoreCase))
        {
            return configuredExecutable;
        }

        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            string desktopCli = Path.Combine(userProfile, ".codex", ".sandbox-bin", "codex.exe");
            if (File.Exists(desktopCli))
                return desktopCli;
        }

        return configuredExecutable;
    }

    private static async Task ObserveAsync(params Task<string>[] tasks)
    {
        try { await Task.WhenAll(tasks).ConfigureAwait(false); }
        catch { }
    }

    private static void TryKill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch { }
    }

    private static void TryDelete(string directory)
    {
        try { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
        catch { }
    }

    public void Dispose() => _concurrency.Dispose();
}
