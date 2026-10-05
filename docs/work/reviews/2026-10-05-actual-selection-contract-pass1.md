**Not ready: one P2 contract-ordering defect remains.**

Review instance: **S3 set1/pass1, total1of9**. Reviewed branch `codex/combat-actual-selection-contract`, base `96596dde066b0d8c9a0110eba50fcfcb01d99a46`, head `02bb6fc1100370f1757a75da45042d1f69b978c8`.

## Findings

**[P2] Validate forbidden command arms before segment identity.**

[Transition line174](/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/docs/specs/verify-combat-actual-selection-v1.py:174) checks segment identity before allowed-arm validation. The [specification](/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/docs/specs/combat-actual-selection-v1.md:135) explicitly requires arm error `003` before segment error `004`.

Reproduced for both owners: take the valid opening input, set `segmentId="foreign"` and forbidden `choice="finish-without-attack"`, then call `apply(source, [], input)`. Actual result: **CMB-ASE-004**. Required result: **CMB-ASE-003**.

This freezes inconsistent behavior for the future native adapter. Move the segment check after arm validation and add combined-error regression vectors for both owners. Successful literal traces need no regeneration. The current 48 ordering probes miss this case.

No other actionable findings established.

## Plan Review

The five primary paths and three administrative packets match the authorized scope. All five retained manifest hashes match; only `.serena/` is untracked. HEAD remained unchanged.

The implementation preserves both original sources, full provenance and twelve entry receipts. Positive selection reaches defender decline19 and stops at FA20; all seven fallback variants per owner reach Reserve Release. Original-source admission, separate trusted-ledger comparison, immutable caches and all16 dependency checks precede authority/cache access.

Historical readers remain unchanged. Cold separation checks admit both sources without blocked Breakdown fixture admission, Snapshot or outward readback. Native/public activation and downstream round/result/repeat gates remain open.

The finding needs a bounded correction within the existing scope. **No heavy pivot is required.**

## Author-Claim Reconciliation

| Claim | Assessment |
|---|---|
| Exact scope, manifest and16 frozen dependencies | Confirmed independently |
| Positive endpoint, fallbacks, cuts, retries and literal bytes | Confirmed by direct oracle execution |
| Independent-ledger consistency and authentication limits | Confirmed; coherent replacement remains an explicit caller-trust limitation |
| Complete error-order acceptance | **Contradicted** by the combined arm/segment case |
| Pin-before-cache and historical separation | Confirmed by executed acceptance and source inspection |
| Retained semantic RED | Hash verified and expected16 failures reproduced; original chronology remains author evidence |

The preliminary ledger was recorded in commentary before opening the author packet. No author conversation history was inherited.

## Verification Performed

From the exact worktree:

- `python3 -B docs/specs/verify-combat-actual-selection-v1.py` — **exit0**, reproducing all reported counts. Output SHA256 matches `0a015bd67749b35da65e2d15a300c756f4eeaafc048c966c715d7249cbc129f6`.
- Original positive-entry, selection-steps, sealed-round and result-settlement commands — **exit0**.
- Original Breakdown, cycle-sequence, Snapshot and outward commands — **exit1**, separately, at retained pin gates.
- Retained supplement — **passed** four historical-family rejects and four re-signed entry rejects.
- In-memory sensitivity probes — existing tests detected removal of the FA stop and substitution of event-derived inputs for the independent ledger.
- `git diff --check` — **exit0**.

[New oracle log](/private/tmp/s3-review-pass1-oracle.log) · [Predecessor results](/private/tmp/s3-review-pass1-originals/results.json)

The initial FA sensitivity mutation hit a harness bounds error; the corrected temporary mutation produced the intended unexpected-acceptance failure. Repository files were unchanged.

## Open Questions And Residual Risks

Serena was activated for the exact checkout, but its C# server cannot inspect Python symbols. Codebase Memory excludes `docs`; focused source reads supplied coverage.

No .NET/full/Boundary/format gate or hosted CI was executed. Historical failures and timeouts remain unchanged. Production seat, clock and retained-store authentication, native byte parity and outward privacy remain separate obligations.

## Verdict

**Not ready** for freezing the current executable contract because its required error ordering disagrees with implementation.

## Recommended Next Actions

Coordinator should reconcile the P2 finding, require the bounded correction and regression vectors, then dispatch the next counted independent pass. Exact-head CI/merge must still precede S4. No fixes, commits, publication, additional reviewers or workstreams were initiated.
