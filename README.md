# DION Meeting Assistant

Windows-приложение для локальной стенографии ВКС DION, подготовки протокола, интеграции с DION и диагностики качества аудио/распознавания.

Текущая **опубликованная** версия: **1.0 Post-meeting Precision**. Версия **1.0.1 Audit Hardening** готовится как отдельный неизменяемый исправляющий релиз.

## Для человека

### Опубликованный релиз

- Release: `v1.0-post-meeting-precision`.
- Release page: `https://github.com/Zios86/test/releases/tag/v1.0-post-meeting-precision`.
- Исправляющая линия: `v1.0.1-audit-hardening` (после прохождения Windows CI).
- Size: `627,722,376 bytes`.
- SHA-256: `3e57b7c1fac965a14518d6eecc86642bcd3367af1fcf66af01e71142c09aef22`.
- Target commit: `f5ae18ef98d26236e9c7f5f42aa5b7e685c5a7e6`.

### Что нового в 0.9

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
- API base URL настраивается для корпоративного deployment.
- При наличии IAPI участники могут запрашиваться по slug; этот результат **не считается доказательством текущего присутствия** пользователя в комнате.
- Добавлен best-effort localhost-only browser adapter для гостевого входа и чтения только явных participant/speaking DOM/ARIA-сигналов.
- Программа не угадывает активного спикера по цвету, обычному тексту страницы или состоянию «микрофон включён».
- Browser active-speaker пока служит live-индикатором и не переподписывает задержанные Whisper chunks до полевой калибровки времени.
- Основной звук для STT по-прежнему берётся через Windows WASAPI Loopback; отдельный per-user DION media stream не заявляется.

0.8 visual shell и 0.7.1 hardening сохранены: offline Whisper `small`, shared PortAudio, mTLS privacy rules, opt-in diarization, DPAPI voice profiles.

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
- `dion-browser-gate/apply_091.py` — двухэтапный корпоративный browser gate 0.9.1;
- `dion-postprocess/apply_100.py` — запись и точная пакетная обработка 1.0.

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
- `docs/RELEASES.md` — фактически опубликованные binaries;
- `docs/ROADMAP.md` — направление;
- `docs/exec-plans/CURRENT.md` — активный план;
- `CHANGELOG.md` — user-visible history.

## Validation status 0.9

Reconstructed source + 0.9 patch прошёл локально:

```text
36/36 tests passed
compileall passed
```

Windows PR CI `33150603611` успешно прошёл tests, pinned models, PyInstaller build и packaged `--portable-selftest`; Release step был пропущен как и должен быть в PR.

Production Windows CI `33150927129` повторно прошёл tests, pinned models, EXE build, packaged self-test и успешно опубликовал `v0.9-guest-secretary-bot`.

CI не доказывает фактическое поведение guest join/DOM active-speaker/WASAPI на корпоративном DION — это остаётся field validation.

## Репозиторий

GitHub ранее сообщал `Zios86/test` как `public`. Если проект должен быть закрытым, visibility требуется изменить в настройках GitHub. Токены, private keys, реальные meeting URLs, стенограммы и данные участников нельзя коммитить независимо от visibility.
