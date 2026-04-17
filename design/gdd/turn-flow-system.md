# 回合流程系统 (Turn Flow System)

> **Status**: In Design
> **Author**: zhangtielin + agents
> **Last Updated**: 2026-04-17
> **Implements Pillar**: 高易用性, 动态资源管理, 涌现式策略

## Overview

回合流程系统是《深渊食堂》的**全局节奏编排器**。从数据角度，它管理从"局开始"到"局结束"的完整时间结构——局（21 天）、日（最多 4 个轮次 + 商店阶段）、轮次（食客分配 → 玩家操作 → 结算）三级嵌套的时序控制。它决定食客何时分配、玩家何时可出牌、结算何时触发、商店何时开放、日目标何时检查。回合流程系统不实现具体逻辑（吞噬判定由餐厅系统执行、情绪变化由食客系统驱动），而是确保各系统在正确的时机、以正确的顺序被调用。

从玩家角度，回合流程系统定义了游戏的**心跳节奏**——每轮次是"观察→决策→行动→结果"的紧凑循环：机械手甩下食客，玩家审视猎物，精准出牌加菜，看着食客情绪从轻松滑向恐惧，最后按结束键收网。轮次间的节奏感由出牌配额控制（紧迫感），日间的节奏感由营业额目标驱动（压力递增），局间的节奏感由商店和牌库进化提供（成长释放）。它让玩家永远处于"再来一局"的状态。

核心职责：

1. **局级管理**：21 天结构、局开始初始化（牌组、餐厅布局）、局结束判定
2. **日级管理**：日营业额目标检查、提前结束机制、商店阶段编排
3. **轮次级管理**：食客分配 → 玩家操作 → 结算的三阶段流转
4. **阶段边界控制**：明确阶段进入/退出条件，防止非法状态转换
5. **系统调用编排**：在正确时机以正确顺序调用餐厅/食客/卡牌系统的接口

## Player Fantasy

**核心幻想：** "我是深渊赌场的荷官——机械手每次甩下食客，就是新一轮赌局的开始。我有 5 次出牌机会、4 个轮次、1 个营业额目标。牌桌上的猎物在不断变化，我的手牌在不断消耗和重生，日目标像发条一样一天比一天紧。每一轮我都在和时间对赌：赌我的读心术够准，赌我的牌够用，赌我今天能活下来去商店里换取更强的筹码。"

**情感锚点：** **压力容器 × 节奏脉动**。玩家感受到"间歇性窒息"——轮次开始时深吸气（观察猎物），出牌时屏住呼吸（信息不完整的决策），结束时呼出一口气（结算释放）。日目标攀升让每次"深吸气"的时间窗口越来越短，商店让每次"呼气"越来越不轻松。整个游戏像一个不断加压的气缸，玩家在压缩和释放之间找到自己的呼吸节奏。

**关键玩家时刻：**

1. **轮次倒计时的指尖温度**：出牌配额剩 1，还有 3 张手牌、2 个没动过的食客。最后一张出牌权——打给谁？"最后一箭"的抉择是轮次节奏的峰值。

2. **日目标的倒计时钟**：第 3 轮结算后，累积 280/390，剩 1 轮。之前"随便打打"浪费的配额、放走的食客，账单在这一刻一起到。

3. **商店里的深呼吸**：日目标达成进入商店——不是休息区，是压力调节站。奖券有限、选择无穷，每个选择都在为明天的压力容器加码或减压。

**a-ha moment：** 玩家在第 7 天经历惊险一搏——最后一张出牌打中伪装型食客，消费恰好超出金币 3 点触发吞噬，产出面值 800 的卡牌。那一刻玩家意识到：**真正的赌注从来不是日目标——日目标只是倒计时器。真正的赌注是每一轮的出牌配额，每一次"再打一张还是收手"的选择。这个游戏不是模拟经营——它是一场 21 天的俄罗斯轮盘，每一天都是一发子弹，而我既是赌徒，也是荷官。**

## Detailed Design

### Core Rules

**规则 1：局级时间结构**

1. 每局游戏固定 21 天（MVP 不支持局外成长调整局长）
2. 21 天划分为 7 个**循环**，每个循环 3 天：2 个普通日 + 1 个 Boss 日
   - Day 1,2 = 普通日，Day 3 = Boss 日
   - Day 4,5 = 普通日，Day 6 = Boss 日
   - ...以此类推，Day 21 = 第 7 循环的 Boss 日
