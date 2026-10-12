using AWVA.Core;
using AWVA.Domain;

namespace AWVA.Tools.Passive;

/// <summary>
/// check_components(url) (Section 4 step 2a, A03). STUB: wraps retire.js (JS library
/// vulnerability scan) and Trivy (container image / SBOM scan of the lab target image).
/// TODO (Student 3, Section 7): shell out via IExternalProcessRunner, e.g.
/// `retire --js --outputformat json --path &lt;downloaded bundles&gt;` and
/// `trivy image --format json &lt;lab image&gt;`, then merge both JSON outputs.
/// </summary>
public sealed class ComponentAuditTool : ToolBase
{
    private readonly IExternalProcessRunner _processRunner;
    public ComponentAuditTool(IExternalProcessRunner processRunner) => _processRunner = processRunner;

    public override string Name => "check_components";
    public override ToolKind Kind => ToolKind.Passive;
    public override IReadOnlyList<string> OwaspCategories => new[] { "A03" };

    public override Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
        => NotYetImplemented(Name, "retire.js / Trivy",
            "Run retire.js against downloaded JS bundles and Trivy against the lab image; merge their JSON into one normalized result.");
}

/// <summary>
/// scan_misconfigurations(urls, tags) (Section 4 step 2d, A02/A03). STUB: wraps the
/// Nuclei CLI with `-jsonl`. The tag allowlist itself is already enforced centrally by
/// AWVA.Core.ScopeGuard.ValidateToolCall before this class is ever invoked.
/// TODO (Student 4): `nuclei -l &lt;urls file&gt; -tags &lt;allowed tags&gt; -jsonl`.
/// </summary>
public sealed class MisconfigScanTool : ToolBase
{
    private readonly IExternalProcessRunner _processRunner;
    public MisconfigScanTool(IExternalProcessRunner processRunner) => _processRunner = processRunner;

    public override string Name => "scan_misconfigurations";
    public override ToolKind Kind => ToolKind.Passive;
    public override IReadOnlyList<string> OwaspCategories => new[] { "A02", "A03" };

    public override Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
        => NotYetImplemented(Name, "Nuclei CLI (-jsonl)",
            "Invoke `nuclei -jsonl` with the allowed tags only; parse each JSON line into a normalized finding candidate.");
}

/// <summary>
/// scan_js_secrets(urls) (Section 4 step 2d, Section 5.1 "Secrets in JavaScript"). STUB:
/// wraps TruffleHog or gitleaks against downloaded JS bundles/source maps.
/// TODO (Student 3): `trufflehog filesystem &lt;downloaded bundles dir&gt; --json`.
/// </summary>
public sealed class JsSecretsScanTool : ToolBase
{
    private readonly IExternalProcessRunner _processRunner;
    public JsSecretsScanTool(IExternalProcessRunner processRunner) => _processRunner = processRunner;

    public override string Name => "scan_js_secrets";
    public override ToolKind Kind => ToolKind.Passive;
    public override IReadOnlyList<string> OwaspCategories => Array.Empty<string>();

    public override Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
        => NotYetImplemented(Name, "TruffleHog / gitleaks",
            "Download JS bundles and source maps discovered by the crawler, then run TruffleHog or gitleaks over them.");
}
