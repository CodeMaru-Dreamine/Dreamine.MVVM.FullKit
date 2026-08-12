using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.SecsGem.Interop.Runtime.Logging;

namespace Dreamine.SecsGem.Interop.Runtime.Templates;

/// <summary>
/// Maps validated template body-log metadata to Core wire-capture rules that are evaluated before
/// HSMS copies application payload bytes.
/// </summary>
public static class MessageTemplateWireCaptureAdapter
{
    private const int MaximumTemplateRules = 10_000;

    /// <summary>Maps one template to a direction-aware Core wire-capture rule.</summary>
    public static HsmsWireCaptureRule ToHsmsWireCaptureRule(
        MessageTemplateV1 template,
        SecsRole localRole,
        int maximumFullBodyBytes = 64 * 1024)
    {
        var rule = CreateWireBodyCaptureRule(template, localRole, maximumFullBodyBytes);
        return new HsmsWireCaptureRule(
            rule.Stream,
            rule.Function,
            rule.Direction,
            rule.Mode switch
            {
                WireBodyCaptureMode.Excluded => HsmsWireCaptureMode.Excluded,
                WireBodyCaptureMode.HeaderOnly => HsmsWireCaptureMode.HeaderOnly,
                WireBodyCaptureMode.FullBody => HsmsWireCaptureMode.FullFrame,
                _ => throw new InvalidOperationException($"Unsupported capture mode {rule.Mode}.")
            },
            rule.Mode == WireBodyCaptureMode.FullBody
                ? checked(WireLogPolicy.HsmsPrefixAndHeaderLength + rule.MaximumBodyBytes)
                : 0);
    }

    /// <summary>
    /// Creates bounded Core observation options with a fail-safe HeaderOnly default and explicit per-template
    /// rules. FullBodyExplicit retains at most <paramref name="maximumFullBodyBytes"/> body bytes.
    /// </summary>
    public static HsmsWireObservationOptions CreateObservationOptions(
        SecsRole localRole,
        IEnumerable<MessageTemplateV1> templates,
        int queueCapacity = 256,
        int maximumFullBodyBytes = 64 * 1024,
        int maximumDecodedCharacters = 16 * 1024)
    {
        var policy = CreateWireLogPolicy(
            localRole, templates, maximumFullBodyBytes, maximumDecodedCharacters);
        return policy.CreateObservationOptions(queueCapacity);
    }

    internal static WireLogPolicy CreateWireLogPolicy(
        SecsRole localRole,
        IEnumerable<MessageTemplateV1> templates,
        int maximumFullBodyBytes,
        int maximumDecodedCharacters)
    {
        ValidateRoleAndLimit(localRole, maximumFullBodyBytes);
        ArgumentNullException.ThrowIfNull(templates);
        var rules = new List<WireBodyCaptureRule>();
        foreach (var template in templates)
        {
            if (rules.Count == MaximumTemplateRules)
                throw new ArgumentException(
                    $"At most {MaximumTemplateRules} template capture rules are allowed.", nameof(templates));
            rules.Add(CreateWireBodyCaptureRule(template, localRole, maximumFullBodyBytes));
        }
        return new WireLogPolicy(WireBodyCaptureMode.HeaderOnly, rules, maximumDecodedCharacters);
    }

    private static WireBodyCaptureRule CreateWireBodyCaptureRule(
        MessageTemplateV1 template,
        SecsRole localRole,
        int maximumFullBodyBytes)
    {
        ArgumentNullException.ThrowIfNull(template);
        ValidateRoleAndLimit(localRole, maximumFullBodyBytes);
        template.Validate();

        var outbound = (localRole == SecsRole.Host &&
                template.Direction == MessageTemplateDirection.HostToEquipment) ||
            (localRole == SecsRole.Equipment &&
                template.Direction == MessageTemplateDirection.EquipmentToHost);
        var mode = template.BodyLogPolicy switch
        {
            TemplateBodyLogPolicy.HeaderOnly => WireBodyCaptureMode.HeaderOnly,
            TemplateBodyLogPolicy.Excluded => WireBodyCaptureMode.Excluded,
            TemplateBodyLogPolicy.FullBodyExplicit => WireBodyCaptureMode.FullBody,
            _ => throw new TemplateValidationException(
                $"bodyLogPolicy: unsupported value {template.BodyLogPolicy}.")
        };
        return new WireBodyCaptureRule(
            template.Stream,
            template.Function,
            outbound ? HsmsWireDirection.Outbound : HsmsWireDirection.Inbound,
            mode,
            mode == WireBodyCaptureMode.FullBody ? maximumFullBodyBytes : 0);
    }

    private static void ValidateRoleAndLimit(SecsRole localRole, int maximumFullBodyBytes)
    {
        if (localRole is not (SecsRole.Host or SecsRole.Equipment))
            throw new ArgumentOutOfRangeException(nameof(localRole));
        if (maximumFullBodyBytes is < 1 or > WireLogPolicy.MaximumAllowedBodyBytes)
            throw new ArgumentOutOfRangeException(nameof(maximumFullBodyBytes));
    }
}
