using System.Diagnostics;
using Dreamine.SecsGem.FactoryScale.Models;

namespace Dreamine.SecsGem.FactoryScale.Metrics;

/// <summary>
/// Supplies an operating-system or host-registry socket measurement. The
/// collector does not infer socket count from Process.HandleCount.
/// </summary>
internal interface IProcessSocketMetricsProvider
{
    FactorySocketMetricSnapshot Capture(Process process);
}

internal sealed class UnavailableProcessSocketMetricsProvider : IProcessSocketMetricsProvider
{
    internal static readonly UnavailableProcessSocketMetricsProvider Instance = new();
    private UnavailableProcessSocketMetricsProvider() { }

    public FactorySocketMetricSnapshot Capture(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        return FactorySocketMetricSnapshot.Unavailable();
    }
}

/// <summary>Small injection adapter used by platform probes and unit tests.</summary>
internal sealed class DelegateProcessSocketMetricsProvider(
    Func<Process, FactorySocketMetricSnapshot> capture) : IProcessSocketMetricsProvider
{
    private readonly Func<Process, FactorySocketMetricSnapshot> _capture =
        capture ?? throw new ArgumentNullException(nameof(capture));

    public FactorySocketMetricSnapshot Capture(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        return _capture(process) ?? throw new InvalidOperationException("The socket metric provider returned null.");
    }
}
