# Architecture

## Цель системы

DION Meeting Assistant — локальное Windows-приложение, которое захватывает звук ВКС, распознаёт русскую речь, сохраняет стенограмму, при необходимости различает удалённых говорящих и строит проверяемый протокол встречи.

Главные качества архитектуры:

- offline-first;
- отсутствие обязательного облачного AI;
- отказоустойчивость во время ВКС;
- понятная диагностика;
- возможность проверить и экспортировать результат;
- явное разделение «факт из речи» и «вывод протокола».

## Runtime data flow

```text
DION/Windows output ── WASAPI Loopback ─┐
                                       ├─> AudioCaptureWorker ─> AudioChunk queue
Local microphone ──────────────────────┘                            │
                                                                   v
                                                     TranscriptionWorker
                                                     faster-whisper
                                                     + VAD
                                                     + hotwords
                                                     + prior context
                                                                   │
                                                                   v
                                                        TranscriptEntry
                                                          /       \
                                                         /         \
                                             TranscriptStore    SpeakerIdentifier*
                                                   │                │
                                                   │                └─ speaker aliases
                                                   v
                                             ProtocolAnalyzer
                                                   │
                                                   ├─ MeetingProtocol
                                                   ├─ export TXT/JSON/DOCX
                                                   └─ optional Ollama refinement

* diarization is disabled by default in the current stable profile.
```

## Main component responsibilities

### UI/orchestration

`app/ui.py` owns the meeting lifecycle and wires the components together. Heavy audio/STT work must not execute directly on the Qt UI thread.

### Audio capture

`app/audio.py` owns Windows audio devices and PCM capture. System audio and microphone use one shared PortAudio context. Audio workers write short temporary WAV chunks and enqueue metadata rather than keeping an unbounded in-memory recording.

### Transcription

`app/transcriber.py` consumes chunks serially, runs faster-whisper and emits transcript entries. Version 0.6 adds persistent short-term context and domain terms so chunks are no longer treated as completely independent utterances.

### Speaker identification

`app/speakers.py` calculates embeddings and assigns remote audio to online clusters. Because sherpa-onnx and audio/STT dependencies use native libraries, this feature is kept optional and off by default until its crash boundary is stronger.

### Transcript storage

`app/storage.py` is the canonical in-session transcript store and export layer. Speaker aliases are applied here so a user can rename `speaker_1` once and regenerate views/exports consistently.

### Deterministic protocol

`app/protocol.py` extracts decisions, tasks, deadlines and questions using deterministic rules. It intentionally prefers «не определён/не указан» over guessing missing facts.

### Optional local AI

`app/local_ai.py` refines wording only after a deterministic structure exists. It accepts only localhost endpoints and validates that the AI response preserves the protocol structure.

### Diagnostics

`app/preflight.py` checks the environment before the meeting. `app/health.py` collects runtime counters and health status. `app/crash.py` writes redacted crash reports for uncaught errors.

## Session lifecycle

1. User selects output loopback and microphone.
2. Preflight validates devices, dependencies, model path and writable session directory.
3. UI starts transcription worker and one/two audio workers.
4. Audio workers create chunks and place them in a bounded queue.
5. Transcriber consumes chunks, emits text and updates health metrics.
6. UI appends entries, autosaves transcript and recalculates protocol preview.
7. On stop, audio workers flush partial tail chunks.
8. Transcriber drains the remaining queue before final session completion.
9. Final transcript/protocol/diagnostics are available for export.

## Persistence

Per-meeting directory:

```text
%LOCALAPPDATA%\DIONMeetingAssistant\Sessions\<YYYY-MM-DD_HH-MM-SS>\
```

Typical files:

```text
transcript_autosave.txt
transcript_autosave.json
protocol_autosave.txt
protocol_autosave.json
diagnostic_report.txt
diagnostic_report.json
```

Optional local-AI result:

```text
protocol_ai_autosave.txt
protocol_ai_autosave.json
```

Temporary audio chunks are working files for recognition and are removed after processing in the normal path.

## Failure boundaries

### Audio device failure

Should be detected in preflight or reported as a non-fatal runtime error. A failed mic must not corrupt the system-audio stream and vice versa.

### STT lag

The queue is bounded. The system records dropped chunks instead of allowing unlimited WAV accumulation. Health status becomes degraded/critical based on metrics.

### STT/model failure

A model-loading failure is fatal for the active transcription session but should not terminate the whole GUI process through ordinary Python exceptions.

### Native-library crash

Python `try/except` cannot catch every access violation in PortAudio/CTranslate2/sherpa-onnx. Architecture therefore minimizes simultaneous native contexts and keeps risky diarization off by default.

### Protocol failure

Protocol generation is secondary. Failure must not destroy the saved transcript.

### Local AI failure

Ollama refinement is optional. If it is unavailable or returns invalid structure, deterministic protocol remains authoritative.

## Trust boundaries

- DION audio and microphone are sensitive meeting data.
- Transcript and protocol remain local by default.
- External network services are not required for STT in portable releases.
- Ollama calls are restricted to loopback hosts.
- Diagnostics must contain technical state only, not transcript/audio.

See `design-docs/PRIVACY_SECURITY.md` for the detailed rules.

## Architecture invariants

Do not break these without an explicit design decision:

1. One shared PortAudio context for concurrent capture workers.
2. Offline STT is the default.
3. UI thread does not perform long recognition work.
4. Queue growth is bounded.
5. Transcript survives protocol/AI failures.
6. Missing assignees/deadlines are not invented.
7. Diagnostic artifacts exclude transcript and raw audio.
8. Portable release must pass packaged self-test before publishing.
