# Changelog

All notable user-visible changes to DION Meeting Assistant are recorded here.

## 0.6 Quality — 2026-08-27

### Improved

- Replaced portable Whisper `base` with offline Whisper `small` for better Russian recognition.
- Increased decoding quality from greedy/beam 1 to beam search 5.
- Added short-term context from previous utterances between audio chunks.
- Added editable terminology/hotwords input for names, systems and project vocabulary.
- Added built-in support vocabulary such as DION, Naumen, Service Desk, КСПД, ALT Linux and WMS.
- Tuned VAD to preserve short Russian utterances more reliably.
- Added merging of neighboring short recognition segments.
- Increased default audio chunk length from 8 to 12 seconds.

### Preserved

- Shared PortAudio safety architecture from 0.5.1.
- Offline-first transcription.
- Diarization remains disabled by default for stability.

### Validation

- Reconstructed source: 25 automated tests passing.
- Windows packaged EXE self-test passed before GitHub Release publication.

## 0.5.1 Safe — 2026-08-27

### Fixed

- Fixed a likely native crash path when starting loopback + microphone capture with separate PortAudio contexts.
- Loopback and microphone now share one PortAudio context.
- Audio stream open/close behavior is synchronized.
- Startup errors are intended to return the UI to a safe state instead of terminating the application through ordinary Python failures.

### Changed

- Speaker diarization is disabled by default to reduce native-library risk during first field use.

### Validation

- Shared PortAudio context check passed on Windows CI.
- Packaged portable EXE self-test passed.

## 0.5 — 2026-08-27

### Added

- Preflight check before a meeting.
- Diagnostics tab and READY / OK / DEGRADED / CRITICAL health states.
- Bounded audio queue and dropped-chunk accounting.
- Recognition latency and queue metrics.
- Redacted technical diagnostic reports.
- Crash reports for unexpected Python-level failures.
- Field test checklist.

## 0.4

### Added

- Optional local Ollama protocol wording refinement.
- Loopback-only endpoint restriction for local AI.
- Structural validation so AI cannot arbitrarily invent/remove protocol items.

## Earlier MVP line

Initial versions introduced:

- Windows WASAPI loopback capture;
- separate local microphone capture;
- local faster-whisper transcription;
- timestamped live transcript;
- autosave and TXT/JSON/DOCX export;
- manual decision/task markers;
- deterministic meeting protocol;
- experimental remote-speaker clustering.

## Maintenance rule

Every future user-visible behavior change must update this file in the same task. Architecture-only refactors that do not change behavior may be omitted here but must still update the relevant technical documentation.
