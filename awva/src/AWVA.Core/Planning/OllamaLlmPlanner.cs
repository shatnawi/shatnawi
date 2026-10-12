using AWVA.Domain;

namespace AWVA.Core.Planning;

public class OllamaOptions
{
    /// <summary>Ollama's OpenAI-compatible endpoint, e.g. "http://localhost:11434/v1".</summary>
    public string BaseUrl { get; set; } = "http://localhost:11434/v1";
    public string Model { get; set; } = "qwen2.5-coder:7b-instruct";
    public double Temperature { get; set; } = 0;
}

/// <summary>
/// STUB: intended real implementation of <see cref="ILlmPlanner"/> against Ollama's
/// /v1/chat/completions endpoint with JSON-schema structured outputs (Section 3:
/// "Qwen2.5-Coder-7B-Instruct ... supports JSON-schema structured outputs").
///
/// Not wired up to an actual model in this codebase: TODO for the team (Student 2,
/// Section 7) is to:
///   1. Build the tool-schema JSON (one per ISecurityTool, from its argument shape).
///   2. POST to {BaseUrl}/chat/completions with `tools` set to those schemas and
///      `response_format` constraining the triage call to the Finding JSON schema.
///   3. Enforce the retry/step-budget policy from Section 4 step 3 on malformed output.
///   4. Never let the model's own argument values bypass AWVA.Core.ScopeGuard — this
///      class must only ever return *proposals*; AgentOrchestrator remains the place
///      scope is enforced, independent of anything this class returns.
/// </summary>
public sealed class OllamaLlmPlanner : ILlmPlanner
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public OllamaLlmPlanner(HttpClient httpClient, OllamaOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public Task<PlannedToolCall?> PlanNextStepAsync(Scan scan, IReadOnlyList<ToolCall> history, CancellationToken ct = default)
        => throw new NotImplementedException(
            "OllamaLlmPlanner is a stub. Wire this up to POST {BaseUrl}/chat/completions " +
            "with tool schemas once Ollama is running (see class remarks). Use " +
            "DeterministicPlanner for development and for the no-LLM evaluation baseline.");

    public Task<IReadOnlyList<DraftFinding>> TriageAsync(Scan scan, IReadOnlyList<ToolCall> history, CancellationToken ct = default)
        => throw new NotImplementedException(
            "OllamaLlmPlanner is a stub. Wire this up to a structured-output triage call " +
            "constrained to the Finding JSON schema (Section 4 step 5).");
}
