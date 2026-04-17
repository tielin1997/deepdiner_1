# 经济系统 (Economy System)

> **Status**: Designed (pending review)
> **Author**: zhangtielin + agents
> **Last Updated**: 2026-04-17
> **Implements Pillar**: 动态资源管理

## Overview

经济系统是《深渊食堂》的**局内资源循环引擎**。从数据角度，它管理奖券（Tickets）的完整生命周期——所有收入来源（日结算奖券、提前结束奖励、利息、出售卡牌、Boss 击杀奖励）和所有消耗途径（座位替换、全局设施购买、遗物购买、分类刷新），以及商店的库存陈列、定价和刷新机制。经济系统通过 Luban 配置表驱动所有定价和产出参数，确保数值可热调。

从玩家角度，经济系统是每天的**压力调节站和策略赌场**。每轮结束后，玩家带着有限的奖券走进商店——升级座位、购买设施、精简牌库、押注遗物——每个选择都是在为明天的压力容器加码或减压。奖券不够用意味着放弃关键升级，奖券太多意味着今天的风险冒得不够。经济系统让"再来一轮"的冲动不仅来自战斗的快感，更来自"如果我买了那个设施，明天的策略就会完全不同"的**投资期待**。

核心职责：

1. **奖券收支管理**：维护奖券余额，处理所有收入入账和消费扣款
2. **商店系统**：管理商品库存（座位/设施/遗物）、定价、陈列刷新
3. **卡牌出售**：处理玩家出售手牌换奖券的交易
4. **定价系统**：Luban 配置驱动的基础价格 + 餐厅修饰符折扣
5. **利息机制**：基于奖券余额或日结算的额外收入

> **技术参考**：本系统的 ADR（架构决策记录）将在 `/create-architecture` 阶段创建，涵盖商店数据结构、交易事务性、Luban 配置表结构等技术决策。

## Player Fantasy

**核心幻想：** "我是深渊赌场的庄家——每天我用食客的骨头和血肉赚取奖券，然后在商店里把所有筹码推上桌。升级座位是买新赌桌，买遗物是押注赔率，卖掉卡牌是当掉旧武器换新弹药。奖券永远不够——想买的东西永远比手里的钱多三倍。每次选择都是在回答同一个问题：今天的投资，明天能不能连本带利收回来？"

**情感锚点：** **赌注压下的快感 × 资金链断裂的恐惧**。赌场里最刺激的不是赢钱，是下注的瞬间——筹码离开手指的那一刻，一切已成定局。经济系统让每次商店购物都重现这个瞬间：奖券余额从 280 跳到 30，手里多了一个遗物和一个新座位，但明天的日目标又涨了 60。每次下注都是对明天的赌——赌这把椅子能多赚回它的成本，赌这个遗物能触发关键的连锁，赌今天"割肉"卖掉的那张卡不会在明天成为救命稻草。

**关键玩家时刻：**

1. **商店开门的瞬间**：日目标达成，商店亮起霓虹招牌。奖券 280——镀膜要 200，新座位要 150，那个稀有遗物要 120。三样都想要，只够买一样。手指在三个选项之间悬停——这 280 奖券能让我明天多赚 100 还是少亏 100？这不是购物，是**下注**。

2. **卖卡当铺的割肉感**：奖券不够，手指移到那张面值 4 的哥布林肉排上——出售价 15 奖券。卖掉它牌库薄了一层，但多了 15 奖券可以凑够买那个吞噬概率+5%的遗物。每张被出售的卡都是被当掉的旧武器——带着一丝不舍，换取明天的筹码。

3. **空钱包的赌场退场**：购物结束，奖券归零。看着明天日目标 420 的数字，口袋空空——没有安全网了。像赌场里筹码全部推上桌——赢了会所，输了下海。但这一次，庄家是自己，赌桌也是自己亲手搭建的。

**a-ha moment：** 玩家在第 8 天进入商店，手里只有 90 奖券。想买新座位（120）买不起，想买遗物（80）勉强够但明天会更紧。突然发现可以卖掉手里两张面值 3 的垃圾卡换 40 奖券——够买座位了，但牌库变薄了。玩家意识到：**奖券不是货币，是另一种赌注——我在用牌库的厚度换餐厅的硬件，用今天的弹药换明天的陷阱。这游戏里的每一枚硬币都是一枚子弹，而我在决定朝哪个方向开枪。**

