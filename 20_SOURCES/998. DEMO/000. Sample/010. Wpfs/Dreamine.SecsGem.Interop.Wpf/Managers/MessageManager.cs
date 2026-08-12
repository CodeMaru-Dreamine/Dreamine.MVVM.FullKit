using System.Globalization;
using Dreamine.Gem.Demo;
using Dreamine.Gem.Protocol.E30;
using Dreamine.Gem.Transport;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

public sealed class MessageManager : IDisposable
{
    private static readonly SecsDialogueDefinition[] EducationalResponderDialogues =
    [
        Dialogue(1, 1),
        Dialogue(1, 3),
        Dialogue(1, 11),
        Dialogue(1, 13),
        Dialogue(1, 15),
        Dialogue(1, 17),
        Dialogue(2, 17)
    ];

    private readonly ConnectionManager _connection;
    private readonly InteropLogManager _log;
    private readonly object _responderGate = new();
    private IDisposable[] _responderRegistrations = [];
    private E30EquipmentRouter? _e30Router;
    private ISecsMessageSession? _responderSession;
    private bool _equipmentResponderEnabled;
    private EquipmentResponderProfileKind _equipmentResponderProfile = EquipmentResponderProfileKind.EducationalDemoOnly;
    private int _bindingGeneration;
    private long _communicatingEpoch;
    private int _disposed;

