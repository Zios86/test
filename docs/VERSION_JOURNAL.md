# Version and update journal

Этот файл — хронологический инженерный журнал проекта DION Meeting Assistant.

Он дополняет, но не заменяет:

- `/CHANGELOG.md` — заметные пользовательские изменения;
- `RELEASES.md` — опубликованные GitHub Release и бинарные артефакты;
- `ROADMAP.md` — текущее состояние и планы.

## Обязательное правило

Любое значимое изменение проекта должно получить запись в этом журнале **в той же задаче**, в которой выполнено изменение.

Журнал ведётся по принципу append-only: после принятия записи в рабочую историю старые записи не переписываются для соответствия новой реализации. Если ранее записанный факт оказался ошибочным, добавляется новая корректирующая запись со ссылкой на исходную.

## Формат записи

Для каждого изменения фиксировать:

- `ID` — `YYYY-MM-DD.NN`;
- дата;
- версия/ветка;
- тип: `feature`, `fix`, `quality`, `docs`, `build`, `security`, `refactor`, `release`;
- статус: `implemented`, `tested`, `released`, `experimental`, `reverted`;
- цель изменения;
- изменённые компоненты/файлы;
- что изменилось;
- проверки и их результат;
- известные ограничения/риски;
- связанные Release/коммиты/артефакты;
- SHA-256 бинарника, если изменение выпущено как EXE;
- способ отката/предыдущая стабильная версия, если применимо.

## Текущая версия

**1.0 Post-meeting Precision** — текущий опубликованный релиз (`v1.0-post-meeting-precision`).

Следующий кандидат: **1.0.1 Audit Hardening** (`v1.0.1-audit-hardening`).

Основной опубликованный rollback: **0.8 Visual Refresh**.

## 2026-08-28.08 — 1.0.1 Audit Hardening

- Версия/ветка: `1.0.1` / `dion-audit-fixes-1.0.1`.
- Тип: `fix`, `quality`, `security`, `build`, `docs`.
- Статус: `implemented`, `tested`; Windows CI/release pending.
- Закрыты пункты аудита 1–4, 6–7: безопасные ZIP/manifest, отказоустойчивая WAV-запись, объединение draft/Whisper, последовательная очередь сервера, сквозные тесты и полевые сценарии 15/60/120 минут.
- Правки Ollama, меняющие числа или большую часть строки, не применяются автоматически.
- Локально: `46 passed`, compileall приложения и сервера, воспроизведение всей цепочки патчей успешно.
- Пункт 5 (TLS/VPN и автоматическая очистка аудио) исключён владельцем из этого выпуска.
- Запланированы `DION_Meeting_Assistant_1.0.1_Audit_Hardening_Portable.exe` и `DION_Postprocess_Server_1.0.1.zip`; SHA фиксируются после публикации.

---

## 2026-08-28.07 — Post-meeting Precision

- Версия/ветка: `1.0.0` / `dion-postprocess-1.0`.
- Тип: `feature`, `quality`, `security`, `build`.
- Статус: `implemented`, `tested`; Windows CI/release pending.
- Цель: сохранить исходную запись и после встречи повысить точность стенограммы на отдельном LAN-компьютере.

### Изменения
- System audio и микрофон непрерывно записываются в разные WAV без дополнительного PortAudio-потока.
- GUI отправляет WAV и исходный autosave только по явной команде пользователя; передача больших файлов потоковая.
- Добавлен отдельный authenticated/allowlisted сервер для faster-whisper `large-v3-turbo` и консервативной коррекции Ollama `qwen3:4b`.
- Исходники не изменяются; результат сохраняется отдельно, подозрительные сильные исправления помечаются для проверки.
- При недоступности Ollama сохраняется точная Whisper-расшифровка; при недоступности сервера live STT продолжает работать.

### Изменённые компоненты
- `app/audio.py`, `app/postprocess.py`, `app/ui.py`;
- `postprocess-server/`;
- `dion-postprocess/apply_100.py`, workflow, тесты и проектная документация.

### Проверка
- `42 passed`; `compileall` исходного приложения и сервера прошёл.
- Проверены private-IP policy, состав пакета, отсутствие аудио и защита от ZIP path traversal.

