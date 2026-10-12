using System.Diagnostics;

namespace AWVA.Tools;

public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);

/// <summary>
/// Thin wrapper around starting an external CLI tool (Nuclei, sqlmap, SSLyze, jwt_tool,
/// TruffleHog, Trivy, WhatWeb, Schemathesis, graphql-cop, ...). Every tool wrapper in this
/// namespace that needs an external binary goes through this interface instead of calling
/// System.Diagnostics.Process directly, so tests can fake it and so there is exactly one
/// place that shells out.
/// </summary>
public interface IExternalProcessRunner
{
    Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken ct = default);
}

public sealed class ExternalProcessRunner : IExternalProcessRunner
{
    public async Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in arguments) psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        process.Start();
        var stdOutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var stdErrTask = process.StandardError.ReadToEndAsync(cts.Token);
        await process.WaitForExitAsync(cts.Token);

        return new ProcessResult(process.ExitCode, await stdOutTask, await stdErrTask);
    }
}