## Detailed Design

### Core Rules

**规则 1：奖券作为独立货币**

1. 奖券（Tickets）是局内唯一商店货币，与营业额（金币）分离
2. 营业额（DailyRevenue）用于日目标判定（回合流程系统 FT3）
3. 营业额按比例转换为奖券后进入商店经济循环
4. 奖券在局内跨日累积，不设上限
5. 每局开始奖券余额归零

**规则 2：日结算奖券发放**

每个日的营业结束后，执行奖券发放：

```
日结算奖券 = BaseDayTickets + floor(DailyRevenue × RevenueToTicketRatio) + EarlyBonusTickets + BossKillBonus
```

1. **基础日奖券**（BaseDayTickets）：每日固定发放，由 TbShopConfig 配置
2. **营业额兑换**：`floor(DailyRevenue × RevenueToTicketRatio)`，RevenueToTicketRatio 由 TbShopConfig 配置
3. **提前结束奖励**（EarlyBonusTickets）：回合流程系统 FT4 已定义
4. **Boss 击杀奖励**（BossKillBonus）：若当日有 Boss 食客被吞噬，按 TbBossTemplate 配置的固定金额发放（每个 Boss 一次）
5. 发放时序：日结算判定（TARGET_MET）后、商店阶段开放前

**规则 3：利息机制（上限封顶）**

1. 利息在每个**日结算后**计算，基于商店阶段**开始前**的奖券余额
2. `Interest = floor(min(TicketBalance, InterestCap) × InterestRate)`
3. InterestCap：计息基数上限，由 TbShopConfig 配置
4. InterestRate：利率，由 TbShopConfig 配置
5. 利息与日结算奖券同时入账，在商店阶段开放前完成
6. **设计意图**：鼓励适度储蓄，但上限封顶防止囤积策略

**规则 4：商店系统**

商店在日目标达成后开放，提供以下分类商品：

| 商品分类 | 商品来源 | 定价 | 说明 |
|----------|---------|------|------|
| 座位 | TbSeatType（排除当前已有类型） | shop_price × 折扣 | 购买替换现有座位或扩展新座位 |
| 全局设施 | TbFacility | shop_price × 折扣 | 购买放入 3 槽位（满则替换） |
| 遗物 | TbRelic（随机子集） | shop_price × 折扣 | 包含卡牌增益类遗物（如"镀膜"效果） |
| 卡牌出售 | 玩家手牌 | TbCardSellPrice | 玩家主动出售，非系统陈列 |

**陈列规则**：

1. 商店总陈列数量由 FT5 决定：`min(BaseShopDisplay + Σ(陈列加成), TotalPoolSize)`
2. 陈列按分类分配（各分类陈列数由 TbShopConfig 配置比例）
3. 座位陈列：排除当前已安装的同类型，展示可购买的新座位类型
4. 设施陈列：从 TbFacility 随机抽取（排除当前已安装的）
5. 遗物陈列：从 TbRelic 随机抽取子集，保证至少 1 个 Uncommon+ 稀有度遗物（稀有度保底）
6. 卡牌出售不是"陈列商品"，而是独立的出售面板，始终可用

**规则 5：定价机制**

1. 所有商品基础价格由 Luban 配置（TbSeatType.shop_price, TbFacility.shop_price, TbRelic.shop_price）
2. 商店折扣由餐厅全局修饰符"商店打折"提供
3. 最终价格 = `floor(BasePrice × ShopDiscountMod)`
4. ShopDiscountMod 来自餐厅系统 `GetGlobalModifier("ShopDiscount")`，默认 1.0（无折扣）
5. 价格向下取整，最低 1 奖券

**规则 6：分类刷新**

1. 每个商品分类（座位/设施/遗物）有独立的刷新按钮
2. 刷新仅替换该分类的陈列商品，不影响其他分类
3. 刷新费用：`RefreshCost = BaseRefreshCost + PreviousRefreshes × RefreshCostIncrement`
4. PreviousRefreshes 为**本日该分类已刷新次数**（每日重置）
5. BaseRefreshCost 和 RefreshCostIncrement 由 TbShopConfig 配置
6. 刷新次数无硬上限（递增费用为自然限制）
7. 刷新后的新陈列从对应商品池重新随机抽取

