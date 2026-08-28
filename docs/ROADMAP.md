# Roadmap

## Current line
**0.9 Guest Secretary Bot — published**.

**0.9.1 Browser Gate Hotfix — implemented/tested, release validation pending**. It handles the field-confirmed «Продолжить в браузере» gate before the guest-name form.

Release: `v0.9-guest-secretary-bot`. Exact artifact metadata is in `RELEASES.md`.

## Completed foundation
- **0.5.1 Safe:** shared PortAudio context and safer startup.
- **0.6 Quality:** Whisper small, beam 5, context/hotwords, tuned VAD, 12-second chunks.
- **0.7 Secretary Bot:** named DION invite, participant/session roster, isolated local speaker fallback and overlap markers.
- **0.7.1 Hardening:** mTLS configuration, safer Voice ID persistence/matching, opt-in diarization, speaker-handoff text splitting, pinned build inputs and non-clobber release policy.
- **0.8 Visual Refresh:** modern native PySide6 shell, seven-page navigation, card transcript and canonical visual system.
- **0.9 Guest Secretary Bot:** ordinary `/join/<slug>` room URL as primary entry; corporate/on-prem hosts; no mandatory event_id/token/mTLS for guest entry; isolated Edge/Chrome session; localhost-only best-effort guest automation; optional configurable IAPI; conservative browser participant/speaker probe.

## 0.9 release validation
- Reconstructed source: `36/36 tests passed` + compileall.
- PR Windows CI `33150603611`: source validation, pinned models, EXE build and packaged self-test passed.
- Production Windows CI `33150927129`: all release gates passed and GitHub Release published.
- Artifact: `DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe`.
- Size: `627,722,376 bytes`.
- SHA-256: `3e57b7c1fac965a14518d6eecc86642bcd3367af1fcf66af01e71142c09aef22`.

## Immediate next evidence: corporate DION field validation
1. Real corporate `/join/<slug>` URL parsing.
2. Guest Bot entry without token/mTLS.
3. Isolated Edge/Chrome profile and muted bot audio.
4. Automatic guest-name/click behavior and manual fallback.
5. Actual corporate DION participant DOM/data/ARIA semantics.
6. Actual active-speaker semantics, if any.
7. Browser speaker-event ↔ WASAPI/Whisper timing calibration.
8. Optional corporate IAPI base/token/mTLS and slug metadata.
9. WASAPI + mic stability with 2/5/10 participants and overlap.
10. 60+ minute queue/latency/drop run.

## Engineering after field evidence
- browser-event ↔ audio/STT clock alignment before transcript relabeling;
- compatibility profiles only if real DION DOM exposes stable semantics;
- asynchronous speaker analysis so diarization cannot block STT queue latency;
- PFX/P12 client-certificate support if corporate provisioning requires it;
- direct DION active-speaker/per-user media only if officially provided/authorized;
- terminology profiles and constrained final-pass transcript refinement;
- normal source-tree migration from encoded `part* + apply_*.py`;
- Authenticode signing and corporate packaging.

## Explicit limitations
- Guest Bot does not supply a separate per-user DION audio stream to Whisper.
- Slug IAPI roster is not current-presence proof.
- `microphone enabled` is not speaking evidence.
- Browser speaker state is not retrospectively applied to delayed transcript chunks until timing is calibrated.
- CI does not prove real corporate DION/WASAPI/browser behavior.

## Rollback
Primary published rollback: `v0.8-visual-refresh`.

## GitHub administration
Repository privacy, default branch and branch protection are external settings. GitHub has reported the repository as `public`; if it must be private, change visibility in GitHub settings. Never commit real meeting URLs, credentials, keys, transcripts or participant data regardless of visibility.
