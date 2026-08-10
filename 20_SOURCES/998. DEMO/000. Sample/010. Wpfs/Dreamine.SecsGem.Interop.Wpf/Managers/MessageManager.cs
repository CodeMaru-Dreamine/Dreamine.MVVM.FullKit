using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Protocol;
using Dreamine.Gem.StateMachines;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

public sealed class MessageManager
{
    private readonly ConnectionManager _connection;
    private readonly InteropLogManager _log;
    private readonly object _responderGate = new();
    private GemProtocolEngine? _gemEngine;
    private HsmsSession? _responderSession;
    private bool _equipmentResponderEnabled;

    public MessageManager(ConnectionManager connection, InteropLogManager log)
    {
        _connection = connection; _log = log;
        _connection.MessageReceived += OnMessageReceived;
        _connection.SessionChanged += OnSessionChanged;
    }

    public void EnableEquipmentResponder()
    {
        var session = _connection.RequireSession();
        lock (_responderGate) _equipmentResponderEnabled = true;
        BindEquipmentResponder(session);
        _log.Info("GEM", "Harness equipment responder enabled for the documented basic S1 subset.");
    }

    private void BindEquipmentResponder(HsmsSession session)
    {
        var communication = new GemCommunicationStateMachine();
        communication.Enable(equipmentInitiated: false);
        var engine = new GemProtocolEngine(new HsmsTransportAdapter(session, _log), communication,
            new GemEquipmentIdentity("DREAMINE-INTEROP", "0.1"));
        lock (_responderGate)
        {
            if (!_equipmentResponderEnabled) return;
            _responderSession = session;
            _gemEngine = engine;
        }
    }

    public async Task<SecsMessage?> SendAsync(byte stream, byte function, bool replyExpected, SecsItem? item, CancellationToken token)
    {
        var session = _connection.RequireSession();
        var message = new SecsMessage(new(0), new(stream), new(function), replyExpected,
            session.AllocateSystemBytes(), item);
        _log.Message("TX", message);
        if (!replyExpected) { await session.SendAsync(message, token).ConfigureAwait(false); return null; }
        var response = await session.SendPrimaryAsync(message, token).ConfigureAwait(false);
        _log.Message("RX", response);
        return response;
    }

    private void OnMessageReceived(object? sender, SecsMessage message)
    {
        if (!message.Function.IsPrimary || !message.ReplyExpected) return;
        if (sender is not HsmsSession sourceSession)
        {
            _log.Error("Responder", new InvalidOperationException("The received primary did not identify its source HSMS session."));
            return;
        }
        _ = RespondAsync(sourceSession, message);
    }

    private async Task RespondAsync(HsmsSession sourceSession, SecsMessage message)
    {
        try
        {
            GemProtocolEngine? engine;
            lock (_responderGate)
                engine = ReferenceEquals(_responderSession, sourceSession) ? _gemEngine : null;
            if (engine is not null && await engine.HandleAsync(message).ConfigureAwait(false)) return;
            var item = message.Stream.Value == 1 ? message.Function.Value switch
            {
                3 => BuildS1F4(message.Item),
                11 => BuildS1F12(),
                15 or 17 => new SecsBinaryItem(0),
                _ => null
            } : null;
            if (item is null) return;
            var response = new SecsMessage(message.SessionId, message.Stream, new((byte)(message.Function.Value + 1)),
                false, message.SystemBytes, item);
            _log.Message("TX", response);
            await sourceSession.SendAsync(response).ConfigureAwait(false);
        }
        catch (Exception exception) { _log.Error("Responder", exception); }
    }

    private void OnSessionChanged(HsmsSession? session)
    {
        bool enabled;
        lock (_responderGate)
        {
            enabled = _equipmentResponderEnabled;
            _responderSession = null;
            _gemEngine = null;
        }
        if (!enabled || session is null) return;
        BindEquipmentResponder(session);
        _log.Info("GEM", "Equipment responder rebound to the replacement HSMS session.");
    }

    private static SecsItem BuildS1F4(SecsItem? request)
    {
        var requested = request is SecsListItem list && list.Count > 0 ? list.Count : 2;
        return new SecsListItem(Enumerable.Range(0, requested).Select(index => (SecsItem)new SecsAsciiItem(index == 0 ? "COMMUNICATING" : "ONLINE")).ToArray());
    }

    private static SecsItem BuildS1F12() => new SecsListItem(
        new SecsListItem(new SecsInt16Item(1), new SecsAsciiItem("CommunicationState"), new SecsAsciiItem(string.Empty)),
        new SecsListItem(new SecsInt16Item(2), new SecsAsciiItem("ControlState"), new SecsAsciiItem(string.Empty)));

    private sealed class HsmsTransportAdapter(HsmsSession session, InteropLogManager log) : IGemMessageTransport
    {
        public ISecsConnection Connection => session;
        public SecsSessionId SessionId => new(0);
        public event EventHandler<SecsMessage>? MessageReceived { add => session.MessageReceived += value; remove => session.MessageReceived -= value; }
        public Task SendAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            log.Message("TX", message);
            return session.SendAsync(message, cancellationToken);
        }
        public Task<SecsMessage> RequestAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            log.Message("TX", message);
            return session.SendPrimaryAsync(message, cancellationToken);
        }
        public SecsSystemBytes AllocateSystemBytes() => session.AllocateSystemBytes();
    }
}
