using System.Globalization;
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
        _log.Info("GEM", "Harness equipment responder enabled for the educational basic S1/S2 subset.");
    }

    public void DisableEquipmentResponder()
    {
        lock (_responderGate)
        {
            _equipmentResponderEnabled = false;
            _responderSession = null;
            _gemEngine = null;
        }
        _log.Info("GEM", "Harness built-in equipment responder disabled.");
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
            {
                // EN: The fallback examples must obey the same enable/session boundary as GEM.
                // KO: 아래 예제 응답도 GEM과 동일하게 활성화 여부와 세션 경계를 지켜야 한다.
                if (!_equipmentResponderEnabled || !ReferenceEquals(_responderSession, sourceSession)) return;
                engine = _gemEngine;
            }
            if (engine is not null && await engine.HandleAsync(message).ConfigureAwait(false)) return;
            var item = BuildBasicResponseItem(message);
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

    internal static SecsItem? BuildBasicResponseItem(SecsMessage message)
    {
        // EN: This tuple switch is intentionally small. Add another (Stream, Function) pair
        //     when extending the sample, then build only the secondary message body here.
        // KO: 이 tuple switch는 학습을 위해 작게 유지한다. 예제를 확장할 때
        //     (Stream, Function) 쌍을 추가하고 여기서는 Secondary 본문만 만든다.
        if (!message.Function.IsPrimary || !message.ReplyExpected) return null;

        return (message.Stream.Value, message.Function.Value) switch
        {
            (1, 3) => BuildS1F4(message.Item),        // Flat list / 평면 목록
            (1, 11) => BuildS1F12(),                 // Nested list / 중첩 목록
            (1, 15) or (1, 17) => BuildBinaryAcknowledge(0),
            (2, 17) => BuildS2F18(),                 // ASCII scalar / ASCII 단일 값
            _ => null
        };
    }

    private static SecsItem BuildS1F4(SecsItem? request)
    {
        // EN: Build a dynamic L[n] by adding typed children, then convert the collection
        //     to an array because SecsListItem is immutable.
        // KO: 형식이 지정된 자식 Item을 컬렉션에 추가해 동적 L[n]을 만든다.
        //     SecsListItem은 불변이므로 마지막에 배열로 변환한다.
        if (request is not SecsListItem { Count: > 0 } list)
            return new SecsListItem(new SecsAsciiItem("COMMUNICATING"), new SecsAsciiItem("ONLINE"));

        var values = new List<SecsItem>(list.Count);
        foreach (var requestedId in list.Items)
            values.Add(BuildStatusValue(requestedId));
        return new SecsListItem(values.ToArray());
    }

    private static SecsItem BuildStatusValue(SecsItem requestedId)
    {
        // EN: Read a typed atomic value with Values.Span, then map it to an application value.
        // KO: Values.Span으로 형식이 지정된 원자 값을 읽고 업무 값으로 매핑한다.
        var id = requestedId switch
        {
            SecsInt16Item signed when signed.Count == 1 => signed.Values.Span[0],
            SecsUInt16Item unsigned when unsigned.Count == 1 => (int)unsigned.Values.Span[0],
            _ => -1
        };
        return new SecsAsciiItem(id switch
        {
            1 => "COMMUNICATING",
            2 => "ONLINE",
            _ => "UNKNOWN"
        });
    }

    private static SecsItem BuildS1F12()
    {
        // EN: Passing SecsListItem instances as children creates L[2] containing two L[3] rows.
        // KO: SecsListItem을 자식으로 전달하면 두 개의 L[3] 행을 가진 L[2]가 된다.
        var communicationState = new SecsListItem(
            new SecsInt16Item(1),
            new SecsAsciiItem("CommunicationState"),
            new SecsAsciiItem(string.Empty));
        var controlState = new SecsListItem(
            new SecsInt16Item(2),
            new SecsAsciiItem("ControlState"),
            new SecsAsciiItem(string.Empty));
        return new SecsListItem(communicationState, controlState);
    }

    private static SecsItem BuildS2F18()
    {
        // EN: A scalar item needs no surrounding list. This sample returns local equipment time.
        // KO: 단일 Item에는 바깥 List가 필요 없다. 이 예제는 Equipment 로컬 시간을 반환한다.
        return new SecsAsciiItem(DateTime.Now.ToString("yyyyMMddHHmmssff", CultureInfo.InvariantCulture));
    }

    private static SecsItem BuildBinaryAcknowledge(byte code)
    {
        // EN: params constructors accept one or more atomic values: B[1] in this example.
        // KO: params 생성자에 원자 값을 하나 이상 전달한다. 이 예제는 B[1]이다.
        return new SecsBinaryItem(code);
    }

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
