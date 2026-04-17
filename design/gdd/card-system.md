# 卡牌系统 (Card System)

> **Status**: Designed (pending review)
> **Author**: zhangtielin + agents
> **Last Updated**: 2026-04-17
> **Implements Pillar**: 高易用性, 动态资源管理, 涌现式策略

## Overview

卡牌系统是《深渊食堂》的**核心策略工具箱和动态资源池**。从数据角度，它管理食材卡的完整生命周期——卡牌属性（面值、耐久度、种族来源）全部由 Luban 配置表定义，卡牌从生成（吞噬食客时根据种族 × 美味度 × 新鲜度产生）到打出（作为加菜附加到食客消费上）再到消耗（耐久度归零时从牌库中移除），形成一个"狩猎→产出→使用→消耗"的闭环。卡牌系统同时维护玩家的手牌状态，提供出牌、回收等接口供回合流程系统调用。

从玩家角度，食材卡是猎人手中有限的箭矢——每一张都有明确的攻击力（面值）和有限的寿命（耐久度）。玩家打出一张卡就是向食客射出一箭，增加它的消费负担，逼它走向吞噬的临界点。但箭矢会磨损：每次出牌消耗一点耐久度，归零即毁。玩家在每一轮面临的核心抉择是：**对谁出牌、出多少牌、何时收手**——过早则猎物逃脱（食客付钱走人），过猛则浪费宝贵的卡牌资源。吞噬产出的新食材卡是箭矢的唯一补给来源，使"用箭狩猎→吞噬获得新箭"成为自洽的资源循环。

卡牌系统的核心职责包括：

1. **卡牌属性定义**：维护每张食材卡的面值（CardFaceValue）、耐久度（Durability）、种族来源（SourceRaceId）等数据
2. **卡牌生命周期管理**：管理卡牌从生成到消耗的完整状态流转（牌库→手牌→打出→消耗）
3. **出牌执行**：处理玩家打出食材卡的加菜请求，与餐厅系统修饰符链协作计算有效加菜餐费
4. **吞噬产出**：接收食客系统的吞噬事件，根据食客属性生成新的食材卡
5. **牌库管理**：维护手牌状态，提供出牌、出售、容量管理等机制

> **技术参考**：卡牌系统的 ADR（架构决策记录）将在 `/create-architecture` 阶段创建，涵盖牌库数据结构、卡牌状态机实现、Luban 配置表结构等技术决策。

## Player Fantasy

**核心幻想：** "我是深渊食材进化链的推手。手里的每张卡都是一块未经雕琢的原料——面值 3 的史莱姆凝胶、耐久度 2 的哥布林肉排。它们看起来平平无奇，但只要我用它们精准地逼一个美味度更高的食客入绞肉机，这堆平庸的原料就会蜕变成面值 12 的深渊触手须。从低级到高级，从粗糙到精致，我的牌库是我一手打造的**食材进化谱系**——而每一轮的掠夺都是这条进化链上的一个节点。"

**情感锚点：** **掠夺的快感 × 进化的期待**。玩家感受到的不是"我的箭不够用"的匮乏焦虑，而是"用这张废牌换一张神牌"的赌徒兴奋。出牌不是消耗——是**投资**。每次打出一张面值 3 的低级卡加菜，玩家心里算的不是"我又少了一张牌"，而是"这 3 点面值能不能帮我多吞噬一个食客，换回一张面值 8 的新卡"。耐久度也不是倒计时——是这张卡的**投资窗口**：耐久度 3 意味着我还有 3 次机会用它去"以小搏大"，直到它完成使命功成身退。

**关键玩家时刻：**

1. **起手审视（轮次开始看手牌）**：扫一眼手牌——3 张低面值卡，1 张中等面值。都是"进化前"的普通食材。但下一轮的食客池里可能有美味度更高的稀有种族。普通食材不是负担，是**狩猎的弹药**——用它们把肥美的猎物拖进绞肉机，换来更高级的食材。这种"手里虽烂但未来可期"的感觉就是进化的期待。

2. **以小搏大（用低级卡换取高级卡）**：用两张面值 4 的哥布林肉排精准推过一个美味度 5 的深海族食客——绞肉机一转，产出一张面值 10、耐久度 4 的深渊触手须。两张"垃圾"换一张"金卡"。不是每次都能成功——有时加菜不够食客付钱走人，有时加太多把食客的金币榨干但产出只是一张普通卡。但每次成功时的"升级"快感都让下一轮的赌注更有诱惑力。

3. **进化链推进（看到牌库整体质量上升）**：开局时牌库里全是面值 1-3 的史莱姆凝胶和蘑菇孢子。经过 3 天的精准吞噬，翻开手牌——面值 8 的哥布林肉排、面值 12 的深海触手须、面值 15 的幽灵精华。牌库的整体"品质"肉眼可见地提升了。从"地摊货"到"深渊珍馐"的蜕变过程就是玩家最满足的时刻。

