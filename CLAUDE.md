# DION Meeting Assistant — project memory

Keep this file short. Canonical facts live in `docs/`.

## Before changing anything
1. Read `docs/PROJECT_MAP.md`.
2. Read the latest relevant `docs/VERSION_JOURNAL.md` entries.
3. Read only the matching design doc or `docs/DEVELOPMENT.md`.
4. Inspect mapped code/build files only.
5. Use `.claude/skills/` when a Skill matches the task.

## Build-branch model
`dion-exe-build` applies `apply_051.py` -> `apply_060.py` -> `apply_070.py` -> `apply_071.py` to the reconstructed base source. Do not read every encoded part merely to orient yourself.

## Current rules
- Windows 10/11 x64; local/offline STT.
- Preserve shared PortAudio safety.
- DION control-plane failure or speaker subprocess failure must not terminate transcription.
- DION IAPI credentials/mTLS material remain memory-only.
- Diarization is opt-in by default until field evidence exists.
- Names require confirmed/sufficiently confident voice matching; active DION roster only.
- Persistent voice profiles are opt-in; Windows persistence uses DPAPI and excludes name/e-mail.
- Published releases are immutable; bump version instead of clobbering assets.
- Diagnostics never contain meeting text/audio/tokens/invite secrets.

## Validation
Current published release: **v0.7.1 Hardening**.

0.7.1 reconstructed-source baseline: **46 passing tests locally**. The published release also passed Windows PR CI, production CI, pinned-model verification, EXE build and packaged self-test. Corporate DION/mTLS/WASAPI field behavior remains unverified until an actual field test is recorded.

Every significant change must update `docs/VERSION_JOURNAL.md` and affected docs in the same task. Do not claim corporate DION/WASAPI field validation until it has actually been performed.
