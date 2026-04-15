using OpenUsage.Core.Models;

namespace OpenUsage.Core.Interfaces;

public interface IProbeScheduler : IDisposable
{
    Task StartAsync(TimeSpan interval, CancellationToken ct = default);
    void UpdateInterval(TimeSpan interval);
    Task RunAllProbesAsync();
    Task RunProbeAsync(string pluginId);
    event EventHandler<PluginOutput>? ProbeCompleted;
}
