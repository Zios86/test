# Privacy and security design

DION Meeting Assistant processes sensitive meeting audio/text. Default STT is local/offline and meeting artifacts are not automatically uploaded.

## Sensitive data
Raw microphone/system audio, transcripts, participant identities, meeting content/protocol, DION credentials/invite secrets and voice embeddings are sensitive.

## DION credentials / mTLS
0.7.1 UI accepts access token, PEM client certificate, PEM private key and optional encrypted-key password. Certificate/key must be supplied as a pair. Credentials are captured before background workers start and kept in memory only; do not write them to autosave, diagnostics, logs or repository fixtures. PFX/P12 import is not implemented yet.

## Temporary audio
Recognition uses temporary WAV chunks which are removed after normal processing. Autosave/diagnostics do not archive raw audio. Any future final-pass mode requiring audio retention must be explicit opt-in with retention/deletion rules.

## Secretary Bot browser
A dedicated temporary `secretary-browser-*` profile is used when possible. Normal shutdown attempts invite revoke and profile close; stale temporary profiles are cleaned on later startup. Invite URL/hash must not appear in diagnostic artifacts.

## Voice identity
Persistent Voice ID is opt-in. Persisted profiles exclude participant name/e-mail and contain technical `user_id` + embedding/sample metadata. On Windows persistent payload is protected by current-user DPAPI. Live DION roster supplies current display identity.

Unknown/ambiguous matches stay unknown. Automatic matching considers active participants only and uses conservative thresholds.

## Diagnostics
Technical diagnostics may include device/model/version/queue/latency/error state but must exclude transcript text, raw meeting audio, access tokens, invite secrets, private-key password and unnecessary full personal paths.

## Local AI
Optional Ollama protocol refinement remains loopback-only (`localhost`, `127.0.0.1`, `::1`) and is not an external cloud dependency.

## Supply-chain/repository rules
- Do not commit real corporate audio/transcripts/credentials/participant fixtures.
- CI dependencies/model artifacts are pinned/verified for release reproducibility.
- Published releases are immutable.
- Authenticode remains future work until a signing certificate is provisioned.
- Repository privacy/branch protection/default branch are administrative controls and must be independently configured/verified.

## Invariants
1. Offline STT default.
2. No automatic external upload of meeting content.
3. Credentials remain memory-only.
4. Diagnostics exclude meeting content/secrets.
5. Biometric persistence is opt-in and protected on Windows.
6. Uncertain speaker identity is never guessed as a real name.
