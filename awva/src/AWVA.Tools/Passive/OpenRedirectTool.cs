using System.Text.Json;
using AWVA.Core;
using AWVA.Domain;

namespace AWVA.Tools.Passive;

/// <summary>
/// check_open_redirect(urls) (Section 4 step 2d, Section 5.1 "Open redirect" module).
/// For each candidate URL, tries common redirect parameter names ("next", "url",
/// "return", "redirect") with a benign external marker value and checks whether the
/// server's Location header echoes it back unvalidated. No request ever follows the
/// redirect automatically (HttpClient AllowAutoRedirect=false) so this never actually
/// leaves the authorized target.
/// </summary>
public sealed class OpenRedirectTool : ToolBase
{
    private static readonly string[] ParamNames = { "next", "url", "return", "redirect", "returnUrl" };
    private const string Marker = "https://awva-redirect-check.invalid/marker";

    private readonly IHttpClientFactory _httpClientFactory;

    public OpenRedirectTool(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public override string Name => "check_open_redirect";
    public override ToolKind Kind => ToolKind.Passive;
    public override IReadOnlyList<string> OwaspCategories => Array.Empty<string>();

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
    {
        var urls = arguments.Get<IEnumerable<string>>("urls")?.ToList() ?? new List<string> { arguments.Get<string>("url") ?? throw new ArgumentException("check_open_redirect requires 'urls' or 'url'.") };

        var client = _httpClientFactory.CreateClient(nameof(OpenRedirectTool));
        client.DefaultRequestHeaders.Clear();
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var noRedirectClient = new HttpClient(handler);

        var lines = new List<string>();
        var vulnerable = new List<object>();

        foreach (var baseUrl in urls)
        {
            foreach (var param in ParamNames)
            {
                var separator = baseUrl.Contains('?') ? "&" : "?";
                var testUrl = $"{baseUrl}{separator}{param}={Uri.EscapeDataString(Marker)}";

                HttpResponseMessage response;
                try
                {
                    response = await noRedirectClient.GetAsync(testUrl, ct);
                }
                catch
                {
                    continue;
                }

                var location = response.Headers.Location?.ToString();
                lines.Add($"GET {testUrl} -> {(int)response.StatusCode} Location: {location ?? "(none)"}");

                if (location is not null && location.Contains("awva-redirect-check.invalid", StringComparison.OrdinalIgnoreCase))
                {
                    vulnerable.Add(new { baseUrl, param, testUrl, location });
                }
            }
        }

        var normalized = JsonSerializer.Serialize(new { checkedUrlCount = urls.Count, vulnerable });
        return ToolExecutionResult.Ok(string.Join("\n", lines), normalized);
    }
}
