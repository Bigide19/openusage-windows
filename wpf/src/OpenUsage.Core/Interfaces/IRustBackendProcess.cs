namespace OpenUsage.Core.Interfaces;

/// <summary>
/// Manages the openusage.exe sidecar process running in --headless mode.
/// </summary>
public interface IRustBackendProcess : IAsyncDisposable
{
    /// <summary>Spawn the headless backend with the given enabled plugins and probe interval.</summary>
    Task SpawnAsync(IReadOnlyList<string> enabledPluginIds, int intervalSecs, CancellationToken ct = default);

    /// <summary>Poll /health until 200 OK or timeout.</summary>
    Task WaitForHealthyAsync(int timeoutMs = 30000, CancellationToken ct = default);

    /// <summary>Kill + respawn (used when settings change which plugins are enabled).</summary>
    Task RestartAsync(IReadOnlyList<string> enabledPluginIds, int intervalSecs);

    /// <summary>Stop the sidecar process gracefully.</summary>
    Task ShutdownAsync();
}