4. **进化链中断（吞噬失败或产出不如预期）**：精心策划一轮狩猎，3 次加菜把食客推到临界点——结果霸王餐触发，食客白吃走人，没有产出新卡。或者吞噬成功但产出的卡面值只有 2——食客的美味度太低。进化链被打断的失落感让下一次成功更加甜蜜。

**a-ha moment：** 玩家在第 5 天翻开手牌，发现所有面值 1-3 的"垃圾卡"都已经被吞噬产出的面值 8-15 的卡牌替换了。牌库从一个"地摊食材铺"进化成了"深渊美食殿堂"。玩家意识到：**我一直在做的不是管理消耗品，而是经营一条食材进化生产线——用低级食材狩猎高级食材，再用高级食材狩猎更高级的食材。每一张被用掉的卡都是进化链的一环，不是损失。**

**与食客猎人幻想的关系：** 食客系统回答"我该狩猎谁"（读心、判断猎物价值），卡牌系统回答"我用什么去狩猎、猎到之后能得到什么"（武器升级和战利品品质）。猎人负责选目标，卡牌系统负责武器的**进化节奏**——从木弓到铁弓到深渊弓的升级路径不是商店买来的，是通过一次次精准吞噬亲手打造出来的。

## Detailed Design

### Core Rules

**规则 1：卡牌属性体系**

每张食材卡实例拥有以下核心属性：

| 属性 | 类型 | 来源 | 描述 |
|------|------|------|------|
| CardId | int | 系统生成 | 唯一标识 |
| CardFaceValue | int | 吞噬：由食客 Flavor 按指数曲线转换；起手：TbStartingCardConfig | 卡牌面值，作为餐厅系统 F2 公式的输入（CardFaceValue） |
| MaxDurability | int | 吞噬：食客 Freshness；起手：TbStartingCardConfig | 卡牌最大耐久度（= 使用次数） |
| CurrentDurability | int | 初始 = MaxDurability | 剩余使用次数 |
| SourceRaceId | int | 吞噬：食客 RaceId；起手：配置值 | 种族来源（影响部分修饰符，如"种族/食材加菜价格加成"） |
| CardName | string | Luban 配置 | 显示名称 |

**属性来源规则**：
- **吞噬产卡**：CardFaceValue = FlavorCurveBase × FlavorCurveExponent^Flavor（指数曲线，参数由 Luban 配置）；MaxDurability = 食客的 Freshness（直接映射为使用次数）；SourceRaceId = 食客的 RaceId
- **起手卡牌**：全部属性由 TbStartingCardConfig 配置提供
- **卡牌实例一旦创建，CardFaceValue 和 MaxDurability 不再改变**（后续遗物/食谱进化的动态修饰通过餐厅修饰符链在出牌时计算，不修改卡牌实例数据）

**面值指数曲线说明**：
- 设计意图曲线：FaceValue = 50 × 2^Flavor（美味度每+1，面值翻倍，强化"以小搏大"的进化感）
- 曲线参数（FlavorCurveBase、FlavorCurveExponent）由 Luban 全局常量配置，可调整缩放强度
- 具体数值可预计算存入 TbFlavorToFaceValue 查找表，运行时直接查表

**规则 2：起手牌组**

每局游戏开始时，玩家获得一组初始食材卡：

1. 从 TbStartingCardConfig 读取初始牌组配置
2. 初始牌组的内容**由元进度系统决定**（元进度系统 GDD 未设计，MVP 阶段使用固定默认配置）
3. MVP 默认配置由 Luban 表定义，包含 N 张低面值食材卡（具体种族、面值、耐久度由配置决定）
4. 元进度系统就绪后，初始牌组可包含更多种族和更高面值的卡牌

**规则 3：手牌容量限制**

1. 手牌数量有硬性上限（MaxHandSize，由 Luban 常量配置）
2. 吞噬产卡时，若当前手牌数 < MaxHandSize：新卡直接加入手牌
3. 吞噬产卡时，若当前手牌数 = MaxHandSize：触发**先弃后得**流程——
   a. 系统暂停吞噬结算
   b. 弹出 UI 让玩家从当前手牌中选择一张丢弃
   c. 丢弃的卡牌从游戏中永久移除（不获得任何收益）
   d. 选定后，新卡加入手牌，吞噬结算继续
4. 若 T-D5 幽灵残留触发额外产卡，每次产卡都独立检查手牌上限（可能出现连续两次"先弃后得"）
5. 商店阶段出售卡牌后，手牌自然腾出空间

**规则 4：每轮出牌上限**

