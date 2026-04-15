using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenUsage.Core.Models;

namespace OpenUsage.Services;

public sealed class ProxyService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".openusage",
        "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public HttpClientHandler CreateHandler()
    {
        var handler = new HttpClientHandler();
        var config = LoadConfig();

        if (config is { Enabled: true, Url: not null })
        {
            handler.Proxy = new WebProxy(config.Url);
            handler.UseProxy = true;
        }

        return handler;
    }

    private static ProxyConfig? LoadConfig()
    {
        if (!File.Exists(ConfigPath))
            return null;

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<ProxyConfig>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