3. **Boss 日规则**：Boss 日的最后一轮（第 maxRounds 轮）额外分配 1 个 Boss 食客到餐厅（其他轮次正常）
4. Boss 食客难度随循环递增（第 1 循环 Boss 最简单，第 7 循环 Boss 最难），具体属性由 TbBossTemplate 配置

**规则 2：局开始初始化**

局开始时执行以下步骤：

1. **创建起手牌组**：从 TbStartingCardConfig 读取配置，生成初始食材卡加入玩家手牌
2. **初始化餐厅**：创建 4 个木椅座位（S1），清空全局设施槽位
3. **初始化局级状态**：DayNumber = 1，CumulativeRunRevenue = 0
4. **进入第 1 天**

**规则 3：局结束判定**

局结束有且仅有两种结局：

1. **胜利（RunComplete）**：第 21 天（Boss 日）日目标达成 → 局胜利，触发元进度结算
2. **失败（RunFailed）**：任意一天的 maxRounds 轮全部结束后，累积日营业额 < 日目标 → 局立即结束，游戏失败

**规则 4：日级时间结构**

每个日包含以下阶段，按顺序执行：

```
日开始 (DayInit)
  → 重置日营业额 = 0
  → 从 TbStage 加载当日食客池配置
  → 判断日类型（普通日 / Boss 日）
  → 进入轮次循环

轮次循环 (RoundLoop)
  → 最多 maxRounds 轮（默认 4）
  → 每轮：食客分配 → 玩家操作 → 轮次结算
  → 每轮结算后检查：累积日营业额 ≥ DailyTarget → 提前结束日

日结算 (DaySummary)
  → 检查日目标是否达成
  → 未达成 → Game Over
  → 达成 → 进入商店阶段

商店阶段 (ShopPhase)
  → 购买座位替换 / 全局设施
  → 出售卡牌
  → 购买遗物（10-20 个，TbRelic 配置表驱动）
  → 玩家主动点击"完成"结束商店

日结束 → DayNumber += 1 → 进入下一天
```

**规则 5：日营业额累积**

1. 每个轮次结算后，将该轮次产生的所有收入（食客付费 + 吞噬金币 + 特性额外收入）累加到 `DailyRevenue`
2. 每个轮次结算完成后检查：`DailyRevenue >= DailyTarget`
3. 若达成：标记当前轮次为"最后一轮"，跳过剩余轮次，进入 DaySummary
4. 若未达成且 `roundNumber < maxRounds`：进入下一轮次
5. 若未达成且 `roundNumber == maxRounds`：进入 DaySummary → 判定失败

**规则 6：商店阶段（MVP 扩展版）**

1. 商店在日目标达成后开放（日失败无商店）
2. 商店提供以下选项（由 TbShopInventory 配置每日陈列内容）：
   - **座位替换**：购买新座位类型替换现有座位（餐厅系统接口）
   - **全局设施**：购买全局设施放入 3 个槽位（餐厅系统接口）
   - **卡牌出售**：出售手牌中的食材卡换奖券（卡牌系统接口）
   - **遗物购买**：10-20 个遗物由 TbRelic 配置表定义，每日随机陈列 subset（经济系统接口）
3. 商店陈列数量受餐厅系统全局修饰符"商店陈列数量"影响
4. 商店价格受餐厅系统全局修饰符"商店打折"影响
5. 玩家主动点击"完成购物"结束商店阶段
6. 商店阶段结束后，座位替换和新增设施在**次日生效**

**规则 7：轮次级流程**

每个轮次分为三个阶段：

```
轮次开始 (RoundInit)
  1. 重置出牌配额：调用卡牌系统 ResetPlayQuota(roundIndex)
  2. 食客分配：调用食客系统 GenerateAndAssignDiners()
     - 从食客池抽取食客分配到空座位
     - 食客落座，产生基础餐费
     - 展示初始情绪
  3. Boss 食客分配（仅 Boss 日最后一轮）：
     - 从 TbBossTemplate 加载对应循环的 Boss
     - Boss 分配到指定座位（如果无空座位则替换最弱食客）
  4. 进入玩家操作阶段

玩家操作 (PlayerTurn)
  玩家可执行以下操作：
  a. 出牌：选择手牌 → 选择目标食客 → 执行加菜流程
  b. 查看食客信息：查看公开的视觉外观和当前情绪
  c. 查看手牌信息：查看手牌属性
  d. 结束轮次：主动结束当前轮次

  结束轮次触发条件：
  - 玩家点击"结束轮次"（主动）
  - 所有座位空置（所有食客被吞噬/霸王餐）
  - 出牌配额耗尽不自动结束（玩家仍可查看信息）

轮次结算 (RoundSettle)
  1. 快照采集：对所有 Occupied 座位的食客采集当前状态快照
  2. 串行结算：按座位索引升序逐个判定
  3. 释放座位：所有结算完成的食客释放座位
  4. 计算轮次收入，累加到 DailyRevenue
```

