# R03 reference checkpoint evidence

**Status:** contract remains open (`closureFrozen=false`). Native consumption is gated. Full
corrected catalogue, complete prior-cut partition, vector audit, review and exact-head CI remain
open.

## Superseded catalogue

The original R03 verifier completed `PASS 300 authentic R03 traces`, with full-vector gzip
SHA-256 `b8f09c197d937294de0242d8f3dbb8d55e39d7860bcec58839b1f8cd3e7e2976`. Independent
review then found that R03 omitted the new receipt from inherited cumulative `receipts`, and
that a live pin map could diverge from the captured manifest. The original run is **not freeze
evidence**. The vector audit failed its first cumulative receipt check; pin-map deletion was
accepted in the RED test.

## Reproducible checks

- `python3 docs/work/reviews/2026-10-16-actual-moved-repeat-breakdown-capacity.py` passed.
  Structural encoding allocations: Source 257,846,784 < 268,435,456 bytes; parent Source
  128,857,600 < 134,217,728; Control 790,528 < 2,097,152; Proof 1,589,248 < 4,194,304;
  Event 19,728 < 65,536. These are profile arithmetic, not arbitrary malformed-carrier bounds.
- `python3 docs/work/reviews/2026-10-16-actual-moved-repeat-breakdown-physical.py` passed:
  108 pinned source paths plus two own files, five modes each, 550 rejected faults. Modes are
  changed, missing, unreadable, null and drift between independent acquisitions. The null check
  revealed and fixed an unclassified `TypeError`; it now maps to `CMB-MRB-009`.
- `python3 docs/work/reviews/2026-10-16-actual-moved-repeat-breakdown-cuts.py` passed:
  one authentic paid and three authentic armed nonterminal R02 lifecycle cuts rejected before
  R03 admission. The complete 300-row nonterminal partition remains a separate gate.
- `python3 docs/work/reviews/2026-10-16-actual-moved-repeat-breakdown-manifest.py` passed:
  pin-map deletion and valid-file substitution both reject `CMB-MRB-009`. The 56-root moved-repeat
  fixture and 300-case R02 completion catalogue are now physical pins.
- `python3 -u docs/specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py --rows 0 --progress --evidence-gzip /tmp/r03-corrected-row0.jsonl.gz` passed for authentic row `r000-c01` with
  full canonical parent ledgers, input, event, source, controls and proofs retained.
- `python3 docs/work/reviews/2026-10-16-actual-moved-repeat-breakdown-negatives.py /tmp/r03-corrected-row0.jsonl.gz` passed: 15 framing, ledger, actor/action/context, signed-event, readback
  and clock-ceiling rejections; exact retry preserves both receipt ledgers.

## Corrected catalogue in progress

The corrected 300-row run started in detached `tmux` session `r03-corrected-full` on October 16,
2026 (America/Toronto). It writes progress to `/tmp/r03-corrected-full-progress.log` and complete
canonical vectors to `/tmp/r03-corrected-full-vectors.jsonl.gz`. After its final `PASS 300 authentic
R03 traces`, run `2026-10-16-actual-moved-repeat-breakdown-vector-audit.py` on the gzip file. The
audit checks 56 distinct pinned roots, 300 ordered unique rows, paid/armed partitions, both fixture
identities, hashes, receipts and proof readbacks, and prints transcript and file digests.

The corrected verifier SHA-256 is
`33028180e8ef852c7e710a8e2dd5f399d4fd78b04d617b5d15b5ebedccf1ee12`;
schema SHA-256 is
`6bb331f2768fec44d016f7941947cf1597fa42c7f886b0b5c2cc7ffa6b313be4`.
Changing either while this run is active invalidates its source check. Query status with
`tmux list-sessions` and `tail -5 /tmp/r03-corrected-full-progress.log`.

## Open gates

After corrected catalogue PASS and vector audit, run
`2026-10-16-actual-moved-repeat-breakdown-all-cuts.py` on the completed vectors for all 788 R02
nonterminal cuts, update representative fixture hashes, finish raw-framing partitions, obtain
fresh independent review, and pass exact-head hosted CI. Until those gates are recorded,
`closureFrozen=false` is intentional and no native R03 consumer should be written.
