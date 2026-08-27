# Project map

Эта карта создана, чтобы не перечитывать весь проект при каждой задаче.

## 1. С чего начинать

Если задача относится к приложению, сначала найдите её в таблице **«Куда идти за изменением»** ниже. Не сканируйте весь репозиторий и не читайте `dion-portable/part*` по одному.

Если задача продолжает недавнее изменение, касается регрессии или версии — после карты откройте последние релевантные записи `VERSION_JOURNAL.md`.

## 2. Физическая структура release/build-ветки

```text
/
├── README.md
├── AGENTS.md
├── CLAUDE.md
├── CHANGELOG.md
├── docs/
│   ├── README.md
│   ├── PROJECT_MAP.md
│   ├── ARCHITECTURE.md
│   ├── DEVELOPMENT.md
│   ├── DOCUMENTATION_POLICY.md
│   ├── VERSION_JOURNAL.md
│   ├── RELEASES.md
│   ├── ROADMAP.md
│   ├── AI_HANDOFF.md
│   ├── design-docs/
│   └── exec-plans/
├── .claude/skills/
├── .github/workflows/build-dion-portable.yml
├── dion-portable/
│   ├── part00
│   ├── part01
│   └── ...
├── dion-hotfix/
│   └── apply_051.py
├── dion-quality/
│   └── apply_060.py
└── dion-build/
    └── project.b64.part00   # исторический/служебный сборочный фрагмент
```

### Что здесь является чем

- `dion-portable/part*` — base64-фрагменты ZIP базового Python-проекта. Это транспортный формат для Windows CI, а не удобный формат чтения исходников.
- `dion-hotfix/apply_051.py` — накладывает изменения 0.5.1 Safe на восстановленный проект.
- `dion-quality/apply_060.py` — накладывает изменения 0.6 Quality: Whisper small, beam 5, контекст, hotwords, VAD и UI словаря.
- `.github/workflows/build-dion-portable.yml` — текущий production release pipeline ветки: восстановление -> патчи -> зависимости -> offline-модели -> PyInstaller onefile -> self-test -> GitHub Release.
- `docs/VERSION_JOURNAL.md` — append-only инженерная история значимых изменений между версиями и релизами.

## 3. Логическая структура восстановленного приложения

После восстановления исходника рабочее дерево выглядит так:

```text
run.py
app/
├── __init__.py
├── audio.py
├── crash.py
├── health.py
├── local_ai.py
├── models.py
├── preflight.py
├── protocol.py
├── speakers.py
├── storage.py
├── transcriber.py
└── ui.py
requirements.txt
build_exe.bat
install_and_run.bat
diagnostics.py
diagnostics.bat
check_local_ai.py
check_local_ai.bat
download_speaker_model.py
download_speaker_model.bat
tests/
```

## 4. Карта модулей и ключевых символов

### `run.py` — точка входа

Ключевое:
- `_portable_selftest()` — быстрый тест уже упакованного EXE без GUI/аудиоустройств;
- устанавливает crash handlers до запуска UI;
- затем вызывает `app.ui.run_app()`.

Менять, если: меняется запуск приложения, self-test, обязательные bundled assets.

### `app/ui.py` — оркестратор приложения и весь GUI

Класс: `MainWindow`.

Ключевые участки:
- `_build_ui()` — создаёт интерфейс;
- `refresh_devices()` / `_fill_device_combo()` — аудиоустройства;
- `run_preflight_ui()` — проверка перед ВКС;
- `start_session()` / `_start_session_impl()` — запуск стенографии;
- `toggle_pause()` / `stop_session()` — управление сессией;
- `on_transcript()` — добавление результата распознавания;
- `refresh_speaker_controls()` / `rename_selected_speaker()` — работа со спикерами;
- `_autosave()` — автосохранение;
- `refresh_protocol_preview()` — обновление автопротокола;
- `enhance_protocol_local_ai()` — локальное улучшение через Ollama;
- `refresh_health_view()` / `save_diagnostic_report()` — диагностика;
- `export_transcript()` / `export_protocol()` — экспорт;
- `closeEvent()` — безопасное завершение.

