//! Headless entry point: runs the plugin engine + local HTTP API without
//! initializing the Tauri UI. Used by the Windows WPF frontend, which spawns
//! this binary as a sidecar process and consumes the HTTP API on 127.0.0.1:6736.
//!
//! Invoke with `openusage.exe --headless [--enabled id1,id2] [--interval-secs 60]`.

use std::path::PathBuf;
use std::time::Duration;

use crate::local_http_api;
use crate::plugin_engine;

pub struct HeadlessConfig {
    /// If empty, all bundled plugins are enabled.
    pub enabled_plugin_ids: Vec<String>,
    /// Probe loop interval in seconds. Floored to 5s.
    pub interval_secs: u64,
    pub app_version: String,
}

pub fn run(cfg: HeadlessConfig) -> std::io::Result<()> {
    // Resolve paths without Tauri's path resolver.
    let app_data_dir = dirs::config_dir()
        .ok_or_else(|| std::io::Error::new(std::io::ErrorKind::NotFound, "no config dir"))?
        .join("OpenUsage");
    std::fs::create_dir_all(&app_data_dir)?;

    let exe_dir = std::env::current_exe()?
        .parent()
        .map(PathBuf::from)
        .ok_or_else(|| std::io::Error::new(std::io::ErrorKind::NotFound, "no exe dir"))?;

    eprintln!("[headless] app_data_dir={}", app_data_dir.display());
    eprintln!("[headless] exe_dir={}", exe_dir.display());
    eprintln!("[headless] enabled_plugin_ids={:?}", cfg.enabled_plugin_ids);
    eprintln!("[headless] interval_secs={}", cfg.interval_secs);

    // Initialize plugins (reads plugins/ in dev, otherwise resources/bundled_plugins/).
    let (_install_dir, all_plugins) =
        plugin_engine::initialize_plugins(&app_data_dir, &exe_dir);

    let enabled: Vec<_> = if cfg.enabled_plugin_ids.is_empty() {
        all_plugins
    } else {
        all_plugins
            .into_iter()
            .filter(|p| cfg.enabled_plugin_ids.contains(&p.manifest.id))
            .collect()
    };

    let known_ids: Vec<String> = enabled.iter().map(|p| p.manifest.id.clone()).collect();
    eprintln!("[headless] active plugins: {:?}", known_ids);

    if enabled.is_empty() {
        eprintln!("[headless] WARNING: no enabled plugins matched; HTTP API will return empty");
    }

    // Start HTTP server (background thread, binds 127.0.0.1:6736).
    // init() also loads any existing cache from disk into the in-memory snapshots.
    local_http_api::init(&app_data_dir, known_ids);
    local_http_api::start_server();

    // Probe loop on the main thread (keeps the process alive).
    let interval = Duration::from_secs(cfg.interval_secs.max(5));
    eprintln!("[headless] probe loop starting (interval={}s)", interval.as_secs());

    // If we just got restarted (e.g., by WPF after a settings toggle) and every enabled
    // plugin already has a fresh on-disk snapshot, defer the first probe by a full
    // interval. This prevents external APIs (e.g., Claude) from being hit on every
    // settings change and triggering rate limits.
    if cache_is_fresh_for_all(&app_data_dir, &enabled, interval) {
        eprintln!(
            "[headless] all enabled plugins have fresh cache; deferring first probe by {}s",
            interval.as_secs()
        );
        std::thread::sleep(interval);
    }

    loop {
        for plugin in &enabled {
            let pid = plugin.manifest.id.clone();
            let output =
                plugin_engine::runtime::run_probe(plugin, &app_data_dir, &cfg.app_version);
            let line_count = output.lines.len();
            local_http_api::cache_successful_output(&output);
            eprintln!("[headless] probed {} ({} lines)", pid, line_count);
        }
        std::thread::sleep(interval);
    }
}

/// Returns true iff every enabled plugin has a cached snapshot whose age is less
/// than `interval`. When true, the caller should skip its initial probe.
fn cache_is_fresh_for_all(
    app_data_dir: &std::path::Path,
    enabled: &[crate::plugin_engine::manifest::LoadedPlugin],
    interval: Duration,
) -> bool {
    use time::OffsetDateTime;
    use time::format_description::well_known::Rfc3339;

    if enabled.is_empty() {
        return false;
    }

    let snapshots = crate::local_http_api::cache::load_cache(app_data_dir);
    let now = OffsetDateTime::now_utc();
    let interval_secs = interval.as_secs() as i64;

    for plugin in enabled {
        let snap = match snapshots.get(&plugin.manifest.id) {
            Some(s) => s,
            None => return false,
        };
        let fetched = match OffsetDateTime::parse(&snap.fetched_at, &Rfc3339) {
            Ok(t) => t,
            Err(_) => return false,
        };
        let age_secs = (now - fetched).whole_seconds();
        if age_secs >= interval_secs {
            return false;
        }
    }
    true
}

/// Parse `--enabled` and `--interval-secs` from raw args (everything after the program name).
pub fn parse_args(args: &[String]) -> HeadlessConfig {
    let mut enabled = Vec::new();
    let mut interval = 60u64;

    let mut i = 0;
    while i < args.len() {
        match args[i].as_str() {
            "--enabled" if i + 1 < args.len() => {
                enabled = args[i + 1]
                    .split(',')
                    .map(|s| s.trim().to_string())
                    .filter(|s| !s.is_empty())
                    .collect();
                i += 2;
            }
            "--interval-secs" if i + 1 < args.len() => {
                interval = args[i + 1].parse().unwrap_or(60);
                i += 2;
            }
            _ => i += 1,
        }
    }

    HeadlessConfig {
        enabled_plugin_ids: enabled,
        interval_secs: interval,
        app_version: env!("CARGO_PKG_VERSION").to_string(),
    }
}
