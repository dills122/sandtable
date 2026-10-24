# Force deadline independent review

Review instance: 2 of 3 across RC execution flow.

## Preliminary independent ledger

Recorded before reading author evidence. Review target: proposed
`artifacts/rc-overnight/force-deadline-validation.patch` against dirty worktree on
`codex/rc-overnight-plan-20261023`, HEAD
`16d76448a008ea4f16299402b8ef44e8c96a4843`. Source patch not yet applied.

- No actionable source defect found in bounded patch. Moving gate after retained
  opening, current owner draft, action mapping and HMAC checks prevents invalid
  proposals from committing cancellation. Existing receipt lookup still precedes
  round binding and clock handling.
- Existing immediate transaction still encloses validation, clock gate and
  cancellation publication. `GetOrOpenForceRound(requireExisting: true)` reads
  existing round without opening or touching clock. Ordinary rejection disposes
  transaction without commit.
- New four-case theory checks entire retained campaign/step/round/opening/action/
  seat rows before and after rejection, cold replay, receipt retry before and
  after cancellation, and genuine pending-owner deadline fallback.
- Scope does not implement v2 fixed-opening floor or separate System expiry
  semantics. Existing high-water and legitimate-submit cancellation remain;
  partial v2 conformance must not be represented as full adoption.
- Executable verification pending author scratch tests and final integrated gate.
  No duplicate tests launched. `git apply --check` passes against inspected tree.
- Optional coverage extension: invalid proposals during unavailable/regressing
  clocks and after closed rounds; source ordering covers same gate but added
  theory directly exercises deadline equality only.

## Findings

No actionable finding in reviewed patch. Review limited to patch hash
`0a4b955ee5f8324a11e1c68ca180597a935dc3b46a5f0b840bcc03526801990e`.
Earlier dirty RC packet remains outside this review.

## Plan Review

Bounded fix fits deterministic rejection and persisted-authority invariants in
sealed-round v1, v2 proposal-validation ordering, and durable journal contract.
Existing legitimate-submit System cancellation remains as explicitly scoped;
full v2 timing adoption, RBA timeout, scheduled ticking and P01 certification
remain excluded. RC plan requires integrated validation and exact-head CI before
merge; patch review cannot close those gates. No architecture pivot needed.

## Author-Claim Reconciliation

Author evidence read only after preliminary ledger was written.

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Invalid syntactically valid proposal could cancel before offer validation | Original `SubmitOwnerActionCore` lines 135–144 precede retained opening and HMAC validation at lines 152–288 | Confirmed | Proposed relocation fixes identified path |
| Accepted retries retain original receipt before clock gates | Retry lookup at lines 96–115 precedes Force binding; patch leaves it unchanged | Confirmed | Deadline and closed-round recovery preserved |
| Required existing-round lookup remains non-mutating | `GetOrOpenForceRound` lines 16–68 with `requireExisting: true` | Confirmed | Reject paths cannot open new rounds |
| Four invalid cases preserve retained authority and legitimate cancellation | Proposed `InvalidForceProposalAtDeadlineCannotCancelCurrentRound` theory | Confirmed by source and 4/4 scratch pass | Regression detects original defect and passes patched source |
| Closed-round behavior and current timing policy unchanged | Required binding/read ordering remains; `GateForceRound` lines 287–300 unchanged | Confirmed | No claim of complete v2 adoption |
| Apply check passes; build/test not yet performed | Independent apply check exit 0; author packet explicitly pending | Confirmed / pending | Final executable readiness unverified |

## Verification Performed

- `git branch --show-current`: expected branch.
- `git rev-parse HEAD`: expected base commit.
- `git status --short`: dirty earlier RC packet acknowledged, including tests;
  production submissions file unchanged before patch application.
- `git apply --check artifacts/rc-overnight/force-deadline-validation.patch`:
  exit 0, no output.
- Direct patch, implementation, existing test and contract inspection. Graph
  search, direct Force-gate call trace and helper snippets used for relationships.
  Coverage checks for relied-on source/test/spec paths returned no recorded
  issue and metadata match, generation `2026-10-24T18:29:47Z`; best-effort signal,
  not proof of exhaustive indexing. CCE tools unavailable in supplied tool set.
- Author scratch command: `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj --filter-method '*InvalidForceProposalAtDeadlineCannotCancelCurrentRound*'`.
  Independently inspected `force-validation-red.log`: original implementation
  fails 4/4 unchanged-authority checks, head sequence 16→20, exit 2,
  duration 12.512s. Independently inspected `force-validation-green.log`:
  patched implementation passes 4/4, duration 13.065s; worker confirms exit 0.
  Both logs retained under `artifacts/rc-overnight/`.
- No build or tests launched by reviewer: integrated gate owns main outputs;
  scratch validation belongs to author worker. No production/test edits,
  staging or commit.

## Open Questions And Residual Risks

New regression RED/GREEN and Force compatibility verified; final integrated test outcomes pending. New theory covers exact deadline;
clock-unavailable/regression and closed-round malformed proposals remain optional
additional regression vectors. Existing source gate ordering covers those invalid
paths, while existing tests cover trusted unavailable/regression ticking. No new
scheduler, public-host readiness, or certificate completeness inferred.

## Verdict

**Ready with non-blocking follow-ups** for bounded patch: no material defect
found, regression fails before fix and passes after, existing Force tests pass.
Optional clock-fault invalid-proposal vectors remain follow-ups. Final-tree
integrated validation remains required before RC/merge readiness; this verdict
approves patch integration, not overall RC acceptance.

## Recommended Next Actions

Apply exact reviewed patch after main output lease permits, then run integrated
checks on final tree under coordinator's lease. Record
actual results before closing RC gate. Further reviewer instance needed only
for material behavior changes, subject to remaining one-instance budget.

### Completed Compatibility Evidence

Author scratch command: `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj --no-build --filter-method '*ForceRound*'`.
Independently inspected `artifacts/rc-overnight/force-validation-compatibility.log`:
8/8 passed, 0 failed/skipped, duration 21.904s; worker confirms exit 0.
Scratch working directory: `artifacts/rc-overnight/force-validation-scratch`.
No active scratch process remains per worker. All main-tree integration checks
remain coordinator-owned. Initial preliminary pending statements above preserve
chronology; completed evidence here supersedes scratch-pending status.
