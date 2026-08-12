using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.SecsGem.Interop.Runtime.Templates;

/// <summary>
/// Binds one captured inbound W-bit Primary to an editor-safe copy and its provider-neutral,
/// one-shot <see cref="ISecsPrimaryContext.ReplyAsync"/> capability.
/// </summary>
public sealed class PendingPrimaryReply
{
    private readonly ISecsPrimaryContext _context;
    private int _replyAttempted;

    private PendingPrimaryReply(ISecsPrimaryContext context, MessageTemplateV1 inboundPrimary)
    {
        _context = context;
        SourceIdentity = context.ConnectionIdentity;
        SessionId = context.Primary.SessionId;
        Stream = context.Primary.Stream;
        PrimaryFunction = context.Primary.Function;
        SecondaryFunction = new SecsFunction(checked((byte)(PrimaryFunction.Value + 1)));
        SystemBytes = context.Primary.SystemBytes;
        InboundPrimary = inboundPrimary;
    }

    /// <summary>Gets the immutable provider/session/connection-epoch identity captured with the Primary.</summary>
    public SecsConnectionIdentity SourceIdentity { get; }

    /// <summary>Gets the actual inbound message Session ID.</summary>
    public SecsSessionId SessionId { get; }

    /// <summary>Gets the registered dialogue Stream.</summary>
    public SecsStream Stream { get; }

    /// <summary>Gets the inbound Primary function.</summary>
    public SecsFunction PrimaryFunction { get; }

    /// <summary>Gets the adjacent normal Secondary function guaranteed by the reply-capable Gate2 context.</summary>
    public SecsFunction SecondaryFunction { get; }

    /// <summary>Gets the inbound Primary System Bytes retained by the Gate2 reply context.</summary>
    public SecsSystemBytes SystemBytes { get; }

    /// <summary>Gets an independent editor copy of the inbound Primary; it never owns reply correlation.</summary>
    public MessageTemplateV1 InboundPrimary { get; }

    /// <summary>Gets whether a validated reply has already been delegated, regardless of its outcome.</summary>
    public bool ReplyAttempted => Volatile.Read(ref _replyAttempted) != 0;

    /// <summary>
    /// Captures a reply-capable inbound Primary. The context remains the sole authority for timeout,
    /// reconnect, disposal, cancellation, and transaction ownership checks.
    /// </summary>
    public static PendingPrimaryReply Capture(
        ISecsPrimaryContext context,
        string? inboundTemplateName = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        var identity = context.ConnectionIdentity ??
            throw new ArgumentException("The Primary context has no connection identity.", nameof(context));
        var primary = context.Primary ??
            throw new ArgumentException("The Primary context has no message.", nameof(context));

        if (identity.ConnectionEpoch <= 0)
            throw new ArgumentException("An inbound Primary requires a positive connection epoch.", nameof(context));
        if (identity.SessionId != primary.SessionId)
            throw new ArgumentException("The context identity and inbound Primary Session IDs do not match.", nameof(context));
        if (!primary.Function.IsPrimary || primary.Stream.Value == 0)
            throw new ArgumentException("The context message is not a normal SECS-II Primary.", nameof(context));
        if (!primary.ReplyExpected || !context.CanReply || primary.Function.Value == byte.MaxValue)
            throw new InvalidOperationException(
                "The context does not own a registered W1 dialogue with an adjacent normal Secondary.");

        var name = string.IsNullOrWhiteSpace(inboundTemplateName)
            ? $"Inbound S{primary.Stream.Value}F{primary.Function.Value}"
            : inboundTemplateName;
        var direction = identity.Role == SecsRole.Host
            ? MessageTemplateDirection.EquipmentToHost
            : MessageTemplateDirection.HostToEquipment;
        var inbound = MessageTemplateV1.FromReceivedMessage(name, direction, primary);
        return new PendingPrimaryReply(context, inbound);
    }

    /// <summary>Creates a metadata-correct, empty-body Secondary draft for independent editing.</summary>
    public MessageTemplateV1 CreateSecondaryDraft(string? name = null) => new()
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? $"Reply S{Stream.Value}F{SecondaryFunction.Value}"
            : name,
        Description = $"Manual reply to S{Stream.Value}F{PrimaryFunction.Value}.",
        Stream = Stream.Value,
        Function = SecondaryFunction.Value,
        WaitBit = false,
        Kind = MessageTemplateKind.Secondary,
        Direction = SourceIdentity.Role == SecsRole.Host
            ? MessageTemplateDirection.HostToEquipment
            : MessageTemplateDirection.EquipmentToHost,
        BodyLogPolicy = TemplateBodyLogPolicy.HeaderOnly,
        Root = null
    };

    /// <summary>
    /// Validates the edited Secondary against the captured dialogue and delegates it once. Invalid editor
    /// input does not consume ownership. Once delegation begins, success, failure, cancellation, or a stale
    /// Gate2 rejection permanently consumes this binding.
    /// </summary>
    public async ValueTask ReplyAsync(
        MessageTemplateV1 secondaryTemplate,
        CancellationToken cancellationToken = default,
        MessageTemplateLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(secondaryTemplate);
        ValidateSecondaryMetadata(secondaryTemplate);
        var item = secondaryTemplate.BuildItem(limits);

        if (Interlocked.CompareExchange(ref _replyAttempted, 1, 0) != 0)
            throw new InvalidOperationException("This captured Primary reply has already been attempted.");

        await _context.ReplyAsync(item, cancellationToken).ConfigureAwait(false);
    }

    private void ValidateSecondaryMetadata(MessageTemplateV1 template)
    {
        var expectedDirection = SourceIdentity.Role == SecsRole.Host
            ? MessageTemplateDirection.HostToEquipment
            : MessageTemplateDirection.EquipmentToHost;
        if (template.Kind != MessageTemplateKind.Secondary || template.WaitBit ||
            template.Stream != Stream.Value || template.Function != SecondaryFunction.Value ||
            template.Direction != expectedDirection)
        {
            throw new TemplateValidationException(
                $"reply: expected a {expectedDirection} Secondary S{Stream.Value}F{SecondaryFunction.Value} with W=false.");
        }
    }
}
