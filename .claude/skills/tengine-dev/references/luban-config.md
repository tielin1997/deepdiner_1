# TEngine Luban 配置表指南

## 技术栈

- **Luban**：Excel/JSON/YAML → C# 代码 + 二进制数据
- **生成格式**：`cs-bin`（C#）+ `bin`（二进制）
- **数据位置**：`Assets/AssetRaw/Configs/bytes/`（YooAsset 管理）
- **代码位置**：`GameScripts/HotFix/GameProto/GameConfig/`（热更程序集）

---

## 配置工程结构

```
TEngine/Configs/GameConfig/
├── luban.conf                      # Luban 主配置
├── gen_code_bin_to_project.bat     # 生成脚本
├── Datas/                          # Excel 数据源
│   ├── __tables__.xlsx             # 表索引（注册所有表）
│   ├── __beans__.xlsx              # Bean 复合类型
│   ├── __enums__.xlsx              # 枚举类型
│   └── item.xlsx                   # 业务数据表
└── CustomTemplate/                 # 自定义模板（通常不改）
```

---

## 数据表定义

### __tables__.xlsx 表索引

| full_name | value_type | read_mode | comment |
|-----------|-----------|-----------|---------|
| cfg.TbItem | Item | map | 道具表 |
| cfg.TbLevel | Level | list | 关卡表 |

- `full_name`：`cfg.表名` → 生成 `Tables.TbXxx`
- `read_mode`：`map`（按 id 索引）/ `list`（列表）

### Excel 数据行结构

```
第1行：字段名（id, name, hp, atk, desc）
第2行：类型（int, string, int, int, string）
第3行：分组（c=客户端, s=服务端, cs=双端）
第4行：注释
第5行起：数据
```

---

## 代码生成

```bat
# 在 TEngine/Configs/GameConfig/ 目录下运行
gen_code_bin_to_project.bat
```

生成产物：
```
GameProto/GameConfig/   → Tables.cs, Item.cs, TbItem.cs, ...（自动生成，勿手改）
AssetRaw/Configs/bytes/ → item.bytes, skill.bytes, ...
```

---

## ConfigSystem 加载器

```csharp
// ConfigSystem.cs（GameProto/ 中，桥接 Luban 与 YooAsset）
public class ConfigSystem
{
    public static ConfigSystem Instance => _instance ??= new ConfigSystem();
    public Tables Tables { get { if (!_init) Load(); return _tables; } }

    public void Load()
    {
        _tables = new Tables(LoadByteBuf);
        _init = true;
    }

    private ByteBuf LoadByteBuf(string file)
    {
        var textAsset = GameModule.Resource.LoadAsset<TextAsset>(file);
        return new ByteBuf(textAsset.bytes);
    }
}
```

初始化时机：`ProcedurePreload` 预加载 PRELOAD 资源后，在 `GameApp.Entrance` 中调用 `ConfigSystem.Instance.Load()`。

---

## 配置数据访问

```csharp
var tables = ConfigSystem.Instance.Tables;

// 按 ID 查询（map 模式）
var itemCfg = tables.TbItem.Get(1001);

// 遍历所有行
foreach (var item in tables.TbItem.DataList) { }

// 条件查询
var rareSwords = tables.TbItem.DataList.Where(i => i.Type == ItemType.Sword).ToList();
```

### 配置管理器封装（推荐）

复杂模块封装配置管理器，不直接在业务代码散落 `ConfigSystem.Instance.Tables.TbXxx`：

```csharp
public class LevelConfigMgr
{
    public static LevelConfigMgr Instance => _instance ??= new LevelConfigMgr();
    public Level GetLevel(int id) => ConfigSystem.Instance.Tables.TbLevel.GetOrDefault(id);
    public List<Level> GetChapterLevels(int chId) => ConfigSystem.Instance.Tables.TbLevel.DataList.Where(l => l.ChapterId == chId).ToList();
}
```

---

## 类型支持

| Excel 类型 | C# 类型 | 示例 |
|-----------|---------|------|
| `int` | `int` | `100` |
| `long` | `long` | `1000000` |
| `float` | `float` | `1.5` |
| `bool` | `bool` | `true` |
| `string` | `string` | `"剑士"` |
| `int[]` | `int[]` | `1,2,3` |
| `vector3` | `UnityEngine.Vector3` | `1,2,3` |
| 自定义 Bean | C# 类 | `{id:1001,count:5}` |

---

## 添加新配置表

```
1. __tables__.xlsx 注册新表：full_name=cfg.TbNewTable, value_type=NewTableRow, read_mode=map
2. 创建 Datas/new_table.xlsx，添加列定义和数据
3. （如需）在 __beans__.xlsx 定义 Bean
4. 运行 gen_code_bin_to_project.bat
5. 验证：GameConfig/ 下新增文件，Configs/bytes/ 下新增 .bytes
6. 创建 Config/NewTableConfigMgr.cs 封装查询方法
```

**注意**：新增字段前向兼容（旧客户端忽略），删除字段不可前向兼容。