### Ограничения/риски
- AMD FX-6350 CPU будет обрабатывать long-form audio медленнее реального времени.
- HTTP допустим только в доверенной изолированной LAN; для недоверенной сети нужен VPN/TLS proxy.
- Требуются полевые тесты Windows WASAPI, продолжительной записи, реального качества и объёма диска.

### Release/артефакт
- `DION_Meeting_Assistant_1.0_Post_Meeting_Precision_Portable.exe` и `DION_Postprocess_Server_1.0.zip` запланированы; SHA фиксируются только после публикации.

### Откат
- Текущий опубликованный `v0.9.1-browser-gate-hotfix`.

Стабильные резервные точки для отката: **0.8 Visual Refresh**, **0.7.1 Hardening**, **0.7 Secretary Bot**, **0.6 Quality**, **0.5.1 Safe** в зависимости от характера регрессии.

---

## 2026-08-28.06 — 0.9.1: corporate browser gate hotfix

- Версия/ветка: `0.9.1`, `dion-browser-gate-0.9.1`
- Тип: `fix`, `quality`, `build`, `docs`
- Статус: `implemented`, `tested`, `unreleased`
- Цель: поддержать подтверждённый полевой сценарий корпоративного DION, где до формы гостевого входа показывается отдельный экран «Переход в Конференции».

### Изменения

- `DionBrowserAdapter.attempt_guest_join()` сначала ищет видимую кнопку «Продолжить в браузере» и нажимает только web-вариант;
- после перехода повторные попытки находят поле имени, вводят `Секретарь-бот` и нажимают «Войти как гость»;
- сохранён ручной visible fallback;
- добавлен патч `dion-browser-gate/apply_091.py` и тест двухэтапной последовательности;
- release workflow подготовлен для отдельного immutable-релиза `v0.9.1-browser-gate-hotfix`.

### Проверка

Reconstructed source: `37/37 tests passed`. Windows CI, packaged self-test и публикация EXE ещё не завершены.

### Ограничения/риски

DOM корпоративного DION может меняться; при отсутствии распознаваемой видимой кнопки автоматизация должна оставлять браузер для ручного входа. Реальный переход и вход требуют повторной проверки на целевом АРМ.

### Откат

Опубликованный rollback: `v0.9-guest-secretary-bot`.

## 2026-08-28.05 — v0.9 Guest Secretary Bot опубликован

- Версия/ветка: `v0.9-guest-secretary-bot`, `dion-exe-build`
- Тип: `release`, `build`, `docs`
- Статус: `released`
- Цель: зафиксировать фактический выпуск реализации 0.9, описанной в записи `2026-08-28.04`, после успешных Windows PR и production gates.

### Проверка

- PR #4 Windows CI `33150603611` — success: source validation, pinned offline models, PyInstaller build и packaged `--portable-selftest`; Release step skipped by design.
- Merge commit: `f5ae18ef98d26236e9c7f5f42aa5b7e685c5a7e6`.
- Production Windows CI `33150927129` — success: tests, pinned models, EXE build, packaged self-test и GitHub Release publication.

### Release/артефакт

- Release: `https://github.com/Zios86/test/releases/tag/v0.9-guest-secretary-bot`
- Artifact: `DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe`
- Размер: `627,722,376 bytes`
- SHA-256: `3e57b7c1fac965a14518d6eecc86642bcd3367af1fcf66af01e71142c09aef22`
- Target commit: `f5ae18ef98d26236e9c7f5f42aa5b7e685c5a7e6`

### Ограничения/риски

Release/CI не доказывает реальную корпоративную DION guest-form/DOM совместимость, waiting-room behavior, browser active-speaker semantics, browser↔WASAPI timing или длительную стабильность реального аудио. Основной STT media source остаётся Windows WASAPI Loopback.

### Откат

Основной опубликованный rollback: `v0.8-visual-refresh`.

---

## 2026-08-28.04 — 0.9 Guest Secretary Bot: room-URL-first guest flow

- Версия/ветка: `0.9.0`, `dion-guest-bot-0.9`
- Тип: `feature`, `fix`, `security`, `build`, `docs`
- Статус: `implemented`, `tested`, `unreleased`
- Цель: сделать обычную гостевую ссылку DION основным способом подключения Секретаря-бота, убрать обязательность `event_id`/token/mTLS для обычного пользователя и подготовить безопасный browser-based metadata/speaker adapter без ложных утверждений о live presence/active speaker.

