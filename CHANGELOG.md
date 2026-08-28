# Changelog

All notable user-visible changes to DION Meeting Assistant are recorded here.

## 0.9 Guest Secretary Bot — Unreleased

### Guest entry
- Ordinary DION room URL is now the primary Secretary Bot input: `https://host/join/<slug>`.
- `event_id` is no longer required in the normal guest-entry workflow.
- Corporate/on-prem HTTPS DION hosts are supported; the app does not require a `dion.vc` hostname.
- Room slug and host are parsed automatically and shown in the UI.
- Guest Bot can open without DION Integration API token or mTLS credentials.
- Bot name defaults to `Секретарь-бот`.
- Edge/Chrome opens in a separate temporary profile with bot-browser audio muted.
- Added best-effort automatic name fill and `Войти как гость` click through localhost-only browser DevTools.
- If automation is unavailable or DION markup/policy differs, the visible guest page remains available for manual confirmation.

### Optional DION API
- Integration API fields moved to an advanced/optional group.
- Added configurable IAPI base URL for corporate deployments.
- Existing token + PEM mTLS certificate/key/password support is retained.
- Added participant metadata lookup by room slug.
- Slug-derived participant rows are explicitly treated as metadata only; they do not prove who is currently in the room.

### Browser participant / speaker observation
- Added experimental capability-gated browser adapter.
- Participant detection accepts only explicit `data-participant-id` / `data-user-id` style identifiers plus explicit names.
- Active-speaker detection accepts only explicit speaking attributes or speaking ARIA semantics.
- The app does not infer a speaker from CSS highlight/color, generic page text, participant order or microphone-enabled state.
- Browser active-speaker state is shown as live state only; it does not retroactively rewrite delayed Whisper transcript chunks until field timing is calibrated.

### Compatibility / privacy
- Main STT audio remains Windows WASAPI Loopback; 0.9 does not claim direct per-user DION audio tracks.
- 0.8 visual shell remains intact.
- 0.7.1 hardening remains intact: shared PortAudio safety, memory-only DION secrets, opt-in diarization, conservative Voice ID, DPAPI-protected persistent profiles.
- Real room URL/slug are excluded from public diagnostics/state.
- DevTools is bound to `127.0.0.1` and guest profiles are temporary.

### Dependency / build
- Added locked `websocket-client==1.8.0` for local DevTools WebSocket communication.
- Added `dion-guest-bot/apply_090.py` after the 0.8 visual patch in the release chain.
- Planned binary: `DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe`.
- Planned tag: `v0.9-guest-secretary-bot`.

### Validation so far
- Reconstructed 0.8 + 0.9 source: **36/36 tests passed**.
- `compileall` passed.
- Windows PR CI, packaged EXE self-test and production Release are still pending; 0.9 must not be described as released yet.

## 0.8 Visual Refresh — 2026-08-28

### Interface
- Replaced the utility-style single screen with a modern native PySide6/QSS application shell.
- Added left navigation: Встреча, Стенограмма, Протокол, Участники, Секретарь-бот, Диагностика, Настройки.
- Live transcript now uses readable speaker cards with timestamp, speaker identity and state styling.
- Current speaker receives a blue `Говорит` state; overlapping speech receives a separate `Перебивание` danger state.
- Added top live-status bar for meeting/recording/audio/DION state.
- Added right summary rail for participants, active speaker, audio quality, protocol draft and hotwords.
- Added persistent bottom actions for start/stop, DOCX export and protocol access.
- Added a dedicated Secretary Bot visual status card and dedicated settings/participants pages.
- Existing pause, decision/task markers, DION/mTLS, Voice ID, diagnostics and recognition settings were preserved and redistributed rather than removed.

### Design system
- Added canonical `docs/design-docs/UI_VISUAL_SYSTEM.md`.
- Shipping UI is native Qt widgets/QSS, not a rasterized mockup.

### Build / validation
- Added `dion-visual/apply_080.py` after 0.7.1 hardening.
- Added Windows Qt `offscreen` MainWindow construction smoke-test.
- Local visual-refresh workspace: **48/48 tests passed** and compileall passed.
- Windows PR CI `33129215245` passed source checks, Qt smoke, pinned models, EXE build and packaged self-test.
- Initial production run `33129501062` passed through EXE/self-test but failed only at the old release-existence guard.
- PR #3 fixed the guard; validation run `33145190036` passed.
- Final production CI `33145419554` passed all gates and published the release.

### Published artifact
- Release: `v0.8-visual-refresh`
- `DION_Meeting_Assistant_0.8_Visual_Refresh_Portable.exe`
- Size: `627,541,530 bytes`
- SHA-256: `0ea963916ecf00d9bf9ef219377e709718d1c5d458ec656fc54f5527d43f3fa9`

Field visual/usability testing on the target Windows workstation and real corporate DION validation remain required.

## 0.7.1 Hardening — 2026-08-28

### Security / DION
- Added DION mTLS client certificate, PEM private key and optional encrypted-key password fields.
- DION credentials are captured before worker threads and remain memory-only.
- Persistent voice profiles no longer store participant name/e-mail; Windows persistence is DPAPI-protected.
- Secretary Bot invite is revoked on normal application shutdown when possible.
- Stale temporary Secretary Bot browser profiles are cleaned on later startup.

### Speaker accuracy / safety
- Diarization is off by default until field CPU/stability is proven.
- Automatic Voice ID only considers currently active DION participants.
- Cross-meeting Voice ID thresholds are conservative.
- When diarization is active, Whisper word timestamps split a recognized segment at speaker-handoff boundaries.

### Build / release
- Added exact Windows CI dependency lock.
- Pinned Whisper revision and speaker-model SHA-256 values.
- Pinned GitHub Actions by immutable commit SHA.
- PR builds do not publish releases.
- Published version tags/assets are not overwritten.
- Published `v0.7.1` after green production Windows build and packaged self-test.

### Validation
- Reconstructed source before PR: **46 tests passed locally**; compileall passed.
- PR run `33126146077` passed.
- Production run `33126756679` passed and published Release.

### Published artifact
- `DION_Meeting_Assistant_0.7.1_Hardening_Portable.exe`
- Size: `627,528,485 bytes`
- SHA-256: `90751e2d7a71a5bbcf3e3f0e185284ba08099244779ad8174f0afb89ada04239`

## 0.7 Secretary Bot — 2026-08-27
- Added API-created `Секретарь-бот` invite flow.
- Added direct DION participant/session polling and bot-presence status.
- Added isolated sherpa-onnx diarization subprocess, no five-speaker app limit and `[ПЕРЕБИВАНИЕ]` markers.
- Documented that current IAPI is not a Windows/Python per-user media/verified active-speaker API.

## 0.6 Quality — 2026-08-27
- Offline Whisper small instead of base.
- Beam 5, bounded previous-utterance context, editable terminology/hotwords.
- Tuned VAD, adjacent segment merging and 12-second chunks.
- Shared PortAudio safety retained.

## 0.5.1 Safe — 2026-08-27
- Shared PortAudio context for loopback + microphone.
- Synchronized audio stream open/close.
- Safer startup and portable self-test.

## Earlier MVP line
Introduced local WASAPI/microphone capture, faster-whisper, live transcript, autosave/export, deterministic protocol, diagnostics and experimental speaker clustering.

User-visible changes update this file in the same task. Published binary facts belong in `docs/RELEASES.md`; detailed engineering chronology belongs in `docs/VERSION_JOURNAL.md`.
