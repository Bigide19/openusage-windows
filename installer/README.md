# Windows Installer

NSIS script used by `.github/workflows/windows-installer.yml` to package the
Windows build into a single-file setup executable.

## Build locally

Requires [NSIS](https://nsis.sourceforge.io/) in `PATH`.

```powershell
# 1. Build the Rust headless backend
node copy-bundled.cjs
pushd src-tauri
cargo build --release --bin openusage
popd

# 2. Publish the WPF app (self-contained win-x64)
dotnet publish wpf/src/OpenUsage.App/OpenUsage.App.csproj `
  -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false `
  -o dist/payload

# 3. Stage the Rust binary under backend/
New-Item -ItemType Directory -Force dist/payload/backend | Out-Null
Copy-Item src-tauri/target/release/openusage.exe dist/payload/backend/
Copy-Item -Recurse src-tauri/resources dist/payload/backend/

# 4. Build the installer
pushd installer
makensis /DVERSION=0.0.0-dev openusage.nsi
popd

# Result: installer/OpenUsage-Setup-0.0.0-dev.exe
```

## Installer behavior

- **Per-user install** to `%LOCALAPPDATA%\OpenUsage` (no admin prompt).
- Registers with Add/Remove Programs under HKCU.
- Start Menu shortcut to `OpenUsage.exe`.
- Uninstaller kills `OpenUsage.exe` + `openusage.exe` before removing files.
