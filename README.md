# DION Meeting Assistant

Windows-приложение для локальной стенографии ВКС DION, подготовки протокола, интеграции с DION и диагностики качества аудио/распознавания.

Текущая опубликованная версия: **0.8 Visual Refresh**.

## Для человека

- Последний релиз: `v0.8-visual-refresh`.
- Portable EXE: `DION_Meeting_Assistant_0.8_Visual_Refresh_Portable.exe`.
- Release: `https://github.com/Zios86/test/releases/tag/v0.8-visual-refresh`.
- Прямая загрузка: `https://github.com/Zios86/test/releases/download/v0.8-visual-refresh/DION_Meeting_Assistant_0.8_Visual_Refresh_Portable.exe`.
- Размер: `627,541,530 bytes`.
- SHA-256: `0ea963916ecf00d9bf9ef219377e709718d1c5d458ec656fc54f5527d43f3fa9`.
- Portable EXE содержит offline Whisper `small`; облачный STT не требуется.
- Основной режим: системный звук Windows через WASAPI Loopback + отдельный микрофон пользователя.
- 0.8 переносит одобренный современный интерфейс в native PySide6/QSS: левая навигация, карточки стенограммы, верхняя статус-панель, правая сводка и нижняя панель быстрых действий.
- Hardening 0.7.1 сохранён: DION mTLS, opt-in diarization, active-only Voice ID, DPAPI-защита persistent voice profiles, Secretary Bot lifecycle hardening.
- Корпоративная DION/mTLS/WASAPI и визуальная проверка на целевом АРМ остаются отдельным полевым этапом и не считаются доказанными только по CI.

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
- `dion-visual/apply_080.py` — native PySide6 visual refresh 0.8;
- `.github/workflows/build-dion-portable.yml` — восстановление проекта, применение патчей, locked dependencies, pinned offline models, Qt offscreen smoke-test, PyInstaller, packaged self-test и публикация Release.

Не изучайте `part*` по одному. Логическая карта восстановленного Python-проекта находится в `docs/PROJECT_MAP.md`.

## Документация

- `docs/README.md` — индекс документации;
- `docs/PROJECT_MAP.md` — куда идти за конкретной функцией;
- `docs/ARCHITECTURE.md` — архитектура и потоки данных;
- `docs/DEVELOPMENT.md` — запуск, тесты, зависимости, модели и сборка;
- `docs/design-docs/UI_VISUAL_SYSTEM.md` — каноническая дизайн-система 0.8;
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

## Проверка 0.8

Для восстановленного исходника основной тестовый набор:

```bash
python -m pytest -q
```

При разработке visual refresh локальный рабочий набор прошёл **48/48 тестов** и `compileall`.

Опубликованный `v0.8-visual-refresh` дополнительно прошёл:

- первоначальный Windows PR CI `33129215245`;
- release-guard PR CI `33145190036`;
- финальный production Windows CI `33145419554`;
- locked dependency validation;
- pinned offline-model verification;
- Qt `offscreen` construction smoke-test для нового `MainWindow`;
- one-file EXE build;
- packaged `--portable-selftest`;
- успешную публикацию GitHub Release.

Первый production-run `33129501062` также успешно дошёл до EXE/self-test, но был остановлен ошибкой release-existence guard; этот guard исправлен PR #3 до финальной публикации.

## Репозиторий

GitHub сейчас сообщает `Zios86/test` как **public**. Если проект должен быть закрытым, visibility необходимо переключить в настройках GitHub; секреты, private keys, токены, реальные стенограммы и данные участников нельзя коммитить независимо от visibility.
