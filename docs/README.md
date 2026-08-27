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
- `design-docs/SPEECH_RECOGNITION.md` — Whisper/VAD/context/word timestamps.
- `design-docs/AUDIO_STABILITY.md` — WASAPI/PortAudio/startup safety.
- `design-docs/SPEAKER_IDENTIFICATION.md` — diarization, Voice ID, overlap, confidence.
- `design-docs/DION_INTEGRATION.md` — IAPI, mTLS, Secretary Bot control plane.
- `design-docs/PRIVACY_SECURITY.md` — local data, credentials, voice profiles, diagnostics.

## Execution plan
`exec-plans/CURRENT.md` is the single current plan; durable history belongs in `VERSION_JOURNAL.md`.

AI-specific files `AGENTS.md`, `CLAUDE.md` and `.claude/skills/*` are adapters/procedures, not duplicate knowledge bases.

## Current documentation baseline
The canonical docs describe the published **0.7.1 Hardening** release and its predecessor **0.7 Secretary Bot**. Exact artifact names, sizes and SHA-256 values must be taken from `RELEASES.md`. Windows/DION field validation must not be inferred from CI success; the remaining field-test scope is maintained in `exec-plans/CURRENT.md` and `ROADMAP.md`.
