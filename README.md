# DION Meeting Assistant

Windows-приложение для локальной стенографии ВКС DION, подготовки протокола, интеграции с DION и диагностики качества аудио/распознавания.

Текущая опубликованная версия: **0.7.1 Hardening**.

## Для человека

- Последний релиз: `v0.7.1`.
- Portable EXE: `DION_Meeting_Assistant_0.7.1_Hardening_Portable.exe`.
- Release: `https://github.com/Zios86/test/releases/tag/v0.7.1`.
- SHA-256: `90751e2d7a71a5bbcf3e3f0e185284ba08099244779ad8174f0afb89ada04239`.
- Portable EXE содержит offline Whisper `small`; облачный STT не требуется.
- Основной режим: системный звук Windows через WASAPI Loopback + отдельный микрофон пользователя.
- DION 0.7/0.7.1 добавляет Секретаря-бота, participant/session roster, mTLS-конфигурацию и более безопасную локальную speaker-attribution архитектуру.
- Корпоративная DION/mTLS/WASAPI полевая проверка остаётся отдельным этапом и не считается доказанной только по CI.

## Для Claude, ChatGPT, Codex и других ИИ

Не начинайте работу с полного сканирования репозитория.

1. Прочитайте `AGENTS.md` или `CLAUDE.md`.
2. Откройте `docs/PROJECT_MAP.md`.
3. Проверьте последние релевантные записи `docs/VERSION_JOURNAL.md`.
4. Затем читайте только документ и модуль, относящиеся к задаче.
5. После любого значимого изменения обновите документацию по правилам `docs/DOCUMENTATION_POLICY.md`.

`docs/` является **единой системой истины** для знаний о проекте. AI-специфичные файлы содержат только короткие инструкции и ссылки на эту базу.

## Важная особенность release/build-ветки

Ветка `dion-exe-build` является release/build-веткой. Базовый исходник восстанавливается из частей в `dion-portable/`, после чего применяются патчи по порядку:

- `dion-hotfix/apply_051.py` — stability fix 0.5.1;
- `dion-quality/apply_060.py` — recognition quality 0.6;
- `dion-secretary-bot/apply_070.py` — DION Secretary Bot/roster/speaker fallback 0.7;
- `dion-hardening/apply_071.py` — mTLS/privacy/lifecycle/speaker/release hardening 0.7.1;
- `.github/workflows/build-dion-portable.yml` — восстановление проекта, применение патчей, locked dependencies, pinned offline models, PyInstaller, self-test и публикация Release.

Не изучайте `part*` по одному. Логическая карта восстановленного Python-проекта находится в `docs/PROJECT_MAP.md`.

## Документация

- `docs/README.md` — индекс документации;
- `docs/PROJECT_MAP.md` — куда идти за конкретной функцией;
- `docs/ARCHITECTURE.md` — архитектура и потоки данных;
- `docs/DEVELOPMENT.md` — запуск, тесты, зависимости, модели и сборка;
- `docs/design-docs/DION_INTEGRATION.md` — DION IAPI/mTLS/Секретарь-бот;
- `docs/design-docs/SPEAKER_IDENTIFICATION.md` — diarization/Voice ID/overlap;
- `docs/design-docs/SPEECH_RECOGNITION.md` — Whisper/VAD/context;
- `docs/design-docs/PRIVACY_SECURITY.md` — секреты, голосовые профили и диагностика;
- `docs/DOCUMENTATION_POLICY.md` — обязательное обновление документации;
- `docs/VERSION_JOURNAL.md` — append-only инженерная история;
- `docs/RELEASES.md` — фактически опубликованные EXE, размеры и SHA-256;
- `docs/AI_HANDOFF.md` — передача проекта между разными ИИ;
- `docs/ROADMAP.md` — текущее состояние и следующие шаги;
- `CHANGELOG.md` — история пользовательских изменений.

## Проверка 0.7.1

Для восстановленного исходника основной тестовый набор:

```bash
python -m pytest -q
```

0.7.1 reconstructed-source baseline: **46 тестов**.

Опубликованный `v0.7.1` дополнительно прошёл:

- Windows PR CI run `33126146077`;
- production Windows CI run `33126756679`;
- locked dependency validation;
- pinned offline-model verification;
- one-file EXE build;
- packaged `--portable-selftest`;
- успешную публикацию GitHub Release.

## Репозиторий

GitHub сейчас сообщает `Zios86/test` как **public**. Если проект должен быть закрытым, visibility необходимо переключить в настройках GitHub; секреты, private keys, токены, реальные стенограммы и данные участников нельзя коммитить независимо от visibility.
