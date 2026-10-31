import type { SavedSource } from "../lib/types";
import { CheckIcon } from "./Icons";

interface SavedCardProps {
  saved: SavedSource;
  onAgain: () => void;
}

export function SavedCard({ saved, onAgain }: SavedCardProps) {
  return (
    <section className="saved-card" role="status" aria-live="polite">
      <div className="saved-icon">
        <CheckIcon />
      </div>
      <div className="saved-body">
        <p className="eyebrow">已保存到本地</p>
        <h2>{saved.title}</h2>
        <p className="muted">正文和元数据已经写入工作区：</p>
        <code>{saved.markdownPath}</code>
        <code>{saved.jsonPath}</code>
      </div>
      <button type="button" className="primary" onClick={onAgain}>
        再抓一篇
      </button>
    </section>
  );
}
