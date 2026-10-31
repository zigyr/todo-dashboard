# ADR-0002: 改用 .NET 8 + WPF 桌面应用

- **状态**：Accepted（已确认）
- **日期**：2026-11-01
- **决策者**：zigyr
- **关联**：[PRD v0.2](../PRD.md) · 取代 [ADR-0001](0001-desktop-stack.md)

## 背景

第一版用 Tauri 2 + React 实现，代码能编译、单元测试通过。但在这台 Windows 10 机器上，Tauri 创建窗口时 WebView2 子进程崩溃（`msedge.dll`，异常码 `0x80000003`）。把前端换成空白页仍然崩溃，说明不是页面代码的问题；Edge 浏览器本身正常。

用户希望：单个 exe、运行环境不复杂、启动快、界面米白简约。继续修 WebView2 的成本不可控，因此改用不依赖 WebView2 的原生界面。

## 决策

采用 **.NET 8 + WPF**：

- C# + XAML，Windows 原生界面；
- 自包含单文件发布（`win-x64`），用户机器不需要安装 .NET；
- 数据仍是本地 JSON + Markdown，格式与之前一致；
- HTML 解析使用 AngleSharp（当前 1.9.0）。

## 理由

- 不依赖 WebView2，避开当前机器的崩溃；
- 单个 exe 自带运行库，运行环境最简单；
- WPF 启动快、Windows 原生，符合“不复杂、启动快”；
- 数据格式不变，后续接 DeepSeek 仍然容易。

## 代价

- exe 约 72 MB（自包含 WPF 的正常体积），比 Tauri 大；
- 界面用 XAML 写，迭代速度比 Web 慢一些；
- 只支持 Windows，但这本来就是目标平台。

## 结果

- `desktop/TodoDashboard/` 为当前主程序；
- `src-tauri/` 和 `src/` 的 Tauri/React 代码保留为原型，不再作为主路径；
- 已实测：粘贴真实博客 → 抓取 → 预览 → 保存为 `.md` + `.json` 成功。

> 如果未来要跨平台或回到 Web 技术栈，另开 ADR 记录，不直接改本文件。
