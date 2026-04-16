# Unity Engine — Version Reference

| Field | Value |
| ----- | ----- |
| **Engine Version** | Unity 2022.3.62f3 |
| **Scripting Backend** | IL2CPP + HybridCLR |
| **API Compatibility** | .NET Framework 4.7.1 |
| **Framework** | TEngine (HybridCLR + YooAsset + UniTask + Luban) |
| **Project Pinned** | 2026-04-16 |
| **Last Docs Verified** | 2026-04-16 |
| **LLM Knowledge Cutoff** | May 2025 |

## Framework Stack

| Component | Purpose |
| --------- | ------- |
| **TEngine** | Modular game framework (resource, UI, event, procedure) |
| **YooAsset** | Resource/AssetBundle management |
| **HybridCLR** | C# hot-update for all platforms |
| **UniTask** | Zero-GC async/await for Unity |
| **Luban** | Config table code generation |

## Key Unity APIs Used by TEngine

- `AssetBundle` — wrapped by YooAsset
- `SceneManager` — wrapped by TEngine SceneModule
- `Object.Instantiate` / `Object.Destroy` — wrapped by TEngine ObjectPool
- `Canvas` / `RectTransform` — UGUI, wrapped by TEngine UIModule
- `ScriptableObject` — TEngine settings

## Build Targets

- **Standalone**: Windows (D3D11)
- **Mobile**: Android, iOS (via HybridCLR hot-update)

## Important Notes

- TEngine targets Unity 2022.3.x LTS — do NOT use Unity 6+ APIs
- UGUI is the UI system (not UI Toolkit)
- All resource loading goes through YooAsset, not Addressables
- Hot-update code lives in `Assets/GameScripts/HotFix/`

## Verified Sources

- TEngine repo: https://github.com/ALEXTANGXIAO/TEngine
- TEngine docs: `docs/tengine-books/` (local)
- Unity 2022.3 docs: https://docs.unity3d.com/2022.3/Documentation/Manual/
- YooAsset: https://github.com/tuyoogame/YooAsset
- HybridCLR: https://github.com/focus-creative-games/hybridclr
- UniTask: https://github.com/Cysharp/UniTask
