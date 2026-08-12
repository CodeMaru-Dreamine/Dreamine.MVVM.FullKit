using Dreamine.MVVM.Core;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Options;
using Dreamine.Secs.Abstractions.Providers;
using Dreamine.Secs.Com;
using Dreamine.SecsGem.Interop.Runtime.Logging;
using Dreamine.SecsGem.Interop.Runtime.Profiles;
using Dreamine.SecsGem.Interop.Runtime.Scenarios;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Dreamine.SecsGem.Interop.Wpf.Models;
using Dreamine.SecsGem.Interop.Wpf.ViewModels;
using Dreamine.SecsGem.Interop.Wpf.Views;
using System.IO;
using System.Text.Json;

namespace Dreamine.SecsGem.Interop.Wpf;

public partial class App
{
    static partial void ShowMainWindow()
    {
        var arguments = Environment.GetCommandLineArgs();
        if (arguments.Contains("--scenario", StringComparer.OrdinalIgnoreCase) ||
            arguments.Contains("--profile", StringComparer.OrdinalIgnoreCase))
        {
            LaunchHeadlessProcess(
                arguments,
                static values => RunHeadlessScenarioAsync(values, CancellationToken.None),
                ScenarioExitCodesV1.Failed);
            return;
        }
        if (arguments.Contains("--multi-self-test", StringComparer.OrdinalIgnoreCase))
        {
            LaunchHeadlessProcess(
                arguments,
                RunHeadlessMultiEquipmentSelfTestAsync,
                failureExitCode: 1);
            return;
        }
        if (arguments.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
        {
            LaunchHeadlessProcess(
                arguments,
                RunHeadlessSelfTestAsync,
                failureExitCode: 1);
            return;
        }

        var view = new MainWindow
        {
            DataContext = DMContainer.Resolve<MainWindowViewModel>()
        };
        Current.MainWindow = view;
        view.Show();
    }

    // WPF startup is synchronous. This async-void method is an intentional event-entry boundary:
    // every awaited operation and shutdown attempt is observed and caught inside the method.
    private static async void LaunchHeadlessProcess(
        string[] arguments,
        Func<string[], Task<int>> runAsync,
        int failureExitCode)
    {
        var exitCode = failureExitCode;
        try
        {
            exitCode = await runAsync(arguments).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.WriteLine(exception);
        }
        try
        {
            Environment.ExitCode = exitCode;
            await Current.Dispatcher.InvokeAsync(() => Current.Shutdown(exitCode));
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.WriteLine(exception);
            Environment.ExitCode = failureExitCode;
            try { Current.Shutdown(failureExitCode); }
            catch (Exception shutdownException) { System.Diagnostics.Trace.WriteLine(shutdownException); }
        }
    }

    /// <summary>
    /// Parses the bounded Scenario v1 headless command line. Both document paths are mandatory;
    /// output is optional and never inferred.
    /// </summary>
    internal static ScenarioCommandLineArguments ParseScenarioArguments(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        string? scenario = null;
        string? profile = null;
        string? output = null;
        try
        {
            for (var index = 1; index < arguments.Count; index++)
            {
                var option = arguments[index];
                var normalized = option.ToLowerInvariant();
                if (normalized is not ("--scenario" or "--profile" or "--output"))
                    return ScenarioCommandLineArguments.Invalid($"Unknown headless scenario option '{option}'.");
                if (++index >= arguments.Count || string.IsNullOrWhiteSpace(arguments[index]))
                    return ScenarioCommandLineArguments.Invalid($"Option '{option}' requires a path.");
                var value = arguments[index];
                switch (normalized)
                {
                    case "--scenario" when scenario is null:
                        scenario = Path.GetFullPath(value);
                        break;
                    case "--profile" when profile is null:
                        profile = Path.GetFullPath(value);
                        break;
                    case "--output" when output is null:
                        output = Path.GetFullPath(value);
                        break;
                    default:
                        return ScenarioCommandLineArguments.Invalid($"Option '{option}' was specified more than once.");
                }
            }
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return ScenarioCommandLineArguments.Invalid(exception.Message);
        }

        if (scenario is null || profile is null)
            return ScenarioCommandLineArguments.Invalid(
                "Headless Scenario v1 requires --scenario <file> and --profile <file>.");
        return new ScenarioCommandLineArguments(
            true,
            ScenarioExitCodesV1.Passed,
            scenario,
            profile,
            output,
            null);
    }

    /// <summary>Runs the exact shared ScenarioRunnerV1 against one profile-created native session.</summary>
    internal static async Task<int> RunHeadlessScenarioAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken) =>
        await RunHeadlessScenarioAsync(
            arguments,
            cancellationToken,
            sessionFactory: null,
            wireLogFactory: null).ConfigureAwait(false);

