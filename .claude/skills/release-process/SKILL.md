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

## Current published baseline

```text
Version: 0.9 Guest Secretary Bot
Tag: v0.9-guest-secretary-bot
EXE: DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe
Size: 627,722,376 bytes
SHA-256: 3e57b7c1fac965a14518d6eecc86642bcd3367af1fcf66af01e71142c09aef22
Target commit: f5ae18ef98d26236e9c7f5f42aa5b7e685c5a7e6
Rollback: v0.8-visual-refresh
```

Validation references:
- PR Windows CI `33150603611` passed through packaged self-test; Release step skipped by design.
- Production Windows CI `33150927129` passed through GitHub Release publication.

Corporate DION/WASAPI/browser-DOM behavior remains field-validation evidence, not a CI claim.

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

## Required validation for future releases
- Reconstruct source in patch order.
- Run `python -m compileall` and `python -m pytest -q`.
- Preserve shared PortAudio safety checks.
- Validate exact dependency lock and `pip check`.
- Validate affected UI/source markers and version values.
- Qt smoke must construct/close `MainWindow` offscreen.
- Verify pinned model hashes/revision.
- Build the exact versioned EXE.
- Run packaged `--portable-selftest` on Windows.
- PR build must not publish.
- Do not merge/release if required checks fail.

## 0.9 invariants that future releases must preserve unless explicitly changed
- `/join/<slug>` supports corporate/on-prem hostname;
- normal guest flow does not require token/mTLS/event_id;
- `dion_event_id_edit` is not primary UI;
- optional `dion_api_base_edit` remains advanced configuration;
- browser DevTools stays bound to `127.0.0.1`;
- auto guest entry has a visible manual fallback;
- slug IAPI results do not claim `is_active=true` without evidence;
- speaker probe does not rely on CSS color, generic text or microphone-enabled state;
- `websocket-client==1.8.0` remains locked while the current browser adapter ships.

## Candidate rules
For a future unreleased version:
- clearly label it candidate/unreleased;
- never invent artifact size/SHA;
- do not overwrite the current published tag/asset;
- keep the last published fallback explicit until the new Release actually exists.

## Artifact rules
After **actual publication**:
1. read the GitHub Release API, not a planned path;
2. confirm SHA-256, size, target commit and uploaded asset state;
3. record exact artifact facts in `../../../docs/RELEASES.md`;
4. append a new **released** entry to `../../../docs/VERSION_JOURNAL.md` rather than rewriting the earlier implemented/unreleased entry;
5. update `../../../CHANGELOG.md`;
6. update `../../../docs/ROADMAP.md` and `../../../docs/exec-plans/CURRENT.md`;
7. update README/AGENTS/CLAUDE and AI handoff status;
8. distinguish CI validation from field validation.

Published tags/assets are immutable. Never use `--clobber` to replace a production binary; bump the version/tag.

## Model rule
Do not silently substitute Whisper `base` for `small` in the current Quality line. Any model-profile change requires explicit version/profile naming, tests and documentation.

## Browser/dependency rule
The current browser automation uses installed Edge/Chrome and local DevTools. Do not bundle a second Chromium or introduce a cloud/network dependency merely to automate guest entry without an explicit architecture/release decision.

## Security rule
Do not embed credentials, room URLs, participant data or real meeting content in workflow artifacts. Bundled runtime must not require cloud STT.

## Final checks
- GitHub Release exists before calling a version released.
- Asset state is uploaded.
- Release notes and `docs/RELEASES.md` agree with Release API.
- `VERSION_JOURNAL.md` preserves implementation history and final release facts.
- `CHANGELOG.md`, `ROADMAP.md`, `CURRENT.md`, README and AI adapters show the correct published/candidate distinction.
- Documentation policy is satisfied.
