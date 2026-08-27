# Roadmap

## Current line
**0.7.1 Hardening** — published security/stability/release hardening of the 0.7 Secretary Bot architecture.

Published artifact and exact SHA-256 are recorded in `RELEASES.md`.

## Completed foundation
- **0.5.1 Safe:** shared PortAudio context and safer startup.
- **0.6 Quality:** Whisper small, beam 5, context/hotwords, tuned VAD, 12-second chunks.
- **0.7 Secretary Bot:** named DION invite, participant/session roster, visible guest browser, isolated local speaker fallback and overlap markers.
- **0.7.1 Hardening:** mTLS configuration, safer Voice ID persistence/matching, opt-in diarization, speaker-handoff text splitting, pinned build inputs and immutable versioned release policy.

## 0.7.1 release validation
- PR Windows build `33126146077` passed tests, locked dependencies, pinned models, EXE build and packaged self-test.
- Production Windows build `33126756679` passed the same release gates and successfully published `v0.7.1`.
- Corporate DION/mTLS/WASAPI behavior is still field-test pending and must not be treated as proven by CI alone.

## Next evidence: field hardening
1. Corporate DION token + mTLS connection test.
2. Secretary Bot room join/roster/revoke/waiting-room behavior.
3. WASAPI + mic coexistence on target PCs.
4. 2/5/10 participant tests with rapid speaker changes and overlap.
5. 60+ minute latency/queue/drop measurement.
6. Calibrate Voice ID false accepts/rejects and diarization error.
7. Compare Russian recognition against a corrected reference transcript using WER/CER where permissible.

## Next engineering candidates
- asynchronous speaker analysis so diarization cannot extend live STT queue latency;
- PFX/P12 client-certificate support if corporate provisioning requires it;
- direct DION active-speaker/per-user media only if DION documents/provides it;
- approved terminology profiles and optional final-pass transcript refinement;
- migrate encoded `part* + apply_*.py` build tree to a normal source tree;
- Authenticode signing and corporate package format.

## GitHub administration
Repository privacy, default branch and branch protection are external settings. The repository is currently reported by GitHub as `public`; if it is intended to be restricted, this must be changed in GitHub repository settings. Application credentials, private keys, real participant data and meeting transcripts must never be committed regardless of repository visibility.

## Definition of done for the next release
An item moves to completed only when:
- implementation exists;
- relevant automated checks pass;
- Windows/DION-specific limitations are stated;
- affected documentation is updated;
- user-visible behavior is in `CHANGELOG.md`;
- exact Release metadata and SHA-256 are recorded after publication.