**规则 7：购买流程**

1. 玩家选择商品 → 检查奖券余额 ≥ 最终价格
2. 余额不足 → 购买被拒绝，UI 提示"奖券不足"
3. 余额充足 → 弹出确认弹窗（高价值商品 > 100 奖券强制确认）
4. 确认后 → 扣除奖券 → 触发对应系统变更：
   - 座位：调用餐厅 `TryReplaceSeat(seatIndex, newType)` 或 `TryExpandSeat(newType)`
   - 设施：调用餐厅 `TryAddFacility(facilityId)`（满槽需选择替换目标）
   - 遗物：添加到玩家遗物列表，激活遗物效果
5. **所有交易不可撤回**——确认即生效

**规则 8：出售卡牌**

1. 仅在商店阶段可出售
2. 玩家从手牌中选择卡牌 → 系统显示出售价格（TbCardSellPrice）
3. 确认后 → 卡牌从手牌移除 → 奖券余额增加
4. 出售不触发卡牌系统事件（不算 Exhaust，遗物系统不触发）
5. 出售不可撤回

**规则 9：商店阶段结束**

1. 玩家主动点击"完成购物"结束商店阶段
2. 经济系统调用 `CloseShop()` 通知回合流程系统进入下一天
3. 座位替换和新增设施在**次日生效**
4. 遗物效果在购买后**立即生效**
5. 日结算时序：奖券发放 → 利息计算 → 商店开放 → 商店关闭 → 次日开始

**规则 10：遗物在经济系统中的角色**

1. 遗物作为商店商品之一，由 TbRelic 配置价格和效果
2. 遗物的具体效果设计由遗物系统 GDD 负责
3. 经济系统仅负责遗物的**购买流程和价格**
4. 遗物购买后立即添加到玩家遗物列表，效果即时激活
5. MVP 阶段包含 10-20 个遗物，每日商店随机陈列 subset
6. 遗物**不可出售、不可替换、不可撤回**——买定离手

### States and Transitions

**商店阶段状态机**：

| 状态 | 描述 | 可转换至 | 触发条件 |
|------|------|---------|---------|
| 关闭 (Closed) | 非商店阶段 | 开放 | 日目标达成（TARGET_MET） |
| 开放 (Open) | 商店可操作 | 购买中 / 出售中 / 刷新中 | 玩家交互 |
| 购买中 (Purchasing) | 正在执行购买 | 开放 | 购买完成 |
| 出售中 (Selling) | 正在出售卡牌 | 开放 | 出售完成 |
| 刷新中 (Refreshing) | 正在刷新陈列 | 开放 | 刷新完成 |
| 关闭 (Closed) | 商店关闭 | — | 玩家点击"完成购物" |

**奖券状态**：

| 状态 | 描述 |
|------|------|
| 余额 (Balance) | 当前可用奖券数 |
| 收入入队 (PendingIncome) | 待入账的奖券（日结算、利息等） |

### Interactions with Other Systems

| 交互方向 | 对方系统 | 数据流向 | 接口 |
|----------|---------|---------|------|
| 经济 ← | 回合流程 | 触发奖券发放 | `AwardTickets(amount, source)` — 回合流程在日结算后调用 |
| 经济 ← | 回合流程 | 触发商店开放 | `OpenShop(dayNumber, modifiers)` — 回合流程传入天数和修饰符 |
| 经济 → | 回合流程 | 商店关闭通知 | `CloseShop()` — 玩家完成购物后通知回合流程 |
| 经济 → | 餐厅系统 | 购买/替换座位 | `TryReplaceSeat(seatIndex, newType)`, `TryExpandSeat(newType)` |
| 经济 → | 餐厅系统 | 购买/替换设施 | `TryAddFacility(facilityId)` |
| 经济 ← | 餐厅系统 | 查询商店折扣 | `GetGlobalModifier("ShopDiscount")` |
| 经济 ← | 餐厅系统 | 查询陈列加成 | `GetGlobalModifier("ShopDisplayBonus")` |
| 经济 ← | 卡牌系统 | 出售卡牌 | `SellCard(cardId) → ticketAmount` |
| 经济 → | 遗物系统 | 购买遗物 | `AddRelic(relicId)` — 添加到玩家遗物列表 |
| 经济 → | UI 系统 | 奖券余额变化通知 | `OnTicketsChanged(newBalance, delta, source)` |
| 经济 → | UI 系统 | 商店库存数据 | `GetShopInventory() → ShopData` |