### Изменения

- основной пользовательский вход теперь принимает HTTPS URL вида `/join/<slug>`;
- `parse_dion_join_url()` извлекает host/slug и не жёстко привязан к `dion.vc`, поэтому поддерживает corporate/on-prem DION;
- URL с embedded username/password отвергаются;
- `SecretaryBotController.prepare_guest()` готовит гостевой режим без DION IAPI client;
- `event_id` убран из обычного 0.9 UI flow;
- имя `Секретарь-бот` и auto-join checkbox находятся в primary guest section;
- Edge/Chrome запускается в отдельном временном профиле, в private/incognito режиме по возможности, с `--mute-audio`;
- DevTools endpoint привязан к `127.0.0.1`, порт выбирается динамически;
- `DionBrowserAdapter.attempt_guest_join()` best-effort заполняет имя и нажимает `Войти как гость`/`Join as guest`;
- если DevTools/DOM/enterprise policy не позволяют автоматизацию, остаётся visible manual guest fallback;
- добавлен `DionBrowserAdapter.probe_room_state()` с capability-gated чтением только явных participant/speaking data/ARIA semantics;
- запрещено выводить speaker identity из CSS color/highlight, generic text, participant order или microphone-enabled state;
- browser active-speaker state пока используется только как live indicator и не переподписывает delayed Whisper chunks до field timing calibration;
- DION Integration API перенесён в optional advanced settings;
- API base URL стал настраиваемым для corporate deployment;
- добавлен `DionIntegrationClient.list_event_users_by_slug()` для `GET /events/slug/<slug>`;
- slug-IAPI metadata намеренно возвращается без доказанного `is_active=true`: implemented response не рассматривается как live presence source;
- legacy event-id invite/users API остаётся для backward compatibility;
- main STT media source остаётся Windows WASAPI Loopback; per-user DION PCM не заявляется;
- добавлен locked dependency `websocket-client==1.8.0` для localhost DevTools WebSocket;
- release chain расширена `dion-guest-bot/apply_090.py` после `apply_080.py`;
- workflow подготовлен к будущим `DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe` и `v0.9-guest-secretary-bot`;
- временный development workflow для экспорта reconstructed 0.8 source был создан для точной разработки 0.9 и удалён до merge candidate.

### Изменённые компоненты

- `dion-guest-bot/apply_090.py` и multipart payload;
- логические `app/dion_bot.py`, `app/dion_api.py`, `app/ui.py`, `app/health.py`, `app/__init__.py`;
- `requirements.txt`, `requirements-ci.lock.txt`;
- `tests/test_guest_bot_09.py` и связанные DION/UI tests;
- `.github/workflows/build-dion-portable.yml`;
- canonical docs: README/AGENTS/CLAUDE, PROJECT_MAP, ARCHITECTURE, DEVELOPMENT, DION_INTEGRATION, PRIVACY_SECURITY, UI_VISUAL_SYSTEM, ROADMAP, CURRENT, RELEASES, CHANGELOG, AI handoff/skills.

### Проверка

Reconstructed 0.8 source + `apply_090.py`:

```text
36/36 tests passed
compileall passed
```

Новые tests подтверждают:
- parsing `https://corporate-host/join/<slug>`;
- reject non-join URL;
- Guest Bot без API token;
- slug API path без `event_id`;
- slug roster не заявляет `is_active=true`;
- manual browser fallback;
- successful guest-click state не считается доказательством `room_observed=true`;
- primary 0.9 UI содержит room URL/auto-join и advanced API base, без primary `dion_event_id_edit`.

Windows PR CI, Qt smoke, packaged EXE self-test, merge и production Release на момент этой записи **ещё не зафиксированы как завершённые**.

### Ограничения/риски

- automatic guest form automation зависит от фактического DOM и enterprise browser policy;
- browser participant/speaking semantics в corporate DION ещё не field-validated;
- отсутствие сильных DOM semantics должно приводить к `capability unavailable`, а не к угадыванию;
- browser live-speaker timing относительно WASAPI/Whisper не откалиброван;
- browser speaker state не применяется ретроспективно к transcript chunks;
- slug IAPI metadata не является current-presence proof;
- main STT audio всё ещё WASAPI mixed output;
- CI не может доказать фактический guest join/waiting-room/live speaker behavior.

### Release/артефакт

