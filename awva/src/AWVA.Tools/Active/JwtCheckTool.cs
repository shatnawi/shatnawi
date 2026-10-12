using System.Text;
using System.Text.Json;
using AWVA.Core;
using AWVA.Domain;

namespace AWVA.Tools.Active;

/// <summary>
/// check_jwt(token) (Section 4 step 2e, A04/A08). Decodes the JWT header/payload (no
/// signature verification, no secret cracking) and flags the "alg: none" and
/// unsigned/weak-algorithm patterns called out in Section 5's A08 row. Full weak-secret
/// brute forcing is explicitly delegated to jwt_tool rather than reimplemented here —
/// TODO (Student 2): shell out to `jwt_tool &lt;token&gt; -C -d &lt;wordlist&gt;` via
/// IExternalProcessRunner for that part, bounded to the lab-provided wordlist.
/// </summary>
public sealed class JwtCheckTool : ToolBase
{
    private readonly IExternalProcessRunner _processRunner;
    public JwtCheckTool(IExternalProcessRunner processRunner) => _processRunner = processRunner;

    public override string Name => "check_jwt";
    public override ToolKind Kind => ToolKind.Active;
    public override IReadOnlyList<string> OwaspCategories => new[] { "A04", "A08" };

    public override Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
    {
        var token = arguments.Get<string>("token") ?? throw new ArgumentException("check_jwt requires 'token'.");
        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return Task.FromResult(ToolExecutionResult.Fail($"'{token}' does not look like a JWT (expected at least header.payload)."));
        }

        string headerJson;
        try
        {
            headerJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolExecutionResult.Fail($"Failed to base64url-decode JWT header: {ex.Message}"));
        }

        using var headerDoc = JsonDocument.Parse(headerJson);
        var alg = headerDoc.RootElement.TryGetProperty("alg", out var algProp) ? algProp.GetString() : null;
        var isAlgNone = string.Equals(alg, "none", StringComparison.OrdinalIgnoreCase);
        var isUnsigned = parts.Length < 3 || parts[2].Length == 0;

        var raw = $"JWT header: {headerJson}\nSignature segment present: {!isUnsigned}";
        var normalized = JsonSerializer.Serialize(new { alg, isAlgNone, isUnsigned, weakSecretCheck = "not run (see JwtCheckTool TODO: jwt_tool integration)" });
        return Task.FromResult(ToolExecutionResult.Ok(raw, normalized));
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }
}
