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
- `VERSION_JOURNAL.md` — append-only engineering history, including unreleased work.
- `AI_HANDOFF.md` — cross-AI handoff.
- `ROADMAP.md` — current direction/future work.
- `RELEASES.md` — actually published binaries and hashes; never treat a candidate as a release.

## Design docs
- `design-docs/UI_VISUAL_SYSTEM.md` — canonical 0.8+ UI/visual system and 0.9 guest-flow placement.
- `design-docs/SPEECH_RECOGNITION.md` — Whisper/VAD/context/word timestamps.
- `design-docs/AUDIO_STABILITY.md` — WASAPI/PortAudio/startup safety.
- `design-docs/SPEAKER_IDENTIFICATION.md` — diarization, Voice ID, overlap, confidence.
- `design-docs/DION_INTEGRATION.md` — 0.9 room-URL-first Guest Bot, browser adapter, optional IAPI/mTLS.
- `design-docs/PRIVACY_SECURITY.md` — local data, credentials, guest-browser DevTools, voice profiles and diagnostics.

## Execution plan
`exec-plans/CURRENT.md` is the single current plan; durable history belongs in `VERSION_JOURNAL.md`.

AI-specific files `AGENTS.md`, `CLAUDE.md` and `.claude/skills/*` are adapters/procedures, not duplicate knowledge bases.

## Current documentation baseline

- Published release: **0.8 Visual Refresh**.
- Current development candidate: **0.9 Guest Secretary Bot** on `dion-guest-bot-0.9`.
- 0.9 source status: **36/36 tests + compileall passed** on reconstructed 0.8 + `apply_090.py`.
- 0.9 Windows PR CI / packaged EXE / production Release: **pending until explicitly recorded**.
- Exact published artifact names/sizes/SHA values come only from `RELEASES.md`.
- Corporate DION guest-form/DOM semantics, real WASAPI behavior and target-display usability must not be inferred from CI success.
