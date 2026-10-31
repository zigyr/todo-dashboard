# ADR-0001: Windows 桌面技术栈

- **状态**：Proposed（待确认）
- **日期**：2026-10-31
- **决策者**：zigyr
- **关联**：[PRD v0.2](../PRD.md)

## 背景

产品是一个 Windows 桌面应用，需要同时满足：

- 单个 exe 分发，安装/启动成本低；
- 本地文件读写、文件监听、CLI 参数、单实例；
- 仪表盘 UI、Markdown 渲染、流式输出；
- 调用 DeepSeek API，安全保存 API Key；
- 一个人维护，迭代速度优先；
- 未来可能扩展到 macOS / Linux，但不是 v0 目标。

## 选项

| 方案 | 体积 | UI 速度 | Windows 集成 | 学习成本 | 生态 |
| --- | --- | --- | --- | --- | --- |
| Tauri 2 + React + TS | 小（~5–15 MB） | 高 | 好 | 中（Rust + Web） | 强 |
| .NET 8 + WPF | 中（自包含 ~60 MB+） | 中 | 最好 | 中（C#/XAML） | 强 |
| .NET 8 + Avalonia | 中 | 中 | 好 | 中 | 中 |
| Electron | 大（~100 MB+） | 高 | 好 | 低 | 最强 |
| Python + PySide6 | 大 | 中 | 中 | 低 | 中 |

## 决策

采用 **Tauri 2 + React + TypeScript**：

- Rust 负责特权操作：文件读写、HTTP 抓取、Keyring、CLI、文件监听；
- React + TypeScript 负责 UI；
- Vite 构建；TypeScript `strict`；
- 数据层保持纯文件（Markdown + JSON），不引入数据库。

## 理由

1. **体积与启动**：Tauri 产物显著小于 Electron，符合「一个 exe」的预期。
2. **UI 迭代速度**：仪表盘、Markdown、流式输出用 Web 技术实现最快。
3. **安全边界**：前端不直接拿文件系统/网络权限，敏感操作走 Rust command。
4. **可迁移**：未来上 macOS/Linux 的成本较低。
5. **目标系统匹配**：Windows 10 19045 / Windows 11 自带 WebView2。

## 后果

### 正面

- 安装包小，启动快；
- UI 生态成熟（React、Tailwind、Markdown 组件）；
- Rust 侧可以严格限制 SSRF、路径穿越、Key 泄露。

### 负面

- 需要 Rust 工具链；两种语言带来上下文切换；
- 依赖 WebView2（缺失时需引导安装）；
- Tauri 插件生态不如 Electron 成熟；
- 调试跨语言问题比单栈慢。

### 缓解

- 把 Rust 代码限制在 `src-tauri/` 的少量 command，业务逻辑尽量放 TypeScript；
- 建立 `pnpm tauri dev` 的一键开发脚本；
- CI 构建 Windows 安装包与便携版；
- 如果 Rust 成为瓶颈，回退到 .NET 8 + Avalonia，数据格式不变。

## 被拒绝的方案

- **Electron**：体积和内存不适合「轻量个人工具」。
- **WPF**：Windows 集成最好，但仪表盘 UI 的开发速度与样式灵活性不如 Web。
- **Avalonia**：跨平台不错，但生态与资料少于 Tauri/React。
- **Python + PySide6**：原型快，但打包体积、杀软误报、升级体验较差。

## 待确认

- 是否接受 WebView2 依赖？
- 是否接受 Rust 工具链？
- 是否需要代码签名（v0 可暂不）？
- 是否需要自动更新（v0 不做）？

> 确认后状态改为 Accepted；如果后续更换，另开 ADR 记录原因，不直接改本文件。
