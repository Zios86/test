# Development and release

## Target
- Windows 10/11 x64.
- Release CI: Python `3.11.9`.
- PySide6 GUI.
- PyAudioWPatch/WASAPI capture.
- faster-whisper/CTranslate2 local STT.
- sherpa-onnx optional speaker analysis.
- Optional DION IAPI control plane.
- 0.9 browser Guest Bot uses standard Edge/Chrome plus `websocket-client` for loopback DevTools communication.

## Build model
```text
dion-portable/part* -> base source
  -> dion-hotfix/apply_051.py
  -> dion-quality/apply_060.py
  -> dion-secretary-bot/apply_070.py
  -> dion-hardening/apply_071.py
  -> dion-visual/apply_080.py
  -> dion-guest-bot/apply_090.py
  -> compileall + tests
  -> Qt offscreen MainWindow smoke
  -> pinned/verified offline models
  -> PyInstaller onefile
  -> packaged --portable-selftest
  -> versioned GitHub Release on release-branch push only
```

Do not edit encoded payload parts manually. Long-term roadmap remains migration to a normal source tree.

## 0.9 source tests
Canonical command:

```bash
python -m pytest -q
```

Current reconstructed 0.8 + `apply_090.py` development result:

```text
36/36 tests passed
compileall passed
```

This is the current 0.9 local/reconstructed baseline. Do not mix it with the historical 0.8 local 48-test workspace or 0.8 release-workflow 29-test discovery count.

Important new suite:
- `tests/test_guest_bot_09.py`.

It validates:
- corporate/on-prem HTTPS `/join/<slug>` parsing;
- rejection of non-join URLs;
- Guest Bot without token/IAPI;
- slug IAPI path and unknown live-presence semantics;
- visible manual browser fallback;
- guest join submitted state without falsely claiming room presence;
- UI primary guest mode and advanced API controls.

## 0.9 UI smoke gate
Windows PR/release CI must create the actual PySide6 `MainWindow` with:

```text
QT_QPA_PLATFORM=offscreen
QApplication([])
MainWindow()
```

0.9 smoke should verify both 0.8 visual invariants and new guest-flow controls, including:
- seven-page navigation;
- `TranscriptCardView`;
- `dion_room_url_edit`;
- `secretary_auto_join`;
- advanced `dion_api_base_edit`;
- no primary `dion_event_id_edit` requirement.

This is a construction/API smoke, not a substitute for visual and corporate-DION field testing.

## Dependency lock
0.7.1 introduced exact Windows CI locks; 0.9 updates the lock for the browser adapter.

New 0.9 direct dependency:

```text
websocket-client==1.8.0
```

It is used only for local DevTools WebSocket communication to the dedicated Edge/Chrome guest session. It is not a cloud service dependency.

CI installs the exact lock without dependency re-resolution and runs `pip check`. Any dependency change requires lock review, tests, documentation and journal entry.

## Model supply chain
`release/model-manifest.json` pins the Whisper repository revision and SHA-256 values for downloadable speaker-model artifacts. 0.9 does not change the offline model set.

## GitHub Actions/release policy
First-party Actions are referenced by immutable commit SHA. Pull requests run reconstruct/test/Qt-smoke/model/build/self-test but do **not** publish.

0.9 workflow candidate:
- watches `dion-guest-bot/**` in addition to earlier patch paths;
- applies `apply_090.py` after `apply_080.py`;
- builds `DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe`;
- candidate tag: `v0.9-guest-secretary-bot`;
- refuses to overwrite an existing tag/asset.

Do not mark 0.9 released until production CI and GitHub Release are actually successful.

## Portable self-test
`run.py --portable-selftest` checks critical imports and bundled model assets without opening normal GUI/audio devices. A release must not publish if it returns non-zero.

For 0.9, `websocket-client` must be present in the packaged dependency set so Guest Bot auto-join can work where the browser exposes local DevTools.

## Browser field checks
0.9 adds browser-specific field evidence requirements:

1. Paste a real corporate DION `/join/<slug>` URL.
2. Confirm host/slug are parsed correctly.
3. Launch Guest Bot with no token/mTLS configured.
4. Verify isolated Edge/Chrome window/profile starts.
5. Verify bot-browser audio is muted.
6. Test auto-fill + `Войти как гость`.
7. If auto-join fails, verify visible manual entry still works.
8. After join, inspect whether the corporate DION web UI exposes explicit participant IDs/names.
9. Inspect whether explicit speaking data/ARIA semantics exist.
10. Verify absence of speaker semantics degrades cleanly to local diarization/unknown.
11. Confirm temporary guest profile is removed on normal close.
12. Confirm diagnostics do not include real room URL/slug or browser page content.

## DION IAPI field checks
If Integration API is available:
1. confirm the deployment API base URL;
2. test token + mTLS;
3. call participant metadata by slug;
4. verify UI explicitly describes slug results as non-authoritative for current live presence;
5. do not use slug-only rows as automatic live Voice-ID candidates.

## Audio/speaker field checks
1. real WASAPI + microphone coexistence;
2. 2/5/10 participants with speaker switching/overlap;
3. 60+ minute queue/latency/drop run;
4. calibrate Voice ID false accepts/rejects;
5. compare browser speaker-event time against actual audio/STT timestamps before any retrospective relabel feature is enabled.

## Published 0.8 validation reference
The current published fallback remains `v0.8-visual-refresh`, production run `33145419554`, SHA-256 `0ea963916ecf00d9bf9ef219377e709718d1c5d458ec656fc54f5527d43f3fa9`.

## Remaining release hardening
- Authenticode signing requires a real signing certificate/secure CI secret.
- Repository visibility/default branch/branch protection are GitHub administrative settings.
- Future source-tree migration should replace encoded `part* + apply_*.py`.
- Browser adapter selectors/semantics require ongoing compatibility tests against actual DION web versions.

## Documentation rule
A code/build/config task is incomplete until `VERSION_JOURNAL.md` and all affected canonical documents are updated according to `DOCUMENTATION_POLICY.md`.
