import { formatDate, formatWordCount } from "../lib/format";
import type { SourcePreview } from "../lib/types";

interface PreviewCardProps {
  preview: SourcePreview;
  saving: boolean;
  onSave: () => void;
  onDiscard: () => void;
  onOpen: () => void;
}

export function PreviewCard({ preview, saving, onSave, onDiscard, onOpen }: PreviewCardProps) {
  return (
    <section className="preview-card" aria-labelledby="preview-title">
      <div className="preview-top">
        <div>
          <p className="eyebrow">抓取成功 · 先确认再保存</p>
          <h2 id="preview-title">{preview.title}</h2>
          <p className="meta">
            {preview.author ?? "作者未知"} · {formatDate(preview.publishedAt ?? preview.fetchedAt)} ·{" "}
            {formatWordCount(preview.wordCount)}
          </p>
        </div>
        <span className="badge">未保存</span>
      </div>

      <blockquote>{preview.excerpt}</blockquote>

      <details className="source-details">
        <summary>查看正文（{formatWordCount(preview.wordCount)}）</summary>
        <pre>{preview.text}</pre>
      </details>

      <div className="actions">
        <button type="button" className="primary" onClick={onSave} disabled={saving}>
          {saving ? "保存中…" : "保存到本地"}
        </button>
        <button type="button" className="secondary" onClick={onDiscard} disabled={saving}>
          放弃
        </button>
        <button type="button" className="text-link" onClick={onOpen}>
          打开原文 ↗
        </button>
      </div>
    </section>
  );
}
