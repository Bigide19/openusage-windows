using OpenUsage.Core.Models;

namespace OpenUsage.Core.Interfaces;

/// <summary>
/// HTTP client for the Rust headless backend's /v1/usage endpoints.
/// </summary>
public interface IRustBackendClient
{
    /// <summary>GET /v1/usage — list of all enabled provider snapshots.</summary>
    Task<List<PluginOutput>> GetAllUsageAsync(CancellationToken ct = default);

    /// <summary>GET /v1/usage/{providerId} — single provider snapshot, or null if not cached yet.</summary>
    Task<PluginOutput?> GetUsageAsync(string providerId, CancellationToken ct = default);
}
