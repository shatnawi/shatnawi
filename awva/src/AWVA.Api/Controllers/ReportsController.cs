using AWVA.Api.Services;
using AWVA.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AWVA.Api.Controllers;

/// <summary>Markdown/PDF export (Section 4 step 6). PDF is a TODO — see MarkdownReportBuilder.</summary>
[ApiController]
[Route("api/scans/{scanId:int}/report")]
public class ReportsController : ControllerBase
{
    private readonly AwvaDbContext _db;

    public ReportsController(AwvaDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetMarkdown(int scanId, CancellationToken ct)
    {
        var scan = await _db.Scans.Include(s => s.Target).FirstOrDefaultAsync(s => s.Id == scanId, ct);
        if (scan?.Target is null) return NotFound();

        var findings = await _db.Findings
            .Include(f => f.SourceToolCall)
            .Where(f => f.ScanId == scanId)
            .ToListAsync(ct);

        var markdown = MarkdownReportBuilder.Build(scan, scan.Target, findings);
        return Content(markdown, "text/markdown");
    }
}
