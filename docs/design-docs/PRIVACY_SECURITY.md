# Privacy and security design

DION Meeting Assistant processes sensitive meeting audio/text. Default STT is local/offline and meeting artifacts are not automatically uploaded.

## Sensitive data
Sensitive data includes:
- microphone/system audio;
- transcripts/protocol;
- participant identities;
- real DION room URLs and slugs;
- invite links/hashes;
- DION access tokens;
- mTLS certificate/private-key material and key password;
- browser session/profile state;
- voice embeddings.

## 0.9 room URL handling
The primary Guest Bot input is an HTTPS `/join/<slug>` URL.

Security rules:
- reject URLs containing username/password credentials;
- do not hard-code or require a public DION hostname;
- treat real room URL/slug as sensitive meeting-access metadata;
- do not include room URL/slug in diagnostics, crash reports or public bot-state serialization;
- examples/tests use synthetic hostnames/slugs only.

`SecretaryBotState.to_public_dict()` exposes only whether a room is configured, not the real URL/slug.

## Guest browser / DevTools
0.9 starts a dedicated temporary Edge/Chrome guest profile where possible.

The controlled browser is launched with:
- a temporary `secretary-browser-*` user-data directory;
- private/incognito mode when supported;
- bot audio muted;
- remote debugging explicitly bound to `127.0.0.1`;
- a dynamically selected local port.

`DionBrowserAdapter` communicates only with that loopback endpoint. It must not expose the DevTools port on a non-loopback interface.

Automatic guest form interaction and DOM probing are best-effort. If unavailable, the application falls back to a visible manual guest flow. Browser automation failure is not a reason to weaken browser/network isolation.

Temporary profile lifecycle:
- deleted on normal `GuestBrowserSession.close()`;
- stale `secretary-browser-*` directories cleaned on later startup;
- no assumption that abnormal OS/process termination can always clean immediately.

## Browser observation minimization
The browser probe should collect the minimum metadata needed for the product capability.

Allowed live fields are limited to explicit participant ID/name and explicit speaking state exposed by strong data/ARIA semantics.

Do not scrape/store:
- arbitrary page text;
- chat/messages;
- meeting content unrelated to participant/speaker state;
- cookies/session tokens;
- passwords;
- microphone/camera control labels as identity evidence.

The browser adapter must not infer a speaker from colors, CSS highlight, participant order or microphone-enabled state.

Browser active-speaker observations remain ephemeral live state in 0.9 and are not used to rewrite historical transcript speaker names until field timing calibration exists.

## DION credentials / mTLS
Advanced Integration API settings accept:
- configurable API base URL;
- access token;
- PEM client certificate;
- PEM private key;
- optional encrypted-key password.

Certificate/key are supplied as a pair. Credentials remain in process memory and are captured before background workers start. Do not write them to autosave, diagnostics, logs or repository fixtures. PFX/P12 import is not implemented.

Guest entry itself does not require these credentials.

## Slug roster privacy and semantics
`list_event_users_by_slug()` can return participant metadata such as e-mail/join times. Treat this as sensitive personal metadata.

The slug endpoint result is not treated as proof of current room presence. Do not use an uncertain slug roster row to automatically assign a real speaker name.

## Meeting audio and 1.0 final pass
Recognition chunks remain temporary, but 1.0 also creates explicit session evidence files `system_audio.wav` and `microphone_audio.wav`. They remain local unless the user presses the post-processing button. The original WAV files and `transcript_autosave.json` are never rewritten by AI.

Post-processing accepts only an explicit private/loopback IP over HTTP, uses a bearer token, and the network server requires an allowed client IP. This protects the intended trusted LAN workflow but does not provide TLS against a hostile local network; use a trusted isolated LAN or a VPN tunnel when transport confidentiality is required. Tokens must not be committed or logged. Server job directories contain sensitive meeting data and require an operator-defined deletion/retention policy.

## Voice identity
Persistent Voice ID is opt-in. Persisted profiles exclude participant name/e-mail and contain technical `user_id` + embedding/sample metadata. On Windows persistent payload is protected by current-user DPAPI.

Unknown/ambiguous matches stay unknown. A roster entry alone is not speech-identity evidence.

## Diagnostics
Technical diagnostics may include device/model/version/queue/latency/error/capability state but must exclude:
- transcript text;
- raw meeting audio;
- real room URL/slug;
- access token;
- invite secrets;
- private-key material/password;
- unnecessary full personal paths;
- arbitrary browser DOM/page content.

## Local AI
Optional Ollama protocol refinement remains loopback-only (`localhost`, `127.0.0.1`, `::1`) and is not an external cloud dependency.

## Supply-chain/repository rules
- Do not commit real corporate audio/transcripts/room URLs/credentials/participant fixtures.
- CI dependencies/model artifacts are pinned/verified for release reproducibility.
- 0.9 adds `websocket-client==1.8.0` to the locked dependency set for local DevTools WebSocket communication.
- Published releases are immutable.
- Authenticode remains future work until a signing certificate is provisioned.
- Repository privacy/branch protection/default branch are administrative controls and must be independently configured/verified.

## Invariants
1. Offline STT default.
2. No automatic external upload of meeting content.
3. Guest room URL works without API credentials.
4. API credentials remain memory-only.
5. Browser DevTools is loopback-only and temporary.
6. Browser probe reads only minimal explicit participant/speaking semantics.
7. Diagnostics exclude meeting content/access URLs/secrets.
8. Biometric persistence is opt-in and DPAPI-protected on Windows.
9. Uncertain speaker identity is never guessed as a real name.
10. `microphone enabled` is never equated with `speaking`.
11. Post-processing is explicit, private-LAN-only, authenticated and never mutates source evidence.
