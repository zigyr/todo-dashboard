# Todo Dashboard

> 粘贴一篇博客，把它变成下一步行动。

一个 Windows 桌面的 **reading-to-action** 仪表盘。它不做通用任务管理，而是把「读到的想法」压缩成少量可执行动作，按「值得做 × 做得到」排序，让你只关注当前这一件。

## 它解决什么

- 博客收藏了很多，真正用起来的很少；
- 方法看得越多，越不知道下一步做什么；
- 普通 TODO 只记录「要做」，不解决「从哪开始」；
- 缺少反馈，做完/放弃都没有留下可复盘的原因。

## 核心闭环

1. 粘贴一篇博客的 URL
2. DeepSeek 提取摘要、关键观点、3–7 个行动
3. 你复核、修改、保留（AI 不自动替你决定）
4. 仪表盘按推荐分排序，默认只突出一个「现在做」
5. 完成或放弃时记录原因
6. 周末复盘：做了什么、卡在哪、下周只做哪几件

## 不是什么

- 不是通用 TODO：v0 不做手动记录、日历、GitHub Issue、笔记集成
- 不是稍后读/书签工具：输入是 URL，输出是行动
- 不是自动执行器：所有行动必须经过你确认

## 数据与文件

- 本地优先，文件即数据库；
- 每个行动是一个 Markdown 文件（YAML frontmatter），可以直接用 Obsidian / VS Code 打开；
- `todo-dashboard.exe --source <url>` 可从命令行导入；
- 支持导出 JSON / Markdown，数据随时带走。

## 项目状态

**Pre-alpha / PRD v0.2 待确认**。平台已定为 Windows 桌面；技术栈提议 Tauri 2 + React + TypeScript（见 ADR-0001，待确认）。

- [x] 仓库初始化
- [x] 项目章程 / 工程规范
- [x] PRD v0.2 草稿
- [ ] PRD 评审
- [ ] 技术栈 ADR 确认
- [ ] M1 垂直切片（URL → 分析 → 复核 → 文件 → 列表）
- [ ] 两周 dogfood

## 文档

- [项目章程](docs/CHARTER.md) — 为什么做、为谁做、不做什么
- [PRD](docs/PRD.md) — v0.2，待评审
- [方法论](docs/METHODOLOGY.md) — GTD、Personal Kanban、ICE/WSJF、执行意图
- [工程规范](docs/ENGINEERING.md) — 分支、提交、质量门禁与协作约定
- [ADR-0001](docs/adr/0001-desktop-stack.md) — Windows 技术栈提案

## License

[MIT](LICENSE)
