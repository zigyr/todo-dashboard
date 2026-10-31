# ADR-0001: Windows 桌面技术栈

> 2026-11-01：本文档已被 [ADR-0002](0002-wpf-desktop.md) 取代。当前主程序改用 .NET 8 + WPF，原因见 ADR-0002。

- **状态**：Superseded（已被 ADR-0002 取代）
- **日期**：2026-11-01
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

## 确认记录

2026-11-01：接受 Tauri 2 + React + TypeScript；接受 WebView2 与 Rust 工具链。
暂不做：代码签名、自动更新（后续单独立项）。

## 2026-11-01 复核：性价比与运行环境

**结论：继续用 Tauri 2 + React + TypeScript。**

理由：

- 最终用户不需要装 Node / Rust，只需要 Windows 10/11 通常自带的 WebView2；
- 安装包小（几 MB 到十几 MB），启动快；
- 仪表盘、Markdown、流式输出用 Web 技术开发最快；
- 开发环境已经搭好，换栈要重写界面，反而更贵。

代价与风险：

- 开发机需要 Node + Rust + VS Build Tools（已装好）；
- 依赖 WebView2。当前这台机器的 WebView2 运行时崩溃（见 `docs/KNOWN_ISSUES.md`），但 Edge 浏览器正常，判断是运行时/会话问题，不是 Tauri 本身。

**换栈触发条件：**

- 用管理员权限修复/重装 WebView2 后仍然崩溃；
- 或者目标用户环境中 WebView2 普遍不可用。

满足任一条件时，改用 **.NET 8 + WPF**（备选 Avalonia）。换栈只换界面层，数据格式（JSON + Markdown）、PRD、交互流程不变；换栈前另开 ADR-0002 记录原因，不直接改本文件。

> 如果后续更换，另开 ADR 记录原因，不直接改本文件。