На момент записи **нет опубликованного 0.9 Release** и поэтому нет достоверных size/SHA-256.

Planned identity:

```text
v0.9-guest-secretary-bot
DION_Meeting_Assistant_0.9_Guest_Secretary_Bot_Portable.exe
```

Фактические metadata должны быть добавлены отдельной released-записью после успешного production publication.

### Откат

Опубликованный rollback: `v0.8-visual-refresh`.

При DION-интеграционной регрессии также доступны `v0.7.1` / `v0.7-secretary-bot`.

---

## 2026-08-28.03 — v0.8 Visual Refresh опубликован

- Версия/ветка: `v0.8-visual-refresh`, `dion-exe-build`
- Тип: `feature`, `fix`, `build`, `docs`, `release`
- Статус: `released`
- Цель: применить одобренный современный дизайн непосредственно к Windows-приложению, сохранив hardening/аудио/STT/DION-инварианты 0.7.1, и выпустить проверенный portable EXE.

### Изменения

- старый утилитарный single-screen UI заменён native PySide6/QSS оболочкой;
- добавлена левая навигация из семи страниц: Встреча, Стенограмма, Протокол, Участники, Секретарь-бот, Диагностика, Настройки;
- live transcript переведён на `TranscriptCardView` с карточками спикеров;
- активный спикер получает состояние `Говорит`, overlap/interruption — отдельное состояние `Перебивание`;
- добавлена верхняя live-status панель для встречи/записи/аудио/DION;
- добавлена правая summary rail: участники, активный спикер, качество аудио, черновик протокола, горячие слова;
- добавлена постоянная нижняя панель Start/Stop/DOCX/Protocol;
- сохранены pause, decision/task markers, DION/mTLS, Voice ID, diagnostics и recognition settings;
- добавлен canonical design doc `docs/design-docs/UI_VISUAL_SYSTEM.md`;
- в release workflow добавлен Qt `offscreen` smoke-test, который реально создаёт/закрывает новый `MainWindow` на Windows runner;
- в цепочку release-патчей добавлен `dion-visual/apply_080.py`;
- исправлен release-existence guard: ожидаемый exit code `1` от `gh release view` при отсутствии релиза больше не прерывает PowerShell раньше проверки `$LASTEXITCODE`.

### Изменённые компоненты

- `dion-visual/apply_080.py` и multipart payload;
- логические `app/ui.py`, `app/health.py`, `app/__init__.py`;
- `tests/test_visual_refresh.py`;
- `.github/workflows/build-dion-portable.yml`;
- `docs/design-docs/UI_VISUAL_SYSTEM.md`;
- canonical release/project/AI navigation docs.

### Проверка

Локальный visual-refresh workspace:

```text
48/48 tests passed
compileall passed
```

Первичный Visual Refresh PR:

```text
run 33129215245 — success
```

Прошли source checks, Qt offscreen `MainWindow` smoke, pinned-model validation, PyInstaller EXE build и packaged `--portable-selftest`; Release step был пропущен как и должен быть в PR.

Первый production run после merge:

```text
run 33129501062 — failure only at Release publication guard
```

При этом source validation, Qt smoke, models, EXE build и packaged self-test были успешны. Причина: `$PSNativeCommandUseErrorActionPreference = $true` превратил ожидаемый non-zero результат `gh release view` для несуществующего тега в исключение раньше guard-логики.

Workflow-only исправление прошло PR #3:

```text
run 33145190036 — success
```

Финальный production run:

```text
run 33145419554 — success
```

Успешно прошли source checks, Qt smoke, pinned models, EXE build, packaged self-test, Release publication и Actions artifact upload.

Примечание о тестах: локальный workspace имел 48 тестов; reconstructed Windows release workflow в логах финальной линии обнаруживает 29 pytest tests. Эти два набора не следует молча считать одним и тем же.

### Release/артефакт

Release:

`https://github.com/Zios86/test/releases/tag/v0.8-visual-refresh`

Artifact:

`DION_Meeting_Assistant_0.8_Visual_Refresh_Portable.exe`

Размер:

`627,541,530 bytes`

SHA-256:

`0ea963916ecf00d9bf9ef219377e709718d1c5d458ec656fc54f5527d43f3fa9`

Target commit:

`b7ee9bb5017348a83b99e48246a65c5309d35315`

### Ограничения/риски

