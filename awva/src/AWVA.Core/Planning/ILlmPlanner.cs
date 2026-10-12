using AWVA.Domain;

namespace AWVA.Core.Planning;

/// <summary>One step the planner wants the orchestrator to take next.</summary>
public sealed record PlannedToolCall(string ToolName, ToolArguments Arguments);

/// <summary>
/// Abstraction over "the model chooses tools in a logical order" (Section 4 step 2).
/// The real implementation talks to Ollama's OpenAI-compatible /v1/chat/completions
/// endpoint with JSON-schema structured outputs (Section 3). It is intentionally an
/// interface: the orchestrator treats the planner's output as a *proposal* only — every
/// call it returns is re-validated by the scope guard and schema before it ever executes,
/// because "tools detect, the LLM plans and explains" and the LLM is treated as untrusted
/// (Section 6).
/// </summary>
public interface ILlmPlanner
{
    /// <summary>
    /// Given the scan so far (prior tool calls and their normalized outputs), returns the
    /// next tool call to attempt, or null when the planner believes the scan is complete.
    /// </summary>
    Task<PlannedToolCall?> PlanNextStepAsync(Scan scan, IReadOnlyList<ToolCall> history, CancellationToken ct = default);

    /// <summary>
    /// Given completed tool calls, produces draft findings in the strict schema (Section 4
    /// step 5). The orchestrator still runs FindingValidator over every draft before it is
    /// persisted — the planner's own claim that evidence supports a finding is never trusted
    /// on its own.
    /// </summary>
    Task<IReadOnlyList<DraftFinding>> TriageAsync(Scan scan, IReadOnlyList<ToolCall> history, CancellationToken ct = default);
}

/// <summary>A finding as proposed by the planner, before hallucination validation.</summary>
public sealed class DraftFinding
{
    public required string ExternalId { get; init; }
    public required string Title { get; init; }
    public required string Cwe { get; init; }
    public required string OwaspCategory { get; init; }
    public string? WstgId { get; init; }
    public Severity Severity { get; init; }
    public SeveritySource SeveritySource { get; init; } = SeveritySource.Rule;
    public string? SeverityJustification { get; init; }
    public required string Evidence { get; init; }
    public required string SourceToolCallExternalId { get; init; }
    public required IReadOnlyList<string> AffectedUrls { get; init; }
    public required string Remediation { get; init; }
    public Confidence Confidence { get; init; }
}
