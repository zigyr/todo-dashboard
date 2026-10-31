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

### Changed

- PRD v0.2 confirmed: URL-only capture, Tauri stack, MIT license.
- Delivery split: M1 is URL capture and local save; DeepSeek analysis moves to M2.

### Known issues

- KI-001: WebView2 runtime crashes on the current development machine. See `docs/KNOWN_ISSUES.md`.
