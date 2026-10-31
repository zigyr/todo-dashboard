# 已知问题

## KI-001：WebView2 子进程在创建窗口时崩溃

- **发现时间**：2026-11-01
- **影响**：`npm run tauri dev` 能创建窗口，但 `msedgewebview2.exe` 在 `msedge.dll` 中崩溃（异常码 `0x80000003`），应用以退出码 101 结束。
- **环境**：Windows 10 19045.6456；WebView2 Runtime 156.0.4314.50；Tauri 2.12.5。
- **复现**：把前端换成一个空白 HTML 页面，仍然崩溃；因此不是 React / CSS / 业务代码的问题。
- **已排除**：
  - 项目路径含空格或中文（路径为 `F:\todo_dashboard`）
  - 数据目录权限（默认路径和工作区路径都试过）
  - GPU / 沙箱参数（`--disable-gpu --no-sandbox`）
  - 用计划任务在普通桌面会话启动（窗口能创建，WebView2 仍崩溃）
- **判断**：本机 WebView2 Runtime 或当前 Windows 会话与 WebView2 156 不兼容。Edge 浏览器本身可以正常运行。
- **下一步**：
  1. 用管理员权限修复 / 重装 WebView2 Runtime；
  2. 修复后运行 `npm run tauri dev`，确认窗口与页面正常；
  3. 如果仍崩溃，改用不依赖 WebView2 的原生界面方案（需要另开 ADR）。
