using AWVA.Core;
using AWVA.Domain;
using Xunit;

namespace AWVA.Tests;

/// <summary>
/// Covers the scope-guard invariants Section 6 of the proposal calls out explicitly:
/// active tools require LabActive + the lab network, Nuclei's tag allowlist (and the
/// dos/fuzz exclusion outside the lab) is enforced in code, and crawler bounds are capped
/// independent of whatever the planner asks for.
/// </summary>
public class ScopeGuardTests
{
    private static ScopeGuard CreateGuard(IEnumerable<string>? allowedHosts = null) => new(new ScopeGuardOptions
    {
        AllowedHosts = (allowedHosts ?? new[] { "*.lab" }).ToList(),
        LabNetworkCidrs = new List<string> { "172.16.0.0/12" },
        NucleiTagAllowlist = new List<string> { "misconfig", "exposure", "tech", "ssl", "cve" },
        CrawlerMaxDepth = 3,
        CrawlerMaxPages = 200,
    });

    private static Target ApprovedTarget(string host, ScanProfile profile, string resolvedIp) => new()
    {
        Id = 1,
        Url = $"http://{host}",
        Host = host,
        Profile = profile,
        IsApproved = true,
        ResolvedIp = resolvedIp,
    };

    [Fact]
    public void ValidateToolCall_RefusesActiveToolOnPassiveProfile()
    {
        var guard = CreateGuard();
        var target = ApprovedTarget("juice-shop.lab", ScanProfile.Passive, "172.20.0.5");
        var scan = new Scan { Profile = ScanProfile.Passive };

        var decision = guard.ValidateToolCall(scan, target, ToolKind.Active, "active_scan", new Dictionary<string, object?>());

        Assert.False(decision.Allowed);
    }

    [Fact]
    public void ValidateToolCall_RefusesActiveToolWhenIpNotOnLabNetwork()
    {
        var guard = CreateGuard();
        var target = ApprovedTarget("juice-shop.lab", ScanProfile.LabActive, "203.0.113.9"); // public IP, not lab
        var scan = new Scan { Profile = ScanProfile.LabActive };

        var decision = guard.ValidateToolCall(scan, target, ToolKind.Active, "confirm_sqli", new Dictionary<string, object?>());

        Assert.False(decision.Allowed);
    }

    [Fact]
    public void ValidateToolCall_AllowsActiveToolOnLabActiveWithLabIp()
    {
        var guard = CreateGuard();
        var target = ApprovedTarget("juice-shop.lab", ScanProfile.LabActive, "172.20.0.5");
        var scan = new Scan { Profile = ScanProfile.LabActive };

        var decision = guard.ValidateToolCall(scan, target, ToolKind.Active, "confirm_sqli", new Dictionary<string, object?>());

        Assert.True(decision.Allowed);
    }

    [Fact]
    public void ValidateToolCall_RefusesDisallowedNucleiTag()
    {
        var guard = CreateGuard();
        var target = ApprovedTarget("juice-shop.lab", ScanProfile.Passive, "172.20.0.5");
        var scan = new Scan { Profile = ScanProfile.Passive };
        var args = new Dictionary<string, object?> { ["tags"] = new List<string> { "misconfig", "sqli" } };

        var decision = guard.ValidateToolCall(scan, target, ToolKind.Passive, "scan_misconfigurations", args);

        Assert.False(decision.Allowed);
    }

    [Fact]
    public void ValidateToolCall_RefusesDosFuzzTagsOutsideLab()
    {
        var guard = CreateGuard();
        guard = new ScopeGuard(new ScopeGuardOptions
        {
            AllowedHosts = new List<string> { "*.lab" },
            NucleiTagAllowlist = new List<string> { "misconfig", "dos", "fuzz" }, // even if misconfigured to allow them
        });
        var target = ApprovedTarget("juice-shop.lab", ScanProfile.Passive, "172.20.0.5");
        var scan = new Scan { Profile = ScanProfile.Passive };
        var args = new Dictionary<string, object?> { ["tags"] = new List<string> { "dos" } };

        var decision = guard.ValidateToolCall(scan, target, ToolKind.Passive, "scan_misconfigurations", args);

        Assert.False(decision.Allowed);
    }

    [Fact]
    public void ValidateToolCall_RefusesCrawlExceedingConfiguredBounds()
    {
        var guard = CreateGuard();
        var target = ApprovedTarget("juice-shop.lab", ScanProfile.Passive, "172.20.0.5");
        var scan = new Scan { Profile = ScanProfile.Passive };
        var args = new Dictionary<string, object?> { ["max_depth"] = 99 };

        var decision = guard.ValidateToolCall(scan, target, ToolKind.Passive, "crawl", args);

        Assert.False(decision.Allowed);
    }

    [Fact]
    public void ValidateToolCall_RefusesHostNotOnAllowlist()
    {
        var guard = CreateGuard(new[] { "*.lab" });
        var target = ApprovedTarget("evil.example.com", ScanProfile.Passive, "203.0.113.9");
        var scan = new Scan { Profile = ScanProfile.Passive };

        var decision = guard.ValidateToolCall(scan, target, ToolKind.Passive, "check_security_headers", new Dictionary<string, object?>());

        Assert.False(decision.Allowed);
    }

    [Theory]
    [InlineData("172.20.5.9", "172.16.0.0/12", true)]
    [InlineData("10.0.0.5", "172.16.0.0/12", false)]
    [InlineData("172.31.255.255", "172.16.0.0/12", true)]
    [InlineData("172.32.0.1", "172.16.0.0/12", false)]
    public void IsInCidrRange_MatchesExpected(string address, string cidr, bool expected)
    {
        var result = ScopeGuard.IsInCidrRange(System.Net.IPAddress.Parse(address), cidr);
        Assert.Equal(expected, result);
    }
}