**规则 8：轮次结算的串行判定流程**

对每个 Occupied 座位的食客执行以下判定：

1. **霸王餐概率判定**（餐厅系统 F5）：若命中 → DineAndDash，食客支付 0 离场
2. **吞噬概率判定**（餐厅系统 F4）：若命中 → 进入吞噬流程（含 T-D1 抵御检查）
3. **超支吞噬判定**：TotalCost > EffectiveDinerGold → 吞噬
4. **付费成功**：TotalCost ≤ EffectiveDinerGold → 食客付费离场
5. **T-D2 连坐处理**：吞噬发生时，从**尚未结算的食客**中随机选一个，标记为"连坐吞噬"

**结算顺序规则**：
- 按座位索引升序排列（确定性顺序）
- 已被 T-D2 标记为"连坐吞噬"的食客跳过正常判定，直接进入吞噬流程
- **已结算的结果不可撤回**：T-D2 只能影响尚未结算的食客

**规则 9：提前结束检查**

1. **全部空位检查**：玩家操作阶段，若所有座位变为 Empty → 立即跳转轮次结算（结算队列为空，跳过结算）
2. **日目标检查**：轮次结算完成后（所有食客已结算），检查 `DailyRevenue >= DailyTarget`
3. 不在结算过程中实时检查（保证结算的原子性）

### States and Transitions

**回合流程系统采用"协调器 + 三阶段 FSM"架构**：

```
TurnFlowCoordinator（协调器）
├── RunFsm（局状态机）
│   ├── RunInit → DayLoop → RunEnd_Win / RunEnd_Lose
├── DayFsm（日状态机）
│   ├── DayInit → RoundLoop → DaySummary → ShopPhase
└── RoundFsm（轮次状态机）
    ├── RoundInit → PlayerTurn → RoundSettle
```

**RunFsm 状态表**：

| 状态 | 描述 | 进入条件 | 退出条件 |
|------|------|---------|---------|
| RunInit | 局初始化 | 游戏开始 | 牌组+餐厅初始化完成 |
| DayLoop | 日循环 | RunInit 完成 / 上一天结束 | DayNumber > 21(胜利) 或 日失败(失败) |
| RunEnd_Win | 局胜利 | 第21天日目标达成 | 触发元进度结算 |
| RunEnd_Lose | 局失败 | 任意天日目标未达成 | 触发失败结算 |

**DayFsm 状态表**：

| 状态 | 描述 | 进入条件 | 退出条件 |
|------|------|---------|---------|
| DayInit | 日初始化 | DayLoop 开始新日 | 食客池加载完成 |
| RoundLoop | 轮次循环 | DayInit 完成 | maxRounds 轮完成 OR 提前结束 |
| DaySummary | 日结算 | RoundLoop 结束 | 日目标检查完成 |
| ShopPhase | 商店阶段 | 日目标达成 | 玩家完成购物 |

**RoundFsm 状态表**：

| 状态 | 描述 | 进入条件 | 退出条件 |
|------|------|---------|---------|
| RoundInit | 轮次初始化 | RoundLoop 开始新轮 | 食客分配完成 + 配额重置完成 |
| PlayerTurn | 玩家操作 | RoundInit 完成 | 玩家结束轮次 OR 全部座位空置 |
| RoundSettle | 轮次结算 | PlayerTurn 结束 | 所有食客结算完成 |

### Interactions with Other Systems

