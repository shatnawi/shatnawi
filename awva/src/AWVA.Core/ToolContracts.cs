using AWVA.Domain;

namespace AWVA.Core;

/// <summary>Arguments passed to a tool call, mirroring the Pydantic tool schemas in the proposal.</summary>
public sealed class ToolArguments : Dictionary<string, object?>
{
    public ToolArguments() : base(StringComparer.OrdinalIgnoreCase) { }

    public T? Get<T>(string key, T? @default = default)
        => TryGetValue(key, out var value) && value is T typed ? typed : @default;
}

/// <summary>Result of one tool execution, before triage. RawOutput is stored verbatim for
/// evidence; NormalizedOutput is the compact form sent back to the LLM (Section 4 step 4).</summary>
public sealed class ToolExecutionResult
{
    public bool Success { get; init; }
    public required string RawOutput { get; init; }
    public required string NormalizedOutput { get; init; }
    public string? Error { get; init; }

    public static ToolExecutionResult Ok(string raw, string normalized) => new() { Success = true, RawOutput = raw, NormalizedOutput = normalized };
    public static ToolExecutionResult Fail(string error) => new() { Success = false, RawOutput = error, NormalizedOutput = error, Error = error };
}

/// <summary>
/// Common contract every security tool wrapper implements. The orchestrator never calls
/// an external tool directly — only through this interface, after the scope guard has
/// approved the call.
/// </summary>
public interface ISecurityTool
{
    /// <summary>Tool name as the LLM's tool schema names it, e.g. "check_security_headers".</summary>
    string Name { get; }

    /// <summary>Passive (read-only recon) or Active (sends adversarial input); decides whether
    /// the scope guard requires the LabActive profile.</summary>
    ToolKind Kind { get; }

    /// <summary>OWASP Top 10:2025 categories this tool contributes evidence for, e.g. "A02", "A03".</summary>
    IReadOnlyList<string> OwaspCategories { get; }

    Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default);
}

/// <summary>Registry of every tool wrapper available to the agent loop (Section 3/4/5).</summary>
public interface IToolRegistry
{
    IReadOnlyCollection<ISecurityTool> All { get; }
    ISecurityTool? Find(string name);
}

public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ISecurityTool> _tools;

    public ToolRegistry(IEnumerable<ISecurityTool> tools)
    {
        _tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<ISecurityTool> All => _tools.Values;

    public ISecurityTool? Find(string name) => _tools.GetValueOrDefault(name);
}
