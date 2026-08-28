# DION Meeting Assistant — project memory

Keep this file short. Canonical facts live in `docs/`.

## Before changing anything
1. Read `docs/PROJECT_MAP.md`.
2. Read the latest relevant `docs/VERSION_JOURNAL.md` entries.
3. Read only the matching design doc or `docs/DEVELOPMENT.md`.
4. Inspect mapped code/build files only.
5. Use `.claude/skills/` when a Skill matches the task.

## Build-branch model
`dion-exe-build` applies `apply_051.py` -> `apply_060.py` -> `apply_070.py` -> `apply_071.py` -> `apply_080.py` -> `apply_090.py` to the reconstructed base source. Do not read every encoded part merely to orient yourself.

## Current rules
- Published baseline is **0.8 Visual Refresh**; current development candidate is **0.9 Guest Secretary Bot**.
- Windows 10/11 x64; local/offline STT.
- Preserve shared PortAudio safety and approved 0.8 visual language.
- 0.9 primary DION flow is ordinary HTTPS `/join/<slug>` guest URL; `event_id`/token/mTLS are not required just to open the bot.
- Corporate/on-prem DION hosts are valid; do not hard-code `dion.vc`.
- Integration API is optional enrichment. Slug API metadata is not proof of current live presence.
- Browser automation/probing is localhost-only, capability-gated and must fall back to visible manual guest entry.
- Do not infer active speaker from CSS color, generic text or microphone-enabled state.
- Browser active-speaker indications do not yet retroactively relabel delayed Whisper chunks until field timing is calibrated.
- DION/browser/speaker failures must not terminate local transcription.
- DION credentials/mTLS material remain memory-only.
- Diarization is opt-in until field evidence exists.
- Names require confirmed/sufficiently confident matching; do not promote roster identity to speech identity without evidence.
- Persistent voice profiles are opt-in; Windows persistence uses DPAPI and excludes name/e-mail.
- Published version assets are not clobbered; bump version instead.
- Diagnostics never contain meeting text/audio/tokens/meeting URLs/invite secrets/private-key passwords.

## Validation
Current published release: **v0.8-visual-refresh**.

0.9 reconstructed source passes **36/36 tests** plus compileall. Windows PR CI, packaged EXE self-test and production Release are still pending until recorded in canonical docs. Corporate DION guest form/DOM semantics/WASAPI and target-display behavior remain field-unverified.

Every significant change must update `docs/VERSION_JOURNAL.md` and affected docs in the same task. Do not claim field validation until it has actually been performed.
