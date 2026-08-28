# Changelog

All notable user-visible changes to DION Meeting Assistant are recorded here.

## 1.0 Post-meeting Precision — release validation pending

- Added continuous, separate WAV recording for system audio and microphone while live transcription continues.
- Added an explicit post-meeting action that packages the original draft transcript and audio without modifying either source.
- Added authenticated private-LAN streaming upload, job progress polling and safe result download.
- Added a Windows server package for faster-whisper `large-v3-turbo` retranscription and conservative local Ollama correction.
- Results include precise JSON, corrected TXT and a comparison/review report; Ollama failure falls back to raw precise recognition.
- Public/hostname endpoints are rejected by the client; the server requires a token and an allowed client IP in network mode.
- Added five regression/security tests; reconstructed source passes 42 tests.

## 0.9.1 Browser Gate Hotfix — unreleased

- Added automatic handling of the corporate DION «Переход в Конференции» page.
- The bot now clicks «Продолжить в браузере», waits for the guest form, enters `Секретарь-бот`, and clicks «Войти как гость».
- The visible manual fallback remains available when browser automation is unavailable.
- Added a regression test for the confirmed two-stage corporate DION flow.

## 0.9 Guest Secretary Bot — 2026-08-28

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
- Slug-derived participant rows are metadata only and do not prove who is currently in the room.

### Browser participant / speaker observation
- Added experimental capability-gated browser adapter.
- Participant detection accepts only explicit participant/user identifiers plus explicit names.
- Active-speaker detection accepts only explicit speaking attributes or speaking ARIA semantics.
- The app does not infer a speaker from CSS highlight/color, generic page text, participant order or microphone-enabled state.
- Browser active-speaker state is live state only; it does not retroactively rewrite delayed Whisper transcript chunks until field timing is calibrated.

### Compatibility / privacy
- Main STT audio remains Windows WASAPI Loopback; 0.9 does not claim direct per-user DION audio tracks.
- 0.8 visual shell and 0.7.1 hardening remain intact.
- Real room URL/slug are excluded from public diagnostics/state.
- DevTools is bound to `127.0.0.1` and guest profiles are temporary.

### Dependency / build
- Added locked `websocket-client==1.8.0` for local DevTools WebSocket communication.
- Added `dion-guest-bot/apply_090.py` after the 0.8 visual patch in the release chain.

### Validation
- Reconstructed 0.8 + 0.9 source: **36/36 tests passed**; compileall passed.
- PR Windows CI `33150603611` passed tests, pinned models, EXE build and packaged self-test; Release step skipped by design.
- Production Windows CI `33150927129` passed tests, pinned models, EXE build, packaged self-test and Release publication.

### Published artifact
- Release: `v0.9-guest-secretary-bot`
- `DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe`
- Size: `627,722,376 bytes`
- SHA-256: `3e57b7c1fac965a14518d6eecc86642bcd3367af1fcf66af01e71142c09aef22`

Corporate DION guest form/DOM semantics, WASAPI behavior and speaker timing still require field validation.

## 0.8 Visual Refresh — 2026-08-28
- Replaced the utility-style single screen with a modern native PySide6/QSS application shell.
- Added left navigation, card-based live transcript, active/overlap visual states, top status bar, right summary rail and bottom actions.
- Added canonical `docs/design-docs/UI_VISUAL_SYSTEM.md` and Windows Qt offscreen MainWindow smoke-test.
- Final production CI `33145419554` passed all gates and published `v0.8-visual-refresh`.
- Artifact: `DION_Meeting_Assistant_0.8_Visual_Refresh_Portable.exe`, `627,541,530 bytes`, SHA-256 `0ea963916ecf00d9bf9ef219377e709718d1c5d458ec656fc54f5527d43f3fa9`.

## 0.7.1 Hardening — 2026-08-28
- Added DION mTLS certificate/key/password UI, memory-only secrets, DPAPI voice-profile protection and safer Secretary Bot shutdown cleanup.
- Diarization is opt-in; Voice ID considers active DION participants only; word timestamps can split speaker handoffs.
- Added exact dependency lock, pinned model inputs and immutable release policy.
- Published `v0.7.1`; artifact `627,528,485 bytes`, SHA-256 `90751e2d7a71a5bbcf3e3f0e185284ba08099244779ad8174f0afb89ada04239`.

## 0.7 Secretary Bot — 2026-08-27
- Added API-created `Секретарь-бот` invite flow, DION participant/session polling, isolated diarization subprocess, no five-speaker app limit and `[ПЕРЕБИВАНИЕ]` markers.
- Documented that IAPI is not a Windows/Python per-user media/verified active-speaker API.

## 0.6 Quality — 2026-08-27
- Offline Whisper small instead of base.
- Beam 5, bounded previous context, editable terminology/hotwords, tuned VAD and 12-second chunks.

## 0.5.1 Safe — 2026-08-27
- Shared PortAudio context for loopback + microphone, synchronized stream open/close, safer startup and portable self-test.

## Earlier MVP line
Introduced local WASAPI/microphone capture, faster-whisper, live transcript, autosave/export, deterministic protocol, diagnostics and experimental speaker clustering.

Published binary facts belong in `docs/RELEASES.md`; detailed engineering chronology belongs in `docs/VERSION_JOURNAL.md`.
