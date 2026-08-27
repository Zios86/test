# Privacy and security design

## Scope

DION Meeting Assistant processes potentially sensitive meeting audio and text. The default architecture is intentionally local/offline.

This document defines the data boundaries that should not be weakened accidentally during future development.

## Data classes

### Highly sensitive

- raw microphone audio;
- DION/system meeting audio;
- transcript text;
- speaker names/aliases;
- meeting title/content;
- protocol decisions/tasks/questions.

### Technical diagnostics

- device names/indices;
- queue depth;
- latency counters;
- error counts;
- dependency/model status;
- application/OS versions;
- redacted paths.

Diagnostics must not silently become a copy of meeting content.

## Default network policy

### Speech-to-text

Portable release STT is local and bundled. Do not add a cloud STT dependency to the default pipeline without explicit project approval and corresponding documentation/security review.

### Local AI / Ollama

The optional protocol enhancer is restricted to loopback endpoints such as:

```text
localhost
127.0.0.1
::1
```

`app/local_ai.py` must continue to reject external hostnames for this feature unless an explicit architecture/security decision changes the policy.

## Temporary audio

Current recognition architecture uses temporary WAV chunks.

Expected behavior:

- chunk exists only long enough for processing;
- transcriber removes it after processing in the normal path;
- normal autosave does not archive raw audio;
- crash/diagnostic reports must not embed raw audio.

If a future high-quality final reprocessing mode needs session audio, it must be a clearly labeled opt-in design with retention/deletion rules.

## Transcript storage

Per-session transcript/protocol files are local under the user's application data directory.

Users must treat exported DOCX/TXT/JSON according to their organization policies. The application should not automatically upload them.

## Diagnostic redaction

`app/health.py`, `app/crash.py` and `diagnostics.py` contain redaction logic.

Rules:

- do not include transcript text;
- do not include meeting audio;
- avoid full user-specific filesystem paths where not necessary;
- avoid meeting title in generic diagnostics unless the user intentionally exports a meeting artifact;
- log technical error context, not sensitive meeting payload.

## Crash reports

Crash reports are for application debugging. They should contain exception/stack/environment details sufficient to locate a bug while minimizing personal/corporate content.

Never attach the current transcript buffer to a crash report for convenience.

## Speaker identity

Remote speaker clusters are technical identifiers (`speaker_1`, etc.) until the user maps them to a name. Do not infer or persist biometric identity across meetings by default.

If persistent voice identification is ever added, it is a separate privacy-sensitive feature requiring explicit user control, retention policy and security review.

## Terminology dictionary

Meeting-specific hotwords may include names and internal systems.

Current/future rules:

- keep dictionary local;
- user controls additions;
- do not automatically publish it to repository diagnostics;
- adaptive persistence should require an explicit approved-term mechanism rather than learning every phrase.

## DION integration

Future API/chat integration must follow least privilege:

- obtain only meeting metadata needed for the feature;
- do not assume undocumented raw-audio access;
- do not send protocol/transcript to DION chat automatically without an explicit user action/configured workflow;
- separate API credentials from source code and documentation.

## Repository hygiene

Do not commit:

- real meeting audio;
- unredacted transcript JSON;
- corporate credentials/tokens;
- participant personal data solely as test fixtures;
- diagnostic reports containing identifiable local paths when sanitized fixtures are sufficient.

Use synthetic or sanitized test data.

## New dependency review

If a dependency can access network, audio, microphone, files or model runtime:

1. record purpose;
2. verify its normal network behavior;
3. update `DEVELOPMENT.md`;
4. update this document if the trust boundary changes;
5. verify packaging does not introduce silent downloads at meeting time.

## Security invariants

1. STT is local/offline by default.
2. No automatic external upload of transcript/audio.
3. Ollama protocol enhancer is loopback-only.
4. Diagnostics exclude transcript/raw audio.
5. Speaker biometric persistence is not enabled by default.
6. Secrets never belong in repository documentation.