| 交互方向 | 对方系统 | 数据流向 | 接口 |
|----------|---------|---------|------|
| 回合流程 → 餐厅 | 餐厅系统 | 日开始加载布局 | `LoadDayLayout(dayNumber)` |
| 回合流程 → 餐厅 | 餐厅系统 | 日结束重置座位 | `ResetAllSeats()` |
| 回合流程 → 餐厅 | 餐厅系统 | 查询日营业额目标 | `GetDailyTarget(dayNumber)` — 返回 F8 结果 |
| 回合流程 → 食客 | 食客系统 | 触发食客生成分配 | `GenerateAndAssignDiners()` |
| 回合流程 → 食客 | 食客系统 | 触发轮次结算 | `SettleRound()` — 逐食客判定 |
| 回合流程 → 卡牌 | 卡牌系统 | 轮次开始重置配额 | `ResetPlayQuota(roundIndex)` |
| 回合流程 → 卡牌 | 卡牌系统 | 局开始创建起手牌组 | `CreateStartingDeck()` |
| 回合流程 → 经济 | 经济系统 | 商店阶段购买/出售 | `OpenShop(shopInventory)` — 经济系统处理交易 |
| 回合流程 → 经济 | 经济系统 | 日结算触发奖券获取 | `AwardTickets(dailyRevenue, bonus)` |
| 回合流程 ← 餐厅 | 餐厅系统 | 座位状态变化通知 | 事件 `OnSeatStateChanged(seatIndex, newState)` |
| 回合流程 ← 食客 | 食客系统 | 全部空位通知 | 事件 `OnAllDinersRemoved` |
| 回合流程 ← 卡牌 | 卡牌系统 | 出牌事件通知 | 事件 `OnCardPlayed`, `OnCardExhausted` |
| 回合流程 → UI | UI 系统 | 阶段切换通知 | 阶段变更事件（供 UI 切换画面） |
| 回合流程 → 事件(预留) | 事件系统 | 事件钩子 | `OnBeforeRoundStart`, `OnAfterRoundSettle`, `OnBeforeShopPhase` 等（空实现） |

## Formulas

> **设计原则**：回合流程系统是全局编排器，本节公式主要是聚合规则和时序判定。具体数值计算（餐费、金币、概率）由对应系统负责。

### FT1：轮次收入汇总

`RoundRevenue = Σ(SettlementAmount_i)` for i in SettledDiners

**变量：**

| 变量 | 类型 | 范围 | 描述 |
|------|------|------|------|
| SettlementAmount_i | int | ≥ 0 | 第 i 个食客的结算金额 |
| SettledDiners | list | — | 本轮已结算食客（按座位索引升序） |

**结算金额规则：**
- 付费离场 (PAY_SUCCESS)：SettlementAmount = TotalCost（餐厅系统 F6）
- 被吞噬 (DEVOUR)：SettlementAmount = EffectiveDinerGold + T-D3额外 + T-D4额外
- 霸王餐 (DineAndDash)：SettlementAmount = 0

**输出范围：** ≥ 0
**示例：** 食客A付费(TotalCost=36) + 食客B吞噬(Gold=140) + 食客C霸王餐(0) → 176

### FT2：日营业额累积

`DailyRevenue = Σ(RoundRevenue_j)` for j = 1..CompletedRounds

**变量：**

| 变量 | 类型 | 范围 | 描述 |
|------|------|------|------|
| RoundRevenue_j | int | ≥ 0 | 第 j 轮次收入（FT1 输出） |
| CompletedRounds | int | [1, MaxRounds] | 当天已完成轮次数 |

**输出范围：** ≥ 0
**示例：** Day 5，3轮收入 176 + 220 + 145 = 541

### FT3：日目标达成判定

```text
DayResult =
  if DailyRevenue >= DailyTarget → TARGET_MET
  else if roundNumber < MaxRounds → CONTINUE
  else → GAME_OVER
```

**变量：**

| 变量 | 类型 | 范围 | 描述 |
|------|------|------|------|
| DailyRevenue | int | ≥ 0 | FT2 输出 |
| DailyTarget | int | > 0 | 餐厅系统 F8 输出 |
| roundNumber | int | [1, MaxRounds] | 当前轮次序号 |
| MaxRounds | int | [1, 8] | 每日最大轮次（TbStage 配置，默认 4） |

**输出：** `TARGET_MET` | `CONTINUE` | `GAME_OVER`

### FT4：提前结束奖券奖励

当日在所有轮次用尽前达成日目标（TARGET_MET 且 CompletedRounds < MaxRounds），剩余未使用的轮次转化为奖券：

`EarlyBonusTickets = UnusedRounds × TicketPerUnusedRound`

**变量：**

| 变量 | 类型 | 范围 | 描述 |
|------|------|------|------|
| UnusedRounds | int | [0, MaxRounds-1] | 剩余未使用的轮次数 = MaxRounds - CompletedRounds |
| TicketPerUnusedRound | int | > 0 | 每个未使用轮次对应的奖券数（TbShopConfig 配置） |
| EarlyBonusTickets | int | ≥ 0 | 提前结束奖励的奖券数 |

