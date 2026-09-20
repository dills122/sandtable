# Combat delivery execution

Base: `e64bed9` (merged planning PR #123). Branch: `codex/combat-task008-creation-binding`.
Integration destination: reviewed feature branch; lead owns retained commits and PR.

## Objective and boundaries

Advance Sprint 5 capability gates in canonical dependency order from
[Task008 A1c](../../docs/design/combat-cycle-implementation-plan.md#task008-execution-index).
Canonical plan and specs own requirements; this file tracks execution only.
No public activation or parent completion inferred from a codec slice.

## Execution queue

| Item | Dependencies | Owner / unit | Evidence | Status |
| --- | --- | --- | --- | --- |
| 008A1c | merged A1b | implementation internal subagent; lead integrates | frozen request/Created11 bytes, trusted inputs, retry/conflict/raw-byte negatives; focused52, boundary81, final full1840 pass | Accepted; feature-branch checkpoint |
| A2 publication boundary | existing contract and HOST-RSH-001 | read-only research subagent; lead decides | explicit seam and evidence limits, no provider selection | Research in progress |
| 008A1c dev review | implementation | lead | code-review-and-quality axes, spec/test reconciliation | Complete; no remaining findings |
| 008A1c independent rounds 1–3 | dev review, fixes serially | fresh-context reviewers | separate bootstrap/author/report per round | Round1 Ready; round2 P2 accepted/fixed with RED/GREEN; round3 Ready; accepted |
| 008A2 | A1c accepted | unassigned | canonical plan creation readback and publication boundary | Pending |
| 008B–H, 019A | canonical dependency graph | unassigned | cumulative Core replay, full retained-history restore | Pending |
| 009–025 | canonical gates | unassigned | cumulative gameplay/public/Runner/72-AC evidence | Pending |

## Review and stop policy

User requests three sequential independent review rounds after dev review before next task.
Each reviewer starts without implementation conversation, inspects neutral bootstrap before author
rationale, and returns evidence-backed findings. Lead addresses findings between rounds.
If blockers remain after round 3, run one bounded deep research/experiment against blockers,
then one final independent review (round 4). Proceed only if blockers cleared; otherwise stop
for user intervention. Do not restart review counts or silently expand scope.

## Verification

Use repository .NET 10 native MTP commands with `--project` / `--solution`, focused xUnit
filters, unique binlogs, frozen Python oracle and full repository format/build/boundary/tests gate.
Record exact commands, failures and totals in per-slice evidence; never count unrun checks.

## Decisions

- Existing frozen C2 contract supplies A1c bytes. No new storage-provider or six-turn inventory
  spike before A1c; publication evidence boundary reviewed separately for A2.
- Initial checkout clean, already includes merged #123; feature branch created before edits.
- CCE current-task search returned unrelated/stale matches; canonical local files used as fallback.
