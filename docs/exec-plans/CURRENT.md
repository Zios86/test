# Current execution plan

## Objective

Field-validate the now-published **DION Meeting Assistant 0.7.1 Hardening** release and use real evidence to choose the next engineering iteration.

## Current state

Completed:

- PR #1 merged into `dion-exe-build` at commit `a8f8a08d1f80f25fa6281ec16fe171e5ac788776`;
- PR Windows CI run `33126146077` passed tests, dependency validation, pinned-model validation, EXE build and packaged `--portable-selftest`;
- production Windows CI run `33126756679` repeated the release gates successfully;
- GitHub Release `v0.7.1` was published successfully;
- artifact: `DION_Meeting_Assistant_0.7.1_Hardening_Portable.exe`;
- size: `627,528,485 bytes`;
- SHA-256: `90751e2d7a71a5bbcf3e3f0e185284ba08099244779ad8174f0afb89ada04239`;
- release page: `https://github.com/Zios86/test/releases/tag/v0.7.1`;
- canonical release metadata is recorded in `docs/RELEASES.md`.

## What is implemented in 0.7.1

- DION mTLS client certificate, PEM key and optional key password support;
- diarization opt-in by default;
- Voice ID candidates constrained to active DION participants;
- persistent voice-profile payload without participant name/e-mail and protected with Windows DPAPI;
- Secretary Bot invite revocation on normal shutdown when possible;
- stale temporary Secretary Bot browser-profile cleanup;
- Whisper word timestamps used only with diarization and speaker-handoff text splitting;
- more conservative cross-meeting voice-match thresholds;
- locked CI dependencies and pinned model inputs;
- versioned release policy that refuses to overwrite an existing `v0.7.1` tag.

## Still requires field validation

CI/self-test does **not** prove:

- real corporate DION mTLS authorization with production certificates;
- Secretary Bot behavior in an actual corporate meeting;
- long-duration WASAPI loopback + microphone stability on user endpoints;
- real speaker-attribution accuracy with overlapping speech;
- real Russian WER/CER improvement on reference audio.

These remain explicitly unverified until tested on the target Windows/DION environment.

## Next evidence to collect

1. Connect with the corporate DION token and mTLS material.
2. Test Secretary Bot join, participant roster, revoke and waiting-room behavior.
3. Test system audio + microphone together on the target PC.
4. Test 2/5/10 participant meetings with rapid speaker switching and overlap.
5. Run a 60+ minute meeting and inspect queue depth, dropped chunks and latency.
6. Measure false accepts/rejects for Voice ID and diarization errors.
7. Where permitted, compare recognition against a manually corrected reference transcript using WER/CER.

## Candidate next work

After field evidence, prioritize one or more of:

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