## Formulas

> **设计原则**：经济系统通过 Luban 配置表驱动所有参数。以下公式定义计算结构，具体数值由 TbShopConfig 等配置表提供。

### FE1：日结算奖券发放

`DayTickets = BaseDayTickets + floor(DailyRevenue × RevenueToTicketRatio) + EarlyBonusTickets + BossKillBonus`

**变量：**

| 变量 | 类型 | 范围 | 描述 |
|------|------|------|------|
| BaseDayTickets | int | [0, 100] | 每日固定发放（TbShopConfig.base_day_tickets） |
| DailyRevenue | int | ≥ 0 | FT2 输出（回合流程系统） |
| RevenueToTicketRatio | float | (0, 1.0] | 营业额到奖券的兑换比例（TbShopConfig.revenue_to_ticket_ratio） |
| EarlyBonusTickets | int | ≥ 0 | FT4 输出（回合流程系统） |
| BossKillBonus | int | ≥ 0 | Boss 击杀固定奖励（TbBossTemplate.kill_bonus），当日每个被吞噬的 Boss 各计一次 |
| DayTickets | int | ≥ 0 | 当日总发放奖券数 |

**输出范围：** ≥ 0
**示例：** Day 5, BaseDayTickets=20, DailyRevenue=390, Ratio=0.1, EarlyBonus=0, 无Boss → 20 + floor(390×0.1) + 0 + 0 = 59

### FE2：利息计算

`Interest = floor(min(TicketBalance, InterestCap) × InterestRate)`

**变量：**

| 变量 | 类型 | 范围 | 描述 |
|------|------|------|------|
| TicketBalance | int | ≥ 0 | 当前奖券余额（商店开放前） |
| InterestCap | int | > 0 | 计息基数上限（TbShopConfig.interest_cap） |
| InterestRate | float | (0, 1.0) | 利率（TbShopConfig.interest_rate） |
| Interest | int | ≥ 0 | 利息收入 |

**输出范围：** [0, floor(InterestCap × InterestRate)]
**示例：** 余额 250, Cap=200, Rate=0.15 → floor(min(250,200) × 0.15) = floor(30) = 30

### FE3：商品最终价格

`FinalPrice = max(1, floor(BasePrice × ShopDiscountMod))`

**变量：**

| 变量 | 类型 | 范围 | 描述 |
|------|------|------|------|
| BasePrice | int | > 0 | 商品基础价格（TbSeatType/TbFacility/TbRelic 的 shop_price） |
| ShopDiscountMod | float | ≥ 0.1 | 商店折扣修饰符（餐厅系统 GetGlobalModifier("ShopDiscount")，默认 1.0） |
| FinalPrice | int | ≥ 1 | 最终售价 |

**输出范围：** ≥ 1（最低 1 奖券）
**示例：** BasePrice=150, 折扣 0.8 → floor(150 × 0.8) = 120

### FE4：分类刷新费用

`RefreshCost = BaseRefreshCost + PreviousRefreshes × RefreshCostIncrement`

**变量：**

| 变量 | 类型 | 范围 | 描述 |
|------|------|------|------|
| BaseRefreshCost | int | > 0 | 首次刷新价格（TbShopConfig.base_refresh_cost） |
| PreviousRefreshes | int | [0, 99] | 本日该分类已刷新次数（每日重置） |
| RefreshCostIncrement | int | > 0 | 每次刷新的增量（TbShopConfig.refresh_cost_increment） |
| RefreshCost | int | > 0 | 本次刷新费用 |

**输出范围：** > 0（递增无上限）
**示例：** Base=15, Increment=10, 已刷新 2 次 → 15 + 2 × 10 = 35

### FE5：日结算时序与数据流

