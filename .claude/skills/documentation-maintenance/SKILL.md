---
name: documentation-maintenance
description: Keep DION Meeting Assistant project documentation and version/update journal synchronized with code, build, release, and architecture changes. Use whenever you modify project behavior, files, dependencies, tests, workflows, roadmap, documentation rules, or release artifacts.
---

# Documentation maintenance

## Goal

Finish the technical change and the knowledge-base/history update together so the next AI or human can continue without relying on chat history.

## Workflow

1. Read `../../../docs/DOCUMENTATION_POLICY.md`.
2. Read the latest relevant entries in `../../../docs/VERSION_JOURNAL.md`.
3. Identify the change type in the mandatory update matrix.
4. Update every required canonical document in the same task.
5. Append a new `VERSION_JOURNAL.md` entry for every significant update.
6. Keep `AGENTS.md` and `CLAUDE.md` short; point them to canonical docs instead of copying detail.
7. If a recurring workflow changed, update the corresponding Skill.
8. Run applicable tests/checks.
9. Verify that `docs/PROJECT_MAP.md` still points to the right files/symbols.

## Required habits

- Significant project update -> append `../../../docs/VERSION_JOURNAL.md`.
- User-visible change -> update `../../../CHANGELOG.md`.
- Architecture/data-flow change -> update architecture/design docs.
- Build/dependency/test change -> update `../../../docs/DEVELOPMENT.md`.
- Release -> update `../../../docs/RELEASES.md`, `../../../docs/ROADMAP.md`, `../../../CHANGELOG.md` and `../../../docs/VERSION_JOURNAL.md`.
- Active work changed -> rewrite `../../../docs/exec-plans/CURRENT.md` as needed.

## Journal rules

- Treat `VERSION_JOURNAL.md` as append-only history.
- Use the next `YYYY-MM-DD.NN` ID.
- Record purpose, changed components, validation, limitations and rollback/release metadata when applicable.
- Do not invent unknown historical dates, SHA values or test results.
- Correct an old entry by adding a new correction entry, not by silently rewriting history.

## Anti-drift rule

Do not create separate Claude-only and ChatGPT-only copies of project facts or version history. Shared facts belong under `docs/`.

## Final checks

- Documentation describes implemented behavior, not an intention.
- A journal entry exists for each significant update made in the task.
- Planned features are marked Planned.
- No real meeting transcript/audio or secrets were copied into docs.
- The active plan has no stale completed work presented as future work.
