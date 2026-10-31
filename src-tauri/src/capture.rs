use std::net::IpAddr;
use std::time::Duration;

use chrono::Utc;
use reqwest::header::{ACCEPT, CONTENT_TYPE, LOCATION};
use scraper::{Html, Selector};
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};
use url::Url;

use crate::error::AppError;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SourcePreview {
    pub url: String,
    pub canonical_url: String,
    pub title: String,
    pub author: Option<String>,
    pub published_at: Option<String>,
    pub fetched_at: String,
    pub site_name: Option<String>,
    pub text: String,
    pub excerpt: String,
    pub word_count: usize,
    pub language: String,
    pub content_hash: String,
}

pub async fn fetch(client: &reqwest::Client, raw_url: &str) -> Result<SourcePreview, AppError> {
    let mut current = Url::parse(raw_url).map_err(|_| AppError::InvalidUrl)?;

    if !matches!(current.scheme(), "http" | "https") {
        return Err(AppError::InvalidUrl);
    }

    for _ in 0..=5 {
        ensure_public(&current).await?;

        let response = client
            .get(current.clone())
            .header(ACCEPT, "text/html,application/xhtml+xml")
            .timeout(Duration::from_secs(20))
            .send()
            .await
            .map_err(map_reqwest)?;

        if response.status().is_redirection() {
            let location = response
                .headers()
                .get(LOCATION)
                .and_then(|value| value.to_str().ok())
                .ok_or_else(|| AppError::Fetch("重定向缺少目标地址。".to_string()))?;
            current = current
                .join(location)
                .map_err(|_| AppError::Fetch("重定向地址无效。".to_string()))?;
            continue;
        }

        let status = response.status();
        if !status.is_success() {
            return Err(AppError::Fetch(format!("服务器返回 {status}。")));
        }

        let content_type = response
            .headers()
            .get(CONTENT_TYPE)
            .and_then(|value| value.to_str().ok())
            .unwrap_or("");

        if !content_type.is_empty()
            && !content_type.contains("text/html")
            && !content_type.contains("application/xhtml")
        {
            return Err(AppError::Fetch(format!("这不是网页（{content_type}）。")));
        }

        if let Some(length) = response.content_length() {
            if length > 5 * 1024 * 1024 {
                return Err(AppError::Fetch("页面超过 5 MB，暂不处理。".to_string()));
            }
        }

        let bytes = response.bytes().await.map_err(map_reqwest)?;
        if bytes.len() > 5 * 1024 * 1024 {
            return Err(AppError::Fetch("页面超过 5 MB，暂不处理。".to_string()));
        }

        let html = String::from_utf8_lossy(&bytes);
        return extract(&html, &current);
    }

    Err(AppError::Fetch("重定向次数过多。".to_string()))
}

async fn ensure_public(url: &Url) -> Result<(), AppError> {
    if !matches!(url.scheme(), "http" | "https") {
        return Err(AppError::InvalidUrl);
    }
    if !url.username().is_empty() || url.password().is_some() {
        return Err(AppError::InvalidUrl);
    }

    let host = url.host_str().ok_or(AppError::InvalidUrl)?;
    if host.eq_ignore_ascii_case("localhost") || host.ends_with(".localhost") {
        return Err(AppError::BlockedUrl);
    }

    if let Ok(ip) = host.parse::<IpAddr>() {
        return if is_blocked(ip) {
            Err(AppError::BlockedUrl)
        } else {
            Ok(())
        };
    }

    let port = url.port_or_known_default().ok_or(AppError::InvalidUrl)?;
    let addrs = tokio::net::lookup_host((host, port))
        .await
        .map_err(|_| AppError::Fetch("域名解析失败，请检查网址或网络。".to_string()))?;

    let mut found = false;
    for addr in addrs {
        found = true;
        if is_blocked(addr.ip()) {
            return Err(AppError::BlockedUrl);
        }
    }

    if !found {
        return Err(AppError::Fetch("这个域名没有可用地址。".to_string()));
    }

    Ok(())
}

