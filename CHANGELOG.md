# Changelog

All notable user-visible changes to DION Meeting Assistant are recorded here.

## 0.7.1 Hardening — 2026-08-28

### Security / DION
- Added DION mTLS client certificate, PEM private key and optional encrypted-key password fields.
- DION credentials are captured before worker threads and remain memory-only.
- Persistent voice profiles no longer store participant name/e-mail; Windows persistence is DPAPI-protected.
- Secretary Bot invite is revoked on normal application shutdown when possible.
- Stale temporary Secretary Bot browser profiles are cleaned on later startup.

### Speaker accuracy / safety
- Diarization is off by default again until field CPU/stability is proven.
- Automatic Voice ID only considers currently active DION participants.
- Cross-meeting Voice ID thresholds are more conservative.
- When diarization is active, Whisper word timestamps split a recognized segment at speaker-handoff boundaries.

### Build / release
- Added exact Windows CI dependency lock.
- Pinned Whisper revision and speaker-model SHA-256 values.
- Pinned GitHub Actions by immutable commit SHA.
- PR builds do not publish releases.
- Published version tags/assets are not overwritten; an existing version requires a version bump.
- Published `v0.7.1` portable EXE after a green production Windows build and packaged self-test.

### Validation
- Reconstructed source before PR: **46 automated tests passing locally**; compileall passed.
- PR Windows CI run `33126146077` passed tests, model checks, EXE build and packaged self-test.
- Production Windows CI run `33126756679` passed tests, model checks, EXE build, packaged self-test and Release publication.
- Corporate DION/mTLS/WASAPI field validation remains required.

### Published artifact
- `DION_Meeting_Assistant_0.7.1_Hardening_Portable.exe`
- Size: `627,528,485 bytes`
- SHA-256: `90751e2d7a71a5bbcf3e3f0e185284ba08099244779ad8174f0afb89ada04239`

## 0.7 Secretary Bot — 2026-08-27
- Added `DION -> Секретарь-бот` connect/status/disconnect flow.
- Added individual DION invite with visible name `Секретарь-бот` and dedicated temporary browser profile.
- Added direct DION participant/session polling and bot-presence status.
- Added isolated sherpa-onnx diarization subprocess, no five-speaker application limit, and `[ПЕРЕБИВАНИЕ]` markers.
- Documented limitation: current documented IAPI is not a Windows/Python live active-speaker/per-user-media API.
- `v0.7-secretary-bot` passed Windows build and packaged self-test; exact artifact metadata is in `docs/RELEASES.md`.

## 0.6 Quality — 2026-08-27
- Offline Whisper small instead of base.
- Beam 5, bounded previous-utterance context, editable terminology/hotwords.
- Tuned VAD, adjacent segment merging and 12-second default chunks.
- Shared PortAudio safety from 0.5.1 preserved.

## 0.5.1 Safe — 2026-08-27
- Shared PortAudio context for loopback + microphone.
- Synchronized audio stream open/close.
- Safer startup and portable self-test.

## Earlier MVP line
Introduced local WASAPI/microphone capture, faster-whisper, live transcript, autosave/export, deterministic protocol, diagnostics and experimental speaker clustering.

User-visible changes update this file in the same task. Published binary facts belong in `docs/RELEASES.md`; detailed engineering chronology belongs in `docs/VERSION_JOURNAL.md`.
