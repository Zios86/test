# Speaker identification, diarization and overlap

## Current role
0.9 has several possible identity/speaker evidence sources, but they are intentionally kept separate until field calibration proves how they can be combined.

Sources:
1. **Browser explicit live speaker state** — only when DION web UI exposes strong participant IDs/names and explicit speaking data/ARIA semantics.
2. **Legacy live DION event/session roster** — where the event-id API provides current session semantics.
3. **Slug IAPI participant metadata** — roster hint only; not current-presence proof.
4. **Local sherpa-onnx diarization** — acoustic who-spoke-when when user enables it.
5. **Voice ID** — conservative opt-in mapping of acoustic speaker clusters to eligible live identities.

Diarization remains **off by default** until CPU/latency/stability are proven on target corporate PCs.

## Acoustic pipeline
```text
mixed system WAV
  -> pyannote segmentation ONNX
  -> WeSpeaker embedding
  -> diarization timeline
  -> overlap detection
  -> optional conservative Voice ID
  -> transcript attribution
```

Native diarization runs in a spawned subprocess. Timeout/crash must degrade speaker labeling rather than kill STT/GUI.

## 0.9 browser speaker evidence
`DionBrowserAdapter.probe_room_state()` may expose `active_speakers` only from strong explicit browser semantics.

Accepted:
- participant elements with explicit `data-participant-id` / `data-user-id`;
- explicit participant names;
- `data-speaking=true`;
- `data-is-speaking=true`;
- `data-active-speaker=true`;
- explicit `говорит` / `speaking` ARIA semantics.

Rejected as speaker evidence:
- CSS color/border/highlight;
- arbitrary/generic page text;
- participant order;
- microphone enabled/unmuted state;
- mere roster membership;
- slug API participant row.

If strong semantics are unavailable, browser active-speaker capability is unavailable; the app must not guess.

## Browser/audio timing rule
0.9 **does not retroactively relabel Whisper transcript chunks from current browser active-speaker state**.

Reason: browser UI events and WASAPI/Whisper chunks can have different buffering/latency. Before combining them automatically, field tests must measure:
- browser event timestamp;
- actual audible speech onset in captured WASAPI;
- Whisper word/segment timestamps;
- stable offset/jitter over a meeting.

Until then, browser active speaker is a live UI indicator/evidence channel only.

## Roster eligibility
A real participant name may become a Voice ID candidate only from a source that proves sufficiently current live presence.

- legacy event/session API rows with open sessions may be eligible;
- strong browser participant state may be eligible after field validation of semantics;
- slug IAPI rows have `is_active = unknown` and must not automatically become live Voice-ID candidates.

Roster identity is not sentence-level speech identity.

## Speaker handoffs inside one Whisper segment
When diarization is enabled, faster-whisper requests word timestamps for system audio. Word ranges are matched to diarization intervals so one Whisper segment can be split when the acoustic speaker changes.

Example target:
```text
Иван: Мы решили.
Пётр: Хорошо, сделаю.
```

Word timestamps are not enabled for the normal non-diarization path.

## Overlapping speech
If diarization reports overlapping active speakers, transcript entries can carry `[ПЕРЕБИВАНИЕ]` and multiple labels. This does **not** separate mixed speech into independent clean texts; Whisper still receives mixed system audio.

Browser UI may also show multiple explicit speakers, but it still does not provide separate audio tracks.

True per-speaker transcription requires either authorized per-user media tracks or a dedicated speech-separation stage.

## Voice profiles
Cross-meeting persistence is opt-in.

Persisted form:
- `user_id`;
- embedding centroid/sample metadata;
- update timestamp.

It excludes name/e-mail. On Windows it is protected with current-user DPAPI.

## Conservative identity policy
- Unknown/ambiguous stays unknown.
- Cross-meeting match threshold: `0.78`.
- Minimum margin over the second candidate: `0.08`.
- Do not lower thresholds based only on anecdotal success.
- Do not use slug-only IAPI rows as live candidates.

Thresholds are safety-oriented defaults, not statistically calibrated guarantees. Field testing should measure false accepts/rejects and diarization error before relaxing them.

## Known performance limitation
Whisper and diarization are still coordinated serially for a chunk. Because diarization is opt-in, this is currently tolerated, but future work should decouple speaker analysis from live STT so attribution cannot extend transcript queue latency.

## Next speaker-integration milestone
After 0.9 field testing:
1. verify actual corporate browser participant/speaker semantics;
2. measure browser/audio clock offset and jitter;
3. define a timestamped `SpeakerEvidence`/resolver layer;
4. only then allow browser evidence to influence transcript attribution;
5. preserve `unknown` whenever evidence conflicts or confidence is insufficient.
