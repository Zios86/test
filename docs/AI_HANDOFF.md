# AI handoff: Claude + ChatGPT Work + Codex

## Goal
Один проект должен одинаково продолжаться в разных ИИ без копирования всей истории чатов и без расхождения документации.

## Canonical model
Repository files are authoritative:

```text
README.md
AGENTS.md          -> OpenAI/Codex entry instructions
CLAUDE.md          -> Claude Code entry instructions
docs/              -> shared canonical project knowledge
.claude/skills/    -> repeatable Claude workflows
```

Do not treat a chat transcript as the only place where an important decision exists.

## Current handoff status

- Published release: **0.8 Visual Refresh**.
- Active development branch: `dion-guest-bot-0.9`.
- Development candidate: **0.9 Guest Secretary Bot**.
- 0.9 reconstructed source validation: **36/36 tests + compileall passed**.
- Windows PR CI / packaged EXE / production Release: not yet recorded as complete.

Any AI continuing 0.9 must not call it released until `docs/RELEASES.md` contains the actual uploaded artifact metadata.

## 0.9 decision that must survive AI handoff
The normal DION user flow is now:

```text
room URL /join/<slug>
  -> guest Secretary Bot
  -> visible Edge/Chrome guest session
  -> optional localhost DevTools automation/probe
```

Integration API is optional/advanced. `event_id` must not be reintroduced as a mandatory common-path input.

Key safety rules:
- corporate/on-prem DION hostnames are supported;
- browser automation failure falls back to manual guest entry;
- slug IAPI metadata is not live-presence proof;
- microphone enabled is not speaker evidence;
- browser speaker state is not yet used to relabel delayed Whisper chunks without timing calibration;
- STT audio remains WASAPI in 0.9.

## Claude Code start protocol
1. Open repository/branch.
2. Read `CLAUDE.md`.
3. Read `docs/PROJECT_MAP.md`.
4. For 0.9 guest/browser work read `docs/design-docs/DION_INTEGRATION.md` and `PRIVACY_SECURITY.md`.
5. If a matching Skill exists, use it.
6. Load only mapped files needed for the task.

## OpenAI Codex start protocol
1. Open repository/branch.
2. Read root `AGENTS.md`.
3. Read `docs/PROJECT_MAP.md`.
4. Follow only relevant design/development links.
5. Run validation commands from `DEVELOPMENT.md` after changes.

## ChatGPT Work / Projects protocol
When using ChatGPT Work/Projects:
- keep repository/docs as source of truth;
- project instructions should point to `AGENTS.md` + `docs/PROJECT_MAP.md`;
- do not maintain a second architecture/roadmap copy in Project instructions;
- use Work for reports/plans/docs and repository tooling for code/tests/releases.

Suggested project instruction:

```text
DION Meeting Assistant is maintained from repository documentation.
Treat docs/ as canonical. Read AGENTS.md and docs/PROJECT_MAP.md first.
For significant changes update docs/VERSION_JOURNAL.md and affected docs.
Do not treat unreleased 0.9 work as a published Release until RELEASES.md has actual artifact metadata.
```

## Skill portability
`.claude/skills/` contains procedural playbooks, not duplicate project facts. Facts stay under `docs/` so the same source works for Claude, ChatGPT/Codex and humans.

## Handoff at the end of a task
Before switching AI/system, ensure repository contains:
- current `PROJECT_MAP.md`;
- architecture/design docs matching code;
- `CHANGELOG.md` user-visible changes;
- `ROADMAP.md` and `exec-plans/CURRENT.md` status;
- `VERSION_JOURNAL.md` entry for every significant update;
- exact tests/build status;
- explicit unresolved limitations;
- release metadata only when actually published.

For 0.9 specifically record whether each of these is still pending or complete:
- Windows PR CI;
- Qt guest-flow smoke;
- packaged EXE self-test;
- merge;
- production CI;
- GitHub Release;
- real corporate DION guest-form/DOM field check.

## What belongs where
| Information | File |
|---|---|
| Permanent architecture | `ARCHITECTURE.md` / `design-docs/*` |
| Where a feature lives | `PROJECT_MAP.md` |
| Build/test commands | `DEVELOPMENT.md` |
| Current next steps | `ROADMAP.md`, `exec-plans/CURRENT.md` |
| User-visible history | `CHANGELOG.md` |
| Published artifacts | `RELEASES.md` |
| Chronological engineering work | `VERSION_JOURNAL.md` |
| Claude workflow instructions | `CLAUDE.md`, `.claude/skills/*` |
| Codex workflow instructions | `AGENTS.md` |
| Temporary brainstorming | chat until promoted into docs |

## Decision promotion rule
Once a chat discussion changes how the project should work, promote the stable result into repository documentation before ending the task.

0.9 example:

```text
Chat: "В комнату можно зайти как гость по обычной ссылке"
    ↓
Implementation/tests
    ↓
DION_INTEGRATION.md
ARCHITECTURE.md
PROJECT_MAP.md
CHANGELOG.md
VERSION_JOURNAL.md
ROADMAP/CURRENT
```

## Privacy
Do not copy real meeting links, transcripts, names, internal tickets, tokens, certificates or private keys into permanent documentation. Use synthetic examples.
