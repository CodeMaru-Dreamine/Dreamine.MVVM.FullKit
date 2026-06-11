namespace SampleCrossUi.Shared.Services;

/// <summary>
/// Provides the default counter calculation implementation.
/// </summary>
public sealed class CounterService : ICounterService
{
    /// <inheritdoc />
    public int Increment(int currentValue) => currentValue + 1;

    /// <inheritdoc />
    public int Reset() => 0;
}
