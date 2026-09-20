# Task016 independent review

Review instance: 2 of 3.

## Findings

No actionable defects found in `d600966..d4805567716b7acb4d99f5c4bd8b3465df976450` within bounded Task016 scope.

Independent blind ledger persisted as `task016-review2-preliminary.md` before reading author explanation. No inherited implementation conversation, CCE, memory, prior reviews or worker/check evidence used. Exact candidate reviewed in clean detached clone `/tmp/sandtable-task016-review2-1852`; integration metadata excluded. No source changes or commits.

## Plan Review

Task016 implements its selected acceptance criteria. `CampaignCombatLossRetreat.cs:140` derives adjacency from authenticated Content edges and current original element locations, suppresses Engaged for raw required Retreat, and writes original endpoints only. `CampaignCombatObligations.cs:289` requires disposition, losses, retreat and positive-capture custody before relationship append. `CampaignCombatResolution.cs:300` advances relationships, ordered immediate proof, and CA completion; future duties survive projection without execution.

`CampaignCombatResolutionCodec.cs:79` recomputes expected terminal command/event identities and prefix progression from internal origins. Complete typed World comparison remains at `WorldValue`; relaxed shared World movement semantics cannot admit changed Result2 resources. Raw readback rebuilds complete causal history, independent of caller-supplied state. Retry/no-window callback checks precede terminal rejection.

Five material source/test paths plus three narrow regression-maintenance paths match explicit refinement. Existing tests preserve prior golden frontiers and skipped-history rejection. Contracts, fixtures, shared World7, topology and registration unchanged. Fixed turn1 topology is appropriate: `CampaignCombatSelectionSteps.ValidateBoundary` authenticates exact retained first-operation route before round replay.

Plan ordering and exclusions remain coherent: Task017 Release, later Movement/repeat, actual positive creation-rooted history, snapshot successor and host/public activation remain later work. No migration or rollout implied by dormant internal extension. README's accepted-through-Task015 wording remains appropriate while candidate is under verification; update accepted-state map when Task016 is promoted.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| 32 contexts, 272 event literals, 304 hash cuts, 32 final JSON states | Closure golden test executed; Result2 oracle separately executed | Confirmed | Exact selected corpus matches contract |
| Original-only adjacency; Retreat suppresses Engaged even refused | Relationships projection; 16 Contact, 4 Engaged, 12 absent assertions | Confirmed | No guard/new endpoint inheritance |
| Immediate custody precedes closure; future resources retained | Typed append guard; projection; closure final-state equality; custody regressions | Confirmed | Complete selected immediate settlement, no future execution |
| Terminal origins prevent borrowed receipt relabeling | ValidClosureEvidence; BorrowedReceiptCannotInventTerminalStage and mutation tests | Confirmed | Internal serialization checks actual expected terminal receipt semantics |
| Retry survives closure; fresh actions reject, stale callbacks no-op | Transition ordering; every-cut retry test | Confirmed | No duplicate closure or restarted choice |
| Earlier golden frontiers retained | Custody/LossRetreat/Resolution diff and executed tests | Confirmed | Regression edits limited to newly supported next transition |
| Historical RED/GREEN and worker102/73 counts | Author testimony only; worker history deliberately not consumed | Unverified | Not relied on; independent execution below supplies evidence |
| No full positive campaign/host/public proof claimed | Internal API and plan exclusions | Confirmed | Verdict limited to dormant Task016 |

## Verification Performed

All commands ran at exact candidate in isolated clone, shell `login:false`; .NET commands used approved escalation for restore/network and native test IPC. Unique binary logs remain in clone.

1. `git clone --no-hardlinks /Users/dsteele/.codex/worktrees/8fcc/sandtable /tmp/sandtable-task016-review2-1852` then detached checkout at exact candidate: passed; clean tracked state verified.
2. `dotnet restore tests/Cna.Core.Tests/Cna.Core.Tests.csproj --disable-parallel -bl:/tmp/sandtable-task016-review2-1852/restore-review2.binlog`: passed.
3. `dotnet build tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore -m:1 /nodeReuse:false -p:UseSharedCompilation=false -bl:/tmp/sandtable-task016-review2-1852/build-review2.binlog`: passed, zero warnings/errors.
4. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatClosureTests'`: 7 passed, 0 failed/skipped, 47.530s.
5. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatCustodyTests' --filter-class '*CombatLossRetreatTests' --filter-class '*CombatResolutionTests' --filter-class '*CombatWorldSettlementTests' --filter-class '*CombatObligationsTests' --filter-class '*CombatRelationshipTests'`: 23 passed, 0 failed/skipped, 29.593s. Last three patterns match no current classes; first three provide 23 tests. Initial attempt joined patterns with `|`, ran zero tests and exited5; corrected invocation above passed.
6. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatWorldTests' --filter-class '*CombatCommitTests' --filter-class '*CombatSealsTests' --filter-class '*CombatSelectedRulesTests' --filter-class '*CombatRulesInputArtifactTests' --filter-class '*CombatRulesetManifestTests'`: 119 passed, 0 failed/skipped, 8.633s.
7. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'`: 81 passed, 0 failed/skipped, 9.369s.
8. `python3 -B docs/specs/verify-combat-result-settlement-v2.py`: passed, 10 semantic groups, 32 traces, 304 cuts, 3728 mutations, 1360 raw rejects, 384 timing checks, 200 same-owner comparisons.
9. `python3 -B docs/specs/verify-combat-world-settlement-v1.py`: passed, 6 goldens, 57 rejection vectors, 112 isolated cuts/20 scenarios, 8840 arithmetic cases, 8 calendar boundaries.
10. `dotnet format tests/Cna.Core.Tests/Cna.Core.Tests.csproj --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs src/Cna.Core/Campaigns/CampaignCombatObligations.cs src/Cna.Core/Campaigns/CampaignCombatResolution.cs src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs tests/Cna.Core.Tests/Campaigns/CombatClosureTests.cs tests/Cna.Core.Tests/Campaigns/CombatCustodyTests.cs tests/Cna.Core.Tests/Campaigns/CombatLossRetreatTests.cs tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs`: passed.
11. `git diff --check d600966 HEAD`: passed. Final tracked status clean. Process inspection found no remaining clone-owned test/build processes; no global build-server shutdown performed. Initial sandboxed `ps` was unavailable; approved read-only inspection succeeded.

## Open Questions And Residual Risks

No blocking open questions. Full solution `just check` belongs to separate root/reviewer gate and was not run or claimed here. Executed 230 test cases across four runs may overlap at boundary suite; no unique-total claim. Fixture replay is strong deterministic selected-profile evidence, not process restart, durable publication, arbitrary scenario coverage, or executable future obligations. Internal origin metadata is replay-derived validation support, not standalone externally authenticated authority.

## Verdict

Ready — bounded Task016 candidate, subject to separately owned full integration gate and remaining configured review.

## Recommended Next Actions

Retain exact candidate identity, reconcile remaining independent review and full gate results, then update accepted-state documentation when promoting Task016. Preserve explicit later-task exclusions.
