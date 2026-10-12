using AWVA.Domain;

namespace AWVA.Api.Dtos;

public record SubmitTargetRequest(string Url, ScanProfile Profile);

public record TargetResponse(int Id, string Url, string Host, string? ResolvedIp, ScanProfile Profile, bool IsApproved, string? RejectionReason, DateTimeOffset CreatedAt)
{
    public static TargetResponse From(Target t) => new(t.Id, t.Url, t.Host, t.ResolvedIp, t.Profile, t.IsApproved, t.RejectionReason, t.CreatedAt);
}

public record StartScanRequest(int TargetId, int? StepBudget);

public record ScanResponse(int Id, int TargetId, ScanProfile Profile, ScanStatus Status, int StepBudget, int StepsUsed, DateTimeOffset CreatedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt)
{
    public static ScanResponse From(Scan s) => new(s.Id, s.TargetId, s.Profile, s.Status, s.StepBudget, s.StepsUsed, s.CreatedAt, s.StartedAt, s.CompletedAt);
}

public record ToolCallResponse(int Id, string ExternalCallId, string ToolName, string Status, string? RefusalReason, string? NormalizedOutput, DateTimeOffset CreatedAt)
{
    public static ToolCallResponse From(ToolCall t) => new(t.Id, t.ExternalCallId, t.ToolName, t.Status.ToString(), t.RefusalReason, t.NormalizedOutput, t.CreatedAt);
}

public record FindingResponse(
    int Id, string ExternalId, string Title, string Cwe, string OwaspCategory, string? WstgId,
    Severity Severity, SeveritySource SeveritySource, string? SeverityJustification,
    string Evidence, string? SourceToolCallExternalId, IReadOnlyList<string> AffectedUrls,
    string Remediation, Confidence Confidence, bool IsHallucination)
{
    public static FindingResponse From(Finding f) => new(
        f.Id, f.ExternalId, f.Title, f.Cwe, f.OwaspCategory, f.WstgId,
        f.Severity, f.SeveritySource, f.SeverityJustification,
        f.Evidence, f.SourceToolCall?.ExternalCallId,
        System.Text.Json.JsonSerializer.Deserialize<List<string>>(f.AffectedUrlsJson) ?? new(),
        f.Remediation, f.Confidence, f.IsHallucination);
}
