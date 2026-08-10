using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

public sealed class ConnectionManager(InteropLogManager log) : IAsyncDisposable
{
    private HsmsSession? _session;
    private ConnectionSettings? _lastSettings;
    public HsmsSession? Session => _session;
    public string TcpState => _session?.State.ToString() ?? "Disconnected";
    public string HsmsState => _session?.HsmsState.ToString() ?? "NotConnected";
    public event EventHandler? StateChanged;
    public event Action<HsmsSession?>? SessionChanged;
    public event EventHandler<SecsMessage>? MessageReceived;

    public async Task ConnectAsync(ConnectionSettings settings, CancellationToken token)
    {
        if (_session is not null) await DisconnectAsync(token).ConfigureAwait(false);
        var session = new HsmsSession(settings.ToOptions(), diagnostics: new InteropDiagnosticSink(log, _ => RaiseStateChanged()));
        _lastSettings = settings;
        session.MessageReceived += OnMessageReceived;
        _session = session;
        SessionChanged?.Invoke(session);
        RaiseStateChanged();
        try { await session.ConnectAsync(token).ConfigureAwait(false); }
        finally { RaiseStateChanged(); }
    }

    public async Task DisconnectAsync(CancellationToken token)
    {
        var session = _session;
        if (session is null) return;
        session.MessageReceived -= OnMessageReceived;
        _session = null;
        SessionChanged?.Invoke(null);
        await session.DisposeAsync().ConfigureAwait(false);
        RaiseStateChanged();
    }

    public async Task SelectAsync(CancellationToken token)
    {
        await RequireSession().SelectAsync(token).ConfigureAwait(false);
        RaiseStateChanged();
    }
    public Task LinktestAsync(CancellationToken token) => RequireSession().LinktestAsync(token);
    public async Task SeparateAsync(CancellationToken token)
    {
        var session = RequireSession();
        try { await session.SeparateAsync(token).ConfigureAwait(false); }
        finally
        {
            session.MessageReceived -= OnMessageReceived;
            if (ReferenceEquals(Interlocked.CompareExchange(ref _session, null, session), session))
                SessionChanged?.Invoke(null);
            await session.DisposeAsync().ConfigureAwait(false);
            RaiseStateChanged();
        }
    }
    public async Task ReconnectAsync(CancellationToken token)
    {
        var settings = _lastSettings ?? throw new InvalidOperationException("No previous connection settings are available.");
        await DisconnectAsync(token).ConfigureAwait(false);
        await ConnectAsync(settings, token).ConfigureAwait(false);
        await SelectAsync(token).ConfigureAwait(false);
    }
    public HsmsSession RequireSession() => _session ?? throw new InvalidOperationException("Connect an HSMS session first.");
    public async ValueTask DisposeAsync()
    {
        var session = Interlocked.Exchange(ref _session, null);
        if (session is null) return;
        session.MessageReceived -= OnMessageReceived;
        SessionChanged?.Invoke(null);
        await session.DisposeAsync().ConfigureAwait(false);
        RaiseStateChanged();
    }

    private void OnMessageReceived(object? sender, SecsMessage message)
    {
        log.Message("RX", message);
        // Preserve the source session as the event sender. During reconnect a delayed
        // primary must never be answered through the replacement session.
        MessageReceived?.Invoke(sender, message);
        RaiseStateChanged();
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
