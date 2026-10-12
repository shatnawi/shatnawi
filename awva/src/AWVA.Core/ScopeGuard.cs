using System.Net;
using AWVA.Domain;

namespace AWVA.Core;

/// <summary>
/// Configuration for the scope guard. In production this is bound from appsettings
/// ("ScopeGuard" section) / environment variables, never from anything the LLM says.
/// </summary>
public class ScopeGuardOptions
{
    /// <summary>Hostnames (or wildcard suffixes like "*.lab") authorized for scanning at all.</summary>
    public List<string> AllowedHosts { get; set; } = new();

    /// <summary>CIDR ranges that count as "on the lab Docker network", required for LabActive/active tools.</summary>
    public List<string> LabNetworkCidrs { get; set; } = new() { "172.16.0.0/12" };

    /// <summary>Nuclei tag allowlist (Section 6). "dos" and "fuzz" are excluded outside the lab regardless.</summary>
    public List<string> NucleiTagAllowlist { get; set; } = new() { "misconfig", "exposure", "tech", "ssl", "cve" };

    /// <summary>Max requests per minute per tool call, enforced independent of the model.</summary>
    public int RequestsPerMinute { get; set; } = 60;

    /// <summary>Crawler bounds (Section 6 "Crawler limits").</summary>
    public int CrawlerMaxDepth { get; set; } = 3;
    public int CrawlerMaxPages { get; set; } = 200;
}

public record ScopeDecision(bool Allowed, string? Reason = null)
{
    public static ScopeDecision Allow() => new(true);
    public static ScopeDecision Deny(string reason) => new(false, reason);
}

public interface IScopeGuard
{
    /// <summary>Validates a target at submission time (Section 4 step 1): host and resolved IP
    /// must match the allowlist; LabActive additionally requires the IP on the lab network.</summary>
    Task<ScopeDecision> ValidateTargetAsync(Target target, CancellationToken ct = default);

    /// <summary>Validates a single proposed tool call before it runs (Section 4 step 3 /
    /// Section 6): checks the tool's kind against the scan's profile, re-validates the
    /// target is still in scope, and enforces the Nuclei tag allowlist where applicable.
    /// This check is authoritative — the model's own judgement is never trusted.</summary>
    ScopeDecision ValidateToolCall(Scan scan, Target target, ToolKind toolKind, string toolName, IReadOnlyDictionary<string, object?> arguments);
}

public class ScopeGuard : IScopeGuard
{
    private readonly ScopeGuardOptions _options;

    public ScopeGuard(ScopeGuardOptions options)
    {
        _options = options;
    }

    public async Task<ScopeDecision> ValidateTargetAsync(Target target, CancellationToken ct = default)
    {
        if (!HostMatchesAllowlist(target.Host))
        {
            return ScopeDecision.Deny($"Host '{target.Host}' is not on the allowlist.");
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(target.Host, ct);
        }
        catch (Exception ex)
        {
            return ScopeDecision.Deny($"DNS resolution failed for '{target.Host}': {ex.Message}");
        }

        if (addresses.Length == 0)
        {
            return ScopeDecision.Deny($"Host '{target.Host}' did not resolve to any address.");
        }

        // Prevents DNS tricks / redirects to hosts outside the lab network even when the
        // hostname itself looks authorized (Section 6).
        if (target.Profile == ScanProfile.LabActive && !addresses.Any(IsOnLabNetwork))
        {
            return ScopeDecision.Deny(
                $"Lab-active profile requires the resolved IP to be on the lab Docker network; got {string.Join(", ", addresses.Select(a => a.ToString()))}.");
        }

        target.ResolvedIp = addresses[0].ToString();
        return ScopeDecision.Allow();
    }

