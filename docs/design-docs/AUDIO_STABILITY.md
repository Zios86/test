# Audio capture and native stability

## Scope

This document covers Windows WASAPI capture, microphone coexistence, PortAudio lifetime, start/stop behavior and native crash risk.

Primary logical module: `app/audio.py`.

Related modules: `app/ui.py`, `app/preflight.py`, `app/crash.py`, `app/health.py`.

## Audio sources

The application captures two independent logical sources:

1. **System/DION audio** through a Windows WASAPI loopback device.
2. **Local microphone** directly from the selected input device.

The local microphone is separate because the user's own voice normally is not present in the Windows output loopback signal.

## Important limitation

WASAPI loopback captures the selected Windows output device, not an isolated DION application stream. Other sounds routed to that output may enter the transcript.

Examples:

- Windows notifications;
- browser media;
- another communication app.

Do not describe the current implementation as process-exclusive DION capture.

## Shared PortAudio context invariant

0.5.1 introduced a critical safety change:

```text
one shared PyAudio/PortAudio context
        ├── loopback worker
        └── microphone worker
```

Do **not** revert to one `PyAudio()` instance per capture thread without a proven reason and Windows stress tests.

Logical functions:

```text
_acquire_pa()
_release_pa()
```

Stream open/close is coordinated so multiple Python threads do not race through PortAudio initialization/termination.

## Why this matters

Native audio libraries can terminate the entire process through an access violation or DLL-level fault. Such failures may bypass normal Python exception handling.

Therefore stability depends on architecture, not only `try/except` blocks.

## Capture worker

`AudioCaptureWorker` owns one selected source stream.

Responsibilities:

- open device using its native rate/channels;
- read PCM frames continuously;
- handle pause without creating a delayed backlog;
- flush chunks to temporary WAV files;
- emit technical errors without blocking the UI;
- flush a final partial chunk on normal stop.

## Pause behavior

During pause, the worker should continue draining the device stream while discarding frames. If reading stops completely, PortAudio/device buffers can accumulate old audio that is then transcribed after resume.

## Stop behavior

Normal stop sequence:

1. request audio workers to stop;
2. workers flush any meaningful partial chunk;
3. transcription worker is told no more chunks are expected;
4. transcription worker drains the existing queue;
5. UI marks the session fully stopped only after the transcription tail is complete.

Do not immediately discard the recognition queue when the user presses Stop.

## Preflight

Before a meeting, `probe_audio_device()` and `run_preflight()` should identify ordinary device/configuration failures while the app is still idle.

Preflight should distinguish:

- blocking errors;
- warnings/degraded optional capabilities;
- healthy state.

Diarization model absence should not block ordinary transcription when diarization is disabled.

## Error handling

### Python-level device error

Show/log the error and return the application to a safe idle state when possible.

### Repeated read error

Record it in health metrics and avoid flooding the user with modal dialogs.

### Native crash

`crash.py` can report uncaught Python exceptions but cannot guarantee capture of all native access violations. Minimize native-library concurrency and use field testing.

## Speaker diarization interaction

`sherpa-onnx` adds another native runtime. After the observed start-time instability, diarization is disabled by default.

Before enabling it automatically:

- consider process isolation;
- test long sessions with system + mic capture;
- test startup/shutdown repeatedly;
- prove a diarization failure does not terminate STT.

## Queue and disk behavior

Audio chunks are temporary files passed to the STT worker. The queue is bounded so a slow recognizer cannot create unlimited files.

When full:

- record a dropped chunk;
- surface degraded health;
- prefer controlled data loss over exhausting disk/memory and crashing the entire meeting.

## Field test matrix

Test after audio/native changes:

| Case | Expected result |
|---|---|
| Loopback only | starts/stops repeatedly without crash |
| Mic only/dev probe | selected device opens cleanly |
| Loopback + mic | both work with shared context |
| Pause/resume | paused audio is not replayed later |
| Stop mid-sentence | final partial chunk is processed |
| Device missing | preflight reports actionable error |
| Slow STT | queue health degrades without unbounded growth |
| DION + Windows notification | limitation understood; notification may be captured |

## Invariants

1. Shared PortAudio context.
2. Bounded queue.
3. Drain devices during pause.
4. Flush useful tail on stop.
5. Do not block the Qt UI thread with audio reads.
6. Diarization off by default until isolated/tested.
7. Preflight before live meeting capture.
