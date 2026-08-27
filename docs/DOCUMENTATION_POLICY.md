# Documentation maintenance policy

## Rule

Documentation is part of the implementation. A code/build/configuration change is **not complete** until the affected canonical documents are updated in the same task.

This policy applies to humans, Claude, ChatGPT, Codex and any other agent working on the project.

## Source-of-truth hierarchy

1. Current code/build configuration and tests.
2. Canonical files under `docs/` and root `CHANGELOG.md`.
3. `AGENTS.md`, `CLAUDE.md`, `.claude/skills/*` as thin guidance layers.
4. Release notes and old chat history as historical/supporting context.

If levels 1 and 2 disagree, fix the documentation before finishing the task.

## Mandatory update matrix

| Change type | Required documentation |
|---|---|
| User-visible feature/fix | `CHANGELOG.md` |
| File/module/class responsibility | `PROJECT_MAP.md` |
| New entry point or key function | `PROJECT_MAP.md` |
| Data flow/threading/runtime behavior | `ARCHITECTURE.md` and relevant design doc |
| Audio/WASAPI/native stability | `design-docs/AUDIO_STABILITY.md` |
| Whisper/VAD/context/dictionary/model | `design-docs/SPEECH_RECOGNITION.md` |
| Privacy, network, storage, diagnostics | `design-docs/PRIVACY_SECURITY.md` |
| Dependency/test/build/CI | `DEVELOPMENT.md` |
| Published release or artifact | `RELEASES.md`, `ROADMAP.md`, `CHANGELOG.md` |
| Planned work/priority changed | `ROADMAP.md`, `exec-plans/CURRENT.md` |
| New recurring workflow | relevant `SKILL.md` or new Skill |
| AI workflow/navigation rule | `AGENTS.md` and/or `CLAUDE.md`, but keep details canonical in `docs/` |

## Before editing

- Read `PROJECT_MAP.md`.
- Identify which documentation rows above will be affected.
- Do not load unrelated docs/code into context.

## While editing

Update docs as soon as an architectural fact becomes stable. Do not postpone all documentation until a later chat.

Keep facts concise and verifiable:

- exact file names;
- exact commands;
- current defaults;
- known limitations;
- status: implemented / experimental / planned.

Do not document an intended behavior as implemented until code/tests/build support it.

## Before finishing

Use this checklist:

- [ ] Tests/checks executed where possible.
- [ ] `PROJECT_MAP.md` still points to the correct modules/functions.
- [ ] Architecture/design docs describe the actual implementation.
- [ ] `CHANGELOG.md` includes user-visible changes.
- [ ] `ROADMAP.md` reflects what is now completed/next.
- [ ] `exec-plans/CURRENT.md` has no stale completed step masquerading as current work.
- [ ] Release metadata/SHA is updated if a release changed.
- [ ] AI-specific files remain short and do not duplicate the whole knowledge base.
- [ ] No sensitive meeting content was added to docs/examples.

## Markdown style

- One clear purpose per file.
- Use descriptive headings and short sections.
- Prefer tables for routing/matrices.
- Prefer relative repository paths in code formatting.
- Link/pointer to deeper docs instead of copying large sections.
- Keep volatile details (versions/releases/status) in dedicated files so architecture docs remain stable.
- Mark planned work explicitly as **Planned**.
- Mark experimental behavior explicitly as **Experimental**.

## Skills style

For `.claude/skills/<name>/SKILL.md`:

- YAML frontmatter must contain `name` and `description`;
- `name` uses lowercase letters/numbers/hyphens;
- description says both **what the skill does** and **when to use it**;
- keep the main skill procedural and focused;
- avoid stuffing all project knowledge into the Skill;
- point to canonical project docs for facts;
- supporting details should be loaded only when needed.

## Anti-drift rule for multiple AI systems

Never maintain separate Claude and ChatGPT versions of architecture, roadmap or project map.

The shared model is:

```text
                  docs/  (canonical)
                 /    \
          CLAUDE.md   AGENTS.md
             |            |
        Claude Code   Codex/agents
             |
      .claude/skills/
```

If ChatGPT Work/Projects are used, their project instructions should point back to the repository `docs/` rather than becoming another independent copy of the project manual.

## Review cadence

Besides per-change updates, review the documentation structure at each release:

- remove stale instructions;
- merge duplicates;
- verify map paths;
- ensure active plan matches roadmap;
- ensure released version and SHA metadata are correct.
