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
- `dion-hardening/apply_071.py` — mTLS/privacy/lifecycle/speaker-attribution/release hardening;
- `dion-visual/apply_080.py` — native PySide6/QSS Visual Refresh.

Do not scan encoded `part*` files for orientation. Use the project map.

## Current validation baseline
Published release: **v0.8-visual-refresh**.

0.8 visual-refresh development workspace passed `48/48` tests and compileall. Published 0.8 additionally passed Windows PR validation, Qt `offscreen` `MainWindow` smoke, pinned-model validation, one-file EXE build and packaged `--portable-selftest`; final production run `33145419554` published the Release. Corporate DION/WASAPI and actual target-display usability remain field checks.

## Non-negotiable rules
- Windows 10/11 x64 target.
- STT local/offline by default.
- Preserve shared PortAudio context safety.
- Preserve the 0.8 visual system unless an explicit redesign is requested; read `docs/design-docs/UI_VISUAL_SYSTEM.md` before UI changes.
- Diarization is opt-in until field performance is proven.
- DION IAPI uses token + mTLS when required; credentials stay memory-only.
- Never infer a participant name from roster alone.
- Persistent voice profiles are opt-in; on Windows they are DPAPI-protected and must not persist name/e-mail.
- Diagnostics/crash reports exclude transcript/audio/tokens/invite secrets.
- Published version assets are not clobbered; bump the version instead.

## Fast routing
- UI/visual design: `docs/design-docs/UI_VISUAL_SYSTEM.md`, `app/ui.py`.
- Speech recognition: `docs/design-docs/SPEECH_RECOGNITION.md`, `app/transcriber.py`.
- Speaker ID/overlap: `docs/design-docs/SPEAKER_IDENTIFICATION.md`, `app/speakers.py`, `app/speaker_profiles.py`.
- DION/mTLS/Secretary Bot: `docs/design-docs/DION_INTEGRATION.md`, `app/dion_api.py`, `app/dion_bot.py`, `app/ui.py`.
- Audio startup/WASAPI: `docs/design-docs/AUDIO_STABILITY.md`, `app/audio.py`.
- Privacy: `docs/design-docs/PRIVACY_SECURITY.md`.
- Release/build: `docs/DEVELOPMENT.md`, workflow, patch scripts, `release/model-manifest.json`.
- Version history: `docs/VERSION_JOURNAL.md`.

## Before finishing
Run applicable tests, update `VERSION_JOURNAL.md` for significant work, and update every affected canonical document according to `docs/DOCUMENTATION_POLICY.md`. Do not claim corporate DION/WASAPI or target-display field validation unless it actually happened.