1. 每轮次有固定的出牌次数上限（BasePlaysPerRound，由 Luban 配置）
2. 餐厅系统的全局修饰符可增减此上限
3. 最终上限 = BasePlaysPerRound + Σ(设施/遗物出牌上限加成)
4. **所有出牌都消耗出牌配额**，包括：
   - 正常加菜（食客正常承受）
   - T-A1 挑食体质抵御的加菜（虽然餐费为 0 且不消耗耐久度，但仍消耗出牌配额）
5. 当轮次已使用配额 = 最终上限时，玩家无法再出牌
6. 配额在每轮次开始时重置为满

**规则 5：卡牌出牌执行**

玩家打出一张食材卡 targeting 一个已落座食客时，执行以下步骤：

1. **前置检查**：
   a. 手牌中存在该卡牌 ✅
   b. 目标食客处于 Dining 状态 ✅
   c. 本轮剩余出牌配额 > 0 ✅
   d. 以上任一条件不满足，拒绝出牌

2. **消耗出牌配额**：本轮已使用配额 +1

3. **T-A1 挑食体质检查**：
   a. 查询目标食客是否有 T-A1 特性且剩余抵御次数 > 0
   b. 若命中：加菜餐费 = 0，**不消耗卡牌耐久度**，食客抵御次数 -1，结束出牌流程（跳至步骤 7a）

4. **计算有效加菜餐费**：
   a. 将 CardFaceValue 传递给餐厅系统
   b. 餐厅系统通过修饰符链计算 EffectiveAddOnFee（F2 公式）
   c. 返回 EffectiveAddOnFee 值

5. **消耗卡牌耐久度**：
   a. 每次出牌消耗 1 点耐久度（固定值，不受修饰符影响）
   b. CurrentDurability -= 1

6. **T-A2 打赏习惯处理**：
   a. 若食客有 T-A2 特性：额外小费 = EffectiveBaseFee × TipRatio，加入营业额

7. **更新食客消费**：
   a. 食客的 CumulativeSpent += EffectiveAddOnFee（T-A1 命中时 += 0）
   b. 更新食客 GoldSpentRatio，触发情绪变化

8. **触发概率判定**（由餐厅系统执行）：
   a. 吞噬触发概率判定（F4）
   b. 霸王餐触发概率判定（F5，优先于吞噬）

9. **耐久度归零检查**：
   a. 若 CurrentDurability ≤ 0：触发卡牌销毁流程（规则 7）

**规则 6：吞噬产卡**

当食客系统发出 `OnDinerDevoured(dinerId, raceId, flavor, freshness, traits[])` 事件时：

1. **生成基础卡牌**：
   a. CardFaceValue = TbFlavorToFaceValue[flavor]（查表，指数曲线预计算值）
   b. MaxDurability = freshness + SeatDurabilityBonus（食客新鲜度 + 座位耐久度加成，如保鲜柜 +1）
   c. CurrentDurability = MaxDurability
   d. SourceRaceId = raceId
   e. CardName = 从 TbCardTemplate 按种族查询

2. **手牌上限检查**（规则 3 的先弃后得流程）

3. **加入手牌**：新卡牌加入玩家手牌，当轮可用

4. **T-D5 幽灵残留处理**：
   a. 检查 traits 中是否包含 T-D5
   b. 若包含，按 ExtraCardChance 概率判定
   c. 命中时生成第二张同种族卡牌（同样的 CardFaceValue 和 MaxDurability）
   d. 第二张卡同样执行手牌上限检查

**规则 7：卡牌销毁与回收**

1. **正常销毁**：当 CurrentDurability ≤ 0 时，卡牌触发 Exhaust（销毁）：
   a. 卡牌从手牌中移除
   b. 触发 `OnCardExhausted(cardId, cardFaceValue, sourceRaceId)` 事件（供遗物系统使用）
   c. 卡牌实例销毁，本局不再可用

2. **回收判定**：在销毁之前，检查餐厅系统的"卡牌耐久归零后回收"全局修饰符：
   a. 若存在回收修饰符（RecoveryChance > 0），按概率判定
   b. 判定成功：CurrentDurability = 配置的恢复值（RecoveryAmount，通常为 1 或 MaxDurability），卡牌保留在手牌中，不触发销毁
   c. 判定失败：正常执行销毁流程

**规则 8：卡牌出售**

1. 仅在商店阶段可出售卡牌
2. 出售价格由 Luban TbCardSellPrice 表直接配置（按卡牌面值/种族查找对应售价）
3. 出售后卡牌从手牌移除，玩家获得对应奖券
4. 出售不触发任何事件（不算 Exhaust，遗物系统不触发）

**规则 9：卡牌出牌目标规则**

1. 玩家可将任意手牌打 targeting 任意处于 Dining 状态的食客
2. 同一食客可被多次加菜（受出牌配额限制，不受每食客单独上限）
3. 一张卡只能 targeting 一个食客，不能拆分面值
4. 出牌后不可撤回（MVP 简化设计）

### States and Transitions

**卡牌实例状态机**：

