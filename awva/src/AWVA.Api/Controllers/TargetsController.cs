using AWVA.Api.Dtos;
using AWVA.Core;
using AWVA.Data;
using AWVA.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AWVA.Api.Controllers;

/// <summary>
/// Target submission and scope validation (Section 4 step 1): "The backend accepts it
/// only if the host and resolved IP match the allowlist; Lab-active additionally requires
/// the target to sit on the lab Docker network. Rejected targets are logged."
/// </summary>
[ApiController]
[Route("api/targets")]
public class TargetsController : ControllerBase
{
    private readonly AwvaDbContext _db;
    private readonly IScopeGuard _scopeGuard;

    public TargetsController(AwvaDbContext db, IScopeGuard scopeGuard)
    {
        _db = db;
        _scopeGuard = scopeGuard;
    }

    [HttpPost]
    public async Task<ActionResult<TargetResponse>> Submit([FromBody] SubmitTargetRequest request, CancellationToken ct)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
        {
            return BadRequest("Url must be an absolute URI, e.g. http://juice-shop.lab:3000");
        }

        var target = new Target
        {
            Url = request.Url,
            Host = uri.Host,
            Profile = request.Profile,
        };

        var decision = await _scopeGuard.ValidateTargetAsync(target, ct);
        target.IsApproved = decision.Allowed;
        target.RejectionReason = decision.Reason;

        _db.Targets.Add(target);
        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            Actor = ActorType.System,
            Action = decision.Allowed ? "target.approved" : "target.rejected",
            DetailsJson = System.Text.Json.JsonSerializer.Serialize(new { target.Url, target.Profile, decision.Reason }),
        });
        await _db.SaveChangesAsync(ct);

        if (!decision.Allowed)
        {
            return BadRequest(new { error = decision.Reason, target = TargetResponse.From(target) });
        }

        return CreatedAtAction(nameof(Get), new { id = target.Id }, TargetResponse.From(target));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TargetResponse>> Get(int id, CancellationToken ct)
    {
        var target = await _db.Targets.FindAsync(new object?[] { id }, ct);
        return target is null ? NotFound() : TargetResponse.From(target);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TargetResponse>>> List(CancellationToken ct)
    {
        var targets = await _db.Targets.OrderByDescending(t => t.CreatedAt).Take(100).ToListAsync(ct);
        return targets.Select(TargetResponse.From).ToList();
    }
}
