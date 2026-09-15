# Blocked draft metadata

```text
Branch: codex/overnight-combat-wave-01
Commit Message
docs(combat): retain blocked side-contract candidate and clock privacy evidence
```

```text
PR Title
Record blocked Combat side-contract candidate and clock privacy conflict
```

```md
PR Description
## Summary

Preserve unaccepted CMB-TASK-004A1 candidate and independently reproduced conflict between accepted clock-regression fallback and equal-outcome privacy. This draft is blocked; no Combat task in this wave or checkpoint B is complete.

## What Changed

- Add bounded synthetic selection/RBA/assignment side contract, ordered schema, canonical fixtures and oracle.
- Retain failing both-seal-order privacy diagnostic and ordinary fresh-context review/source evidence.
- Record overnight execution index, handoff and accurate blocked plan/roadmap/README status.

## Validation

- `just check`: baseline and candidate both pass, 81 boundary tests and 1,670 full tests, zero skipped/warnings/errors.
- `python3 -B docs/specs/verify-combat-side-projection-v1.py`: limited candidate vectors pass, 18 audience traces, 146 cuts, 63 submissions, 297 mutations, 180 raw rejections, 21 receipt/stale checks.
- Selection, sealed-round, cycle-sequence and baseline authority-composition predecessor oracles pass.
- `python3 -B .planning/2026-09-14-overnight-combat-wave-01/evidence/clock-high-water-counterexample.py`: FAILS as retained acceptance evidence, both seal orders.
- AST/JSON/diff checks and local target/new-anchor checks pass; pinned Lychee unavailable.

## Scope Notes

Same waiting-side bytes and proposal at trusted time3500 accept with private high-water3000, but reject/cancel after hidden opposing seal raises it to4000. POL004 requires regression fallback; POL006/PRO-AC010 require equal outcomes. No source exception was found. Owner must reconcile policies and authorize explicit compatible/versioned contract treatment.

No A1 acceptance;004A/004/checkpoint B open. Task005 not begun;006 deferred. Historical authority bytes, runtime registration and protobuf unchanged. Ordinary quality review only; exhausted formal independent-review budget preserved. Do not merge.
```
