using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Dreamine.SecsGem.Interop.Wpf.Models;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class EquipmentSidecarDiagnosticParserTests
{
    [Theory]
    [InlineData("FrameSent")]
    [InlineData("FrameReceived")]
    [InlineData("PrimarySent")]
    [InlineData("SecondaryReceived")]
    [InlineData("StateChanged")]
    public void NormalHsmsDiagnosticsRemainInformationalOnStandardError(string kind)
    {
        var result = EquipmentSidecarDiagnosticParser.Classify(
            EquipmentSidecarOutputStream.StandardError,
            $"HSMS {kind}; state=Selected; frameLength=17.");

        Assert.False(result.IsError);
        Assert.True(result.IsStructuredHsms);
        Assert.Equal(17, result.FrameLength);
        Assert.Equal(EquipmentSidecarTcpStatus.Connected, result.Connection!.Tcp);
        Assert.Equal(EquipmentSidecarHsmsStatus.Selected, result.Connection.Hsms);
    }

    [Theory]
    [InlineData(SecsDiagnosticKind.Timeout)]
    [InlineData(SecsDiagnosticKind.ProtocolError)]
    [InlineData(SecsDiagnosticKind.ApplicationError)]
    public void FaultDiagnosticsAreErrors(SecsDiagnosticKind kind)
    {
        var result = EquipmentSidecarDiagnosticParser.Classify(
            EquipmentSidecarOutputStream.StandardError,
            $"HSMS {kind}; state=Selected; frameLength=n/a.");

        Assert.True(result.IsError);
        Assert.Equal(kind, result.DiagnosticKind);
    }

    [Fact]
    public void ConnectionLifecycleProjectsTcpAndHsmsStates()
    {
        var listening = EquipmentSidecarDiagnosticParser.Classify(
            EquipmentSidecarOutputStream.StandardError,
            "HSMS ConnectionAttempt; state=n/a; frameLength=n/a.");
        var connected = EquipmentSidecarDiagnosticParser.Classify(
            EquipmentSidecarOutputStream.StandardError,
            "HSMS ConnectionEstablished; state=ConnectedNotSelected; frameLength=n/a.");
        var selected = EquipmentSidecarDiagnosticParser.Classify(
            EquipmentSidecarOutputStream.StandardError,
            "HSMS StateChanged; state=Selected; frameLength=n/a.");
        var disconnected = EquipmentSidecarDiagnosticParser.Classify(
            EquipmentSidecarOutputStream.StandardError,
            "HSMS ConnectionClosed; state=NotConnected; frameLength=n/a.");

        Assert.Equal(
            new EquipmentSidecarConnectionProjection(
                EquipmentSidecarTcpStatus.Listening,
                EquipmentSidecarHsmsStatus.NotConnected),
            listening.Connection);
        Assert.Equal(
            new EquipmentSidecarConnectionProjection(
                EquipmentSidecarTcpStatus.Connected,
                EquipmentSidecarHsmsStatus.ConnectedNotSelected),
            connected.Connection);
        Assert.Equal(
            new EquipmentSidecarConnectionProjection(
                EquipmentSidecarTcpStatus.Connected,
                EquipmentSidecarHsmsStatus.Selected),
            selected.Connection);
        Assert.Equal(
            new EquipmentSidecarConnectionProjection(
                EquipmentSidecarTcpStatus.Disconnected,
                EquipmentSidecarHsmsStatus.NotConnected),
            disconnected.Connection);
    }

    [Fact]
    public void RejectIsObservableButNotClassifiedAsProcessFault()
    {
        var result = EquipmentSidecarDiagnosticParser.Classify(
            EquipmentSidecarOutputStream.StandardError,
            "HSMS Reject; state=Selected; frameLength=10.");

        Assert.False(result.IsError);
        Assert.Equal(SecsDiagnosticKind.Reject, result.DiagnosticKind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("HSMS")]
    [InlineData("HSMS Unknown; state=Selected; frameLength=10.")]
    [InlineData("DREAMINE_EQUIPMENT_TELEMETRY {not-json}")]
    public void MalformedOrDifferentEnvelopeIsSafe(string? line)
    {
        var exception = Record.Exception(() => EquipmentSidecarDiagnosticParser.Classify(
            EquipmentSidecarOutputStream.StandardError,
            line));
        var result = EquipmentSidecarDiagnosticParser.Classify(
            EquipmentSidecarOutputStream.StandardError,
            line);

        Assert.Null(exception);
        Assert.False(result.IsStructuredHsms);
        Assert.True(result.IsError);
        Assert.Null(result.Connection);
    }

    [Fact]
    public void UnstructuredStandardOutputRemainsInformational()
    {
        var result = EquipmentSidecarDiagnosticParser.Classify(
            EquipmentSidecarOutputStream.StandardOutput,
            "equipment sidecar ready");

        Assert.False(result.IsError);
        Assert.False(result.IsStructuredHsms);
    }
}