fn is_blocked(ip: IpAddr) -> bool {
    match ip {
        IpAddr::V4(ip) => {
            ip.is_private()
                || ip.is_loopback()
                || ip.is_link_local()
                || ip.is_broadcast()
                || ip.is_documentation()
                || ip.is_unspecified()
        }
        IpAddr::V6(ip) => {
            ip.is_loopback()
                || ip.is_unspecified()
                || ip.is_unique_local()
                || ip.is_unicast_link_local()
        }
    }
}

fn extract(html: &str, url: &Url) -> Result<SourcePreview, AppError> {
    let document = Html::parse_document(html);

    let title = meta_content(&document, "meta[property=\"og:title\"]")
        .or_else(|| meta_content(&document, "meta[name=\"twitter:title\"]"))
        .or_else(|| select_text(&document, "title"))
        .map(|value| clean_inline(&value))
        .filter(|value| !value.is_empty())
        .unwrap_or_else(|| "未命名文章".to_string());

    let author = meta_content(&document, "meta[name=\"author\"]")
        .or_else(|| meta_content(&document, "meta[property=\"article:author\"]"))
        .or_else(|| select_text(&document, "[rel=\"author\"]"))
        .map(|value| clean_inline(&value))
        .filter(|value| !value.is_empty());

    let published_at = meta_content(&document, "meta[property=\"article:published_time\"]")
        .or_else(|| attr_value(&document, "time[datetime]", "datetime"))
        .map(|value| clean_inline(&value))
        .filter(|value| !value.is_empty());

    let site_name = meta_content(&document, "meta[property=\"og:site_name\"]")
        .map(|value| clean_inline(&value))
        .filter(|value| !value.is_empty());

    let text = extract_body_text(&document)?;
    let word_count = count_words(&text);
    let language = detect_language(&text);
    let excerpt = make_excerpt(&text, 280);
    let content_hash = hash_text(&text);

    let mut canonical = url.clone();
    canonical.set_fragment(None);

    Ok(SourcePreview {
        url: url.to_string(),
        canonical_url: canonical.to_string(),
        title,
        author,
        published_at,
        fetched_at: Utc::now().to_rfc3339(),
        site_name,
        text,
        excerpt,
        word_count,
        language,
        content_hash,
    })
}

fn extract_body_text(document: &Html) -> Result<String, AppError> {
    const CANDIDATES: &[&str] = &[
        "article",
        "main",
        "[role=\"main\"]",
        ".post-content",
        ".entry-content",
        ".article-content",
        ".markdown-body",
        "body",
    ];

    let mut best = String::new();

    for candidate in CANDIDATES {
        let Ok(selector) = Selector::parse(candidate) else {
            continue;
        };
        let Some(element) = document.select(&selector).next() else {
            continue;
        };

        let fragment = element.html();
        let text = normalize_text(&html2text::from_read(fragment.as_bytes(), 100));
        let length = text.chars().count();

        if length >= 200 {
            best = text;
            break;
        }

        if length > best.chars().count() {
            best = text;
        }
    }

    if best.chars().count() < 120 {
        return Err(AppError::EmptyContent);
    }

    Ok(best)
}

fn meta_content(document: &Html, selector: &str) -> Option<String> {
    let selector = Selector::parse(selector).ok()?;
    document
        .select(&selector)
        .next()
        .and_then(|element| element.value().attr("content"))
        .map(str::trim)
        .filter(|value| !value.is_empty())
        .map(str::to_string)
}

fn attr_value(document: &Html, selector: &str, attr: &str) -> Option<String> {
    let selector = Selector::parse(selector).ok()?;
    document
        .select(&selector)
        .next()
        .and_then(|element| element.value().attr(attr))
        .map(str::trim)
        .filter(|value| !value.is_empty())
        .map(str::to_string)
}

