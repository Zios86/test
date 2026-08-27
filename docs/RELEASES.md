# Releases

This file records published artifacts and the engineering meaning of each release. GitHub Release is the source for the actual downloadable binary; this document is the project index.

For chronological engineering history, including unreleased significant updates, use `VERSION_JOURNAL.md`.

## v0.6-quality — current quality release

Status: **published**.

Artifact:

```text
DION_Meeting_Assistant_0.6_Quality_Portable.exe
```

Size:

```text
621,933,502 bytes
```

SHA-256:

```text
85a8d0b443b4e07c6b5df16b255775ed7c960da5cc0ddc9e9bab51a5d3658334
```

Release page:

```text
https://github.com/Zios86/test/releases/tag/v0.6-quality
```

Key behavior:

- offline faster-whisper `small` bundled;
- beam search 5;
- prior transcript context;
- editable hotwords/domain dictionary;
- tuned VAD;
- 12-second default chunks;
- shared PortAudio stability fix retained;
- diarization off by default.

Build validation:

- Quality source checks passed in Windows CI;
- packaged EXE self-test passed before publication;
- reconstructed source baseline: 25 tests passing.

Still requires field validation on real DION audio for recognition quality.

Journal entry: `VERSION_JOURNAL.md` -> `2026-08-27.03`.

## v0.5.1-safe — stability fallback

Status: **published** and retained as a fallback.

Purpose:

- fix application closing when transcription starts;
- use one shared PortAudio context for loopback and microphone;
- reduce simultaneous risky native-library initialization;
- keep diarization disabled by default.

Known quality limitation:

- portable build used Whisper `base` to keep artifact size smaller;
- real transcript testing showed unacceptable Russian/technical vocabulary errors, which motivated 0.6.

Release page:

```text
https://github.com/Zios86/test/releases/tag/v0.5.1-safe
```

Journal entry: `VERSION_JOURNAL.md` -> `2026-08-27.02`.

## Release policy

For every future release:

1. Tests and build checks must pass.
2. Packaged EXE self-test must pass on Windows.
3. Actual uploaded binary SHA-256 must be recorded here and in GitHub Release notes.
4. A released entry must be appended to `VERSION_JOURNAL.md` with validation, limitations and rollback/fallback information.
5. `CHANGELOG.md` and `ROADMAP.md` must be updated in the same task.
6. Field-tested claims and CI-only claims must be distinguished explicitly.

Do not overwrite historical facts about an old release to match a newer implementation.
