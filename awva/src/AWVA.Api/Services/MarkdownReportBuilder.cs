using System.Text;
using AWVA.Domain;

namespace AWVA.Api.Services;

/// <summary>
/// Markdown export of a completed scan (Section 4 step 6: "exports Markdown or PDF";
/// Section 3 "Reporting: Jinja2 to Markdown, WeasyPrint or Pandoc to PDF"). PDF export is
/// left as a TODO for Student 1 — pipe this Markdown through a Pandoc process call via
/// IExternalProcessRunner, or an equivalent .NET Markdown-to-PDF library.
/// </summary>
public static class MarkdownReportBuilder
{
    public static string Build(Scan scan, Target target, IReadOnlyList<Finding> findings)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# AWVA Audit Report — {target.Url}");
        sb.AppendLine();
        sb.AppendLine($"- **Scan ID:** {scan.Id}");
        sb.AppendLine($"- **Profile:** {scan.Profile}");
        sb.AppendLine($"- **Status:** {scan.Status}");
        sb.AppendLine($"- **Started:** {scan.StartedAt:u}");
        sb.AppendLine($"- **Completed:** {scan.CompletedAt:u}");
        sb.AppendLine($"- **Tool calls used:** {scan.StepsUsed} / {scan.StepBudget}");
        sb.AppendLine();

        var reportable = findings.Where(f => !f.IsHallucination).ToList();
        var rejected = findings.Count(f => f.IsHallucination);

        sb.AppendLine("## Severity scorecard");
        sb.AppendLine();
        sb.AppendLine("| Severity | Count |");
        sb.AppendLine("|---|---|");
        foreach (var severity in Enum.GetValues<Severity>().OrderByDescending(s => s))
        {
            sb.AppendLine($"| {severity} | {reportable.Count(f => f.Severity == severity)} |");
        }
        sb.AppendLine();
        sb.AppendLine($"_{rejected} draft finding(s) were rejected as hallucinations (no matching tool evidence) and are excluded above._");
        sb.AppendLine();

        sb.AppendLine("## OWASP Top 10:2025 coverage");
        sb.AppendLine();
        var categories = reportable.Select(f => f.OwaspCategory.Split(':')[0]).Distinct().OrderBy(c => c);
        sb.AppendLine(categories.Any() ? string.Join(", ", categories) : "_No categories with findings yet._");
        sb.AppendLine();

        sb.AppendLine("## Findings");
        sb.AppendLine();
        foreach (var finding in reportable.OrderByDescending(f => f.Severity))
        {
            sb.AppendLine($"### {finding.ExternalId}: {finding.Title}");
            sb.AppendLine();
            sb.AppendLine($"- **Severity:** {finding.Severity} ({finding.SeveritySource}{(finding.SeveritySource == SeveritySource.LlmAdjusted ? $" — {finding.SeverityJustification}" : "")})");
            sb.AppendLine($"- **CWE:** {finding.Cwe}");
            sb.AppendLine($"- **OWASP category:** {finding.OwaspCategory}");
            if (!string.IsNullOrWhiteSpace(finding.WstgId)) sb.AppendLine($"- **WSTG:** {finding.WstgId}");
            sb.AppendLine($"- **Confidence:** {finding.Confidence}");
            sb.AppendLine($"- **Source tool call:** {finding.SourceToolCall?.ExternalCallId ?? "(unknown)"}");
            sb.AppendLine();
            sb.AppendLine("**Evidence**");
            sb.AppendLine("```");
            sb.AppendLine(finding.Evidence);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine($"**Remediation:** {finding.Remediation}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
