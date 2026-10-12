namespace AWVA.Domain;

/// <summary>
/// A target the user submitted for scanning. The scope guard (AWVA.Core.ScopeGuard)
/// is the authority on whether a target is in scope, never the LLM. See Section 4
/// step 1 and Section 6 "Allowlist enforcement in every tool wrapper".
/// </summary>
public class Target
{
    public int Id { get; set; }
    public required string Url { get; set; }
    public required string Host { get; set; }
    public string? ResolvedIp { get; set; }
    public ScanProfile Profile { get; set; }
    /// <summary>True once the scope guard has confirmed host/IP against the allowlist
    /// (and, for LabActive, that the IP sits on the lab Docker network).</summary>
    public bool IsApproved { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Scan> Scans { get; set; } = new();
    public List<GroundTruthItem> GroundTruthItems { get; set; } = new();
}

/// <summary>One run of the agent loop against a single target (Section 4).</summary>
public class Scan
{
    public int Id { get; set; }
    public int TargetId { get; set; }
    public Target? Target { get; set; }
    public ScanProfile Profile { get; set; }
    public ScanStatus Status { get; set; } = ScanStatus.Pending;
    /// <summary>Fixed step budget for the agent loop, e.g. 30 tool calls (Section 4 step 3).</summary>
    public int StepBudget { get; set; } = 30;
    public int StepsUsed { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public List<ToolCall> ToolCalls { get; set; } = new();
    public List<Finding> Findings { get; set; } = new();
    public List<AuditLogEntry> AuditLogEntries { get; set; } = new();
}

/// <summary>
/// A single tool invocation proposed by the LLM and mediated by the scope guard.
/// Raw output is stored unchanged for evidence (Section 4 step 4); normalized output
/// is what goes back to the LLM to save context.
/// </summary>
public class ToolCall
{
    public int Id { get; set; }
    public int ScanId { get; set; }
    public Scan? Scan { get; set; }

    /// <summary>Stable external call id referenced by Finding.SourceToolCall, e.g. "call_07".</summary>
    public required string ExternalCallId { get; set; }
    public required string ToolName { get; set; }
    public ToolKind ToolKind { get; set; }
    public required string ArgumentsJson { get; set; }

    public ToolCallStatus Status { get; set; } = ToolCallStatus.Pending;
    public string? RefusalReason { get; set; }

    /// <summary>Verbatim tool output, kept for evidence and hallucination checks.</summary>
    public string? RawOutput { get; set; }
    /// <summary>Compact version returned to the LLM.</summary>
    public string? NormalizedOutput { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public List<Finding> Findings { get; set; } = new();
}

/// <summary>
/// A triaged finding in the strict schema from Section 4 step 5 / the "Finding schema"
/// table. Any finding whose SourceToolCall does not exist, or whose Evidence does not
/// appear in that call's raw output, is rejected as a hallucination (IsHallucination=true)
/// and counted in the evaluation (Section 9, "Hallucination rate").
/// </summary>
public class Finding
{
    public int Id { get; set; }
    public int ScanId { get; set; }
    public Scan? Scan { get; set; }

    /// <summary>Human-facing finding id, e.g. "HDR-003".</summary>
    public required string ExternalId { get; set; }
    public required string Title { get; set; }
    /// <summary>CWE id, validated against the MITRE CWE list, e.g. "CWE-693".</summary>
    public required string Cwe { get; set; }
    /// <summary>e.g. "A02:2025 Security Misconfiguration (WSTG-CONF-12)".</summary>
    public required string OwaspCategory { get; set; }
    public string? WstgId { get; set; }

    public Severity Severity { get; set; }
    public SeveritySource SeveritySource { get; set; } = SeveritySource.Rule;
    /// <summary>Required whenever SeveritySource == LlmAdjusted.</summary>
    public string? SeverityJustification { get; set; }

    /// <summary>Verbatim tool output substring that supports this finding.</summary>
    public required string Evidence { get; set; }

    public int? SourceToolCallId { get; set; }
    public ToolCall? SourceToolCall { get; set; }

    /// <summary>JSON array of affected URLs, e.g. ["/", "/login"].</summary>
    public required string AffectedUrlsJson { get; set; }
    public required string Remediation { get; set; }
    public Confidence Confidence { get; set; }

    /// <summary>Set by AWVA.Core finding validation; true if SourceToolCall is missing
    /// or Evidence does not appear verbatim in its RawOutput.</summary>
    public bool IsHallucination { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Audit log of every prompt, tool call, argument, and refusal (Section 6).
/// Independent of, and not erasable by, the LLM.
/// </summary>
public class AuditLogEntry
{
    public long Id { get; set; }
    public int? ScanId { get; set; }
    public Scan? Scan { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public ActorType Actor { get; set; }
    public required string Action { get; set; }
    public string? DetailsJson { get; set; }
}

/// <summary>
/// One known issue for a target, written by Student 5 before any scanning, used as the
/// evaluation baseline (Section 9 "Ground truth"). At least three items per OWASP
/// Top 10:2025 category are expected per target.
/// </summary>
public class GroundTruthItem
{
    public int Id { get; set; }
    public int TargetId { get; set; }
    public Target? Target { get; set; }

    public required string ExternalId { get; set; }
    public required string OwaspCategory { get; set; }
    public required string Cwe { get; set; }
    public string? WstgId { get; set; }
    public Severity ExpectedSeverity { get; set; }
    public required string Description { get; set; }
}
