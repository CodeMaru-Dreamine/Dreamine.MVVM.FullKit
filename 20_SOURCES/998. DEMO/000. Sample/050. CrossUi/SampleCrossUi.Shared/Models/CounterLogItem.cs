namespace SampleCrossUi.Shared.Models;

/// <summary>
/// Represents a counter operation log item.
/// </summary>
/// <param name="CreatedAt">The time when the log item was created.</param>
/// <param name="Message">The log message.</param>
public sealed record CounterLogItem(
    DateTime CreatedAt,
    string Message);
