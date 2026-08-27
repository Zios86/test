# Speaker identification, diarization and overlap

## Current role
DION provides authoritative participant identity/active-session metadata. Local sherpa-onnx provides acoustic who-spoke-when when the user explicitly enables diarization.

Diarization is **off by default** in 0.7.1 until CPU/latency/stability are proven on target corporate PCs.

## Pipeline
```text
mixed system WAV
  -> pyannote segmentation ONNX
  -> WeSpeaker embedding
  -> diarization timeline
  -> overlap detection
  -> optional Voice ID against active DION participants
  -> transcript attribution
```

Native diarization runs in a spawned subprocess. Timeout/crash must degrade speaker labeling rather than kill STT/GUI.

## Speaker handoffs inside one Whisper segment
When diarization is enabled, faster-whisper requests word timestamps for system audio. Word ranges are matched to diarization intervals so a single Whisper segment can be split when the active speaker changes.

Example target behavior:
```text
Иван: Мы решили.
Пётр: Хорошо, сделаю.
```
instead of assigning both sentences to the dominant speaker of the full segment.

Word timestamps are not enabled for the normal non-diarization path, avoiding unnecessary live cost.

## Overlapping speech
If diarization reports overlapping active speakers, transcript entries can carry `[ПЕРЕБИВАНИЕ]` and multiple speaker labels. This does **not** magically separate mixed speech into independent clean texts; Whisper still receives a mixed system signal. True per-speaker transcription would require DION media tracks or a dedicated speech-separation stage.

## Voice profiles
Cross-meeting persistence is opt-in.

Persisted form:
- `user_id`;
- embedding centroid/sample metadata;
- update timestamp.

It excludes name/e-mail. On Windows the file is protected with current-user DPAPI. Live DION roster overlays names for the current session.

## Conservative identity policy
- Only currently active DION participants are candidate identities.
- Unknown/ambiguous stays unknown.
- Cross-meeting match threshold: `0.78`.
- Minimum margin over the second candidate: `0.08`.

These thresholds are safety-oriented defaults, not statistically calibrated guarantees. Field testing should measure false accepts/rejects and diarization error before relaxing them.

## Known performance limitation
Whisper and diarization are still coordinated serially for a chunk. Because diarization is opt-in, this is acceptable for the hardening release, but a future version should decouple speaker analysis from live STT so delayed speaker attribution cannot increase transcript queue latency.
