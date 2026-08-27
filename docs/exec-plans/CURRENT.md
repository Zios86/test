# Current execution plan

## Objective

Harden version 0.6 speech-recognition quality using real DION field evidence while preserving 0.5.1 audio stability.

## Current state

Completed:

- 0.6 Quality implementation exists;
- Whisper small is bundled in the portable release;
- beam 5/context/hotwords/VAD improvements are implemented;
- source test baseline is 25 passing tests;
- packaged Windows EXE self-test passed;
- `v0.6-quality` is published;
- project documentation has been reorganized for Claude/OpenAI cross-AI use.

Not yet proven:

- quantified WER/CER improvement on reference audio;
- long-duration stability on multiple corporate Windows endpoints;
- safe default diarization.

## Next evidence to collect

1. Run a representative DION meeting with 0.6.
2. Export the transcript JSON.
3. Preferably keep a local reference audio or manually corrected reference transcript for accuracy measurement.
4. Classify errors using `design-docs/SPEECH_RECOGNITION.md` taxonomy.
5. Decide whether the next release should focus on:
   - terminology persistence;
   - chunk-boundary handling;
   - low-confidence review;
   - session-end reprocessing.

## Planned implementation candidate: 0.6.1

### A. Persistent approved terminology

- local dictionary storage;
- separate global and per-meeting terms;
- import/export;
- no automatic learning of arbitrary sensitive phrases.

### B. Recognition confidence/review

- flag suspicious/low-confidence fragments;
- make uncertainty visible in UI/export without deleting original text.

### C. Final-pass mode

Explore an optional post-meeting pass that can use broader context for final transcript quality while keeping live recognition incremental.

Privacy condition: if audio retention is required, it must be explicit/temporary and deleted according to documented policy.

## Out of scope for the immediate quality pass

- cloud STT;
- automatic persistent biometric speaker identity;
- enabling diarization by default;
- automatically sending transcript/protocol to external systems;
- major UI redesign unrelated to quality.

## Completion criteria for next quality iteration

- representative field evidence collected;
- error categories documented;
- automated tests added for deterministic quality logic;
- Windows build/self-test passes;
- documentation updated in the same change;
- release notes distinguish measured improvement from expected improvement.

## Update rule

This file is not a diary. Rewrite it when the active objective changes. Move completed durable facts to `ROADMAP.md`, `RELEASES.md`, `CHANGELOG.md` or a design doc.
