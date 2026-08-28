# Project map

Use this file first. Do not scan encoded payload parts for orientation.

## Release/build tree
```text
AGENTS.md
CLAUDE.md
CHANGELOG.md
docs/
  PROJECT_MAP.md
  ARCHITECTURE.md
  DEVELOPMENT.md
  VERSION_JOURNAL.md
  RELEASES.md
  ROADMAP.md
  design-docs/
    UI_VISUAL_SYSTEM.md
    SPEECH_RECOGNITION.md
    AUDIO_STABILITY.md
    SPEAKER_IDENTIFICATION.md
    DION_INTEGRATION.md
    PRIVACY_SECURITY.md
  exec-plans/CURRENT.md
release/model-manifest.json
.github/workflows/build-dion-portable.yml
dion-portable/                 # encoded base source
dion-hotfix/apply_051.py
dion-quality/apply_060.py
dion-secretary-bot/apply_070.py
dion-hardening/apply_071.py
dion-visual/apply_080.py
```

Patch order is 0.5.1 -> 0.6 -> 0.7 -> 0.7.1 -> 0.8.

## Reconstructed application
- `run.py` — startup, crash handlers, freeze support, portable self-test.
- `app/ui.py` — 0.8 native Qt shell and orchestration: top live-status bar, seven-page left navigation, card-based transcript, right summary rail, persistent bottom actions, DION/mTLS settings, meeting lifecycle, Secretary Bot and speaker controls. Key visual symbols include `MODERN_QSS`, `TranscriptCardView`, `MainWindow`.
- `app/audio.py` — device discovery, shared PortAudio context, WASAPI/microphone capture.
- `app/transcriber.py` — faster-whisper, context/hotwords/VAD, word-level speaker handoff split.
- `app/speakers.py` — isolated sherpa-onnx diarization/overlap/embeddings.
- `app/speaker_profiles.py` — opt-in cross-meeting Voice ID, Windows DPAPI, conservative matching.
- `app/dion_api.py` — DION IAPI HTTPS/token/mTLS, invites/users.
- `app/dion_bot.py` — Secretary Bot invite/browser lifecycle and stale-profile cleanup.
- `app/storage.py` — transcript/autosave/export/aliases.
- `app/protocol.py` — deterministic decisions/tasks/questions.
- `app/local_ai.py` — optional localhost-only protocol wording refinement.
- `app/health.py`, `app/preflight.py`, `app/crash.py` — health/preflight/redacted crash diagnostics.

## Tests and release gates
0.8 visual-refresh development workspace: **48/48 tests passed locally** plus compileall.

Published `v0.8-visual-refresh` additionally passed Windows CI, Qt `offscreen` `MainWindow` construction, pinned-model validation, one-file EXE build and packaged `--portable-selftest`. Final release build: `33145419554`. Field DION/WASAPI and real target-display usability remain separate.

The reconstructed Windows release workflow currently reports 29 discovered pytest tests in CI; this is distinct from the broader 48-test local development workspace and should not be silently conflated.

Key suites include `test_visual_refresh.py`, `test_transcriber_quality.py`, `test_dion_api.py`, `test_dion_bot.py`, `test_speaker_attribution.py`, `test_speaker_profiles.py`, plus storage/protocol/health/crash/local-AI coverage present in the relevant source workspace.

## Routing
| Task | Read first | Main code |
|---|---|---|
| UI / visual design | `design-docs/UI_VISUAL_SYSTEM.md` | `app/ui.py`, `dion-visual/apply_080.py` |
| Speech quality | `design-docs/SPEECH_RECOGNITION.md` | `app/transcriber.py`, `app/ui.py` |
| Speaker/overlap | `design-docs/SPEAKER_IDENTIFICATION.md` | `app/speakers.py`, `app/speaker_profiles.py`, `app/transcriber.py` |
| DION/mTLS/Secretary Bot | `design-docs/DION_INTEGRATION.md` | `app/dion_api.py`, `app/dion_bot.py`, `app/ui.py` |
| WASAPI/start crash | `design-docs/AUDIO_STABILITY.md` | `app/audio.py`, `app/ui.py`, `app/crash.py` |
| Privacy/secrets | `design-docs/PRIVACY_SECURITY.md` | DION/speaker/storage/diagnostics modules |
| Protocol/export | `ARCHITECTURE.md` | `app/protocol.py`, `app/storage.py`, `app/ui.py` |
| EXE/CI/models/deps | `DEVELOPMENT.md` | workflow, patch scripts, model manifest |
| History | `VERSION_JOURNAL.md` | referenced paths |
| Published EXE/SHA | `RELEASES.md` | GitHub Release |

If a module responsibility, patch order, key symbol or canonical document changes, update this map in the same task. GitHub visibility/default branch/branch protection are external settings and must not be claimed fixed without verification.
