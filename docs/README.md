# Project documentation index

`docs/` is the canonical knowledge base for DION Meeting Assistant across humans, Claude, ChatGPT/Codex and other agents.

## Reading order
1. `PROJECT_MAP.md` — locate the relevant component.
2. `VERSION_JOURNAL.md` — check recent relevant changes.
3. Read only the matching design doc.
4. For code/build work, read `DEVELOPMENT.md`.
5. Before finishing, check `DOCUMENTATION_POLICY.md`.

## Canonical documents
- `PROJECT_MAP.md` — file/class/function routing.
- `ARCHITECTURE.md` — runtime flow and failure boundaries.
- `DEVELOPMENT.md` — environment, tests, dependency/model pins, CI/release.
- `DOCUMENTATION_POLICY.md` — mandatory update matrix.
- `VERSION_JOURNAL.md` — append-only engineering history.
- `AI_HANDOFF.md` — cross-AI handoff.
- `ROADMAP.md` — current direction/future work.
- `RELEASES.md` — actually published binaries and hashes.

## Design docs
- `design-docs/UI_VISUAL_SYSTEM.md` — 0.8+ UI system and 0.9 guest-flow placement.
- `design-docs/SPEECH_RECOGNITION.md` — Whisper/VAD/context/word timestamps.
- `design-docs/AUDIO_STABILITY.md` — WASAPI/PortAudio/startup safety.
- `design-docs/SPEAKER_IDENTIFICATION.md` — diarization, Voice ID, overlap, confidence.
- `design-docs/DION_INTEGRATION.md` — room-URL-first Guest Bot, browser adapter, optional IAPI/mTLS.
- `design-docs/PRIVACY_SECURITY.md` — local data, credentials, guest-browser DevTools, voice profiles and diagnostics.

## Execution plan
`exec-plans/CURRENT.md` is the single current plan; durable history belongs in `VERSION_JOURNAL.md`.

AI-specific files `AGENTS.md`, `CLAUDE.md` and `.claude/skills/*` are adapters/procedures, not duplicate knowledge bases.

## Current documentation baseline
- Current published release: **0.9 Guest Secretary Bot** (`v0.9-guest-secretary-bot`).
- Artifact: `DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe`.
- Size: `627,722,376 bytes`.
- SHA-256: `3e57b7c1fac965a14518d6eecc86642bcd3367af1fcf66af01e71142c09aef22`.
- Source baseline: `36/36 tests + compileall passed`.
- PR Windows CI `33150603611` passed through packaged self-test.
- Production Windows CI `33150927129` passed through Release publication.
- Corporate DION guest-form/DOM semantics, real WASAPI behavior and target-display/browser-speaker timing remain field-validation items.
