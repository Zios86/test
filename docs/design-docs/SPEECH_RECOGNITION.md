# Speech recognition design

## Current quality profile
0.7.1 inherits the 0.6 Quality STT path:
- bundled local `Systran/faster-whisper-small`;
- Russian `language="ru"`;
- beam search 5;
- default 12-second audio chunks;
- bounded previous-utterance context;
- editable domain hotwords/terminology;
- VAD tuned to preserve short Russian utterances;
- neighboring compatible segments may be merged.

STT remains local/offline in portable releases.

## Context and terminology
Context history is bounded and separated by source where appropriate. Hotwords guide recognition; they are not forced post-replacements. Do not automatically persist arbitrary sensitive meeting phrases.

## Speaker handoffs in 0.7.1
Normal transcription keeps word timestamps off to avoid unnecessary live cost.

When **system-audio diarization is explicitly enabled**, faster-whisper requests word timestamps. Word ranges are aligned to the diarization timeline so one Whisper segment can be split when the acoustic speaker changes.

Target:
```text
Иван: Мы решили.
Пётр: Хорошо, сделаю.
```
instead of assigning both phrases to the dominant speaker of one long Whisper segment.

If timestamps/timeline are unavailable, the system falls back to segment-level attribution rather than inventing a split.

## Overlap limitation
Two simultaneous speakers are still mixed in the Windows loopback signal. Diarization can mark `[ПЕРЕБИВАНИЕ]` and overlapping identities, but Whisper does not receive clean per-user tracks. Independent text recovery would require separate DION media tracks or a dedicated speech-separation stage.

## Quality versus latency
Priority: preserve speech -> lexical correctness -> stability -> safe speaker attribution -> reasonable latency -> artifact size.

Diarization remains opt-in because speaker processing is still coordinated with chunk transcription. Future work should make speaker analysis asynchronous so slow diarization cannot extend the live STT queue.

## Field-test taxonomy
Track acoustic substitutions, domain/name distortions, chunk-boundary truncation, lost short utterances, hallucinations, Windows-audio contamination, overlap, wrong speaker attribution, unsplit speaker handoffs, and queue/backlog drops.

Meaningful WER/CER requires the same reference audio plus a manually corrected transcript. Speaker quality additionally requires diarization error and false named-assignment measurements. Prefer `unknown` over a false real name.

## Tests
`tests/test_transcriber_quality.py` covers deterministic context/merge/word-handoff behavior. Unit tests do not measure real acoustic WER on DION calls.

## Invariants
- offline STT default;
- bounded context;
- terminology is guidance;
- word timestamp cost only when diarization needs it;
- quality changes must be evaluated for queue/latency impact;
- do not claim measured field improvement without reference evidence.
