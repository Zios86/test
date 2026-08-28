# Architecture

## 1.0 post-meeting precision flow

Live recognition remains offline and independent. During a meeting, each capture worker writes its normal temporary STT chunks and a continuous `system_audio.wav` or `microphone_audio.wav`. After the transcription queue drains, the user may explicitly start a second-stage job:

```text
immutable draft + two WAV tracks
        -> ZIP package
        -> authenticated private-LAN streaming upload
        -> faster-whisper large-v3-turbo
        -> conservative Ollama correction
        -> precise JSON + corrected TXT + comparison JSON
```

The server is a separate Windows package intended for `192.168.1.128`. It queues jobs asynchronously, streams uploads/downloads, rejects archive path traversal, and binds to the LAN only when an allowed client IP is supplied. Failure of the server, model or Ollama never rewrites the originals and never disables live STT. If Ollama fails, the precise Whisper result remains usable.

The target AMD FX-6350/24 GB system is expected to process recordings substantially slower than real time; this is deliberately a post-meeting path.

## Goal
DION Meeting Assistant is a Windows offline-first meeting secretary. The current development line combines 0.5.1 audio safety, 0.6 Russian STT quality, 0.7 Secretary Bot integration, 0.7.1 hardening, 0.8 Visual Refresh and 0.9 room-URL-first Guest Secretary Bot.

Published baseline remains **0.8 Visual Refresh** until 0.9 completes Windows/release gates.

## Runtime flow — 0.9

```text
User pastes DION room URL
https://corporate-host/join/<slug>
                |
                v
      parse_dion_join_url()
      host + slug + normalized HTTPS URL
                |
                v
        SecretaryBotController
          prepare_guest()
                |
                v
          launch_guest_room()
                |
       isolated Edge/Chrome profile
       audio muted in bot browser
       remote debugging bound to 127.0.0.1
                |
       +--------+------------------+
       |                           |
       v                           v
best-effort DevTools          visible manual guest
name fill + guest click       entry fallback
       |
       v
optional DionBrowserAdapter
  explicit participant IDs
  explicit speaking data/ARIA only
  -> live room/speaker indicator
  -> no retroactive Whisper relabel yet

Optional DION IAPI (token + mTLS + configurable base URL)
  GET /events/slug/<slug> ------> participant metadata hint
                                 current presence NOT proven
  legacy event-id invite/users paths retained for compatibility

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

## Guest browser control plane
`app/dion_bot.py` owns guest-room parsing and browser lifecycle.

Key 0.9 objects:
- `DionRoomLink` — normalized URL/host/slug;
- `parse_dion_join_url()` — accepts HTTPS `/join/<slug>` across corporate/on-prem hosts and rejects credentials embedded in the URL;
- `SecretaryBotController.prepare_guest()` — prepares guest mode without requiring IAPI credentials;
- `launch_guest_room()` — starts Edge/Chrome in an isolated temporary profile with bot audio muted;
- `DionBrowserAdapter` — loopback-only best-effort DevTools adapter;
- `GuestBrowserSession` — process/profile/adapter lifecycle;
- `cleanup_stale_guest_profiles()` — cleans abandoned temporary profiles later.

Guest browser failure, missing DevTools, changed DOM or denied automation must degrade to a visible manual guest-entry flow. It must not disable local transcription.

## Browser observation boundary
The browser adapter is intentionally conservative.

It may use only strong explicit semantics such as:
- `data-participant-id` / `data-user-id`;
- explicit participant name attributes/name nodes;
- `data-speaking`, `data-is-speaking`, `data-active-speaker`;
- explicit speaking wording in ARIA metadata.

It must **not** infer the speaker from:
- CSS color/highlight alone;
- arbitrary nearby text;
- microphone-enabled/muted state;
- roster membership alone.

Browser active-speaker state is currently a live indicator only. Whisper chunks are delayed relative to UI speaker events, so automatic retrospective speaker relabeling is blocked until field timing calibration exists.

## Optional DION Integration API
`app/dion_api.py` remains an optional metadata/control-plane client.

0.9 adds `list_event_users_by_slug()`. The documented slug response exposes participant/join metadata but not a reliable leave/current-active flag in the implemented contract, therefore slug results are treated as historical/current-window roster hints, not as authoritative live presence.

Token, PEM certificate, PEM key and optional key password remain memory-only inputs. API base URL is configurable for corporate deployment.

Legacy event-id invite and `/events/{event_id}/users` support is retained for backwards compatibility, but the primary user flow no longer requires `event_id`.

## Audio and STT
The bot browser is **not** the authoritative media source for 0.9 STT. Meeting audio still comes from Windows WASAPI Loopback, with local microphone captured separately.

`app/audio.py` preserves one shared PortAudio context and bounded chunk queues. `app/transcriber.py` runs faster-whisper. When diarization is explicitly enabled, word timestamps allow a Whisper segment to be split at acoustic speaker-handoff boundaries.

No per-participant DION PCM/media-track capability is claimed.

## Speaker processing
Diarization remains opt-in. Native sherpa-onnx processing runs in a spawned subprocess; timeout/crash degrades attribution instead of closing the GUI/STT path.

Voice-name matching remains conservative. A real name is never assigned merely because that person appears in a room roster. Browser explicit active-speaker evidence and acoustic Voice ID remain separate evidence sources until a calibrated resolver is implemented.

## Voice-profile persistence
Cross-meeting persistence is opt-in. On Windows it is protected by current-user DPAPI. Persisted records contain `user_id` plus embedding/sample metadata, not participant name/e-mail.

## Failure boundaries
- Invalid room URL -> clear validation error before browser launch.
- Browser executable/DevTools/DOM failure -> visible manual guest entry.
- Optional IAPI/mTLS failure -> guest browser and local transcription remain available.
- Browser speaker semantics unavailable -> browser speaker capability remains unavailable; local diarization remains fallback.
- Speaker subprocess failure -> anonymous/less-specific speaker attribution.
- STT/model failure -> active transcription fails cleanly.
- Queue backlog -> bounded drops/health metrics rather than unlimited growth.
- Protocol/local-AI failure -> transcript remains authoritative/saved.

## Invariants
1. One shared PortAudio context.
2. Offline STT by default.
3. Long native/STT/network/browser-probe work off the Qt UI thread.
4. Bounded audio queue.
5. Normal guest entry requires only an HTTPS `/join/<slug>` URL and bot name.
6. Corporate DION hostnames are supported; public-cloud hostname is not hard-coded.
7. Optional slug roster data is not treated as live presence.
8. Microphone-enabled state is not treated as speaking.
9. Browser/roster evidence does not invent speaker identity.
10. Diagnostics exclude transcript/audio/tokens/meeting URL/slug/invite secrets/key passwords.
11. Diarization remains opt-in until field performance is proven.
12. Published releases are immutable.
13. Portable releases require Windows CI and packaged self-test.
