using OpenUsage.Core.Models;

namespace OpenUsage.Core.Interfaces;

public interface IProbeScheduler : IDisposable
{
    Task StartAsync(TimeSpan interval, CancellationToken ct = default);
    void UpdateInterval(TimeSpan interval);
    Task RunAllProbesAsync();
    Task RunProbeAsync(string pluginId);
    event EventHandler<PluginOutput>? ProbeCompleted;

    /// <summary>
    /// Fires after a full probe batch (all providers) finishes, carrying the
    /// next scheduled run time. Consumers use this to render "Next update in Xs".
    /// </summary>
    event EventHandler<DateTimeOffset>? BatchCompleted;
}
