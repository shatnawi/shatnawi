using AWVA.Domain;

namespace AWVA.Core.Planning;

/// <summary>
/// A deterministic stand-in for the LLM planner, used for local dev, unit tests, and the
/// "baseline (a): tools run in a fixed script with no LLM" evaluation condition (Section 9).
/// It walks the same logical order the proposal describes in Section 4 step 2 (a-f):
/// fingerprint -> headers/cookies/TLS -> crawl -> misconfig/open-redirect/js-secrets ->
/// (LabActive only) active testing -> design review/logging check. It never adjusts
/// severity and proposes no findings itself; <see cref="AWVA.Core.Triage.RuleBasedTriage"/>
/// does that deterministically from tool output.
/// </summary>
public sealed class DeterministicPlanner : ILlmPlanner
{
    private static readonly string[] PassiveOrder =
    {
        "fingerprint_tech",
        "check_components",
        "check_security_headers",
        "check_cookies",
        "audit_tls",
        "crawl",
        "scan_misconfigurations",
        "check_open_redirect",
        "scan_js_secrets",
    };

    private static readonly string[] ActiveOrder =
    {
        "active_scan",
        "confirm_sqli",
        "test_access_control",
        "test_authentication",
        "probe_errors",
        "check_jwt",
        "test_file_upload",
        "api_scan",
        "design_review",
        "logging_check",
    };

    public Task<PlannedToolCall?> PlanNextStepAsync(Scan scan, IReadOnlyList<ToolCall> history, CancellationToken ct = default)
    {
        var alreadyCalled = history.Select(h => h.ToolName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var order = scan.Profile == ScanProfile.LabActive
            ? PassiveOrder.Concat(ActiveOrder)
            : PassiveOrder;

        var next = order.FirstOrDefault(name => !alreadyCalled.Contains(name));
        if (next is null) return Task.FromResult<PlannedToolCall?>(null);

        var args = new ToolArguments { ["url"] = scan.Target?.Url };
        return Task.FromResult<PlannedToolCall?>(new PlannedToolCall(next, args));
    }

    public Task<IReadOnlyList<DraftFinding>> TriageAsync(Scan scan, IReadOnlyList<ToolCall> history, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DraftFinding>>(Array.Empty<DraftFinding>());
}
