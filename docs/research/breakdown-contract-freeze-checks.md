# BRK-TASK-001 contract-freeze evidence

**Date:** 2026-09-05. **Branch:** `codex/breakdown-adjudication-design`.
**Acceptance checkpoint:** `05e9e42` records owner acceptance of DEC-004–007 after design review 3.
**Scope:** Documentation, frozen migration inventory and specification audit only. No production
code, existing scenario bytes or active runtime identity changed.

The [governing specification](../specs/breakdown-adjudication-v1.md),
[wire contract](../specs/breakdown-wire-contract-v1.md) and
[migration inventory](../specs/breakdown-fixture-migration.v1.json) complete Task 001's outputs.

## Executed checks

| Check | Result |
| --- | --- |
| `python3 docs/research/verify-breakdown-outcomes.py` | PASS: 324 coordinate/band cells, complete unique ranges, monotone columns, nine source probes, 606 bounded loss combinations; unchanged research hash `f63dc336364648acb52d20070bb1076205a36a40769f4391531ba65aaf94c401` |
| `python3 docs/research/verify-breakdown-contract-freeze.py` | PASS: 14 historical scenario hashes, 15 Reaction child dispositions (13 successor obligations / two deferred), 15 pinned predecessor schema hashes, 10 requirements / 12 acceptance mappings, 21 local links |
| In-memory copies of migration inventory with missing Reaction child, wrong predecessor hash, positive ZOC relabeled successor, or original fixture path reused | All four rejected with specific audit failures; original inventory/files restored unchanged (mutants existed only in temporary files) |
| Local-file link check across changed architecture/status/spec Markdown | PASS: 260 targets exist; URL validity and anchors outside this check |
| `git diff --check` and staged diff check | PASS; user-owned changes excluded from staging |

The numerical checker retains its historical wording “proposed-loss combinations”; those arithmetic
choices are now accepted. Its input artifact is intentionally unchanged, preserving source-review
hashes. Production must normalize its own authority artifact rather than loading this research file.

## Self-check conclusions and limits

The predecessor ordinary move factory is combat-only. Frozen successor adds admitted standalone
Truck movement without classifying Trucks as combat units or expanding ZOR's combat-only trigger.
CP/rationals remain inherited; BP evidence retains **all** crossed directional hexside modifiers.
Finite flow explicitly retains both deferred phasing stop and active reactor stop through System
closure. Profile certification includes nominal formations, distinct represented leaves and own
co-located combat stacks. Positive ZOC needs stacking >1 and therefore has no public successor in
this profile; historical/direct evidence remains mandatory. Low-defense public successor evidence
cannot claim an isolated defense threshold when size independently prevents ZOC.

Strict predecessor codec hashes make inherited field/type/order references immutable. New public
creation failure extends the typed rejection enum without renumbering; authority stop position uses
existing no-active-side representation and System action audience. No RNG algorithm, intelligence
contract, generic Runner envelope or existing scenario is changed by this freeze.

These are specification consistency checks, not an independent review or executed production
acceptance evidence. Research/design reviews 1–3 retain their original targets; their exhausted
budget was not restarted. No .NET build/tests were run for this docs-only change. Tasks 002–007 own
all Core tests, strict replay/privacy checks, Runner migration and activation gates.

**Next:** BRK-TASK-002 — dormant outcome rules and exact loss arithmetic. Public runtime continues
to stop at Breakdown Determination until the coordinated successor activation in Task 006.
