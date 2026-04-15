// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

fn main() {
    let args: Vec<String> = std::env::args().skip(1).collect();
    if args.iter().any(|a| a == "--headless") {
        let cfg = openusage_lib::headless::parse_args(&args);
        if let Err(e) = openusage_lib::headless::run(cfg) {
            eprintln!("[headless] fatal: {}", e);
            std::process::exit(1);
        }
    } else {
        openusage_lib::run()
    }
}
