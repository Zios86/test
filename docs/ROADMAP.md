# Roadmap

## Current line
**0.9 Guest Secretary Bot — development candidate**.

Primary goal: make ordinary DION guest entry work from a normal `/join/<slug>` room URL without forcing the user to obtain `event_id`, token or mTLS merely to open `Секретарь-бот`.

Current published release remains **0.8 Visual Refresh**. Published artifact metadata stays in `RELEASES.md`.

## Completed foundation
- **0.5.1 Safe:** shared PortAudio context and safer startup.
- **0.6 Quality:** Whisper small, beam 5, context/hotwords, tuned VAD, 12-second chunks.
- **0.7 Secretary Bot:** named DION invite, participant/session roster, visible guest browser, isolated local speaker fallback and overlap markers.
- **0.7.1 Hardening:** mTLS configuration, safer Voice ID persistence/matching, opt-in diarization, speaker-handoff text splitting, pinned build inputs and non-clobber release policy.
- **0.8 Visual Refresh:** modern native PySide6 shell, seven-page navigation, card transcript, live status/summary/action panels and canonical UI design system.

## 0.9 implemented in development branch
- normal HTTPS DION `/join/<slug>` URL is primary Guest Bot input;
- corporate/on-prem DION hostnames are accepted without hard-coded public hostname;
- slug is extracted automatically;
- `event_id` removed from normal user-facing guest flow;
- token/mTLS/API base moved to optional advanced settings;
- isolated Edge/Chrome guest session with muted bot audio;
- local `127.0.0.1` DevTools adapter for best-effort name fill and `Войти как гость` click;
- visible manual guest fallback when automation is unavailable;
- optional IAPI participant metadata by slug;
- slug metadata explicitly does not claim current live presence;
- conservative browser participant/speaker probe accepts only explicit data/ARIA semantics;
- no speaker inference from color, generic text or microphone-enabled state;
- browser live-speaker indicator is not yet used to retroactively relabel delayed Whisper chunks;
- 0.8 visual shell, 0.7.1 hardening and WASAPI-based offline STT remain intact.

## Current validation
Reconstructed 0.8 + 0.9 patch:

```text
36/36 tests passed
compileall passed
```

Windows PR CI, packaged EXE self-test, merge and production Release are still pending until recorded in the journal/release docs.

## Next release gates for 0.9
1. Open PR from `dion-guest-bot-0.9` to `dion-exe-build`.
2. Pass Windows reconstruct + locked dependencies.
3. Pass 0.9 source tests and Qt offscreen `MainWindow` smoke.
4. Verify pinned offline models.
5. Build `DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe`.
6. Pass packaged `--portable-selftest`.
7. Merge only after green PR build.
8. Pass production build and publish immutable `v0.9-guest-secretary-bot`.
9. Record actual artifact size/SHA-256 in `RELEASES.md` and a released journal entry.

## Required field evidence after build
1. Real corporate room URL parsing on target Windows PC.
2. Guest Bot entry without token/mTLS.
3. Edge/Chrome isolated profile and muted bot audio.
4. Auto-name/guest-click success or visible manual fallback.
5. Real corporate DION participant DOM semantics.
6. Real corporate active-speaker DOM/data/ARIA semantics, if any.
7. Timing comparison between browser speaker events and captured WASAPI audio/Whisper timestamps.
8. Optional corporate IAPI base/token/mTLS and slug metadata.
9. Real WASAPI + mic stability and 2/5/10 participant speaker tests.
10. 60+ minute queue/latency/drop run.

## Engineering after 0.9 field evidence
- browser-event ↔ audio/STT clock alignment before transcript relabeling;
- robust browser adapter compatibility profiles only if the real DION DOM exposes stable semantics;
- asynchronous speaker analysis so diarization cannot block STT queue latency;
- PFX/P12 client-certificate support if corporate provisioning requires it;
- direct DION active-speaker/per-user media only if officially provided/authorized;
- terminology profiles / constrained final-pass transcript refinement;
- normal source-tree migration from encoded `part* + apply_*.py`;
- Authenticode signing and corporate packaging.

## Explicit non-goals / claims not yet proven
- Guest Bot does not supply a separate per-user audio stream to Whisper.
- Browser participant/speaker DOM semantics are not assumed stable before field inspection.
- Slug IAPI roster is not current-presence proof.
- `microphone enabled` is not speaking evidence.
- CI does not prove real corporate DION/WASAPI/browser behavior.

## GitHub administration
Repository privacy, default branch and branch protection are external settings. GitHub has reported the repository as `public`; if it must be private, change visibility in GitHub settings. Never commit real meeting URLs, credentials, keys, transcripts or participant data regardless of visibility.

## Definition of done for 0.9 release
0.9 is `released` only when:
- implementation exists;
- source and Windows release gates pass;
- user-visible behavior is documented;
- limitations are explicit;
- GitHub Release actually exists;
- exact uploaded EXE size/SHA-256 are recorded in `RELEASES.md` and `VERSION_JOURNAL.md`;
- previous published fallback remains documented.
