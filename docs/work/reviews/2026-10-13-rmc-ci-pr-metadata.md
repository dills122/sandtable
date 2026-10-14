# PR182 CI recovery metadata

```text
Branch: codex/actual-repeated-movement-completion
Commit Message
fix(ci): give Core-B a bounded twenty-minute budget
```

```text
PR Title
Add Movement completion contract and fix Core-B CI deadline
```

```md
PR Description
Repeated ordinary Movement currently stops before lifecycle completion. Add a private executable contract that completes authentic zero-move histories directly and requires owner stop, trusted System empty resolution, and owner completion for positive tracks. Every path enters ordinal2 Breakdown Determination with a new receipt-bound current MovementEndProof while preserving the original proof, World, resources, NONE history, RNG and actual move progress.

The additive spec, closed schema, Python oracle and retained fixture cover all four owner/seal roots and move cuts0–4. Full five-ledger ARMV replay precedes the sixth lifecycle ledger and public claims. Canonical plans and documentation retain the private boundary and separate native/later-phase gates.

Validation:

- Genuine semantic RED: all20 first publications fail only missingconsumer007 after authentic prerequisites; GREEN and failure receipts retained.
- `python3 -B -u docs/specs/verify-combat-actual-repeated-movement-completion-v1.py`: PASS824.354s/1200, exit0, no timeout;20traces/52events/72cuts/100prior-command retries,224root rejection checks and retained fixture equality.
- All342 physical dependency faults, closed grammar, inherited-clock/context, causal claims and capacity checks pass. Oracle/schema/fixture hashes are identical before/after the full run.
- Combined frozen focused recovery timed out at120.006s without pass credit; split checks passed, and the full run includes all omitted middle cuts.

Private reference only. Native completion, downstream phase execution and public activation remain separate work. Independent review1 returned Ready at c20a84c; the CI deadline delta requires fresh review2 and successful exact-head CI under Brain coordination. Neutral bootstrap and separate author rationale/self-review are in `docs/work/reviews/2026-10-13-rmc-*`; exact test evidence is `2026-10-13-rmc-final-normal.json` and its retained log.

CI deadline recovery:

Two previous Core-B attempts exhausted the existing15-minute job cap during successful execution. Attempt2 completed2290/2329rows without assertions; the investigation measured18.915%matched-row summed slowdown and estimated16–17minutes for the whole job. Change only Core-B to a bounded20-minute cap; other shards stay15, prepare/verify15 and dependency-review5. Row filters, native workload, exhaustive coverage and process/exact-SHA/attempt gates are unchanged.

Focused configuration RED demonstrates Core-B15vs20; GREEN2checks pass, all98coverage-helper tests pass, and actionlint1.7.12 validates the workflow. Frozen Python artifacts and their prior824.354s full-verifier evidence are unchanged. Host slowdown cause remains unmeasured; the new hosted run must establish complete2329Core-B and2879aggregate success. Full report/analyzer/timing analysis and separate neutral CI delta review packet are retained under dated research/reviews.
```
