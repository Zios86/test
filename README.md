# DION Meeting Assistant

Windows-приложение для локальной стенографии ВКС DION, подготовки протокола, интеграции с DION и диагностики качества аудио/распознавания.

Текущая **опубликованная** версия: **0.8 Visual Refresh**.

Текущая **ветка разработки**: **0.9 Guest Secretary Bot** (`dion-guest-bot-0.9`). 0.9 реализована и локально протестирована, но ещё не считается опубликованным Release до прохождения Windows PR CI, merge, production build и GitHub Release.

## Для человека

### Опубликованный релиз

- Release: `v0.8-visual-refresh`.
- Portable EXE: `DION_Meeting_Assistant_0.8_Visual_Refresh_Portable.exe`.
- Release page: `https://github.com/Zios86/test/releases/tag/v0.8-visual-refresh`.
- Direct download: `https://github.com/Zios86/test/releases/download/v0.8-visual-refresh/DION_Meeting_Assistant_0.8_Visual_Refresh_Portable.exe`.
- Size: `627,541,530 bytes`.
- SHA-256: `0ea963916ecf00d9bf9ef219377e709718d1c5d458ec656fc54f5527d43f3fa9`.

### Что меняется в 0.9

Основной пользовательский сценарий DION упрощён:

```text
Ссылка на встречу DION
https://корпоративный-dion/join/room-slug
        ↓
Секретарь-бот
        ↓
отдельный Edge/Chrome guest profile
        ↓
автозаполнение имени / «Войти как гость»
        ↓
видимый ручной fallback, если автоматизация недоступна
```

- `event_id` больше не требуется для обычного гостевого входа.
- Из `/join/<slug>` автоматически извлекается slug конференции.
- Поддерживаются корпоративные/on-prem DION host names, включая URL не на `dion.vc`.
- Integration API, token и mTLS остаются **необязательными расширенными настройками** для дополнительных метаданных.
- API base URL теперь настраивается для корпоративного deployment.
- При наличии IAPI участники могут запрашиваться по slug; этот результат **не считается доказательством текущего присутствия** пользователя в комнате.
- Добавлен best-effort localhost-only browser adapter для гостевого входа и чтения только явных participant/speaking DOM/ARIA-сигналов.
- Программа не угадывает активного спикера по цвету, обычному тексту страницы или состоянию «микрофон включён».
- Browser active-speaker пока служит live-индикатором и не переподписывает задержанные Whisper chunks до полевой калибровки времени.
- Основной звук для STT по-прежнему берётся через Windows WASAPI Loopback; отдельный per-user DION media stream не заявляется.

0.8 visual shell и 0.7.1 hardening сохраняются: offline Whisper `small`, shared PortAudio, mTLS privacy rules, opt-in diarization, DPAPI voice profiles.

## Для Claude, ChatGPT, Codex и других ИИ

Не начинайте работу с полного сканирования репозитория.

1. Прочитайте `AGENTS.md` или `CLAUDE.md`.
2. Откройте `docs/PROJECT_MAP.md`.
3. Проверьте последние релевантные записи `docs/VERSION_JOURNAL.md`.
4. Читайте только профильный design/development doc и mapped source files.
5. После значимого изменения обновите документацию по `docs/DOCUMENTATION_POLICY.md`.

`docs/` — единая система истины; chat history является вторичной.

## Release/build-ветка

`dion-exe-build` восстанавливает базовый source и применяет патчи по порядку:

- `dion-hotfix/apply_051.py` — stability 0.5.1;
- `dion-quality/apply_060.py` — recognition quality 0.6;
- `dion-secretary-bot/apply_070.py` — Secretary Bot/IAPI/speaker fallback 0.7;
- `dion-hardening/apply_071.py` — mTLS/privacy/lifecycle/release hardening 0.7.1;
- `dion-visual/apply_080.py` — native PySide6 Visual Refresh 0.8;
- `dion-guest-bot/apply_090.py` — room-URL-first Guest Secretary Bot 0.9.

Не изучайте encoded `part*` по одному. Логическая карта проекта — в `docs/PROJECT_MAP.md`.

## Документация

- `docs/README.md` — индекс;
- `docs/PROJECT_MAP.md` — карта модулей/маршрутизация;
- `docs/ARCHITECTURE.md` — runtime/data flow;
- `docs/DEVELOPMENT.md` — тесты, dependencies, models, CI/release;
- `docs/design-docs/UI_VISUAL_SYSTEM.md` — UI system 0.8+;
- `docs/design-docs/DION_INTEGRATION.md` — Guest Bot, browser adapter, optional IAPI/mTLS;
- `docs/design-docs/SPEAKER_IDENTIFICATION.md` — diarization/Voice ID/overlap;
- `docs/design-docs/SPEECH_RECOGNITION.md` — Whisper/VAD/context;
- `docs/design-docs/PRIVACY_SECURITY.md` — secrets, guest profile, DevTools, voice profiles;
- `docs/VERSION_JOURNAL.md` — append-only engineering history;
- `docs/RELEASES.md` — только фактически опубликованные binaries;
- `docs/ROADMAP.md` — направление;
- `docs/exec-plans/CURRENT.md` — активный план;
- `CHANGELOG.md` — user-visible history.

## Validation status 0.9

На чистом восстановленном source 0.8 после применения `apply_090.py`:

```text
36/36 tests passed
compileall passed
```

Это **не** означает, что 0.9 уже прошла Windows PR CI или реальную корпоративную DION field validation. До Release должны пройти Windows Qt smoke, pinned models, PyInstaller, packaged `--portable-selftest`, merge и production publication.

## Репозиторий

GitHub ранее сообщал `Zios86/test` как `public`. Если проект должен быть закрытым, visibility требуется изменить в настройках GitHub. Токены, private keys, реальные meeting URLs, стенограммы и данные участников нельзя коммитить независимо от visibility.
