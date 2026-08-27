# Roadmap

## Current line
**0.7.1 Hardening** — security/stability/release hardening of the 0.7 Secretary Bot architecture. Local source validation baseline: 46 tests. Windows CI + packaged self-test are required before marking it published.

## Completed foundation
- **0.5.1 Safe:** shared PortAudio context and safer startup.
- **0.6 Quality:** Whisper small, beam 5, context/hotwords, tuned VAD, 12-second chunks.
- **0.7 Secretary Bot:** named DION invite, participant/session roster, visible guest browser, isolated local speaker fallback and overlap markers.

## 0.7.1 hardening goals
Implemented in the release candidate:
- mTLS certificate/key/password configurable in UI;
- diarization opt-in by default;
- Voice ID only against active DION participants;
- DPAPI-protected persistent voice profiles without name/e-mail;
- invite revoke on normal close and stale guest-profile cleanup;
- word timestamps split Whisper text at speaker handoffs;
- conservative Voice-ID thresholds;
- locked dependencies and pinned model hashes/revisions;
- immutable release assets/tags;
- PR CI before release publication.

## Next evidence: field hardening
1. Corporate DION token + mTLS connection test.
2. Secretary Bot room join/roster/revoke/waiting-room behavior.
3. WASAPI + mic coexistence on target PCs.
4. 2/5/10 participant tests with rapid speaker changes and overlap.
5. 60+ minute latency/queue/drop measurement.
6. Calibrate Voice ID false accepts/rejects and diarization error.

## Next engineering candidates
- asynchronous speaker analysis so diarization cannot extend live STT queue latency;
- PFX/P12 client-certificate support if corporate provisioning requires it;
- direct DION active-speaker/per-user media only if DION documents/provides it;
- approved terminology profiles and optional final-pass transcript refinement;
- migrate encoded `part* + apply_*.py` build tree to a normal source tree;
- Authenticode signing and corporate package format.

## GitHub administration
Repository privacy, default branch and branch protection are external settings. During the audit the repository was observed public; this must be manually corrected/verified before treating repository contents/releases as restricted.
