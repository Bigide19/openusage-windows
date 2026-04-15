using Microsoft.Extensions.Logging;
using OpenUsage.Core.Interfaces;
using OpenUsage.Core.Models;

namespace OpenUsage.Services;

/// <summary>
/// Polls the Rust headless backend's /v1/usage endpoint on a timer and emits
/// ProbeCompleted events. Rust runs the actual probes; we just consume the cache.
/// </summary>
public sealed class ProbeScheduler : IProbeScheduler
{
    private readonly IRustBackendClient _client;
    private readonly ILogger<ProbeScheduler> _logger;

    private PeriodicTimer? _timer;
    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;
    private TimeSpan _interval;
    private bool _disposed;

    public event EventHandler<PluginOutput>? ProbeCompleted;

    public ProbeScheduler(IRustBackendClient client, ILogger<ProbeScheduler> logger)
    {
        _client = client;
        _logger = logger;
    }

    public Task StartAsync(TimeSpan interval, CancellationToken ct = default)
    {
        _interval = interval < TimeSpan.FromSeconds(2) ? TimeSpan.FromSeconds(2) : interval;
        _loopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _timer = new PeriodicTimer(_interval);
        _loopTask = RunLoopAsync(_loopCts.Token);
        return Task.CompletedTask;
    }

    public void UpdateInterval(TimeSpan interval)
    {
        _interval = interval < TimeSpan.FromSeconds(2) ? TimeSpan.FromSeconds(2) : interval;

        var oldCts = _loopCts;
        var oldTimer = _timer;

        _loopCts = new CancellationTokenSource();
        _timer = new PeriodicTimer(_interval);
        _loopTask = RunLoopAsync(_loopCts.Token);

        oldCts?.Cancel();
        oldCts?.Dispose();
        oldTimer?.Dispose();
    }

    public async Task RunAllProbesAsync()
    {
        try
        {
            var snapshots = await _client.GetAllUsageAsync().ConfigureAwait(false);
            foreach (var output in snapshots)
                ProbeCompleted?.Invoke(this, output);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RunAllProbesAsync failed");
        }
    }

    public async Task RunProbeAsync(string pluginId)
    {
        try
        {
            var output = await _client.GetUsageAsync(pluginId).ConfigureAwait(false);
            if (output is not null)
                ProbeCompleted?.Invoke(this, output);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RunProbeAsync({PluginId}) failed", pluginId);
        }
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        try
        {
            await RunAllProbesAsync().ConfigureAwait(false);
            while (await _timer!.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                await RunAllProbesAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected on stop
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _loopCts?.Cancel();
        _timer?.Dispose();
        _loopCts?.Dispose();
    }
}