| 状态 | 描述 | 可转换至 | 触发条件 |
|------|------|---------|---------|
| 手牌中 (InHand) | 在玩家手牌中，可被出牌 | 销毁 | 出牌后耐久度归零且回收判定失败 |
| 手牌中 (InHand) | 在玩家手牌中 | 出售 | 商店阶段玩家选择出售 |
| 销毁 (Exhausted) | 从游戏中永久移除 | — | 终态 |

**状态流转**：
```
[InHand] →(出牌→耐久度归零→回收失败)→ [Exhausted]
[InHand] →(商店出售)→ [removed]
```

**手牌状态（全局）**：

| 状态 | 描述 | 触发 |
|------|------|------|
| 正常 (Normal) | 手牌数 < MaxHandSize | 默认状态 |
| 满手 (Full) | 手牌数 = MaxHandSize | 达到上限 |
| 溢出处理 (Overflow) | 等待玩家选择丢弃 | 吞噬产卡 + 手牌已满 |

### Interactions with Other Systems

| 交互方向 | 对方系统 | 数据流向 | 接口 |
|----------|---------|---------|------|
| 卡牌 → 餐厅 | 餐厅系统 | 提供 CardFaceValue | 卡牌系统输出 CardFaceValue，餐厅系统计算 EffectiveAddOnFee（F2） |
| 卡牌 ← 餐厅 | 餐厅系统 | 获取座位修饰符 | `GetSeatModifier(seatIndex, "AddOnMod")`, `GetSeatModifier(seatIndex, "DurabilityMod")` |
| 卡牌 ← 餐厅 | 餐厅系统 | 获取全局出牌上限加成 | `GetGlobalModifier("PlaysPerRound")`, `GetGlobalModifier("CardRecovery")` |
| 卡牌 ← 食客 | 食客系统 | 吞噬事件通知 | `OnDinerDevoured(raceId, flavor, freshness, traits[])` → 卡牌系统生成新卡 |
| 卡牌 → 食客 | 食客系统 | 加菜结果反馈 | 返回 EffectiveAddOnFee、T-A1 抵御结果、T-A2 小费 |
| 卡牌 → 回合流程 | 回合流程系统 | 出牌事件 | `OnCardPlayed(cardId, dinerId, effectiveFee)`，`OnCardExhausted(cardId, ...)` |
| 卡牌 ← 回合流程 | 回合流程系统 | 轮次开始重置出牌配额 | `ResetPlayQuota(roundIndex)` |
| 卡牌 ← 经济 | 经济系统 | 商店出售触发 | `SellCard(cardId) → returns ticketAmount` |
| 卡牌 → 遗物 | 遗物系统（未设计） | 卡牌生成/销毁事件 | `OnCardGenerated(card)`, `OnCardExhausted(card)`（provisional） |
| 卡牌 → 食谱进化 | 食谱进化系统（未设计） | 吞噬产卡事件 | `OnCardGenerated(raceId, card)`（provisional） |
| 卡牌 → UI | UI 系统 | 手牌数据、出牌反馈 | `GetHandCards()`, 事件通知 |

## Formulas

> **设计原则**：卡牌系统的所有数值（面值曲线参数、耐久度、出售价格、手牌上限、出牌上限等）**全部由 Luban 配置表直接提供或通过查找表获取**。本节不定义数学公式，仅描述数据来源、计算规则和数据流向。

### FC1：面值指数曲线（查找表驱动）

卡牌面值由食客美味度通过指数曲线转换：

**查找表**：TbFlavorToFaceValue

| Flavor | FaceValue（示例：50×2^Flavor） |
|--------|-------------------------------|
| 1 | 100 |
| 2 | 200 |
| 3 | 400 |
| 4 | 800 |
| 5 | 1600 |

- 曲线参数（FlavorCurveBase=50, FlavorCurveExponent=2）由 Luban 全局常量配置
- 运行时直接查表，不执行指数运算
- 策划可通过修改查找表值实现任意缩放曲线

### FC2：耐久度直接映射

卡牌耐久度 = 食客新鲜度，无计算：

| 属性 | 数据来源 | 生成规则 |
|------|---------|---------|
| MaxDurability | 食客 Freshness + 座位 DurabilityBonus | 新鲜度直接映射 + 座位产卡时加成（如保鲜柜 +1） |
| CurrentDurability | 初始 = MaxDurability | 使用时 -1（固定值） |

### FC3：出牌消耗规则

| 规则 | 数据来源 | 描述 |
|------|---------|------|
| 耐久消耗 | 常量 | 每次出牌消耗 1 点（固定值，不受修饰符影响） |
| 座位耐久加成 | 餐厅系统 GetSeatModifier | 产卡时增加 MaxDurability，不修改消耗速率 |

### FC4：出售价格

出售价格由 TbCardSellPrice 表直接配置，按卡牌属性（面值范围或种族）查找，不使用计算公式。

