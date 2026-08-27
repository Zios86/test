---
name: release-process
description: Build, validate, and publish a DION Meeting Assistant Windows portable release safely. Use when changing the PyInstaller build, bundled models, release workflow, version, GitHub Release, or release artifact metadata.
---

# Release process

## Goal

Produce a reproducible Windows release that is tested before publication and whose documentation matches the uploaded EXE.

## Before changing the build

1. Read `../../../docs/DEVELOPMENT.md`.
2. Read `../../../docs/RELEASES.md`.
3. Read `../../../docs/PROJECT_MAP.md` for the physical build-branch layout.

## Current build path

The release branch workflow is:

```text
dion-portable parts
 -> reconstruct source
 -> apply dion-hotfix/apply_051.py
 -> apply dion-quality/apply_060.py
 -> install dependencies
 -> bundle offline models
 -> PyInstaller onefile
 -> packaged self-test
 -> GitHub Release
```

## Required validation

- Run the reconstructed-source tests when the source/patch logic changes.
- Preserve the shared PortAudio safety checks.
- Validate the expected Quality recognition settings when changing 0.6+ builds.
- Run packaged `--portable-selftest` on Windows.
- Do not publish the release if self-test fails.

## Artifact rules

After build:

1. calculate SHA-256 from the **actual uploaded EXE**;
2. record the artifact name, size and SHA in `../../../docs/RELEASES.md`;
3. update `../../../CHANGELOG.md` for user-visible changes;
4. update `../../../docs/ROADMAP.md` and active execution plan;
5. distinguish CI validation from real DION field validation.

## Model rule

Do not silently substitute Whisper `base` for `small` in a release named Quality. If a lighter model is required, name/document it as a separate profile or release decision.

## Security rule

Do not embed credentials or real meeting data in workflow artifacts. Bundled models and application runtime must not require a cloud STT service at meeting time.

## Final checks

- GitHub Release exists and asset state is uploaded.
- Release notes and `docs/RELEASES.md` agree on SHA.
- Documentation maintenance policy is satisfied.