**输出范围：** ≥ 0（CompletedRounds == MaxRounds 时为 0）
**示例：** MaxRounds=4，第 2 轮达成目标，UnusedRounds=2，TicketPerUnusedRound=15 → EarlyBonusTickets = 2 × 15 = 30

### FT5：商店陈列数量

`ShopDisplayCount = min(BaseShopDisplay + Σ(陈列加成), TotalPoolSize)`

**变量：**

| 变量 | 类型 | 范围 | 描述 |
|------|------|------|------|
| BaseShopDisplay | int | ≥ 1 | 基础陈列数（TbShopConfig） |
| 陈列加成 | int | ≥ 0 | 设施/遗物提供的额外陈列 |
| TotalPoolSize | int | ≥ 1 | 商品池总大小 |
| ShopDisplayCount | int | ≥ 1 | 实际陈列数量 |

**输出范围：** [1, TotalPoolSize]

### FT6：循环索引计算

`CycleIndex = ceil(DayNumber / CycleLength)`

| 变量 | 类型 | 范围 | 描述 |
|------|------|------|------|
| DayNumber | int | [1, 21] | 当前天数 |
| CycleLength | int | 3 | 每循环天数（常量） |
| CycleIndex | int | [1, 7] | 循环编号（查询 Boss 模板用） |

**示例：** Day 1→1, Day 3→1, Day 4→2, Day 21→7

### 数据流总览

```text
TbStage[DayNumber] → MaxRounds, 食客池, 日类型
TbBossTemplate[CycleIndex] → Boss 属性（Boss 日最后一轮）
餐厅系统 F8 → DailyTarget
轮次结算 → FT1 → RoundRevenue
FT1 × N轮 → FT2 → DailyRevenue
FT2 vs DailyTarget → FT3 → TARGET_MET/CONTINUE/GAME_OVER
TARGET_MET + UnusedRounds → FT4 → EarlyBonusTickets
TbShopConfig + 修饰符 → FT5 → ShopDisplayCount
FT6 → CycleIndex → Boss 日判定
```

## Edge Cases

### 时序边界

- **If 第1天第1轮次开始**：起手牌组由卡牌系统提供，出牌配额正常重置。无教程流程，与后续轮次结构一致。
- **If 第21天（最终Boss日）4轮结束未达标**：日失败=局失败，无宽限。
- **If Boss日最后一轮所有座位已被普通食客占满**：Boss食客**替换最弱食客**（按 DinerBaseGold 最低者）。被替换的食客不触发任何事件，直接离场。

### 状态组合冲突

- **If 提前结束与全部空位同时触发**：轮次结算后 DailyRevenue ≥ DailyTarget 且所有座位 Empty → 日级提前结束优先，进入 ShopPhase，不再检查轮次级条件。
- **If 出牌配额耗尽 + 仍有食客 + 手牌不为空**：玩家无法出牌但可查看信息，只能手动点击"结束轮次"。剩余手牌保留。
- **If RoundInit 时食客池为空**：系统级配置错误，无食客分配，全部座位 Empty，轮次立即结束。4轮后 DailyRevenue = 0，日失败。

### 极端数值

- **If 日营业额恰好为 0**：DailyRevenue(0) < DailyTarget(>0)，日失败=局失败。合法的最差结局。
- **If 第1轮收入即完成日目标**：日提前结束，剩余轮次转奖券（FT4）。进入 DaySummary → ShopPhase。
- **If 手牌为空但出牌配额未耗尽**：玩家无法出牌，只能结束轮次。本轮仅获基础餐费收入。

### 结算边界

- **If T-D2 连坐目标为尚未结算的食客**：目标食客被标记为"连坐吞噬"，跳过正常判定，直接进入吞噬流程。当串行结算到达该食客时，发现已不在 Dining 状态，跳过。
- **If T-D2 连坐目标为已结算的食客**：连坐无效，从剩余未结算食客中重新随机选择。若无未结算食客，连坐不触发。
- **If 快照记录结构（谁在哪），数值实时计算**：T-A3 爆金体质等动态特性在结算期间仍会影响数值。结算食客时使用当前最新值。
- **If 霸王餐与已结算食客的收益**：串行结算中食客 A 已付费入账，后续食客 B 触发霸王餐——A 的收益不受影响，B 的费用清零。"已结算不可撤回"。

