# Task016 independent review

Review instance: 3 of 3.

## Findings

No actionable findings. Reviewed immutable candidate `d4805567716b7acb4d99f5c4bd8b3465df976450` against `d600966` in clean detached clone `/tmp/sandtable-task016-review3-1859`. No implementation history, CCE, memory or prior reports used. Preliminary ledger persisted before reading author explanation. No source edits, commits or subagents.

Evidence supporting conclusion:

- `CampaignCombatLossRetreat.Relationships` derives adjacency from final original element locations and authenticated edges. `RequiredRetreat == 0` gates Engaged even when retreat was refused. Full original UnitKeys populate relation endpoints; guard identity cannot substitute.
- `CampaignCombatSettlementState.WithResultV2Relationships` requires disposition, losses, retreat and custody exactly when captured allocation is positive. Structural transition cannot bypass an open window. `Project` reconstructs custody and relationships, compares complete retained settlement, and preserves future duties.
- `CampaignCombatResolution.Transition` emits ordered immediate proof, then one CA completion tied to actual round closure and final committed step. Exact retries and stale callbacks remain available before Closed guard; fresh commands reject.
- `CampaignCombatResolutionCodec.WorldValue` compares complete projected World even after relationships. `ValidClosureEvidence` reconstructs terminal events, validates actor/version/command/event/receipt hashes and prefix linkage. `ReadState` independently replays all supplied causal history before comparing full bytes.
- `CombatClosureTests` exercised all32 traces,272 exact event literals,304 state hash cuts and32 full final states. Negative tests cover forged relationship fields, missing/reordered/duplicate/foreign proof, wrong topology, borrowed terminal receipts, changed typed resources, closure origins, raw grammar and byte ownership. Earlier loss/retreat/custody golden frontier assertions remain intact.

## Plan Review

Task016 refinement explicitly permits five material paths plus three dependent maintenance paths; actual diff matches eight source/test paths. Remaining ten paths are design/status/evidence. Contract, schema, fixture, shared World7, topology, project registration and public transport unchanged. Tech design and naming describe dormant completion consistently. README project map remains accurate: no project or public setup changed.

Task016 success criteria satisfied for admitted Result2 profile: original relationships; raw-retreat precedence; no pending immediate settlement at CA; retained future obligations; every retained cut replay/readback and terminal retries. Task017 Release execution, later movement/repeat, positive actual creation-rooted history, Snapshot successor, HOST-PUB-001/public activation and future-duty execution stay open. No heavy pivot or additional workstream needed. Candidate plan correctly says acceptance awaits review.

## Author-Claim Reconciliation

| Author claim | Independent evidence | Status | Consequence |
| --- | --- | --- | --- |
| 32 contexts,272 events,304 cuts,32 finals | Executed Closure tests; independent unchanged Result2 oracle | Confirmed | Literal and causal parity supported |
| Original-only relationships and raw Retreat suppression | Relationships projection, typed endpoints, fixture branch assertions | Confirmed | Guards cannot inherit relation; admitted profile remains bounded |
| Immediate proof and actual CA receipt linkage | Transition and ValidClosureEvidence; proof/topology/borrowed-receipt tests | Confirmed | Terminal state has causal closure evidence |
| Full World resources/future duties unchanged by closure | Project/WorldValue equality and all-final-state assertions | Confirmed | No resource reset or execution of future duties |
| Retries survive closure; stale timers no-op; fresh work rejects | EveryCutRetriesAndDiscardRemainStableIncludingClosedAndFreshCommandsReject | Confirmed | Closed lifecycle remains idempotent |
| New fields are internal replay-derived metadata | State model and unchanged schema; ReadState replay | Confirmed | No wire version or field churn |
| Worker RED/GREEN history and102/73 counts | Historical developer execution not rerun as history | Unverified historical testimony | Not needed for verdict; independent checks below substitute |
| Dormant synthetic scope excludes public/actual-history completion | Result2 spec, Task016 refinement, unchanged API surface and docs | Confirmed | Verdict applies only to bounded slice |

## Verification Performed

All commands in isolated clone, `login:false`. Restore/build/tests/format received approved network/local IPC access. No shared build outputs used.

1. `dotnet restore Sandtable.slnx --disable-parallel` — passed with approved access; log `/tmp/task016-review3-restore-approved.log`. Initial sandbox attempt stalled without output and was stopped, not counted as pass.
2. `dotnet build Sandtable.slnx --no-restore --disable-build-servers -m:1 -bl:/tmp/task016-review3-build.binlog` — full solution built from source, **0 warnings,0 errors**,20.90 seconds. Log `/tmp/task016-review3-build.log`.
3. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build -- --filter-class '*CombatClosureTests' --filter-class '*CombatCustodyTests' --filter-class '*CombatLossRetreatTests' --filter-class '*CombatResolutionTests' --filter-class '*CombatWorldTests' --filter-class '*CombatWorldInitialCodecTests'` — **62 passed,0 failed,0 skipped**,1m11s. Log `/tmp/task016-review3-focused-corrected.log`.
4. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'` — **81 passed,0 failed,0 skipped**,9.8 seconds. Exact Boundary command from `justfile`. Log `/tmp/task016-review3-boundary-corrected.log`.
5. `python3 -B docs/specs/verify-combat-result-settlement-v2.py` — PASS:10 semantic groups;32 traces,304 cuts,3728 mutations,1360 raw rejects,384 timing checks;200 same-owner prior-time isolation comparisons. Log `/tmp/task016-review3-oracle.log`.
6. `dotnet format Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatResolution.cs src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs src/Cna.Core/Campaigns/CampaignCombatObligations.cs src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs tests/Cna.Core.Tests/Campaigns/CombatClosureTests.cs tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs tests/Cna.Core.Tests/Campaigns/CombatCustodyTests.cs tests/Cna.Core.Tests/Campaigns/CombatLossRetreatTests.cs` — exit0; log `/tmp/task016-review3-format.log`.
7. `git diff --check d600966 HEAD` — passed. Final clone `git status --short` empty.

Invocation corrections, not product failures: initial pipe-separated class glob selected zero tests (exit5); guessed standalone Boundary project path did not exist (exit1). Replaced with repeated class filters and repository Boundary trait command. Native `dotnet test ... -- --help` hit SDK help-message FailFast; no test outcome inferred from that attempt. Corrected test commands completed successfully.

## Open Questions And Residual Risks

No blocking question. Independent full solution test suite not run; full solution build plus focused/shared regressions and Boundary suite were proportionate to bounded diff. Literal contexts remain synthetic and fixed initial profile; they do not prove later-cycle topology, unsupported arrival/death profiles, durable publication or external-process recovery. Canonical replay/readback checks prove supplied-history reconstruction rather than actual campaign restart. Duplicate topology/proof derivation is small deliberate serializer validation cost; no concrete defect established.

## Verdict

**Ready** for bounded Task016 at exact candidate above. No readiness claim for later integration gates.

## Recommended Next Actions

Initiating task should record acceptance against immutable candidate and preserve explicit later gates. Review budget exhausted: instance3 of3 complete; no further independent review instance starts under current flow.

Process cleanup complete: stopped stalled sandbox restore PID22509 and nine idle restore-created MSBuild nodes (22778–22783,22791–22793), identified by creation timing and parent confirmation of no overlapping root .NET work. Final process inspection showed no remaining review/Node MSBuild processes. No global build-server shutdown used.
