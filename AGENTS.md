# AGENTS.md

## Purpose

This file is the short entry point for AI coding agents. Do not turn it into a project encyclopedia. The canonical knowledge base lives in `docs/`.

## Start every task

1. Read `docs/PROJECT_MAP.md`.
2. If continuing recent work or investigating a regression, read the latest relevant entries in `docs/VERSION_JOURNAL.md`.
3. Read only the design/development document relevant to the requested change.
4. Inspect only the mapped source files before editing.
5. Treat chat history as secondary; repository documentation is the source of truth.

## Repository shape

This branch is a release/build branch, not a normal unpacked source tree.

- `dion-portable/` = encoded base project archive parts.
- `dion-hotfix/apply_051.py` = 0.5.1 stability patch.
- `dion-quality/apply_060.py` = 0.6 recognition-quality patch.
- `.github/workflows/build-dion-portable.yml` = Windows build/release pipeline.
- `docs/PROJECT_MAP.md` = logical map of the reconstructed application.

Do not scan every `dion-portable/part*` file to understand the code. Use the project map and reconstruct only when code changes require it.

## Mandatory documentation rule

Any project change is incomplete until the related documentation is updated according to `docs/DOCUMENTATION_POLICY.md`.

At minimum:

- every significant update -> append a new entry to `docs/VERSION_JOURNAL.md`;
- user-visible behavior -> `CHANGELOG.md`;
- module responsibility, entry point or important symbol change -> `docs/PROJECT_MAP.md`;
- runtime/data-flow change -> `docs/ARCHITECTURE.md` or relevant `docs/design-docs/*`;
- build/test/dependency change -> `docs/DEVELOPMENT.md`;
- release/status change -> `docs/RELEASES.md`, `docs/ROADMAP.md`, and `docs/VERSION_JOURNAL.md`;
- new architectural choice -> record it in the appropriate design document and journal if significant.

`VERSION_JOURNAL.md` is append-only history. Do not rewrite old entries to make them match current behavior; add a correcting entry instead.

Do not duplicate detailed facts in both `AGENTS.md` and `CLAUDE.md`.

## Validation

For reconstructed source, run:

```bash
python -m pytest -q
```

Current 0.6 baseline: 25 passing tests.

For portable releases, the Windows pipeline must also pass the packaged EXE `--portable-selftest` before publication.

## Core constraints

- Windows 10/11 x64 is the target platform.
- Main transcription path is local/offline.
- Do not introduce an external speech/cloud AI dependency without explicit approval.
- Preserve the shared PortAudio context safety fix from 0.5.1.
- Speaker diarization remains disabled by default until the native module is isolated safely.
- Diagnostic/crash files must not contain transcript text or raw audio.

## Fast routing

- Speech recognition quality -> `docs/design-docs/SPEECH_RECOGNITION.md`, logical `app/transcriber.py`.
- Audio/WASAPI/startup crash -> `docs/design-docs/AUDIO_STABILITY.md`, logical `app/audio.py` and `app/ui.py`.
- Protocol extraction -> `docs/ARCHITECTURE.md`, logical `app/protocol.py`.
- Ollama/local AI -> logical `app/local_ai.py`.
- Export/autosave -> logical `app/storage.py`.
- Diagnostics -> logical `app/health.py`, `app/preflight.py`, `app/crash.py`.
- Release build -> `docs/DEVELOPMENT.md`, `.github/workflows/build-dion-portable.yml`.
- Version/update history -> `docs/VERSION_JOURNAL.md`.

## Before finishing

- Run applicable tests/checks.
- Add the required `docs/VERSION_JOURNAL.md` entry for significant work.
- Update all other affected documentation in the same change.
- Update `docs/exec-plans/CURRENT.md` if the change advances or changes active work.
- State any untested Windows/DION-specific behavior explicitly.
