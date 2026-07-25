using DreamineWeb.KnowledgeQa.Application;
using DreamineWeb.KnowledgeQa.Domain;
using System.Text.RegularExpressions;

namespace DreamineWeb.KnowledgeQa.Infrastructure;

/// <summary>Applies conservative publication checks without altering stored evidence.</summary>
public sealed partial class KnowledgePrivacyScanner : IKnowledgePrivacyScanner
{
    /// <inheritdoc />
    public IReadOnlyList<string> Scan(
        string question,
        EvidenceBundle bundle,
        KnowledgeAnswerContent answer)
    {
        string content = string.Join('\n',
            question,
            answer.Summary,
            string.Join('\n', answer.Sections.Select(item => item.Body)),
            string.Join('\n', bundle.Evidence.Select(item => item.CodeExcerpt ?? string.Empty)));
        List<string> findings = [];
        if (WindowsAbsolutePathRegex().IsMatch(content)) findings.Add("absolute-local-path");
        if (SecretAssignmentRegex().IsMatch(content)) findings.Add("possible-secret-literal");
        if (BearerTokenRegex().IsMatch(content)) findings.Add("bearer-token");
        if (PrivateKeyRegex().IsMatch(content)) findings.Add("private-key");
        return findings.Distinct(StringComparer.Ordinal).ToArray();
    }

    [GeneratedRegex(@"(?i)\b[A-Z]:\\(?:Users|Work|Projects|Source|src)\\")]
    private static partial Regex WindowsAbsolutePathRegex();

    [GeneratedRegex("""(?i)\b(password|passwd|api[_-]?key|client[_-]?secret|connectionstring)\s*[:=]\s*["'][^"']{6,}["']""")]
    private static partial Regex SecretAssignmentRegex();

    [GeneratedRegex(@"(?i)\bBearer\s+[A-Za-z0-9._~+/=-]{16,}")]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex(@"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----")]
    private static partial Regex PrivateKeyRegex();
}
