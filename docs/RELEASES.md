# Releases

This file records actually published artifacts and their engineering meaning. GitHub Release is the source for the downloadable binary. Chronological engineering history, including unreleased work, lives in `VERSION_JOURNAL.md`.

## v1.0-post-meeting-precision — current published release

Status: **published**. GitHub Release is the authoritative source for its EXE/server assets and hashes.

Release page:

```text
https://github.com/Zios86/test/releases/tag/v1.0-post-meeting-precision
```

The immutable follow-up `v1.0.1-audit-hardening` is a candidate until Windows CI and release publication complete.

## v0.9-guest-secretary-bot — previous published release

Status: **published** on 2026-08-28.

Artifact:

```text
DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe
```

Size:

```text
627,722,376 bytes
```

SHA-256:

```text
3e57b7c1fac965a14518d6eecc86642bcd3367af1fcf66af01e71142c09aef22
```

Release page:

```text
https://github.com/Zios86/test/releases/tag/v0.9-guest-secretary-bot
```

Direct asset:

```text
https://github.com/Zios86/test/releases/download/v0.9-guest-secretary-bot/DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe
```

Target commit:

```text
f5ae18ef98d26236e9c7f5f42aa5b7e685c5a7e6
```

Key behavior:
- ordinary HTTPS `/join/<slug>` room URL is the primary Guest Bot input;
- corporate/on-prem DION hostnames are supported;
- normal guest entry does not require `event_id`, token or mTLS;
- isolated Edge/Chrome guest session with muted browser audio;
- localhost-only DevTools best-effort guest-name fill/click with visible manual fallback;
- optional configurable Integration API base + token/mTLS advanced settings;
- optional participant metadata lookup by slug, explicitly not treated as proof of current live presence;
- conservative browser participant/speaker probe accepts only explicit IDs/names and explicit speaking data/ARIA semantics;
- no speaker inference from CSS highlight/color, generic text, participant ordering or microphone-enabled state;
- browser live-speaker state is not yet used to retroactively relabel delayed Whisper chunks;
- 0.8 visual shell and 0.7.1 hardening are retained.

Validation:
- reconstructed 0.8 + 0.9 source: `36/36 tests passed`, compileall passed;
- PR Windows CI `33150603611`: source validation, pinned models, EXE build and packaged `--portable-selftest` passed; Release step skipped by design;
- production Windows CI `33150927129`: tests, pinned models, EXE build, packaged self-test and GitHub Release publication passed.

Field limitation:
- automatic guest form automation and DOM speaker semantics are deployment/UI-version dependent;
- corporate DION guest join/waiting-room/live speaker timing and real WASAPI behavior remain field-test pending;
- main STT audio is still Windows WASAPI mixed output, not a documented per-user DION media stream.

Rollback: `v0.8-visual-refresh`.

Journal: implementation entry `2026-08-28.04`; release entry is recorded after publication.

---

## v0.8-visual-refresh

Status: **published** on 2026-08-28.

Artifact: `DION_Meeting_Assistant_0.8_Visual_Refresh_Portable.exe`  
Size: `627,541,530 bytes`  
SHA-256: `0ea963916ecf00d9bf9ef219377e709718d1c5d458ec656fc54f5527d43f3fa9`  
Release: `https://github.com/Zios86/test/releases/tag/v0.8-visual-refresh`  
Target commit: `b7ee9bb5017348a83b99e48246a65c5309d35315`

Key behavior: native PySide6/QSS visual refresh with seven-page navigation, card transcript, top status bar, right summary rail and persistent bottom actions; 0.7.1 hardening retained.

Validation: final production run `33145419554` passed source checks, Qt offscreen MainWindow smoke, pinned models, EXE build, packaged self-test, Release publication and artifact upload.

Rollback: `v0.7.1`.

## v0.7.1

Status: **published**.

Artifact: `DION_Meeting_Assistant_0.7.1_Hardening_Portable.exe`  
Size: `627,528,485 bytes`  
SHA-256: `90751e2d7a71a5bbcf3e3f0e185284ba08099244779ad8174f0afb89ada04239`  
Release: `https://github.com/Zios86/test/releases/tag/v0.7.1`  
Target commit: `a8f8a08d1f80f25fa6281ec16fe171e5ac788776`

Key behavior: DION mTLS configuration, opt-in diarization, active-only Voice ID, DPAPI-protected persistent voice profiles, safer Secretary Bot lifecycle, word-level speaker handoff splitting, pinned dependencies/models and immutable release policy.

## v0.7-secretary-bot

Status: **published**.

Artifact: `DION_Meeting_Assistant_0.7_Secretary_Bot_Portable.exe`  
Size: `627,522,154 bytes`  
SHA-256: `704dfcab816ac687f592baa6ff6c0feea785cd24b920eaf7594fe5e0364a00da`  
Release: `https://github.com/Zios86/test/releases/tag/v0.7-secretary-bot`

Key behavior: API-created named Secretary Bot invite, participant/session roster, isolated local speaker engine, no five-speaker application limit and overlap markers.

## v0.6-quality

Status: **published**.

Artifact: `DION_Meeting_Assistant_0.6_Quality_Portable.exe`  
Size: `621,933,502 bytes`  
SHA-256: `85a8d0b443b4e07c6b5df16b255775ed7c960da5cc0ddc9e9bab51a5d3658334`  
Release: `https://github.com/Zios86/test/releases/tag/v0.6-quality`

Key behavior: bundled offline Whisper small, beam 5, bounded previous context, editable terminology, tuned VAD and 12-second chunks.

## v0.5.1-safe — stability fallback

Status: **published**.

Release: `https://github.com/Zios86/test/releases/tag/v0.5.1-safe`

Purpose: shared PortAudio context, safer startup and diarization disabled by default. This portable line used Whisper base and is retained only as a historical stability fallback.

## Release policy

For every future release:
1. Tests and build checks must pass.
2. Packaged EXE self-test must pass on Windows.
3. Actual uploaded binary size/SHA-256 must be recorded here and in Release notes.
4. A released entry must be appended to `VERSION_JOURNAL.md` with validation, limitations and rollback information.
5. `CHANGELOG.md`, `ROADMAP.md`, `CURRENT.md`, README and AI entry docs must be synchronized in the same task.
6. Field-tested claims and CI-only claims must be distinguished explicitly.
7. Published version tags/assets must not be overwritten; bump the version instead.

Do not invent artifact metadata for an unreleased candidate.
