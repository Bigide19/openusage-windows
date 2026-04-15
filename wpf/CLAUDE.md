# OpenUsage Windows

Windows port of [OpenUsage](https://github.com/robinebers/openusage) — a system tray app for tracking AI coding subscription usage.

## Tech Stack
- .NET 10, WPF, C#
- Jint (JavaScript engine for plugin runtime)
- CommunityToolkit.Mvvm (MVVM framework)
- H.NotifyIcon.Wpf (system tray)

## Project Structure
```
src/
  OpenUsage.Core/          # Models, Interfaces, Enums
  OpenUsage.PluginEngine/  # Jint JS runtime + Host API
  OpenUsage.Services/      # Settings, ProbeScheduler, HTTP API, HotKey
  OpenUsage.ViewModels/    # MVVM ViewModels
  OpenUsage.App/           # WPF application (startup project)
plugins/                   # Original JS plugins (unchanged from upstream)
```

## Build & Run
```bash
dotnet build
dotnet run --project src/OpenUsage.App
```

## Key Patterns
- Plugins are original JavaScript files from upstream, executed via Jint
- System tray popup window (400x550, borderless)
- Local HTTP API on 127.0.0.1:6736
- Settings persisted at %APPDATA%\OpenUsage\settings.json
