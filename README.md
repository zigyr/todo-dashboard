# Todo Dashboard

> 粘贴一篇博客，AI 给你一段摘要。

一个 Windows 桌面小工具：**粘贴网址 → 抓取正文 → AI 摘要 → 复制原文链接**。

只保留两个页面：**摘要** 和 **历史**。正文不落盘；历史只存摘要、标题和原文链接，放在一个本地 CSV 里。

## 现在能做什么

- **摘要页**：粘贴一个博客网址，生成摘要；不滚动，一屏看完；
- DeepSeek 生成一段摘要 + 3–5 条关键观点；
- 点「复制链接」把原文网址放进剪贴板，粘到浏览器地址栏就能看原文；
- **历史页**：左边竖向时间线，右边看摘要；时间线可以滚动；
- 历史自动保存到本地 `history.csv`；
- 支持导入 CSV，**覆盖**本地历史；
- 不保存正文，不生成任务，不拆解行动。

## 为什么是「复制链接」而不是「打开原文」

在这个 Windows 会话里，应用**无法**把网址交给浏览器，四条路都被系统挡住（均已实测）：

- 直接起 chrome/edge 进程：`0xC0000142`，Chrome 自终止；
- `explorer.exe <url>` 转发：`0xC0000142`，弹「应用程序错误」；
- `Shell.Application.ShellExecute`：返回成功但浏览器没起；
- 键盘注入（Ctrl+T/L/V/Enter）：按键已送达，浏览器不响应（UIPI 限制低完整性进程注入）。

所以只保留「复制链接」这一步，点一下链接就在剪贴板里，粘贴即可。在不受限的普通 Windows 桌面上，跳转本身是可以用的，但产品不再提供该按钮。

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
  -o desktop/TodoDashboard-portable
```

把 `desktop/TodoDashboard-portable` 整个文件夹（或 `TodoDashboard-portable.zip`）发给用户，双击 `TodoDashboard.exe` 即可。

## 如果双击 exe 没反应

请用文件夹版 `TodoDashboard-portable.zip`，不要用单文件版。单文件版启动时要把 WPF 组件解压到系统临时目录；临时目录被权限限制时会没有反应。

## 数据

- 默认路径：exe 同目录的 `data/history.csv`（开发时是 `bin/.../data/history.csv`）；
- 也可以用环境变量 `TODO_DASHBOARD_HISTORY` 指定一个 CSV 路径；
- CSV 列：`created_at,url,title,author,published_at,word_count,summary`；
- 只存摘要、标题和原文链接，不存文章正文；
- 「导入 CSV（覆盖）」会先读入选中的 CSV，再用它覆盖本地 `history.csv`。

## 项目状态

- [x] URL → 抓取 → AI 摘要 → 复制原文链接
- [x] 摘要 + 关键观点
- [x] 两个页面：摘要 / 历史
- [x] 本地 CSV 历史 + 导入覆盖
- [x] 时间线竖向贯穿
- [ ] 用真实 DeepSeek Key 跑几篇，打磨摘要质量

## 目录结构

```
README.md              产品说明（当前你在看的）
CHANGELOG.md           变更记录
LICENSE                MIT
desktop/TodoDashboard/ WPF 应用源码（唯一的代码目录）
  ├─ Models/           数据模型：抓取结果、摘要、历史条目
  ├─ Services/         抓取、DeepSeek 调用、CSV 历史、剪贴板、Key 存储
  ├─ ViewModels/       MVVM：摘要页/历史页逻辑与命令
  ├─ Views/            API Key 弹窗
  └─ MainWindow.xaml   两页界面（摘要 / 历史）
docs/ENGINEERING.md    工程规范：分支、提交、质量门禁
docs/KNOWN_ISSUES.md   已知问题与历史排查记录
docs/adr/              架构决策记录（0001 旧栈 / 0002 现用 WPF）
docs/archive/          已不适用历史文档（PRD、章程、方法论、设计稿）
.github/workflows/     CI：windows-latest 上构建 WPF
```

## 文档

- [工程规范](docs/ENGINEERING.md) — 分支、提交、质量门禁与协作约定
- [已有问题](docs/KNOWN_ISSUES.md) — 已知问题与排查记录
- [ADR-0002](docs/adr/0002-wpf-desktop.md) — 当前技术栈：.NET 8 + WPF
- [归档文档](docs/archive/README.md) — 已废弃的 PRD / 章程 / 方法论 / 设计稿

## License

[MIT](LICENSE)
