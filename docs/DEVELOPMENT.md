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
  -> compileall + tests
  -> pinned/verified offline models
  -> PyInstaller onefile
  -> packaged --portable-selftest
  -> immutable GitHub Release on release-branch push only
```
Do not edit encoded payload parts manually. Long-term roadmap remains migration to a normal source tree.

## Tests
Canonical command:
```bash
python -m pytest -q
```
0.7.1 reconstructed-source baseline: **46 passed locally**. Automated checks do not prove real corporate WASAPI/DION/mTLS/CPU behavior.

## Dependency lock
0.7.1 includes `requirements-ci.lock.txt` with exact Windows CI versions. CI installs it without dependency re-resolution and runs `pip check`. Intentional dependency updates require lock review, all tests, documentation and a new journal entry.

## Model supply chain
`release/model-manifest.json` pins the Whisper repository revision and expected SHA-256 values for downloadable speaker-model artifacts. CI downloads using those pins and verifies hashes before packaging.

## GitHub Actions/release policy
First-party actions are referenced by immutable commit SHA. Pull requests run the full reconstruct/test/model/build/self-test pipeline but do **not** publish. Publication occurs only after a qualifying push to `dion-exe-build`.

Release tag `v0.7.1` is immutable: if it already exists, CI fails rather than using `--clobber`. A changed binary requires a new version/tag.

## Portable self-test
`run.py --portable-selftest` checks critical imports and bundled model assets without opening GUI/audio devices. A release must not publish if it returns non-zero.

## Windows/DION field checks
After audio/DION/speaker changes validate on target PCs:
1. launch packaged EXE and run preflight;
2. test real WASAPI + microphone coexistence;
3. test DION token + mTLS and Secretary Bot room lifecycle;
4. verify 2/5/10 participant speaker switching and overlap;
5. run a 60+ minute meeting and inspect queue/latency/drop metrics;
6. verify exports/diagnostics and absence of sensitive data.

## Remaining release hardening
- Authenticode signing requires a real signing certificate/secure CI secret.
- Repository visibility/default branch/branch protection are GitHub administrative settings.
- Future source-tree migration should replace the encoded `part* + apply_*.py` release structure.

## Documentation rule
A code/build/config task is incomplete until `VERSION_JOURNAL.md` and all affected canonical documents are updated according to `DOCUMENTATION_POLICY.md`.
