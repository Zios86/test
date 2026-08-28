# Releases

This file records **published artifacts only** plus the engineering meaning of each published release. GitHub Release is the source for the actual downloadable binary.

For unreleased significant work use `VERSION_JOURNAL.md` and `ROADMAP.md`.

## 0.9 Guest Secretary Bot — NOT YET RELEASED

Status: **development candidate / unreleased** on branch `dion-guest-bot-0.9`.

Planned release identity:

```text
Tag: v0.9-guest-secretary-bot
Artifact: DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe
```

Do **not** add size/SHA-256 or claim a Release until Windows PR CI, merge, production build, packaged self-test and GitHub Release publication actually succeed.

Implemented candidate behavior:
- ordinary HTTPS `/join/<slug>` room URL is primary Guest Bot input;
- corporate/on-prem hostnames supported;
- no mandatory `event_id`/token/mTLS for guest entry;
- isolated Edge/Chrome guest session with bot audio muted;
- localhost-only DevTools best-effort auto-name/guest-click plus manual fallback;
- optional configurable IAPI base URL and token/mTLS advanced settings;
- optional participant metadata by slug, explicitly not current-presence proof;
- conservative explicit-data/ARIA browser participant/speaker probe;
- no speaker inference from color, generic text or microphone-enabled state;
- browser live-speaker state not yet used to retroactively relabel delayed Whisper chunks;
- 0.8 visual shell and 0.7.1 hardening retained.

Source validation so far:

```text
36/36 tests passed
compileall passed
```

Windows/release validation: **pending**.

Rollback/published fallback: `v0.8-visual-refresh`.

Journal entry: `VERSION_JOURNAL.md` -> `2026-08-28.04`.

---

## v0.8-visual-refresh — current published release

Status: **published** on 2026-08-28.

Artifact:

```text
DION_Meeting_Assistant_0.8_Visual_Refresh_Portable.exe
```

Size:

```text
627,541,530 bytes
```

SHA-256:

```text
0ea963916ecf00d9bf9ef219377e709718d1c5d458ec656fc54f5527d43f3fa9
```

Release page:

```text
https://github.com/Zios86/test/releases/tag/v0.8-visual-refresh
```

Direct asset:

```text
https://github.com/Zios86/test/releases/download/v0.8-visual-refresh/DION_Meeting_Assistant_0.8_Visual_Refresh_Portable.exe
```

Target commit:

```text
b7ee9bb5017348a83b99e48246a65c5309d35315
```

Key behavior:
- native PySide6/QSS modern light application shell;
- seven-page navigation;
- card-based live transcript with active/overlap states;
- top live status, right summary rail and bottom quick actions;
- dedicated Secretary Bot visual page;
- 0.7.1 hardening preserved.

Build validation:
- local visual-refresh workspace: 48/48 tests + compileall;
- visual PR build `33129215245` passed;
- initial production build `33129501062` passed application gates but failed only at old release-existence guard;
- release-guard PR build `33145190036` passed;
- final production build `33145419554` passed all gates, Release publication and artifact upload.

Field limitations: target Windows DPI/usability, corporate DION/mTLS/WASAPI and real speaker accuracy remain field-test dependent.

Rollback: `v0.7.1`.

Journal entry: `VERSION_JOURNAL.md` -> `2026-08-28.03`.

## v0.7.1

Status: **published**.

Artifact: `DION_Meeting_Assistant_0.7.1_Hardening_Portable.exe`

Size: `627,528,485 bytes`

SHA-256: `90751e2d7a71a5bbcf3e3f0e185284ba08099244779ad8174f0afb89ada04239`

Release: `https://github.com/Zios86/test/releases/tag/v0.7.1`

Key behavior:
- DION mTLS client certificate + PEM key + optional key password;
- diarization opt-in;
- active-only Voice ID candidates;
- DPAPI-protected persistent voice profiles without name/e-mail;
- Secretary Bot lifecycle cleanup;
- word-timestamp speaker handoff splitting;
- pinned dependencies/models and non-clobber release policy.

Validation: PR run `33126146077`, production run `33126756679`, packaged self-test and Release publication passed.

Journal entry: `2026-08-28.02`.

## v0.7-secretary-bot

Status: **published**.

Artifact: `DION_Meeting_Assistant_0.7_Secretary_Bot_Portable.exe`

Size: `627,522,154 bytes`

SHA-256: `704dfcab816ac687f592baa6ff6c0feea785cd24b920eaf7594fe5e0364a00da`

Release: `https://github.com/Zios86/test/releases/tag/v0.7-secretary-bot`

Key behavior: API-created named Secretary Bot invite, participant/session polling, isolated diarization fallback, overlap marker, no five-speaker app limit.

Known limitation: documented IAPI is not a Windows/Python per-user media or verified live active-speaker feed.

Journal entry: `2026-08-28.01`.

## v0.6-quality

Status: **published**.

Artifact: `DION_Meeting_Assistant_0.6_Quality_Portable.exe`

Size: `621,933,502 bytes`

SHA-256: `85a8d0b443b4e07c6b5df16b255775ed7c960da5cc0ddc9e9bab51a5d3658334`

Release: `https://github.com/Zios86/test/releases/tag/v0.6-quality`

Key behavior: offline Whisper small, beam 5, context/hotwords, tuned VAD, 12-second chunks, 0.5.1 PortAudio safety retained.

Journal entry: `2026-08-27.03`.

## v0.5.1-safe — stability fallback

Status: **published**.

Release: `https://github.com/Zios86/test/releases/tag/v0.5.1-safe`

Purpose: shared PortAudio context and safer startup after start-transcription crash. Historical compact build used Whisper base and has lower recognition quality.

Journal entry: `2026-08-27.02`.

## Release policy
For every future release:
1. Tests/build checks pass.
2. Windows packaged self-test passes.
3. Actual uploaded binary SHA-256 is recorded here and in Release notes.
4. A released journal entry records validation, limitations and rollback.
5. `CHANGELOG.md` and `ROADMAP.md` are updated.
6. Field-tested claims and CI-only claims are distinguished.
7. Published tags/assets are never overwritten; bump the version.

Never invent artifact metadata for an unreleased candidate.