### FC5：数据流总览

```
吞噬事件 → 食客 (raceId, flavor, freshness)
  → TbFlavorToFaceValue[flavor] → CardFaceValue
  → freshness → MaxDurability
  → 生成卡牌实例 → 加入手牌

出牌 → CardFaceValue → 餐厅系统 F2 → EffectiveAddOnFee
     → CurrentDurability -= max(1 × DurabilityMod, 1)
     → 食客 CumulativeSpent += EffectiveAddOnFee
     → 概率判定（餐厅 F4/F5）

耐久度归零 → 回收判定 → 销毁/保留
商店阶段 → TbCardSellPrice → 出售换奖券
```

## Edge Cases

### EC-1：Flavor = 0 导致面值查找失败

- **If** 食客 Flavor = 0（配置约束应避免，但需兜底），TbFlavorToFaceValue 查找表中无 Flavor=0 条目
- **结果**：FaceValue 默认为 0，生成面值 0 的食材卡。打出后 EffectiveAddOnFee = 0，消耗出牌配额和耐久度但无经济收益
- **理由**：配置校验应强制 FlavorMin >= 1。系统层面兜底为 0

### EC-2：手牌为空

- **If** 所有卡牌已消耗或出售，吞噬尚未产出新卡，手牌为空
- **结果**：玩家无法出牌（前置检查失败），只能选择"结束轮次"，仅获基础餐费收入
- **理由**：空手牌是资源管理失误的惩罚，无保底机制。玩家需谨慎管理耐久度消耗

### EC-3：起手牌组数量超过 MaxHandSize

- **If** TbStartingCardConfig 配置的初始卡牌数 > MaxHandSize
- **结果**：游戏启动时配置校验报错，拒绝加载。配置校验强制初始卡牌数 ≤ MaxHandSize
- **理由**：起手阶段无"先弃后得"UI，配置不当应拦截

### EC-4：吞噬 + 霸王餐同一次加菜命中

- **If** 霸王餐与吞噬概率在同一次加菜中同时命中
- **结果**：霸王餐优先（餐厅系统既定规则）。食客离场不触发吞噬，不产出食材卡。出牌配额已消耗，卡牌耐久度已消耗
- **理由**：霸王餐优先于吞噬。对卡牌系统意味着：消耗了资源但 0 新卡产出

### EC-5：T-D5 幽灵残留 + 手牌已满（连续先弃后得）

- **If** 吞噬产卡时手牌 = MaxHandSize，基础产卡触发第一次"先弃后得"，T-D5 额外产卡触发第二次
- **结果**：两次"先弃后得"串行执行。第一次弃牌后加入第一张卡，第二次弃牌后加入第二张卡
- **理由**：每次产卡独立检查上限。实现时需确保 UI 可连续弹出

### EC-6：T-D2 连坐吞噬触发多次产卡

- **If** 食客 A 有 T-D2 被吞噬，连带选中食客 B 也被吞噬。两次 `OnDinerDevoured` 事件先后触发
- **结果**：A 的吞噬先结算（产卡 + 手牌上限检查），B 的吞噬后结算（产卡 + 手牌上限检查）。串行处理
- **理由**：T-D2 不递归，最多 2 次吞噬。串行确保每次产卡基于最新手牌状态

### EC-7：出牌过程中食客被吞噬/霸王餐

- **If** 玩家对食客 A 打出卡牌 X，执行到概率判定时吞噬或霸王餐触发
- **结果**：出牌流程继续执行完毕（耐久度归零检查正常），资源消耗不可撤回。吞噬产卡正常触发
- **理由**：出牌是原子操作，不因中间结果回滚。避免了复杂的回滚逻辑

### EC-8：T-D2 连坐吞噬导致正在被加菜的食客消失

- **If** 食客 A 有 T-D2 被吞噬，连带选中食客 B。B 是玩家下一步打算加菜的目标
- **结果**：B 被吞噬后状态变为 Devoured。玩家尝试对 B 出牌时，前置检查失败（B 不在 Dining 状态），出牌被拒绝，卡牌保留
- **理由**：出牌前的前置检查确保目标有效

### EC-9：保鲜柜等座位耐久度加成在产卡时生效

- **If** 食客 Freshness=3 坐在保鲜柜座位（DurabilityBonus=+1），被吞噬产卡
- **结果**：MaxDurability = 3 + 1 = 4。CurrentDurability = 4
- **理由**：耐久度加成在卡牌生成时一次性应用，不修改消耗速率。每次出牌固定消耗 1 点

### EC-10：S3 绞肉角高面值卡

- **If** S3（所有餐费 ×0.5，15%吞噬概率）打出 FaceValue=800 的卡
- **结果**：EffectiveAddOnFee = floor(800 × 0.5) = 400，吞噬概率 15% 正常判定
- **理由**：S3 的设计意图是"放弃收入换高频吞噬"。高面值卡在 S3 上餐费减半，但吞噬概率判定不受餐费影响

