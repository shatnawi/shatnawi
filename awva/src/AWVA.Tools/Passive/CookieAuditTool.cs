using System.Text.Json;
using AWVA.Core;
using AWVA.Domain;

namespace AWVA.Tools.Passive;

/// <summary>check_cookies(url) (Section 4 step 2b, A02/A04). Inspects Set-Cookie attributes.</summary>
public sealed class CookieAuditTool : ToolBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CookieAuditTool(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public override string Name => "check_cookies";
    public override ToolKind Kind => ToolKind.Passive;
    public override IReadOnlyList<string> OwaspCategories => new[] { "A02", "A04" };

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
    {
        var url = arguments.Get<string>("url") ?? throw new ArgumentException("check_cookies requires 'url'.");
        var client = _httpClientFactory.CreateClient(nameof(CookieAuditTool));

        using var response = await client.GetAsync(url, ct);
        var setCookies = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToList() : new List<string>();

        var findings = setCookies.Select(cookie => new
        {
            cookie,
            missingSecure = !cookie.Contains("Secure", StringComparison.OrdinalIgnoreCase),
            missingHttpOnly = !cookie.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase),
            missingSameSite = !cookie.Contains("SameSite", StringComparison.OrdinalIgnoreCase),
        }).ToList();

        var raw = setCookies.Count == 0
            ? $"GET {url} -> no Set-Cookie header present."
            : string.Join("\n", setCookies.Select(c => $"Set-Cookie: {c}"));

        var normalized = JsonSerializer.Serialize(new { url, cookieCount = setCookies.Count, findings });
        return ToolExecutionResult.Ok(raw, normalized);
    }
}
