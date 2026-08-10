using Dreamine.SecsGem.FactoryScale.Cli;

namespace Dreamine.SecsGem.FactoryScale;

internal static partial class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (FactoryCommandParser.IsHelpRequest(args))
        {
            Console.WriteLine(FactoryCommandParser.Usage);
            return FactoryExitCodes.Success;
        }

        FactoryCommandLine command;
        try
        {
            command = FactoryCommandParser.Parse(args);
        }
        catch (FactoryCommandLineException exception)
        {
            await Console.Error.WriteLineAsync($"Usage error: {exception.Message}").ConfigureAwait(false);
            await Console.Error.WriteLineAsync().ConfigureAwait(false);
            await Console.Error.WriteLineAsync(FactoryCommandParser.Usage).ConfigureAwait(false);
            return FactoryExitCodes.UsageError;
        }

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        EventHandler processExitHandler = (_, _) => cancellation.Cancel();
        Console.CancelKeyPress += cancelHandler;
        AppDomain.CurrentDomain.ProcessExit += processExitHandler;

        try
        {
            IFactoryCommandExecutor? executor = null;
            ConfigureExecutor(ref executor);
            executor ??= new UnconfiguredFactoryCommandExecutor();

            var result = await executor.ExecuteAsync(command, cancellation.Token).ConfigureAwait(false);
            var output = result.Status == FactoryExecutionStatus.Succeeded ? Console.Out : Console.Error;
            await output.WriteLineAsync(result.Summary).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(result.ResultPath))
                await output.WriteLineAsync($"Result: {Path.GetFullPath(result.ResultPath)}").ConfigureAwait(false);

            return result.Status switch
            {
                FactoryExecutionStatus.Succeeded => FactoryExitCodes.Success,
                FactoryExecutionStatus.AcceptanceFailure => FactoryExitCodes.AcceptanceFailure,
                _ => FactoryExitCodes.RuntimeFailure
            };
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            await Console.Error.WriteLineAsync("Factory-Scale run canceled.").ConfigureAwait(false);
            return FactoryExitCodes.Canceled;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(
                $"Factory-Scale runtime failure ({exception.GetType().Name}): {exception.Message}").ConfigureAwait(false);
            return FactoryExitCodes.RuntimeFailure;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
            AppDomain.CurrentDomain.ProcessExit -= processExitHandler;
        }
    }

    static partial void ConfigureExecutor(ref IFactoryCommandExecutor? executor);
}