Менять, если: кнопки, поля настроек, жизненный цикл встречи, взаимодействие модулей.

### `app/audio.py` — Windows WASAPI/PortAudio

Ключевые символы:
- `_acquire_pa()` / `_release_pa()` — **общий PortAudio-контекст** для потоков. Это safety fix 0.5.1; не возвращать отдельный `PyAudio()` на каждый поток;
- `AudioDeviceManager` — Loopback/микрофоны/default device;
- `AudioCaptureWorker` — поток чтения PCM и нарезка WAV chunks;
- `_flush_chunk()` — создаёт `AudioChunk`;
- `probe_audio_device()` — безопасная проверка устройства перед запуском.

Менять, если: устройство, захват, chunking, pause/resume, нативные падения при старте.

### `app/transcriber.py` — качество распознавания

Класс: `TranscriptionWorker`.

Ключевые участки 0.6:
- `_normalize_hotwords()` — очистка словаря терминов;
- `_initial_prompt()` — подсказка Whisper из словаря и контекста;
- `_remember()` — контекст предыдущих реплик;
- `run()` — загрузка WhisperModel и очередь chunks;
- `_speaker_for_segment()` — связь распознавания с diarization;
- `_merge_candidates()` — объединение соседних коротких сегментов;
- `_transcribe_chunk()` — параметры faster-whisper: язык, beam search, VAD, prompt/hotwords.

Менять, если: точность русского языка, словарь, chunk context, задержка, VAD, beam, Whisper model.

### `app/speakers.py` — diarization / speaker embeddings

Ключевое:
- `EmbeddingClusterer` — online-кластеризация embedding в `speaker_*`;
- `SpeakerIdentifier` — ONNX speaker embedding через sherpa-onnx;
- `identify_from_wav()` — определяет кластер по WAV-сегменту.

Статус: функциональность существует, но **выключена по умолчанию** после 0.5.1 из-за риска нативных DLL. Не включать автоматически без отдельной проверки/изоляции.

### `app/protocol.py` — детерминированный автопротокол

Классы данных:
- `ProtocolDecision`;
- `ProtocolTask`;
- `ProtocolQuestion`;
- `ProtocolReviewItem`;
- `MeetingProtocol`.

Главный класс: `ProtocolAnalyzer`.

Ключевые участки:
- `analyze()` — основной проход по стенограмме;
- `_looks_like_task()` / `_looks_like_open_question()` — классификация;
- `_append_decision()` / `_append_task()` / `_append_question()` — формирование пунктов;
- `_assignee()` / `_deadline()` — ответственный и срок;
- `_participants()` / `_topics()` / `_summary()` — метаданные встречи;
- `_review_items()` — что требует ручной проверки.

Менять, если: правила протокола, поручения, решения, сроки, дедупликация.

### `app/local_ai.py` — опциональный Ollama

Класс: `OllamaProtocolEnhancer`.

Ключевое:
- разрешены только loopback-hostnames;
- `_assert_local_endpoint()` блокирует внешние адреса;
- `enhance()` отправляет уже структурированный протокол;
- `_validate_shape()` не позволяет AI менять структуру/количество пунктов произвольно.

Менять, если: формат локального AI, модель, schema/validation. Не превращать этот модуль в обязательную зависимость.

### `app/storage.py` — стенограмма и экспорт

Класс: `TranscriptStore`.

Ключевое:
- start/finish/add;
- speaker aliases;
- форматирование текста;
- экспорт TXT/JSON/DOCX.

Менять, если: формат файлов, состав JSON, отображение имён, экспорт.

### `app/models.py` — DTO

- `AudioChunk` — метаданные временного WAV-фрагмента;
- `TranscriptEntry` — одна реплика стенограммы + speaker metadata.

Менять, если: контракт между audio/transcriber/storage/UI.

### `app/health.py` — runtime health

- `SourceStats`, `TranscriptionStats`, `HealthEvent`, `PreflightCheck`;
- `SessionHealth` — очередь, потери, latency, warning/fatal, status, export.

