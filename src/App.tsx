import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { FormEvent, KeyboardEvent } from "react";
import { openUrl } from "@tauri-apps/plugin-opener";
import { captureUrl, getWorkspaceInfo, listSources, saveSource } from "./lib/api";
import { formatDate, formatWordCount, shortUrl } from "./lib/format";
import { normalizeUrl } from "./lib/validate";
import type { SavedSource, SourcePreview, SourceSummary, WorkspaceInfo } from "./lib/types";
import { AlertIcon, ArrowIcon, CheckIcon, LinkIcon, Spinner } from "./components/Icons";
import { PreviewCard } from "./components/PreviewCard";
import { SavedCard } from "./components/SavedCard";

type Phase = "idle" | "capturing" | "preview" | "saving" | "saved";

export default function App() {
  const [url, setUrl] = useState("");
  const [phase, setPhase] = useState<Phase>("idle");
  const [preview, setPreview] = useState<SourcePreview | null>(null);
  const [saved, setSaved] = useState<SavedSource | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [recent, setRecent] = useState<SourceSummary[]>([]);
  const [recentError, setRecentError] = useState<string | null>(null);
  const [workspace, setWorkspace] = useState<WorkspaceInfo | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const refresh = useCallback(async () => {
    try {
      const [info, sources] = await Promise.all([getWorkspaceInfo(), listSources()]);
      setWorkspace(info);
      setRecent(sources);
      setRecentError(null);
    } catch (cause) {
      setRecentError(String(cause));
    }
  }, []);

  useEffect(() => {
    void refresh();
    inputRef.current?.focus();
  }, [refresh]);

  const reset = useCallback(() => {
    setPhase("idle");
    setPreview(null);
    setSaved(null);
    setError(null);
    setUrl("");
    window.setTimeout(() => inputRef.current?.focus(), 0);
  }, []);

  const handleCapture = async (event?: FormEvent) => {
    event?.preventDefault();
    if (phase === "capturing" || phase === "saving") return;

    const normalized = normalizeUrl(url);
    if (!normalized) {
      setError("请粘贴一个有效的网址，例如 https://example.com/post。");
      return;
    }

    setError(null);
    setPreview(null);
    setSaved(null);
    setPhase("capturing");

    try {
      const result = await captureUrl(normalized);
      setPreview(result);
      setUrl(result.canonicalUrl);
      setPhase("preview");
    } catch (cause) {
      setError(String(cause));
      setPhase("idle");
    }
  };

  const handleSave = async () => {
    if (!preview) return;
    setError(null);
    setPhase("saving");
    try {
      const result = await saveSource(preview);
      setSaved(result);
      setPhase("saved");
      await refresh();
    } catch (cause) {
      setError(String(cause));
      setPhase("preview");
    }
  };

  const handleOpen = async (target: string) => {
    try {
      await openUrl(target);
    } catch (cause) {
      setError(String(cause));
    }
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if ((event.ctrlKey || event.metaKey) && event.key === "Enter") {
      event.preventDefault();
      void handleCapture();
    }
    if (event.key === "Escape" && phase !== "idle") {
      event.preventDefault();
      reset();
    }
  };

  const busy = phase === "capturing" || phase === "saving";
  const canCapture = useMemo(() => url.trim().length > 0 && !busy, [url, busy]);

  return (
    <div className="app" onKeyDown={handleKeyDown}>
      <header className="topbar">
        <div className="brand">
          <div className="brand-mark" aria-hidden="true">
            <CheckIcon />
          </div>
          <div className="brand-text">
            <strong>Todo Dashboard</strong>
            <span>reading → action</span>
          </div>
        </div>
        <div className="topbar-right">
          <span className="status-dot" aria-hidden="true" />
          本地存储 · 无账号
        </div>
      </header>

      <main className="main">
        <section className="hero" aria-labelledby="hero-title">
          <p className="eyebrow">第一步 · 捕获</p>
          <h1 id="hero-title">
            把一篇博客，
            <br />
            <em>变成下一步。</em>
          </h1>
          <p className="lede">
            先粘贴网址。我们会抓取正文并保存到本地；AI 拆解和行动仪表盘在下一步接入。
          </p>

          <form className="capture-form" onSubmit={handleCapture}>
            <label className="sr-only" htmlFor="url">
              博客网址
            </label>
            <div className={["input-shell", error ? "has-error" : ""].filter(Boolean).join(" ")}>
              <LinkIcon className="input-icon" />
              <input
                id="url"
                ref={inputRef}
                value={url}
                onChange={(event) => {
                  setUrl(event.target.value);
                  if (error) setError(null);
                }}
                placeholder="https://example.com/a-good-post"
                spellCheck={false}
                autoComplete="off"
                autoCapitalize="off"
                disabled={busy}
              />
              {url && !busy ? (
                <button
                  type="button"
                  className="clear-button"
                  onClick={() => {
                    setUrl("");
                    inputRef.current?.focus();
                  }}
                  aria-label="清空网址"
                >
                  ×
                </button>
              ) : null}
            </div>
            <button type="submit" className="primary" disabled={!canCapture}>
              {phase === "capturing" ? (
                <>
                  <Spinner /> 抓取中
                </>
              ) : (
                <>
                  抓取正文 <ArrowIcon />
                </>
              )}
            </button>
          </form>

          <p className="hint">支持普通博客文章。付费墙、纯 JS 渲染的页面可能抓不到。</p>

          {error ? (
            <div className="error" role="alert">
              <AlertIcon />
              <span>{error}</span>
              <button type="button" onClick={() => setError(null)} aria-label="关闭错误提示">
                ×
              </button>
            </div>
          ) : null}
        </section>

        <div className="primary-column">
          {preview && phase !== "saved" ? (
            <PreviewCard
              preview={preview}
              saving={phase === "saving"}
              onSave={handleSave}
              onDiscard={reset}
              onOpen={() => void handleOpen(preview.canonicalUrl)}
            />
          ) : null}

          {saved && phase === "saved" ? <SavedCard saved={saved} onAgain={reset} /> : null}

          {!preview && !saved ? (
            <section className="empty-card" aria-label="还没有抓取内容">
              <p className="eyebrow">这里会出现预览</p>
              <h2>粘贴网址，先看看抓到了什么。</h2>
              <p>
                不会自动生成任务。你确认正文没问题后，才会写入本地。下一步才会让 DeepSeek
                把它拆成行动。
              </p>
              <div className="empty-steps">
                <span>1 · 粘贴</span>
                <span>2 · 预览</span>
                <span>3 · 保存</span>
              </div>
            </section>
          ) : null}
        </div>

        <aside className="side" aria-label="最近抓取与工作区">
          <section className="panel">
            <div className="panel-head">
              <h2>最近抓取</h2>
              <button type="button" className="ghost" onClick={() => void refresh()}>
                刷新
              </button>
            </div>
            {recentError ? (
              <p className="muted">{recentError}</p>
            ) : recent.length === 0 ? (
              <p className="muted">还没有记录。抓取第一篇文章后，会出现在这里。</p>
            ) : (
              <ul className="recent-list">
                {recent.map((item) => (
                  <li key={item.id}>
                    <button
                      type="button"
                      className="recent-item"
                      onClick={() => void handleOpen(item.url)}
                    >
                      <strong>{item.title}</strong>
                      <span>
                        {shortUrl(item.url)} · {formatWordCount(item.wordCount)}
                      </span>
                      <time dateTime={item.fetchedAt}>{formatDate(item.fetchedAt)}</time>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className="panel workspace-panel">
            <h2>本地工作区</h2>
            <code>{workspace?.path ?? "读取中…"}</code>
            <p className="muted">
              {workspace ? `${workspace.sourceCount} 篇文章 · JSON + Markdown` : "正在读取…"}
            </p>
          </section>
        </aside>
      </main>

      <footer className="footer">
        <span>Todo Dashboard · 0.1.0</span>
        <span>Ctrl / ⌘ + Enter 抓取 · Esc 重置</span>
      </footer>
    </div>
  );
}
