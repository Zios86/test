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

## Claude Code start protocol

1. Open repository/branch.
2. Claude automatically receives `CLAUDE.md` in supported Claude Code workflows.
3. Read `docs/PROJECT_MAP.md`.
4. If a matching project Skill exists, use it.
5. Load only the referenced design/development files needed for the task.

## OpenAI Codex start protocol

1. Open the repository/branch.
2. Read root `AGENTS.md`.
3. Read `docs/PROJECT_MAP.md`.
4. Follow links only to the relevant design/development document.
5. Run the validation commands listed in `AGENTS.md`/`DEVELOPMENT.md` after changes.

## ChatGPT Work / Projects protocol

OpenAI currently separates Work and Codex by use case: Work is appropriate for long multi-step research/artifact work, while Codex is dedicated to software-development work.

When using ChatGPT Work or a ChatGPT Project for this repository:

- keep the repository/docs as the source of truth;
- add the current repo documentation as project context when practical;
- project instructions should say to read `AGENTS.md` and `docs/PROJECT_MAP.md` first;
- do not paste and maintain a second copy of architecture/roadmap in Project instructions;
- use Work for reports, plans, documentation artifacts and cross-tool workflows;
- use Codex/repository tools for source changes, tests and releases.

Suggested Project instruction:

```text
DION Meeting Assistant is maintained from its repository documentation.
Treat docs/ as the canonical knowledge base.
Before project work, read AGENTS.md and docs/PROJECT_MAP.md, then only the relevant design doc.
For every change, update documentation according to docs/DOCUMENTATION_POLICY.md.
Do not rely on chat memory when it conflicts with repository documentation.
```

## Skill portability

The project stores Claude Code project skills under `.claude/skills/` because that is Claude's project discovery location.

The `SKILL.md` files themselves are intentionally written as portable procedural playbooks: name/description, required context, steps and final checks. They can be adapted/uploaded to ChatGPT Skills when needed without rewriting project facts, because the factual source remains `docs/`.

Do not make a separate `SKILL.md` containing a duplicate architecture snapshot for each AI product.

## Handoff at the end of a task

Before moving from one AI to another, ensure the repository contains:

- current `PROJECT_MAP.md`;
- current architecture/design docs;
- `CHANGELOG.md` entry for user-visible changes;
- current `ROADMAP.md`;
- updated `exec-plans/CURRENT.md` if work remains;
- exact test/build status in the active plan or commit/release notes;
- unresolved issue/limitation written explicitly.

Then the next AI should be able to continue from repository files alone.

## What belongs where

| Information | File |
|---|---|
| Permanent architecture | `ARCHITECTURE.md` / `design-docs/*` |
| Where a feature lives | `PROJECT_MAP.md` |
| Build/test commands | `DEVELOPMENT.md` |
| Current next steps | `ROADMAP.md`, `exec-plans/CURRENT.md` |
| User-visible history | `CHANGELOG.md` |
| Release artifacts | `RELEASES.md` |
| Claude workflow instructions | `CLAUDE.md`, `.claude/skills/*` |
| Codex workflow instructions | `AGENTS.md` |
| Temporary brainstorming | chat, until a decision is made |

## Decision promotion rule

Once a chat discussion changes how the project should work, promote the stable result into the appropriate repository document **before ending the task**.

Example:

```text
Chat: "Whisper base is too inaccurate; use small."
    ↓
Implementation/test
    ↓
design-docs/SPEECH_RECOGNITION.md
CHANGELOG.md
RELEASES.md (when published)
```

## Privacy

Do not copy real meeting transcripts, names, internal tickets or sensitive corporate content into permanent project documentation unless explicitly required and sanitized. Use synthetic examples.
