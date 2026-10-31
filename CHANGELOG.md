# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Repository bootstrap: README, project charter, engineering standards, PR template.
- Product direction: Windows desktop, local-first, URL-to-action workflow.
- PRD v0.2: scope, functional requirements, data model, agent contract, ranking.
- Methodology baseline: GTD, Personal Kanban, ICE/WSJF, implementation intentions.
- ADR-0001 (accepted): Tauri 2 + React + TypeScript desktop stack.
- MIT License.
- Desktop scaffold: Tauri 2 backend, React + TypeScript + Vite frontend.
- URL capture: validation, fetch, article extraction, preview, local JSON + Markdown save.
- Rust unit tests for URL blocking, text extraction, language and word counting.
- CI workflow for frontend build/tests and Rust tests.
- WPF desktop app (`desktop/TodoDashboard`): .NET 8, XAML, AngleSharp, local JSON + Markdown store.
- Design mockup and designer-facing requirements (`design/`).
- Portable data folder fallback, so a blocked `%USERPROFILE%` does not crash the app.
- DeepSeek AI analysis: article → 3–7 structured actions, human review with checkboxes, save to `data/actions/*.md`.
- API Key dialog and Windows Credential Manager storage; `DEEPSEEK_API_KEY` environment variable for development.
- Local mock server verification for the full analyze → review → save flow.
- Action dashboard window: sorted action list, one "现在做" card, start/complete/drop, drop reasons, and status writeback to Markdown.
- ActionRepository for reading/updating action files; ActionsViewModel and DropReasonWindow.
- Portable folder build and `TodoDashboard-portable.zip`, which avoid single-file temp extraction issues.

### Changed

- PRD v0.2 confirmed: URL-only capture, Tauri stack, MIT license.
- Delivery split: M1 is URL capture and local save; DeepSeek analysis moves to M2.
- Primary desktop path switched from Tauri/WebView2 to .NET 8 + WPF (ADR-0002); Tauri code kept as prototype.
- WPF app verified end-to-end on a real blog: fetch → preview → save `.md` + `.json`.

### Fixed

- KI-001 (WebView2 runtime crash) is avoided by the WPF switch; the Tauri prototype is no longer the main path.
