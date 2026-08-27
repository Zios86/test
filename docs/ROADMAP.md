# Roadmap

## Current stable direction

Current release line: **0.6 Quality**.

Primary objective: reliable local Russian meeting transcription on Windows/DION before adding more intelligence around speakers and protocol generation.

## Completed

### 0.5.1 Safe

- shared PortAudio context for system loopback + microphone;
- safer native-audio startup;
- diarization disabled by default;
- portable Windows EXE self-test;
- GitHub Release automation.

### 0.6 Quality

- bundled offline Whisper `small` instead of `base`;
- beam search 5;
- previous-utterance context between audio chunks;
- editable terminology/hotwords field;
- built-in support terminology;
- VAD tuned to preserve short Russian utterances;
- merging of neighboring short segments;
- default chunk length increased to 12 seconds;
- all 0.5.1 audio-safety changes preserved.

## Next: 0.6.x quality hardening

Priority order:

1. **Field comparison on real DION meetings**
   - collect new sanitized transcript JSON;
   - compare recurring error categories against 0.5.1;
   - identify terminology/acoustic errors versus chunk-boundary errors.

2. **Adaptive terminology dictionary**
   - persist approved user terms locally;
   - allow import/export of terminology list;
   - separate global terms from meeting-specific terms;
   - never learn raw sensitive phrases automatically without user control.

3. **Confidence/quality markers**
   - expose low-confidence or suspicious fragments for review;
   - avoid pretending uncertain text is reliable.

4. **Session-end quality pass**
   - optionally reprocess accumulated transcript/audio context after the meeting for a cleaner final transcript;
   - keep live transcript fast enough for use during the call.

## Later: safe speaker separation

Current speaker embedding code exists but remains off by default.

Before enabling by default:

- isolate risky native diarization code from the main GUI/STT process where practical;
- prove that diarization failure cannot terminate the transcription session;
- test overlapping speech and speaker switching;
- add manual speaker-name mapping as the authoritative naming mechanism.

## Later: protocol quality

After transcript quality is stable:

- improve deterministic extraction using real sanitized examples;
- add stronger provenance between protocol item and transcript timestamp;
- improve review queue for ambiguous tasks/deadlines;
- preserve "do not invent missing facts" invariant.

## DION integration

Possible future integration:

- meeting metadata/participant list through documented DION APIs;
- protocol delivery through DION chat bot/API where allowed;
- do not assume DION API exposes a live raw audio/active-speaker stream unless documented and verified.

## Documentation/engineering improvement

The repository should eventually move from encoded `dion-portable/part*` + patch scripts to a normal unpacked source tree with ordinary commits/tags. This would reduce build complexity and make code review easier for humans and AI.

Until that migration happens, `docs/PROJECT_MAP.md` is the authoritative navigation layer.

## Definition of done for a roadmap item

An item moves to Completed only when:

- implementation exists;
- relevant automated checks pass;
- Windows/DION-specific limitations are stated;
- affected documentation is updated;
- user-visible behavior is in `CHANGELOG.md`;
- release metadata is updated if published.
