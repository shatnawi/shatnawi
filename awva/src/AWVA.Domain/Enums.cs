namespace AWVA.Domain;

/// <summary>
/// Scan profile. Passive tools may run under either profile; Active tools
/// (ZAP active scan, sqlmap, auth/access-control testers, error prober, ...)
/// are refused by the scope guard unless the profile is LabActive AND the
/// resolved target IP sits on the lab Docker network. See Section 6 of the
/// project proposal ("Safety, Ethics, and Security of the Agent").
/// </summary>
public enum ScanProfile
{
    Passive = 0,
    LabActive = 1
}

/// <summary>Whether a tool performs read-only reconnaissance or sends adversarial input.</summary>
public enum ToolKind
{
    Passive = 0,
    Active = 1
}

public enum ScanStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Aborted = 4
}

public enum ToolCallStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2,
    Refused = 3
}

public enum Severity
{
    Info = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>Where a finding's final severity came from: the deterministic rule table, or an LLM adjustment with a stated justification.</summary>
public enum SeveritySource
{
    Rule = 0,
    LlmAdjusted = 1
}

public enum Confidence
{
    Low = 0,
    Medium = 1,
    High = 2
}

/// <summary>OWASP WSTG coverage classification reported per category (Section 5.2).</summary>
public enum WstgCoverage
{
    NotCovered = 0,
    Assisted = 1,
    Automated = 2,
    Partial = 3
}

/// <summary>Who/what performed an audited action, for the audit log (Section 6: "Audit log of every prompt, tool call, argument, and refusal").</summary>
public enum ActorType
{
    System = 0,
    Llm = 1,
    User = 2
}
