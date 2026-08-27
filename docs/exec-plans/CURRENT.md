# Current execution plan

## Objective

Complete and publish **DION Meeting Assistant 0.7.1 Hardening** after the 0.7 Secretary Bot release, while preserving offline STT quality and the shared-PortAudio stability architecture.

## Current state

Completed:

- `v0.7-secretary-bot` is published;
- DION Secretary Bot integration is implemented;
- 0.7.1 hardening patch is implemented on `hardening-0.7.1`;
- mTLS client certificate/key/password support is wired into DION integration;
- diarization remains opt-in by default;
- active DION participants are used to constrain voice identity candidates;
- persistent voice profiles omit participant name/e-mail and use Windows DPAPI protection;
- stale Secretary Bot guest profiles are cleaned and normal shutdown revokes the invite;
- mixed-speaker Whisper output can be split using word timestamps at diarization handoffs;
- dependency versions and model inputs are pinned for CI/release builds;
- GitHub Release workflow refuses to overwrite an existing `v0.7.1` tag;
- Windows PR CI passed application tests, dependency checks, pinned-model validation, EXE build and packaged `--portable-selftest`.

Validated PR build:

```text
GitHub Actions run: 33126146077
Result: success
```

The Release publication step was intentionally skipped in the pull-request run. Publication happens only after merge/push to `dion-exe-build`.

## Release sequence now

1. Keep documentation synchronized with the implemented 0.7.1 behavior.
2. Merge PR #1 into `dion-exe-build` only after the green PR build.
3. Let the production push workflow reconstruct, test and build the Windows EXE again.
4. Require packaged `--portable-selftest` to pass.
5. Publish immutable tag `v0.7.1` and artifact:

```text
DION_Meeting_Assistant_0.7.1_Hardening_Portable.exe
```

6. Read the actual GitHub Release asset size and SHA-256.
7. Record those exact values in `docs/RELEASES.md` and append a released entry to `docs/VERSION_JOURNAL.md`.

## Still requires field validation

CI/self-test does **not** prove:

- real corporate DION mTLS authorization with production certificates;
- Secretary Bot behavior in an actual corporate meeting;
- long-duration WASAPI loopback + microphone stability on user endpoints;
- real speaker-attribution accuracy with overlapping speech;
- real Russian WER/CER improvement on reference audio.

These claims must remain marked as field-test pending until verified on actual Windows/DION infrastructure.

## Privacy/security follow-up

Repository visibility must be checked separately from application security. If the project repository is intended to be private, GitHub repository settings must show `visibility: private`; source code must never contain DION tokens, private keys, passwords, real participant data, or meeting transcripts.

## Next engineering work after 0.7.1

Priority order:

1. corporate DION/mTLS field test;
2. real Secretary Bot lifecycle test;
3. speaker-attribution evaluation using sanitized/reference material;
4. recognition WER/CER comparison;
5. decide whether the next release focuses on terminology persistence, confidence review, final-pass transcription, or further speaker isolation.

## Update rule

This file is the active plan, not a historical diary. Durable completed facts belong in `ROADMAP.md`, `RELEASES.md`, `CHANGELOG.md`, design docs and the append-only `VERSION_JOURNAL.md`.
