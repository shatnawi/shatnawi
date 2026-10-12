using AWVA.Core.Planning;
using AWVA.Core.Triage;
using AWVA.Domain;
using Xunit;

namespace AWVA.Tests;

/// <summary>
/// Covers Section 4 step 5 / Section 6's hallucination rule: "Any finding whose
/// source_tool_call does not exist, or whose evidence does not appear in that call's
/// output, is rejected as a hallucination."
/// </summary>
public class FindingValidatorTests
{
    private static ToolCall MakeCall(string externalId, string rawOutput) => new()
    {
        ExternalCallId = externalId,
        ToolName = "check_security_headers",
        ArgumentsJson = "{}",
        RawOutput = rawOutput,
    };

    private static DraftFinding MakeDraft(string sourceCallId, string evidence) => new()
    {
        ExternalId = "HDR-001",
        Title = "Missing Content-Security-Policy header",
        Cwe = "CWE-693",
        OwaspCategory = "A02:2025 Security Misconfiguration",
        Severity = Severity.Medium,
        Evidence = evidence,
        SourceToolCallExternalId = sourceCallId,
        AffectedUrls = new[] { "/" },
        Remediation = "Add a Content-Security-Policy header.",
        Confidence = Confidence.High,
    };

    [Fact]
    public void Validate_AcceptsFindingWhenEvidenceMatchesRawOutput()
    {
        var history = new[] { MakeCall("call_01", "content-security-policy absent in response to GET /") };
        var draft = MakeDraft("call_01", "content-security-policy absent in response to GET /");

        var result = FindingValidator.Validate(draft, history);

        Assert.False(result.IsHallucination);
    }

    [Fact]
    public void Validate_RejectsFindingWhenSourceToolCallDoesNotExist()
    {
        var history = new[] { MakeCall("call_01", "content-security-policy absent in response to GET /") };
        var draft = MakeDraft("call_99", "content-security-policy absent in response to GET /");

        var result = FindingValidator.Validate(draft, history);

        Assert.True(result.IsHallucination);
    }

    [Fact]
    public void Validate_RejectsFindingWhenEvidenceNotInRawOutput()
    {
        var history = new[] { MakeCall("call_01", "GET / -> 200\nServer: nginx") };
        var draft = MakeDraft("call_01", "content-security-policy absent in response to GET /");

        var result = FindingValidator.Validate(draft, history);

        Assert.True(result.IsHallucination);
    }
}
