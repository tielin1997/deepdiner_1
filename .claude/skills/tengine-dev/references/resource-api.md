# 资源加载核心 API

## 核心原则

1. **禁止 `Resources.Load()`**：所有资源通过 YooAsset 加载，放在 `Assets/AssetRaw/` 下
2. **Sprite 用 `SetSprite` 扩展方法**：内置缓存池管理，无需手动释放
3. **GameObject 用 `LoadGameObjectAsync`**：自动管理引用计数，Destroy 时自动卸载
4. **其他 Asset 加载/释放必须配对**：`LoadAssetAsync<T>` → 用完后 → `UnloadAsset`
5. **异步优先**：禁止同步加载大资源

禁止模式详见 [naming-rules.md](naming-rules.md#禁止的代码模式)。

---

## 资源寻址

YooAsset 通过 **location**（文件名，不含路径和扩展名）寻址：

```
Assets/AssetRaw/UI/Prefabs/BattleMainUI.prefab  →  location：BattleMainUI
Assets/AssetRaw/Audios/BGM/MainTheme.mp3        →  location：MainTheme
```

同名文件可使用相对路径去重：`UI/BattleMainUI`

---

## 加载方式选择

| 资源类型 | 推荐 API | 需要手动释放 |
|---------|---------|-------------|
| Sprite / 图集子图 | `SetSprite` / `SetSubSprite` | 否 |
| 需实例化的 Prefab | `LoadGameObjectAsync` | 否（Destroy 自动）|
| TextAsset / SO 等 | `LoadAssetAsync<T>` | **是** |

---

## Sprite 加载（SetSprite，推荐）

```csharp
_imgIcon.SetSprite("item_icon_001");                                    // 基础
_imgIcon.SetSprite("item_icon_001", setNativeSize: true);               // 自适应尺寸
_imgIcon.SetSprite("item_icon_001", cancellationToken: _cts.Token);    // 取消支持
_spriteRenderer.SetSprite("hero_sprite");                               // SpriteRenderer
_imgIcon.SetSubSprite("ItemAtlas", "item_sword_01");                    // 图集子图
```

禁止用 `LoadAssetAsync<Sprite>` 加载图片（无缓存池且需手动释放）。

---

## GameObject 加载（自动引用计数）

```csharp
// 异步（推荐）
var go = await GameModule.Resource.LoadGameObjectAsync("HeroPrefab", parent);

// 同步（需资源已预加载）
var go = GameModule.Resource.LoadGameObject("HeroPrefab", parent);

// 回收：直接 Destroy
Destroy(go);  // 框架自动归还引用计数
```

禁止 `LoadAssetAsync<GameObject>` + `Instantiate` 组合（需手动追踪和 UnloadAsset）。

---

## 其他 Asset 加载（必须手动释放）

```csharp
// 异步加载
var config = await GameModule.Resource.LoadAssetAsync<TextAsset>("level_data");
// 用完后释放
GameModule.Resource.UnloadAsset(config);
```

---

## 资源卸载

```csharp
GameModule.Resource.UnloadAsset(asset);                          // 引用计数-1
GameModule.Resource.UnloadUnusedAssets();                        // 卸载引用计数为0的资源
GameModule.Resource.ForceUnloadUnusedAssets(gcCollection: true); // 场景切换后强制整理
GameModule.Resource.ForceUnloadAllAssets();                      // 退出游戏时
```

---

## 资源信息查询

```csharp
bool valid = GameModule.Resource.CheckLocationValid("location");   // 校验地址
HasAssetResult result = GameModule.Resource.HasAsset("location");  // Valid/NotExist/AssetOnline/AssetOnDisk
AssetInfo info = GameModule.Resource.GetAssetInfo("location");     // 单个资源信息
AssetInfo[] infos = GameModule.Resource.GetAssetInfos("tag");      // 按标签批量获取
```

---

## 生命周期管理模式

### 自动管理（无需手动释放）

```csharp
// Sprite：SetSprite 内置缓存池
_imgIcon.SetSprite("item_icon_001");
_imgIcon.SetSubSprite("ItemAtlas", "item_sword_01");

// GameObject：LoadGameObjectAsync 自动引用计数
var go = await GameModule.Resource.LoadGameObjectAsync("HeroPrefab", parent);
Destroy(go);  // 框架自动归还

// ❌ 禁止 LoadAssetAsync<Sprite> 或 LoadAssetAsync<GameObject> + Instantiate
```

### 手动管理（必须配对释放）

```csharp
private TextAsset _configData;

protected override async void OnRefresh()
{
    _configData = await GameModule.Resource.LoadAssetAsync<TextAsset>("level_data");
}

protected override void OnDestroy()
{
    if (_configData != null) { GameModule.Resource.UnloadAsset(_configData); _configData = null; }
}
```

### CancellationToken 取消加载

```csharp
private CancellationTokenSource _cts = new();

private async UniTaskVoid LoadAsync()
{
    try { var asset = await GameModule.Resource.LoadAssetAsync<Sprite>("icon", _cts.Token); }
    catch (OperationCanceledException) { /* 正常取消 */ }
}

protected override void OnDestroy() { _cts.Cancel(); _cts.Dispose(); }
```

### 并发与批量加载

```csharp
// 多类型并发
var (sprite, prefab, config) = await UniTask.WhenAll(
    GameModule.Resource.LoadAssetAsync<Sprite>("hero_icon"),
    GameModule.Resource.LoadGameObjectAsync("HeroModel"),
    GameModule.Resource.LoadAssetAsync<TextAsset>("hero_config")
);

// 同类型批量
var sprites = await UniTask.WhenAll(
    locations.Select(loc => GameModule.Resource.LoadAssetAsync<Sprite>(loc)));
```

### 场景切换资源整理

```csharp
await GameModule.Scene.LoadSceneAsync("BattleScene");
GameModule.Resource.UnloadUnusedAssets();  // 整理未使用资源
GameModule.Resource.ForceUnloadUnusedAssets(gcCollection: true);  // 强制整理+GC
```
