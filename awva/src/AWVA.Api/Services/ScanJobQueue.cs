using System.Collections.Concurrent;
using System.Threading.Channels;
using AWVA.Core;
using AWVA.Domain;
using Microsoft.EntityFrameworkCore;
using AWVA.Data;

namespace AWVA.Api.Services;

/// <summary>
/// ASP.NET analog of the proposal's "Job queue: asyncio tasks (Celery/RQ optional) ...
/// Scans run in the background while the UI polls or streams progress" (Section 3). A
/// Channel-backed queue plus one BackgroundService keeps this dependency-free; swap for
/// Hangfire/Azure Queues if the team needs multi-instance scaling.
/// </summary>
public interface IScanJobQueue
{
    void Enqueue(int scanId);
    IAsyncEnumerable<int> DequeueAllAsync(CancellationToken ct);
}

public sealed class ScanJobQueue : IScanJobQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

    public void Enqueue(int scanId) => _channel.Writer.TryWrite(scanId);

    public IAsyncEnumerable<int> DequeueAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}

/// <summary>
/// Per-scan kill switch (Section 6: "Step and time budgets on the agent loop; a kill
/// switch in the UI"). ScansController.Abort cancels the token for a running scan; the
/// background worker passes it into AgentOrchestrator.RunAsync.
/// </summary>
public interface IScanCancellationRegistry
{
    CancellationTokenSource GetOrCreate(int scanId);
    bool TryCancel(int scanId);
    void Remove(int scanId);
}

public sealed class ScanCancellationRegistry : IScanCancellationRegistry
{
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _tokens = new();

    public CancellationTokenSource GetOrCreate(int scanId) => _tokens.GetOrAdd(scanId, _ => new CancellationTokenSource());

    public bool TryCancel(int scanId)
    {
        if (_tokens.TryGetValue(scanId, out var cts))
        {
            cts.Cancel();
            return true;
        }
        return false;
    }

    public void Remove(int scanId) => _tokens.TryRemove(scanId, out _);
}

/// <summary>Drains the queue and runs the agent loop for each scan, outside the request pipeline.</summary>
public sealed class ScanBackgroundWorker : BackgroundService
{
    private readonly IScanJobQueue _queue;
    private readonly IScanCancellationRegistry _cancellationRegistry;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScanBackgroundWorker> _logger;

    public ScanBackgroundWorker(
        IScanJobQueue queue,
        IScanCancellationRegistry cancellationRegistry,
        IServiceScopeFactory scopeFactory,
        ILogger<ScanBackgroundWorker> logger)
    {
        _queue = queue;
        _cancellationRegistry = cancellationRegistry;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var scanId in _queue.DequeueAllAsync(stoppingToken))
        {
            var scanCts = _cancellationRegistry.GetOrCreate(scanId);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, scanCts.Token);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IAgentOrchestrator>();
                await orchestrator.RunAsync(scanId, linked.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Scan {ScanId} was aborted.", scanId);
                await MarkAborted(scanId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scan {ScanId} failed.", scanId);
                await MarkFailed(scanId, stoppingToken);
            }
            finally
            {
                _cancellationRegistry.Remove(scanId);
            }
        }
    }

    private async Task MarkAborted(int scanId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwvaDbContext>();
        var scan = await db.Scans.FindAsync(new object?[] { scanId }, ct);
        if (scan is not null)
        {
            scan.Status = ScanStatus.Aborted;
            scan.CompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task MarkFailed(int scanId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwvaDbContext>();
        var scan = await db.Scans.FindAsync(new object?[] { scanId }, ct);
        if (scan is not null)
        {
            scan.Status = ScanStatus.Failed;
            scan.CompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
