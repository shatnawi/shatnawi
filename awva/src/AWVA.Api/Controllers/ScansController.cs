using AWVA.Api.Dtos;
using AWVA.Api.Services;
using AWVA.Data;
using AWVA.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AWVA.Api.Controllers;

/// <summary>Agent-loop lifecycle: start, poll status/progress, list tool calls and
/// findings, and abort (the "kill switch" from Section 6).</summary>
[ApiController]
[Route("api/scans")]
public class ScansController : ControllerBase
{
    private readonly AwvaDbContext _db;
    private readonly IScanJobQueue _queue;
    private readonly IScanCancellationRegistry _cancellationRegistry;

    public ScansController(AwvaDbContext db, IScanJobQueue queue, IScanCancellationRegistry cancellationRegistry)
    {
        _db = db;
        _queue = queue;
        _cancellationRegistry = cancellationRegistry;
    }

    [HttpPost]
    public async Task<ActionResult<ScanResponse>> Start([FromBody] StartScanRequest request, CancellationToken ct)
    {
        var target = await _db.Targets.FindAsync(new object?[] { request.TargetId }, ct);
        if (target is null) return NotFound($"Target {request.TargetId} not found.");
        if (!target.IsApproved) return BadRequest($"Target {request.TargetId} was not approved by the scope guard: {target.RejectionReason}");

        var scan = new Scan
        {
            TargetId = target.Id,
            Profile = target.Profile,
            StepBudget = request.StepBudget is > 0 and <= 200 ? request.StepBudget.Value : 30,
        };
        _db.Scans.Add(scan);
        await _db.SaveChangesAsync(ct);

        _queue.Enqueue(scan.Id);

        return CreatedAtAction(nameof(Get), new { id = scan.Id }, ScanResponse.From(scan));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ScanResponse>> Get(int id, CancellationToken ct)
    {
        var scan = await _db.Scans.FindAsync(new object?[] { id }, ct);
        return scan is null ? NotFound() : ScanResponse.From(scan);
    }

    [HttpGet("{id:int}/tool-calls")]
    public async Task<ActionResult<IReadOnlyList<ToolCallResponse>>> ToolCalls(int id, CancellationToken ct)
    {
        var calls = await _db.ToolCalls.Where(t => t.ScanId == id).OrderBy(t => t.Id).ToListAsync(ct);
        return calls.Select(ToolCallResponse.From).ToList();
    }

    [HttpGet("{id:int}/findings")]
    public async Task<ActionResult<IReadOnlyList<FindingResponse>>> Findings(int id, CancellationToken ct)
    {
        var findings = await _db.Findings
            .Include(f => f.SourceToolCall)
            .Where(f => f.ScanId == id)
            .OrderByDescending(f => f.Severity)
            .ToListAsync(ct);
        return findings.Select(FindingResponse.From).ToList();
    }

    /// <summary>The UI kill switch (Section 6). Cancels the running agent loop for this
    /// scan; already-persisted tool calls and findings are kept.</summary>
    [HttpPost("{id:int}/abort")]
    public async Task<IActionResult> Abort(int id, CancellationToken ct)
    {
        var scan = await _db.Scans.FindAsync(new object?[] { id }, ct);
        if (scan is null) return NotFound();

        var cancelled = _cancellationRegistry.TryCancel(id);
        return cancelled ? Accepted() : Conflict("Scan is not currently running.");
    }
}
