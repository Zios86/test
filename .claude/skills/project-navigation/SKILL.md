---
name: project-navigation
description: Locate the correct DION Meeting Assistant files and symbols without scanning the whole repository. Use when starting a coding, debugging, architecture, or review task and you need to find where a feature is implemented.
---

# Project navigation

## Goal

Find the smallest relevant part of the project before reading or editing code.

## Workflow

1. Read `../../../docs/PROJECT_MAP.md`.
2. Match the requested task to the routing table near the end of that file.
3. Read the linked design/development document only if needed.
4. Inspect only the mapped source/build files.
5. Expand to neighboring modules only when the call/data flow requires it.

## Special rule for this branch

`dion-exe-build` stores the base project as encoded `dion-portable/part*` files plus patch scripts.

Do **not** read all encoded parts for orientation.

For current behavior, use:

- `dion-hotfix/apply_051.py` for the 0.5.1 stability delta;
- `dion-quality/apply_060.py` for the 0.6 recognition delta;
- `.github/workflows/build-dion-portable.yml` for release behavior;
- the logical reconstructed-source map in `docs/PROJECT_MAP.md`.

Reconstruct the source only when the requested code change actually requires it.

## Fast routes

- recognition accuracy -> `app/transcriber.py` logical module;
- audio/start crash -> `app/audio.py`, then `app/ui.py`;
- protocol -> `app/protocol.py`;
- Ollama -> `app/local_ai.py`;
- speaker separation -> `app/speakers.py`;
- export -> `app/storage.py`;
- diagnostics -> `app/health.py`, `app/preflight.py`, `app/crash.py`;
- release -> workflow + patch scripts.

## Final check

If the change modifies where a feature lives, update `docs/PROJECT_MAP.md` before finishing.
