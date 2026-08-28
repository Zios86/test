# UI visual system — 0.8 Visual Refresh + 0.9 Guest Flow

## Purpose
This document is the canonical visual/UI specification for DION Meeting Assistant.

0.8 established the native PySide6/QSS visual language. 0.9 keeps that visual system and changes the **interaction hierarchy** for DION: ordinary guest room URL is primary; Integration API/mTLS is advanced/optional.

Generated concept images are references only. Shipping UI remains native Qt widgets/QSS.

## Product principles
1. **Читаемость** — meeting text is the primary content; controls must not compete with transcript reading.
2. **Спокойная обратная связь** — connection/audio/recording state is visible without modal noise.
3. **Быстрые действия** — start/stop/export/protocol stay one click away.
4. **Простая настройка** — common workflows should ask for business/user concepts, not API internals.
5. **Конфиденциальность** — UI must not encourage exposing token/certificate/meeting URL/transcript data in screenshots or diagnostics.

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

Use blue for selected navigation/primary actions/active speaker emphasis; green for healthy/connected state; red for recording/stop/overlap warnings. Important state also has text.

## Typography
Use platform-native Segoe UI where available.

- Page title: 22–24 px, semibold.
- Section title: 16–18 px, semibold.
- Main transcript: 14–16 px.
- Secondary metadata/timestamp: 11–13 px.
- Button labels: 13–14 px, medium/semibold.

## Application shell
The 0.8 shell remains stable:

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

The Secretary Bot status card below navigation is a summary, not the configuration form.

### Top status bar
Show high-value live state only:
- meeting title;
- recording state/time;
- microphone state;
- system-audio state;
- DION/Guest Bot status.

### Bottom action bar
- `Начать стенографию` — blue primary;
- `Остановить` — red danger;
- `Экспорт DOCX` — secondary;
- `Открыть протокол` — secondary/ghost.

## Transcript cards
`TranscriptCardView` is the canonical live transcript presentation.

Each card includes timestamp, initials/avatar, display speaker name, optional state/role and recognized text.

States:
- Normal — neutral;
- `Говорит` — primary-blue outline/state;
- `Перебивание` — danger accent.

Do not rely solely on color.

## Right summary rail
Canonical cards:
- Участники;
- Активный спикер;
- Качество аудио;
- Черновик протокола;
- Горячие слова.

These summarize actual state only. Browser/IAPI uncertainty must remain visible rather than presenting uncertain identity as fact.

## 0.9 Secretary Bot page hierarchy
The dedicated `Секретарь-бот` page must present the common path first.

### Primary group: `Гостевой вход в конференцию`
Fields/actions:
- `Ссылка на встречу` — HTTPS `/join/<slug>` URL;
- `Имя бота` — default `Секретарь-бот`;
- `Автоматически заполнить имя и нажать «Войти как гость»` — checked by default;
- parsed `Slug` / host feedback;
- connect/disconnect/status actions.

User should not need to understand `event_id` for ordinary guest entry.

Invalid URL feedback should be inline and readable, e.g. URL not recognized or missing `/join/<slug>`.

### Advanced group: DION Integration API / mTLS
Token, API base, certificate, private key and optional password belong under a clearly labeled **advanced/optional** group.

UI copy must state:
- guest entry works without these fields;
- slug participant metadata does not prove who is currently in the room;
- mTLS/token are deployment/admin integration settings.

Do not show an `event_id` field in the primary 0.9 flow.

## Guest Bot status language
Recommended states:
- `Готов к гостевому входу`;
- `Открываю гостевой вход…`;
- `Имя отправлено / вход подтверждается`;
- `Требуется ручное подтверждение в браузере`;
- `В комнате` only when live evidence is strong enough;
- `Browser speaker signal unavailable` should be presented as capability absence, not as an error in transcription.

Never display `В комнате` only because slug IAPI returned a participant record.

## Browser live speaker display
When explicit browser speaking semantics are available, show them as a **live indicator**, clearly distinct from already-finalized transcript attribution.

0.9 must not silently rewrite old transcript cards based on current browser speaker state because timing alignment is not calibrated.

If browser speaker evidence is missing, the UI falls back to acoustic/unknown state without inventing a name.

## Settings and security controls
The visual layer must preserve these behaviors:
- DION token/mTLS values are not persisted for convenience;
- real meeting URL is not copied into diagnostics;
- guest browser automation remains optional/fallback-friendly;
- diarization remains opt-in;
- persistent Voice ID remains separately opt-in.

## Spacing and shape
Use an 8 px rhythm where practical. Typical radii 8–14 px. Cards use subtle borders/restraint; avoid heavy gradients or distracting animation.

## Accessibility / long-session use
- strong contrast;
- comfortable pointer targets;
- no rapid blinking;
- recording/failure states readable without color perception;
- preserve native keyboard focus;
- advanced integration fields should not dominate the common guest flow.

## Implementation boundary
Primary implementation: `app/ui.py` (`MODERN_QSS`, `TranscriptCardView`, `MainWindow`).

0.9 adds guest-flow controls including `dion_room_url_edit`, `secretary_auto_join`, parsed slug feedback and advanced `dion_api_base_edit`, while preserving the 0.8 shell.

Browser/URL semantics are governed by `DION_INTEGRATION.md`; privacy rules by `PRIVACY_SECURITY.md`.

## Validation
Before 0.9 release:
- source test suite passes;
- Windows CI instantiates `MainWindow` with `QT_QPA_PLATFORM=offscreen`;
- smoke asserts room-URL primary flow and advanced API controls exist;
- packaged EXE self-test passes;
- actual guest-form/browser rendering on target corporate DION remains a field check after publication.
