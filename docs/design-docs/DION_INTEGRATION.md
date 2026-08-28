# DION integration and Guest Secretary Bot

## Scope
0.9 changes the primary integration model from «API credentials first» to **ordinary guest room URL first**.

Primary goal:

```text
https://corporate-dion.example/join/<slug>
        ↓
Секретарь-бот
        ↓
visible guest session
```

Integration API remains optional for additional metadata/control-plane capabilities. The project must not pretend that IAPI or browser UI automatically provides a reliable per-user media stream.

## Primary 0.9 guest flow

User-facing inputs:
- room URL: HTTPS URL containing `/join/<slug>`;
- bot display name, default `Секретарь-бот`;
- checkbox for best-effort automatic guest-name fill/click.

`event_id`, token and mTLS are **not required** just to open the bot as a guest.

### URL parsing
`app/dion_bot.py::parse_dion_join_url()`:
- requires HTTPS;
- rejects user/password embedded in URL;
- searches the path for `/join/<slug>`;
- extracts `host` and `slug`;
- preserves supplied query/fragment;
- does not hard-code `dion.vc`, so corporate/on-prem hosts are supported.

Example shape:

```text
https://dion.russianpost.ru/join/example-room?showWeb=true
                               └── slug: example-room
```

Real production room URLs must not be committed to documentation/tests/logs; use synthetic examples.

## Guest browser lifecycle
`launch_guest_room()` prefers installed Microsoft Edge or Google Chrome on Windows.

The browser session uses:
- separate temporary `secretary-browser-*` profile;
- private/incognito mode where available;
- `--mute-audio` to avoid duplicate audible output from the bot browser;
- a new visible window;
- local DevTools bound to `127.0.0.1` and dynamically assigned port;
- automatic profile deletion on normal session close;
- stale-profile cleanup on later startup.

If the supported browser cannot be controlled, the application opens the URL through the default browser and enters **manual guest mode**.

## Automatic guest entry
`DionBrowserAdapter.attempt_guest_join()` is best-effort only.

It tries to:
1. locate an input whose placeholder/ARIA resembles `Имя` / `name`;
2. set the bot name through the browser DOM;
3. dispatch normal input/change events;
4. find a button whose accessible text resembles `Войти как гость` / `Join as guest`;
5. click it only when not disabled.

Failure modes such as changed markup, security policy or missing DevTools return a reason and leave the visible browser available for manual confirmation.

Automatic entry is **not** a release invariant. Manual guest entry is the required fallback.

## Browser room-state adapter
`DionBrowserAdapter.probe_room_state()` is experimental/capability-gated.

Accepted participant signals:
- visible elements with `data-participant-id` or `data-user-id`;
- explicit participant names from `data-participant-name`, `data-user-name`, `data-name`, or a dedicated participant-name node;
- carefully filtered ARIA labels when they are not merely microphone/camera controls.

Accepted speaking signals:
- explicit `data-speaking=true`;
- explicit `data-is-speaking=true`;
- explicit `data-active-speaker=true`;
- explicit `говорит` / `speaking` semantics in ARIA metadata.

Not accepted as speaker evidence:
- CSS highlight/color/border alone;
- generic DOM text;
- participant ordering;
- microphone enabled/unmuted state;
- API roster membership.

If strong semantics are absent, browser speaker capability stays unavailable.

### Timing limitation
Browser active-speaker state is shown as a live indicator only in 0.9. It is **not** used to retroactively rename delayed Whisper transcript chunks because UI speaker events and STT timestamps have not yet been calibrated on real corporate DION meetings.

## Optional Integration API
Default base remains:

```text
https://api-integration.dion.vc/v1
```

but 0.9 exposes API base URL as an advanced setting because corporate/on-prem deployments may use a different endpoint.

Requests use:
- `X-Client-Access-Token`;
- HTTPS;
- client mTLS where required by deployment.

mTLS fields:
- PEM client certificate (`.pem/.crt/.cer`);
- PEM private key (`.pem/.key`);
- optional encrypted-key password.

Values remain process-memory-only. PFX/P12 import is not implemented.

## Participant metadata by slug
0.9 implements:

```text
GET /events/slug/<event_slug>
```

through `DionIntegrationClient.list_event_users_by_slug()`.

The implementation uses a recent time window and pagination and returns e-mail/join-time metadata available in the implemented response contract.

Crucial rule: this endpoint is **not treated as authoritative live presence** because the implemented response does not provide a reliable leave/current-active property. `is_active` remains unknown for slug-derived rows.

Slug metadata may be used for:
- participant hints;
- display/meeting metadata;
- terminology preparation where appropriate.

It must not automatically make someone an active Voice-ID candidate without stronger live evidence.

## Legacy event-id path
The following 0.7/0.7.1 capabilities remain for backwards compatibility:

```text
POST /invites
GET /invites/<hash>
DELETE /invites/<hash>
GET /events/<event_id>/users
```

They are no longer the normal user-facing path for joining as `Секретарь-бот`.

## What DION metadata means
- Browser explicit participant state can indicate what the web client currently exposes.
- Slug IAPI metadata indicates who was observed/joined in the requested window, not necessarily who is still present.
- Legacy event-id session data can have stronger session semantics where documented/available.
- None of these alone guarantees sentence-level speech attribution.

`microphone enabled != speaking` remains a hard rule.

## Media limitation
0.9 does not claim:
- direct per-user PCM/audio tracks from DION;
- a documented Windows/Python `active_speaker_user_id` IAPI feed;
- that the guest browser audio is routed directly into Whisper.

STT still uses Windows WASAPI Loopback. Local opt-in diarization/Voice ID remains the acoustic fallback.

## Security rules
- Never persist access token, mTLS key password, private key material, real room URL/slug or invite URL/hash in diagnostics/autosave/repository.
- DevTools access is loopback-only and temporary.
- Temporary browser profiles are deleted on normal close and stale profiles are cleaned later.
- Do not automatically send transcript/protocol to DION.
- Use least privilege and deployment-issued credentials.
- Corporate guest-form, browser DOM semantics, IAPI endpoint and mTLS rights require field validation on the target environment.
