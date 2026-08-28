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
dion-guest-bot/apply_090.py
```

Patch order is `0.5.1 -> 0.6 -> 0.7 -> 0.7.1 -> 0.8 -> 0.9`.

## Reconstructed application
- `run.py` — startup, crash handlers, freeze support, portable self-test.
- `app/ui.py` — native 0.8 shell plus 0.9 guest-flow orchestration: room URL, bot name, auto-join toggle, optional advanced IAPI/mTLS, browser-room polling, participants display, meeting lifecycle.
- `app/audio.py` — device discovery, shared PortAudio context, WASAPI/microphone capture.
- `app/transcriber.py` — faster-whisper, context/hotwords/VAD, word-level speaker handoff split.
- `app/speakers.py` — isolated sherpa-onnx diarization/overlap/embeddings.
- `app/speaker_profiles.py` — opt-in cross-meeting Voice ID, Windows DPAPI, conservative matching.
- `app/dion_api.py` — optional DION IAPI HTTPS/token/mTLS client; legacy event-id users/invites plus 0.9 `list_event_users_by_slug()` metadata path.
- `app/dion_bot.py` — 0.9 Guest Bot core: `DionRoomLink`, `parse_dion_join_url()`, `SecretaryBotController.prepare_guest()`, `DionBrowserAdapter`, `GuestBrowserSession`, `launch_guest_room()`, guest-profile cleanup; legacy API-invite launcher retained for compatibility.
- `app/storage.py` — transcript/autosave/export/aliases.
- `app/protocol.py` — deterministic decisions/tasks/questions.
- `app/local_ai.py` — optional localhost-only protocol wording refinement.
- `app/health.py`, `app/preflight.py`, `app/crash.py` — health/preflight/redacted crash diagnostics.

## 0.9 Guest Bot data sources

```text
room URL /join/<slug>
  -> parse_dion_join_url()
  -> launch_guest_room()
     -> isolated Edge/Chrome profile
     -> localhost DevTools auto-join when available
     -> visible manual fallback

optional IAPI token+mTLS+base URL
  -> list_event_users_by_slug(slug)
  -> metadata/roster hint only; is_active is unknown

optional DionBrowserAdapter
  -> explicit data-participant-id/data-user-id
  -> explicit data-speaking/data-is-speaking/data-active-speaker or speaking ARIA
  -> live UI indicator only until timing calibration

meeting audio
  -> Windows WASAPI Loopback
  -> Whisper
  -> optional local diarization/Voice ID fallback
```

Do not equate slug metadata with current room presence. Do not equate microphone enabled with speaking.

## Tests and release gates

0.9 reconstructed development source: **36/36 tests passed** plus compileall.

Important 0.9 suites/symbol checks include:
- `tests/test_guest_bot_09.py` — corporate `/join/<slug>` parsing, no-token guest mode, slug API semantics, manual fallback and UI-primary-flow assertions;
- existing `test_visual_refresh.py`, `test_dion_api.py`, `test_dion_bot.py`, `test_transcriber_quality.py`, speaker/storage suites.

Published `v0.8-visual-refresh` remains the current release until 0.9 passes Windows PR CI, merge, production CI, packaged `--portable-selftest` and Release publication.

## Routing
| Task | Read first | Main code |
|---|---|---|
| Guest room URL / slug / browser auto-join | `design-docs/DION_INTEGRATION.md` | `app/dion_bot.py`, `app/ui.py` |
| DION IAPI/mTLS/slug metadata | `design-docs/DION_INTEGRATION.md` | `app/dion_api.py`, `app/ui.py` |
| Browser participant/speaker probe | `design-docs/DION_INTEGRATION.md`, `design-docs/PRIVACY_SECURITY.md` | `app/dion_bot.py`, `app/ui.py` |
| UI / visual design | `design-docs/UI_VISUAL_SYSTEM.md` | `app/ui.py`, `dion-visual/apply_080.py`, `dion-guest-bot/apply_090.py` |
| Speech quality | `design-docs/SPEECH_RECOGNITION.md` | `app/transcriber.py`, `app/ui.py` |
| Speaker/overlap | `design-docs/SPEAKER_IDENTIFICATION.md` | `app/speakers.py`, `app/speaker_profiles.py`, `app/transcriber.py` |
| WASAPI/start crash | `design-docs/AUDIO_STABILITY.md` | `app/audio.py`, `app/ui.py`, `app/crash.py` |
| Privacy/secrets | `design-docs/PRIVACY_SECURITY.md` | DION/browser/speaker/storage/diagnostics modules |
| Protocol/export | `ARCHITECTURE.md` | `app/protocol.py`, `app/storage.py`, `app/ui.py` |
| EXE/CI/models/deps | `DEVELOPMENT.md` | workflow, patch scripts, model manifest, lock files |
| History | `VERSION_JOURNAL.md` | referenced paths |
| Published EXE/SHA | `RELEASES.md` | GitHub Release |

If a module responsibility, patch order, key symbol or canonical document changes, update this map in the same task. GitHub visibility/default branch/branch protection are external settings and must not be claimed fixed without verification.
