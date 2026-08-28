# Speech recognition design

## Current quality profile
0.9 retains the 0.6/0.7.1 Quality STT path:
- bundled local `Systran/faster-whisper-small`;
- Russian `language="ru"`;
- beam search 5;
- default 12-second audio chunks;
- bounded previous-utterance context;
- editable domain hotwords/terminology;
- VAD tuned to preserve short Russian utterances;
- neighboring compatible segments may be merged.

STT remains local/offline in portable releases.

## Audio source in 0.9
The new Guest Secretary Bot browser does **not** replace the STT audio path.

Primary sources remain:
- DION/system output through Windows WASAPI Loopback;
- local microphone through its separate capture path.

The Guest Bot browser is launched muted to avoid duplicate audible output and is used for room presence/UI metadata probing, not as a direct Whisper PCM provider.

No per-participant DION media stream is claimed.

## Context and terminology
Context history is bounded and separated by source where appropriate. Hotwords guide recognition; they are not forced post-replacements. Do not automatically persist arbitrary sensitive meeting phrases.

Slug IAPI participant metadata may inform user-visible participant hints where appropriate, but it must not be silently converted into forced speaker names or sensitive persistent terminology.

## Speaker handoffs
Normal transcription keeps word timestamps off to avoid unnecessary live cost.

When **system-audio diarization is explicitly enabled**, faster-whisper requests word timestamps. Word ranges are aligned to the acoustic diarization timeline so one Whisper segment can be split when the speaker changes.

If timestamps/timeline are unavailable, fall back to segment-level attribution rather than inventing a split.

## 0.9 browser speaker timing boundary
The browser adapter can sometimes report an explicit live active speaker from DION web UI semantics. That state is **not directly applied to a completed/delayed Whisper chunk in 0.9**.

Reason:
- browser speaker event has its own UI/render/network timing;
- WASAPI capture has buffering;
- chunk creation has timing;
- Whisper inference has latency;
- the current browser observation does not automatically represent who spoke at the earlier audio timestamp.

Before browser speaker evidence can alter transcript attribution, field tests must measure offset/jitter between browser events and captured audio/Whisper timestamps.

## Overlap limitation
Two simultaneous speakers are still mixed in Windows loopback. Diarization/browser UI can indicate overlap or multiple active speakers, but Whisper does not receive clean per-user tracks. Independent text recovery requires authorized separate media tracks or a dedicated speech-separation stage.

## Quality versus latency
Priority: preserve speech -> lexical correctness -> stability -> safe speaker attribution -> reasonable latency -> artifact size.

Diarization remains opt-in because speaker processing is still coordinated with chunk transcription. Future work should make speaker analysis asynchronous so slow diarization cannot extend the live STT queue.

Browser probing is also asynchronous and must never block the STT worker/UI thread.

## Field-test taxonomy
Track:
- acoustic substitutions;
- domain/name distortions;
- chunk-boundary truncation;
- lost short utterances;
- hallucinations;
- Windows-audio contamination;
- overlap;
- wrong speaker attribution;
- unsplit handoffs;
- browser-speaker timing mismatch;
- queue/backlog drops.

Meaningful WER/CER requires the same reference audio plus a manually corrected transcript. Speaker quality additionally requires diarization error and false named-assignment measurements. Prefer `unknown` over a false real name.

## Tests
`tests/test_transcriber_quality.py` covers deterministic context/merge/word-handoff behavior. `tests/test_guest_bot_09.py` confirms 0.9 guest/browser integration does not replace the core STT contract. Unit tests do not measure real acoustic WER on DION calls.

## Invariants
- offline STT default;
- WASAPI remains the meeting-audio source in 0.9;
- bounded context;
- terminology is guidance;
- word timestamp cost only when diarization needs it;
- current browser speaker state does not retroactively relabel uncalibrated delayed audio;
- quality changes must be evaluated for queue/latency impact;
- do not claim measured field improvement without reference evidence.
