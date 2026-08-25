# Reserve Designation v1 Final Implementation Review

**Verdict:** Ready

**Date:** 2026-08-25

**Target:** `codex/stage-entry-exercise-profile` at
`ca3e3385e26551407b09ff17c0629ed7b7a7eaa3`, including the complete 103-entry intentional dirty
working-tree implementation target.

**Independent review task:** `01a03990-dfe6-7bd1-a041-148d87cc45df`

## Protocol

The reviewer received a neutral bootstrap and completed a blind, read-only preliminary pass before
receiving the separate Author Explanation. Findings were frozen with stable identifiers before any
author correction. The same reviewer then verified both corrected targets read-only and issued the
final verdict. No reviewer modified the author workspace.

## Findings and dispositions

| Finding | Severity | Final disposition |
| --- | --- | --- |
| `RDV1-001` | P1 | Closed. Observation v4/legal-action-set v2 use audience-visible revisions, and zero/one/all valid histories prove opponent bytes do not reveal the hidden Reserve count. |
| `RDV1-002` | P2 | Closed. Empty nested worlds reject through the public `InvalidDataException` boundary. |
| `RDV1-003` | P2 | Closed. Durable task state, review remediation, and current documentation are synchronized. |
| `RDV1-004` | P1 | Closed. Snapshot facts now pass through Core's complete strict snapshot/world decoder; a rehashed failed-reconstruction bundle with internally consistent hashes and incomplete World v2 data rejects. |

The Author Explanation accepted every finding. The correction loops preserved authoritative
snapshot/event versions, replay arithmetic, legal-action membership, exact Reserve transitions,
and the intentional Movement stop.

## Final verified evidence

- focused Core strict decoder: 1/1;
- focused failed-reconstruction public reader: 1/1;
- focused fog invariance: 2/2;
- `Cna.Core.Tests`: 411/411;
- `Cna.ExerciseRunner.Tests`: 271/271;
- full solution: 683/683, 0 skipped;
- build: 0 warnings and 0 errors;
- format verification, `git diff --check`, and `just check`: passed; and
- complete 46-item security review: no candidates or coverage gaps.

The review's frozen security digest was
`cf328e955a9f455997f9c008f20ef5583d7580f7bdca69094a5daa33ff0bee6c`.

## Residual boundaries

- Movement is a validated terminal checkpoint, not an implemented movement rules loop.
- Context-authoritative membership is not inferred from arbitrary snapshot bytes; the new decoder
  returns only scalar checkpoint coordinates and cannot create an authority handle or session.
- Model controllers, side-safe exported evidence, paired comparison, victory, and full-game balance
  evaluation remain later packages.

The implementation is ready to proceed to simulator/back-tester evaluation of determinism,
correctness, evidence stability, and local throughput at the delivered Movement boundary.
