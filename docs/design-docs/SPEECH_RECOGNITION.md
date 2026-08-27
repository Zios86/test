# Speech recognition design

## Scope

This document describes the live Russian STT path: audio chunking, faster-whisper settings, terminology prompting, context and quality trade-offs.

Primary logical module: `app/transcriber.py`.

Related modules: `app/audio.py`, `app/ui.py`, `app/models.py`, `app/health.py`.

## Why 0.6 exists

Real 0.5.1 DION transcript testing showed frequent phonetic substitutions, broken technical vocabulary and low-quality short utterances. The portable `base` model was selected primarily for artifact size, not maximum recognition quality.

0.6 changes priority: recognition quality is more important than keeping the EXE below chat-upload limits. GitHub Release can host the larger artifact.

## Current Quality profile

### Model

Portable release bundles:

```text
Systran/faster-whisper-small
```

The model is local in the packaged application; STT does not need a cloud API.

### Language

Russian is explicitly requested:

```text
language = "ru"
```

### Decoding

Current intended quality setting:

```text
beam_size = 5
```

Reason: evaluate multiple decoding candidates instead of accepting the first greedy path.

### Audio chunk size

Default:

```text
12 seconds
```

Reason: longer chunks provide more lexical context than 8-second chunks while still keeping live latency acceptable for the MVP architecture.

Do not increase chunk size blindly. Larger chunks improve context but increase delay and make queue backlog more expensive.

## Context across chunks

Problem: independent chunk recognition loses sentence context at arbitrary chunk boundaries.

0.6 uses a small history of recently recognized text and builds an `initial_prompt` for the next chunk.

Logical methods:

- `TranscriptionWorker._remember()`;
- `TranscriptionWorker._initial_prompt()`.

Constraints:

- history must be bounded;
- do not feed the entire meeting transcript back into every chunk;
- keep local/system source context separate where appropriate;
- do not allow a mistaken phrase to dominate unlimited future recognition.

## Domain terminology / hotwords

Logical method:

```text
TranscriptionWorker._normalize_hotwords()
```

The UI allows meeting-specific terms such as:

- participant surnames;
- DION;
- Naumen;
- Service Desk;
- КСПД;
- ALT Linux;
- WMS;
- project/system abbreviations.

The terminology list is used as recognition guidance, not as blind post-replacement. A word in the dictionary must not force the model to output it when acoustics do not support it.

Future adaptive dictionary work must be explicit/user-controlled; do not automatically persist arbitrary meeting phrases as trusted terms.

## VAD

Voice activity detection filters silence/noise but aggressive VAD can remove very short responses such as «да», «нет», names or clipped sentence endings.

0.6 adjusts VAD parameters to be less destructive to short Russian speech.

When changing VAD:

- test short utterances;
- test normal continuous speech;
- monitor false speech from background sound;
- monitor empty chunks and dropped meaningful fragments.

## Segment merging

Whisper can emit several adjacent tiny segments that are awkward as separate transcript entries.

`_merge_candidates()` merges appropriate neighboring segments so the live transcript is closer to a natural utterance.

Do not merge across:

- clearly different sources;
- large time gaps;
- known speaker boundaries when diarization is enabled.

## Quality versus latency

Current priority order:

1. preserve meaningful speech;
2. improve lexical correctness;
3. keep UI/session stable;
4. keep latency reasonable;
5. minimize EXE size last.

Health metrics should reveal when the quality profile is too slow for a target PC.

## Error taxonomy for field tests

When comparing transcripts, classify errors rather than only saying «плохо распознаёт»:

1. **Acoustic substitution** — wrong normal word.
2. **Domain term** — product/name/abbreviation distorted.
3. **Boundary truncation** — phrase starts/ends at chunk edge.
4. **Short utterance lost** — VAD/segment issue.
5. **Hallucinated filler** — text without clear speech basis.
6. **Source contamination** — Windows notification/other application sound entered loopback.
7. **Overlap** — two people speak simultaneously.
8. **Speaker attribution** — text is correct but assigned to wrong speaker.

## Tests

Primary automated coverage:

```text
tests/test_transcriber_quality.py
```

The test suite checks Quality-profile logic such as terminology/context/merge behavior. Automated unit tests do not measure real WER without reference audio + human transcript.

## How to measure improvement properly

For a meaningful accuracy number, use the same audio with a manually corrected reference transcript and calculate WER/CER. Comparing two model outputs without ground truth is not sufficient.

For privacy-sensitive corporate testing, keep reference audio/transcript local and sanitize any material committed to the repository.

## Planned quality improvements

See `../ROADMAP.md`.

Likely next work:

- persistent approved terminology lists;
- low-confidence review markers;
- optional session-end re-decode/final pass;
- representative local WER benchmark with sanitized audio.

## Invariants

- offline STT remains default;
- terminology is guidance, not forced substitution;
- context history remains bounded;
- quality changes must be tested for queue/latency impact;
- a faster/lighter profile must not silently replace the Quality release behavior.
