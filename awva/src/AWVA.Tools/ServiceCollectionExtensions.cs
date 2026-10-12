using AWVA.Core;
using AWVA.Tools.Active;
using AWVA.Tools.Passive;
using Microsoft.Extensions.DependencyInjection;

namespace AWVA.Tools;

/// <summary>Registers every ISecurityTool from Section 3/4/5 of the proposal, plus the
/// shared external-process runner used by the CLI-wrapper stubs.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAwvaTools(this IServiceCollection services)
    {
        services.AddSingleton<IExternalProcessRunner, ExternalProcessRunner>();

        // Passive tools (Passive profile; may run on any authorized target).
        services.AddSingleton<ISecurityTool, FingerprintTechTool>();
        services.AddSingleton<ISecurityTool, ComponentAuditTool>();
        services.AddSingleton<ISecurityTool, SecurityHeadersTool>();
        services.AddSingleton<ISecurityTool, CookieAuditTool>();
        services.AddSingleton<ISecurityTool, TlsAuditTool>();
        services.AddSingleton<ISecurityTool, CrawlerTool>();
        services.AddSingleton<ISecurityTool, MisconfigScanTool>();
        services.AddSingleton<ISecurityTool, OpenRedirectTool>();
        services.AddSingleton<ISecurityTool, JsSecretsScanTool>();

        // Active tools (LabActive profile only; enforced by AWVA.Core.ScopeGuard).
        services.AddSingleton<ISecurityTool, ActiveScanTool>();
        services.AddSingleton<ISecurityTool, SqliConfirmTool>();
        services.AddSingleton<ISecurityTool, AccessControlTestTool>();
        services.AddSingleton<ISecurityTool, AuthenticationTestTool>();
        services.AddSingleton<ISecurityTool, ErrorProberTool>();
        services.AddSingleton<ISecurityTool, JwtCheckTool>();
        services.AddSingleton<ISecurityTool, FileUploadTestTool>();
        services.AddSingleton<ISecurityTool, ApiScanTool>();
        services.AddSingleton<ISecurityTool, DesignReviewTool>();
        services.AddSingleton<ISecurityTool, LoggingCheckTool>();

        services.AddSingleton<IToolRegistry>(sp => new ToolRegistry(sp.GetServices<ISecurityTool>()));
        return services;
    }
}
