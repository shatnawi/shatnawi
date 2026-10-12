using System.Text.Json;
using AWVA.Core;
using AWVA.Domain;

namespace AWVA.Tools.Active;

/// <summary>
/// probe_errors(urls) (Section 4 step 2e, A10). Sends malformed/oversized/unexpected
/// inputs and HTTP methods and looks for stack traces, HTTP 500s, or fail-open behaviour
/// (Section 5 A10 row). Payloads are inert (long strings, odd methods, malformed JSON) —
/// no code execution or injection payloads.
/// </summary>
public sealed class ErrorProberTool : ToolBase
{
    private static readonly HttpMethod[] UnexpectedMethods = { HttpMethod.Put, HttpMethod.Delete, HttpMethod.Trace, new("PATCH") };
    private static readonly string[] StackTraceMarkers = { "Exception", "StackTrace", "at System.", "Microsoft.AspNetCore", "Traceback (most recent call last)" };

    private readonly IHttpClientFactory _httpClientFactory;
    public ErrorProberTool(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public override string Name => "probe_errors";
    public override ToolKind Kind => ToolKind.Active;
    public override IReadOnlyList<string> OwaspCategories => new[] { "A10" };

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
    {
        var urls = arguments.Get<IEnumerable<string>>("urls")?.ToList() ?? new List<string> { arguments.Get<string>("url") ?? throw new ArgumentException("probe_errors requires 'urls' or 'url'.") };
        var client = _httpClientFactory.CreateClient(nameof(ErrorProberTool));

        var lines = new List<string>();
        var exposures = new List<object>();

        foreach (var url in urls)
        {
            // Oversized / malformed input via query string.
            var oversized = new string('a', 20_000);
            var probes = new (string label, Func<Task<HttpResponseMessage>> send)[]
            {
                ("oversized-query", () => client.GetAsync($"{url}{(url.Contains('?') ? "&" : "?")}awva_probe={oversized}", ct)),
                ("malformed-json-body", () => client.PostAsync(url, new StringContent("{not: valid json", System.Text.Encoding.UTF8, "application/json"), ct)),
            }.Concat(UnexpectedMethods.Select(m => ((string label, Func<Task<HttpResponseMessage>> send))(
                $"method-{m.Method}", () => client.SendAsync(new HttpRequestMessage(m, url), ct)))).ToArray();

            foreach (var (label, send) in probes)
            {
                HttpResponseMessage response;
                try
                {
                    response = await send();
                }
                catch (Exception ex)
                {
                    lines.Add($"{label} {url} -> request failed: {ex.Message}");
                    continue;
                }

                var body = await response.Content.ReadAsStringAsync(ct);
                lines.Add($"{label} {url} -> {(int)response.StatusCode}, body length {body.Length}");

                var markerHit = StackTraceMarkers.FirstOrDefault(m => body.Contains(m, StringComparison.Ordinal));
                if (markerHit is not null || (int)response.StatusCode == 500)
                {
                    exposures.Add(new { url, probe = label, statusCode = (int)response.StatusCode, marker = markerHit });
                }
            }
        }

        var normalized = JsonSerializer.Serialize(new { urlCount = urls.Count, exposures });
        return ToolExecutionResult.Ok(string.Join("\n", lines), normalized);
    }
}
