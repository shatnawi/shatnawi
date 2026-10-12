using System.Text.Json;
using AWVA.Core;
using AWVA.Domain;

namespace AWVA.Tools.Passive;

/// <summary>
/// check_security_headers(url) (Section 4 step 2b, A02). Deterministic checks against the
/// OWASP Secure Headers guidance, matching the "Header and cookie audit / Custom Python
/// (httpx library)" row of the tech-stack table. Check ids match
/// <see cref="AWVA.Core.Triage.SeverityRuleTable"/>.
/// </summary>
public sealed class SecurityHeadersTool : ToolBase
{
    private static readonly string[] ExpectedHeaders =
    {
        "Content-Security-Policy",
        "Strict-Transport-Security",
        "X-Content-Type-Options",
        "X-Frame-Options",
        "Referrer-Policy",
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public SecurityHeadersTool(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public override string Name => "check_security_headers";
    public override ToolKind Kind => ToolKind.Passive;
    public override IReadOnlyList<string> OwaspCategories => new[] { "A02" };

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
    {
        var url = arguments.Get<string>("url") ?? throw new ArgumentException("check_security_headers requires 'url'.");
        var client = _httpClientFactory.CreateClient(nameof(SecurityHeadersTool));

        using var response = await client.GetAsync(url, ct);
        var present = new List<string>();
        var missing = new List<string>();
        var lines = new List<string> { $"GET {url} -> {(int)response.StatusCode}" };

        foreach (var header in ExpectedHeaders)
        {
            var hasHeader = response.Headers.TryGetValues(header, out var values) ||
                             response.Content.Headers.TryGetValues(header, out values);
            if (hasHeader)
            {
                present.Add(header);
                lines.Add($"{header}: {string.Join(", ", values!)}");
            }
            else
            {
                missing.Add(header);
                lines.Add($"{header} absent in response to GET {url}");
            }
        }

        var normalized = JsonSerializer.Serialize(new { url, present, missing });
        return ToolExecutionResult.Ok(string.Join("\n", lines), normalized);
    }
}
