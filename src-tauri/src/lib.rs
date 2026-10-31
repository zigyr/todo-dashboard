mod capture;
mod commands;
mod error;
mod store;
mod workspace;

use commands::AppState;

pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .manage(AppState::new())
        .invoke_handler(tauri::generate_handler![
            commands::capture_url,
            commands::save_source,
            commands::list_sources,
            commands::workspace_info,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