    public MessageManager(ConnectionManager connection, InteropLogManager log)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _connection.MessageSessionChanged += OnMessageSessionChanged;
    }

    public EquipmentResponderProfileKind? ActiveEquipmentResponderProfile
    {
        get
        {
            lock (_responderGate)
                return _equipmentResponderEnabled && _responderSession is not null
                    ? _equipmentResponderProfile
                    : null;
        }
    }

    public void EnableEquipmentResponder(
        EquipmentResponderProfileKind profile = EquipmentResponderProfileKind.EducationalDemoOnly)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (!Enum.IsDefined(profile)) throw new ArgumentOutOfRangeException(nameof(profile));
        var session = _connection.RequireMessageSession();
        EnsureEquipmentRole(session);
        lock (_responderGate)
        {
            _equipmentResponderEnabled = true;
            _equipmentResponderProfile = profile;
        }
        try { RebindEquipmentResponder(session, throwOnFailure: true); }
        catch
        {
            (IDisposable[] Registrations, E30EquipmentRouter? Router) binding;
            lock (_responderGate)
            {
                _equipmentResponderEnabled = false;
                _bindingGeneration++;
                binding = DetachResponderLocked();
            }
            DisposeBinding(binding);
            throw;
        }
        _log.Info("GEM", profile == EquipmentResponderProfileKind.E30DerivedSubsetDemo
            ? $"Equipment responder enabled with public Demo profile '{E30DerivedSubsetManifest.ProfileName}'."
            : "Educational Demo-only exact S1/S2 responder enabled; it is not a GEM profile.");
    }

    public void DisableEquipmentResponder()
    {
        (IDisposable[] Registrations, E30EquipmentRouter? Router) binding;
        lock (_responderGate)
        {
            _equipmentResponderEnabled = false;
            _bindingGeneration++;
            binding = DetachResponderLocked();
        }
        DisposeBinding(binding);
        _log.Info("GEM", "Workbench Demo equipment responder disabled.");
    }

    public async Task<SecsMessage?> SendAsync(
        byte stream,
        byte function,
        bool replyExpected,
        SecsItem? item,
        CancellationToken token)
    {
        var session = _connection.RequireMessageSession();
        var primaryStream = new SecsStream(stream);
        var primaryFunction = new SecsFunction(function);
        if (!replyExpected)
        {
            // EN: The high-level W0 API owns Session ID and System Bytes allocation.
            // KO: 고수준 W0 API가 Session ID와 System Bytes 할당을 소유한다.
            await session.SendAsync(primaryStream, primaryFunction, item, token).ConfigureAwait(false);
            _log.Info("SECS-II", $"TX S{stream}F{function} submitted through the safe W0 session API.");
            return null;
        }

        var dialogue = new SecsDialogueDefinition(
            primaryStream,
            primaryFunction,
            new SecsFunction(checked((byte)(function + 1))));
        // EN: RequestAsync validates the declared adjacent Secondary and correlates the reply.
        // KO: RequestAsync가 선언된 인접 Secondary를 검증하고 응답을 correlation한다.
        var response = await session.RequestAsync(dialogue, item, token).ConfigureAwait(false);
        _log.Message("RX", response);
        return response;
    }

    internal static SecsItem? BuildBasicResponseItem(SecsMessage message)
    {
        // EN: This switch is deliberately a small educational responder, not a customer profile.
        // KO: 이 switch는 고객사 profile이 아니라 의도적으로 작게 유지한 학습용 responder다.
        if (!message.Function.IsPrimary || !message.ReplyExpected) return null;

        return (message.Stream.Value, message.Function.Value) switch
        {
            (1, 1) => BuildIdentityList(),
            (1, 3) => BuildS1F4(message.Item),
            (1, 11) => BuildS1F12(),
            (1, 13) => BuildS1F14(),
            (1, 15) or (1, 17) => BuildBinaryAcknowledge(0),
            (2, 17) => BuildS2F18(),
            _ => null
        };
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _connection.MessageSessionChanged -= OnMessageSessionChanged;
        (IDisposable[] Registrations, E30EquipmentRouter? Router) binding;
        lock (_responderGate)
        {
            _equipmentResponderEnabled = false;
            _bindingGeneration++;
            binding = DetachResponderLocked();
        }
        DisposeBinding(binding);
    }

    private void OnMessageSessionChanged(ISecsMessageSession? session)
    {
        bool enabled;
        lock (_responderGate) enabled = _equipmentResponderEnabled;
        if (!enabled)
        {
            RebindEquipmentResponder(null, throwOnFailure: false);
            return;
        }

        try
        {
            if (session is not null) EnsureEquipmentRole(session);
            RebindEquipmentResponder(session, throwOnFailure: false);
            if (session is not null)
                _log.Info("GEM", "Equipment responder rebound to the replacement message session.");
        }
        catch (Exception exception)
        {
            _log.Error("Responder", exception);
        }
    }

    private void RebindEquipmentResponder(ISecsMessageSession? session, bool throwOnFailure)
    {
        (IDisposable[] Registrations, E30EquipmentRouter? Router) previous;
        int generation;
        EquipmentResponderProfileKind profile;
        lock (_responderGate)
        {
            generation = ++_bindingGeneration;
            profile = _equipmentResponderProfile;
            previous = DetachResponderLocked();
        }
        DisposeBinding(previous);
        if (session is null) return;

        var created = new List<IDisposable>(EducationalResponderDialogues.Length);
        E30EquipmentRouter? createdRouter = null;
        try
        {
            if (profile == EquipmentResponderProfileKind.E30DerivedSubsetDemo)
            {
                var demoProfile = E30DemoEquipmentProfile.Create();
                var context = demoProfile.CreateContext(new HsmsGemTransport(session));
                createdRouter = new E30EquipmentRouter(session, context, new E30EquipmentRouterOptions
                {
                    CommandCompletionEvents = new Dictionary<string, ulong>(StringComparer.Ordinal)
                    {
                        [E30DemoEquipmentProfile.StartCommand] = E30DemoEquipmentProfile.CommandCompletedEventId
                    }
                });
            }
            else
            {
                foreach (var dialogue in EducationalResponderDialogues)
                {
                    created.Add(session.PrimaryDispatcher.Register(
                        dialogue,
                        (context, token) => HandleEducationalPrimaryAsync(session, context, token)));
                }
            }

            (IDisposable[] Registrations, E30EquipmentRouter? Router)? discard = null;
            lock (_responderGate)
            {
                if (!_equipmentResponderEnabled || generation != _bindingGeneration ||
                    Volatile.Read(ref _disposed) != 0)
                {
                    discard = (created.ToArray(), createdRouter);
                }
                else
                {
                    _responderSession = session;
                    _responderRegistrations = created.ToArray();
                    _e30Router = createdRouter;
                    Volatile.Write(ref _communicatingEpoch, 0);
                }
            }
            if (discard is { } value) DisposeBinding(value);
        }
        catch (Exception exception)
        {
            DisposeBinding((created.ToArray(), createdRouter));
            if (throwOnFailure) throw;
            _log.Error("Responder", exception);
        }
    }

    private async ValueTask HandleEducationalPrimaryAsync(
        ISecsMessageSession sourceSession,
        ISecsPrimaryContext context,
        CancellationToken cancellationToken)
    {
        if (!IsCurrentResponder(sourceSession, context.ConnectionIdentity) || !context.CanReply) return;

        var primary = context.Primary;
        SecsItem? body;
        if (primary.Stream.Value == 1 && primary.Function.Value == 13)
        {
            Volatile.Write(ref _communicatingEpoch, context.ConnectionIdentity.ConnectionEpoch);
            body = BuildS1F14();
        }
        else if (primary.Stream.Value == 1 && primary.Function.Value == 1)
        {
            if (Volatile.Read(ref _communicatingEpoch) != context.ConnectionIdentity.ConnectionEpoch) return;
            body = BuildIdentityList();
        }
        else
        {
            body = BuildBasicResponseItem(primary);
        }

        if (body is null) return;
        try
        {
            await context.ReplyAsync(body, cancellationToken).ConfigureAwait(false);
            var response = new SecsMessage(
                primary.SessionId,
                primary.Stream,
                new SecsFunction((byte)(primary.Function.Value + 1)),
                false,
                primary.SystemBytes,
                body);
            _log.Message("TX", response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            // EN: The dispatcher awaits this handler; logging here keeps failures observed and bounded.
            // KO: dispatcher가 이 handler를 await하므로 여기서 기록된 실패는 관찰되며 bounded하다.
            _log.Error("Responder", exception);
        }
    }

    private bool IsCurrentResponder(ISecsMessageSession sourceSession, SecsConnectionIdentity contextIdentity)
    {
        lock (_responderGate)
        {
            return _equipmentResponderEnabled &&
                ReferenceEquals(_responderSession, sourceSession) &&
                contextIdentity == sourceSession.ConnectionIdentity;
        }
    }

    private (IDisposable[] Registrations, E30EquipmentRouter? Router) DetachResponderLocked()
    {
        var registrations = _responderRegistrations;
        var router = _e30Router;
        _responderRegistrations = [];
        _e30Router = null;
        _responderSession = null;
        Volatile.Write(ref _communicatingEpoch, 0);
        return (registrations, router);
    }

    private void DisposeBinding((IDisposable[] Registrations, E30EquipmentRouter? Router) binding)
    {
        DisposeRegistrations(binding.Registrations);
        if (binding.Router is null) return;
        try { binding.Router.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
        catch (Exception exception) { _log.Error("Responder", exception); }
    }

    private void DisposeRegistrations(IEnumerable<IDisposable> registrations)
    {
        foreach (var registration in registrations)
        {
            try { registration.Dispose(); }
            catch (Exception exception) { _log.Error("Responder", exception); }
        }
    }

    private static void EnsureEquipmentRole(ISecsMessageSession session)
    {
        if (session.ConnectionIdentity.Role != SecsRole.Equipment)
            throw new InvalidOperationException("The built-in responder can only bind to an Equipment-role session.");
    }

    private static SecsDialogueDefinition Dialogue(byte stream, byte primaryFunction) => new(
        new SecsStream(stream),
        new SecsFunction(primaryFunction),
        new SecsFunction((byte)(primaryFunction + 1)));

    private static SecsItem BuildS1F4(SecsItem? request)
    {
        // EN: Add typed children to a dynamic L[n], then freeze them into the immutable list item.
        // KO: 동적 L[n]에 typed 자식을 추가한 뒤 immutable list item으로 고정한다.
        if (request is not SecsListItem { Count: > 0 } list)
            return new SecsListItem(new SecsAsciiItem("COMMUNICATING"), new SecsAsciiItem("ONLINE"));

        var values = new List<SecsItem>(list.Count);
        foreach (var requestedId in list.Items) values.Add(BuildStatusValue(requestedId));
        return new SecsListItem(values.ToArray());
    }

    private static SecsItem BuildStatusValue(SecsItem requestedId)
    {
        // EN: Values.Span reads a typed atomic value without converting the SECS item to text.
        // KO: Values.Span으로 SECS item을 문자열로 바꾸지 않고 typed 원자 값을 읽는다.
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
        // EN: Nested SecsListItem children create an L[2] containing two L[3] rows.
        // KO: SecsListItem 자식을 중첩하면 두 L[3] 행을 포함한 L[2]가 된다.
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

    private static SecsItem BuildS1F14() => new SecsListItem(
        BuildBinaryAcknowledge(0),
        BuildIdentityList());

    private static SecsItem BuildIdentityList() => new SecsListItem(
        new SecsAsciiItem("DREAMINE-INTEROP-DEMO"),
        new SecsAsciiItem("0.1-DEMO"));

    private static SecsItem BuildS2F18() =>
        new SecsAsciiItem(DateTime.Now.ToString("yyyyMMddHHmmssff", CultureInfo.InvariantCulture));

    private static SecsItem BuildBinaryAcknowledge(byte code) => new SecsBinaryItem(code);
}
