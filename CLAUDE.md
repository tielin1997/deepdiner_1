# DeepDiner -- Unity + TEngine Game Project

请使用中文写提案和回答。

Indie game development managed through Claude Code subagents with TEngine framework.

## Technology Stack

- **Engine**: Unity 2022.3.62f3
- **Framework**: TEngine (HybridCLR + YooAsset + UniTask + Luban)
- **Language**: C#
- **Version Control**: Git with trunk-based development
- **Build System**: Unity Build Pipeline + HybridCLR hot-update
- **Asset Pipeline**: YooAsset

## Project Structure

@.claude/docs/directory-structure.md

## Engine Version Reference

@docs/engine-reference/unity/VERSION.md

## Technical Preferences

@.claude/docs/technical-preferences.md

## Coordination Rules

@.claude/docs/coordination-rules.md

## Collaboration Protocol

**User-driven collaboration, not autonomous execution.**
Every task follows: **Question -> Options -> Decision -> Draft -> Approval**

- Agents MUST ask "May I write this to [filepath]?" before using Write/Edit tools
- Agents MUST show drafts or summaries before requesting approval
- Multi-file changes require explicit approval for the full changeset
- No commits without user instruction

See `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md` for full protocol and examples.

## Coding Standards

@.claude/docs/coding-standards.md

## Context Management

@.claude/docs/context-management.md

---

## TEngine Workflow (Mandatory)

> **All coding tasks MUST follow this workflow.**

### Step 0: Task Level Classification

| Level | Criteria | Knowledge Query |
|-------|----------|----------------|
| **L1 Simple** | typo fix, comment, log output, variable rename | Skip query, code directly |
| **L2 API Call** | Known API call, single-module local change | Trigger `tengine-dev` skill (topic only) |
| **L3 Feature** | New feature, cross-file changes, new UI/resource/event | Trigger `tengine-dev` skill (full related topics) |
| **L4 Architecture** | Module design, system refactor, multi-module coordination | Trigger `tengine-dev` skill (parallel topics) |

> **When in doubt, upgrade one level.**

### Step 1: Query TEngine Specs (L2-L4 only)

**Knowledge source**: `.claude/skills/tengine-dev/references/` (authoritative AI docs)

```
Use Skill tool, skill = "tengine-dev"
Describe the technical question or feature needed
```

**Session caching**: Re-use already-queried specs within the same session. Only re-trigger for new topics.

**Trigger scenarios**:

| Scenario | Required Reference |
|----------|-------------------|
| UI development | ui-lifecycle.md |
| Resource loading | resource-api.md |
| Hot-update code | hotfix-workflow.md |
| Event system | event-system.md |
| Module usage | modules.md |
| Luban config | luban-config.md |
| Code conventions | naming-rules.md |

### Step 2: Implement

When `references` specs conflict with actual code API:
1. Trust the actual code implementation
2. Annotate the conflict in output

## Core Coding Principles (Red Lines)

1. **Async-first**: Use `UniTask` for IO operations. No synchronous loading or Coroutines
2. **Module access**: Use `GameModule.XXX`, not `ModuleSystem.GetModule<T>()`
3. **Release resources**: `LoadAssetAsync` pairs with `UnloadAsset`. Use `LoadGameObjectAsync` for GameObjects
4. **Hot-update boundary**: `GameScripts/Main` = never hot-updated. `GameScripts/HotFix/` = all hot-updated
5. **Event decoupling**: `GameEvent` between modules, `AddUIEvent` inside UI

## TEngine Reference Docs

> **Authoritative source**: `.claude/skills/tengine-dev/references/`

| Document | Content | Level |
|----------|---------|-------|
| architecture.md | Project structure / startup flow | Core |
| modules.md | Module API (Timer/Scene/Audio/Fsm) | Core |
| ui-lifecycle.md | UI development (lifecycle/layers/properties) | Core |
| event-system.md | Event system (two modes/core interfaces) | Core |
| resource-api.md | Resource loading/unloading | Core |
| hotfix-workflow.md | Hot-update code (HybridCLR/assembly split) | Core |
| luban-config.md | Config tables | Core |
| naming-rules.md | Code conventions / naming / node prefixes | Core |
| ui-patterns.md | UI advanced (Widget templates/node binding) | Advanced |
| troubleshooting.md | Issue resolution | Troubleshooting |
