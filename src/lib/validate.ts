const SCHEME_RE = /^[a-zA-Z][a-zA-Z\d+\-.]*:/;

/**
 * 把用户输入整理成可抓取的地址。
 * - 没有协议时补 https://
 * - 只允许 http / https
 * - 空字符串、非法地址返回 null
 */
export function normalizeUrl(input: string): string | null {
  const trimmed = input.trim();
  if (!trimmed) return null;

  const candidate = SCHEME_RE.test(trimmed) ? trimmed : `https://${trimmed}`;

  try {
    const url = new URL(candidate);
    if (url.protocol !== "http:" && url.protocol !== "https:") return null;
    if (!url.hostname || url.hostname.includes(" ")) return null;
    return url.toString();
  } catch {
    return null;
  }
}

export function isProbablyUrl(input: string): boolean {
  return normalizeUrl(input) !== null;
}
