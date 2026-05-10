using System;

namespace Sample01.States
{
    /// <summary>
    /// \brief Represents the shared counter state for the hybrid sample.
    /// </summary>
    /// <param name="Count">The current counter value.</param>
    /// <param name="LastSource">The last update source.</param>
    /// <param name="LastUpdated">The last update time.</param>
    public sealed record CounterState(
        int Count,
        string LastSource,
        DateTime? LastUpdated);
}