### EC-11：S4 板前座 + T-A1 挑食体质

- **If** 食客坐在 S4（基础餐费 = 0，加菜 ×1.5）且有 T-A1(ResistCount=2)
- **结果**：前 2 次加菜被抵御（消耗配额，不消耗耐久度）。第 3 次起正常生效，EffectiveAddOnFee × 1.5。S4 与 T-A1 作用于不同层面，互不干扰
- **理由**：T-A1 作用于"加菜是否有效"（前置检查），S4 的 AddOnMod 作用于"有效餐费计算"

### EC-12：S5 贵宾包厢 + T-D5 幽灵残留

- **If** 食客坐在 S5（金币 ×5）被吞噬，T-D5 触发
- **结果**：产出 2 张相同 FaceValue 和 MaxDurability 的卡牌。S5 的 ×5 修饰符影响食客金币（更多收入），不影响食客属性（卡牌品质不变）
- **理由**：吞噬产卡基于食客 Flavor/Freshness，不受座位修饰符影响

### EC-13：卡牌回收判定与吞噬结果独立

- **If** 出牌后耐久度归零，同时概率判定触发了吞噬
- **结果**：回收判定独立执行——回收成功则卡牌保留（恢复配置的 DurabilityAmount），与食客被吞噬/霸王餐/付费的结果无关
- **理由**：卡牌生命周期和食客生命周期是两个独立系统

### EC-14：吞噬产卡发生在"先弃后得"UI 等待期间

- **If** 吞噬产卡 → 手牌已满 → 弹出"先弃后得"UI → 此时另一个吞噬事件触发
- **结果**：第二个吞噬事件入队等待。第一个"先弃后得"完成后处理第二个产卡请求。不会出现两个 UI 叠加
- **理由**：产卡事件串行处理，需事件队列确保 UI 交互完成后处理下一个请求

### EC-15：出售价格配置缺失

- **If** 卡牌属性在 TbCardSellPrice 表中无匹配条目
- **结果**：出售价格为 0（兜底值）。配置校验应确保所有可能的卡牌属性组合都有对应售价
- **理由**：缺配置 = 免费卖，鼓励策划完善配置

## Dependencies

| 方向 | 系统 | 关系 | 数据接口 | 硬/软依赖 |
|------|------|------|---------|----------|
| 卡牌 ← | 餐厅系统 | **上游** | `GetSeatModifier(seatIndex, "AddOnMod")`, `GetSeatModifier(seatIndex, "DurabilityBonus")` — 获取加菜餐费修饰和产卡耐久度加成 | **硬依赖** |
| 卡牌 ← | 餐厅系统 | **上游** | `GetGlobalModifier("PlaysPerRound")`, `GetGlobalModifier("CardRecovery")` — 获取出牌上限和回收修饰 | **硬依赖** |
| 卡牌 → | 餐厅系统 | **上游反向** | 输出 CardFaceValue，餐厅系统计算 EffectiveAddOnFee（F2 公式） | **硬依赖** |
| 卡牌 ← | 食客系统 | **上游** | `OnDinerDevoured(raceId, flavor, freshness, traits[])` — 吞噬事件触发产卡 | **硬依赖** |
| 卡牌 → | 食客系统 | **下游** | 返回加菜结果（EffectiveAddOnFee、T-A1 抵御、T-A2 小费） | **硬依赖** |
| 卡牌 ← | 回合流程系统 | **上游** | `ResetPlayQuota(roundIndex)` — 轮次开始重置出牌配额 | **硬依赖** |
| 卡牌 → | 回合流程系统 | **下游** | `OnCardPlayed(cardId, dinerId, fee)`, `OnCardExhausted(cardId)` — 出牌和销毁事件 | **硬依赖** |
| 卡牌 ← | 经济系统 | **上游** | `SellCard(cardId)` — 商店阶段出售卡牌换奖券 | **硬依赖** |
| 卡牌 → | 遗物系统 | **下游** | `OnCardGenerated(card)`, `OnCardExhausted(card)` — 产卡/销毁事件（provisional） | **软依赖** |
| 卡牌 → | 食谱进化系统 | **下游** | `OnCardGenerated(raceId, card)` — 产卡事件通知熟练度（provisional） | **软依赖** |
| 卡牌 ← | 元进度系统 | **上游** | 初始牌组配置来源（provisional，MVP 使用固定默认配置） | **软依赖** |
| 卡牌 → | UI 系统 | **下游** | `GetHandCards()`, 出牌反馈事件 | **硬依赖** |

