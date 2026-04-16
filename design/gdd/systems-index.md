# Systems Index: 深渊食堂 (DeepDiner)

> **Status**: Approved
> **Created**: 2026-04-16
> **Last Updated**: 2026-04-16
> **Source Concept**: design/gdd/game-concept.md

---

## Overview

《深渊食堂》是一款 Rogue-lite 卡牌构筑 + 餐厅模拟经营游戏。玩家在深渊经营怪物食堂，通过随机分配的食客、可打出加菜的食材卡、以及"付钱还是被吃掉"的判定机制，构成核心策略循环。系统范围覆盖卡牌生命周期管理、食客属性与分配、餐厅座位与设施、回合流程编排、经济与商店、遗物被动效果、Boss/变异事件、跨 Run 元进度、食谱进化分支。MVP 需要前 5 个系统跑通"分配食客 → 出牌加菜 → 结算吞噬 → 商店购买 → 下一轮"的核心循环。

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | 餐厅系统 (Restaurant) | Foundation | MVP | Not Started | — | — |
| 2 | 食客系统 (Diner) | Core Gameplay | MVP | Not Started | — | 餐厅系统 |
| 3 | 卡牌系统 (Card) | Core Gameplay | MVP | Not Started | — | 食客系统 |
| 4 | 回合流程系统 (Turn Flow) | Game Flow | MVP | Not Started | — | 餐厅系统, 食客系统, 卡牌系统 |
| 5 | 经济系统 (Economy) | Economy | MVP | Not Started | — | 回合流程系统 |
| 6 | 遗物系统 (Relic) | Economy | Vertical Slice | Not Started | — | 经济系统, 卡牌系统 |
| 7 | 事件系统 (Event) | Game Flow | Vertical Slice | Not Started | — | 回合流程系统, 食客系统 |
| 8 | 元进度系统 (Meta-Progression) | Progression | Alpha | Not Started | — | 回合流程系统 |
| 9 | 食谱进化系统 (Recipe Evolution) | Progression | Alpha | Not Started | — | 卡牌系统, 元进度系统 |
| 10 | 润色包 (Polish) | Polish | Full Vision | Not Started | — | 全部 |

---

## Categories

| Category | Description | Systems |
|----------|-------------|---------|
| **Foundation** | 视觉画布和基础数据结构，其他系统在此基础上构建 | 餐厅系统 |
| **Core Gameplay** | 玩家核心交互对象和策略引擎 | 食客系统, 卡牌系统 |
| **Game Flow** | 游戏节奏编排和流程控制 | 回合流程系统, 事件系统 |
| **Economy** | 资源的产出与消耗循环 | 经济系统, 遗物系统 |
| **Progression** | 跨 Run 的长期成长 | 元进度系统, 食谱进化系统 |
| **Polish** | 体验润色（原型阶段后置） | 润色包 |

---

## Priority Tiers

| Tier | Definition | Target Milestone | Design Urgency |
|------|------------|------------------|----------------|
| **MVP** | 核心循环可玩，能回答"这个游戏好玩吗？" | 第一个可玩原型 | Design FIRST |
| **Vertical Slice** | 一次完整体验，包含深度和变化 | 垂直切片 / Demo | Design SECOND |
| **Alpha** | 所有功能就位（粗糙形式可接受） | Alpha 里程碑 | Design THIRD |
| **Full Vision** | 润色、边缘情况、体验完善 | Beta / Release | Design as needed |

---

## Dependency Map

### Foundation Layer (no dependencies)

1. **餐厅系统** — 提供座位布局和设施定义，是所有空间交互的基础画布

### Core Gameplay Layer (depends on foundation)

1. **食客系统** — depends on: 餐厅系统（食客需要分配到座位上）
2. **卡牌系统** — depends on: 食客系统（吞噬转化需要食客种族数据；出牌需要食客作为目标）

### Feature Layer (depends on core)

1. **回合流程系统** — depends on: 餐厅系统, 食客系统, 卡牌系统（编排全局流程）
2. **经济系统** — depends on: 回合流程系统（奖券收益由结算触发）

### Feature+ Layer (depends on feature)

1. **遗物系统** — depends on: 经济系统（商店购买）, 卡牌系统（修改卡牌行为）
2. **事件系统** — depends on: 回合流程系统（事件触发时机）, 食客系统（Boss 是特殊食客）

### Meta Layer (depends on feature)

1. **元进度系统** — depends on: 回合流程系统（Run 完成触发存档）
2. **食谱进化系统** — depends on: 卡牌系统（吞噬事件）, 元进度系统（熟练度持久化）

### Polish Layer (depends on everything)

1. **润色包** — depends on: 全部

---

## Recommended Design Order

| Order | System | Priority | Layer | Agent(s) | Est. Effort |
|-------|--------|----------|-------|----------|-------------|
| 1 | 餐厅系统 | MVP | Foundation | game-designer | S |
| 2 | 食客系统 | MVP | Core Gameplay | game-designer | M |
| 3 | 卡牌系统 | MVP | Core Gameplay | game-designer | L |
| 4 | 回合流程系统 | MVP | Game Flow | game-designer + systems-designer | L |
| 5 | 经济系统 | MVP | Economy | game-designer + economy-designer | M |
| 6 | 遗物系统 | Vertical Slice | Economy | game-designer | S |
| 7 | 事件系统 | Vertical Slice | Game Flow | game-designer | M |
| 8 | 元进度系统 | Alpha | Progression | game-designer | S |
| 9 | 食谱进化系统 | Alpha | Progression | game-designer + systems-designer | M |
| 10 | 润色包 | Full Vision | Polish | multidisciplinary | M |

> Effort estimates: S = 1 session, M = 2-3 sessions, L = 4+ sessions.

---

## Circular Dependencies

- None found. All dependencies flow in a single direction: Restaurant → Diner → Card → Turn Flow → Economy/Events → Meta.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| 回合流程系统 | Design | 4 个系统依赖它作为编排器，流程设计出错影响面最大；"提前结束机制"和"轮次结算"的时机判定可能产生边界问题 | 早期原型验证核心循环，先实现最简流程再逐步添加机制 |
| 卡牌系统 | Design | 耐久度 + 吞噬转化 + 遗物修改 + 食谱进化四级叠加，卡牌状态机复杂度高；数值平衡直接影响策略深度 | 先定义清晰的卡牌状态机，预留 Buff/Modifier 接口供遗物和食谱进化接入 |
| 经济系统 | Scope | 奖券获取途径多（日常、提前结束、利息、出售、Boss），消耗途径多（遗物、设施、黑市），经济平衡需要大量测试 | 先用简化数值跑通循环，后续通过 Luban 配置表调参 |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 10 |
| Design docs started | 0 |
| Design docs reviewed | 0 |
| Design docs approved | 0 |
| MVP systems designed | 0/5 |
| Vertical Slice systems designed | 0/2 |

---

## Next Steps

- [ ] Design MVP-tier systems in order: 餐厅 → 食客 → 卡牌 → 回合流程 → 经济
- [ ] Run `/design-review` on each completed GDD
- [ ] Prototype the core loop after MVP GDDs are complete (`/prototype`)
- [ ] Run `/gate-check pre-production` when MVP systems are designed and prototyped
- [ ] Design Vertical Slice systems (遗物, 事件) after MVP is validated
