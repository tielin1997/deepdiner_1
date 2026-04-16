# MCP 材质与视觉操作

## manage_material：材质管理

| action | 说明 | 关键参数 |
|--------|------|---------|
| `create` | 创建材质 | `materialName`, `shaderName`, `savePath` |
| `set_material_color` | 设置颜色 | `materialPath`, `colorProperty`, `r/g/b/a` |
| `set_material_shader_property` | 设置 Shader 属性 | `materialPath`, `propertyName`, `propertyType`, `value` |
| `assign_material_to_renderer` | 赋给渲染器 | `target`, `materialPath`, `materialIndex` |
| `set_renderer_color` | 快捷设置渲染器颜色 | `target`, `r/g/b/a` |
| `get_material_info` | 获取材质信息 | `materialPath` |

```json
{ "tool": "manage_material", "params": {
  "action": "create", "materialName": "EnemyMat",
  "shaderName": "Universal Render Pipeline/Lit",
  "savePath": "Assets/AssetRaw/Materials/EnemyMat.mat"
} }
```

常用 Shader：`Universal Render Pipeline/Lit`、`Universal Render Pipeline/Unlit`、`Standard`、`Sprites/Default`、`UI/Default`

常用颜色属性：`_BaseColor`（URP）、`_Color`（标准）、`_EmissionColor`（自发光）

`propertyType`：`float`、`int`、`color`、`vector`、`texture`

## manage_shader：Shader 文件

| action | 说明 | 关键参数 |
|--------|------|---------|
| `create` | 创建 Shader | `name`, `path`, `contents` |
| `delete` | 删除 Shader | `name`, `path` |

## manage_texture：纹理导入设置

```json
{ "tool": "manage_texture", "params": {
  "action": "set_import_settings",
  "path": "Assets/AssetRaw/Textures/Icon.png",
  "maxSize": 512, "format": "RGBA32",
  "generateMipMaps": false, "textureType": "Sprite"
} }
```

`textureType`：`Default`、`Sprite`、`NormalMap`、`GUI`、`Cubemap`

## manage_vfx：粒子与特效

### ParticleSystem

| action | 说明 | 关键参数 |
|--------|------|---------|
| `particle_create` | 创建粒子系统 | `target`, `autoAssignMaterial` |
| `particle_set_main` | 主模块 | `duration`, `looping`, `startLifetime`, `startSpeed`, `startSize`, `startColor`, `maxParticles`, `simulationSpace` |
| `particle_set_emission` | 发射模块 | `rateOverTime`, `rateOverDistance` |
| `particle_add_burst` | 爆发 | `time`, `count`, `cycles` |
| `particle_set_shape` | 形状 | `shapeType`（Sphere/Cone/Box/Mesh）, `radius`, `arc` |
| `particle_play/stop/clear` | 播放控制 | `target` |

```json
{ "tool": "manage_vfx", "params": {
  "action": "particle_create", "target": "HitEffect", "autoAssignMaterial": true
} }
```

### LineRenderer

```json
{ "tool": "manage_vfx", "params": {
  "action": "line_create", "target": "TrailLine",
  "positions": [{ "x": 0, "y": 0, "z": 0 }, { "x": 5, "y": 2, "z": 0 }],
  "startWidth": 0.1, "endWidth": 0.05
} }
```

## manage_animation：动画控制器

| action | 说明 | 关键参数 |
|--------|------|---------|
| `create_controller` | 创建 AnimatorController | `controllerPath` |
| `add_parameter` | 添加参数 | `controllerPath`, `parameterName`, `parameterType`（Float/Int/Bool/Trigger）, `defaultValue` |
| `add_state` | 添加状态 | `controllerPath`, `stateName`, `clipPath`, `isDefault` |
| `add_transition` | 添加过渡 | `controllerPath`, `fromState`, `toState`, `hasExitTime`, `conditions` |
| `create_clip` | 创建 Clip | `clipPath`, `frameRate`, `isLooping` |
| `create_blend_tree` | 创建混合树 | `controllerPath`, `stateName`, `blendType`（1D/2D）, `blendParameter`, `motions` |
| `set_parameter` | 运行时设参数 | `target`, `parameterName`, `value` |

```json
{ "tool": "manage_animation", "params": {
  "action": "add_transition",
  "controllerPath": "Assets/AssetRaw/Animations/Player.controller",
  "fromState": "Idle", "toState": "Run", "hasExitTime": false,
  "conditions": [{ "parameter": "Speed", "mode": "Greater", "threshold": 0.1 }]
} }
```
