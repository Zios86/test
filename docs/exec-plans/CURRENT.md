# Current execution plan

## Objective
Finish **DION Meeting Assistant 0.9 Guest Secretary Bot** safely: validate the room-URL-first guest flow on Windows CI, publish a new immutable EXE only after green gates, then field-test against the real corporate DION web client.

## Published baseline
Current published fallback:

```text
v0.8-visual-refresh
DION_Meeting_Assistant_0.8_Visual_Refresh_Portable.exe
SHA-256: 0ea963916ecf00d9bf9ef219377e709718d1c5d458ec656fc54f5527d43f3fa9
```

0.9 is not yet a published Release.

## 0.9 implemented state
Completed in `dion-guest-bot-0.9`:

- ordinary HTTPS `/join/<slug>` room URL is the primary Secretary Bot input;
- corporate/on-prem hosts are supported without hard-coded `dion.vc`;
- slug parsing and inline UI feedback;
- Guest Bot works without token/mTLS/API credentials;
- Edge/Chrome isolated temporary guest profile;
- bot-browser audio muted;
- local DevTools bound to `127.0.0.1` with dynamic port;
- best-effort automatic name fill and `Войти как гость` click;
- visible manual guest fallback;
- optional configurable DION IAPI base URL + existing token/mTLS advanced settings;
- optional participant metadata by slug;
- slug metadata explicitly marked as non-authoritative for current presence;
- conservative browser probe for explicit participant IDs/names and explicit speaking data/ARIA;
- no CSS-color/generic-text/microphone-enabled speaker inference;
- browser speaker observation displayed as live state only, not retrospective Whisper relabel;
- 0.8 visual shell and 0.7.1 hardening retained;
- `websocket-client==1.8.0` added to locked dependency set;
- release workflow updated to apply `dion-guest-bot/apply_090.py` and prepare a 0.9 binary/tag;
- temporary reconstructed-source export workflow removed after development use.

## Source validation already complete
On reconstructed 0.8 + 0.9 patch:

```text
36/36 tests passed
compileall passed
```

New tests include corporate URL parsing, no-token guest mode, slug-IAPI semantics, manual browser fallback and primary/advanced UI hierarchy.

## Immediate next steps
1. Ensure all canonical documentation/AI adapters match 0.9 implemented status.
2. Open PR `dion-guest-bot-0.9` -> `dion-exe-build`.
3. Run Windows PR CI.
4. Confirm Qt offscreen `MainWindow` smoke includes the guest URL/advanced API controls.
5. Confirm pinned model validation.
6. Build PR EXE and pass packaged `--portable-selftest`.
7. Do not publish from PR.
8. Merge only after green PR CI.
9. Run production CI from `dion-exe-build`.
10. Publish immutable `v0.9-guest-secretary-bot` only after production self-test.
11. Read the actual Release API and record artifact size/SHA-256 in `RELEASES.md` + a new released journal entry.

## Field validation after a 0.9 binary exists
### Guest entry
- Paste a real corporate `/join/<slug>` URL.
- Confirm parsed host/slug.
- Connect with token/mTLS left empty.
- Confirm isolated Edge/Chrome guest session opens.
- Confirm bot-browser audio is muted.
- Test auto-name + guest click.
- If auto-join does not work, verify manual guest entry remains usable.

### Browser adapter
- Inspect whether the corporate DION version exposes `data-participant-id`/`data-user-id` or equivalent strong identifiers.
- Inspect whether explicit speaking attributes/ARIA semantics exist.
- Confirm lack of strong semantics is reported as capability unavailable, not as a false speaker guess.
- Record timing offset between browser speaker state and actual captured audio/Whisper chunks.

### Optional IAPI
- Confirm real corporate IAPI base URL.
- Test token + mTLS if available.
- Test slug participant metadata.
- Confirm UI does not label slug rows as currently active without stronger evidence.

### Audio/speaker
- Test WASAPI + mic together.
- Test 2/5/10 participants and overlap.
- Run 60+ minutes and inspect queue/latency/drop metrics.
- Calibrate Voice ID and diarization accuracy.

## Blocking limitations
- No documented/verified per-user DION PCM stream is used by 0.9.
- Main STT audio remains Windows WASAPI Loopback.
- Browser DOM semantics are deployment/version dependent and unverified on the target corporate DION until field testing.
- Browser live-speaker state is not automatically applied to delayed transcript chunks until clock alignment is measured.
- CI cannot prove real guest join, waiting room, enterprise browser policy or live speaker semantics.

## Repository administration
GitHub has reported `Zios86/test` as `public`. If the project is intended to be private, repository visibility must be changed in GitHub settings. Never commit real meeting links, participant data, tokens, certificates, private keys or transcripts.

## Update rule
This file is the active plan, not the historical ledger. Durable completed facts go to `ROADMAP.md`, released artifact facts to `RELEASES.md`, user-visible changes to `CHANGELOG.md`, and every significant engineering step to append-only `VERSION_JOURNAL.md`.
