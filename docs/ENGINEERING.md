# 工程规范与协作约定

> 状态：v0.1（引导期）· 从首次提交起生效 · 变更需 PR + ADR

## 0. 总则

- 先定义问题，再选择技术。
- 可运行 > 完整；可验证 > 感觉。
- 小步提交，持续集成，`main` 始终可发布。
- 决策留痕：聊天记录不是事实来源，PRD / ADR / Issue 才是。
- 不为假设中的未来过度设计。

## 1. 单一事实来源

| 文档 | 回答什么 |
| --- | --- |
| `README.md` | 是什么、怎么跑 |
| `docs/PRD.md` | 为谁解决什么问题、验收标准 |
| `docs/CHARTER.md` | 为什么做、不做什么 |
| `docs/adr/` | 为什么这样选 |
| `CHANGELOG.md` | 每个版本发生了什么 |

规则：口头或聊天中做出的重要决定，24 小时内落进仓库；否则视为未决定。

## 2. 分支与发布

- Trunk-based：`main` 常绿，使用短期分支 `feat/<slug>`、`fix/<slug>`、`docs/<slug>`、`chore/<slug>`。
- 引导期首次提交可直接进 `main`；之后 `main` 受保护，所有变更走 PR。
- 合并策略：Squash merge；PR 标题遵循 Conventional Commits。
- 发布：SemVer + tag（`v0.1.0`，预发布 `v0.2.0-rc.1`）。
- 不设长期 `develop` 分支；不做长期 fork。

## 3. 提交信息

格式：`<type>(<scope>): <subject>`

type：`feat`、`fix`、`docs`、`refactor`、`test`、`chore`、`ci`、`perf`、`build`。

- subject 用祈使句，≤ 72 字符，结尾不加句号。
- 正文解释 why，而不是复述 what。
- 一个提交只做一件事；不把半成品提交到 `main`。
- 禁止 `update`、`fix bug`、`wip` 作为唯一信息。

示例：

```text
feat(today): show one next action above the fold

The dashboard previously listed every task equally, which recreated
the decision paralysis the product is meant to solve.
```

## 4. Pull Request

- 小而聚焦：目标 < 400 行有效 diff；超出先拆。
- 描述必须包含：问题、方案、取舍、验证方式；UI 变更附截图/录屏。
- 关联 Issue；无 Issue 的小改动需说明原因。
- 作者先自审；至少 1 次 review（早期可以是 self-review checklist + CI）。
- CI 全绿才可合并；不 bypass。紧急 bypass 必须记录原因和回滚方案。

## 5. 质量门禁

流水线至少包含：

1. format check
2. lint
3. type check（如适用）
4. unit tests
5. build
6. e2e smoke（核心流程）
7. dependency / security scan

- 覆盖率不作为 KPI；核心路径必须有测试。
- 每个 bug fix 先补回归测试。
- 测试命名描述行为，不描述实现。

## 6. 代码与依赖

- 先读再改，遵循现有风格。
- 业务逻辑与 UI / IO 分离；模块边界清晰。
- 不引入「可能有用」的依赖；新增依赖需说明体积、维护状态、许可证。
- 不提交生成物、密钥、个人数据。
- 复杂逻辑注释解释 why；不写复述代码的注释。

## 7. 安全与隐私

- 密钥只进环境变量 / 密钥管理；仓库只留 `.env.example`。
- 最小权限；OAuth scope 最小化。
- Local-first；任何上传 / 同步必须显式告知、可关闭、可导出。
- 依赖漏洞扫描；高危阻断合并。
- 提交前检查个人信息（真实姓名、邮箱、本机路径、token）。

## 8. UX / 可访问性 / 性能（桌面 WebView 适用）

- WCAG 2.2 AA：键盘可达、焦点可见、对比度达标、语义化标签。
- 性能预算（75th percentile，中端移动设备）：LCP ≤ 2.5s，INP ≤ 200ms，CLS ≤ 0.1。
- 必须有空状态、加载态、错误态。
- UI 用 Web 技术实现，但交付形态是 Windows 桌面；浏览器版本不作为 v0 目标。

## 9. 完成的定义（Definition of Done）

- 验收标准全部满足
- 测试通过，CI 绿
- 相关文档更新（README / PRD / ADR / CHANGELOG）
- 无密钥 / 隐私泄露
- 可回滚（feature flag 或 revert 方案）
- 有演示证据（截图 / 录屏 / 日志）

## 10. 协作约定：我会质疑的情况

出现以下情况，我会先指出风险、给替代方案，再执行；如果你坚持，我会把它记录为 ADR 例外，而不是沉默执行：

1. 没有验收标准就开始写代码；
2. 先定技术栈，后补问题定义；
3. 范围蔓延（「顺便再加一个……」）；
4. 直接 push `main` 或跳过 CI；
5. 把密钥、token、个人数据写进仓库；
6. 用「差不多完成了」代替可验证的完成；
7. 长期不复盘、不记录决策；
8. 为了「以后可能用得上」引入抽象 / 依赖。

## 11. 引导期例外

- 首次提交允许直接 push `main`（仅限初始化）。
- PRD 与技术选型确定前，不引入框架 / 依赖。
- 开源前必须确定 LICENSE；已暂定 MIT，待最终确认。
- CI 在技术栈确定后 1 个 PR 内补齐；补齐前以本地检查为准。

## 12. 待定决策（将记录为 ADR）

- Tauri vs .NET / WPF（见 ADR-0001）
- Local-first vs 云同步
- 技术栈
- License
- 是否引入 AI 建议，及隐私边界
- 遥测：默认关闭
