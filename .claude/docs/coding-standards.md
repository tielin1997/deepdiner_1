# Coding Standards

## TEngine Core Principles

1. **Async-first**: Use `UniTask` for all IO operations. No synchronous loading or Coroutines
2. **Module access**: Use `GameModule.XXX`, not `ModuleSystem.GetModule<T>()`
3. **Release resources**: `LoadAssetAsync` pairs with `UnloadAsset`. Use `LoadGameObjectAsync` for GameObjects
4. **Hot-update boundary**: `GameScripts/Main` = never hot-updated. `GameScripts/HotFix/` = all hot-updated
5. **Event decoupling**: `GameEvent` between modules, `AddUIEvent` inside UI

## Code Organization

- Game code lives in `Assets/GameScripts/`
- **Main** assembly: `GameEntry.cs` (launcher), `Procedure/` (startup procedures) — NOT hot-updated
- **HotFix/GameBase**: Base framework extensions [DLL]
- **HotFix/GameProto**: Config/proto definitions [DLL]
- **HotFix/GameLogic**: Business logic, `GameApp.cs` entry point [DLL]
- All new game features go into `HotFix/GameLogic/`

## C# Conventions

- All public APIs require XML doc comments (`/// <summary>`)
- Gameplay values must be **data-driven** (Luban config tables), never hardcoded
- Use dependency injection via TEngine module system over singletons
- Every new system needs a corresponding ADR in `docs/architecture/`
- Commits must reference the relevant design document or task ID
- Use `PascalCase` for all types and public members
- Use `camelCase` for private fields and local variables
- Use `_camelCase` for private instance fields

## UI Development

- Use TEngine's `UIWindow` / `UIWidget` pattern, NOT raw MonoBehaviour
- UI scripts auto-generated via TEngine code generation tool
- Node naming prefix convention: `btn_`, `img_`, `txt_`, `go_`
- Event binding via `AddUIEvent` in UI lifecycle methods
- Full-screen panels managed by `UIModule`

## Resource Management

- Load via `GameModule.Resource.LoadAssetAsync<T>()`
- Always pair load with unload — use `AssetReference` for lifecycle management
- Hot-updatable assets go in `Assets/AssetRaw/`
- Editor-only assets go in `Assets/AssetArt/`

## Design Document Standards

- All design docs use Markdown
- Each mechanic has a dedicated document in `design/gdd/`
- Documents must include these 8 required sections:
  1. **Overview** -- one-paragraph summary
  2. **Player Fantasy** -- intended feeling and experience
  3. **Detailed Rules** -- unambiguous mechanics
  4. **Formulas** -- all math defined with variables
  5. **Edge Cases** -- unusual situations handled
  6. **Dependencies** -- other systems listed
  7. **Tuning Knobs** -- configurable values identified
  8. **Acceptance Criteria** -- testable success conditions
- Balance values must link to their source formula or rationale

## Testing Standards

### Test Evidence by Story Type

| Story Type | Required Evidence | Location | Gate Level |
| --- | --- | --- | --- |
| **Logic** (formulas, AI, state machines) | Automated unit test | `tests/unit/[system]/` | BLOCKING |
| **Integration** (multi-system) | Integration test OR documented playtest | `tests/integration/[system]/` | BLOCKING |
| **Visual/Feel** (animation, VFX, feel) | Screenshot + lead sign-off | `production/qa/evidence/` | ADVISORY |
| **UI** (menus, HUD, screens) | Manual walkthrough doc OR interaction test | `production/qa/evidence/` | ADVISORY |
| **Config/Data** (balance tuning) | Smoke check pass | `production/qa/smoke-[date].md` | ADVISORY |

### Automated Test Rules

- **Framework**: Unity Test Framework (NUnit)
- **Naming**: `[system]_[feature]_test.cs` for files; `Test_[Scenario]_[Expected]` for methods
- **Determinism**: Tests must produce the same result every run
- **Isolation**: Each test sets up and tears down its own state
- **No hardcoded data**: Test fixtures use constant files or factory functions

## CI/CD Rules

- Automated test suite runs on every push to main and every PR
- No merge if tests fail
- Unity CI: `game-ci/unity-test-runner@v4` (GitHub Actions)
- Build pipeline: Unity Build + HybridCLR hot-update DLLs + YooAsset bundles
