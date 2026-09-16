# Published draft PR115 metadata

Title: Add privacy-preserving Combat clocks and Exercise evidence

URL: https://github.com/dills122/sandtable/pull/115

## Summary
Private Combat seal timestamps previously changed whether an opponent’s unchanged proposal succeeded after a clock regression. Keep acceptance tied to the published opening instant, and carry that privacy rule through authenticated side projections, settlement and cycle completion. Add private Exercise checkpoints and child execution evidence through B2.

## What Changed
- Introduce explicit v2 round/settlement clock contracts. Private accepted timestamps remain audit evidence; mandatory windows retain fixed deadlines and deterministic fallback.
- Authenticate side observations, candidates, receipts and native cycle-finish history, preserving historical v1 contracts and all 18 original audience traces.
- Add B1 source-bound occurrence checkpoints across 134 sources and 2,576 cuts, retaining native World, RNG and control state.
- Add B2 manifests, actual accepted transitions, separate reconstruction/re-adjudication proofs and artifact inventories. Bind current oracle/schema build identity; reject unsupported controller configurations and incomplete-reference overruns. Preserve rejected owner inputs that trigger System fallback, with no fabricated player acceptance.

## Validation
- Retained `just check`: format/build clean; 81 boundary tests and 1,670 full tests passed, zero skipped. No runtime source/test changes followed that gate.
- B1: 134 sources, 2,576 cuts, 192 dual schedules, 352 full-World bridge checks, 67 rejection checks and five exact-byte fixture mutants.
- B2 complete native run at the preceding code freeze: 134 sources, 2,442 transitions and 44 owner-triggered System fallbacks. Independent AST/schema checks establish applicability to final native execution logic.
- Final B2 focused driver: 134 admissions, seven native readbacks, 22 failure/clock/copy checks, stale-fixture rejection and regenerated exact readback; exit 0 in 458.829 seconds. Fixture contains 134 source summaries and 12 child records.
- Independent final literal/source audits and ordinary review passed. Earlier full native execution and final focused verification are recorded separately; no full native rerun on final bytes is claimed.
- Reproducible final commands and logs: `.planning/2026-09-14-overnight-combat-wave-01/evidence/004b2-author-focused.py`, `004b2-root-literal-probe.py`, `004b2-final-applicability-check.py`, and `004b2-verification-chronology.md`.

## Scope Notes
These are private, unregistered contract evidence artifacts. Combat runtime activation, hosted transport, durable artifact publication and full fresh-session parity remain deferred. B3 parent comparison, integrated checkpoint B and Task005 remain open; this PR stays draft.

Publication verified through51f0274; this metadata-only follow-up uses the same branch.
