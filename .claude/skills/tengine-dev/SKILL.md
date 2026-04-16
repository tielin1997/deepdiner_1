---
name: tengine-dev
description: TEngine Unity 游戏框架开发指导。Use when working with TEngine framework code: (1) creating UIWindow/UIWidget or UI lifecycle questions, (2) using GameModule APIs (Resource/Timer/Scene/Audio/Fsm), (3) implementing event system (GameEvent/AddUIEvent/EventInterface), (4) resource loading with YooAsset (SetSprite/LoadGameObjectAsync/LoadAssetAsync), (5) HybridCLR hotfix code in GameScripts/HotFix/, (6) Luban config tables (ConfigSystem/TbXxx), (7) UniTask async patterns, (8) naming conventions and code standards, (9) Unity MCP tool operations (batch_execute/manage_ui/manage_scene). Triggers on: UIWindow, UIWidget, GameModule, ResourceModule, YooAsset, HybridCLR, UniTask, EventInterface, GameEvent, Luban, ConfigSystem, GameApp, unity-mcp, batch_execute, ShowUIAsync, LoadAssetAsync, SetSprite, AddUIEvent, ProcedureBase. Do NOT trigger for: L1 tasks (typo fixes, comment changes, variable renames).
---

# TEngine 开发技能

TEngine 基于 HybridCLR + YooAsset + UniTask + Luban 构建。

## 参考文档导航

按需读取 references/ 下的文件，不要一次全部加载。

### 核心开发

| 场景 | 文档 |
|------|------|
| 项目架构 / 启动流程 / 程序集划分 | [architecture.md](references/architecture.md) |
| 模块 API（Timer/Scene/Audio/Fsm/MemoryPool/Log） | [modules.md](references/modules.md) |
| UI 开发（生命周期 / UILayer / WindowAttribute / Widget） | [ui-lifecycle.md](references/ui-lifecycle.md) |
| 事件系统（int 事件 / 接口事件 / 常见错误） | [event-system.md](references/event-system.md) |
| 资源加载（SetSprite / LoadGameObject / 卸载 / 生命周期模式） | [resource-api.md](references/resource-api.md) |
| 热更代码（GameApp / Procedure / HybridCLR / 热更包下载） | [hotfix-workflow.md](references/hotfix-workflow.md) |
| Luban 配置表（定义 / 生成 / 访问 / 新增表） | [luban-config.md](references/luban-config.md) |
| 代码规范（命名 / UI 节点前缀 / 异步模式 / 禁止模式） | [naming-rules.md](references/naming-rules.md) |

### 进阶模式

| 场景 | 文档 |
|------|------|
| UI 进阶（完整示例 / Widget 模板 / 列表复用 / 手动绑定） | [ui-patterns.md](references/ui-patterns.md) |

### MCP 工具

| 场景 | 文档 |
|------|------|
| MCP 全部工具（场景/GO/UI Prefab/脚本/资源/编辑器/调试） | [mcp-tools.md](references/mcp-tools.md) |
| 材质 / Shader / 纹理 / 粒子 / 动画 | [mcp-visual.md](references/mcp-visual.md) |

### 排障

| 场景 | 文档 |
|------|------|
| 编译/热更/资源/UI/事件/内存/Luban/UniTask 问题 | [troubleshooting.md](references/troubleshooting.md) |

## 使用指引

1. **代码优先于文档**：若 references 中的 API 与项目实际代码冲突，以代码为准，在输出中标注冲突点
2. **按需加载**：只读取与当前任务相关的参考文件，避免浪费 context

---

## 核心原则（编码红线）

1. **模块访问**：通过 `GameModule.XXX` 访问，而非 `ModuleSystem.GetModule<T>()`
2. **资源必须释放**：`LoadAssetAsync` 对应 `UnloadAsset`，GameObject 用 `LoadGameObjectAsync`
3. **热更边界**：`GameScripts/Main` 不热更，`GameScripts/HotFix/` 全部热更
4. **事件解耦**：模块间用 `GameEvent`，UI 内部用 `AddUIEvent`

---
