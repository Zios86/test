# DION integration and Secretary Bot

## Scope
Use documented DION control-plane APIs to create a visible named room participant and retrieve participant/session identity without pretending the documented IAPI is a live media/active-speaker API.

## Base and authentication
Default IAPI base:
```text
https://api-integration.dion.vc/v1/
```

Requests use:
- `X-Client-Access-Token`;
- HTTPS;
- mTLS client authentication where required by DION deployment.

### 0.7.1 mTLS UI
The DION settings accept:
- access token;
- PEM client certificate (`.pem/.crt/.cer`);
- PEM private key (`.pem/.key`);
- optional password for an encrypted private key.

Certificate/key must be supplied as a pair. Values stay in process memory and are captured on the UI thread before background IAPI work starts. PFX/P12 import is not implemented yet.

## Participant roster
```text
GET /events/{event_id}/users
```
Used for DION `user_id`, display identity and session intervals. Current open sessions determine `is_active`. Voice-name matching uses active participants only; historical attendees can still remain useful as meeting metadata.

## Secretary Bot invite
```text
POST /invites
```
Creates an individual invite with the visible name `Секретарь-бот`. The invite is opened in a dedicated temporary Edge/Chrome profile when possible, with bot output muted to avoid audible duplication.

Lookup/revoke support uses the documented invite resources available to the integration.

### Shutdown lifecycle
Normal app close initiates Secretary Bot disconnect/revoke before final GUI close. The revoke request is best-effort: network/API failure must not indefinitely block application shutdown. Old temporary `secretary-browser-*` profiles older than the configured stale threshold are cleaned on later startup.

## What DION metadata means
Roster identity is authoritative for **who is/was in the room**, not automatically **who spoke a sentence**.

A DION microphone-enabled audit signal, where available, is only an auxiliary state. `microphone enabled != speaking`, so it must not be used as exact speaker attribution.

## Current documented limitation
No documented IAPI/Windows-Python path is assumed for:
- `active_speaker_user_id` in real time;
- separate PCM/audio tracks per participant.

Until DION provides/authorizes such a channel, the app uses local opt-in diarization + confirmed/conservative voice mapping for acoustic attribution.

## Security rules
- Never write access token, mTLS key password, invite URL/hash or private key material to autosave/diagnostics/repository.
- Do not automatically send transcript/protocol to DION.
- Use least privilege and deployment-issued credentials.
- Corporate mTLS/room rights require field validation on the target environment.
