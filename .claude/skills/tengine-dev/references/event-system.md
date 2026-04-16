# 事件系统

TEngine 提供两种事件模式：**int 事件**（轻量）和**接口事件**（类型安全）。

## 模式对比

| 特性 | int 事件 | 接口事件 |
|------|---------|---------|
| 定义方式 | `const int` 常量 | 带 `[EventInterface]` 的接口 |
| 发送 | `GameEvent.Send(int)` | `GameEvent.Send<ITrade>(s => s.OnTrade(...))` |
| 监听 | `GameEvent.AddEventListener(int, callback)` | 实现接口 + `RegisterListener` |
| 类型安全 | ❌ 无编译检查 | ✅ 编译期检查 |
| 适用场景 | 简单通知、UI 内部 | 模块间通信、多参数 |

---

## int 事件

```csharp
// 定义事件 ID
public static class GameEventDef
{
    public const int OnGoldChanged = 1;
    public const int OnLevelUp = 2;
}

// 发送
GameEvent.Send(GameEventDef.OnGoldChanged);
GameEvent.Send<int>(GameEventDef.OnHpChanged, 50);

// 监听（UI 内用 AddUIEvent 自动清理）
AddUIEvent(GameEventDef.OnGoldChanged, OnGoldChanged);
AddUIEvent<int>(GameEventDef.OnHpChanged, OnHpChanged);

// 非 UI 类监听（必须手动移除）
private EventDelegate _handler;
void OnEnable() { _handler = GameEvent.AddEventListener<int>(GameEventDef.OnHpChanged, OnHpChanged); }
void OnDisable() { GameEvent.RemoveEventListener(GameEventDef.OnHpChanged, _handler); }
```

---

## 接口事件

```csharp
// 1. 定义接口（必须 [EventInterface]）
[EventInterface]
public interface ITrade
{
    void OnTradeComplete(int itemId, int count);
}

// 2. 实现并注册
public class TradeSystem : ITrade
{
    void ITrade.OnTradeComplete(int itemId, int count) { /* 处理 */ }
    public void Init() { GameEventHelper.RegisterListener<ITrade>(this); }
}

// 3. 发送
GameEvent.Send<ITrade>(s => s.OnTradeComplete(itemId, count));
```

**前提**：`GameEventHelper.Init()` 已在 `GameApp.Entrance` 中最先调用。

---

## 事件定义规范

| 规则 | 说明 |
|------|------|
| ID 范围 | 自定义事件 ≥ 10000，1~9999 保留 |
| 命名 | `On` + 过去式动词 + 名词：`OnGoldChanged`、`OnBattleEnded` |
| 接口命名 | `I` + 动词 + 名词：`ITrade`、`IBattle` |
| 泛型参数 | 最多 1 个，复杂参数用 struct/类 |
| 禁止 | 手写事件 ID 硬编码数字，应用常量 |

---

## 常见错误

### 1. 忘记 GameEventHelper.Init()

```csharp
// ❌ GameEvent.Get<T>() 全部无响应，无报错，极难排查
public static void Entrance(Assembly[] assemblies)
{
    // GameEventHelper.Init();  ← 忘记调用
    GameApp_RegisterSystem.Register();
}

// ✅ 必须最先调用
GameEventHelper.Init();
```

### 2. UI 外部使用 AddEventListener（内存泄漏）

```csharp
// ❌ 退出窗口不会自动清理
void SomeMethod()
    => GameEvent.AddEventListener<int>(IBattleEvent_Event.OnHpChanged, OnHpChanged);

// ✅ UIWindow 中用 AddUIEvent（自动清理）
protected override void RegisterEvent()
    => AddUIEvent<int>(IBattleEvent_Event.OnHpChanged, OnHpChanged);

// ✅ 非 UI 类用 GameEventMgr
private readonly GameEventMgr _eventMgr = new();
public void Dispose() => _eventMgr.Clear();
```

### 3. 手写事件 ID 常量

```csharp
// ❌ 手写 int 常量，容易重复/拼错
public const int OnHpChanged = 1001;

// ✅ Source Generator 自动生成
AddUIEvent(IBattleEvent_Event.OnHpChanged, OnHpChanged);
```

### 4. 事件回调签名不匹配

```csharp
// ❌ 事件发送 int，回调接收 string → 运行时异常
GameEvent.Send<int>(IBattleEvent_Event.OnHpChanged, hp);
AddUIEvent<string>(IBattleEvent_Event.OnHpChanged, OnHp);

// ✅ 接口事件模式可编译期检查
GameEvent.Get<IBattleEvent>().OnHpChanged(hp); // 类型安全
```

### 5. 非 UI 类忘记移除监听

```csharp
// ❌ 销毁时不移除，回调引用已释放对象
public class PlayerSystem
{
    public void Init() => GameEvent.AddEventListener(IPlayerEvent_Event.OnDead, OnDead);
    // 没有 RemoveEventListener → 泄漏
}

// ✅ 使用 GameEventMgr 批量清理
private readonly GameEventMgr _eventMgr = new();
public void Init()    => _eventMgr.AddEvent(IPlayerEvent_Event.OnDead, OnDead);
public void Dispose() => _eventMgr.Clear();
```
