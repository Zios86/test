---
name: release-process
description: Build, validate, publish, and journal a DION Meeting Assistant Windows portable release safely. Use when changing PyInstaller, bundled models, dependencies, version patches, GitHub Actions, Release tags, or artifact metadata.
---

# Release process

## Goal
Produce a reproducible Windows release that is tested before publication and whose code, docs, GitHub Release and uploaded EXE agree.

## Before changing the build
1. Read `../../../docs/DEVELOPMENT.md`.
2. Read `../../../docs/RELEASES.md`.
3. Read latest relevant `../../../docs/VERSION_JOURNAL.md` entries.
4. Read `../../../docs/PROJECT_MAP.md` for physical build layout.
5. Confirm whether the target version is only a candidate or already published.

## Current build path

```text
dion-portable parts
 -> reconstruct source
 -> apply dion-hotfix/apply_051.py
 -> apply dion-quality/apply_060.py
 -> apply dion-secretary-bot/apply_070.py
 -> apply dion-hardening/apply_071.py
 -> apply dion-visual/apply_080.py
 -> apply dion-guest-bot/apply_090.py
 -> install exact locked dependencies
 -> source tests + compile checks
 -> Qt offscreen MainWindow smoke
 -> pinned/verified offline models
 -> PyInstaller onefile
 -> packaged --portable-selftest
 -> GitHub Release only on qualifying dion-exe-build push
```

## 0.9 candidate identity
Until actually published:

```text
Version: 0.9 Guest Secretary Bot
Planned tag: v0.9-guest-secretary-bot
Planned EXE: DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe
Published fallback: v0.8-visual-refresh
```

Never invent size/SHA for the planned artifact.

## Required validation
- Reconstruct source in patch order.
- Run `python -m compileall` and `python -m pytest -q`.
- Preserve shared PortAudio safety checks.
- Validate exact dependency lock and `pip check`.
- Validate 0.9 guest UI source markers and version values.
- Qt smoke must construct/close `MainWindow` offscreen.
- Verify Guest Bot room URL is primary and API fields are advanced/optional.
- Verify pinned model hashes/revision.
- Build the exact versioned EXE.
- Run packaged `--portable-selftest` on Windows.
- PR build must not publish.
- Do not merge/release if required checks fail.

## 0.9-specific checks
Validate at minimum:
- `/join/<slug>` parser exists and supports corporate hostname;
- normal guest flow does not require token/mTLS/event_id;
- `dion_event_id_edit` is not reintroduced as primary UI;
- `dion_api_base_edit` exists in advanced settings;
- browser DevTools address stays `127.0.0.1`;
- auto guest entry has a visible manual fallback;
- slug IAPI results do not claim `is_active=true` without evidence;
- speaker probe does not rely on CSS color/generic text/mic-enabled state;
- `websocket-client==1.8.0` is in the exact lock when browser adapter ships.

## Artifact rules
After **actual publication**:
1. read the GitHub Release API, not a planned path;
2. calculate/confirm SHA-256 of the actual uploaded EXE;
3. record artifact name, size, target commit and SHA in `../../../docs/RELEASES.md`;
4. append a new **released** entry to `../../../docs/VERSION_JOURNAL.md` rather than rewriting the earlier implemented/unreleased entry;
5. update `../../../CHANGELOG.md` status/artifact block;
6. update `../../../docs/ROADMAP.md` and `exec-plans/CURRENT.md`;
7. update README/AGENTS/CLAUDE current-published status;
8. distinguish CI validation from corporate DION field validation.

Published tags/assets are immutable. Never use `--clobber` to replace a production binary; bump the version/tag.

## Model rule
Do not silently substitute Whisper `base` for `small` in the current Quality line. Any model-profile change requires explicit version/profile naming, tests and documentation.

## Browser/dependency rule
0.9 browser automation uses installed Edge/Chrome and local DevTools; do not bundle a second Chromium without an explicit architecture/release decision.

Do not introduce a network/cloud dependency merely to automate guest entry. Browser DevTools communication remains loopback-only.

## Security rule
Do not embed credentials, room URLs, participant data or real meeting content in workflow artifacts. Bundled application runtime must not require cloud STT.

## Final checks
- GitHub Release exists before calling the version released.
- Asset state is uploaded.
- Release notes and `docs/RELEASES.md` agree with Release API.
- `VERSION_JOURNAL.md` has both prior implementation history and final release facts without deleting accepted entries.
- `CHANGELOG.md`, `ROADMAP.md`, `CURRENT.md`, README and AI adapters show the correct published/candidate distinction.
- Documentation policy is satisfied.
