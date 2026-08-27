# DION Meeting Assistant — project memory

## How to work with this repository

Keep this file short. Detailed project knowledge belongs in `docs/`; do not duplicate the same architecture or procedures here.

Before changing anything:

1. Read `docs/PROJECT_MAP.md`.
2. If continuing recent work or investigating a regression, read the latest relevant entries in `docs/VERSION_JOURNAL.md`.
3. Read the relevant document under `docs/design-docs/` or `docs/DEVELOPMENT.md`.
4. Inspect only the mapped code/build files.
5. Use project Skills under `.claude/skills/` when their descriptions match the task.

## Source of truth

`docs/` is the canonical project knowledge base. Chat history and old release notes are supporting context only.

This branch (`dion-exe-build`) is a release/build branch:

- `dion-portable/` stores encoded base-project parts;
- `dion-hotfix/apply_051.py` applies the safe shared-PortAudio patch;
- `dion-quality/apply_060.py` applies the 0.6 recognition-quality patch;
- `.github/workflows/build-dion-portable.yml` reconstructs, patches, tests, bundles models, builds the EXE, self-tests it and publishes the release.

Do not read every encoded part merely to orient yourself. Use `docs/PROJECT_MAP.md` first.

## Non-negotiable project rules

- Windows 10/11 x64 is the target.
- Speech recognition must remain local/offline by default.
- Do not send transcript/audio to external services without explicit approval.
- Preserve the 0.5.1 shared PortAudio safety architecture.
- Diarization stays off by default until safely isolated.
- Diagnostic/crash reports must not contain transcript text or raw meeting audio.
- Do not silently invent speaker names, protocol assignees or deadlines.

## Documentation is part of the change

Every code/build/configuration change must update the affected documentation in the same task. Follow `docs/DOCUMENTATION_POLICY.md`.

Typical mapping:

- every significant update -> append `docs/VERSION_JOURNAL.md`;
- behavior -> `CHANGELOG.md`;
- code location/responsibility -> `docs/PROJECT_MAP.md`;
- architecture/data flow -> `docs/ARCHITECTURE.md` or a design doc;
- tests/build/dependencies -> `docs/DEVELOPMENT.md`;
- release/status -> `docs/RELEASES.md`, `docs/ROADMAP.md`, `docs/VERSION_JOURNAL.md`;
- active work -> `docs/exec-plans/CURRENT.md`.

`VERSION_JOURNAL.md` is append-only. Correct history with a new entry instead of rewriting old entries.

## Validation baseline

For reconstructed 0.6 source:

```bash
python -m pytest -q
```

Baseline: 25 passing tests.

Portable release builds must also pass the packaged `--portable-selftest` on Windows before publication.

## Useful project Skills

- `project-navigation` — locate the right code without scanning the whole project.
- `documentation-maintenance` — update the knowledge base and version journal after any change.
- `release-process` — build/test/release workflow and release journal entry.

When a Skill is relevant, read its `SKILL.md` and only then load referenced supporting files as needed.
