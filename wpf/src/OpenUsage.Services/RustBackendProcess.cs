using System.Diagnostics;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using OpenUsage.Core.Interfaces;

namespace OpenUsage.Services;

public sealed class RustBackendProcess : IRustBackendProcess
{
    private const string HealthUrl = "http://127.0.0.1:6736/health";
    private const string ExeFileName = "openusage.exe";

    private readonly ILogger<RustBackendProcess> _logger;
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(2) };
    private Process? _process;
    private IReadOnlyList<string> _lastEnabledIds = Array.Empty<string>();
    private int _lastIntervalSecs = 60;

    public RustBackendProcess(ILogger<RustBackendProcess> logger)
    {
        _logger = logger;
    }

    public Task SpawnAsync(IReadOnlyList<string> enabledPluginIds, int intervalSecs, CancellationToken ct = default)
    {
        var exePath = GetExePath();
        if (!File.Exists(exePath))
            throw new FileNotFoundException($"Rust backend binary not found: {exePath}");

        _lastEnabledIds = enabledPluginIds;
        _lastIntervalSecs = intervalSecs;

        var args = new List<string> { "--headless" };
        if (enabledPluginIds.Count > 0)
        {
            args.Add("--enabled");
            args.Add(string.Join(",", enabledPluginIds));
        }
        args.Add("--interval-secs");
        args.Add(intervalSecs.ToString());

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process.OutputDataReceived += (_, e) => { if (e.Data != null) _logger.LogDebug("[rust:stdout] {Line}", e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data != null) _logger.LogInformation("[rust] {Line}", e.Data); };
        _process.Exited += (_, _) => _logger.LogWarning("[rust] backend exited unexpectedly (code={Code})", _process?.ExitCode);

        if (!_process.Start())
            throw new InvalidOperationException("Failed to start Rust backend");

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        _logger.LogInformation("[rust] spawned PID={Pid} exe={Exe} args={Args}",
            _process.Id, exePath, string.Join(" ", args));

        return Task.CompletedTask;
    }

    public async Task WaitForHealthyAsync(int timeoutMs = 30000, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMs);

        var sw = Stopwatch.StartNew();
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                using var response = await _httpClient.GetAsync(HealthUrl, cts.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[rust] healthy after {Ms}ms", sw.ElapsedMilliseconds);
                    return;
                }
            }
            catch
            {
                // not ready yet; retry
            }

            try { await Task.Delay(150, cts.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }

        throw new TimeoutException($"Rust backend did not become healthy within {timeoutMs}ms");
    }

    public async Task RestartAsync(IReadOnlyList<string> enabledPluginIds, int intervalSecs)
    {
        await ShutdownAsync().ConfigureAwait(false);
        await SpawnAsync(enabledPluginIds, intervalSecs).ConfigureAwait(false);
        await WaitForHealthyAsync().ConfigureAwait(false);
    }

    public async Task ShutdownAsync()
    {
        if (_process is null) return;
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync().ConfigureAwait(false);
            }
            _logger.LogInformation("[rust] shut down");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[rust] shutdown error");
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync().ConfigureAwait(false);
        _httpClient.Dispose();
    }

    private static string GetExePath()
    {
        // Rust sidecar lives in a subdirectory to avoid OpenUsage.exe / openusage.exe
        // case-insensitive filename collision with the WPF assembly.
        var baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, "backend", ExeFileName);
    }
}
