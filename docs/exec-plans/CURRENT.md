# Current execution plan

## Objective
Build and publish **DION Meeting Assistant 0.9.1 Browser Gate Hotfix**, then field-validate the confirmed two-stage corporate DION entry flow on the target Windows workstation.

## Published baseline
```text
v0.9-guest-secretary-bot
DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe
Size: 627,722,376 bytes
SHA-256: 3e57b7c1fac965a14518d6eecc86642bcd3367af1fcf66af01e71142c09aef22
Target commit: f5ae18ef98d26236e9c7f5f42aa5b7e685c5a7e6
```

Release page: `https://github.com/Zios86/test/releases/tag/v0.9-guest-secretary-bot`.

## Completed release gates
- reconstructed source: `36/36 tests passed`, compileall passed;
- PR #4 merged after Windows CI `33150603611` succeeded through packaged self-test;
- production Windows CI `33150927129` succeeded through tests, pinned models, EXE build, packaged self-test and Release publication.

## What 0.9 implements
- ordinary HTTPS `/join/<slug>` room URL is the primary Secretary Bot input;
- corporate/on-prem hosts supported without hard-coded `dion.vc`;
- guest mode works without token/mTLS/API credentials;
- isolated temporary Edge/Chrome guest profile with muted bot-browser audio;
- localhost-only best-effort automatic name fill + `Войти как гость` click;
- visible manual fallback;
- optional configurable DION IAPI base + existing token/mTLS advanced settings;
- optional participant metadata by slug, explicitly not current-presence proof;
- conservative browser probe for explicit participant IDs/names and explicit speaking data/ARIA;
- no CSS-color/generic-text/microphone-enabled speaker inference;
- browser live-speaker state remains a live indicator, not retrospective transcript relabeling;
- main STT audio remains Windows WASAPI Loopback;
- 0.8 visual shell and 0.7.1 hardening retained.

## Immediate field-validation checklist
### Guest entry
1. Paste a real corporate `/join/<slug>` URL.
2. Confirm the bot clicks «Продолжить в браузере», not the native-app action.
3. Confirm the guest form appears.
4. Confirm `Секретарь-бот` is entered and «Войти как гость» is clicked.
5. Confirm isolated Edge/Chrome session and muted bot-browser audio.
6. If auto-join fails, preserve the visible page and record the exact stopped stage.

### Browser adapter
1. Inspect whether the corporate DION version exposes stable explicit participant IDs/names.
2. Inspect explicit speaking attributes/ARIA semantics.
3. Confirm lack of strong semantics becomes `capability unavailable`, not a guessed speaker.
4. Record timing offset between browser speaker state and actual captured WASAPI/Whisper chunks.

### Optional IAPI
1. Confirm corporate IAPI base URL if available.
2. Test token + mTLS.
3. Test slug participant metadata.
4. Confirm slug rows are not automatically labeled currently active without stronger evidence.

### Audio/speaker
1. Test WASAPI + mic together.
2. Test 2/5/10 participants and overlap.
3. Run 60+ minutes and inspect queue/latency/drop metrics.
4. Calibrate Voice ID and diarization false matches/errors.

## Blocking limitations
- No documented/verified per-user DION PCM stream is used by 0.9.
- Main STT audio remains Windows WASAPI Loopback.
- Browser DOM semantics are deployment/version dependent and unverified on the target corporate DION until field testing.
- Browser live-speaker state is not automatically applied to delayed transcript chunks until clock alignment is measured.
- CI cannot prove real guest join, waiting room, enterprise browser policy or live speaker semantics.

## Rollback
If 0.9 has a Guest Bot/browser regression, use published `v0.8-visual-refresh` while preserving evidence for the fix.

## Repository administration
GitHub has reported `Zios86/test` as `public`. If the project should be private, visibility must be changed in repository settings. Never commit real meeting links, participant data, tokens, certificates, private keys or transcripts.

## Update rule
This file is the active plan, not the historical ledger. Durable facts go to `ROADMAP.md`, released artifact facts to `RELEASES.md`, user-visible changes to `CHANGELOG.md`, and every significant engineering step to append-only `VERSION_JOURNAL.md`.
