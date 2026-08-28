# Current execution plan

## Objective

Field-validate the now-published **DION Meeting Assistant 0.8 Visual Refresh** release on the target Windows workstation and in a real corporate DION meeting, then use evidence/screenshots/metrics to choose the next engineering iteration.

## Current state

Completed:

- 0.8 visual redesign implemented as native PySide6 widgets/QSS on top of 0.7.1 Hardening;
- canonical `docs/design-docs/UI_VISUAL_SYSTEM.md` added;
- PR #2 (`Implement 0.8 Visual Refresh`) merged into `dion-exe-build`;
- visual PR Windows CI run `33129215245` passed source checks, Qt offscreen `MainWindow` smoke, pinned models, EXE build and packaged `--portable-selftest`;
- initial production run `33129501062` passed application/model/build/self-test gates but failed only at the old release-existence probe;
- PR #3 fixed that release guard without changing application code;
- release-guard PR CI run `33145190036` passed;
- final production Windows CI run `33145419554` passed all gates including Release publication and Actions artifact upload;
- GitHub Release `v0.8-visual-refresh` was published successfully;
- artifact: `DION_Meeting_Assistant_0.8_Visual_Refresh_Portable.exe`;
- size: `627,541,530 bytes`;
- SHA-256: `0ea963916ecf00d9bf9ef219377e709718d1c5d458ec656fc54f5527d43f3fa9`;
- release page: `https://github.com/Zios86/test/releases/tag/v0.8-visual-refresh`;
- direct asset: `https://github.com/Zios86/test/releases/download/v0.8-visual-refresh/DION_Meeting_Assistant_0.8_Visual_Refresh_Portable.exe`.

## What is implemented in 0.8

- modern light Windows-style native Qt shell;
- left navigation with seven logical pages;
- card-based live transcript instead of the old console-like text block;
- active-speaker and overlap/interruption card states;
- top status bar for meeting/recording/audio/DION state;
- right summary rail for participants, speaker, audio quality, protocol draft and hotwords;
- persistent bottom action bar for start/stop, DOCX export and protocol access;
- dedicated Secretary Bot visual card and page;
- existing DION/mTLS, Voice ID, diagnostics, protocol and recognition controls preserved and redistributed;
- all 0.7.1 privacy/stability invariants retained.

## Still requires field validation

CI/self-test does **not** prove:

- that the new UI renders exactly as intended on the user's target Windows resolution, DPI and scaling;
- that every navigation/control path is comfortable during a long meeting;
- real corporate DION mTLS authorization with production certificates;
- Secretary Bot behavior in an actual corporate meeting;
- long-duration WASAPI loopback + microphone stability on user endpoints;
- real speaker-attribution accuracy with overlapping speech;
- real Russian WER/CER improvement on reference audio.

These remain explicitly unverified until tested on the target Windows/DION environment.

## Next evidence to collect

1. Download and launch the published 0.8 EXE on the target PC.
2. Capture screenshots of the initial window, live transcript and settings at the actual Windows scaling/DPI.
3. Verify all seven navigation pages and bottom actions.
4. Connect with the corporate DION token and mTLS material.
5. Test Secretary Bot join, participant roster, revoke and waiting-room behavior.
6. Test system audio + microphone together on the target PC.
7. Test 2/5/10 participant meetings with rapid speaker switching and overlap.
8. Run a 60+ minute meeting and inspect queue depth, dropped chunks and latency.
9. Measure false accepts/rejects for Voice ID and diarization errors.
10. Where permitted, compare recognition against a manually corrected reference transcript using WER/CER.

## Candidate next work

After field evidence, prioritize one or more of:

- UI responsive/high-DPI polish based on real 0.8 screenshots;
- asynchronous speaker analysis;
- PFX/P12 certificate support if required by corporate provisioning;
- approved terminology profiles;
- confidence/review markers;
- optional final-pass transcript refinement;
- migration from encoded `part* + apply_*.py` release tree to a normal source tree;
- Authenticode/corporate packaging.

## Repository administration

GitHub currently reports repository `Zios86/test` as `public`. If this project is intended to remain private, repository visibility must be changed in GitHub settings; the available connector actions in this session do not expose a repository-visibility mutation.

## Update rule

This file is the active plan, not a historical diary. Durable completed facts belong in `ROADMAP.md`, `RELEASES.md`, `CHANGELOG.md`, design docs and the append-only `VERSION_JOURNAL.md`.