```text
回合流程系统完成日结算判定 (FT3 → TARGET_MET)
  ↓
经济系统执行：
  1. 计算 FE1（日结算奖券）→ 入账
  2. 计算 FE2（利息）→ 入账
  3. 生成商店库存（FT5 + 分类配置）
  ↓
商店阶段开放 (OpenShop)
  → 玩家购买/出售/刷新
  → 所有交易不可撤回
  ↓
玩家点击"完成购物" (CloseShop)
  ↓
回合流程进入下一天
```

### 配置表建议

| 配置表 | 关键字段 | 说明 |
|--------|---------|------|
| TbShopConfig | base_day_tickets, revenue_to_ticket_ratio, interest_cap, interest_rate, base_refresh_cost, refresh_cost_increment, base_shop_display, ticket_per_unused_round | 商店全局配置 |
| TbSeatType | shop_price, modifiers[] | 座位类型和价格 |
| TbFacility | shop_price, modifiers[], tier | 设施类型和价格 |
| TbRelic | shop_price, rarity, effect | 遗物价格和效果 |
| TbCardSellPrice | face_value_range, race_id, price | 卡牌出售价格表 |
| TbBossTemplate | kill_bonus | Boss 击杀奖券奖励 |

## Edge Cases

### 零值/极值

- **If DailyRevenue=0 且无 Boss 且 BaseDayTickets=0（极端配置）**: DayTickets=0，利息=0，玩家以余额 0 进入商店。商店正常开放但无法购买，只能点击"完成购物"跳过。合法的最差状态。

- **If 刷新后商品池无变化（池仅剩 1 个可购买商品）**: 刷新重新随机抽取，可能抽到同一商品。陈列不变但刷新费用已扣。合法行为——池小则刷新价值低，递增费用自然抑制。UI 应提示"商品池已接近耗尽"。

- **If 折扣导致价格 floor 后为 0**: FE3 使用 max(1, ...) 兜底，价格最低 1 奖券。防止免费购买。

- **If 出售最后一张手牌**: 出售成功，手牌变空列表。次日轮次开始无卡可出，仅获基础餐费。经济系统不阻止此操作——"割肉"是策略选择。

### 时序与计算

- **If 利息计算的余额包含 FE1 日结算奖券（时序语义）**: FE5 时序为 FE1 入账 → FE2 利息计算 → 商店开放。利息基于 FE1 入账后的新余额计算。日结算奖券本身被计息。设计意图：利息基于"商店开放前的全部余额"，简单直觉。

- **If 多个 Boss 食客同日被吞噬（T-D2 连坐）**: BossKillBonus = Σ(每个被吞噬 Boss 的 kill_bonus)，每个 Boss 各计一次。奖励可叠加。

- **If 利息因 floor 归零（余额极低）**: 余额=3, Rate=0.1 → floor(0.3)=0。微薄余额不产生利息，floor 的自然结果。

### 遗物即时效果

- **If 购买打折遗物后当前商店剩余商品价格变化**: 遗物立即生效，剩余商品价格实时按新折扣重算。已购买的商品不追溯退款（不可撤回）。策略空间：先买打折遗物再买其他商品更划算，但需要先凑够遗物的钱。

- **If 购买陈列加成遗物后当前陈列不扩展**: 遗物立即生效但当日陈列已在开放时一次性生成，不中途增加新商品行。陈列加成只影响次日及以后的商店。

### 商品池耗尽

- **If 所有遗物已购买（遗物池为空）**: 遗物分类不陈列任何商品（0 行），刷新按钮禁用不收费。其他分类不受影响。

- **If 所有设施已安装且 TbFacility 总数 = 3**: 设施分类不陈列任何商品，刷新按钮禁用。若 TbFacility 总数 > 3，玩家仍可购买替换已有设施。

- **If 所有分类商品池同时耗尽**: 商店仅剩"卡牌出售"功能。陈列区域显示"所有商品已售罄"，玩家只能出售卡牌或点击"完成购物"。通过配置足够的 TbRelic(10-20)和 TbFacility 数量推迟此情况。

- **If 座位已达上限 8 且有未安装类型**: 商店仍陈列可购买的新座位类型，购买时进入"替换模式"（必须选择替换目标）。TryExpandSeat()返回 false，TryReplaceSeat()仍可用。

### 套利与边界

