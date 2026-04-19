# Project Stage Analysis

**Date**: 2026-04-19
**Stage**: Pre-Production
**Stage Confidence**: PASS — 信号清晰，设计/配置完成，源代码尚未开始

---

## Completeness Overview

| Domain | Completeness | Status |
|--------|-------------|--------|
| **Design** | ~85% | 5/5 MVP GDD 完成 (restaurant, diner, card, turn-flow, economy)；game-concept.md 存在；systems-index.md 完整 |
| **Code** | ~5% | 34 个 .cs 文件，全部为 TEngine 脚手架 (GameEntry, Procedure*, UIModule 模板, LoginUI/BattleMainUI 示例)，无业务逻辑代码 |
| **Architecture** | 0% | 无 ADR，无架构概览文档 |
| **Config Tables** | ~90% | 17 张 Luban 表已建 (7 枚举 + 5 Bean + ~90 行初始数据)，待 CLI 导出验证 |
| **Production** | ~20% | 有 session-state/active.md，无 sprint plan、milestone、roadmap |
| **Tests** | 0% | 无测试文件 |
| **Prototype** | Done | core-loop 原型验证通过 (PROCEED)，有 REPORT.md |
| **Narrative** | 0% | 无叙事设计文档 |
| **Levels** | 0% | 无关卡设计文档 |
| **UX** | 0% | 无 UX 设计文档 |

---

## Artifact Inventory

### Design Documents (`design/gdd/`)

| File | Sections | Status |
|------|----------|--------|
| game-concept.md | Overview, Core Loops, Mechanics, Economy, Pacing | Designed (早期格式) |
| systems-index.md | 完整 (10 系统, 依赖图, 优先级分层) | Approved |
| restaurant-system.md | 8 必需 + Visual/Audio + UI + Open Questions | Designed (pending review) |
| diner-system.md | 8 必需 + Visual/Audio + UI + Open Questions | Designed (pending review) |
| card-system.md | 8 必需 + Visual/Audio + UI + Open Questions | Designed (pending review) |
| turn-flow-system.md | 8 必需 + Visual/Audio + UI + Open Questions | Designed (pending review) |
| economy-system.md | 8 必需 + Visual/Audio + UI + Open Questions | Designed (pending review) |

### Source Code (`Assets/GameScripts/`)

| Directory | Files | Purpose |
|-----------|-------|---------|
| GameEntry.cs | 1 | Unity 入口 |
| Procedure/ | 12 | TEngine 启动流程链 |
| HotFix/GameLogic/Module/UIModule/ | 10 | UI 框架基础类 |
| HotFix/GameLogic/UI/ | 2 | 示例 UI (LoginUI, BattleMainUI) |
| HotFix/GameLogic/SingletonSystem/ | 3 | 单例基类 |
| HotFix/GameProto/LubanLib/ | 4 | Luban 运行时库 |
| HotFix/GameLogic/ | 3 | GameApp, GameModule, IEvent |

### Config Tables (`Configs/GameConfig/Datas/`)

| Category | Tables | Rows | Status |
|----------|--------|------|--------|
| 基础数据 | TbRace, TbGlobalConst, TbShopConfig, TbFlavorToFaceValue | 28 | Schema + Data |
| Restaurant | TbSeatType, TbFacility, TbRestaurantProgress | 26+ | Schema + Data |
| Diner | TbTrait, TbEmotionPath, TbDinerTemplate, TbDinerPool, TbBossTemplate | 37 | Schema + Data |
| Card | TbStartingCardConfig, TbCardTemplate, TbCardSellPrice | 19 | Schema + Data |
| TurnFlow | TbStage | 21 | Schema + Data |
| Economy (预留) | TbRelic | 0 | Schema only |

### Prototype (`prototypes/core-loop/`)

- **Status**: Concluded (PROCEED)
- **Artifacts**: REPORT.md, SETUP.md
- **Missing**: README.md (按原型规范需要)
- **Conclusion**: 核心循环验证通过，卡牌耐久度 + 吞噬转化机制产生有意义策略决策

---

## Gaps Identified

### Critical (blocks Production entry)

1. **Luban CLI 未安装，配置表未导出验证**
   - 17 张表已建但未运行 `gen_code_bin_to_project_lazyload.bat` 验证
   - 问题：导出是否成功？C# 代码生成是否正确？这是 Production 前必须解除的阻塞项

2. **无架构决策记录 (ADR)**
   - 项目使用 TEngine + HybridCLR + YooAsset + UniTask + Luban 技术栈，但无正式记录
   - 问题：是故意跳过等编码时再补，还是现在就写 ADR-001？

### Important (should address before/during early Production)

3. **无 Sprint Plan / Milestone**
   - systems-index.md 有推荐实施顺序，但无正式迭代计划
   - 问题：你在用什么方式跟踪工作进度？是否需要用 `/sprint-plan` 建立第一个迭代？

4. **原型目录缺少 README.md**
   - `prototypes/core-loop/` 有 REPORT.md 但按项目规范需要 README.md
   - 问题：需要补一个 README，还是 REPORT.md 已经足够？

5. **game-concept.md 格式不统一**
   - 5 个系统 GDD 使用标准 8 章节模板，但 game-concept.md 是早期格式
   - 问题：是否需要更新为统一格式？还是作为历史文档保留？

### Advisory (can address during Production)

6. **无测试基础设施** — 无 tests/ 目录，无测试框架配置
7. **无叙事/关卡/UX 设计** — Vertical Slice 阶段才需要，当前不阻塞
8. **Design Review 未执行** — 5 个 GDD 标注 "pending review"，尚未运行 `/design-review`

---

## Stage Classification Rationale

| Indicator | Observed | Points To |
|-----------|----------|-----------|
| Game concept doc | Complete | Pre-Production+ |
| Systems index | 10 systems enumerated | Pre-Production+ |
| MVP GDDs | 5/5 complete with formulas | Pre-Production+ |
| Prototype | Core loop validated | Pre-Production+ |
| Engine configured | Unity + TEngine initialized | Pre-Production+ |
| Config tables | 17 tables built | Pre-Production+ |
| Game logic code | 0 business logic files | Pre-Production (not yet Production) |
| Architecture docs | None | Pre-Production (gap) |
| Sprint plan | None | Pre-Production (gap) |
| Tests | None | Pre-Production (acceptable) |

**Conclusion**: Project is firmly in **Pre-Production**. Design and configuration are mature, prototype validated the concept. The project is ready for the Pre-Production gate check and, upon passing, can begin Production sprint 1.

---

## Recommended Next Steps

| Priority | Action | Skill/Tool |
|----------|--------|------------|
| **P0** | 安装 Luban CLI 并验证配置表导出 | 手动 + `/luban-dev` |
| **P0** | 运行 `/gate-check pre-production` 评估是否可进入 Production | `/gate-check` |
| **P1** | 补写 ADR-001 (TEngine 框架选型) | `/architecture-decision` |
| **P1** | 建立 Sprint 1 计划 | `/sprint-plan` |
| **P1** | 运行 `/review-all-gdds` 交叉审查 5 个 GDD | `/review-all-gdds` |
| **P2** | 补 prototype README.md | `/reverse-document concept prototypes/core-loop` |
| **P2** | 清理旧 demo bean/enum (item.ItemExchange, test.*) | `/luban-dev` |
| **P3** | 建立测试框架 | `/test-setup` |
| **P3** | 创建架构概览文档 | `/create-architecture` |
