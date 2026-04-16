# UI 生命周期与核心 API

## UILayer 层级

| 值 | 层级 | 用途 |
|----|------|------|
| 0 | Bottom | 底层（世界空间 UI、背景）|
| 1 | UI | 普通 UI 层（主要界面）|
| 2 | Top | 顶层（弹窗、全屏遮罩）|
| 3 | Tips | 提示层（Toast、飘字）|
| 4 | System | 系统层（加载中、异常提示）|

---

## WindowAttribute 窗口标记

每个 UIWindow 子类必须标记 `[Window]` 特性：

```csharp
[Window(UILayer.UI, "BattleMainUI")]
public class BattleMainUI : UIWindow { }

[Window(layer: UILayer.Top, location: "LoginUI", fullScreen: true, hideTimeToClose: 10f)]
public class LoginUI : UIWindow { }
```

**location** 即 Prefab 资源地址（`AssetRaw/UI/Prefabs/` 下的文件名，不含扩展名）。

---

## UIWindow 生命周期

```
ShowUI<T>()
    │
    ▼
ScriptGenerator()     ← 首次：绑定 UI 节点引用（仅一次）
    │
    ▼
BindMemberProperty()  ← 首次：框架预留扩展点，通常跳过
    │
    ▼
RegisterEvent()       ← 首次：注册 UI 事件（随窗口销毁自动清理）
    │
    ▼
OnCreate()            ← 首次：窗口创建初始化（仅一次）
    │
    ▼
OnRefresh()           ← 每次 ShowUI 都执行（刷新显示数据）
    │
    ▼
OnUpdate()            ← 每帧更新（尽量避免，改用 Timer）
    │
    ▼
HideUI() / CloseUI()
    ├── 隐藏：超时后触发 Close
    └── 关闭 → OnDestroy()  ← 销毁前清理
```

**关键规则**：
- `ScriptGenerator` / `RegisterEvent` / `OnCreate` 只执行一次
- `OnRefresh` 每次 Show 都执行
- 禁止在 `OnUpdate` 做高频计算，改用 `GameModule.Timer`
- 完整前缀表见 [naming-rules.md](naming-rules.md#ui-节点命名规范)

---

## UIModule 核心 API

```csharp
// 打开窗口
GameModule.UI.ShowUIAsync<BattleMainUI>();                       // fire and forget
var win = await GameModule.UI.ShowUIAsyncAwait<BattleMainUI>();  // 等待实例
GameModule.UI.ShowUIAsync<ItemDetailUI>(itemId, extraData);      // 携带用户数据

// 关闭 / 隐藏
GameModule.UI.CloseUI<BattleMainUI>();          // 关闭并销毁
GameModule.UI.HideUI<BattleMainUI>();           // 隐藏（超时自动关闭）
GameModule.UI.CloseAll();                       // 关闭所有
GameModule.UI.CloseAllWithOut<BattleMainUI>();  // 保留指定窗口

// 查询
bool exists   = GameModule.UI.HasWindow<BattleMainUI>();
bool loading  = GameModule.UI.IsAnyLoading();
```

---

## UIWidget 子组件

```csharp
var widget = CreateWidget<ItemWidget>("path/to/node");                     // 路径
var widget = CreateWidgetByPath<ItemWidget>(parent, "Location");           // 动态加载
var widget = await CreateWidgetByPathAsync<ItemWidget>(parent, "Loc");     // 异步
var widget = CreateWidgetByPrefab<ItemWidget>(prefab, parent);             // Prefab 克隆
AdjustIconNum<ItemWidget>(listIcon, count: items.Count, parent, prefab);   // 列表数量
```

UIWidget 生命周期与 UIWindow 相同：`ScriptGenerator → RegisterEvent → OnCreate → OnRefresh → OnDestroy`

---

## UI 内部事件

在 `RegisterEvent()` 中使用，事件监听随窗口销毁**自动清理**：

```csharp
protected override void RegisterEvent()
{
    AddUIEvent(GameEventDef.OnGoldChanged, OnGoldChanged);
    AddUIEvent<int>(GameEventDef.OnHpChanged, OnHpChanged);
}
```

**禁止**在 `RegisterEvent` 外使用 `GameEvent.AddEventListener`（不会自动清理，导致内存泄漏）。
详见 [event-system.md](event-system.md#常见错误)。
