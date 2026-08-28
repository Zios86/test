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

Do not treat chat history as the only place where an important decision exists.

## Current handoff status
- Current published release: **0.9 Guest Secretary Bot**.
- Tag: `v0.9-guest-secretary-bot`.
- Artifact: `DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe`.
- Size: `627,722,376 bytes`.
- SHA-256: `3e57b7c1fac965a14518d6eecc86642bcd3367af1fcf66af01e71142c09aef22`.
- PR Windows CI `33150603611` passed through packaged self-test.
- Production Windows CI `33150927129` passed through Release publication.
- Published rollback: **0.8 Visual Refresh**.

## 0.9 decision that must survive AI handoff
Normal DION user flow:

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
- browser speaker state is not used to relabel delayed Whisper chunks without timing calibration;
- STT audio remains WASAPI in 0.9.

## Start protocol
Claude: read `CLAUDE.md` -> `docs/PROJECT_MAP.md` -> matching design doc/Skill.  
OpenAI/Codex: read `AGENTS.md` -> `docs/PROJECT_MAP.md` -> matching design/development doc.  
ChatGPT Work/Projects: keep repository/docs as source of truth; do not maintain a second architecture/roadmap copy.

## Next field evidence
Any AI continuing after 0.9 should treat these as **unverified until user/target-environment evidence exists**:
- actual corporate guest join/waiting-room behavior;
- corporate DION DOM participant identifiers;
- explicit active-speaker data/ARIA semantics;
- browser speaker-event ↔ WASAPI/Whisper timing;
- long-duration WASAPI + microphone stability;
- 2/5/10 participant and overlap accuracy.

## Handoff checklist
Before switching AI/system, ensure the repository contains current:
- `PROJECT_MAP.md`;
- architecture/design docs;
- `CHANGELOG.md`;
- `ROADMAP.md` and `exec-plans/CURRENT.md`;
- `VERSION_JOURNAL.md` entry for every significant update;
- exact tests/build/release status;
- explicit unresolved limitations.

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

## Privacy
Do not copy real meeting links, transcripts, names, internal tickets, tokens, certificates or private keys into permanent documentation. Use synthetic examples.
