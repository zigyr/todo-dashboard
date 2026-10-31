use std::path::Path;

use chrono::Utc;
use serde::{Deserialize, Serialize};
use uuid::Uuid;

use crate::capture::SourcePreview;
use crate::error::AppError;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SourceRecord {
    pub schema: u32,
    pub id: String,
    pub url: String,
    pub canonical_url: String,
    pub title: String,
    pub author: Option<String>,
    pub published_at: Option<String>,
    pub fetched_at: String,
    pub site_name: Option<String>,
    pub language: String,
    pub content_hash: String,
    pub word_count: usize,
    pub excerpt: String,
    pub content_file: String,
    pub status: String,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct SourceSummary {
    pub id: String,
    pub title: String,
    pub url: String,
    pub author: Option<String>,
    pub fetched_at: String,
    pub word_count: usize,
    pub status: String,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct SavedSource {
    pub id: String,
    pub title: String,
    pub json_path: String,
    pub markdown_path: String,
    pub saved_at: String,
}

pub fn save(root: &Path, preview: &SourcePreview) -> Result<SavedSource, AppError> {
    crate::workspace::ensure(root)?;

    let sources = root.join("sources");
    let id = format!(
        "{}-{}",
        Utc::now().format("%Y%m%d-%H%M%S"),
        &Uuid::new_v4().simple().to_string()[..8]
    );
    let json_path = sources.join(format!("{id}.json"));
    let markdown_path = sources.join(format!("{id}.md"));
    let saved_at = Utc::now().to_rfc3339();

    let record = SourceRecord {
        schema: 1,
        id: id.clone(),
        url: preview.url.clone(),
        canonical_url: preview.canonical_url.clone(),
        title: preview.title.clone(),
        author: preview.author.clone(),
        published_at: preview.published_at.clone(),
        fetched_at: preview.fetched_at.clone(),
        site_name: preview.site_name.clone(),
        language: preview.language.clone(),
        content_hash: preview.content_hash.clone(),
        word_count: preview.word_count,
        excerpt: preview.excerpt.clone(),
        content_file: format!("{id}.md"),
        status: "captured".to_string(),
    };

    let json = serde_json::to_vec_pretty(&record)
        .map_err(|e| AppError::Save(format!("序列化来源数据失败：{e}")))?;
    atomic_write(&json_path, &json)?;
    atomic_write(&markdown_path, render_markdown(preview).as_bytes())?;

    Ok(SavedSource {
        id,
        title: preview.title.clone(),
        json_path: json_path.to_string_lossy().into_owned(),
        markdown_path: markdown_path.to_string_lossy().into_owned(),
        saved_at,
    })
}

pub fn list(root: &Path) -> Result<Vec<SourceSummary>, AppError> {
    let dir = root.join("sources");
    if !dir.is_dir() {
        return Ok(Vec::new());
    }

    let entries = std::fs::read_dir(&dir)
        .map_err(|e| AppError::Read(format!("读取 {} 失败：{e}", dir.display())))?;

    let mut items = Vec::new();
    for entry in entries.flatten() {
        let path = entry.path();
        if path.extension().and_then(|ext| ext.to_str()) != Some("json") {
            continue;
        }

        let Ok(raw) = std::fs::read_to_string(&path) else {
            continue;
        };
        let Ok(record) = serde_json::from_str::<SourceRecord>(&raw) else {
            continue;
        };

        items.push(SourceSummary {
            id: record.id,
            title: record.title,
            url: record.canonical_url,
            author: record.author,
            fetched_at: record.fetched_at,
            word_count: record.word_count,
            status: record.status,
        });
    }

    items.sort_by(|a, b| b.fetched_at.cmp(&a.fetched_at));
    items.truncate(20);
    Ok(items)
}

fn render_markdown(preview: &SourcePreview) -> String {
    let mut out = String::new();
    out.push_str(&format!("# {}\n\n", preview.title));
    out.push_str(&format!("- 来源：{}\n", preview.canonical_url));
    if let Some(author) = &preview.author {
        out.push_str(&format!("- 作者：{author}\n"));
    }
    if let Some(site) = &preview.site_name {
        out.push_str(&format!("- 站点：{site}\n"));
    }
    if let Some(date) = &preview.published_at {
        out.push_str(&format!("- 发布：{date}\n"));
    }
    out.push_str(&format!("- 抓取：{}\n", preview.fetched_at));
    out.push_str(&format!("- 字数：{}\n", preview.word_count));
    out.push_str(&format!("- 指纹：{}\n\n---\n\n", preview.content_hash));
    out.push_str(&preview.text);
    out.push('\n');
    out
}

fn atomic_write(path: &Path, bytes: &[u8]) -> Result<(), AppError> {
    let file_name = path
        .file_name()
        .and_then(|name| name.to_str())
        .unwrap_or("source");
    let temp = path.with_file_name(format!(".{file_name}.tmp"));

    std::fs::write(&temp, bytes)
        .map_err(|e| AppError::Save(format!("写入 {} 失败：{e}", temp.display())))?;

    if let Err(error) = std::fs::rename(&temp, path) {
        let _ = std::fs::remove_file(&temp);
        return Err(AppError::Save(format!(
            "写入 {} 失败：{error}",
            path.display()
        )));
    }

    Ok(())
}
