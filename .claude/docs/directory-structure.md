# Directory Structure

```text
/
├── CLAUDE.md                    # Master configuration (Game Studio + TEngine workflow)
├── .claude/                     # Agent definitions, skills, hooks, rules, docs
│   ├── docs/                    # Game studio config docs (technical-preferences, etc.)
│   └── skills/                  # Skills including tengine-dev, luban-dev, game studio skills
├── Assets/                      # Unity project assets
│   ├── TEngine/                 # TEngine framework core (Runtime + Editor)
│   ├── GameScripts/             # All game C# code
│   │   ├── GameEntry.cs         # Unity entry point (non-hot-updated)
│   │   ├── Procedure/           # Startup procedure chain
│   │   └── HotFix/              # Hot-updated game logic assemblies
│   │       ├── GameBase/        # Base framework extensions [DLL]
│   │       ├── GameProto/       # Config/proto definitions [DLL]
│   │       └── GameLogic/       # Business logic + GameApp.cs entry [DLL]
│   ├── AssetArt/                # Art resources (editor-only, not bundled)
│   ├── AssetRaw/                # Hot-updatable resources
│   │   ├── UIRaw/               # UI images
│   │   ├── Audios/              # Audio assets
│   │   ├── Effects/             # VFX assets
│   │   └── Scenes/              # Scene assets
│   ├── Editor/                  # Unity editor scripts
│   ├── Scenes/                  # Main scenes
│   └── Launcher/                # Launcher scene & assets
├── Packages/                    # Unity package dependencies (UniTask, YooAsset)
├── ProjectSettings/             # Unity project settings
├── design/                      # Game design documents (gdd, narrative, levels, balance)
├── docs/                        # Technical documentation
│   ├── architecture/            # Architecture Decision Records (ADRs)
│   ├── engine-reference/        # Engine API snapshots (version-pinned)
│   │   └── unity/               # Unity version reference
│   └── tengine-books/           # TEngine module documentation
├── Configs/                     # Luban configuration source files
│   └── GameConfig/              # Game config definitions
├── Tools/                       # Build and pipeline tools
│   └── Luban/                   # Luban config generator
├── BuildCLI/                    # Build command-line tools
├── tests/                       # Test suites (unit, integration, performance)
├── production/                  # Production management (sprints, milestones)
│   ├── session-state/           # Ephemeral session state (active.md)
│   └── session-logs/            # Session audit trail
├── prototypes/                  # Throwaway prototypes (isolated)
└── TEngine/                     # TEngine source repository (reference only)
```
