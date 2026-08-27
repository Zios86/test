# Architecture

## Goal
DION Meeting Assistant is a Windows offline-first meeting secretary. The current line combines 0.5.1 audio safety, 0.6 Russian STT quality, 0.7 Secretary Bot control-plane integration and 0.7.1 hardening.

## Runtime flow
```text
DION IAPI (token + mTLS)
  POST /invites ---------------------> named guest "Секретарь-бот"
  GET /events/{event_id}/users ------> active participant/session roster
                |
                +--> participant names -> Whisper prompt / Voice-ID candidates

DION/Windows output -- WASAPI Loopback --+
Local microphone -------------------------+--> bounded AudioChunk queue
                                                |
                                                v
                                      TranscriptionWorker
                                      faster-whisper small
                                      VAD + context + hotwords
                                                |
                           if diarization is enabled (opt-in)
                                                |
                                                +--> SpeakerDiarizationProcess
                                                     separate spawned process
                                                     pyannote segmentation ONNX
                                                     + WeSpeaker embeddings
                                                |
                                                v
                                      speaker-attributed entries
                                                |
                                      TranscriptStore / protocol / export
```

## DION control plane
`app/dion_api.py` owns IAPI requests. UI captures token, PEM certificate, PEM key and optional encrypted-key password before background work starts; workers must not read Qt widgets.

`app/dion_bot.py` owns the named guest browser lifecycle. Normal shutdown attempts best-effort invite revoke; stale `secretary-browser-*` profiles are cleaned later. DION control-plane failure must not terminate local transcription.

The documented IAPI is not treated as a media API. It supplies identity/session metadata, not a documented Windows/Python `active_speaker_user_id` or per-user PCM stream.

## Audio and STT
`app/audio.py` preserves one shared PortAudio context and bounded chunk queues. `app/transcriber.py` runs faster-whisper. When diarization is explicitly enabled, word timestamps allow one Whisper segment to be split at acoustic speaker-handoff boundaries. Without diarization the lower-cost segment path remains.

## Speaker processing
Diarization is opt-in. Native sherpa-onnx processing runs in a spawned subprocess; timeout/crash degrades attribution instead of closing the GUI/STT path. Voice-name matching considers only currently active DION participants and prefers `unknown` over a false real name.

## Voice-profile persistence
Cross-meeting persistence is opt-in. On Windows it is protected by current-user DPAPI. Persisted records contain `user_id` plus embedding/sample metadata, not participant name/e-mail; current names are overlaid from the live DION roster.

## Failure boundaries
- DION/mTLS/guest-browser failure -> local transcription remains available.
- Speaker subprocess failure -> anonymous/less-specific speaker attribution.
- STT/model failure -> active transcription fails cleanly rather than through an intended native crash path.
- Queue backlog -> bounded drops/health metrics rather than unlimited growth.
- Protocol/local-AI failure -> transcript remains authoritative/saved.

## Invariants
1. One shared PortAudio context.
2. Offline STT by default.
3. Long native/STT/network work off the Qt UI thread.
4. Bounded audio queue.
5. Missing names/assignees/deadlines are not invented.
6. Diagnostics exclude transcript/audio/tokens/invite secrets/key passwords.
7. Diarization remains opt-in until field performance is proven.
8. Published releases are immutable.
9. Portable releases require Windows CI and packaged self-test.
