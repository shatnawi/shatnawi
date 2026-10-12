using System.Text.Json;
using AWVA.Core.Planning;
using AWVA.Core.Triage;
using AWVA.Data;
using AWVA.Domain;
using Microsoft.EntityFrameworkCore;

namespace AWVA.Core;

/// <summary>
/// Runs the agent loop described in Section 4: planning -> guarded execution ->
/// normalization -> triage -> (implicitly) reporting. "The LLM never talks to the target
/// directly; it only proposes tool calls, which the scope guard approves or refuses"
/// (Section 2) — this class is where that boundary is enforced in code.
/// </summary>
public interface IAgentOrchestrator
{
    Task RunAsync(int scanId, CancellationToken ct = default);
}

public sealed class AgentOrchestrator : IAgentOrchestrator
{
    private readonly AwvaDbContext _db;
    private readonly IScopeGuard _scopeGuard;
    private readonly IToolRegistry _tools;
    private readonly ILlmPlanner _planner;

    public AgentOrchestrator(AwvaDbContext db, IScopeGuard scopeGuard, IToolRegistry tools, ILlmPlanner planner)
    {
        _db = db;
        _scopeGuard = scopeGuard;
        _tools = tools;
        _planner = planner;
    }

    public async Task RunAsync(int scanId, CancellationToken ct = default)
    {
        var scan = await _db.Scans
            .Include(s => s.Target)
            .Include(s => s.ToolCalls)
            .FirstOrDefaultAsync(s => s.Id == scanId, ct)
            ?? throw new InvalidOperationException($"Scan {scanId} not found.");

        if (scan.Target is null)
            throw new InvalidOperationException($"Scan {scanId} has no target.");

        scan.Status = ScanStatus.Running;
        scan.StartedAt = DateTimeOffset.UtcNow;
        await Log(scan, ActorType.System, "scan.started", new { scan.TargetId, scan.Profile });
        await _db.SaveChangesAsync(ct);

        while (scan.StepsUsed < scan.StepBudget)
        {
            ct.ThrowIfCancellationRequested();

            var plan = await _planner.PlanNextStepAsync(scan, scan.ToolCalls, ct);
            if (plan is null)
            {
                await Log(scan, ActorType.Llm, "planner.done", null);
                break;
            }

            await Log(scan, ActorType.Llm, "planner.proposed_call", new { plan.ToolName, plan.Arguments });

            var tool = _tools.Find(plan.ToolName);
            if (tool is null)
            {
                await Log(scan, ActorType.System, "tool_call.refused", new { plan.ToolName, reason = "unknown tool" });
                scan.StepsUsed++;
                continue;
            }

            var decision = _scopeGuard.ValidateToolCall(scan, scan.Target, tool.Kind, tool.Name, plan.Arguments);

            var callId = $"call_{scan.StepsUsed + 1:D2}";
            var toolCall = new ToolCall
            {
                ScanId = scan.Id,
                ExternalCallId = callId,
                ToolName = tool.Name,
                ToolKind = tool.Kind,
                ArgumentsJson = JsonSerializer.Serialize(plan.Arguments),
            };

            if (!decision.Allowed)
            {
                toolCall.Status = ToolCallStatus.Refused;
                toolCall.RefusalReason = decision.Reason;
                toolCall.CompletedAt = DateTimeOffset.UtcNow;
                _db.ToolCalls.Add(toolCall);
                await Log(scan, ActorType.System, "tool_call.refused", new { tool.Name, decision.Reason });
                // The refusal is returned to the model as the "result" of its proposed call
                // (Section 4 step 3), represented here by the refused ToolCall itself being
                // part of `history` on the next PlanNextStepAsync.
                scan.StepsUsed++;
                await _db.SaveChangesAsync(ct);
                continue;
            }

            try
            {
                var result = await tool.ExecuteAsync(plan.Arguments, ct);
                toolCall.Status = result.Success ? ToolCallStatus.Succeeded : ToolCallStatus.Failed;
                toolCall.RawOutput = result.RawOutput;
                toolCall.NormalizedOutput = result.NormalizedOutput;
            }
            catch (Exception ex)
            {
                toolCall.Status = ToolCallStatus.Failed;
                toolCall.RawOutput = ex.ToString();
                toolCall.NormalizedOutput = $"Tool '{tool.Name}' threw: {ex.Message}";
            }

            toolCall.CompletedAt = DateTimeOffset.UtcNow;
            _db.ToolCalls.Add(toolCall);
            scan.ToolCalls.Add(toolCall);
            await Log(scan, ActorType.System, "tool_call.executed", new { tool.Name, toolCall.Status });
            scan.StepsUsed++;
            await _db.SaveChangesAsync(ct);
        }

        await TriageAsync(scan, ct);

        scan.Status = ScanStatus.Completed;
        scan.CompletedAt = DateTimeOffset.UtcNow;
        await Log(scan, ActorType.System, "scan.completed", new { scan.StepsUsed });
        await _db.SaveChangesAsync(ct);
    }

    private async Task TriageAsync(Scan scan, CancellationToken ct)
    {
        var drafts = await _planner.TriageAsync(scan, scan.ToolCalls, ct);

        foreach (var draft in drafts)
        {
            var validation = FindingValidator.Validate(draft, scan.ToolCalls);
            var sourceCall = scan.ToolCalls.FirstOrDefault(c => c.ExternalCallId == draft.SourceToolCallExternalId);

            var finding = new Finding
            {
                ScanId = scan.Id,
                ExternalId = draft.ExternalId,
                Title = draft.Title,
                Cwe = draft.Cwe,
                OwaspCategory = draft.OwaspCategory,
                WstgId = draft.WstgId,
                Severity = draft.Severity,
                SeveritySource = draft.SeveritySource,
                SeverityJustification = draft.SeverityJustification,
                Evidence = draft.Evidence,
                SourceToolCallId = sourceCall?.Id,
                AffectedUrlsJson = JsonSerializer.Serialize(draft.AffectedUrls),
                Remediation = draft.Remediation,
                Confidence = draft.Confidence,
                IsHallucination = validation.IsHallucination,
            };

            _db.Findings.Add(finding);

            if (validation.IsHallucination)
            {
                await Log(scan, ActorType.System, "finding.rejected_hallucination", new { draft.ExternalId, validation.Reason });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task Log(Scan scan, ActorType actor, string action, object? details)
    {
        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            ScanId = scan.Id,
            Actor = actor,
            Action = action,
            DetailsJson = details is null ? null : JsonSerializer.Serialize(details),
        });
        await Task.CompletedTask;
    }
}
