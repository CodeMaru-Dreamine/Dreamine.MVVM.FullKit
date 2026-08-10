namespace Dreamine.SecsGem.FactoryScale.Cli;

internal interface IFactoryCommandExecutor
{
    Task<FactoryExecutionResult> ExecuteAsync(
        FactoryCommandLine command,
        CancellationToken cancellationToken);
}

internal enum FactoryExecutionStatus
{
    Succeeded,
    RuntimeFailure,
    AcceptanceFailure
}

internal sealed record FactoryExecutionResult(
    FactoryExecutionStatus Status,
    string Summary,
    string? ResultPath = null)
{
    internal static FactoryExecutionResult Success(string summary, string? resultPath = null) =>
        new(FactoryExecutionStatus.Succeeded, summary, resultPath);

    internal static FactoryExecutionResult Failed(string summary, string? resultPath = null) =>
        new(FactoryExecutionStatus.RuntimeFailure, summary, resultPath);

    internal static FactoryExecutionResult Rejected(string summary, string? resultPath = null) =>
        new(FactoryExecutionStatus.AcceptanceFailure, summary, resultPath);
}

internal sealed class UnconfiguredFactoryCommandExecutor : IFactoryCommandExecutor
{
    public Task<FactoryExecutionResult> ExecuteAsync(
        FactoryCommandLine command,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(FactoryExecutionResult.Failed(
            $"The Factory-Scale runtime executor is not configured for '{command.Command}'."));
    }
}
