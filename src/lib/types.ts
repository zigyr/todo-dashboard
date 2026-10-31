export interface SourcePreview {
  url: string;
  canonicalUrl: string;
  title: string;
  author: string | null;
  publishedAt: string | null;
  fetchedAt: string;
  siteName: string | null;
  text: string;
  excerpt: string;
  wordCount: number;
  language: string;
  contentHash: string;
}

export interface SavedSource {
  id: string;
  title: string;
  jsonPath: string;
  markdownPath: string;
  savedAt: string;
}

export interface SourceSummary {
  id: string;
  title: string;
  url: string;
  author: string | null;
  fetchedAt: string;
  wordCount: number;
  status: string;
}

export interface WorkspaceInfo {
  path: string;
  sourceCount: number;
}