- **If 出售卡牌价格高于低价遗物购买价**: 合法的"以卡养券"策略。Tuning 时需确保卡牌出售价不系统性偏高导致 degenerate 策略。

- **If 奖券余额极端积累（Day 15 余额 2000+）**: InterestCap 封顶利息（最多 floor(Cap×Rate)/天），防止利滚利。余额本身不封顶，后期通过配置高价商品和足够大的遗物池吸收奖券。

## Dependencies

| 方向 | 系统 | 关系 | 数据接口 | 硬/软依赖 |
|------|------|------|---------|----------|
| 经济 ← | 回合流程系统 | **上游** | `AwardTickets(amount, source)` — 触发奖券发放；`OpenShop(dayNumber, modifiers)` — 触发商店开放 | **硬依赖** |
| 经济 → | 回合流程系统 | **下游** | `CloseShop()` — 商店关闭通知 | **硬依赖** |
| 经济 → | 餐厅系统 | **下游** | `TryReplaceSeat()`, `TryExpandSeat()`, `TryAddFacility()` — 购买座位和设施 | **硬依赖** |
| 经济 ← | 餐厅系统 | **上游** | `GetGlobalModifier("ShopDiscount")`, `GetGlobalModifier("ShopDisplayBonus")` — 折扣和陈列加成 | **硬依赖** |
| 经济 ← | 卡牌系统 | **上游** | `SellCard(cardId) → ticketAmount` — 出售卡牌换奖券 | **硬依赖** |
| 经济 → | 遗物系统 | **下游** | `AddRelic(relicId)` — 购买遗物后添加到玩家遗物列表 | **硬依赖** |
| 经济 → | UI 系统 | **下游** | `OnTicketsChanged(newBalance, delta, source)` — 奖券变化通知；`GetShopInventory() → ShopData` — 商店数据 | **硬依赖** |
| 经济 ← | 食客系统 | **上游** | Boss 击杀事件触发 BossKillBonus（通过回合流程结算传递） | **软依赖** |
| 经济 ← | 事件系统 | **上游** | 事件可能临时修改商品池或价格（预留，MVP 空实现） | **软依赖** |

**双向依赖验证**：

- 回合流程系统 GDD 列出"经济系统"为下游依赖（`OpenShop`, `AwardTickets`）✅
- 餐厅系统 GDD 列出"经济系统"为下游依赖（商店购买接口）✅
- 卡牌系统 GDD 列出"经济系统"为上游依赖（`SellCard`）✅
- 遗物系统 GDD 未设计（需列出"经济系统"为上游依赖——遗物购买触发）
- 事件系统 GDD 未设计（需列出"经济系统"为可修改商品池的系统）

## Tuning Knobs

| 旋钮 | 配置表 | 字段 | 安全范围 | 调高效果 | 调低效果 |
|------|--------|------|---------|---------|---------|
| 每日基础奖券 | TbShopConfig | base_day_tickets | [0, 100] | 每天保底收入更高，前期更宽裕 | 前期更紧，依赖营业额兑换 |
| 营业额兑换比例 | TbShopConfig | revenue_to_ticket_ratio | (0, 1.0] | 高营业额日奖券暴涨 | 营业额几乎不转换，经济紧缩 |
| 利息计息上限 | TbShopConfig | interest_cap | [0, 9999] | 储蓄回报更高，鼓励攒钱 | 利息几乎无感 |
| 利息利率 | TbShopConfig | interest_rate | (0, 1.0) | 储蓄回报更丰厚 | 利息微薄 |
| 首次刷新费用 | TbShopConfig | base_refresh_cost | [1, 100] | 第一次刷新就很贵 | 第一次刷新几乎免费 |
| 刷新费用增量 | TbShopConfig | refresh_cost_increment | [0, 100] | 后续刷新急剧变贵 | 刷新成本增长平缓 |
| 座位售价 | TbSeatType | shop_price | > 0 | 座位升级更昂贵 | 座位更容易获取 |
| 设施售价 | TbFacility | shop_price | > 0 | 设施更昂贵 | 设施更容易获取 |
| 遗物售价 | TbRelic | shop_price | > 0 | 遗物更昂贵，选择更谨慎 | 遗物更容易入手 |
| 卡牌出售价格 | TbCardSellPrice | price | ≥ 0 | 出售卡牌更划算，鼓励精简牌库 | 出售不划算，倾向保留 |
| Boss 击杀奖券奖励 | TbBossTemplate | kill_bonus | ≥ 0 | Boss 日收入暴涨 | Boss 额外收入减少 |
| 商店折扣修饰 | 餐厅系统设施/遗物 | ShopDiscountMod | [0.1, 2.0] | 更便宜（<1.0 时）或更贵（>1.0 时） | 反向影响 |