**双向依赖验证**：
- 餐厅系统 GDD 列出"卡牌系统"为下游依赖 ✅
- 食客系统 GDD 列出"卡牌系统"为下游依赖 ✅
- 回合流程系统 GDD 尚未设计（需列出卡牌系统为交互系统）
- 经济系统 GDD 尚未设计（需列出卡牌系统为出售交互）
- 遗物系统、食谱进化系统 GDD 尚未设计（需列出卡牌系统为上游依赖）

## Tuning Knobs

| 旋钮 | 配置表 | 字段 | 安全范围 | 调高效果 | 调低效果 |
|------|--------|------|---------|---------|---------|
| 面值曲线基数 | Luban 常量 | FlavorCurveBase | > 0 | 所有卡牌面值整体提升 | 面值整体降低 |
| 面值曲线指数 | Luban 常量 | FlavorCurveExponent | > 1.0 | 高 Flavor 食客产出面值暴涨，低 Flavor 几乎不变 | 缩小高低面值差距 |
| 手牌上限 | Luban 常量 | MaxHandSize | [3, 20] | 更多手牌空间，进化链更宽松 | 频繁触发"先弃后得"，精简压力大 |
| 每轮基础出牌上限 | Luban 常量 | BasePlaysPerRound | [1, 20] | 每轮可操作更多，策略空间更大 | 操作受限，必须精准选目标 |
| 出售价格 | TbCardSellPrice | price | ≥ 0 | 出售收益高，鼓励精简牌库 | 出售不划算，倾向保留 |
| 回收概率 | 餐厅设施配置 | RecoveryChance | [0, 1.0] | 卡牌更耐用，资源压力减小 | 纯消耗体验，资源管理更重要 |
| 回收恢复量 | 餐厅设施配置 | RecoveryAmount | [1, MaxDurability] | 恢复满耐久（几乎无限使用） | 仅恢复 1 次（续命 1 轮） |
| 起手牌数量 | TbStartingCardConfig | count | ≤ MaxHandSize | 开局更多资源 | 开局更紧，依赖早期吞噬 |
| 起手牌属性 | TbStartingCardConfig | face_value, durability | ≥ 1 | 开局卡更强 | 开局更弱，进化空间更大 |
| 座位耐久度加成 | TbSeatType | DurabilityBonus | ≥ 0 | 该座位产出的卡更耐用 | 无加成 |

> **注**：所有旋钮由 Luban 配置表定义，策划可在不修改代码的情况下调整。

## Visual/Audio Requirements

卡牌系统的视觉/音频需求将在 UX 设计阶段定义。

## UI Requirements

卡牌系统的 UI 需求将在 `/ux-design` 阶段定义，不在本 GDD 范围内。

## Acceptance Criteria

### 卡牌属性与生成

1. **GIVEN** 食客被吞噬(RaceId=R3, Flavor=2, Freshness=4), **WHEN** 卡牌系统接收 OnDinerDevoured, **THEN** 生成卡牌 FaceValue=TbFlavorToFaceValue[2]=200, MaxDurability=4, SourceRaceId=R3

2. **GIVEN** 食客被吞噬(Flavor=1), TbFlavorToFaceValue 配置 Flavor=1→100, **WHEN** 查找面值, **THEN** FaceValue=100

3. **GIVEN** 食客 Freshness=3, 座位 DurabilityBonus=+1(保鲜柜), **WHEN** 生成卡牌, **THEN** MaxDurability=3+1=4

4. **GIVEN** TbStartingCardConfig 配置 5 张初始卡, **WHEN** 游戏开始, **THEN** 手牌包含 5 张卡, 属性与配置一致

### 面值曲线

5. **GIVEN** Flavor=1→100, Flavor=2→200, Flavor=3→400(50×2^Flavor), **WHEN** 查表, **THEN** FaceValue 随 Flavor 指数增长

6. **GIVEN** Flavor=0(TbFlavorToFaceValue 无此条目), **WHEN** 查找, **THEN** FaceValue=0(兜底默认)

### 手牌管理

7. **GIVEN** MaxHandSize=10, 当前手牌=9, **WHEN** 吞噬产卡 1 张, **THEN** 手牌=10, 无弃牌 UI

8. **GIVEN** MaxHandSize=10, 当前手牌=10, **WHEN** 吞噬产卡 1 张, **THEN** 弹出弃牌 UI, 玩家选择丢弃后新卡加入手牌仍=10

9. **GIVEN** MaxHandSize=10, 当前手牌=10, T-D5(ExtraCardChance=100%), **WHEN** 吞噬产卡, **THEN** 连续两次"先弃后得", 最终手牌=10(丢弃 2 张旧卡, 加入 2 张新卡)

10. **GIVEN** 手牌=0, **WHEN** 轮次开始, **THEN** 玩家无法出牌, 只能结束轮次

### 出牌上限

11. **GIVEN** BasePlaysPerRound=5, 无设施加成, **WHEN** 玩家打出第 5 张卡, **THEN** 成功; **WHEN** 尝试打出第 6 张, **THEN** 拒绝