    internal static async Task<int> RunHeadlessScenarioAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken,
        Func<SingleConnectionProfileV1, HsmsWireObservationOptions, ISecsMessageSession>? sessionFactory,
        Func<InteropLogManager, string, WireLogManager>? wireLogFactory)
    {
        var parsed = ParseScenarioArguments(arguments);
        if (!parsed.IsValid) return parsed.ExitCode;

        ScenarioRunResultV1? result = null;
        ISecsMessageSession? session = null;
        WireLogManager? wireLog = null;
        EventHandler<SecsDiagnosticEvent>? diagnosticHandler = null;
        EventHandler<SecsSessionStateChangedEventArgs>? stateHandler = null;
        Exception? cleanupFailure = null;
        try
        {
            var profile = await ConnectionProfileStore.Create()
                .LoadAsync(parsed.ProfilePath!, cancellationToken)
                .ConfigureAwait(false);
            var scenario = await new ScenarioFileStoreV1()
                .LoadAsync(parsed.ScenarioPath!, cancellationToken)
                .ConfigureAwait(false);

            var semanticLog = new InteropLogManager();
            var logRootBase = Path.GetDirectoryName(
                parsed.OutputPath ?? parsed.ScenarioPath) ?? Environment.CurrentDirectory;
            var logRoot = Path.Combine(logRootBase, "dreamine-wire-logs");
            wireLog = wireLogFactory?.Invoke(semanticLog, logRoot) ??
                new WireLogManager(semanticLog, logRoot);

            var settings = new ConnectionSettings();
            settings.ApplyProfile(profile);
            var observationOptions = wireLog.CreateObservationOptions(profile.LogPolicyId);
            settings.WireObservation = observationOptions;
            session = sessionFactory?.Invoke(profile, observationOptions);
            if (session is null)
            {
                var provider = new DreamineSecsCommunicationProvider(_ => settings.ToOptions());
                session = provider.CreateSession(new SecsConnectionOptions
                {
                    ProviderKey = SecsProviderKeys.Dreamine,
                    Role = profile.Role,
                    Mode = profile.Mode
                });
            }
            var identity = session.ConnectionIdentity;
            wireLog.Start(
                session,
                new WireLogIdentity(
                    "Headless",
                    identity.SessionInstanceId.ToString("N"),
                    "redacted",
                    profile.SessionId),
                profile.LogPolicyId);
            diagnosticHandler = (_, diagnostic) =>
            {
                try
                {
                    wireLog.RecordDiagnostic(
                        diagnostic,
                        session.ConnectionIdentity.ConnectionEpoch);
                }
                catch (Exception exception) { System.Diagnostics.Trace.WriteLine(exception); }
            };
            stateHandler = (_, transition) =>
            {
                try { wireLog.RecordState(transition); }
                catch (Exception exception) { System.Diagnostics.Trace.WriteLine(exception); }
            };
            session.DiagnosticReceived += diagnosticHandler;
            session.StateChanged += stateHandler;

            result = await new ScenarioRunnerV1()
                .RunAsync(scenario, session, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            result = new ScenarioRunResultV1(
                ScenarioRunStatusV1.Cancelled,
                ScenarioExitCodesV1.Cancelled,
                now,
                now,
                [],
                "cancelled",
                "The headless scenario was cancelled by the caller.");
        }
        catch (Exception exception)
        {
            var now = DateTimeOffset.UtcNow;
            result = new ScenarioRunResultV1(
                ScenarioRunStatusV1.Invalid,
                ScenarioExitCodesV1.Invalid,
                now,
                now,
                [],
                "invalid_input",
                exception.Message);
        }
        finally
        {
            if (session is not null)
            {
                try { await session.DisposeAsync().ConfigureAwait(false); }
                catch (Exception exception)
                {
                    cleanupFailure = exception;
                    System.Diagnostics.Trace.WriteLine(exception);
                }
                finally
                {
                    if (diagnosticHandler is not null)
                        session.DiagnosticReceived -= diagnosticHandler;
                    if (stateHandler is not null)
                        session.StateChanged -= stateHandler;
                }
            }
            if (wireLog is not null)
            {
                try { await wireLog.DisposeAsync().ConfigureAwait(false); }
                catch (Exception exception)
                {
                    cleanupFailure = cleanupFailure is null
                        ? exception
                        : new AggregateException(cleanupFailure, exception);
                    System.Diagnostics.Trace.WriteLine(exception);
                }
            }
        }

        result ??= CreateHeadlessFailure(
            "headless_failed",
            "The headless scenario ended without a structured result.");
        if (result.Status == ScenarioRunStatusV1.Passed && cleanupFailure is not null)
        {
            result = CreateHeadlessFailure(
                "cleanup_failed",
                cleanupFailure.Message,
                result);
        }
        else if (result.Status == ScenarioRunStatusV1.Passed &&
                 wireLog is not null &&
                 !wireLog.LastHealth.IsEvidenceEligible)
        {
            var health = wireLog.LastHealth;
            result = CreateHeadlessFailure(
                "wire_log_unhealthy",
                $"Wire log was not flushed without loss: sourceDropped={health.SourceDropped}, " +
                $"recorderDropped={health.RecorderDropped}, flushCompleted={health.FlushCompleted}, " +
                $"writerFailure={health.WriterFailure ?? "none"}.",
                result);
        }

        if (parsed.OutputPath is not null)
            await WriteScenarioResultAsync(parsed.OutputPath, result, CancellationToken.None)
                .ConfigureAwait(false);
        return result.ExitCode;
    }

    private static ScenarioRunResultV1 CreateHeadlessFailure(
        string errorCode,
        string message,
        ScenarioRunResultV1? previous = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new ScenarioRunResultV1(
            ScenarioRunStatusV1.Failed,
            ScenarioExitCodesV1.Failed,
            previous?.StartedAtUtc ?? now,
            now,
            previous?.Steps ?? [],
            errorCode,
            message,
            previous?.DroppedInboundMessageCount ?? 0);
    }

    /// <summary>Durably replaces one structured result through a same-directory temporary file.</summary>
    internal static async Task WriteScenarioResultAsync(
        string path,
        ScenarioRunResultV1 result,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(result);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ??
            throw new ArgumentException("The output path has no parent directory.", nameof(path));
        Directory.CreateDirectory(directory);
        RejectReparsePoint(directory, "The output directory cannot be a reparse point.");
        if (File.Exists(fullPath))
            RejectReparsePoint(fullPath, "The output file cannot be a reparse point.");

        var temporary = Path.Combine(
            directory,
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(
                             temporary,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             64 * 1024,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var publicResult = CreatePublicHeadlessResult(result);
                await JsonSerializer.SerializeAsync(stream, publicResult, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static HeadlessScenarioResultDocumentV1 CreatePublicHeadlessResult(
        ScenarioRunResultV1 result)
    {
        var steps = result.Steps ?? [];
        var passed = 0;
        var failed = 0;
        var timedOut = 0;
        var cancelled = 0;
        foreach (var step in steps)
        {
            switch (step.Status)
            {
                case ScenarioStepStatusV1.Passed:
                    passed++;
                    break;
                case ScenarioStepStatusV1.Failed:
                    failed++;
                    break;
                case ScenarioStepStatusV1.TimedOut:
                    timedOut++;
                    break;
                case ScenarioStepStatusV1.Cancelled:
                    cancelled++;
                    break;
            }
        }

        return new HeadlessScenarioResultDocumentV1(
            result.Status,
            result.ExitCode,
            result.StartedAtUtc,
            result.CompletedAtUtc,
            steps.Count,
            passed,
            failed,
            timedOut,
            cancelled,
            result.DroppedInboundMessageCount,
            SafeHeadlessErrorCode(result.Status, result.ErrorCode));
    }

    private static string? SafeHeadlessErrorCode(
        ScenarioRunStatusV1 status,
        string? errorCode) => errorCode switch
    {
        "assertion_failed" => errorCode,
        "cancelled" => errorCode,
        "cleanup_failed" => errorCode,
        "failed" => errorCode,
        "headless_failed" => errorCode,
        "inbound_messages_dropped" => errorCode,
        "invalid" => errorCode,
        "invalid_input" => errorCode,
        "invalid_scenario" => errorCode,
        "run_timeout" => errorCode,
        "step_failed" => errorCode,
        "step_timeout" => errorCode,
        "timed_out" => errorCode,
        "wire_log_unhealthy" => errorCode,
        _ => status switch
        {
            ScenarioRunStatusV1.Passed => null,
            ScenarioRunStatusV1.Invalid => "invalid",
            ScenarioRunStatusV1.TimedOut => "timed_out",
            ScenarioRunStatusV1.Cancelled => "cancelled",
            _ => "failed"
        }
    };

    private static void RejectReparsePoint(string path, string message)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException(message);
    }

    private static async Task<int> RunHeadlessMultiEquipmentSelfTestAsync(string[] arguments)
    {
        var log = DMContainer.Resolve<InteropLogManager>();
        var scenarioManager = new MultiEquipmentScenarioManager(log);
        var result = await scenarioManager.RunAsync(100, CancellationToken.None);
        var outputIndex = Array.FindIndex(arguments, value => value.Equals("--output", StringComparison.OrdinalIgnoreCase));
        if (outputIndex >= 0 && outputIndex + 1 < arguments.Length)
        {
            var exportManager = DMContainer.Resolve<ResultExportManager>();
            await exportManager.ExportMultiEquipmentSelfTestAsync(arguments[outputIndex + 1], result, CancellationToken.None);
        }
        return result.Result == "Passed" &&
               result.RemainingSessions == 0 &&
               result.RemainingBackgroundOperations == 0
            ? 0
            : 2;
    }

    private static async Task<int> RunHeadlessSelfTestAsync(string[] arguments)
    {
        var scenarioManager = DMContainer.Resolve<ScenarioManager>();
        var result = await scenarioManager.RunSelfLoopbackAsync(1000, 100, CancellationToken.None);
        var outputIndex = Array.FindIndex(arguments, value => value.Equals("--output", StringComparison.OrdinalIgnoreCase));
        if (outputIndex >= 0 && outputIndex + 1 < arguments.Length)
        {
            var exportManager = DMContainer.Resolve<ResultExportManager>();
            await exportManager.ExportSelfTestAsync(arguments[outputIndex + 1], result, CancellationToken.None);
        }
        return result.Failed == 0 ? 0 : 2;
    }
}

internal sealed record ScenarioCommandLineArguments(
    bool IsValid,
    int ExitCode,
    string? ScenarioPath,
    string? ProfilePath,
    string? OutputPath,
    string? Error)
{
    internal static ScenarioCommandLineArguments Invalid(string error) =>
        new(false, ScenarioExitCodesV1.Invalid, null, null, null, error);
}

/// <summary>
/// Closed public projection for headless Scenario v1 output. It deliberately contains no
/// profile/scenario paths, provider diagnostics, step identifiers, endpoints, or message data.
/// </summary>
internal sealed record HeadlessScenarioResultDocumentV1(
    ScenarioRunStatusV1 Status,
    int ExitCode,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int TotalStepCount,
    int PassedStepCount,
    int FailedStepCount,
    int TimedOutStepCount,
    int CancelledStepCount,
    long DroppedInboundMessageCount,
    string? ErrorCode);
