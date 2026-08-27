# Releases

This file records published artifacts and the engineering meaning of each release. GitHub Release is the source for the actual downloadable binary; this document is the project index.

For chronological engineering history, including unreleased significant updates, use `VERSION_JOURNAL.md`.

## v0.7.1 — current release

Status: **published** on 2026-08-28 (Europe/Tallinn local date; GitHub published at 2026-08-27 23:39:19 UTC).

Artifact:

```text
DION_Meeting_Assistant_0.7.1_Hardening_Portable.exe
```

Size:

```text
627,528,485 bytes
```

SHA-256:

```text
90751e2d7a71a5bbcf3e3f0e185284ba08099244779ad8174f0afb89ada04239
```

Release page:

```text
https://github.com/Zios86/test/releases/tag/v0.7.1
```

Direct asset:

```text
https://github.com/Zios86/test/releases/download/v0.7.1/DION_Meeting_Assistant_0.7.1_Hardening_Portable.exe
```

Target commit:

```text
a8f8a08d1f80f25fa6281ec16fe171e5ac788776
```

Key behavior:

- DION mTLS client certificate + PEM key + optional key password support;
- diarization is opt-in by default;
- Voice ID candidates are limited to active DION participants;
- persistent voice-profile payload omits participant name/e-mail and is protected with Windows DPAPI;
- Secretary Bot invite is revoked on normal shutdown when possible;
- stale temporary Secretary Bot browser profiles are cleaned;
- Whisper word timestamps are enabled only with diarization and can split text at speaker handoffs;
- more conservative cross-meeting voice-match thresholds;
- locked Windows CI dependency set and pinned model inputs;
- release workflow refuses to overwrite an existing `v0.7.1` tag.

Build validation:

- PR Windows build `33126146077`: tests, locked dependencies, pinned models, EXE build and packaged self-test passed;
- production push build `33126756679`: tests, pinned models, EXE build and packaged self-test passed before Release publication;
- Release publication step completed successfully.

Still requires field validation on corporate DION/mTLS, real WASAPI endpoints and real speaker-attribution accuracy.

Journal entry: `VERSION_JOURNAL.md` -> `2026-08-28.02`.

## v0.7-secretary-bot

Status: **published**.

Artifact:

```text
DION_Meeting_Assistant_0.7_Secretary_Bot_Portable.exe
```

Size:

```text
627,522,154 bytes
```

SHA-256:

```text
704dfcab816ac687f592baa6ff6c0feea785cd24b920eaf7594fe5e0364a00da
```

Release page:

```text
https://github.com/Zios86/test/releases/tag/v0.7-secretary-bot
```

Key behavior:

- `DION -> Секретарь-бот` connect/status/disconnect flow;
- individual DION invite with visible guest name `Секретарь-бот`;
- dedicated temporary browser profile;
- participant/session polling through documented DION IAPI;
- isolated local sherpa-onnx diarization fallback;
- no five-speaker application limit;
- overlap marker `[ПЕРЕБИВАНИЕ]`;
- 0.6 Quality STT behavior retained.

Known limitation:

- documented IAPI does not provide a Windows/Python live active-speaker user ID or separate per-user live audio track;
- real corporate DION permissions/join behavior require field validation.

Journal entry: `VERSION_JOURNAL.md` -> `2026-08-28.01`.

## v0.6-quality

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
7. Published version tags/assets must not be overwritten; bump the version instead.

Do not overwrite historical facts about an old release to match a newer implementation.
