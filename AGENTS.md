# AGENTS.md

## Purpose
Short entry point for AI coding agents. Canonical project knowledge lives in `docs/`.

## Start every task
1. Read `docs/PROJECT_MAP.md`.
2. Read recent relevant `docs/VERSION_JOURNAL.md` entries.
3. Read only the relevant design/development document.
4. Inspect only mapped source/build files.
5. Treat chat history as secondary to repository documentation.

## Release/build branch
`dion-exe-build` reconstructs the base source and applies, in order:
- `dion-hotfix/apply_051.py` — shared-PortAudio stability;
- `dion-quality/apply_060.py` — recognition quality;
- `dion-secretary-bot/apply_070.py` — DION roster/Secretary Bot + isolated speaker fallback;
- `dion-hardening/apply_071.py` — mTLS/privacy/lifecycle/speaker/release hardening;
- `dion-visual/apply_080.py` — native PySide6/QSS Visual Refresh;
- `dion-guest-bot/apply_090.py` — room-URL-first Guest Secretary Bot, optional slug IAPI and browser adapter.

Do not scan encoded `part*` files for orientation. Use the project map.

## Current validation baseline
Published release: **v0.8-visual-refresh**.

Development candidate: **0.9 Guest Secretary Bot** on `dion-guest-bot-0.9`.

0.9 reconstructed source currently passes **36/36 tests** plus compileall. Windows PR CI, PyInstaller packaged self-test and production Release are not yet recorded as complete. Do not describe 0.9 as released until `docs/RELEASES.md` has the actual artifact metadata.

## Non-negotiable rules
- Windows 10/11 x64 target.
- STT remains local/offline by default.
- Preserve shared PortAudio context safety.
- Preserve the approved 0.8 visual system unless an explicit redesign is requested; read `docs/design-docs/UI_VISUAL_SYSTEM.md` before UI changes.
- 0.9 primary DION flow is guest-by-room-URL; do not reintroduce mandatory `event_id`/token/mTLS for normal guest entry.
- Accept corporate/on-prem HTTPS `/join/<slug>` URLs; do not hard-code a public DION hostname.
- Integration API is optional metadata/control-plane enrichment. Slug roster results do not prove current live presence.
- Browser automation/probing is best-effort and loopback-only. A browser/DOM change must degrade to visible manual guest entry, not break transcription.
- Never infer a speaker from CSS color, generic page text or microphone-enabled state.
- Browser active-speaker state is not yet used to retroactively relabel delayed Whisper chunks without timing calibration.
- Diarization is opt-in until field performance is proven.
- DION credentials/mTLS material stay memory-only.
- Never infer a participant name from roster alone.
- Persistent voice profiles are opt-in; Windows persistence is DPAPI-protected and excludes name/e-mail.
- Diagnostics/crash reports exclude transcript/audio/tokens/meeting URLs/invite secrets/private-key passwords.
- Published version assets are not clobbered; bump the version instead.

## Fast routing
- UI/visual design: `docs/design-docs/UI_VISUAL_SYSTEM.md`, `app/ui.py`.
- Guest Bot / room URL / browser adapter / slug IAPI: `docs/design-docs/DION_INTEGRATION.md`, `app/dion_bot.py`, `app/dion_api.py`, `app/ui.py`.
- Speech recognition: `docs/design-docs/SPEECH_RECOGNITION.md`, `app/transcriber.py`.
- Speaker ID/overlap: `docs/design-docs/SPEAKER_IDENTIFICATION.md`, `app/speakers.py`, `app/speaker_profiles.py`.
- Audio startup/WASAPI: `docs/design-docs/AUDIO_STABILITY.md`, `app/audio.py`.
- Privacy/network secrets: `docs/design-docs/PRIVACY_SECURITY.md`.
- Release/build: `docs/DEVELOPMENT.md`, workflow, patch scripts, `release/model-manifest.json`.
- Version history: `docs/VERSION_JOURNAL.md`.

## Before finishing
Run applicable tests, update `VERSION_JOURNAL.md` for significant work, and update every affected canonical document according to `docs/DOCUMENTATION_POLICY.md`. Do not claim corporate DION/WASAPI/browser-DOM/target-display field validation unless it actually happened.
