using Dreamine.MVVM.Attributes;

namespace Dreamine.SecsGem.Interop.Wpf.ViewModels;

public sealed partial class MainWindowViewModel
{
    [DreamineCommand("Event.Connect", CanExecute = nameof(CanConnect))]
    private partial Task Connect();
    private bool CanConnect() => Event.CanConnect;

    [DreamineCommand("Event.Disconnect", CanExecute = nameof(CanDisconnect))]
    private partial Task Disconnect();
    private bool CanDisconnect() => Event.CanDisconnect;

    [DreamineCommand("Event.Select", CanExecute = nameof(CanSelect))]
    private partial Task Select();
    private bool CanSelect() => Event.CanSelect;

    [DreamineCommand("Event.Linktest", CanExecute = nameof(CanLinktest))]
    private partial Task Linktest();
    private bool CanLinktest() => Event.CanLinktest;

    [DreamineCommand("Event.Separate", CanExecute = nameof(CanSeparate))]
    private partial Task Separate();
    private bool CanSeparate() => Event.CanSeparate;

    [DreamineCommand("Event.Reconnect", CanExecute = nameof(CanReconnect))]
    private partial Task Reconnect();
    private bool CanReconnect() => Event.CanReconnect;

    [DreamineCommand("Event.Send", CanExecute = nameof(CanSend))]
    private partial Task Send();
    private bool CanSend() => Event.CanSend;

    [DreamineCommand("Event.RunSelfTest", CanExecute = nameof(CanRunOperation))]
    private partial Task RunSelfTest();

    [DreamineCommand("Event.Export", CanExecute = nameof(CanRunOperation))]
    private partial Task Export();

    [DreamineCommand("Event.RunSelected", CanExecute = nameof(CanRunOperation))]
    private partial Task RunSelected();

    [DreamineCommand("Event.RunAll", CanExecute = nameof(CanRunOperation))]
    private partial Task RunAll();

    [DreamineCommand("Event.EnableResponder", CanExecute = nameof(CanEnableResponder))]
    private partial void EnableResponder();
    private bool CanEnableResponder() => Event.CanEnableResponder;

    [DreamineCommand("Event.StartEquipmentSidecar", CanExecute = nameof(CanStartEquipmentSidecar))]
    private partial Task StartEquipmentSidecar();
    private bool CanStartEquipmentSidecar() => Event.CanStartEquipmentSidecar;

    [DreamineCommand("Event.ForceStopEquipmentSidecar", CanExecute = nameof(CanForceStopEquipmentSidecar))]
    private partial Task ForceStopEquipmentSidecar();
    private bool CanForceStopEquipmentSidecar() => Event.CanForceStopEquipmentSidecar;

    [DreamineCommand("Event.BrowseEquipmentSidecar", CanExecute = nameof(CanAlways))]
    private partial void BrowseEquipmentSidecar();

    [DreamineCommand("Event.BrowseEquipmentSidecarEvidence", CanExecute = nameof(CanAlways))]
    private partial void BrowseEquipmentSidecarEvidence();

    [DreamineCommand("Event.LaunchSimulator", CanExecute = nameof(CanAlways))]
    private partial void LaunchSimulator();

    [DreamineCommand("Event.BrowseSimulator", CanExecute = nameof(CanAlways))]
    private partial void BrowseSimulator();

    [DreamineCommand("Event.BrowseExport", CanExecute = nameof(CanAlways))]
    private partial void BrowseExport();

    [DreamineCommand("Event.Cancel", CanExecute = nameof(CanAlways))]
    private partial void Cancel();

    [DreamineCommand("Event.MarkWaiting", CanExecute = nameof(CanAlways))]
    private partial void MarkWaiting();

    [DreamineCommand("Event.ResetScenarios", CanExecute = nameof(CanAlways))]
    private partial void ResetScenarios();

    [DreamineCommand("Event.AddRootItem", CanExecute = nameof(CanAlways))]
    private partial void AddRootItem();

    [DreamineCommand("Event.AddChildItem", CanExecute = nameof(CanAlways))]
    private partial void AddChildItem();

    [DreamineCommand("Event.RemoveItem", CanExecute = nameof(CanAlways))]
    private partial void RemoveItem();

    [DreamineCommand("Event.ClearLog", CanExecute = nameof(CanAlways))]
    private partial void ClearLog();

