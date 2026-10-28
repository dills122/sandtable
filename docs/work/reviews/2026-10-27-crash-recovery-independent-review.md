# Custody and Retreat crash-recovery independent review

Review instance10 of10, explicitly authorized for combined crash-recovery packet.
Fresh-context read-only reviewer `/root/crash_pair_review10`; blind first pass
completed before author explanation. No actionable findings. Verdict: **Ready**.

Base `72a167f763f436511b341105751a0a4a506ac89d`; reviewed head
`6dce1a7d0276467392a9a0f9c774b0cb05121dc2`; clean tree on
`codex/rc-retreat-crash-recovery`. Commits611a4be2 and6dce1a7d retained.

## Findings and plan review

Plan meets bounded evidence objective: genuine accepted repeated histories, real
child process termination at precommit and postcommit/lost-response boundaries,
then separate recovery/retry processes. Scope remains tests and documentation.
No runtime/schema/contract/pin changes; no actionable defect found.

Runtime ordering independently checked: timeout/closed-round writes precede
successor/settlement/empty Reserve completion; InitializeCycleRound reads clock
before Cycle insert; enclosing Tick commits afterward. Postcommit pause follows
tick return before normal response.

## Author-claim reconciliation

| Claim | Independent evidence | Result |
| --- | --- | --- |
| Genuine second captured lot / required Retreat | Fixture setup, accepted-history counts, actual paid result/lot/RNG assertions | Confirmed |
| Abrupt termination, not exception rollback | Open stdin, handshake, OS kill, process reaping and nonzero exit | Confirmed |
| Atomic recovery and idempotent retry | Committed-table comparisons, fresh children, exactly-one timeout and unchanged retry version/hash | Confirmed |
| Full14-table/replay parity | IntegratedCampaignCompare, reconstructed bytes and contact-record comparison | Confirmed |
| Retreat matches legal refusal | Separate saved branch submits accepted refusal; settled World matches timeout and differs from paid World | Confirmed |
| Selected validation passed | XML, receipts, build log and source-hash ledgers | Confirmed |

Owner-refusal reference is meaningful path parity; both paths share Core semantics.
It does not establish independent source-rule certification.

## Verification performed

Reviewed diff, canonical Custody/Retreat/journal specs, checklist, test child,
fixtures and transaction chain. `git diff --check 72a167f..6dce1a7d` passed.
Graph coverage on12 relied code files matched metadata with no recorded issue;
source cross-checks performed. CCE unavailable; graph/source used.

Parsed retained XML: Custody packet14 passed; final Retreat packet16 passed;
zero failures/skips. All five recorded source hashes matched respective commits.
Final Host Release build had zero warnings/errors; format exit0 recorded.
Reviewer did not rerun duplicate suites.

## Residual risks and verdict

**Ready** for bounded test/evidence change. Required exact-head CI precedes merge.
Two logical process-kill boundaries do not prove arbitrary write/fsync faults,
power loss, corruption, broader profiles or public certification. Full local
Host/Core/solution validation and hosted CI unrun at review time.

Package retained commits and run CI. Review allowance exhausted; no additional
instance authorized by this report.

## CI timeout addendum — same review10

First PR336 CI run40616911744 reached the Orleans20-minute test-step limit without
completed Host XML; aggregate verification correctly failed that dependency.
Same reviewer inspected the bounded workflow-only correction: step20→25minutes,
job25→30minutes, preserving five-minute setup/upload headroom. Commands, exact-head
verification, required artifacts and failure propagation unchanged. No actionable
findings; **Ready**. New head still requires complete green CI before merge.

Local actionlint exit0; WorkflowDeadlineTests2/2 pass; diff whitespace check passed.
No new reviewer instance. Test/runtime implementation remains unchanged from the
original review target; this addendum covers only CI budget and explanatory comment.
