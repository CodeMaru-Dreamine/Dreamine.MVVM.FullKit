using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;

namespace PortfolioApp.Services;

/// <summary>
/// Captures connection information while an interactive server circuit is connected.
/// </summary>
public sealed class PortfolioCircuitClientContext(IHttpContextAccessor httpContextAccessor) : CircuitHandler
{
    private string? _remoteIpAddress;

    /// <summary>Gets the last remote address observed for this circuit.</summary>
    public string? RemoteIpAddress => Volatile.Read(ref _remoteIpAddress);

    /// <inheritdoc />
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CaptureRemoteAddress();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CaptureRemoteAddress();
        return Task.CompletedTask;
    }

    private void CaptureRemoteAddress()
    {
        string? address = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(address))
        {
            Volatile.Write(ref _remoteIpAddress, address);
        }
    }
}