### Boss 相关

- **If Boss 食客坐在 S5 触发霸王餐**：霸王餐优先于所有吞噬特性（食客 EC-8），Boss 支付 0 离场，不产出食材卡。Boss 不享有豁免权。
- **If Boss 食客被吞噬**：卡牌系统正常执行产卡。Boss 高 Flavor 值产出高面值食材卡，"高风险高回报"。

### 商店边界

- **If 进入商店阶段时奖券为 0**：ShopPhase 正常进入，玩家无法购买商品，只能跳过。合法状态。
- **If 手牌为空进入商店阶段**：无法出售卡牌换奖券，下一日开局无卡可出。
- **If DailyRevenue 恰好等于 DailyTarget**：日目标达成（≥ 判定），正常进入 ShopPhase。与餐厅系统 EC-8 一致。

## Dependencies

| 方向 | 系统 | 关系 | 数据接口 | 硬/软依赖 |
|------|------|------|---------|----------|
| 回合流程 ← | 餐厅系统 | **上游** | `LoadDayLayout(dayNumber)`, `ResetAllSeats()`, `GetDailyTarget(dayNumber)` — 日布局加载、座位重置、日目标查询 | **硬依赖** |
| 回合流程 ← | 食客系统 | **上游** | `GenerateAndAssignDiners()`, `SettleRound()` — 食客生成分配、轮次结算 | **硬依赖** |
| 回合流程 ← | 卡牌系统 | **上游** | `ResetPlayQuota(roundIndex)`, `CreateStartingDeck()` — 配额重置、起手牌组 | **硬依赖** |
| 回合流程 → | 经济系统 | **下游** | `OpenShop(shopInventory)`, `AwardTickets(dailyRevenue, bonus)` — 商店阶段、奖券发放 | **硬依赖** |
| 回合流程 → | 遗物系统 | **下游** | 商店阶段遗物陈列和购买（通过经济系统接口） | **软依赖（MVP 扩展）** |
| 回合流程 → | 事件系统 | **下游** | 事件钩子（`OnBeforeRoundStart`, `OnAfterRoundSettle`, `OnBeforeShopPhase`）— MVP 空实现 | **软依赖（预留）** |
| 回合流程 → | 元进度系统 | **下游** | 局胜利时触发元进度结算 | **软依赖（MVP 简化）** |
| 回合流程 → | UI 系统 | **下游** | 阶段切换通知、手牌/座位状态展示 | **硬依赖** |

**双向依赖验证**：

- 餐厅系统 GDD 列出"回合流程系统"为下游依赖 ✅
- 食客系统 GDD 列出"回合流程系统"为交互系统 ✅
- 卡牌系统 GDD 列出"回合流程系统"为交互系统 ✅
- 经济系统 GDD 未设计（需列出回合流程系统为上游依赖）
- 事件系统 GDD 未设计（需列出回合流程系统为事件触发源）
- 元进度系统 GDD 未设计（需列出回合流程系统为触发源）

## Tuning Knobs

| 旋钮 | 配置表 | 字段 | 安全范围 | 调高效果 | 调低效果 |
|------|--------|------|---------|---------|---------|
| 每日最大轮次数 | TbStage | max_rounds | [1, 8] | 更多操作空间，日目标更容易达成 | 更紧迫，配额压力更大 |
| 局长（天数） | 常量 | run_length | [7, 42] | 局更长，策略深度更大 | 局更短，节奏更快 |
| 循环长度 | 常量 | cycle_length | [2, 5] | Boss日间隔更久，普通日更多 | Boss日更频繁，压力更大 |
| 提前结束奖券倍率 | TbShopConfig | ticket_per_unused_round | [1, 50] | 提前结束奖励更丰厚，鼓励效率 | 提前结束收益低，倾向打满轮次 |
| 商店基础陈列数 | TbShopConfig | base_shop_display | [1, 20] | 更多商品可选 | 选择受限，策略空间小 |
| Boss难度曲线 | TbBossTemplate | 属性范围 | — | Boss更难，终局压力更大 | Boss更简单 |
| 日营业额基础目标 | TbRestaurantProgress | base_target | > 0 | 开局压力更大 | 开局更轻松 |
| 日营业额每日增长 | TbRestaurantProgress | target_growth | ≥ 0 | 后期压力飙升 | 后期压力平缓 |

