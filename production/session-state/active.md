# Active Session State

> **Last Updated**: 2026-04-17

## Current Task

- Luban 配置表设计与建立完成

## Status

- **17张配置表全部创建** (Schema + 初始数据)
- **7个枚举 + 5个Bean** 已定义
- **~90行初始数据** 已填充
- 导出验证: **待完成** (需安装Luban CLI工具)

## Completed This Session

- [x] Phase 1: 7个枚举 (ERace, EEmotionType, EEmotionPathId, ETraitCategory, ERelicRarity, EFacilityScope, EShopCategory)
- [x] Phase 2: 5个Bean (FloatRange, EmotionStep, DinerPoolEntry, StartingCardEntry, ModifierEntry)
- [x] Phase 3A: 基础数据表 (TbRace 6行, TbGlobalConst 1行, TbShopConfig 1行, TbFlavorToFaceValue 20行)
- [x] Phase 3B: Restaurant表 (TbSeatType 5行, TbFacility Schema, TbRestaurantProgress 21行)
- [x] Phase 3C: Diner表 (TbTrait 13行, TbEmotionPath 4行, TbDinerTemplate 6行, TbDinerPool 7行, TbBossTemplate 7行)
- [x] Phase 3D: Card表 (TbStartingCardConfig 1行, TbCardTemplate 6行, TbCardSellPrice 12行)
- [x] Phase 3E: TurnFlow表 (TbStage 21行)
- [x] Phase 3F: Economy预留 (TbRelic Schema only)
- [x] 清理旧item表

## Files Modified

- `Configs/GameConfig/Datas/__enums__.xlsx` — 7个新枚举
- `Configs/GameConfig/Datas/__beans__.xlsx` — 5个新Bean
- `Configs/GameConfig/Datas/__tables__.xlsx` — 17张新表注册
- `Configs/GameConfig/Datas/#*.xlsx` — 17个数据文件
- `scripts/luban_helper.py` — 复制到项目根目录
- `production/session-state/active.md` — 本文件

## Next Steps

- 安装Luban CLI工具到 `Tools/Luban/`
- 运行 `gen_code_bin_to_project_lazyload.bat` 导出C#代码和二进制数据
- 清理旧demo bean/enum (item.ItemExchange, test.*等)
- 在 `GameLogic/Config/` 创建各系统的ConfigMgr封装