    public ScopeDecision ValidateToolCall(
        Scan scan,
        Target target,
        ToolKind toolKind,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments)
    {
        if (!target.IsApproved)
        {
            return ScopeDecision.Deny("Target has not been approved by the scope guard.");
        }

        if (!HostMatchesAllowlist(target.Host))
        {
            return ScopeDecision.Deny($"Host '{target.Host}' is no longer on the allowlist.");
        }

        // Active tools (ZAP active scan, sqlmap, auth/access-control testers, error prober, ...)
        // refuse to run unless the scan's profile is LabActive. This is enforced in code on
        // every call, not only at submission time (Section 4: "the scope guard sits in front
        // of every tool call, not only at the start").
        if (toolKind == ToolKind.Active && scan.Profile != ScanProfile.LabActive)
        {
            return ScopeDecision.Deny($"Tool '{toolName}' is Active and the scan profile is {scan.Profile}, not LabActive.");
        }

        if (toolKind == ToolKind.Active &&
            (target.ResolvedIp is null || !IsOnLabNetwork(IPAddress.Parse(target.ResolvedIp))))
        {
            return ScopeDecision.Deny($"Tool '{toolName}' is Active and the target's resolved IP is not on the lab Docker network.");
        }

        if (string.Equals(toolName, "scan_misconfigurations", StringComparison.OrdinalIgnoreCase)
            && arguments.TryGetValue("tags", out var tagsObj)
            && tagsObj is IEnumerable<string> tags)
        {
            var disallowed = tags.Where(t => !_options.NucleiTagAllowlist.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList();
            // "dos" and "fuzz" are always excluded outside the lab, even if someone adds them
            // to the configured allowlist by mistake.
            var dangerousOutsideLab = tags.Where(t =>
                (string.Equals(t, "dos", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "fuzz", StringComparison.OrdinalIgnoreCase))
                && scan.Profile != ScanProfile.LabActive).ToList();
            if (disallowed.Count > 0)
            {
                return ScopeDecision.Deny($"Nuclei tags not on the allowlist: {string.Join(", ", disallowed)}.");
            }
            if (dangerousOutsideLab.Count > 0)
            {
                return ScopeDecision.Deny($"Nuclei tags '{string.Join(", ", dangerousOutsideLab)}' are excluded outside the lab.");
            }
        }

        if (string.Equals(toolName, "crawl", StringComparison.OrdinalIgnoreCase))
        {
            if (arguments.TryGetValue("max_depth", out var depthObj) && depthObj is int depth && depth > _options.CrawlerMaxDepth)
            {
                return ScopeDecision.Deny($"Crawl max_depth {depth} exceeds the configured limit of {_options.CrawlerMaxDepth}.");
            }
            if (arguments.TryGetValue("max_pages", out var pagesObj) && pagesObj is int pages && pages > _options.CrawlerMaxPages)
            {
                return ScopeDecision.Deny($"Crawl max_pages {pages} exceeds the configured limit of {_options.CrawlerMaxPages}.");
            }
        }

        return ScopeDecision.Allow();
    }

    private bool HostMatchesAllowlist(string host)
    {
        foreach (var pattern in _options.AllowedHosts)
        {
            if (pattern.StartsWith("*.", StringComparison.Ordinal))
            {
                var suffix = pattern[1..]; // ".lab"
                if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return true;
            }
            else if (string.Equals(pattern, host, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsOnLabNetwork(IPAddress address)
    {
        foreach (var cidr in _options.LabNetworkCidrs)
        {
            if (IsInCidrRange(address, cidr)) return true;
        }
        return false;
    }

    public static bool IsInCidrRange(IPAddress address, string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2) return false;
        if (!IPAddress.TryParse(parts[0], out var baseAddress)) return false;
        if (!int.TryParse(parts[1], out var prefixLength)) return false;
        if (address.AddressFamily != baseAddress.AddressFamily) return false;

        var addressBytes = address.GetAddressBytes();
        var baseBytes = baseAddress.GetAddressBytes();
        int fullBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;

        for (int i = 0; i < fullBytes; i++)
        {
            if (addressBytes[i] != baseBytes[i]) return false;
        }

        if (remainingBits > 0)
        {
            int mask = 0xFF << (8 - remainingBits);
            if ((addressBytes[fullBytes] & mask) != (baseBytes[fullBytes] & mask)) return false;
        }

        return true;
    }
}
