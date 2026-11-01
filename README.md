# Todo Dashboard

> 粘贴一篇博客，AI 给你一段摘要。

一个 Windows 桌面小工具：**粘贴网址 → 抓取正文 → AI 摘要 → 打开原文链接**。

只保留两个页面：**摘要** 和 **历史**。正文不落盘；历史只存摘要、标题和原文链接，放在一个本地 CSV 里。

## 现在能做什么

- **摘要页**：粘贴一个博客网址，生成摘要；不滚动，一屏看完；
- DeepSeek 生成一段摘要 + 3–5 条关键观点；
- 点「打开原文 ↗」直达原博客；
- **历史页**：左边竖向时间线，右边看摘要；时间线可以滚动；
- 历史自动保存到本地 `history.csv`；
- 支持导入 CSV，**覆盖**本地历史；
- 不保存正文，不生成任务，不拆解行动。

## API Key

- 第一次生成摘要时弹窗输入 DeepSeek API Key；
- Key 保存在 Windows 凭据管理器，不写进项目文件；
- 开发/测试时也可以用环境变量 `DEEPSEEK_API_KEY`，它会优先于凭据管理器。

## 技术栈

- **.NET 8 + WPF**（Windows 原生界面）
- **AngleSharp** 解析 HTML
- **DeepSeek** 生成摘要和讲解
- 自包含便携版，用户电脑不需要安装 .NET

## 运行

需要 .NET 8 SDK。

```powershell
dotnet run --project desktop/TodoDashboard/TodoDashboard.csproj
```

## 打包成便携版

```powershell
dotnet publish desktop/TodoDashboard/TodoDashboard.csproj -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  -o desktop/publish-portable
```

把 `desktop/publish-portable` 整个文件夹（或 `TodoDashboard-portable.zip`）发给用户，双击 `TodoDashboard.exe` 即可。

## 如果双击 exe 没反应

请用文件夹版 `TodoDashboard-portable.zip`，不要用单文件版。单文件版启动时要把 WPF 组件解压到系统临时目录；临时目录被权限限制时会没有反应。

## 数据

- 默认路径：exe 同目录的 `data/history.csv`（开发时是 `bin/.../data/history.csv`）；
- 也可以用环境变量 `TODO_DASHBOARD_HISTORY` 指定一个 CSV 路径；
- CSV 列：`created_at,url,title,author,published_at,word_count,summary`；
- 只存摘要、标题和原文链接，不存文章正文；
- 「导入 CSV（覆盖）」会先读入选中的 CSV，再用它覆盖本地 `history.csv`。

## 项目状态

- [x] URL → 抓取 → AI 摘要 → 原文链接
- [x] 摘要 + 关键观点
- [x] 两个页面：摘要 / 历史
- [x] 本地 CSV 历史 + 导入覆盖
- [x] 时间线竖向贯穿
- [ ] 用真实 DeepSeek Key 跑几篇，打磨摘要质量

## 文档

- [项目章程](docs/CHARTER.md) — 为什么做、为谁做
- [PRD](docs/PRD.md) — 历史需求；顶部有最终简化说明
- [工程规范](docs/ENGINEERING.md) — 分支、提交、质量门禁与协作约定
- [ADR-0002](docs/adr/0002-wpf-desktop.md) — 改用 .NET 8 + WPF

## License

[MIT](LICENSE)
