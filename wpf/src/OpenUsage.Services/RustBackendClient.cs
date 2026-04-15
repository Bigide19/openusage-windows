using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenUsage.Core.Interfaces;
using OpenUsage.Core.Models;

namespace OpenUsage.Services;

public sealed class RustBackendClient : IRustBackendClient
{
    private const string BaseUrl = "http://127.0.0.1:6736";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly HttpClient _httpClient;

    public RustBackendClient()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task<List<PluginOutput>> GetAllUsageAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"{BaseUrl}/v1/usage", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var snapshots = await response.Content
            .ReadFromJsonAsync<List<CachedSnapshot>>(JsonOptions, ct)
            .ConfigureAwait(false);
        return snapshots?.Select(ToPluginOutput).ToList() ?? new();
    }

    public async Task<PluginOutput?> GetUsageAsync(string providerId, CancellationToken ct = default)
    {
        var response = await _httpClient
            .GetAsync($"{BaseUrl}/v1/usage/{Uri.EscapeDataString(providerId)}", ct)
            .ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NoContent) return null;
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content
            .ReadFromJsonAsync<CachedSnapshot>(JsonOptions, ct)
            .ConfigureAwait(false);
        return snapshot is null ? null : ToPluginOutput(snapshot);
    }

    /// <summary>
    /// Wire-format from Rust local_http_api/cache.rs `CachedPluginSnapshot` (no iconUrl field —
    /// WPF gets icon data URLs from its own PluginManifestLoader instead).
    /// </summary>
    private sealed class CachedSnapshot
    {
        public string ProviderId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? Plan { get; set; }
        public List<MetricLine> Lines { get; set; } = new();
        public string? FetchedAt { get; set; }
    }

    private static PluginOutput ToPluginOutput(CachedSnapshot s) => new()
    {
        ProviderId = s.ProviderId,
        DisplayName = s.DisplayName,
        Plan = s.Plan,
        Lines = s.Lines,
        IconUrl = string.Empty, // populated by ViewModel via PluginMeta
    };
}
