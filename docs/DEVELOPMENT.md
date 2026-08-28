# Development and release

## Target
- Windows 10/11 x64.
- Release CI: Python `3.11.9`.
- PySide6 GUI; PyAudioWPatch/WASAPI capture; faster-whisper/CTranslate2 local STT; sherpa-onnx optional speaker analysis.
- Optional DION IAPI control plane.
- 0.9 browser Guest Bot uses installed Edge/Chrome plus `websocket-client` for loopback DevTools communication.

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
  -> immutable versioned GitHub Release on release-branch push only
```

Do not edit encoded payload parts manually. Long-term roadmap remains migration to a normal source tree.

## Tests
Canonical command:
```bash
python -m pytest -q
```

0.9 reconstructed source baseline:
```text
36/36 tests passed
compileall passed
```

Important 0.9 coverage includes corporate/on-prem `/join/<slug>` parsing, no-token guest mode, slug-IAPI semantics, visible browser fallback, guest-submit state without false room-presence claims and primary/advanced UI hierarchy.

Do not mix this count with historical 0.8 local/release test counts.

## UI smoke gate
Windows CI constructs the actual PySide6 `MainWindow` with `QT_QPA_PLATFORM=offscreen` and verifies 0.8 visual invariants plus 0.9 guest controls, including room URL, auto-join and advanced API base. This is a construction/API smoke, not a human visual or corporate-DION field test.

## Dependency lock
0.7.1 introduced exact Windows CI locks. 0.9 adds:
```text
websocket-client==1.8.0
```
It is used only for local DevTools WebSocket communication to the dedicated Edge/Chrome guest session; it is not a cloud dependency.

CI installs the exact lock, runs `pip check`, and requires tests after dependency changes.

## Model supply chain
`release/model-manifest.json` pins the Whisper repository revision and SHA-256 values for downloadable speaker-model artifacts. 0.9 does not change the offline model set.

## GitHub Actions/release policy
Pull requests run reconstruct/test/Qt-smoke/model/build/self-test but do not publish. Publication occurs only after a qualifying push to `dion-exe-build`. Existing version tags/assets are not overwritten.

### Published 0.9 validation
- PR #4 Windows CI `33150603611` — **success** through packaged `--portable-selftest`; Release step skipped by design.
- Merge commit: `f5ae18ef98d26236e9c7f5f42aa5b7e685c5a7e6`.
- Production Windows CI `33150927129` — **success** through tests, pinned models, EXE build, packaged self-test and GitHub Release publication.
- Release: `v0.9-guest-secretary-bot`.
- Artifact: `DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe`.
- Size: `627,722,376 bytes`.
- SHA-256: `3e57b7c1fac965a14518d6eecc86642bcd3367af1fcf66af01e71142c09aef22`.

## Portable self-test
`run.py --portable-selftest` checks critical imports and bundled model assets without opening normal GUI/audio devices. It passed for the published 0.9 production build.

## Required 0.9 field checks
CI does not prove the following. Validate on the target environment:
1. Paste a real corporate DION `/join/<slug>` URL and confirm parsing.
2. Launch Guest Bot with token/mTLS empty.
3. Verify isolated Edge/Chrome guest profile and muted bot-browser audio.
4. Test automatic name fill/guest click and visible manual fallback.
5. Inspect whether the corporate DION UI exposes stable explicit participant IDs/names.
6. Inspect explicit speaking data/ARIA semantics; absence must degrade to capability unavailable.
7. Measure timing offset between browser speaker events and captured WASAPI/Whisper chunks before any retrospective relabel feature.
8. If IAPI is available, confirm corporate base URL, token/mTLS and slug metadata semantics.
9. Test WASAPI + mic, 2/5/10 participants, overlap, and a 60+ minute queue/latency/drop run.

## Remaining hardening
- Authenticode signing requires a real signing certificate/secure CI secret.
- Repository visibility/default branch/branch protection are GitHub administrative settings.
- Future source-tree migration should replace encoded `part* + apply_*.py`.
- Browser adapter semantics require ongoing compatibility testing against actual DION web versions.

## Documentation rule
A code/build/config task is incomplete until `VERSION_JOURNAL.md` and all affected canonical documents are updated according to `DOCUMENTATION_POLICY.md`.
