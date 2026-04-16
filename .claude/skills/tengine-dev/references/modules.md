# TEngine 模块 API 速查

## GameModule 统一访问入口

所有模块通过 `GameModule` 静态类访问（已缓存），禁止重复 `ModuleSystem.GetModule<T>()`：

```csharp
GameModule.Resource      // IResourceModule  — 资源加载
GameModule.UI            // UIModule         — UI 管理
GameModule.Audio         // IAudioModule     — 音频
GameModule.Timer         // ITimerModule     — 计时器
GameModule.Scene         // ISceneModule     — 场景
GameModule.Fsm           // IFsmModule       — 有限状态机
GameModule.Procedure     // IProcedureModule — 流程
GameModule.Localization  // ILocalizationModule — 本地化
```

---

## TimerModule 计时器

```csharp
// 添加计时器
int tid = GameModule.Timer.AddTimer(OnTick, time: 3f);                         // 单次
int tid = GameModule.Timer.AddTimer(OnTick, time: 1f, isLoop: true);           // 循环
int tid = GameModule.Timer.AddTimer(OnTick, time: 5f, isUnscaled: true);       // 非缩放时间

// 控制
GameModule.Timer.Stop(timerId);         // 暂停
GameModule.Timer.Resume(timerId);       // 恢复
GameModule.Timer.Restart(timerId);      // 重置
GameModule.Timer.RemoveTimer(timerId);  // 移除（销毁时必须调用）
GameModule.Timer.RemoveAllTimer();      // 清除所有

// 查询
float left = GameModule.Timer.GetLeftTime(timerId);
bool running = GameModule.Timer.IsRunning(timerId);
```

**OnDestroy 中必须 `RemoveTimer(tid)`**，防止空引用。

---

## SceneModule 场景管理

```csharp
// 加载
Scene scene = await GameModule.Scene.LoadSceneAsync("SceneName");
Scene scene = await GameModule.Scene.LoadSceneAsync("SceneName", LoadSceneMode.Additive);
Scene scene = await GameModule.Scene.LoadSceneAsync("SceneName", LoadSceneMode.Single,
    progressCallBack: p => { /* 0~1 */ });

// 卸载/激活
bool ok = await GameModule.Scene.UnloadAsync("SceneName");
GameModule.Scene.ActivateScene("SceneName");
bool has = GameModule.Scene.IsContainScene("SceneName");
```

场景资产须在 `AssetRaw/Scenes/`，禁止 `SceneManager.LoadScene()`。

---

## AudioModule 音频

```csharp
// 播放
AudioAgent agent = GameModule.Audio.Play(AudioType.Music, "bgm_path", bLoop: true);   // BGM
AudioAgent agent = GameModule.Audio.Play(AudioType.Sound, "sfx_path");                  // 音效
AudioAgent agent = GameModule.Audio.Play(AudioType.UISound, "ui_click", bAsync: true);  // UI

// 停止
GameModule.Audio.Stop(AudioType.Music, fadeout: true);
GameModule.Audio.StopAll(fadeout: false);

// 音量
GameModule.Audio.Volume      = 1.0f;  // 全局 (0~1)
GameModule.Audio.MusicVolume = 0.8f;
GameModule.Audio.SoundVolume = 1.0f;
GameModule.Audio.MusicEnable = true;
GameModule.Audio.SoundEnable = true;
```

---

## FsmModule 有限状态机

```csharp
// 定义状态
public class IdleState : FsmState<MyOwner>
{
    protected override void OnEnter(IFsm<MyOwner> fsm)  { }
    protected override void OnUpdate(IFsm<MyOwner> fsm, float elapse, float real) { }
    protected override void OnLeave(IFsm<MyOwner> fsm, bool isShutdown) { }
}

// 创建并启动
IFsm<MyOwner> fsm = GameModule.Fsm.CreateFsm<MyOwner>("FsmName", owner,
    new IdleState(), new RunState(), new AttackState());
fsm.Start<IdleState>();

// 切换与传数据
fsm.ChangeState<RunState>();
fsm.SetData<int>("Key", value);
int val = fsm.GetData<int>("Key");

// 销毁
GameModule.Fsm.DestroyFsm<MyOwner>("FsmName");
```

---

## MemoryPool 内存池

频繁创建/销毁的纯 C# 对象，避免 GC：

```csharp
public class DamageInfo : IMemory
{
    public int Damage;
    public void Clear() { Damage = 0; }  // 归还时重置
}

var info = MemoryPool.Acquire<DamageInfo>();
info.Damage = 100;
MemoryPool.Release(info);  // Release 后禁止再访问，禁止 Release 两次
```

---

## Log 日志系统

```csharp
Log.Debug("仅 Development Build 输出");  // 发布包自动剥离
Log.Info("普通信息");
Log.Warning("警告");
Log.Error("错误，始终保留");
Log.Fatal("严重错误");
Log.Assert(condition, "断言失败提示");
```
