---
name: project-navigation
description: Locate the correct DION Meeting Assistant files and symbols without scanning the whole repository. Use when starting coding, debugging, architecture, review, UI, DION guest-bot, or release work and you need the smallest relevant source area.
---

# Project navigation

## Goal
Find the smallest relevant part of the project before reading or editing code.

## Workflow
1. Read `../../../docs/PROJECT_MAP.md`.
2. Match the request to its routing table.
3. Read the linked design/development doc only if needed.
4. Inspect only mapped source/build files.
5. Expand to neighboring modules only when call/data flow requires it.

## Special rule for the release branch
`dion-exe-build` reconstructs a base project from encoded `dion-portable/part*` files and applies version patches.

Do **not** read all encoded parts for orientation.

Current patch order:

```text
apply_051.py
 -> apply_060.py
 -> apply_070.py
 -> apply_071.py
 -> apply_080.py
 -> apply_090.py
```

Use:
- `dion-hotfix/apply_051.py` — shared-PortAudio stability;
- `dion-quality/apply_060.py` — recognition quality;
- `dion-secretary-bot/apply_070.py` — legacy API Secretary Bot/roster/speaker fallback;
- `dion-hardening/apply_071.py` — mTLS/privacy/lifecycle/release hardening;
- `dion-visual/apply_080.py` — native PySide6 Visual Refresh;
- `dion-guest-bot/apply_090.py` — room-URL-first Guest Bot/browser adapter/slug API;
- `.github/workflows/build-dion-portable.yml` — Windows validation/build/release behavior.

Reconstruct source only when the requested code change actually requires it.

## Fast routes
- guest URL parsing / slug / guest browser launch -> `app/dion_bot.py`;
- DION slug API / token / mTLS / API base -> `app/dion_api.py`;
- Guest Bot UI / browser status -> `app/ui.py`;
- browser speaker semantics -> `app/dion_bot.py`, then `docs/design-docs/SPEAKER_IDENTIFICATION.md`;
- recognition accuracy -> `app/transcriber.py`;
- audio/start crash -> `app/audio.py`, then `app/ui.py`;
- protocol -> `app/protocol.py`;
- Ollama -> `app/local_ai.py`;
- acoustic speaker separation -> `app/speakers.py` / `app/speaker_profiles.py`;
- export -> `app/storage.py`;
- diagnostics -> `app/health.py`, `app/preflight.py`, `app/crash.py`;
- UI styling/layout -> `docs/design-docs/UI_VISUAL_SYSTEM.md`, `app/ui.py`;
- release -> `docs/DEVELOPMENT.md` + workflow + patch scripts.

## 0.9 evidence rule
Do not confuse these sources:
- slug IAPI metadata = participant hint, live presence unknown;
- browser explicit speaking semantics = live UI evidence only until timing calibration;
- microphone-enabled state = not speaking;
- WASAPI = actual current STT audio source;
- local diarization/Voice ID = acoustic fallback.

## Final check
If the change modifies where a feature lives, update `docs/PROJECT_MAP.md`. If it is significant, also append `docs/VERSION_JOURNAL.md` and follow `docs/DOCUMENTATION_POLICY.md`.