> **注**：商店折扣修饰由餐厅系统全局修饰符体系管理，经济系统通过 `GetGlobalModifier("ShopDiscount")` 读取。陈列数量相关旋钮由回合流程系统 FT5 定义。

## Visual/Audio Requirements

经济系统的视觉/音频需求将在 UX 设计阶段定义。商店 UI 布局、奖券余额显示、购买反馈动画等由 UX 规格书指定。

## UI Requirements

经济系统的 UI 需求将在 `/ux-design` 阶段定义，不在本 GDD 范围内。

## Acceptance Criteria

### 奖券收支与利息

1. **GIVEN** Day 5, BaseDayTickets=20, DailyRevenue=390, Ratio=0.1, EarlyBonus=0, 无Boss, **WHEN** 计算FE1, **THEN** DayTickets=20+floor(390×0.1)+0+0=59

2. **GIVEN** Boss A(kill_bonus=50)和Boss B(kill_bonus=80)同日被吞噬, **WHEN** 计算FE1, **THEN** BossKillBonus=50+80=130

3. **GIVEN** 余额250, InterestCap=200, InterestRate=0.15, **WHEN** 计算FE2, **THEN** Interest=floor(min(250,200)×0.15)=30

4. **GIVEN** 余额3, InterestCap=200, InterestRate=0.1, **WHEN** 计算FE2, **THEN** Interest=floor(min(3,200)×0.1)=floor(0.3)=0

5. **GIVEN** 日结算前余额=50, BaseDayTickets=20, DailyRevenue=390, Ratio=0.1, InterestCap=200, InterestRate=0.15, **WHEN** 执行FE5时序, **THEN** FE1入账59(余额→109), FE2利息=floor(min(109,200)×0.15)=16, 最终余额=50+59+16=125

6. **GIVEN** Day 3结束余额=180, **WHEN** 进入Day 4商店, **THEN** 余额仍为180（跨日累积）

7. **GIVEN** 上一局余额=500, **WHEN** 开始新一局(RunInit), **THEN** 奖券余额=0

8. **GIVEN** BaseDayTickets=0, DailyRevenue=0, 无Boss无EarlyBonus, **WHEN** 计算FE1, **THEN** DayTickets=0，商店正常开放但无法购买

### 定价与购买

9. **GIVEN** BasePrice=150, ShopDiscountMod=0.8, **WHEN** 计算FE3, **THEN** FinalPrice=max(1,floor(150×0.8))=120

10. **GIVEN** BasePrice=5, ShopDiscountMod=0.1, **WHEN** 计算FE3, **THEN** FinalPrice=max(1,floor(5×0.1))=1

11. **GIVEN** 余额100, 商品FinalPrice=120, **WHEN** 尝试购买, **THEN** 拒绝并提示奖券不足，余额不变

12. **GIVEN** 余额150, 遗物FinalPrice=120, **WHEN** 确认购买, **THEN** 余额→30, 调用AddRelic(relicId), 遗物效果立即生效

13. **GIVEN** 余额200, 座位FinalPrice=150, **WHEN** 确认购买替换seat[0], **THEN** 余额→50, 调用TryReplaceSeat(0, newType), 次日生效

14. **GIVEN** 余额200, 设施FinalPrice=100, **WHEN** 确认购买, **THEN** 余额→100, 调用TryAddFacility(facilityId), 次日生效

15. **GIVEN** 商品价格=120(>100), 余额=150, **WHEN** 点击购买, **THEN** 弹出确认弹窗，确认后扣款

16. **GIVEN** 已购买遗物R1(扣奖券80), **WHEN** 尝试撤回, **THEN** 拒绝，奖券不退还

### 刷新

