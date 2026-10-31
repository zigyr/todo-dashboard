use std::path::{Path, PathBuf};

use crate::error::AppError;

/// 工作区位置：
/// 1. 环境变量 TODO_DASHBOARD_DATA_DIR
/// 2. exe 同目录的 data/（便携模式）
/// 3. 开发模式：仓库根目录的 data/
/// 4. 正式模式：%USERPROFILE%\TodoDashboard
pub fn resolve() -> PathBuf {
    if let Ok(dir) = std::env::var("TODO_DASHBOARD_DATA_DIR") {
        let dir = dir.trim();
        if !dir.is_empty() {
            return PathBuf::from(dir);
        }
    }

    if let Ok(exe) = std::env::current_exe() {
        if let Some(parent) = exe.parent() {
            let portable = parent.join("data");
            if portable.is_dir() {
                return portable;
            }
        }
    }

    if cfg!(debug_assertions) {
        return PathBuf::from(env!("CARGO_MANIFEST_DIR"))
            .join("..")
            .join("data");
    }

    if let Ok(profile) = std::env::var("USERPROFILE") {
        return PathBuf::from(profile).join("TodoDashboard");
    }

    PathBuf::from("data")
}

pub fn ensure(root: &Path) -> Result<(), AppError> {
    for folder in ["sources", "reviews", "exports", "quarantine"] {
        let path = root.join(folder);
        std::fs::create_dir_all(&path)
            .map_err(|e| AppError::Save(format!("无法创建工作区目录 {}：{e}", path.display())))?;
    }
    Ok(())
}

pub fn source_count(root: &Path) -> usize {
    let dir = root.join("sources");
    let Ok(entries) = std::fs::read_dir(dir) else {
        return 0;
    };

    entries
        .filter_map(|entry| entry.ok())
        .filter(|entry| {
            entry
                .path()
                .extension()
                .and_then(|ext| ext.to_str())
                .map(|ext| ext == "json")
                .unwrap_or(false)
        })
        .count()
}