> **注**：日营业额目标相关旋钮由餐厅系统 GDD 定义，此处仅列出回合流程系统的调参影响。Boss难度曲线由 TbBossTemplate 按循环索引配置属性范围，非单一数值。

## Visual/Audio Requirements

回合流程系统的视觉/音频需求将在 UX 设计阶段定义。阶段切换动画、轮次结算演出、Boss 登场特效等由 UX 规格书指定。

## UI Requirements

回合流程系统的 UI 需求将在 `/ux-design` 阶段定义，不在本 GDD 范围内。

### 局初始化和结束

1. **GIVEN** 玩家开始新一局, **WHEN** 回合流程系统执行 RunInit, **THEN** 依次完成：调用卡牌系统创建起手牌组、创建4个木椅座位(S1)且全部Empty、清空全局设施槽位、DayNumber=1、CumulativeRunRevenue=0，随后进入第1天DayInit

2. **GIVEN** DayNumber=21(第7循环Boss日), 当日maxRounds轮全部结算完毕, **WHEN** DailyRevenue >= DailyTarget, **THEN** 日结果TARGET_MET，触发RunEnd_Win(局胜利)，触发元进度结算

3. **GIVEN** DayNumber=5(任意天), maxRounds=4轮全部结算完毕, **WHEN** DailyRevenue < DailyTarget(如280<390), **THEN** 日结果GAME_OVER，立即触发RunEnd_Lose(局失败)，不进入商店

4. **GIVEN** DayNumber分别为1,3,4,6,21, **WHEN** 计算CycleIndex=ceil(DayNumber/3), **THEN** CycleIndex分别为1,1,2,2,7

### 日流程和提前结束

5. **GIVEN** DayNumber=6(第2循环Boss日), **WHEN** 执行DayInit, **THEN** DailyRevenue重置为0、从TbStage(Day=6)加载食客池、日类型判定为Boss日

6. **GIVEN** DayNumber=2, DailyTarget=210, MaxRounds=4, **WHEN** 第1轮结算后DailyRevenue=250>=210, **THEN** TARGET_MET，跳过第2/3/4轮，进入ShopPhase，UnusedRounds=3转奖券(FT4)

7. **GIVEN** DayNumber=3, DailyTarget=270, **WHEN** 第3轮结算后DailyRevenue恰好=270, **THEN** TARGET_MET(>=判定)，进入ShopPhase

8. **GIVEN** DayNumber=7, DailyTarget=510, MaxRounds=4, **WHEN** 4轮全部结算后DailyRevenue=430<510, **THEN** GAME_OVER，触发局失败，不进入商店

### 轮次流程

9. **GIVEN** 日初始化完成，当前第2轮(roundIndex=2)，4个空座位, **WHEN** 执行RoundInit, **THEN** 调用ResetPlayQuota(2)重置配额、调用GenerateAndAssignDiners()分配食客(落座产生基础餐费、展示初始情绪)

10. **GIVEN** PlayerTurn阶段，配额剩余2，手牌3张，2个Occupied座位, **WHEN** 玩家点击"结束轮次", **THEN** 进入RoundSettle，剩余配额不保留

11. **GIVEN** PlayerTurn阶段，BasePlaysPerRound=5，已打出5张(配额=0)，手牌2张, **WHEN** 检查配额, **THEN** 无法出牌但仍可查看信息，必须手动结束轮次

12. **GIVEN** PlayerTurn阶段，加菜过程中所有食客被吞噬/霸王餐，全部座位Empty, **WHEN** 检测到全部Empty, **THEN** 立即进入RoundSettle，结算队列为空，RoundRevenue=0

13. **GIVEN** PlayerTurn阶段，手牌=0，配额仍有剩余, **WHEN** 尝试出牌, **THEN** 前置检查失败，只能结束轮次，本轮仅获基础餐费

### 结算逻辑

14. **GIVEN** RoundSettle阶段，4个Occupied座位(0/1/2/3), **WHEN** 执行结算, **THEN** 采集结构快照，按seatIndex升序串行判定：seat0→seat1→seat2→seat3

15. **GIVEN** 4个食客结算：A付费(TotalCost=36)、B吞噬(Gold=140)、C霸王餐(0)、D付费(TotalCost=55), **WHEN** 计算RoundRevenue(FT1), **THEN** 36+140+0+55=231

