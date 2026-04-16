# 热更代码开发与热更包管理

## 程序集划分

```
GameScripts/HotFix/
├── GameProto/          # Luban 生成（勿手改）
│   ├── LubanLib/       # ByteBuf 等序列化库
│   ├── GameConfig/     # Tables + 配置类
│   └── ConfigSystem.cs # 配置加载器
│
└── GameLogic/          # 业务逻辑（主开发区域）
    ├── GameApp.cs                  # 热更主入口
    ├── GameApp_RegisterSystem.cs   # 系统注册
    ├── GameModule.cs               # 模块统一访问入口
    ├── IEvent/                     # 事件接口定义
    ├── Module/                     # 模块实现（如 UIModule）
    ├── System/                     # 业务系统
    ├── UI/                         # UI 窗口代码
    └── Data/                       # 玩家数据、运行时缓存
```

**依赖规则**（不可逆向）：`GameLogic → GameProto`、`GameLogic → TEngine.Runtime`
主包 `GameScripts/Main/` 不可热更，仅含启动器和流程驱动。

---

## GameApp 入口

```csharp
// GameApp.cs
public class GameApp
{
    public static void Entrance(Assembly[] assemblies)
    {
        GameEventHelper.Init();                      // 1. 必须最先调用
        GameApp_RegisterSystem.Register();            // 2. 注册业务系统
        ModuleSystem.GetModule<IProcedureModule>()
            .ChangeState<ProcedureLogin>();           // 3. 启动游戏流程
    }
}
```

**`GameEventHelper.Init()` 必须在任何 `GameEvent.Get<T>()` 之前调用**。

---

## 流程状态机

热更域流程继承 `ProcedureBase`：

```csharp
public class ProcedureLogin : ProcedureBase
{
    protected override void OnEnter(ProcedureOwner owner)
        => GameModule.UI.ShowUIAsync<LoginUI>();
    protected override void OnLeave(ProcedureOwner owner, bool isShutdown)
        => GameModule.UI.CloseUI<LoginUI>();
}

// 流程切换
ChangeState<ProcedureMain>(procedureOwner);            // 内部
GameModule.Procedure.ChangeState<ProcedureMain>();      // 外部
```

---

## HybridCLR 注意事项

```csharp
// ❌ 热更代码不能引用主包 internal 类型
// ❌ 避免 System.Reflection 大量使用（AOT 限制多）
// ❌ 不支持：dynamic、Expression<T> 编译、部分 Emit、Marshal/P-Invoke
```

### AOT 泛型补充

```
HybridCLR → Generate → AOT Generic References → 生成 AOTGenericReferences.cs
如出现 ExecutionEngineException，手动添加对应泛型使用后重新打包
```

常见需补充：`List<自定义类型>`、`Dictionary<K,V>` 新组合、`UniTask<自定义类型>`、`Action<自定义类型>`

---

## 热更包下载流程

主包流程驱动，不可热更：

```
ProcedureInitResources → ProcedureInitPackage → ProcedureCreateDownloader
→ ProcedureDownloadFile → ProcedurePreload → ProcedureLoadAssembly
```

### 核心代码

```csharp
public async UniTask CheckAndDownloadUpdate()
{
    // 1. 请求远端版本
    var versionOp = GameModule.Resource.RequestPackageVersionAsync();
    await versionOp.Task;
    // 2. 更新 Manifest
    var manifestOp = GameModule.Resource.UpdatePackageManifestAsync(versionOp.PackageVersion);
    await manifestOp.Task;
    // 3. 创建下载器
    var downloader = GameModule.Resource.CreateResourceDownloader();
    if (downloader.TotalDownloadCount == 0) return;
    // 4. 下载
    downloader.OnDownloadProgressCallback = OnProgress;
    downloader.BeginDownload();
    await downloader.Task;
    // 5. 清理旧缓存
    await GameModule.Resource.ClearCacheFilesAsync();
}
```

### API 速查

| 方法 | 说明 |
|------|------|
| `GetPackageVersion()` | 本地资源包版本号 |
| `RequestPackageVersionAsync()` | 请求远端版本号 |
| `UpdatePackageManifestAsync(ver)` | 更新资源清单 |
| `CreateResourceDownloader()` | 创建差量下载器 |
| `downloader.TotalDownloadCount` | 待下载文件数 |
| `downloader.TotalDownloadBytes` | 待下载总字节 |
| `ClearCacheFilesAsync()` | 清理冗余缓存 |

---

## 日常开发步骤

```
1. 在 GameScripts/HotFix/ 下修改/添加代码
2. Editor 模式：直接 Play（热更 DLL 编译进 Assets）
3. 模拟热更：HybridCLR → Generate All → 打资源包 → 拷贝 DLL
4. 真机测试：出包 → 部署热更资源到 CDN → 启动触发热更
```

### 新功能开发

```
1. IEvent/ 定义事件接口（跨模块通信时）
2. UI/ 创建 UIWindow 子类（[Window] 特性）
3. System/ 实现业务系统
4. GameApp_RegisterSystem.cs 注册新系统
5. Procedure 中连接 UI 打开与系统初始化
```
