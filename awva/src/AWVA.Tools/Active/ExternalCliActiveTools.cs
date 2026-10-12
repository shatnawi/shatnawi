using AWVA.Core;
using AWVA.Domain;

namespace AWVA.Tools.Active;

/// <summary>
/// active_scan(urls) (Section 4 step 2e, A01/A02/A03/A07). STUB: wraps the OWASP ZAP
/// Docker container's REST API (ascan) with the DoS scan rules disabled, per Section 6
/// "ZAP scan policy with denial-of-service rules disabled". LabActive-only.
/// TODO (Student 4): start ZAP's `zap.sh -daemon`, drive it over its REST API
/// (zap-api-python-equivalent HTTP calls), poll `/JSON/ascan/view/status`, then pull
/// `/JSON/core/view/alerts` and map each alert to a DraftFinding.
/// </summary>
public sealed class ActiveScanTool : ToolBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    public ActiveScanTool(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public override string Name => "active_scan";
    public override ToolKind Kind => ToolKind.Active;
    public override IReadOnlyList<string> OwaspCategories => new[] { "A01", "A02", "A03", "A07" };

    public override Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
        => NotYetImplemented(Name, "OWASP ZAP (Docker REST API)",
            "Drive ZAP's ascan over its REST API with DoS rules disabled; poll for completion then pull /JSON/core/view/alerts.");
}

/// <summary>
/// confirm_sqli(url, param) (Section 4 step 2e, A01). STUB: wraps the sqlmap CLI at
/// level 1 / risk 1 with no data dumping (Section 6: "sqlmap at level 1 and risk 1 with
/// no data dumping"), used only to confirm candidates ZAP already flagged — never to run
/// an independent injection sweep. LabActive-only.
/// TODO (Student 4): `sqlmap -u &lt;url&gt; -p &lt;param&gt; --level=1 --risk=1 --batch
/// --output-dir=... ` (no --dump, no --os-shell); parse the text report for a positive
/// confirmation line.
/// </summary>
public sealed class SqliConfirmTool : ToolBase
{
    private readonly IExternalProcessRunner _processRunner;
    public SqliConfirmTool(IExternalProcessRunner processRunner) => _processRunner = processRunner;

    public override string Name => "confirm_sqli";
    public override ToolKind Kind => ToolKind.Active;
    public override IReadOnlyList<string> OwaspCategories => new[] { "A01" };

    public override Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
        => NotYetImplemented(Name, "sqlmap CLI (level 1, risk 1, no --dump)",
            "Confirm a ZAP-flagged SQLi candidate with sqlmap at the lowest level/risk and without data extraction.");
}

/// <summary>
/// test_file_upload(url) (Section 4 step 2e, Section 5.1 "File upload" module). STUB:
/// extension/content-type bypass and path-traversal-in-filename checks against the lab's
/// own upload endpoint, with any uploaded probe file inert (no executable payload) and
/// a following request to see whether it is served back verbatim. LabActive-only.
/// TODO (Student 4): implement the upload attempts described above via IHttpClientFactory,
/// then delete the inert probe file afterward if the app does not already quarantine it.
/// </summary>
public sealed class FileUploadTestTool : ToolBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    public FileUploadTestTool(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public override string Name => "test_file_upload";
    public override ToolKind Kind => ToolKind.Active;
    public override IReadOnlyList<string> OwaspCategories => Array.Empty<string>();

    public override Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
        => NotYetImplemented(Name, "custom Python-equivalent upload tester",
            "Attempt extension/content-type bypass and traversal-in-filename uploads of inert probe files against the lab upload endpoint only.");
}

/// <summary>
/// api_scan(spec) (Section 4 step 2e, Section 5.1 "API security" module). STUB: wraps
/// Schemathesis for OpenAPI-spec-driven property tests and graphql-cop for GraphQL
/// introspection/depth checks. LabActive-only (crAPI/VAmPI).
/// TODO (Student 3): `schemathesis run &lt;spec url&gt; --checks all` and
/// `graphql-cop -t &lt;endpoint&gt;`, each via IExternalProcessRunner.
/// </summary>
public sealed class ApiScanTool : ToolBase
{
    private readonly IExternalProcessRunner _processRunner;
    public ApiScanTool(IExternalProcessRunner processRunner) => _processRunner = processRunner;

    public override string Name => "api_scan";
    public override ToolKind Kind => ToolKind.Active;
    public override IReadOnlyList<string> OwaspCategories => Array.Empty<string>();

    public override Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
        => NotYetImplemented(Name, "Schemathesis + graphql-cop",
            "Run Schemathesis against the imported OpenAPI spec and graphql-cop against any GraphQL endpoint.");
}

/// <summary>
/// design_review() (Section 4 step 2f, A06). STUB: the LLM-guided design-review
/// checklist the proposal assigns to Student 2 — a set of architecture/design questions
/// (e.g. "are security decisions made client-side only?") the planner walks through using
/// the scan's accumulated tool output as context, rather than a tool that sends traffic.
/// TODO (Student 2): define the checklist and have ILlmPlanner answer each item, citing
/// the ToolCall evidence it used.
/// </summary>
public sealed class DesignReviewTool : ToolBase
{
    public override string Name => "design_review";
    public override ToolKind Kind => ToolKind.Active;
    public override IReadOnlyList<string> OwaspCategories => new[] { "A06" };

    public override Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
        => NotYetImplemented(Name, "LLM-guided design-review checklist",
            "Define the A06 checklist and have the planner answer each item from accumulated scan evidence.");
}

/// <summary>
/// logging_check() (Section 4 step 2f, A09). STUB: replays this scan's own attack log
/// against the lab's SIEM (Wazuh/ELK) to check whether the attacks were logged and
/// alerted — "black-box checks alone cannot see logs" (Section 5 A09 row). LabActive-only.
/// TODO (Student 5): query the lab Wazuh/ELK API for alerts correlated with this scan's
/// AuditLogEntry timestamps and source IP.
/// </summary>
public sealed class LoggingCheckTool : ToolBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    public LoggingCheckTool(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public override string Name => "logging_check";
    public override ToolKind Kind => ToolKind.Active;
    public override IReadOnlyList<string> OwaspCategories => new[] { "A09" };

    public override Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
        => NotYetImplemented(Name, "Wazuh/ELK query API",
            "Correlate this scan's AuditLogEntry rows with SIEM alerts in the lab to confirm detection coverage.");
}