fn select_text(document: &Html, selector: &str) -> Option<String> {
    let selector = Selector::parse(selector).ok()?;
    document
        .select(&selector)
        .next()
        .map(|element| element.text().collect::<Vec<_>>().join(" "))
        .map(|value| clean_inline(&value))
        .filter(|value| !value.is_empty())
}

fn clean_inline(value: &str) -> String {
    value.split_whitespace().collect::<Vec<_>>().join(" ")
}

fn normalize_text(input: &str) -> String {
    let normalized = input.replace("\r\n", "\n").replace('\r', "\n");
    let mut out = String::with_capacity(normalized.len());
    let mut blank_run = 0usize;

    for line in normalized.lines() {
        let line = line.trim_end();
        if line.trim().is_empty() {
            blank_run += 1;
            if blank_run > 1 {
                continue;
            }
            if !out.is_empty() {
                out.push('\n');
            }
        } else {
            blank_run = 0;
            out.push_str(line);
            out.push('\n');
        }
    }

    out.trim().to_string()
}

fn make_excerpt(text: &str, max_chars: usize) -> String {
    let mut excerpt: String = text.chars().take(max_chars).collect();
    if text.chars().count() > max_chars {
        excerpt.push('…');
    }
    excerpt
}

fn count_words(text: &str) -> usize {
    let cjk = text.chars().filter(|ch| is_cjk(*ch)).count();
    let rest: String = text
        .chars()
        .map(|ch| if is_cjk(ch) { ' ' } else { ch })
        .collect();
    cjk + rest.split_whitespace().count()
}

fn detect_language(text: &str) -> String {
    let cjk = text.chars().filter(|ch| is_cjk(*ch)).count();
    let latin_words = text
        .split_whitespace()
        .filter(|word| word.chars().any(|ch| ch.is_ascii_alphabetic()))
        .count();

    if cjk > 0 && cjk >= latin_words * 2 {
        "zh".to_string()
    } else if latin_words > 0 {
        "en".to_string()
    } else {
        "unknown".to_string()
    }
}

fn is_cjk(ch: char) -> bool {
    matches!(
        ch as u32,
        0x3400..=0x4DBF | 0x4E00..=0x9FFF | 0x3040..=0x30FF | 0xAC00..=0xD7AF
    )
}

fn hash_text(text: &str) -> String {
    let mut hasher = Sha256::new();
    hasher.update(text.as_bytes());
    hasher
        .finalize()
        .iter()
        .map(|byte| format!("{byte:02x}"))
        .collect()
}

fn map_reqwest(error: reqwest::Error) -> AppError {
    if error.is_timeout() {
        AppError::Fetch("请求超时，网站可能太慢或无法访问。".to_string())
    } else if error.is_connect() {
        AppError::Fetch("连接失败，请检查网络或网址。".to_string())
    } else {
        AppError::Fetch(error.to_string())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn counts_chinese_and_english() {
        assert_eq!(count_words("你好 world"), 3);
    }

    #[test]
    fn detects_chinese_text() {
        assert_eq!(detect_language("这是一段中文内容，用来测试。"), "zh");
    }

    #[test]
    fn blocks_private_and_loopback_addresses() {
        assert!(is_blocked("127.0.0.1".parse().unwrap()));
        assert!(is_blocked("192.168.1.10".parse().unwrap()));
        assert!(!is_blocked("1.1.1.1".parse().unwrap()));
    }

    #[test]
    fn extracts_title_and_body() {
        let body = "这是一段足够长的正文，用来测试提取逻辑。".repeat(8);
        let html = format!(
            r#"<html><head><title>标题</title><meta property="og:title" content="测试文章"></head><body><article><h1>测试文章</h1><p>{body}</p></article></body></html>"#
        );
        let url = Url::parse("https://example.com/post").unwrap();
        let preview = extract(&html, &url).unwrap();

        assert_eq!(preview.title, "测试文章");
        assert!(preview.text.contains("正文"));
        assert!(preview.word_count > 0);
        assert_eq!(preview.language, "zh");
    }
}
