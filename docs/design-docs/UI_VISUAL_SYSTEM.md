# UI visual system — 0.8 Visual Refresh

## Purpose

This document is the canonical visual/UI specification for DION Meeting Assistant. Version 0.8 translates the approved concept into native PySide6 widgets and QSS; the generated concept images are references only and are not embedded as application screenshots or raster UI.

## Product principles

1. **Читаемость** — meeting text is the primary content; controls must not compete with the transcript.
2. **Спокойная обратная связь** — connection/audio/recording state is visible without modal noise.
3. **Быстрые действия** — start/stop/export/protocol actions stay one click away.
4. **Конфиденциальность** — visuals must never encourage exposing token/certificate/transcript data in screenshots or diagnostics.

## Color tokens

- Primary blue: `#2563EB`
- Success green: `#22C55E`
- Danger red: `#EF4444`
- Gray 900: `#0F172A`
- Gray 700: `#334155`
- Gray 500: `#64748B`
- Gray 300: `#CBD5E1`
- Gray 200: `#E2E8F0`
- Gray 100: `#F1F5F9`
- Gray 50: `#F8FAFC`

Use blue for selected navigation, primary actions and active speaker emphasis; green for healthy/connected state; red for recording/stop and overlap warnings. Do not use color as the only signal: important states also have text.

## Typography

Use the platform-native Segoe UI family where available.

- Page title: 22–24 px, semibold.
- Section title: 16–18 px, semibold.
- Main transcript: 14–16 px, regular.
- Secondary metadata/role/timestamp: 11–13 px.
- Button labels: 13–14 px, medium/semibold.

## Application shell

The 0.8 shell is intentionally stable:

```text
Top status bar
┌──────────────┬─────────────────────────┬──────────────────┐
│ Left nav     │ Main working area       │ Right summary    │
│              │                         │ rail             │
└──────────────┴─────────────────────────┴──────────────────┘
Bottom action bar
```

### Left navigation

Canonical entries:

1. Встреча
2. Стенограмма
3. Протокол
4. Участники
5. Секретарь-бот
6. Диагностика
7. Настройки

Selected item uses primary blue text/accent and a soft blue background. The Secretary Bot card below navigation shows connection state but is not a replacement for the dedicated bot page.

### Top status bar

Show only high-value live state:

- meeting title;
- recording state and elapsed time;
- microphone state;
- system-audio state;
- DION connection state.

### Bottom action bar

Primary actions remain visible:

- `Начать стенографию` — blue primary;
- `Остановить` — red danger;
- `Экспорт DOCX` — secondary;
- `Открыть протокол` — secondary/ghost.

## Transcript cards

`TranscriptCardView` is the canonical live transcript presentation.

Each card includes:

- timestamp;
- initials/avatar-style circle;
- display speaker name;
- optional role/state;
- recognized text;
- state styling.

States:

- Normal: neutral border/background.
- Current/active speaker: primary-blue outline plus `Говорит` label.
- Overlap/interruption: danger/red accent plus `Перебивание` label.

Do not rely solely on color for `Говорит` or `Перебивание`.

## Right summary rail

Canonical cards:

- Участники;
- Активный спикер;
- Качество аудио;
- Черновик протокола;
- Горячие слова.

These cards summarize existing state. They must not invent participant identity, protocol readiness or audio-quality facts that the underlying application has not computed.

## Settings and DION controls

DION token, mTLS certificate, private key and optional key password remain settings controls with the same security behavior as 0.7.1. The visual redesign must not persist secrets merely for convenience.

## Spacing and shape

Use an 8 px spacing rhythm where practical. Typical radii: 8–14 px. Cards use subtle borders and restrained shadows; avoid heavy gradients, glossy effects and decorative animation that distracts from transcript reading.

## Accessibility / long-session use

- Keep strong text contrast on light backgrounds.
- Keep buttons at comfortable pointer targets.
- Avoid rapid blinking/pulsing indicators.
- Keep recording and failure states readable without color perception.
- Preserve keyboard focus behavior of native Qt controls.

## Implementation boundary

Primary implementation: `app/ui.py` (`MODERN_QSS`, `TranscriptCardView`, `MainWindow` shell/navigation/summary methods).

0.8 is a visual/interaction restructuring only. Audio capture, DION mTLS, offline STT, privacy rules, protocol extraction and speaker-identification invariants remain governed by their existing design docs.

## Validation

Before release:

- source test suite must pass;
- Windows CI must instantiate `MainWindow` with `QT_QPA_PLATFORM=offscreen`;
- packaged EXE self-test must pass;
- actual user-visible rendering on the target Windows display remains a field check after publication.