17. **GIVEN** BaseRefreshCost=15, Increment=10, 遗物分类已刷新0次, **WHEN** 刷新遗物, **THEN** 费用=15+0×10=15, 新陈列从遗物池重新抽取

18. **GIVEN** BaseRefreshCost=15, Increment=10, 遗物分类已刷新2次, **WHEN** 刷新遗物, **THEN** 费用=15+2×10=35

19. **GIVEN** 座位分类已刷新3次, 设施分类已刷新0次, **WHEN** 刷新设施, **THEN** 费用按设施分类独立计数=15（分类计数互不影响）

20. **GIVEN** 余额=10, 刷新费用=15, **WHEN** 尝试刷新, **THEN** 拒绝，余额不变，陈列不变

### 陈列规则

21. **GIVEN** 餐厅已安装S2铁板桌, **WHEN** 生成座位陈列, **THEN** S2不出现在可购买列表中

22. **GIVEN** 已安装设施F1和F2, **WHEN** 生成设施陈列, **THEN** F1和F2不出现在可购买列表中

23. **GIVEN** 遗物池有5个Common和3个Uncommon, ShopDisplayCount=3, **WHEN** 生成遗物陈列, **THEN** 陈列至少包含1个Uncommon+稀有度遗物

### 遗物即时效果

24. **GIVEN** 遗物A效果=ShopDiscountMod 0.8, 商品B BasePrice=150, **WHEN** 购买遗物A(立即生效), **THEN** 商品B价格实时变为max(1,floor(150×0.8))=120

25. **GIVEN** 已以原价购买商品C, 随后购买打折遗物, **WHEN** 检查已购商品, **THEN** 不追溯退款，已扣奖券不变

26. **GIVEN** 商店已开放(陈列已生成3个遗物), **WHEN** 购买陈列加成遗物(+2陈列), **THEN** 当日遗物陈列不增加(仍为3个)，次日生效

### 出售卡牌

27. **GIVEN** 商店阶段, 卡牌FaceValue=200, TbCardSellPrice配置售价=50, **WHEN** 出售, **THEN** 卡牌从手牌移除，奖券+50，不触发OnCardExhausted事件

28. **GIVEN** 商店阶段, 手牌仅剩1张卡, **WHEN** 出售该卡, **THEN** 手牌变为空列表(0张), 奖券增加

29. **GIVEN** 轮次进行中(PlayerTurn阶段), **WHEN** 尝试出售卡牌, **THEN** 拒绝

### 商品池耗尽

30. **GIVEN** 所有遗物已购买, **WHEN** 商店开放, **THEN** 遗物分类0陈列，刷新按钮禁用不收费

31. **GIVEN** 座位=8/8(上限), 有未安装类型S3, **WHEN** 购买S3, **THEN** TryExpandSeat()返回false, TryReplaceSeat(index, S3)成功

32. **GIVEN** 所有遗物已购买, 所有设施已安装, 所有座位类型已安装, **WHEN** 商店开放, **THEN** 仅显示卡牌出售功能, "完成购物"按钮可用

## Open Questions

1. **后期经济消耗口是否充足**：座位/设施买完后、遗物池耗尽后，奖券是否会出现通胀？需要通过 Luban 配置足够多的遗物和高价商品来验证。——**Owner**: 数值平衡阶段
2. **RevenueToTicketRatio 的最佳值**：0.1 是否合适？后期 DailyRevenue 增长是否会奖券过剩？需要跑模拟验证21天经济曲线。——**Owner**: 数值平衡阶段
3. **遗物系统的详细设计**：遗物效果、稀有度分层、数量（10-20个）由遗物系统 GDD 定义。经济系统仅负责购买流程。——**Owner**: 遗物系统 GDD
4. **利息对策略多样性的影响**：利息是否会导致"囤积策略"成为最优解？InterestCap 是否需要更严格？——**Owner**: 数值平衡阶段
5. **商店陈列的 UI 交互**：分类刷新按钮的位置、确认弹窗的设计、商品池耗尽的提示方式由 UX 设计阶段定义。——**Owner**: UX 设计阶段
6. **事件系统对经济的影响**：事件可能临时修改商品池或价格（如"打折日"、"限量遗物"），具体由事件系统 GDD 定义。——**Owner**: 事件系统 GDD
