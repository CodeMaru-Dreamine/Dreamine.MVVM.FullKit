namespace Dreamine.SecsGem.FactoryScale.Metrics;

internal static class FactorySnapshotSchedule
{
    /// <summary>
    /// Produces the required soak checkpoints: 1 minute, 5 minutes, 10 minutes,
    /// then every 10 minutes. The scenario runner should also capture a final
    /// snapshot when duration does not end on a checkpoint.
    /// </summary>
    internal static IReadOnlyList<TimeSpan> Create(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        var result = new List<TimeSpan>();
        AddIfDue(TimeSpan.FromMinutes(1));
        AddIfDue(TimeSpan.FromMinutes(5));
        for (var due = TimeSpan.FromMinutes(10); due <= duration; due += TimeSpan.FromMinutes(10))
            result.Add(due);
        return result;

        void AddIfDue(TimeSpan due)
        {
            if (due <= duration) result.Add(due);
        }
    }
}
