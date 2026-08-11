using System.Globalization;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

/// <summary>
/// Parses the bounded, customer-neutral diagnostic envelope emitted by an
/// equipment sidecar. Stream selection is deliberately not treated as log
/// severity because console applications commonly reserve stdout for machine
/// readable telemetry and write informational diagnostics to stderr.
/// </summary>
internal static class EquipmentSidecarDiagnosticParser
{
    internal const string Prefix = "HSMS ";

    public static EquipmentSidecarDiagnosticClassification Classify(
        EquipmentSidecarOutputStream stream,
        string? line)
    {
        var message = line?.Trim() ?? string.Empty;
        if (TryParse(message, out var diagnostic) && diagnostic is not null)
            return diagnostic;

        // Preserve the conventional meaning of stderr for unstructured process
        // output. Only a successfully parsed HSMS envelope may override it.
        return new EquipmentSidecarDiagnosticClassification(
            message,
            stream == EquipmentSidecarOutputStream.StandardError
                ? EquipmentSidecarDiagnosticSeverity.Error
                : EquipmentSidecarDiagnosticSeverity.Information,
            null,
            null,
            null);
    }

    public static bool TryParse(
        string? line,
        out EquipmentSidecarDiagnosticClassification? diagnostic)
    {
        diagnostic = null;
        if (string.IsNullOrWhiteSpace(line)) return false;

        var message = line.Trim();
        if (!message.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) return false;

        try
        {
            var segments = message.Split(';', StringSplitOptions.TrimEntries);
            if (segments.Length == 0) return false;

            var kindText = segments[0][Prefix.Length..].Trim().TrimEnd('.');
            if (!Enum.TryParse<SecsDiagnosticKind>(kindText, ignoreCase: true, out var kind) ||
                !Enum.IsDefined(kind))
            {
                return false;
            }

            HsmsConnectionState? hsmsState = null;
            int? frameLength = null;
            foreach (var segment in segments.Skip(1))
            {
                var separator = segment.IndexOf('=');
                if (separator <= 0) continue;

                var key = segment[..separator].Trim();
                var value = segment[(separator + 1)..].Trim().TrimEnd('.');
                if (key.Equals("state", StringComparison.OrdinalIgnoreCase))
                {
                    if (!value.Equals("n/a", StringComparison.OrdinalIgnoreCase) &&
                        Enum.TryParse<HsmsConnectionState>(value, ignoreCase: true, out var parsedState) &&
                        Enum.IsDefined(parsedState))
                    {
                        hsmsState = parsedState;
                    }
                }
                else if (key.Equals("frameLength", StringComparison.OrdinalIgnoreCase) &&
                         int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedLength) &&
                         parsedLength >= 0)
                {
                    frameLength = parsedLength;
                }
            }

            diagnostic = new EquipmentSidecarDiagnosticClassification(
                message,
                IsFault(kind)
                    ? EquipmentSidecarDiagnosticSeverity.Error
                    : EquipmentSidecarDiagnosticSeverity.Information,
                kind,
                frameLength,
                ProjectConnection(kind, hsmsState));
            return true;
        }
        catch (Exception)
        {
            // Sidecar output crosses a process boundary. Malformed diagnostics
            // must never terminate the redirected-output pump or the WPF UI.
            return false;
        }
    }

    private static bool IsFault(SecsDiagnosticKind kind) => kind is
        SecsDiagnosticKind.Timeout or
        SecsDiagnosticKind.ProtocolError or
        SecsDiagnosticKind.ApplicationError;

    private static EquipmentSidecarConnectionProjection? ProjectConnection(
        SecsDiagnosticKind kind,
        HsmsConnectionState? state)
    {
        if (kind == SecsDiagnosticKind.ConnectionClosed)
        {
            return new(
                EquipmentSidecarTcpStatus.Disconnected,
                EquipmentSidecarHsmsStatus.NotConnected);
        }

        if (kind == SecsDiagnosticKind.ConnectionAttempt && state is null or HsmsConnectionState.NotConnected)
        {
            return new(
                EquipmentSidecarTcpStatus.Listening,
                EquipmentSidecarHsmsStatus.NotConnected);
        }

        if (kind == SecsDiagnosticKind.ConnectionEstablished && state is null)
        {
            return new(
                EquipmentSidecarTcpStatus.Connected,
                EquipmentSidecarHsmsStatus.ConnectedNotSelected);
        }

        return state switch
        {
            HsmsConnectionState.NotConnected => new(
                EquipmentSidecarTcpStatus.Disconnected,
                EquipmentSidecarHsmsStatus.NotConnected),
            HsmsConnectionState.ConnectedNotSelected => new(
                EquipmentSidecarTcpStatus.Connected,
                EquipmentSidecarHsmsStatus.ConnectedNotSelected),
            HsmsConnectionState.Selected => new(
                EquipmentSidecarTcpStatus.Connected,
                EquipmentSidecarHsmsStatus.Selected),
            _ => null
        };
    }
}
