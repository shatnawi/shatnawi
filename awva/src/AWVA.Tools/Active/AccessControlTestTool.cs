using System.Net.Http.Headers;
using System.Text.Json;
using AWVA.Core;
using AWVA.Domain;

namespace AWVA.Tools.Active;

/// <summary>
/// test_access_control(endpoints, user_a, user_b) (Section 4 step 2e, A01). Custom
/// access-control / BOLA replay tester (Section 5.1 "API security" and Section 3 "Custom
/// testers" row): for each endpoint, requests it as user_a's own resource using user_b's
/// bearer token. A 2xx response is evidence of broken object-level authorization.
/// Active/LabActive-only — enforced by AWVA.Core.ScopeGuard before this ever runs.
/// </summary>
public sealed class AccessControlTestTool : ToolBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    public AccessControlTestTool(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public override string Name => "test_access_control";
    public override ToolKind Kind => ToolKind.Active;
    public override IReadOnlyList<string> OwaspCategories => new[] { "A01" };

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
    {
        var endpoints = arguments.Get<IEnumerable<string>>("endpoints")?.ToList()
            ?? throw new ArgumentException("test_access_control requires 'endpoints' (user_a's own resource URLs).");
        var userBToken = arguments.Get<string>("user_b_token")
            ?? throw new ArgumentException("test_access_control requires 'user_b_token' (a second lab test account's bearer token).");

        var client = _httpClientFactory.CreateClient(nameof(AccessControlTestTool));
        var lines = new List<string>();
        var bolaFindings = new List<object>();

        foreach (var endpoint in endpoints)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userBToken);

            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            lines.Add($"GET {endpoint} as user_b -> {(int)response.StatusCode}");

            if ((int)response.StatusCode is >= 200 and < 300)
            {
                bolaFindings.Add(new { endpoint, statusCode = (int)response.StatusCode, bodyPreview = body.Length > 200 ? body[..200] : body });
            }
        }

        var normalized = JsonSerializer.Serialize(new { checkedCount = endpoints.Count, bolaFindings });
        return ToolExecutionResult.Ok(string.Join("\n", lines), normalized);
    }
}
