using System.Text.Json;
using AWVA.Core;
using AWVA.Domain;

namespace AWVA.Tools.Active;

/// <summary>
/// test_authentication(login_url) (Section 4 step 2e, A07). Covers the bounded checks
/// listed for A07 in Section 5 ("Login rate limiting and lockout (small bounded attempt
/// count), username enumeration, session rotation after login, logout invalidation"):
/// here, a small fixed number of failed logins to see whether the lab app starts
/// rate-limiting/locking out (never a real brute force), plus a username-enumeration
/// diff between a known-bad and a known-good username. Session rotation / logout
/// invalidation checks are left as a TODO since they need valid lab credentials supplied
/// by the caller.
/// </summary>
public sealed class AuthenticationTestTool : ToolBase
{
    /// <summary>Hard cap, independent of whatever the LLM asks for, per Section 6
    /// "authentication tests capped at a small fixed number of attempts".</summary>
    private const int MaxAttempts = 5;

    private readonly IHttpClientFactory _httpClientFactory;
    public AuthenticationTestTool(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public override string Name => "test_authentication";
    public override ToolKind Kind => ToolKind.Active;
    public override IReadOnlyList<string> OwaspCategories => new[] { "A07" };

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
    {
        var loginUrl = arguments.Get<string>("login_url") ?? throw new ArgumentException("test_authentication requires 'login_url'.");
        var knownUsername = arguments.Get<string>("known_username") ?? "admin";
        var unknownUsername = arguments.Get<string>("unknown_username") ?? "awva-nonexistent-user";

        var client = _httpClientFactory.CreateClient(nameof(AuthenticationTestTool));
        var lines = new List<string>();

        // Username enumeration: compare responses for a known vs. unknown username with a wrong password.
        var knownResponse = await PostLoginAttempt(client, loginUrl, knownUsername, "awva-wrong-password", ct);
        var unknownResponse = await PostLoginAttempt(client, loginUrl, unknownUsername, "awva-wrong-password", ct);
        lines.Add($"POST {loginUrl} known-username wrong-password -> {knownResponse.status}, body length {knownResponse.bodyLength}");
        lines.Add($"POST {loginUrl} unknown-username wrong-password -> {unknownResponse.status}, body length {unknownResponse.bodyLength}");
        var enumerationSuspected = knownResponse.status != unknownResponse.status || knownResponse.bodyLength != unknownResponse.bodyLength;

        // Bounded lockout probe: a handful of failed attempts, never more than MaxAttempts.
        var statuses = new List<int>();
        for (var i = 0; i < MaxAttempts; i++)
        {
            var attempt = await PostLoginAttempt(client, loginUrl, knownUsername, $"awva-wrong-password-{i}", ct);
            statuses.Add(attempt.status);
            lines.Add($"POST {loginUrl} attempt {i + 1}/{MaxAttempts} -> {attempt.status}");
        }
        var lockoutObserved = statuses.Distinct().Count() > 1 || statuses.Any(s => s == 429 || s == 423);

        var normalized = JsonSerializer.Serialize(new { loginUrl, enumerationSuspected, lockoutObserved, attemptStatuses = statuses });
        return ToolExecutionResult.Ok(string.Join("\n", lines), normalized);
    }

    private static async Task<(int status, int bodyLength)> PostLoginAttempt(HttpClient client, string loginUrl, string username, string password, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["username"] = username, ["password"] = password });
        using var response = await client.PostAsync(loginUrl, content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        return ((int)response.StatusCode, body.Length);
    }
}