Менять, если: метрики, READY/OK/DEGRADED/CRITICAL, диагностический отчёт.

### `app/preflight.py` — проверка до ВКС

- `_dependency_check()`;
- `_folder_check()`;
- `_whisper_model_check()`;
- `run_preflight()`;
- `has_blocking_errors()`.

Менять, если: новые обязательные зависимости, модели, папки, критерии блокировки запуска.

### `app/crash.py` — crash reports

- `_redact()` — маскировка чувствительных путей/данных;
- `write_crash_report()`;
- `install_crash_handlers()`.

Менять, если: аварийная диагностика. Не добавлять стенограмму/аудио в crash report.

## 5. Инструменты

- `diagnostics.py/.bat` — диагностика окружения без GUI;
- `check_local_ai.py/.bat` — проверка Ollama;
- `download_speaker_model.py/.bat` — получение speaker model для dev-установки;
- `build_exe.bat` — локальная Windows-сборка unpacked-проекта;
- `install_and_run.bat` — dev-установка и запуск.

## 6. Тесты

| Тест | Что покрывает |
|---|---|
| `tests/test_transcriber_quality.py` | hotwords/context/merge и Quality profile 0.6 |
| `tests/test_storage.py` | стенограмма и экспорт |
| `tests/test_speakers.py` | кластеризация спикеров |
| `tests/test_protocol.py` | базовый автопротокол |
| `tests/test_protocol_v04.py` | расширенные правила протокола |
| `tests/test_health.py` | метрики/diagnostics |
| `tests/test_crash.py` | crash report/redaction |
| `tests/test_local_ai.py` | Ollama safety/schema |

Текущий baseline 0.6: `25 passed`.

## 7. Куда идти за изменением

| Задача | Сначала читать | Основные файлы |
|---|---|---|
| Плохо распознаёт речь | `design-docs/SPEECH_RECOGNITION.md` | `app/transcriber.py`, `app/ui.py` |
| Теряет начало/конец фраз | `design-docs/SPEECH_RECOGNITION.md` | `app/audio.py`, `app/transcriber.py` |
| Закрывается при старте | `design-docs/AUDIO_STABILITY.md` | `app/audio.py`, `app/ui.py`, `app/crash.py` |
| Не видит DION audio | `design-docs/AUDIO_STABILITY.md` | `app/audio.py`, `app/preflight.py` |
| Ошибочно определяет спикеров | `ARCHITECTURE.md` | `app/speakers.py`, `app/transcriber.py` |
| Неверный протокол | `ARCHITECTURE.md` | `app/protocol.py` |
| Неверный ответственный/срок | `ARCHITECTURE.md` | `app/protocol.py` |
| Изменить DOCX/TXT/JSON | `ARCHITECTURE.md` | `app/storage.py`, `app/protocol.py` |
| Изменить диагностику | `design-docs/PRIVACY_SECURITY.md` | `app/health.py`, `app/preflight.py`, `diagnostics.py` |
| Добавить локальный AI | `design-docs/PRIVACY_SECURITY.md` | `app/local_ai.py`, `app/ui.py` |
| Изменить EXE/Release | `DEVELOPMENT.md`, `VERSION_JOURNAL.md` | workflow + patch scripts |
| Новая версия | `DEVELOPMENT.md`, `DOCUMENTATION_POLICY.md`, `VERSION_JOURNAL.md` | workflow, `CHANGELOG.md`, `RELEASES.md` |
| Понять что и когда менялось | `VERSION_JOURNAL.md` | затем только связанные файлы из записи |
| Найти опубликованный EXE/SHA | `RELEASES.md` | GitHub Release |

## 8. Правило поддержания карты

Если файл, класс, ключевая функция или ответственность модуля изменилась — эта карта обновляется **в той же задаче**. Если карта расходится с кодом, задача считается незавершённой.

Если добавлен новый канонический документ, который влияет на навигацию/историю проекта, он также должен быть отражён в этой карте.
