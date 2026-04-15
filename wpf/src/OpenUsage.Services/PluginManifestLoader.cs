using System.Text.Json;
using System.Text.Json.Serialization;

using OpenUsage.Core.Interfaces;
using OpenUsage.Core.Models;

namespace OpenUsage.Services;

/// <summary>
/// Loads plugin manifests + icon data from disk for UI display. Does not execute
/// plugin JS — that responsibility belongs to the Rust headless backend.
/// </summary>
public sealed class PluginManifestLoader : IPluginLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public List<LoadedPlugin> LoadPlugins(string pluginsDirectory)
    {
        var plugins = new List<LoadedPlugin>();

        if (!Directory.Exists(pluginsDirectory))
            return plugins;

        foreach (var dir in Directory.GetDirectories(pluginsDirectory))
        {
            var manifestPath = Path.Combine(dir, "plugin.json");
            if (!File.Exists(manifestPath))
                continue;

            try
            {
                var plugin = LoadSinglePlugin(dir, manifestPath);
                if (plugin is not null)
                    plugins.Add(plugin);
            }
            catch
            {
                // Skip plugins that fail to load
            }
        }

        plugins.Sort((a, b) => string.Compare(a.Manifest.Id, b.Manifest.Id, StringComparison.Ordinal));
        return plugins;
    }

    private static LoadedPlugin? LoadSinglePlugin(string pluginDir, string manifestPath)
    {
        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<PluginManifest>(json, JsonOptions);
        if (manifest is null)
            return null;

        var entryPath = Path.Combine(pluginDir, manifest.Entry);
        if (!File.Exists(entryPath))
            return null;

        // EntryScript is no longer used (Rust executes), but kept for model compat.
        var iconDataUrl = LoadIconDataUrl(pluginDir, manifest.Icon);

        return new LoadedPlugin
        {
            Manifest = manifest,
            PluginDir = pluginDir,
            EntryScript = string.Empty,
            IconDataUrl = iconDataUrl,
        };
    }

    private static string LoadIconDataUrl(string pluginDir, string? iconRelPath)
    {
        if (string.IsNullOrWhiteSpace(iconRelPath))
            return string.Empty;

        var iconPath = Path.Combine(pluginDir, iconRelPath);
        if (!File.Exists(iconPath))
            return string.Empty;

        var ext = Path.GetExtension(iconPath).ToLowerInvariant();
        var bytes = File.ReadAllBytes(iconPath);
        var base64 = Convert.ToBase64String(bytes);

        var mime = ext switch
        {
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream",
        };

        return $"data:{mime};base64,{base64}";
    }
}