16. **GIVEN** RoundSettle，seat0食客A有T-D2被吞噬, **WHEN** T-D2触发连坐从尚未结算的{B(seat1),C(seat2),D(seat3)}中选中C, **THEN** C标记为"连坐吞噬"跳过正常判定，B和D正常结算

17. **GIVEN** 串行结算中seat0食客A已付费(收益36已入账), seat1食客B霸王餐, **WHEN** B的霸王餐生效, **THEN** B收益=0，A的36不受影响(已结算不可撤回)

### Boss日

18. **GIVEN** DayNumber=3(第1循环Boss日)，第4轮(最后一轮), **WHEN** RoundInit执行Boss分配, **THEN** 从TbBossTemplate[CycleIndex=1]加载Boss。有空座位→分配到空座位；无空座位→替换DinerBaseGold最低的食客(被替换者不触发事件)

19. **GIVEN** DayNumber=6(第2循环Boss日)，当前第2轮(非最后一轮), **WHEN** RoundInit执行, **THEN** 只执行普通食客分配，不触发Boss分配

20. **GIVEN** Boss食客在场, **WHEN**(场景1)Boss被吞噬, **THEN**卡牌系统正常产卡(高Flavor→高面值卡)；**WHEN**(场景2)Boss触发霸王餐, **THEN**Boss支付0离场不产卡，霸王餐优先于吞噬特性

### 商店阶段

21. **GIVEN** DayNumber=4，DailyTarget达成(TARGET_MET), **WHEN** DaySummary完成, **THEN** 进入ShopPhase，提供：座位替换、全局设施(3槽位)、卡牌出售、遗物购买(10-20个TbRelic配置)

22. **GIVEN** DayNumber=8，DailyRevenue<DailyTarget(GAME_OVER), **WHEN** DaySummary判定失败, **THEN** 直接RunEnd_Lose，不进入ShopPhase

23. **GIVEN** 进入ShopPhase时奖券=0, **WHEN** 商店开放, **THEN** 商店正常显示但无法购买，可点击"完成购物"跳过

24. **GIVEN** BaseShopDisplay=5，陈列加成=3，TotalPoolSize=15, **WHEN** 计算ShopDisplayCount(FT5), **THEN** min(5+3,15)=8。若TotalPoolSize=6则min(8,6)=6

### 边缘情况

25. **GIVEN** DayNumber=1, DailyTarget=150, 当日所有食客霸王餐或食客池为空, **WHEN** 4轮结束DailyRevenue=0<150, **THEN** GAME_OVER，局失败

26. **GIVEN** TbDinerPool配置为空, **WHEN** RoundInit调用GenerateAndAssignDiners()无食客可分配, **THEN** 全部座位Empty，轮次立即结束，4轮后DailyRevenue=0日失败

27. **GIVEN** 最后一轮加菜中所有食客被吞噬(全部Empty)且DailyRevenue>=DailyTarget, **WHEN** 同时满足轮次级全部空位和日级提前结束, **THEN** 日级提前结束优先，进入DaySummary→ShopPhase

28. **GIVEN** 串行结算中seat2食客C(有T-D2)被吞噬，seat0/1已结算，仅seat3未结算, **WHEN** T-D2随机选中已结算的A/B, **THEN** 连坐对A/B无效，从{D}重选。若无未结算食客则连坐不触发

## Open Questions

1. **经济系统对商店阶段的完整定义**：商店阶段的购买流程、定价机制、库存刷新规则由经济系统 GDD 定义。回合流程系统只负责触发 ShopPhase。——**Owner**: 经济系统 GDD
2. **Boss 食客的具体模板和特性设计**：TbBossTemplate 的 Boss 属性范围、专属特性（如"法术护盾"）的详细设计由食客系统/事件系统 GDD 负责。——**Owner**: 事件系统 GDD
3. **元进度系统对局结构的影响**：局外成长可能调整局长（21天→可变）、初始牌组、座位数等。当前 MVP 使用固定配置。——**Owner**: 元进度系统 GDD
4. **变异日期机制的详细设计**：用户提出"后续可能加入随机变异的日期，变异日期出现全局buff"。此功能推迟到事件系统 GDD 中设计，回合流程系统预留事件钩子。——**Owner**: 事件系统 GDD
5. **教程流程**：第1天是否需要简化教程（如引导出牌、引导结算），还是直接以标准流程运行？——**Owner**: UX 设计阶段
6. **局胜利结算的详细内容**：21天完成后触发什么？元进度货币发放、成就统计、Run总结面板？——**Owner**: 元进度系统 GDD
