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

**0.7.1 Hardening** — текущий опубликованный релиз.

Предыдущий релиз: **0.7 Secretary Bot**.

Стабильные резервные точки для отката: **0.7 Secretary Bot**, **0.6 Quality**, **0.5.1 Safe** в зависимости от характера регрессии.

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
