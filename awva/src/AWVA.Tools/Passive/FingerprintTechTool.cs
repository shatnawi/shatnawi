using System.Text.Json;
using AWVA.Core;
using AWVA.Domain;

namespace AWVA.Tools.Passive;

/// <summary>
/// fingerprint_tech(url) (Section 4 step 2a, A03). Real but intentionally lightweight:
/// reads the Server/X-Powered-By response headers. The proposal's fuller version
/// (WhatWeb or httpx) is left as a TODO for richer fingerprinting (JS framework
/// detection, favicon hashing, etc.) — see <see cref="ComponentAuditTool"/> for the
/// companion library-version check.
/// </summary>
public sealed class FingerprintTechTool : ToolBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FingerprintTechTool(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public override string Name => "fingerprint_tech";
    public override ToolKind Kind => ToolKind.Passive;
    public override IReadOnlyList<string> OwaspCategories => new[] { "A03" };

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
    {
        var url = arguments.Get<string>("url") ?? throw new ArgumentException("fingerprint_tech requires 'url'.");
        var client = _httpClientFactory.CreateClient(nameof(FingerprintTechTool));

        using var response = await client.GetAsync(url, ct);
        var server = response.Headers.TryGetValues("Server", out var s) ? string.Join(", ", s) : null;
        var poweredBy = response.Headers.TryGetValues("X-Powered-By", out var p) ? string.Join(", ", p) : null;
        var aspNetVersion = response.Headers.TryGetValues("X-AspNet-Version", out var a) ? string.Join(", ", a) : null;

        var raw = $"GET {url} -> {(int)response.StatusCode}\n" +
                  $"Server: {server ?? "(absent)"}\n" +
                  $"X-Powered-By: {poweredBy ?? "(absent)"}\n" +
                  $"X-AspNet-Version: {aspNetVersion ?? "(absent)"}";

        var normalized = JsonSerializer.Serialize(new { url, status = (int)response.StatusCode, server, poweredBy, aspNetVersion });
        return ToolExecutionResult.Ok(raw, normalized);
    }
}