CI/self-test не доказывают:

- точное соответствие UI ожиданиям на пользовательском Windows-мониторе, DPI/scaling и разрешении;
- удобство интерфейса в длительной рабочей встрече;
- реальную авторизацию в корпоративном DION с production mTLS;
- фактический Secretary Bot lifecycle в корпоративной комнате;
- длительную стабильность WASAPI + microphone на целевом АРМ;
- качество speaker attribution/overlap и фактическую WER/CER русской речи.

### Откат

При визуальной/навигационной регрессии — `v0.7.1`.

При регрессии DION/Voice ID — `v0.7-secretary-bot`.

При необходимости отката интеграционной линии — `v0.6-quality`.

---

## 2026-08-28.02 — v0.7.1 Hardening опубликован

- Версия/ветка: `v0.7.1`, `dion-exe-build`
- Тип: `security`, `fix`, `build`, `release`
- Статус: `released`
- Цель: закрыть найденные после аудита 0.7 риски DION/mTLS, Voice ID, хранения голосовых профилей, lifecycle Секретаря-бота и воспроизводимости релизной сборки.

### Изменения

- добавлена настройка DION mTLS client certificate + PEM key + optional key password;
- diarization возвращена в opt-in состояние по умолчанию;
- автоматический Voice ID ограничен текущими активными участниками DION;
- persistent voice-profile payload больше не хранит имя/e-mail участника;
- Windows persistence голосовых профилей защищена DPAPI;
- на нормальном завершении приложение пытается аннулировать invite Секретаря-бота;
- добавлена очистка устаревших временных guest-browser profiles;
- при активной diarization Whisper использует word timestamps и может разделять текст на speaker handoff;
- ужесточены cross-meeting Voice ID thresholds;
- Windows CI dependencies зафиксированы lock-файлом;
- Whisper revision и speaker-model inputs закреплены manifest/hash значениями;
- GitHub Actions закреплены immutable commit SHA;
- PR-build не публикует Release;
- release workflow запрещает перезапись существующего тега `v0.7.1` и требует bump версии.

### Изменённые компоненты

- `dion-hardening/apply_071.py` и multipart payload;
- `.github/workflows/build-dion-portable.yml`;
- `release/model-manifest.json`;
- логические модули `app/dion_api.py`, `app/dion_bot.py`, `app/speaker_profiles.py`, `app/speakers.py`, `app/transcriber.py`, `app/ui.py`, `app/health.py`;
- тесты DION/Voice ID/transcriber;
- canonical docs, включая DION и speaker-identification design docs.

### Проверка

PR Windows CI:

```text
run 33126146077 — success
```

В PR прошли:

- применение цепочки патчей 0.5.1 -> 0.6 -> 0.7 -> 0.7.1;
- locked dependency validation;
- automated test suite;
- pinned offline model verification;
- PyInstaller one-file EXE build;
- packaged `--portable-selftest`.

Production Windows CI после merge:

```text
run 33126756679
```

Успешно прошли тесты, model verification, EXE build, packaged self-test и Release publication.

### Release/артефакт

Release:

`https://github.com/Zios86/test/releases/tag/v0.7.1`

Artifact:

`DION_Meeting_Assistant_0.7.1_Hardening_Portable.exe`

Размер:

`627,528,485 bytes`

SHA-256:

`90751e2d7a71a5bbcf3e3f0e185284ba08099244779ad8174f0afb89ada04239`

Target commit:

`a8f8a08d1f80f25fa6281ec16fe171e5ac788776`

### Ограничения/риски

CI/self-test не доказывают:

- реальную авторизацию в корпоративном DION с production mTLS;
- фактический join/revoke/waiting-room lifecycle Секретаря-бота;
- длительную стабильность WASAPI + microphone на целевых АРМ;
- качество определения спикеров при перекрывающейся речи;
- фактическую WER/CER русской речи.

Эти пункты остаются field-test pending.

### Откат

При регрессии DION/Voice ID — `v0.7-secretary-bot`.

При необходимости отката интеграционной линии — `v0.6-quality`.

При проблемах запуска аудио — `v0.5.1-safe` как историческая stability fallback.

---

## 2026-08-28.01 — v0.7 Secretary Bot зафиксирован в канонической истории

