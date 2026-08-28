# Development and release

## Target
- Windows 10/11 x64.
- Release CI: Python `3.11.9`.
- PySide6 GUI; PyAudioWPatch/WASAPI capture; faster-whisper/CTranslate2 STT; sherpa-onnx optional speaker analysis; DION IAPI control plane.

## Build model
```text
dion-portable/part* -> base source
  -> dion-hotfix/apply_051.py
  -> dion-quality/apply_060.py
  -> dion-secretary-bot/apply_070.py
  -> dion-hardening/apply_071.py
  -> dion-visual/apply_080.py
  -> compileall + tests
  -> Qt offscreen MainWindow smoke
  -> pinned/verified offline models
  -> PyInstaller onefile
  -> packaged --portable-selftest
  -> versioned GitHub Release on release-branch push only
```
Do not edit encoded payload parts manually. Long-term roadmap remains migration to a normal source tree.

## Tests
Canonical command:
```bash
python -m pytest -q
```
0.8 visual-refresh development workspace: **48/48 passed locally** and compileall passed.

The reconstructed Windows release workflow currently discovers and passes **29 pytest tests**. It additionally verifies 0.8 source markers, version values, shared PortAudio behavior and sherpa-onnx availability. Keep the local and reconstructed-suite counts distinct in documentation; neither proves real corporate WASAPI/DION/mTLS/CPU behavior.

## UI smoke gate
Because the development environment used for the visual implementation does not itself provide a real Windows desktop display, CI performs a native PySide6 construction check before packaging:

```text
QT_QPA_PLATFORM=offscreen
QApplication([])
MainWindow()
```

The smoke asserts the seven-page navigation and `TranscriptCardView` exist and that the redesigned `MainWindow` can be constructed and closed. This catches Qt API/layout initialization failures but is not a substitute for a human visual/usability check on the target workstation.

Canonical UI rules live in `design-docs/UI_VISUAL_SYSTEM.md`.

## Dependency lock
0.7.1 introduced `requirements-ci.lock.txt` with exact Windows CI versions; 0.8 retains it. CI installs it without dependency re-resolution and runs `pip check`. Intentional dependency updates require lock review, all tests, documentation and a new journal entry.

## Model supply chain
`release/model-manifest.json` pins the Whisper repository revision and expected SHA-256 values for downloadable speaker-model artifacts. CI downloads using those pins and verifies hashes before packaging.

## GitHub Actions/release policy
First-party actions are referenced by immutable commit SHA. Pull requests run the full reconstruct/test/Qt-smoke/model/build/self-test pipeline but do **not** publish. Publication occurs only after a qualifying push to `dion-exe-build`.

Published version assets must not be overwritten. For `v0.8-visual-refresh`, the release-existence probe deliberately allows the expected non-zero result when the tag is absent, then restores strict native-command failure handling before `gh release create`.

Historical note: production run `33129501062` successfully built/self-tested 0.8 but failed at the old existence probe because PowerShell promoted `gh release view` exit code `1` to an exception before the guard logic could inspect it. PR #3 fixed this; final production run `33145419554` published successfully.

## Portable self-test
`run.py --portable-selftest` checks critical imports and bundled model assets without opening normal GUI/audio devices. A release must not publish if it returns non-zero.

## 0.8 release validation
- Initial visual PR run: `33129215245` — success; Release step skipped by design.
- First production run: `33129501062` — application tests, Qt smoke, models, EXE and self-test succeeded; publication guard failed.
- Release-guard PR run: `33145190036` — success; Release step skipped by design.
- Final production run: `33145419554` — success through Release publication and Actions artifact upload.
- Published asset SHA-256: `0ea963916ecf00d9bf9ef219377e709718d1c5d458ec656fc54f5527d43f3fa9`.

## Windows/DION/UI field checks
After audio/DION/speaker/UI changes validate on target PCs:
1. launch packaged EXE and verify the 0.8 shell visually at normal Windows scaling;
2. verify navigation, transcript cards, right summary rail and bottom actions remain usable at the target resolution/DPI;
3. run preflight and test real WASAPI + microphone coexistence;
4. test DION token + mTLS and Secretary Bot room lifecycle;
5. verify 2/5/10 participant speaker switching and overlap;
6. run a 60+ minute meeting and inspect queue/latency/drop metrics;
7. verify exports/diagnostics and absence of sensitive data.

## Remaining release hardening
- Authenticode signing requires a real signing certificate/secure CI secret.
- Repository visibility/default branch/branch protection are GitHub administrative settings.
- Future source-tree migration should replace the encoded `part* + apply_*.py` release structure.

## Documentation rule
A code/build/config task is incomplete until `VERSION_JOURNAL.md` and all affected canonical documents are updated according to `DOCUMENTATION_POLICY.md`.
