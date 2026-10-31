import { invoke } from "@tauri-apps/api/core";
import type { SavedSource, SourcePreview, SourceSummary, WorkspaceInfo } from "./types";

export function captureUrl(url: string): Promise<SourcePreview> {
  return invoke("capture_url", { url });
}

export function saveSource(preview: SourcePreview): Promise<SavedSource> {
  return invoke("save_source", { preview });
}

export function listSources(): Promise<SourceSummary[]> {
  return invoke("list_sources");
}

export function getWorkspaceInfo(): Promise<WorkspaceInfo> {
  return invoke("workspace_info");
}
