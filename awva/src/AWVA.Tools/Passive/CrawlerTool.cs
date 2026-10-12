using System.Text.Json;
using System.Text.RegularExpressions;
using AWVA.Core;
using AWVA.Domain;

namespace AWVA.Tools.Passive;

/// <summary>
/// crawl(url, max_depth, max_pages) (Section 4 step 2c). Scope-bounded: same-origin only,
/// bounded depth/page count (enforced again here even though AWVA.Core.ScopeGuard already
/// caps the arguments), and excludes logout/destructive-looking paths (Section 6: "Crawler
/// limits: max depth, max pages, same-origin only, and exclusion of logout or destructive
/// endpoints"). Does not crawl authenticated areas in this implementation — TODO: wire in
/// the two lab test accounts the proposal mentions for authenticated crawling.
/// </summary>
public sealed class CrawlerTool : ToolBase
{
    private static readonly Regex HrefPattern = new("href=[\"']([^\"'#]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] ExcludedPathHints = { "logout", "signout", "delete", "destroy" };

    private readonly IHttpClientFactory _httpClientFactory;

    public CrawlerTool(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public override string Name => "crawl";
    public override ToolKind Kind => ToolKind.Passive;
    public override IReadOnlyList<string> OwaspCategories => Array.Empty<string>();

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
    {
        var startUrl = arguments.Get<string>("url") ?? throw new ArgumentException("crawl requires 'url'.");
        var maxDepth = Math.Clamp(arguments.Get<int>("max_depth", 2), 0, 10);
        var maxPages = Math.Clamp(arguments.Get<int>("max_pages", 50), 1, 1000);

        var origin = new Uri(startUrl);
        var client = _httpClientFactory.CreateClient(nameof(CrawlerTool));

        var visited = new HashSet<string>();
        var queue = new Queue<(Uri Url, int Depth)>();
        queue.Enqueue((origin, 0));
        var discovered = new List<string>();

        while (queue.Count > 0 && visited.Count < maxPages)
        {
            var (current, depth) = queue.Dequeue();
            if (!visited.Add(current.ToString())) continue;
            if (depth > maxDepth) continue;

            string html;
            try
            {
                html = await client.GetStringAsync(current, ct);
            }
            catch
            {
                continue;
            }

            discovered.Add(current.ToString());

            if (depth == maxDepth) continue;

            foreach (Match match in HrefPattern.Matches(html))
            {
                if (!Uri.TryCreate(current, match.Groups[1].Value, out var next)) continue;
                if (!string.Equals(next.Host, origin.Host, StringComparison.OrdinalIgnoreCase)) continue; // same-origin only
                if (ExcludedPathHints.Any(hint => next.AbsolutePath.Contains(hint, StringComparison.OrdinalIgnoreCase))) continue;
                if (visited.Contains(next.ToString())) continue;
                queue.Enqueue((next, depth + 1));
            }
        }

        var raw = $"Crawl from {startUrl} (max_depth={maxDepth}, max_pages={maxPages}):\n" + string.Join("\n", discovered);
        var normalized = JsonSerializer.Serialize(new { startUrl, maxDepth, maxPages, discoveredCount = discovered.Count, discovered });
        return ToolExecutionResult.Ok(raw, normalized);
    }
}