- Версия: `v0.7-secretary-bot`
- Тип: `feature`, `release`
- Статус: `released`
- Цель: добавить управляемого Секретаря-бота DION, roster участников и расширенную локальную speaker-attribution архитектуру поверх 0.6 Quality.

### Изменения

- добавлен flow `DION -> Секретарь-бот -> Подключить/Статус/Отключить`;
- создаётся индивидуальный DION invite с видимым именем `Секретарь-бот`;
- guest открывается в отдельном временном browser profile с отключённым выводом звука;
- приложение получает список участников/сессий через DION IAPI;
- добавлен статус присутствия бота и активных участников;
- локальный sherpa-onnx speaker engine изолирован в отдельном процессе;
- снят искусственный лимит пяти удалённых голосов;
- overlapping speech помечается `[ПЕРЕБИВАНИЕ]`;
- 0.6 Quality STT profile сохранён.

### Проверка

Windows production build и packaged self-test для опубликованного релиза прошли успешно до публикации.

### Release/артефакт

Release:

`https://github.com/Zios86/test/releases/tag/v0.7-secretary-bot`

Artifact:

`DION_Meeting_Assistant_0.7_Secretary_Bot_Portable.exe`

Размер:

`627,522,154 bytes`

SHA-256:

`704dfcab816ac687f592baa6ff6c0feea785cd24b920eaf7594fe5e0364a00da`

### Ограничения

Документированный DION IAPI не предоставляет проверенный Windows/Python live active-speaker user ID или отдельные per-user live media tracks. Реальное корпоративное DION поведение требовало полевой проверки, что стало одной из причин последующего hardening 0.7.1.

### Откат

Предыдущий релиз: `v0.6-quality`.

---

## 2026-08-27.05 — Введён обязательный журнал версий и обновлений

- Версия/ветка: `dion-exe-build`
- Тип: `docs`
- Статус: `implemented`
- Цель: обеспечить непрерывную историю не только Release, но и всех значимых изменений между версиями.

### Изменения

- создан `docs/VERSION_JOURNAL.md`;
- журнал объявлен обязательной частью любого значимого изменения;
- правила ведения журнала добавлены в `DOCUMENTATION_POLICY.md`, `AGENTS.md`, `CLAUDE.md` и профильные Skills;
- опубликованный Release по-прежнему отдельно фиксируется в `RELEASES.md`.

### Политика

Каждое следующее значимое изменение получает новую запись; released-изменения дополнительно получают запись в `RELEASES.md`, а пользовательские изменения — в `CHANGELOG.md`.

---

## 2026-08-27.04 — Документационная система Claude + OpenAI/Codex

- Версия/ветка: `dion-exe-build`
- Тип: `docs`
- Статус: `implemented`
- Цель: обеспечить переносимую документацию проекта между Claude, ChatGPT Work и Codex и сократить повторное чтение проекта ИИ.

### Изменения

- добавлен корневой `AGENTS.md` для OpenAI/Codex;
- добавлен корневой `CLAUDE.md` для Claude;
- `docs/` назначен единой canonical knowledge base;
- добавлены `PROJECT_MAP.md`, `ARCHITECTURE.md`, `DEVELOPMENT.md`, `DOCUMENTATION_POLICY.md`, `AI_HANDOFF.md`, `ROADMAP.md`, `RELEASES.md`;
- добавлены профильные design docs;
- добавлены Skills `project-navigation`, `documentation-maintenance`, `release-process`;
- закреплён progressive-disclosure workflow: карта проекта -> профильный документ -> конкретный код;
- добавлено обязательное правило синхронного обновления документации при изменениях проекта.

### Проверка

- структура файлов проверена через GitHub;
- `AGENTS.md` и `CLAUDE.md` оставлены короткими адаптерами и не дублируют всю базу знаний.

### Ограничения

Текущая ветка всё ещё хранит приложение как реконструируемое дерево из `dion-portable` + patch scripts. Переход к обычному исходному дереву остаётся отдельной инфраструктурной задачей.

---

## 2026-08-27.03 — v0.6 Quality

- Версия: `v0.6-quality`
- Тип: `quality`, `release`
- Статус: `released`
- Цель: улучшить качество русской и технической речи после анализа реальной стенограммы DION.

### Изменения