12. **GIVEN** BasePlaysPerRound=5, 设施加成=+2, **WHEN** 玩家打出第 7 张卡, **THEN** 成功; **WHEN** 尝试打出第 8 张, **THEN** 拒绝

### 出牌执行

13. **GIVEN** 卡牌 FaceValue=200, 座位 AddOnMod=1.5(板前座 S4), **WHEN** 打出, **THEN** EffectiveAddOnFee=floor(200×1.5)=300, CurrentDurability-=1

14. **GIVEN** 食客有 T-A1(ResistCount=1), **WHEN** 第 1 次加菜, **THEN** EffectiveAddOnFee=0, 不消耗耐久度, 消耗 1 配额; **WHEN** 第 2 次加菜, **THEN** 正常生效

15. **GIVEN** 食客有 T-A2(TipRatio=0.1), EffectiveBaseFee=20, **WHEN** 被加菜 1 次, **THEN** 额外小费=floor(20×0.1)=2

16. **GIVEN** 卡牌 CurrentDurability=1, **WHEN** 打出, **THEN** CurrentDurability=0, 触发销毁流程

### 卡牌销毁与回收

17. **GIVEN** 卡牌耐久度归零, 无回收修饰符, **WHEN** 检查, **THEN** 卡牌从手牌移除, 触发 OnCardExhausted 事件

18. **GIVEN** 卡牌耐久度归零, RecoveryChance=0.5(固定种子命中), RecoveryAmount=1, **WHEN** 回收判定, **THEN** CurrentDurability=1, 卡牌保留在手牌

19. **GIVEN** 卡牌耐久度归零, RecoveryChance=0.5(固定种子未命中), **WHEN** 回收判定, **THEN** 正常销毁

### 卡牌出售

20. **GIVEN** 商店阶段, 卡牌 FaceValue=200, TbCardSellPrice 配置售价=50, **WHEN** 出售, **THEN** 卡牌从手牌移除, 玩家获得 50 奖券

21. **GIVEN** 非商店阶段(轮次进行中), **WHEN** 尝试出售, **THEN** 拒绝出售

### 吞噬产卡特殊

22. **GIVEN** 食客有 T-D5(ExtraCardChance=100%), **WHEN** 吞噬, **THEN** 产出 2 张相同属性卡牌

23. **GIVEN** T-D2 连坐吞噬食客 A 和 B, A 的 Flavor=2, B 的 Flavor=3, **WHEN** 两次 OnDinerDevoured, **THEN** 串行产出 2 张卡牌，第 1 张 FaceValue=TbFlavorToFaceValue[2], 第 2 张 FaceValue=TbFlavorToFaceValue[3]

### 边缘情况

24. **GIVEN** 霸王餐与吞噬同时命中, **WHEN** 判定, **THEN** 霸王餐优先, 不触发 OnDinerDevoured, 不产出食材卡, 卡牌耐久度已消耗

25. **GIVEN** 出牌后吞噬概率触发(食客被吞噬), **WHEN** 流程完成, **THEN** 出牌配额已消耗, 卡牌耐久度已消耗, 吞噬产卡正常触发

26. **GIVEN** T-D2 连坐吞噬导致目标食客消失, **WHEN** 玩家尝试对已吞噬食客出牌, **THEN** 前置检查失败, 出牌被拒绝, 卡牌保留

27. **GIVEN** S3(餐费×0.5), 卡牌 FaceValue=800, **WHEN** 打出, **THEN** EffectiveAddOnFee=floor(800×0.5)=400, 吞噬概率 15% 正常判定

28. **GIVEN** 卡牌属性在 TbCardSellPrice 无匹配, **WHEN** 出售, **THEN** 价格=0

## Open Questions

1. **元进度系统对初始牌组的影响方式**：元进度系统（GDD 未设计）如何修改初始牌组？是替换整个牌组、添加额外卡牌、还是提高起手卡属性？——**Owner**: 元进度系统 GDD
2. **遗物对卡牌的修改方式**：遗物系统可能修改卡牌行为（如"所有史莱姆族卡牌面值 +2"）。是动态计算（出牌时叠加修饰符）还是生成时固化？——**Owner**: 遗物系统 GDD
3. **食谱进化对卡牌的影响**：食谱进化系统（GDD 未设计）是否在吞噬产卡时修改产出（如进化后产出更高面值卡）？——**Owner**: 食谱进化系统 GDD
4. **"先弃后得"UI 的交互细节**：手牌满时吞噬产卡的弃牌选择 UI 应如何呈现？是否允许查看新卡信息后再决定？——**Owner**: UX 设计阶段
5. **手牌上限的具体数值**：MaxHandSize 应配置为多少？需要配合耐久度消耗速率和吞噬频率共同调整。——**Owner**: 数值平衡阶段
