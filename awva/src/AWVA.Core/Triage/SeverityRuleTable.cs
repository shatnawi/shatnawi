using AWVA.Domain;

namespace AWVA.Core.Triage;

/// <summary>
/// The "deterministic rule table" that severity starts from (Section 4 step 5): "Severity
/// starts from a deterministic rule table; the model may adjust it only with a stated
/// justification." Keyed by a check identifier each tool wrapper assigns to what it found
/// (e.g. "missing-header:content-security-policy"); tools consult this when building a
/// DraftFinding so the starting severity is never left to the LLM.
/// </summary>
public static class SeverityRuleTable
{
    private static readonly Dictionary<string, Severity> Rules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["missing-header:content-security-policy"] = Severity.Medium,
        ["missing-header:strict-transport-security"] = Severity.Medium,
        ["missing-header:x-content-type-options"] = Severity.Low,
        ["missing-header:x-frame-options"] = Severity.Low,
        ["cookie:missing-secure"] = Severity.Medium,
        ["cookie:missing-httponly"] = Severity.Medium,
        ["cookie:missing-samesite"] = Severity.Low,
        ["tls:weak-protocol"] = Severity.High,
        ["tls:weak-cipher"] = Severity.Medium,
        ["tls:expired-certificate"] = Severity.High,
        ["open-redirect"] = Severity.Medium,
        ["js-secret-exposed"] = Severity.High,
        ["sqli-confirmed"] = Severity.Critical,
        ["jwt:alg-none"] = Severity.Critical,
        ["jwt:weak-secret"] = Severity.High,
        ["access-control:bola"] = Severity.High,
        ["error:stack-trace-exposed"] = Severity.Low,
    };

    /// <summary>Returns the rule-table severity for a check id, or Medium if the id is unmapped
    /// (a new tool should add a rule rather than rely on this default).</summary>
    public static Severity SeverityFor(string checkId) => Rules.GetValueOrDefault(checkId, Severity.Medium);
}
