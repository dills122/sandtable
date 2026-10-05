**Not ready: one P2 error-order defect remains.** The prior arm-order finding is corrected.

Review instance: **S3 set1/pass2, total2of9**, maximum3sets×3. Reviewed branch `codex/combat-actual-selection-contract`, base `96596dde066b0d8c9a0110eba50fcfcb01d99a46`, head `5e2ef795f89335988d424c37d2648acddd992681`.

## Findings

**[P2] Validate active state before the clock for completion commands.**

[Transition line199](../../specs/verify-combat-actual-selection-v1.py) validates the clock for `complete-step` and `close-empty-selection` before their active-state checks at lines210/243, including the required defender-decline check at line246.

The [frozen specification](../../specs/combat-actual-selection-v1.md) requires active-state error `006` before clock error `005`.

Reproduced through `apply`, using fully replayed histories for **both owners**:

- At pending selection14, submit System `complete-step`, correct segment/version/Position, `admittedAt=null`, `clockAvailable=false`.
- Actual: **CMB-ASE-005**. Required: **CMB-ASE-006**, because selection remains pending.
- `close-empty-selection` exhibits the same mismatch outside its `system-no-selection` state.
- Completion at RBA before accepted defender decline also exposes this ordering.

With a valid clock, these commands return `006`, confirming that clock validation masks the state failure. A matrix across all16 traces produced **182 mismatches among276 trace-cut probes**; these include repeated states across variants.

This would freeze inconsistent failure behavior for native parity. The smallest correction is to validate each command’s active-state prerequisites—including required decline—before its clock gate, while keeping positive FA error `007` after the clock. Add combined state/clock regressions for both owners. Successful literals need no regeneration.

No other actionable findings established.

## Plan Review

The five primary paths and three administrative packets match the authorized manifest. All five retained hashes match. Scope preserves:

- Both complete original entry histories and twelve entry receipts.
- Seven positive events ending at defender decline19 and FA20, without completion.
- Seven fallback variants per owner reaching Reserve Release.
- Complete source and independent-ledger replay, new framing, owned projections, bounded caches, and all16 pins before cache access.
- Historical family incompatibility and separation from blocked Breakdown fixture, Snapshot and outward admission.

The coordinator disposition remains binding. Native/public activation, round/result/repeat and parent closure remain open. The finding requires a bounded correction; **no heavy pivot is needed**.

## Author-Claim Reconciliation

| Claim | Assessment |
|---|---|
| Prior arm-order correction | Confirmed for both owners |
| Added328 precedence probes | Confirmed; tests detect the pre-fix ordering mutation |
| Successful spec/schema/fixture bytes unchanged | Confirmed against02bb6fc |
| Positive endpoint, fallbacks, cuts and retries | Confirmed by direct oracle execution |
| Independent trusted ledger | Confirmed; tests detect event-derived ledger substitution |
| Complete error-order acceptance | **Contradicted** by active-state/clock cases |
| Dependency checks, cache ownership and cold separation | Confirmed by source inspection and executable acceptance |
| Retained semantic RED | Hash verified; expected16 failures reproduced. Historical chronology remains author evidence |

The preliminary ledger was recorded before opening the author packet or previous report. Author conversation history was not inherited.

## Verification Performed

Fresh checks from the exact worktree:

- `python3 -B docs/specs/verify-combat-actual-selection-v1.py` — **exit0**, reproducing all retained counts:16 literal traces,152cuts,1,962retries,4,456event mutations,8,266proof mutations,128pin rejects,2cold-separation checks and328new precedence probes.
- Independent state/clock probes — reproduced the finding above.
- In-memory sensitivity checks — arm regression detects pre-fix mutation; trust regression detects event-derived ledger mutation.
- Retained supplemental script — **PASS**, four old-family and four re-signed entry rejects.
- Retained RED — verified source hash and reproduced expected16 failures.
- Base-byte comparison — **898 existing spec/runtime/test paths unchanged**.
- `git diff --check 96596dde066b0d8c9a0110eba50fcfcb01d99a46 HEAD` — **exit0**.
- `git diff --exit-code HEAD` — **exit0**.

**Historical predecessor verification explicitly reused**, supported by byte equivalence; not reported as fresh:

| Original oracle | Retained outcome |
|---|---|
| positive-entry, selection-steps, sealed-round, result-settlement | Each exit0 |
| Breakdown, cycle-sequence, Snapshot, outward | Each exit1 at its separate retained pin gate |

Historical S1a timeouts remain unverified. No .NET/full-suite/Boundary/format or hosted CI check was executed.

## Open Questions And Residual Risks

Serena was activated for the exact checkout. Codebase Memory coverage confirms `docs` is excluded; focused source reads supplied evidence.

Production seat/clock/store authentication, native byte parity and outward privacy remain separate obligations. The current passing oracle misses the reported combined state/clock failures.

## Verdict

**Not ready** to freeze the executable contract: required error ordering still disagrees with implementation.

Final HEAD and branch remained exact; only `.serena/` was untracked. No fixes, commits, publication, reviewer dispatch or research-recovery spike occurred.

## Recommended Next Actions

Coordinator should reconcile the P2 finding, require the bounded correction and regression vectors, then decide on set1/pass3,total3of9. Preserve review counts and scope. Exact-head CI/merge must precede S4.