    [DreamineCommand("Event.AddEquipment", CanExecute = nameof(CanAddEquipment))]
    private partial void AddEquipment();
    private bool CanAddEquipment() => Event.CanAddEquipment;

    [DreamineCommand("Event.RemoveEquipment", CanExecute = nameof(CanRemoveEquipment))]
    private partial Task RemoveEquipment();
    private bool CanRemoveEquipment() => Event.CanRemoveEquipment;

    [DreamineCommand("Event.ConnectSelectedEquipment", CanExecute = nameof(CanConnectSelectedEquipment))]
    private partial Task ConnectSelectedEquipment();
    private bool CanConnectSelectedEquipment() => Event.CanConnectSelectedEquipment;

    [DreamineCommand("Event.DisconnectSelectedEquipment", CanExecute = nameof(CanDisconnectSelectedEquipment))]
    private partial Task DisconnectSelectedEquipment();
    private bool CanDisconnectSelectedEquipment() => Event.CanDisconnectSelectedEquipment;

    [DreamineCommand("Event.ReconnectSelectedEquipment", CanExecute = nameof(CanReconnectSelectedEquipment))]
    private partial Task ReconnectSelectedEquipment();
    private bool CanReconnectSelectedEquipment() => Event.CanReconnectSelectedEquipment;

    [DreamineCommand("Event.ConnectAllEquipment", CanExecute = nameof(CanConnectAllEquipment))]
    private partial Task ConnectAllEquipment();
    private bool CanConnectAllEquipment() => Event.CanConnectAllEquipment;

    [DreamineCommand("Event.DisconnectAllEquipment", CanExecute = nameof(CanDisconnectAllEquipment))]
    private partial Task DisconnectAllEquipment();
    private bool CanDisconnectAllEquipment() => Event.CanDisconnectAllEquipment;

    [DreamineCommand("Event.LinktestSelectedEquipment", CanExecute = nameof(CanLinktestSelectedEquipment))]
    private partial Task LinktestSelectedEquipment();
    private bool CanLinktestSelectedEquipment() => Event.CanLinktestSelectedEquipment;

    [DreamineCommand("Event.LinktestAllEquipment", CanExecute = nameof(CanLinktestAllEquipment))]
    private partial Task LinktestAllEquipment();
    private bool CanLinktestAllEquipment() => Event.CanLinktestAllEquipment;

    [DreamineCommand("Event.SendSelectedEquipment", CanExecute = nameof(CanSendSelectedEquipment))]
    private partial Task SendSelectedEquipment();
    private bool CanSendSelectedEquipment() => Event.CanSendSelectedEquipment;

    [DreamineCommand("Event.BroadcastEquipment", CanExecute = nameof(CanBroadcastEquipment))]
    private partial Task BroadcastEquipment();
    private bool CanBroadcastEquipment() => Event.CanBroadcastEquipment;

    [DreamineCommand("Event.RunSelectedEquipmentScenario", CanExecute = nameof(CanRunSelectedEquipmentScenario))]
    private partial Task RunSelectedEquipmentScenario();
    private bool CanRunSelectedEquipmentScenario() => Event.CanRunSelectedEquipmentScenario;

    [DreamineCommand("Event.RunAllEquipmentScenarios", CanExecute = nameof(CanRunAllEquipmentScenarios))]
    private partial Task RunAllEquipmentScenarios();
    private bool CanRunAllEquipmentScenarios() => Event.CanRunAllEquipmentScenarios;

    [DreamineCommand("Event.RunMultiEquipmentSelfTest", CanExecute = nameof(CanUseMultiEquipmentMode))]
    private partial Task RunMultiEquipmentSelfTest();

    [DreamineCommand("Event.ImportEquipmentConfiguration", CanExecute = nameof(CanUseMultiEquipmentMode))]
    private partial Task ImportEquipmentConfiguration();

    [DreamineCommand("Event.ExportEquipmentConfiguration", CanExecute = nameof(CanUseMultiEquipmentMode))]
    private partial Task ExportEquipmentConfiguration();

    [DreamineCommand("Event.ImportFactoryResult", CanExecute = nameof(CanRunOperation))]
    private partial Task ImportFactoryResult();

    private bool CanAlways() => true;
    private bool CanRunOperation() => Event.IsOperationIdle;
    private bool CanUseMultiEquipmentMode() => Event.IsOperationIdle && IsMultiEquipmentMode;
}
