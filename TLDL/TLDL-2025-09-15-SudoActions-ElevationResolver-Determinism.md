# TLDL — 2025-09-15 — SudoActions, Elevation Resolver, and Determinism Hardening

## What we changed
- Implemented a real consumer for `SudoActionRequest` that processes requests and cleans them up (idempotent by design).
- Added a concise README (`Assets/MetVanDAMN/Authoring/README.SudoActions.md`) documenting authoring → dispatch → consume flow and determinism rules.
- Quieted markdownlint MD026 warnings globally to avoid content churn in long-lived docs.

## Why it matters
- Designers can now drop a `SudoActionHintAuthoring` and reliably trigger one-off runtime behavior keyed by `ActionKey` with reproducible placement.
- The pipeline from authoring to runtime action is deterministic: FNV-1a hashing and folded constraints remove platform variance.
- Elevation defaults are applied once via `BiomeElevationResolverSystem` before art selection so visuals match intended elevation layers.

## Technical notes
- Dispatcher: seeds requests as `worldSeed ^ FNV1a(ActionKey) ^ elevation/type folds` unless an explicit seed is provided; zero-radius uses `Center` directly.
- Consumer: sample system tears down `SudoActionRequest` entities after handling. Real gameplay systems should mirror that pattern.
- Docs: `MD026` disabled in `.markdownlint.json` to stop trailing punctuation lints in headings across upstream docs.

## Follow-ups
- Add PlayMode tests for dispatcher one-off and determinism; resolver OR-semantics and non-overwrite.
- Replace sample consumer with a real feature-specific system (e.g., spawner keyed to `spawn_boss`).
- Create a short demo clip in the baseline scene showcasing hint → request → action.
