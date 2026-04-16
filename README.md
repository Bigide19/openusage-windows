# OpenUsage for Windows

Track all your AI coding subscriptions from the Windows system tray. Fork of
[robinebers/openusage](https://github.com/robinebers/openusage) with a native
.NET / WPF frontend driving the original Rust plugin engine as a headless
sidecar.

![OpenUsage Screenshot](screenshot.png)

## Download

- **Windows** — [Latest installer](https://github.com/Bigide19/openusage-windows/releases/latest)
  (NSIS setup, per-user install under `%LOCALAPPDATA%\OpenUsage`, no admin required)
- **macOS / Linux** — use the [upstream release](https://github.com/robinebers/openusage/releases/latest)

> Auto-update is not yet wired in the Windows installer — pull a new version
> from the releases page when you want to upgrade.

## What It Does

OpenUsage lives in your system tray and shows how much of your AI coding
subscriptions you've used. Progress bars, badges, clear labels — no mental math.

- **One glance.** All your AI tools, one panel.
- **Always up-to-date.** Refreshes automatically on a schedule you pick, or click
  the "Next update in …" countdown to refresh immediately.
- **Global shortcut.** Toggle the panel from anywhere with a customizable hotkey.
- **Lightweight.** Opens instantly, stays out of your way.
- **Plugin-based.** New providers get added without updating the whole app.
- **[Local HTTP API](docs/local-http-api.md).** Other apps can read your usage
  data from `127.0.0.1:6736`.
- **[Proxy support](docs/proxy.md).** Route provider HTTP requests through a
  SOCKS5 or HTTP proxy.

## Optional: per-day token breakdown (Claude / Codex)

The Claude and Codex providers include an extra "Today / Yesterday / Last 30
Days" breakdown that reads your local conversation logs (`~/.claude/`,
`~/.codex/`) via [`ccusage`](https://www.npmjs.com/package/ccusage).

To enable those lines, install **one** of the following so OpenUsage can spawn
`ccusage`:

- [Node.js 20+](https://nodejs.org/) (provides `npx` — simplest)
- [Bun](https://bun.sh/) (provides `bunx` — faster)

If neither is on `PATH`, OpenUsage still works — you just won't see the per-day
lines. All other providers (Cursor, Copilot, Gemini, …) and Claude's
session/weekly quotas are unaffected.

## Supported Providers

- [**Amp**](docs/providers/amp.md) / free tier, bonus, credits
- [**Antigravity**](docs/providers/antigravity.md) / all models
- [**Claude**](docs/providers/claude.md) / session, weekly, peak/off-peak, extra usage, local token usage (ccusage)
- [**Codex**](docs/providers/codex.md) / session, weekly, reviews, credits
- [**Copilot**](docs/providers/copilot.md) / premium, chat, completions
- [**Cursor**](docs/providers/cursor.md) / credits, total usage, auto usage, API usage, on-demand, CLI auth
- [**Factory / Droid**](docs/providers/factory.md) / standard, premium tokens
- [**Gemini**](docs/providers/gemini.md) / pro, flash, workspace/free/paid tier
- [**JetBrains AI Assistant**](docs/providers/jetbrains-ai-assistant.md) / quota, remaining
- [**Kiro**](docs/providers/kiro.md) / credits, bonus credits, overages
- [**Kimi Code**](docs/providers/kimi.md) / session, weekly
- [**MiniMax**](docs/providers/minimax.md) / coding plan session
- [**OpenCode Go**](docs/providers/opencode-go.md) / 5h, weekly, monthly spend limits
- [**Windsurf**](docs/providers/windsurf.md) / prompt credits, flex credits
- [**Z.ai**](docs/providers/zai.md) / session, weekly, web searches

Plugins are shared with upstream — a new provider added there lands here on the
next sync. Want a provider that's not listed? Open it against
[robinebers/openusage](https://github.com/robinebers/openusage/issues/new).

## How to Contribute

Where to report depends on what's broken:

- **Windows UI / installer / hotkey bug** → file here:
  [Bigide19/openusage-windows/issues](https://github.com/Bigide19/openusage-windows/issues)
- **Plugin / provider / data issue** → file upstream:
  [robinebers/openusage/issues](https://github.com/robinebers/openusage/issues)
- **Add a new provider** → see the [Plugin API](docs/plugins/api.md) and PR
  upstream (plugins are JS and vendored from the upstream repo).

PRs welcome. Keep it simple, include before/after screenshots for UI changes,
and run `dotnet build` before submitting.

## Credits

- Upstream project and plugin ecosystem: [robinebers/openusage](https://github.com/robinebers/openusage)
- Original idea: [CodexBar](https://github.com/steipete/CodexBar) by [@steipete](https://github.com/steipete)

## License

[MIT](LICENSE)

---

## Build from source

> **Warning:** the `main` branch tracks in-progress work. Use tagged releases
> for stable builds.

### Stack

- **Frontend:** .NET 10 WPF app (`wpf/`) — see [`wpf/CLAUDE.md`](wpf/CLAUDE.md)
- **Backend:** Rust headless sidecar (`src-tauri/`) running the plugin engine +
  local HTTP API on `127.0.0.1:6736`
- **Installer:** NSIS script (`installer/`) — see [`installer/README.md`](installer/README.md)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Rust (stable)](https://www.rust-lang.org/tools/install) with the `x86_64-pc-windows-msvc` target
- [Node.js 20+](https://nodejs.org/) (only for bundling plugins into the Rust binary)
- [NSIS](https://nsis.sourceforge.io/) if you want to build the installer

### Dev loop

```powershell
# Bundle plugins into the Rust resources dir
node copy-bundled.cjs

# Build the Rust headless backend
cargo build --manifest-path src-tauri/Cargo.toml --release --bin openusage

# Run the WPF app (spawns the Rust backend as a child process)
dotnet run --project wpf/src/OpenUsage.App
```

Settings are persisted at `%APPDATA%\OpenUsage\settings.json`; logs at
`%APPDATA%\OpenUsage\logs\`.

### Release build + installer

See [`installer/README.md`](installer/README.md) for the NSIS packaging steps,
or trigger the `windows-installer` GitHub Actions workflow on a tag.
