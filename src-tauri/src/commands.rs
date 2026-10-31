use std::path::PathBuf;

use serde::Serialize;

use crate::capture::{self, SourcePreview};
use crate::error::AppError;
use crate::store::{self, SavedSource, SourceSummary};
use crate::workspace;

pub struct AppState {
    pub workspace: PathBuf,
    pub client: reqwest::Client,
}

impl AppState {
    pub fn new() -> Self {
        let workspace = workspace::resolve();
        let _ = workspace::ensure(&workspace);

        let client = reqwest::Client::builder()
            .redirect(reqwest::redirect::Policy::none())
            .timeout(std::time::Duration::from_secs(20))
            .user_agent("TodoDashboard/0.1 (+https://github.com/zigyr/todo-dashboard)")
            .build()
            .expect("failed to build HTTP client");

        Self { workspace, client }
    }
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct WorkspaceInfo {
    pub path: String,
    pub source_count: usize,
}

#[tauri::command]
pub async fn capture_url(
    state: tauri::State<'_, AppState>,
    url: String,
) -> Result<SourcePreview, AppError> {
    let client = state.client.clone();
    capture::fetch(&client, &url).await
}

#[tauri::command]
pub fn save_source(
    state: tauri::State<'_, AppState>,
    preview: SourcePreview,
) -> Result<SavedSource, AppError> {
    store::save(&state.workspace, &preview)
}

#[tauri::command]
pub fn list_sources(state: tauri::State<'_, AppState>) -> Result<Vec<SourceSummary>, AppError> {
    store::list(&state.workspace)
}

#[tauri::command]
pub fn workspace_info(state: tauri::State<'_, AppState>) -> WorkspaceInfo {
    WorkspaceInfo {
        path: state.workspace.to_string_lossy().into_owned(),
        source_count: workspace::source_count(&state.workspace),
    }
}
