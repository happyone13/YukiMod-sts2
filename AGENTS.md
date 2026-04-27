# YukiMod Workspace Rules

## Project Identity

- This repository was copied from `MeiLinMod-sts2` and is now the working project for the character `友纪 / Yuki`.
- The sibling reference workspace for build and engine adaptation is `E:\DATA\GODOT\MyMod\MeiLinMod-sts2`; when checking version bumps or build-script behavior, compare against `MeiLinMod.csproj` there before assuming YukiMod should diverge.
- Treat the current codebase as a playable template, not finished canon. Existing placeholder data must not be mistaken for final character design.
- Keep the code-facing project id, namespace, and manifest id as `YukiMod` unless the user explicitly requests a broad rename.

## Required Read Order

- Read `docs/project-overview.zh-CN.md` at the start of work to re-establish project context.
- Read `docs/ai-workflow.zh-CN.md` before planning or implementing changes.
- Read `docs/character-brief.zh-CN.md` before touching character identity, mechanics, localization, or presentation.
- Read `docs/template-cleanup-checklist.zh-CN.md` when the task involves placeholder cleanup, naming migration, or release readiness.

## Working Rules

- Inspect the relevant code and docs before editing. Do not assume MeiLin behavior still applies unchanged in YukiMod.
- Current-version base-game unpacked reference is `E:\DATA\GODOT\MyMod\sts104`. When checking vanilla card, power, relic, action, or hook behavior for version `104`, inspect that workspace before reimplementing by guesswork.
- Keep three workstreams separate in reasoning and implementation: template cleanup, new Yuki content, and engine/build fixes.
- Do not silently rename placeholder `meilin` assets or paths across the repo unless the task explicitly includes migration work.
- Use UTF-8 for newly created or edited text files whenever possible.
- When adding content, prefer a minimal closed loop: registration, localization, resource pathing, and any required docs updates in the same task.
- When a task changes project rules, confirmed character setting, or technical conventions, update the corresponding docs in the same turn.
- If build, export, or runtime verification cannot be completed, say so explicitly instead of implying success.
- For code or content tasks, treat completion as requiring a fresh `YukiMod.pck` export when the environment allows it. If the automatic export on build does not produce the pack, run a manual Godot headless export and report the result.
- Runtime log path for this workspace is `C:\Users\lozalia\AppData\Roaming\SlayTheSpire2\logs`. Check the newest `godot.log` or timestamped `godot*.log` there when investigating current runtime failures.
