# Development and release

## Target environment

- Windows 10/11 x64.
- Python 3.11 for development/build.
- Primary GUI: PySide6.
- Audio: PyAudioWPatch / WASAPI Loopback.
- STT: faster-whisper + CTranslate2.
- Optional speaker embeddings: sherpa-onnx.

Core Python requirements of the reconstructed project:

```text
PySide6>=6.8,<7
PyAudioWPatch==0.2.12.8
faster-whisper==1.2.1
python-docx>=1.1,<2
sherpa-onnx>=1.13.6,<2
```

## Important branch model

`dion-exe-build` is currently optimized for reproducible Windows release builds rather than human source editing.

Build inputs:

1. `dion-portable/part*` -> reconstruct `source.zip`.
2. Expand to a temporary `src/` working tree.
3. Apply `dion-hotfix/apply_051.py`.
4. Apply `dion-quality/apply_060.py`.
5. Install dependencies.
6. Download/bundle offline models.
7. Build one-file Windows EXE.
8. Run packaged self-test.
9. Publish GitHub Release.

When changing application code in this branch, update the appropriate patch script or intentionally replace this packaging model with a normal source tree in a separate migration task. Do not silently edit encoded `part*` fragments by hand.

## Local reconstructed-source setup

Recommended dev flow once the source tree is available:

```bat
py -3.11 -m venv .venv
.venv\Scripts\activate
python -m pip install --upgrade pip
pip install -r requirements.txt
```

Or use `install_and_run.bat` from the reconstructed project.

## Tests

Primary command:

```bash
python -m pytest -q
```

Version 0.6 baseline:

```text
25 passed
```

Test responsibilities are mapped in `PROJECT_MAP.md`.

For compatibility with older tooling, the source project may also contain unittest-compatible tests, but pytest is the canonical current command because it covers the full suite including 0.6 quality tests.

## Windows-only field checks

Automated tests cannot fully validate:

- actual WASAPI loopback capture from the user's DION output device;
- interaction with corporate audio drivers;
- real microphone/loopback coexistence;
- audio quality during an actual DION conference;
- native DLL behavior on the user's endpoint protection configuration.

After audio/native changes, run a real Windows field check:

1. launch the packaged EXE;
2. open diagnostics;
3. run preflight;
4. start a short DION meeting;
5. verify system audio and microphone;
6. stop normally and verify queue tail is drained;
7. inspect transcript and diagnostic report;
8. confirm no crash report was created.

## Portable self-test

`run.py --portable-selftest` verifies that the packaged runtime can import critical dependencies and find bundled model assets without opening GUI/audio devices.

A release must not publish if self-test returns non-zero.

Current checks include:

- PyAudioWPatch import;
- sherpa-onnx import;
- faster-whisper import;
- PySide6 import;
- python-docx import;
- bundled Whisper directory and core model files;
- bundled speaker ONNX model file.

## Build pipeline

Canonical workflow for current release branch:

```text
.github/workflows/build-dion-portable.yml
```

High-level stages:

```text
checkout
 -> reconstruct source
 -> setup Python 3.11
 -> apply 0.5.1 + 0.6 patches
 -> install/validate dependencies
 -> download offline models
 -> PyInstaller --onefile --windowed
 -> packaged EXE self-test
 -> GitHub Release
 -> Actions artifact
```

## Model policy

### Whisper

Current quality release bundles `Systran/faster-whisper-small`.

Do not replace it with `base` merely to reduce EXE size without marking the release as a lower-quality profile. The 0.6 release exists specifically because the real 0.5.1 transcript showed unacceptable recognition errors with `base`.

### Speaker model

The speaker embedding model may be bundled for future use, but diarization remains disabled by default until native stability is improved.

## Release checklist

Before creating/updating a release:

- [ ] applicable tests pass;
- [ ] build workflow validation checks pass;
- [ ] packaged self-test passes on Windows;
- [ ] `CHANGELOG.md` updated;
- [ ] `docs/RELEASES.md` updated;
- [ ] `docs/ROADMAP.md` updated;
- [ ] `docs/PROJECT_MAP.md` updated if files/symbols changed;
- [ ] relevant design docs updated;
- [ ] SHA-256 is calculated from the actual uploaded EXE;
- [ ] release notes distinguish tested facts from expected field behavior.

## Dependency changes

When adding/upgrading a dependency:

1. document why it is needed;
2. update `requirements.txt`/build workflow;
3. add or update tests;
4. update `PROJECT_MAP.md` if responsibility changes;
5. update privacy/security docs if the dependency can access audio, files or network;
6. verify PyInstaller inclusion and packaged self-test where applicable.

## Documentation changes are code changes

A task is not complete after tests if the documentation update matrix in `DOCUMENTATION_POLICY.md` is not satisfied.
