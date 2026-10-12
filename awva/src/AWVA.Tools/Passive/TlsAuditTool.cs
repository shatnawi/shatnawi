using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text.Json;
using AWVA.Core;
using AWVA.Domain;

namespace AWVA.Tools.Passive;

/// <summary>
/// audit_tls(host, port) (Section 4 step 2b, A02). Real but partial: negotiates TLS and
/// reports protocol version plus certificate validity window, using the BCL instead of
/// shelling out. The proposal specifies SSLyze for full cipher-suite enumeration — TODO:
/// replace/augment this with an SSLyze CLI JSON run (`sslyze --json_out=- host:port`) via
/// IExternalProcessRunner for cipher-level detail this class does not attempt.
/// </summary>
public sealed class TlsAuditTool : ToolBase
{
    public override string Name => "audit_tls";
    public override ToolKind Kind => ToolKind.Passive;
    public override IReadOnlyList<string> OwaspCategories => new[] { "A02" };

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolArguments arguments, CancellationToken ct = default)
    {
        var host = arguments.Get<string>("host") ?? throw new ArgumentException("audit_tls requires 'host'.");
        var port = arguments.Get<int>("port", 443);

        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port, ct);
        using var sslStream = new SslStream(tcpClient.GetStream(), leaveInnerStreamOpen: false);

        await sslStream.AuthenticateAsClientAsync(host);

        var cert = sslStream.RemoteCertificate;
        var notAfter = cert is System.Security.Cryptography.X509Certificates.X509Certificate2 x509 ? x509.NotAfter : (DateTime?)null;
        var isExpired = notAfter is { } exp && exp < DateTime.UtcNow;
        var isWeakProtocol = sslStream.SslProtocol is SslProtocols.Ssl2 or SslProtocols.Ssl3 or SslProtocols.Tls or SslProtocols.Tls11;

        var raw = $"TLS handshake with {host}:{port}\n" +
                  $"Protocol: {sslStream.SslProtocol}\n" +
                  $"CipherAlgorithm: {sslStream.CipherAlgorithm} ({sslStream.CipherStrength}-bit)\n" +
                  $"Certificate subject: {cert?.Subject}\n" +
                  $"Certificate not-after: {notAfter:O}";

        var normalized = JsonSerializer.Serialize(new
        {
            host,
            port,
            protocol = sslStream.SslProtocol.ToString(),
            isWeakProtocol,
            certificateNotAfter = notAfter,
            isExpired,
        });

        return ToolExecutionResult.Ok(raw, normalized);
    }
}
