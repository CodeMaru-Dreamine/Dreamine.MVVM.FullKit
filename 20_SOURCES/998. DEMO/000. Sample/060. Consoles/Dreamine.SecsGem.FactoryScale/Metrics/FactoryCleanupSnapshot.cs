using Dreamine.SecsGem.FactoryScale.Models;
using Dreamine.SecsGem.FactoryScale.Simulation;
using Dreamine.SecsGem.Interop.Runtime;

namespace Dreamine.SecsGem.FactoryScale.Metrics;

internal static class FactoryCleanupSnapshot
{
    /// <summary>
    /// Captures live metric leases and static orchestration ownership after
    /// deterministic disposal. Values are measured; they are not zero-filled.
    /// The OS process table remains the independent socket/listener authority.
    /// </summary>
    internal static FactoryMetricSnapshot Capture(FactoryMetricsCollector metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        var measured = metrics.CaptureSnapshot();
        var workers = measured.OwnedWorkers;
        var operatingSystemListeners = measured.Process.Sockets is
            { IsProcessOwnedMeasurement: true, ListenerCount: { } listeners }
            ? listeners
            : measured.TrackedListeners;
        return measured with
        {
            RequestedEquipment = 0,
            ConnectedEquipment = 0,
            SelectedEquipment = 0,
            FailedEquipment = 0,
            ReconnectingEquipment = EquipmentConnectionContext.LiveReconnectOperationCount,
            TrackedSessions = measured.TrackedSessions + EquipmentConnectionContext.LiveSessionCount +
                              FactoryEquipmentPeer.LiveSessionCount,
            TrackedListeners = operatingSystemListeners,
            TrackedOperations = measured.TrackedOperations +
                                EquipmentConnectionContext.LiveBackgroundOperationCount +
                                workers.HostMessageWorkers +
                                workers.EquipmentResponderWorkers +
                                workers.DiagnosticDrainWorkers,
            ReconnectOperations = measured.ReconnectOperations +
                                  EquipmentConnectionContext.LiveReconnectOperationCount,
            Queues = Array.Empty<FactoryQueueMetricSnapshot>()
        };
    }
}
