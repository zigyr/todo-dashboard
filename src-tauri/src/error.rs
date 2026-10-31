use serde::{Serialize, Serializer};

#[derive(Debug, thiserror::Error)]
pub enum AppError {
    #[error("网址格式不正确。请输入以 http:// 或 https:// 开头的网址。")]
    InvalidUrl,

    #[error("这个网址指向本机或内网，出于安全考虑不能抓取。")]
    BlockedUrl,

    #[error("抓取失败：{0}")]
    Fetch(String),

    #[error("这个页面没有可提取的正文。")]
    EmptyContent,

    #[error("保存失败：{0}")]
    Save(String),

    #[error("读取本地数据失败：{0}")]
    Read(String),
}

impl Serialize for AppError {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {
        serializer.serialize_str(&self.to_string())
    }
}
