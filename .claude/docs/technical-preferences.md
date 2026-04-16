# Technical Preferences

## Engine & Language

- **Engine**: Unity 2022.3.62f3
- **Framework**: TEngine
- **Language**: C#
- **Rendering**: URP (Universal Render Pipeline)
- **Physics**: Unity Physics (built-in)

## Core Dependencies

- **TEngine**: Modular game framework (resource, UI, event, procedure modules)
- **YooAsset**: Resource management & asset bundle pipeline
- **HybridCLR**: C# hot-update for all platforms
- **UniTask**: Zero-GC async/await for Unity
- **Luban**: Game configuration table generation

## Input & Platform

- **Target Platforms**: [TO BE CONFIGURED]
- **Input Methods**: [TO BE CONFIGURED]
- **Primary Input**: [TO BE CONFIGURED]
- **Gamepad Support**: [TO BE CONFIGURED]
- **Touch Support**: [TO BE CONFIGURED]
- **Platform Notes**: [TO BE CONFIGURED]

## Naming Conventions (TEngine)

- **Classes**: PascalCase (e.g., `GameApp`, `UIPanelBase`)
- **Variables**: camelCase for private, PascalCase for public/serialized
- **Events**: PascalCase with `Event` suffix (e.g., `GameStartEvent`)
- **Files**: PascalCase matching class name
- **Scenes/Prefabs**: PascalCase (e.g., `GameLauncher`, `UIPanelLogin`)
- **Constants**: PascalCase or UPPER_SNAKE_CASE
- **UI Nodes**: Prefix convention (e.g., `btn_` for buttons, `img_` for images, `txt_` for text)
- **Assemblies**: GameBase, GameProto, GameLogic (under HotFix)

## Performance Budgets

- **Target Framerate**: [TO BE CONFIGURED]
- **Frame Budget**: [TO BE CONFIGURED]
- **Draw Calls**: [TO BE CONFIGURED]
- **Memory Ceiling**: [TO BE CONFIGURED]

## Testing

- **Framework**: Unity Test Framework (NUnit)
- **Minimum Coverage**: [TO BE CONFIGURED]
- **Required Tests**: Balance formulas, gameplay systems, TEngine module integration
- **CI**: `game-ci/unity-test-runner@v4` (GitHub Actions)

## Forbidden Patterns

- No synchronous resource loading — use `UniTask` / `LoadAssetAsync`
- No `MonoBehaviour` for game logic — use TEngine module system
- No direct `GameObject.Find` / `FindObjectOfType` — use module references
- No hardcoded gameplay values — use Luban config tables
- No `PlayerPrefs` for game state — use proper save system

## Allowed Libraries / Addons

- TEngine (core framework)
- YooAsset (resource management)
- HybridCLR (hot-update)
- UniTask (async)
- Luban (config generation)
- Sirenix Odin Inspector (editor tooling, paid)

## Architecture Decisions Log

- [ADR-001] TEngine adopted as base framework — Unity + HybridCLR + YooAsset + UniTask + Luban

## Engine Specialists

- **Primary**: unity-specialist
- **Language/Code Specialist**: unity-specialist (C#)
- **Shader Specialist**: unity-shader-specialist
- **UI Specialist**: unity-ui-specialist
- **Addressables Specialist**: (covered by YooAsset in TEngine)
- **Routing Notes**: Use `tengine-dev` skill for all TEngine-specific API questions

### File Extension Routing

| File Extension / Type | Specialist to Spawn |
| --------------------- | ------------------- |
| Game code (.cs) | unity-specialist + tengine-dev skill |
| Shader / material (.shader, .mat) | unity-shader-specialist |
| UI / screen files (.prefab, UGUI) | unity-ui-specialist + tengine-dev skill |
| Scene / prefab files (.unity, .prefab) | unity-specialist |
| Config tables (Luban .xml/.json) | luban-dev skill |
| General architecture review | Primary |
