# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Windows port of [robinebers/openusage](https://github.com/robinebers/openusage) — a system tray app for tracking AI coding subscription usage. Native WPF (.NET 10) frontend drives the original Rust plugin engine as a headless sidecar over HTTP.

## Architecture

Two-process model:
- **WPF app** (`wpf/`) — UI, system tray, settings, hotkey. Spawns and manages the Rust process.
- **Rust sidecar** (`src-tauri/`) — Plugin engine (QuickJS sandbox) + HTTP API on `127.0.0.1:6736`. Runs in `--headless` mode (no Tauri UI).

Communication: WPF polls `GET /v1/usage` from the Rust HTTP server. No IPC pipes.

Startup sequence (App.xaml.cs): single-instance mutex → Serilog → DI → load plugin manifests → load settings → spawn Rust `--headless` → wait `/health` (30s timeout) → start ProbeScheduler → register hotkey → initial probe → apply theme.

### WPF Layer (`wpf/src/`)

```
OpenUsage.App/         # Startup project (WinExe, net10.0-windows)
OpenUsage.Core/        # Models, interfaces, enums (no dependencies)
OpenUsage.Services/    # RustBackendProcess, RustBackendClient, ProbeScheduler, SettingsService
OpenUsage.ViewModels/  # CommunityToolkit.Mvvm ObservableObjects + WeakReferenceMessenger
```

Key patterns:
- MVVM with `[ObservableProperty]`, `[RelayCommand]`, source generators
- Cross-VM communication via `WeakReferenceMessenger` (NavigateToProvider, RefreshRequested, PanelToggle, PluginDataUpdated)
- System tray: `H.NotifyIcon.Wpf` — requires ICO format (not PNG/BitmapSource); see `TrayIconRenderer.cs`
- Themes: `Themes/LightTheme.xaml` and `DarkTheme.xaml` resource dictionaries swapped at runtime
- Settings persisted at `%APPDATA%\OpenUsage\settings.json`; logs at `%APPDATA%\OpenUsage\logs\`

### Rust Layer (`src-tauri/src/`)

```
main.rs           # CLI dispatch: --headless → headless::run(), else Tauri UI (macOS)
headless.rs       # Headless entry: plugin init, HTTP server start, probe loop
plugin_engine/    # manifest.rs (plugin.json), runtime.rs (QuickJS exec), host_api.rs (ctx.*)
local_http_api/   # server.rs (raw TCP on :6736), cache.rs (in-memory snapshots)
config.rs         # Proxy config from ~/.openusage/config.json
```

The `lib.rs` and macOS-specific dependencies (tauri-nspanel, objc2-*) are upstream remnants — not used by the Windows build but kept for plugin compatibility.

### Plugins (`plugins/`)

Shared with upstream. Each plugin: `plugin.json` (manifest) + `plugin.js` (entry) + `icon.svg`. Executed in fresh QuickJS sandboxes per probe. Host API injected as `ctx` object — see `docs/plugins/api.md`.

Bundled into the Rust binary via `node copy-bundled.cjs` → `src-tauri/resources/bundled_plugins/`.

## Build Commands

```bash
# Bundle JS plugins into Rust resources (required before Rust build)
node copy-bundled.cjs

# Build Rust headless backend
cargo build --manifest-path src-tauri/Cargo.toml --release --bin openusage

# Run WPF app (auto-copies Rust binary via post-build MSBuild target)
dotnet run --project wpf/src/OpenUsage.App

# Build WPF for release
dotnet publish wpf/src/OpenUsage.App/OpenUsage.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false

# Build NSIS installer (requires makensis on PATH)
cd installer && makensis /DVERSION=0.1.1 openusage.nsi
```

Rust build requires LLVM (for rquickjs bindgen). The post-build target in `OpenUsage.App.csproj` copies `src-tauri/target/release/openusage.exe` → `$(OutDir)/backend/` automatically.

## CI/CD

`publish-windows.yml` — triggered by `v*-win` tags (e.g. `v0.1.1-win`) or manual dispatch. Builds Rust + WPF + NSIS installer, uploads to GitHub Release (draft).

## Key Gotchas

- **EXE name collision**: WPF produces `OpenUsage.exe`, Rust produces `openusage.exe`. Windows is case-insensitive, so Rust binary lives in `backend/` subdirectory.
- **Windows .cmd shims**: `npx`, `bunx` etc. on Windows are `.cmd` files. Rust `Command::new("npx")` won't find them — `host_api.rs` appends `.cmd` suffix on `#[cfg(windows)]`.
- **H.NotifyIcon**: Only accepts ICO format via `UriSource` (not `StreamSource`, not `RenderTargetBitmap`). `TrayIconRenderer` writes a minimal ICO (ICONDIR + ICONDIRENTRY + PNG payload) to a temp file.
- **Plugin SVG icons**: Must use `currentColor` for theming. Brand color set in `plugin.json`.
- **DisplayMode**: `Used` shows raw usage (26%), `Left` shows remaining (74%). Default is `Left`. Tray icon and UI must agree — stored in `_displayMode` field in `App.xaml.cs`.
