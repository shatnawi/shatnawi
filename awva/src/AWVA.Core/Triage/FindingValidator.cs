using AWVA.Core.Planning;
using AWVA.Domain;

namespace AWVA.Core.Triage;

public sealed record ValidationResult(bool IsHallucination, string? Reason);

/// <summary>
/// Implements the hallucination rule from Section 4 step 5 / Section 6: "Any finding whose
/// source_tool_call does not exist, or whose evidence does not appear in that call's output,
/// is rejected as a hallucination and counted in the evaluation." This runs independent of
/// the LLM on every draft finding, whether it came from <see cref="DeterministicPlanner"/>,
/// the real Ollama planner, or <see cref="RuleBasedTriage"/>.
/// </summary>
public static class FindingValidator
{
    public static ValidationResult Validate(DraftFinding draft, IReadOnlyList<ToolCall> history)
    {
        var sourceCall = history.FirstOrDefault(h => string.Equals(h.ExternalCallId, draft.SourceToolCallExternalId, StringComparison.OrdinalIgnoreCase));

        if (sourceCall is null)
        {
            return new ValidationResult(true, $"source_tool_call '{draft.SourceToolCallExternalId}' does not exist in this scan.");
        }

        var rawOutput = sourceCall.RawOutput ?? string.Empty;
        if (!rawOutput.Contains(draft.Evidence, StringComparison.Ordinal))
        {
            return new ValidationResult(true, "Evidence text does not appear verbatim in the cited tool call's raw output.");
        }

        return new ValidationResult(false, null);
    }
}