- offline Whisper `small` вместо `base`;
- beam search `5`;
- контекст предыдущих реплик между фрагментами;
- редактируемый словарь терминов/FIO/систем;
- встроенная рабочая терминология;
- VAD настроен для коротких русских реплик;
- соседние короткие сегменты объединяются;
- стандартный chunk увеличен с 8 до 12 секунд;
- safety-архитектура общего PortAudio из 0.5.1 сохранена;
- diarization остаётся выключенной по умолчанию.

### Проверка

- reconstructed source: `25` automated tests passing;
- Windows packaged EXE self-test passed before publication.

### Артефакт

`DION_Meeting_Assistant_0.6_Quality_Portable.exe`

Размер: `621,933,502 bytes`.

SHA-256:

`85a8d0b443b4e07c6b5df16b255775ed7c960da5cc0ddc9e9bab51a5d3658334`

Release: `https://github.com/Zios86/test/releases/tag/v0.6-quality`

### Ограничения

Качество требует повторной полевой проверки на реальном аудио DION; CI/self-test не измеряет фактическую WER русской речи.

### Откат

Резервная версия: `v0.5.1-safe`.

---

## 2026-08-27.02 — v0.5.1 Safe

- Версия: `v0.5.1-safe`
- Тип: `fix`, `stability`, `release`
- Статус: `released`
- Цель: устранить аварийное закрытие приложения при запуске стенографии.

### Изменения

- Loopback и микрофон переведены на общий PortAudio-контекст;
- синхронизировано открытие/закрытие аудиопотоков;
- diarization выключена по умолчанию;
- сохранена offline-архитектура.

### Проверка

- shared PortAudio context check passed on Windows CI;
- packaged EXE self-test passed.

### Ограничения

Portable-сборка использовала Whisper `base`; реальная стенограмма показала недостаточное качество русской и технической речи.

Release: `https://github.com/Zios86/test/releases/tag/v0.5.1-safe`

---

## 2026-08-27.01 — 0.5 Diagnostics/Stability

- Версия: `0.5`
- Тип: `diagnostics`, `stability`
- Статус: `implemented`
- Цель: подготовить приложение к реальному полевому тесту на Windows/DION и сделать деградацию/ошибки наблюдаемыми.

### Изменения

- preflight-проверка;
- вкладка диагностики;
- состояния READY / OK / DEGRADED / CRITICAL;
- ограниченная аудиоочередь;
- учёт потерянных chunks;
- latency/queue metrics;
- обезличенные diagnostic/crash reports;
- field-test checklist;
- корректная обработка хвоста очереди при завершении встречи.

### Проверка

По канонической истории 0.5 имела 20 автоматических тестов; реальный WASAPI/DION захват требовал полевой проверки на Windows.

---

## Исторические версии до 0.5

Точные Release-метаданные для части ранних MVP не были зафиксированы в канонической документации при их создании. Ниже сохраняется только подтверждённая функциональная история; отсутствующие даты/SHA не выдумываются.

### 0.4

Тип: `feature`, `local-ai`.

Добавлено:

- локальное улучшение формулировок через Ollama;
- запрет внешних AI endpoint;
- structured JSON output;
- защита от изменения AI количества решений/поручений, ответственных и сроков;
- review-блок для неполных поручений.

### 0.3

Тип: `feature`.

Добавлено:

- автоматическое формирование протокола;
- решения;
- поручения;
- ответственные;
- сроки;
- открытые вопросы;
- экспорт протокола TXT/DOCX/JSON.

### 0.2

Тип: `feature`, `experimental`.

Добавлено:

- локальное различение удалённых голосов;
- `remote_N` идентификаторы;
- ручное сопоставление спикера с ФИО;
- обновление имени во всей стенограмме и экспорте.

### 0.1

Тип: `mvp`.

Базовые возможности:

- Windows WASAPI Loopback;
- отдельный микрофон;
- локальный faster-whisper;
- live transcript с временными метками;
- ручные метки решения/поручения;
- TXT/DOCX/JSON;
- autosave;
- диагностика устройств;
- PyInstaller/GitHub Actions build path.

---

## Шаблон следующей записи

```markdown
## YYYY-MM-DD.NN — Краткое название

- Версия/ветка:
- Тип:
- Статус:
- Цель:

### Изменения
- ...

### Изменённые компоненты
- `path/file.py`

### Проверка
- ...

### Ограничения/риски
- ...

### Release/артефакт
- ...

### Откат
- ...
```
