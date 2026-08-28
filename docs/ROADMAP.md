# Roadmap

## Current line
**0.8 Visual Refresh** — published native PySide6/QSS redesign on top of the 0.7.1 Hardening behavior.

Published artifact and exact SHA-256 are recorded in `RELEASES.md`.

## Completed foundation
- **0.5.1 Safe:** shared PortAudio context and safer startup.
- **0.6 Quality:** Whisper small, beam 5, context/hotwords, tuned VAD, 12-second chunks.
- **0.7 Secretary Bot:** named DION invite, participant/session roster, visible guest browser, isolated local speaker fallback and overlap markers.
- **0.7.1 Hardening:** mTLS configuration, safer Voice ID persistence/matching, opt-in diarization, speaker-handoff text splitting, pinned build inputs and versioned non-clobber release policy.
- **0.8 Visual Refresh:** modern application shell, seven-page left navigation, card transcript, active/overlap visual states, top status bar, right summary rail, bottom quick actions and canonical UI visual system.

## 0.8 release validation
- Local visual-refresh workspace: 48/48 tests + compileall passed.
- Visual PR Windows build `33129215245` passed source validation, Qt offscreen `MainWindow` smoke, pinned models, EXE build and packaged self-test.
- Initial production build `33129501062` passed the application gates but failed only at the old release-existence probe.
- Release-guard PR build `33145190036` passed.
- Final production build `33145419554` passed all gates, published `v0.8-visual-refresh` and uploaded the Actions artifact.
- Corporate DION/mTLS/WASAPI behavior and target-display visual usability are still field-test pending and must not be treated as proven by CI alone.

## Next evidence: field hardening
1. Open 0.8 on the target Windows workstation and validate layout at real resolution/DPI/scaling.
2. Verify all seven navigation pages and bottom actions during a real work session.
3. Corporate DION token + mTLS connection test.
4. Secretary Bot room join/roster/revoke/waiting-room behavior.
5. WASAPI + mic coexistence on target PCs.
6. 2/5/10 participant tests with rapid speaker changes and overlap.
7. 60+ minute latency/queue/drop measurement.
8. Calibrate Voice ID false accepts/rejects and diarization error.
9. Compare Russian recognition against a corrected reference transcript using WER/CER where permissible.

## Next engineering candidates
- polish responsive behavior for smaller Windows resolutions/high DPI after the first 0.8 field screenshots;
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
- Windows/DION/UI-specific limitations are stated;
- affected documentation is updated;
- user-visible behavior is in `CHANGELOG.md`;
- exact Release metadata and SHA-256 are recorded after publication.
