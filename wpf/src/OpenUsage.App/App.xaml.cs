using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenUsage.App.Helpers;
using OpenUsage.Core.Enums;
using OpenUsage.Core.Interfaces;
using OpenUsage.Core.Models;
using OpenUsage.Services;
using OpenUsage.ViewModels;
using OpenUsage.ViewModels.Messages;
using OpenUsage.App.Views;
using Serilog;

namespace OpenUsage.App;

public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexName = "Global\\OpenUsage_SingleInstance_F7A3B2";

    private IServiceProvider _services = null!;
    private TaskbarIcon? _trayIcon;
    private TrayPopupWindow? _popupWindow;
    private System.Threading.Timer? _settingsDebounceTimer;

    // Which provider's primary progress line drives the tray-icon number.
    // Defaults to the first enabled provider; re-evaluated when the user
    // enables/disables providers in settings.
    private string? _primaryProviderId;

    // "Used" shows 26 for a 26%-used quota; "Left" shows 74. Kept in sync
    // with settings so the tray icon matches the rest of the UI.
    private DisplayMode _displayMode = DisplayMode.Left;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        try
        {
            ConfigureLogging();
            _services = ConfigureServices();

            var settingsService = _services.GetRequiredService<ISettingsService>();
            var settings = await settingsService.LoadAsync();

            var pluginLoader = _services.GetRequiredService<IPluginLoader>();
            var pluginsDir = GetPluginsDirectory();
            var plugins = pluginLoader.LoadPlugins(pluginsDir);

            // Setup tray icon from XAML resource
            _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
            var trayVm = _services.GetRequiredService<TrayIconViewModel>();
            _trayIcon.DataContext = trayVm;
            _trayIcon.ForceCreate();

            trayVm.QuitRequested += () =>
            {
                _trayIcon?.Dispose();
                Shutdown();
            };

            // Left click toggles popup
            _trayIcon.TrayMouseDoubleClick += (_, _) => { /* ignore double click */ };
            _trayIcon.TrayLeftMouseUp += (_, _) => trayVm.TogglePanelCommand.Execute(null);

            var mainVm = _services.GetRequiredService<MainViewModel>();
            var overviewVm = mainVm.Overview;

            var metas = plugins.Select(p => new PluginMeta
            {
                Id = p.Manifest.Id,
                Name = p.Manifest.Name,
                IconUrl = p.IconDataUrl,
                BrandColor = p.Manifest.BrandColor,
                Lines = p.Manifest.Lines,
                Links = p.Manifest.Links,
                PrimaryCandidates = p.Manifest.Lines
                    .Where(l => l.PrimaryOrder.HasValue)
                    .OrderBy(l => l.PrimaryOrder)
                    .Select(l => l.Label)
                    .ToList()
            }).ToList();

            // First run: if no order configured, default to claude/codex/cursor enabled only
            var defaultEnabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "claude", "codex", "cursor" };
            if (settings.Plugins.Order.Count == 0)
            {
                settings.Plugins.Order = metas.Select(m => m.Id).ToList();
                settings.Plugins.Disabled = metas
                    .Where(m => !defaultEnabled.Contains(m.Id))
                    .Select(m => m.Id).ToList();
                _ = settingsService.SaveAsync(settings);
            }

            // Filter to enabled plugins only for overview
            var disabledSet = settings.Plugins.Disabled.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var enabledMetas = metas.Where(m => !disabledSet.Contains(m.Id)).ToList();
            overviewVm.Initialize(enabledMetas);

            // Store all metas for sidebar
            mainVm.PluginMetas = enabledMetas;

            // First enabled provider drives the tray-icon number. No UI yet
            // to let the user pick a different one — follow-up task.
            _primaryProviderId = enabledMetas.FirstOrDefault()?.Id;
            _displayMode = settings.DisplayMode;

            var settingsVm = mainVm.Settings;
            settingsVm.LoadSettings(settings, metas);

            _popupWindow = new TrayPopupWindow { DataContext = mainVm };

            WeakReferenceMessenger.Default.Register<PanelToggleMessage>(this, (_, _) =>
            {
                if (_popupWindow.IsVisible)
                    _popupWindow.HidePopup();
                else
                    _popupWindow.ShowPopup();
            });

            WeakReferenceMessenger.Default.Register<ShowAboutMessage>(this, (_, _) =>
            {
                var aboutVm = _services.GetRequiredService<AboutViewModel>();
                var aboutDialog = new AboutDialog { DataContext = aboutVm };
                aboutDialog.ShowDialog();
            });

            // Apply theme
            ApplyTheme(settings.ThemeMode);

            // Compute enabled plugin IDs from settings
            var enabledIds = metas
                .Where(m => !settings.Plugins.Disabled.Contains(m.Id))
                .Select(m => m.Id)
                .ToList();
            var intervalMinutes = (int)settings.AutoUpdateInterval;
            var intervalSecs = Math.Max(intervalMinutes * 60, 30);

            // Spawn Rust headless backend (plugin engine + HTTP API live there now)
            var rust = _services.GetRequiredService<IRustBackendProcess>();
            await rust.SpawnAsync(enabledIds, intervalSecs);
            await rust.WaitForHealthyAsync();

            // Start probe scheduler (polls Rust /v1/usage)
            var scheduler = _services.GetRequiredService<IProbeScheduler>();

            // Register event handler BEFORE starting scheduler (avoids race condition)
            scheduler.ProbeCompleted += (_, output) =>
            {
                Log.Information("[ProbeCompleted] {ProviderId}: {LineCount} lines, Plan={Plan}",
                    output.ProviderId, output.Lines.Count, output.Plan ?? "(null)");

                Dispatcher.Invoke(() =>
                {
                    overviewVm.UpdateProviderData(output.ProviderId, output);
                    WeakReferenceMessenger.Default.Send(new PluginDataUpdatedMessage(output.ProviderId, output));

                    if (string.Equals(output.ProviderId, _primaryProviderId, StringComparison.OrdinalIgnoreCase))
                    {
                        var meta = mainVm.PluginMetas.FirstOrDefault(m =>
                            string.Equals(m.Id, output.ProviderId, StringComparison.OrdinalIgnoreCase));
                        if (meta is not null)
                            UpdateTrayIconForPrimary(output, meta);
                    }
                });
            };

            // Drive the "Next update in Xs" footer label. Setting NextUpdateAt on
            // the UI thread so PropertyChanged fires on the right dispatcher.
            scheduler.BatchCompleted += (_, nextAt) =>
            {
                Dispatcher.Invoke(() => mainVm.NextUpdateAt = nextAt);
            };

            // Manual refresh (footer countdown click, provider card refresh, etc.)
            // was previously a no-op — the message was sent but never handled.
            WeakReferenceMessenger.Default.Register<RefreshRequestedMessage>(this, (_, msg) =>
            {
                _ = string.IsNullOrEmpty(msg.ProviderId)
                    ? scheduler.RunAllProbesAsync()
                    : scheduler.RunProbeAsync(msg.ProviderId!);
            });

            // Poll a bit faster than the Rust probe interval so UI updates appear promptly.
            var pollInterval = TimeSpan.FromSeconds(Math.Max(intervalSecs / 2, 5));
            _ = scheduler.StartAsync(pollInterval);

            // Register global hotkey
            if (!string.IsNullOrEmpty(settings.GlobalShortcut))
            {
                var hotKey = _services.GetRequiredService<IHotKeyService>();
                hotKey.Register(settings.GlobalShortcut);
                hotKey.HotKeyPressed += (_, _) =>
                {
                    Dispatcher.Invoke(() => WeakReferenceMessenger.Default.Send(new PanelToggleMessage()));
                };
            }

            // Initial probe
            _ = scheduler.RunAllProbesAsync();

            // Handle settings changes — debounce to avoid rebuilding on rapid toggles
            settingsVm.SettingsModified += newSettings =>
            {
                // Apply theme immediately for instant visual feedback
                Dispatcher.Invoke(() => ApplyTheme(newSettings.ThemeMode));

                _settingsDebounceTimer?.Dispose();
                _settingsDebounceTimer = new System.Threading.Timer(async _ =>
                {
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        await settingsService.SaveAsync(newSettings);

                        // Rebuild overview with new enabled plugins
                        var newDisabledSet = newSettings.Plugins.Disabled.ToHashSet(StringComparer.OrdinalIgnoreCase);
                        var newEnabledMetas = metas.Where(m => !newDisabledSet.Contains(m.Id)).ToList();
                        overviewVm.Initialize(newEnabledMetas);
                        mainVm.PluginMetas = newEnabledMetas;
                        _primaryProviderId = newEnabledMetas.FirstOrDefault()?.Id;
                        _displayMode = newSettings.DisplayMode;

                        // Restart Rust backend with new enabled set (cheapest live-reload path)
                        var newEnabledIds = newEnabledMetas.Select(m => m.Id).ToList();
                        var newIntervalSecs = Math.Max((int)newSettings.AutoUpdateInterval * 60, 30);
                        try
                        {
                            await rust.RestartAsync(newEnabledIds, newIntervalSecs);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "[rust] restart after settings change failed");
                        }
                        _ = scheduler.RunAllProbesAsync();
                    });
                }, null, 500, Timeout.Infinite);
            };
        }
        catch (Exception ex)
        {
            var crashLog = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OpenUsage", "crash.log");
            Directory.CreateDirectory(Path.GetDirectoryName(crashLog)!);
            File.WriteAllText(crashLog, $"{DateTime.Now}\n{ex}");
            MessageBox.Show($"Startup error:\n\n{ex.Message}\n\nSee: {crashLog}", "OpenUsage Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();

        // Stop the Rust sidecar before tearing down DI
        try
        {
            var rust = _services?.GetService<IRustBackendProcess>();
            if (rust is not null)
                rust.ShutdownAsync().GetAwaiter().GetResult();
        }
        catch { /* best effort */ }

        if (_services is IDisposable disposable)
            disposable.Dispose();

        try { _mutex?.ReleaseMutex(); } catch { /* not owned */ }
        _mutex?.Dispose();

        base.OnExit(e);
    }

    private static void ConfigureLogging()
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenUsage", "logs", "openusage-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                retainedFileCountLimit: 7)
            .CreateLogger();
    }

    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddSerilog(dispose: true));

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IPluginLoader, PluginManifestLoader>();
        services.AddSingleton<IRustBackendProcess, RustBackendProcess>();
        services.AddSingleton<IRustBackendClient, RustBackendClient>();
        services.AddSingleton<IHotKeyService, HotKeyService>();
        services.AddSingleton<IProbeScheduler, ProbeScheduler>();
        services.AddSingleton<ProxyService>();
        services.AddSingleton<AutoStartService>();
        services.AddSingleton<ThemeService>();

        services.AddHttpClient("PluginHttp")
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var proxy = sp.GetRequiredService<ProxyService>();
                return proxy.CreateHandler();
            });

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<OverviewViewModel>();
        services.AddSingleton<ProviderDetailViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<TrayIconViewModel>();
        services.AddSingleton<AboutViewModel>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Picks the primary progress line (per <see cref="PluginMeta.PrimaryCandidates"/>
    /// with a fallback to the first progress line) and repaints the tray
    /// icon with its percentage. Leaves the default icon untouched if the
    /// primary metric isn't a percentage — showing "$12" in a 32×32 icon
    /// reads worse than a brand glyph.
    /// </summary>
    private void UpdateTrayIconForPrimary(PluginOutput output, PluginMeta meta)
    {
        if (_trayIcon is null) return;

        var progressLines = output.Lines.OfType<ProgressMetricLine>().ToList();
        if (progressLines.Count == 0) return;

        ProgressMetricLine? primary = null;
        foreach (var candidate in meta.PrimaryCandidates)
        {
            primary = progressLines.FirstOrDefault(p =>
                string.Equals(p.Label, candidate, StringComparison.OrdinalIgnoreCase));
            if (primary is not null) break;
        }
        primary ??= progressLines[0];

        if (primary.Format.Kind != ProgressFormatKind.Percent)
            return;

        // Plugins always emit Used. "Left" mode is a purely cosmetic flip in
        // the UI — if the user picks it in settings, the tray should show
        // remaining quota too or the tray and the panel will disagree.
        var rawUsed = primary.Used;
        var display = _displayMode == DisplayMode.Left
            ? Math.Max(0, primary.Limit - rawUsed)
            : rawUsed;
        var percent = (int)Math.Round(display);

        var brandColor = TrayIconRenderer.ParseBrandColor(meta.BrandColor);
        _trayIcon.IconSource = TrayIconRenderer.RenderPercent(percent, brandColor, meta.IconUrl);
        _trayIcon.ToolTipText = $"OpenUsage · {meta.Name} {primary.Label} {percent}% ({_displayMode.ToString().ToLowerInvariant()})";
    }

    private string GetPluginsDirectory()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var pluginsDir = Path.Combine(baseDir, "plugins");
        if (Directory.Exists(pluginsDir))
            return pluginsDir;

        var devDir = Path.Combine(Directory.GetCurrentDirectory(), "plugins");
        if (Directory.Exists(devDir))
            return devDir;

        var repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "plugins");
    }

    private void ApplyTheme(Core.Enums.ThemeMode mode)
    {
        var actualMode = mode;
        if (mode == Core.Enums.ThemeMode.System)
        {
            var themeService = _services.GetRequiredService<ThemeService>();
            actualMode = themeService.GetSystemTheme();
        }

        var themeUri = actualMode == Core.Enums.ThemeMode.Light
            ? new Uri("Themes/LightTheme.xaml", UriKind.Relative)
            : new Uri("Themes/DarkTheme.xaml", UriKind.Relative);

        Resources.MergedDictionaries.Clear();
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
    }
}
