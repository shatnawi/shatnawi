using AWVA.Core;
using AWVA.Domain;

namespace AWVA.Tools;

/// <summary>Common boilerplate for the tool metadata every ISecurityTool exposes.</summary>
public abstract class ToolBase : ISecurityTool
{
    public abstract string Name { get; }
    public abstract ToolKind Kind { get; }
    public abstract IReadOnlyList<string> OwaspCategories { get; }

    public abstract Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default);

    /// <summary>Helper for the many tools that are not implemented yet in this codebase —
    /// "full breadth, stubbed": the metadata, scope-guard wiring, and registry entry are
    /// real; the external tool integration itself is a documented TODO for the owning
    /// student (Section 7).</summary>
    protected static Task<ToolExecutionResult> NotYetImplemented(string toolName, string externalTool, string todo)
        => Task.FromResult(ToolExecutionResult.Fail(
            $"{toolName} is a stub in this codebase. TODO: integrate {externalTool}. {todo}"));
}
