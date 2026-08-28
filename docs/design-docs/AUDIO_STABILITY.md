# Audio capture and native stability

## Scope
This document covers Windows WASAPI capture, microphone coexistence, PortAudio lifetime, start/stop behavior, Guest Bot audio interaction and native crash risk.

Primary logical module: `app/audio.py`.

Related modules: `app/ui.py`, `app/dion_bot.py`, `app/preflight.py`, `app/crash.py`, `app/health.py`.

## Audio sources
The application captures two independent logical sources:

1. **System/DION audio** through a Windows WASAPI loopback device.
2. **Local microphone** directly from the selected input device.

The local microphone is separate because the user's own voice normally is not present in Windows output loopback.

## 0.9 Guest Bot audio rule
0.9 opens a second visible DION guest session named `Секретарь-бот` in Edge/Chrome when possible.

That guest browser is launched with:

```text
--mute-audio
```

Purpose: prevent the Secretary Bot browser from playing the same meeting audio into the Windows output device and creating duplicate/echoed content in the WASAPI loopback transcription path.

Important:
- the bot browser is **not** the STT media source;
- STT still listens to the selected Windows loopback device;
- if enterprise browser policy ignores/removes muting, duplicated meeting audio is a field-test risk;
- manual/default-browser fallback may not provide the same process-level mute guarantee, so the user must verify there is no duplicate audible meeting output.

Do not remove `--mute-audio` from the managed Guest Bot browser without a deliberate audio-architecture change and field tests.

## Important limitation
WASAPI loopback captures the selected Windows output device, not an isolated DION application stream. Other sounds routed to that output may enter the transcript.

Examples:
- Windows notifications;
- browser media;
- another communication app;
- a manually opened/unmuted second DION session.

Do not describe current implementation as process-exclusive DION capture.

## Shared PortAudio context invariant
0.5.1 introduced:

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

Stream open/close is coordinated so Python threads do not race through PortAudio initialization/termination.

## Why this matters
Native audio libraries can terminate the process through an access violation or DLL-level fault, bypassing normal Python exception handling. Stability depends on architecture, not only `try/except`.

## Capture worker
`AudioCaptureWorker` responsibilities:
- open device using native rate/channels;
- read PCM continuously;
- handle pause without backlog;
- flush chunks to temporary WAV;
- emit technical errors without blocking UI;
- flush final partial chunk on normal stop.

## Pause behavior
During pause, continue draining the device stream while discarding frames. Otherwise old device-buffer audio can appear after resume.

## Stop behavior
1. request audio workers stop;
2. flush meaningful partial chunks;
3. tell transcription worker no more chunks are expected;
4. drain existing queue;
5. mark session fully stopped only after transcription tail completes.

Do not immediately discard recognition queue when Stop is pressed.

## Preflight
`probe_audio_device()` and `run_preflight()` should identify ordinary device/configuration failures while idle.

Distinguish:
- blocking errors;
- warnings/degraded optional capabilities;
- healthy state.

Diarization model absence must not block ordinary transcription when diarization is disabled.

## Error handling
### Python-level device error
Show/log error and return to safe idle state when possible.

### Repeated read error
Record in health metrics and avoid modal-dialog flooding.

### Native crash
`crash.py` can report uncaught Python exceptions but cannot guarantee capture of every native access violation. Minimize native-library concurrency and field-test repeatedly.

## Speaker diarization interaction
`sherpa-onnx` adds another native runtime. Diarization remains disabled by default.

Before enabling automatically:
- retain process isolation;
- test long sessions with system + mic;
- test startup/shutdown repeatedly;
- prove diarization failure does not terminate STT.

## Queue and disk behavior
Audio chunks are temporary files passed to STT. Queue is bounded so a slow recognizer cannot create unlimited files.

When full:
- record dropped chunk;
- surface degraded health;
- prefer controlled loss over disk/memory exhaustion.

## Field test matrix
| Case | Expected result |
|---|---|
| Loopback only | starts/stops repeatedly without crash |
| Mic only/dev probe | selected device opens cleanly |
| Loopback + mic | both work with shared context |
| Pause/resume | paused audio is not replayed later |
| Stop mid-sentence | final partial chunk is processed |
| Device missing | preflight reports actionable error |
| Slow STT | queue degrades without unbounded growth |
| DION + Windows notification | notification may be captured; limitation understood |
| Managed 0.9 Guest Bot | second browser is muted; no duplicate meeting audio in loopback |
| Manual/default-browser guest fallback | user verifies second DION session is not audibly duplicating meeting audio |
| Guest browser close | no residual browser audio remains after bot disconnect |

## Invariants
1. Shared PortAudio context.
2. Bounded queue.
3. Drain devices during pause.
4. Flush useful tail on stop.
5. Do not block Qt UI thread with audio reads.
6. Diarization off by default until field-tested.
7. Preflight before live meeting capture.
8. Managed Guest Bot browser stays muted in the current WASAPI architecture.
9. Guest browser is not represented as a direct Whisper audio source.
