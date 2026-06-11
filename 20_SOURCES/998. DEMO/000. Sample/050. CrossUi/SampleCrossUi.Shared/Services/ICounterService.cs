namespace SampleCrossUi.Shared.Services;

/// <summary>
/// Provides counter calculation features for sample applications.
/// </summary>
public interface ICounterService
{
    /// <summary>
    /// Increments the specified value.
    /// </summary>
    /// <param name="currentValue">The current counter value.</param>
    /// <returns>The incremented value.</returns>
    int Increment(int currentValue);

    /// <summary>
    /// Resets the counter value.
    /// </summary>
    /// <returns>The reset counter value (always 0).</returns>
    int Reset();
}
