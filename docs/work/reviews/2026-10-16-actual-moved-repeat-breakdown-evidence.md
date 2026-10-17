# R03 reference checkpoint evidence

**Status:** contract remains open. Native consumption is gated. This checkpoint does not certify
all 300 terminals or the complete negative matrix.

## Reproducible checks

- `python3 docs/work/reviews/2026-10-16-actual-moved-repeat-breakdown-capacity.py` passed.
  Structural encoding allocations: Source 257,846,784 < 268,435,456 bytes; parent Source
  128,857,600 < 134,217,728; Control 790,528 < 2,097,152; Proof 1,589,248 < 4,194,304;
  Event 19,728 < 65,536. These are profile arithmetic, not arbitrary malformed-carrier bounds.
- `python3 docs/work/reviews/2026-10-16-actual-moved-repeat-breakdown-physical.py` passed:
  106 pinned source paths plus two own files, five modes each, 540 rejected faults. Modes are
  changed, missing, unreadable, null and drift between independent acquisitions. The null check
  revealed and fixed an unclassified `TypeError`; it now maps to `CMB-MRB-009`.
- `python3 docs/work/reviews/2026-10-16-actual-moved-repeat-breakdown-cuts.py` passed:
  one authentic paid and three authentic armed nonterminal R02 lifecycle cuts rejected before
  R03 admission. The complete 300-row nonterminal partition remains a separate gate.
- `python3 -u docs/specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py --rows 0 --progress --evidence-gzip /tmp/r03-row0-vectors.jsonl.gz` is the single-row/full-vector form;
  row `r000-c01` passed with full canonical parent ledgers, input, event, source, controls and
  proofs retained. The observed single-row run took 7.92 seconds before the gzip option was added.

## Long-running catalogue

The 300-row run started in a detached `tmux` session named `r03-full` on October 16, 2026
(America/Toronto). It writes per-row completion to `/tmp/r03-full-progress.log` and full canonical
vectors to `/tmp/r03-full-vectors.jsonl.gz`. The files are local work products until the run
finishes, vectors are checked for exactly 300 unique expected row keys, and an evidence digest is
retained. The verifier prints `PASS 300 authentic R03 traces` only after all rows complete.

The running verifier SHA-256 is
`56585cc8c9e1a764f7d6352a3c88f57121b92534ffe409bc97b8f88b25e89218`;
the R03 schema SHA-256 is
`ba2047bbe6ab7b9538052aa95f11edb5c347d17853f761fc2704ccd1f859ca30`.
Changing either while this run is active invalidates its source check. Query status with
`tmux list-sessions` and `tail -5 /tmp/r03-full-progress.log`.

## Open gates

Complete the 300-row catalogue, independently check retained vectors and all prior-cut/retry
partitions, run further re-signed event/readback and raw-framing negatives, independently review
the contract and capacity argument, then pass exact-head CI. Until those gates are recorded,
`closureFrozen=false` is intentional and no native R03 consumer should be written.
