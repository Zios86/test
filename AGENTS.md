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
- `dion-hotfix/apply_051.py`;
- `dion-quality/apply_060.py`;
- `dion-secretary-bot/apply_070.py`;
- `dion-hardening/apply_071.py`;
- `dion-visual/apply_080.py`;
- `dion-guest-bot/apply_090.py`.
- `dion-browser-gate/apply_091.py`.
- `dion-postprocess/apply_100.py`.

Do not scan encoded `part*` files for orientation. Use `docs/PROJECT_MAP.md`.

## Current validation baseline
Current published release: **v1.0-post-meeting-precision**. Next candidate: **v1.0.1-audit-hardening**.

Artifact line: `DION_Meeting_Assistant_1.0_Post_Meeting_Precision_Portable.exe`
Size: `627,722,376 bytes`  
SHA-256: `3e57b7c1fac965a14518d6eecc86642bcd3367af1fcf66af01e71142c09aef22`

Validation:
- reconstructed source: `36/36 tests passed` + compileall;
- PR Windows CI `33150603611`: tests, pinned models, EXE build, packaged self-test passed;
- production Windows CI `33150927129`: tests, pinned models, EXE build, packaged self-test and Release publication passed.

Corporate DION guest form/DOM semantics, real WASAPI behavior and browser-speaker timing remain field checks.

The 1.0.1 audit-hardening candidate has `46/46` local tests passing; Windows CI/release validation is pending.

## Non-negotiable rules
- Windows 10/11 x64 target; STT local/offline by default.
- Preserve shared PortAudio context safety.
- Preserve approved 0.8 visual system unless redesign is explicitly requested.
- Primary DION flow is guest-by-room-URL `/join/<slug>`; do not reintroduce mandatory `event_id`/token/mTLS for normal guest entry.
- Accept corporate/on-prem HTTPS DION hosts; do not hard-code `dion.vc`.
- Integration API is optional enrichment; slug roster results do not prove current live presence.
- Browser automation/probing is loopback-only and best-effort; failure must fall back to visible manual guest entry.
- Never infer a speaker from CSS color, generic page text or microphone-enabled state.
- Browser active-speaker state does not retroactively relabel delayed Whisper chunks until timing is field-calibrated.
- Diarization remains opt-in until field performance is proven.
- DION credentials/mTLS material stay memory-only.
- Persistent voice profiles are opt-in, DPAPI-protected on Windows, and exclude name/e-mail.
- Diagnostics/crash reports exclude transcript/audio/tokens/meeting URLs/invite secrets/private-key passwords.
- Published version assets are immutable; bump the version instead of clobbering.

## Fast routing
- Guest Bot / room URL / browser adapter / slug IAPI: `docs/design-docs/DION_INTEGRATION.md`, `app/dion_bot.py`, `app/dion_api.py`, `app/ui.py`.
- UI: `docs/design-docs/UI_VISUAL_SYSTEM.md`, `app/ui.py`.
- Recognition: `docs/design-docs/SPEECH_RECOGNITION.md`, `app/transcriber.py`.
- Speaker ID/overlap: `docs/design-docs/SPEAKER_IDENTIFICATION.md`, `app/speakers.py`, `app/speaker_profiles.py`.
- Audio: `docs/design-docs/AUDIO_STABILITY.md`, `app/audio.py`.
- Post-meeting precision: `docs/ARCHITECTURE.md`, `docs/design-docs/PRIVACY_SECURITY.md`, `app/postprocess.py`, `postprocess-server/`.
- Privacy: `docs/design-docs/PRIVACY_SECURITY.md`.
- Release/build: `docs/DEVELOPMENT.md`, workflow, patch scripts, model manifest.
- History: `docs/VERSION_JOURNAL.md`.

## Before finishing
Run applicable tests and update every affected canonical document according to `docs/DOCUMENTATION_POLICY.md`. Do not claim corporate DION/WASAPI/browser-DOM field validation unless it actually happened.
