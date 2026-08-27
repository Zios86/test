---
name: documentation-maintenance
description: Keep DION Meeting Assistant project documentation synchronized with code, build, release, and architecture changes. Use whenever you modify project behavior, files, dependencies, tests, workflows, roadmap, or release artifacts.
---

# Documentation maintenance

## Goal

Finish the technical change and the knowledge-base update together so the next AI or human can continue without relying on chat history.

## Workflow

1. Read `../../../docs/DOCUMENTATION_POLICY.md`.
2. Identify the change type in its mandatory update matrix.
3. Update every required canonical document in the same task.
4. Keep `AGENTS.md` and `CLAUDE.md` short; point them to canonical docs instead of copying detail.
5. If a recurring workflow changed, update the corresponding Skill.
6. Run applicable tests/checks.
7. Verify that `docs/PROJECT_MAP.md` still points to the right files/symbols.

## Required habits

- User-visible change -> update `../../../CHANGELOG.md`.
- Architecture/data-flow change -> update architecture/design docs.
- Build/dependency/test change -> update `../../../docs/DEVELOPMENT.md`.
- Release -> update `../../../docs/RELEASES.md` and `../../../docs/ROADMAP.md`.
- Active work changed -> rewrite `../../../docs/exec-plans/CURRENT.md` as needed.

## Anti-drift rule

Do not create separate Claude-only and ChatGPT-only copies of project facts. Shared facts belong under `docs/`.

## Final checks

- Documentation describes implemented behavior, not an intention.
- Planned features are marked Planned.
- No real meeting transcript/audio or secrets were copied into docs.
- The active plan has no stale completed work presented as future work.
