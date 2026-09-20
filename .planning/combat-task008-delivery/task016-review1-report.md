# Task016 independent review

Review instance: 1 of 3.

## Findings

No actionable findings in reviewed immutable diff `d600966..d4805567716b7acb4d99f5c4bd8b3465df976450`.

Independent clone `/tmp/sandtable-task016-review1-1846` detached at exact candidate, clean before and after checks. Reviewed canonical requirements and tests before production diff. Persisted `task016-review1-preliminary.md` before reading `task016-author.md`. No CCE/memory/history tools, prior review reports, aggregate execution history or worker evidence used. No source edits, commits or further agents.

Evidence supporting conclusion:

- `CampaignCombatLossRetreat.Relationships` derives adjacency from authenticated Content edges and final original participant locations. Raw required retreat suppresses Engaged even after refusal. Publication uses settlement attacker/defender keys, never guards. Certification already constrains admitted profile to surviving original units; general elimination/new-arrival behavior is outside admitted scope.
- `CampaignCombatSettlementState.WithResultV2Relationships` requires disposition, loss and retreat plus custody iff captured TOE is positive. Existing unconditional receipt validation retains causal ordering and occurrence binding. `Project` independently reconstructs settlement before publishing relationships; full typed World comparison remains enforced by `CampaignCombatResolutionCodec.WorldValue` after relationship publication.
- `CampaignCombatResolution.Transition` advances relationships, ordered four/five-receipt round proof, then one CA completion bound to actual closure receipt and original fifth step. Structural time/version/actor checks remain enforced. Exact retry recovery and stale callbacks precede terminal guard; fresh terminal work rejects.
- `ValidClosureEvidence` recomputes terminal command/event identity from replay-derived pre-event version/prefix, verifies actor and receipt hashes, and links CA predecessor prefix/version to round closure. External readback still replays separate trusted history and compares complete canonical bytes; origin fields are not replacement authority.
- Seven new tests cover complete frozen final states, every prefix, exact retries/discard, rehashed event and state mutations, borrowed terminal receipts, owned proof/byte storage and syntax rejection before trusted context. Existing regression-frontier expectations remain intact except newly legal next transition.

## Plan Review

Task016 implements its bounded dormant Result2 checkpoint: original relationships, terminal round/CA receipts, no pending immediate settlement, retained future duties. Scope exactly matches explicit refinement: four production files plus new closure tests, three dependent regression-maintenance test files. Design/naming/plan changes and seven administrative artifacts account for remaining paths; no schema, fixture, shared World7, topology or registration changes.

Task017 Release execution, Task018/019 movement/repeat, actual positive creation-rooted history, Snapshot successor, durable host publication, public activation and future-duty execution remain excluded and visibly open. README still describes reviewed-through-Task015 status while candidate is under verification; acceptance status should be synchronized by initiating task only after acceptance. This is consistent with current pending status, not a correctness finding.

No architectural pivot, migration or rollout change required. Internal origin fields add bounded event reconstruction work and duplicate proof/topology derivation in writer and validator; independently recomputed checks are justified by forged typed terminal-state regression.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| All 32 contexts match 272 events, 304 hashes and complete terminal JSON | `AllClosurePrefixesMatch272LiteralEvents304HashCutsAnd32CompleteFinalStates`, independently passed; immutable fixture and oracle | Confirmed | Strong byte compatibility and all-cut recovery evidence |
| Original-only adjacency with raw Retreat precedence | `Relationships`, `FinishRelationships`, closure aggregate assertions; canonical World/settlement contracts and certification survivor proof | Confirmed | Guards cannot inherit relationship; refusal produces Contact |
| Full World comparison survives relationships | `WorldValue`, `Project`, `TypedPostRelationshipResourceForgeryAndStageMetadataReject` | Confirmed | Shared World later-movement allowance cannot relax Result2 resource validation |
| Closure proves ordered immediate receipts and CA binds actual closure | `Transition`, `WithResultV2Relationships`, exact literals, proof tampering and borrowed-receipt tests | Confirmed | No skipped custody/loss chain or invented terminal stage through normal APIs |
| Exact retries survive terminal closure; stale callbacks NoOp | Retry-before-closed ordering and all-cut retry test | Confirmed | Closure remains idempotent |
| Future duties/resources/RNG/history unchanged | Complete final JSON plus explicit frontier/final equality assertions | Confirmed | No premature feeding, training, Release or reroll |
| Wire schema and fixture unchanged | Git diff and frozen schema inventory | Confirmed | No compatibility migration introduced |
| Development RED checks and worker 102/73 passes occurred | Author testimony only; historical logs deliberately not used | Unverified | Not relied on; reviewer independently executed 73 focused/adjacent tests and oracle |
| Full integration gates and acceptance await root checks | Author and canonical plan pending wording | Confirmed as scope/status | Reviewer verdict does not assert full solution, durable/public activation evidence |

## Verification Performed

All commands run with `login:false` in isolated clone; no shared build outputs.

1. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatClosureTests' -bl:/tmp/sandtable-task016-review1-1846/closure-review1.binlog`
   - Initial sandbox attempt failed before tests: native MTP `SocketException (13): Permission denied` binding local IPC, exit134. Not a test failure.
2. Same focused command with approved escalation and unique `-bl:/tmp/sandtable-task016-review1-1846/closure-review1-escalated.binlog`.
   - Build and test completed, exit0: **7 passed, 0 failed, 0 skipped**, 49.218s test duration.
3. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatLossRetreatTests' --filter-class '*CombatCustodyTests' --filter-class '*CombatResolutionTests' --filter-class '*CombatWorldTests' --filter-class '*CombatIdentityTests' -bl:/tmp/sandtable-task016-review1-1846/adjacent-review1.binlog`
   - Approved IPC escalation; exit0: **66 passed, 0 failed, 0 skipped**, 38.614s.
4. `python3 -B docs/specs/verify-combat-result-settlement-v2.py > /tmp/sandtable-task016-review1-1846/result-oracle.log 2>&1`
   - Exit0: **10 semantic groups; 32 traces; 304 cuts; 3,728 mutations; 1,360 raw rejects; 384 timing checks; 200 same-owner prior-time isolation comparisons**.
5. `dotnet format tests/Cna.Core.Tests/Cna.Core.Tests.csproj --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatResolution.cs src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs src/Cna.Core/Campaigns/CampaignCombatObligations.cs src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs tests/Cna.Core.Tests/Campaigns/CombatClosureTests.cs tests/Cna.Core.Tests/Campaigns/CombatCustodyTests.cs tests/Cna.Core.Tests/Campaigns/CombatLossRetreatTests.cs tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs`
   - Exit0; output retained in `format-review1.log`. Separate Core check below ensures production project coverage.
6. `dotnet format src/Cna.Core/Cna.Core.csproj --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatResolution.cs src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs src/Cna.Core/Campaigns/CampaignCombatObligations.cs src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs`
   - Exit0; empty `format-core-review1.log`.
7. `git diff --check d600966..HEAD`; final `git status --porcelain`; `git rev-parse HEAD`.
   - Exit0, no whitespace errors or tracked/untracked changes; exact candidate retained.

All launched test/oracle/format sessions completed; no owned foreground process remains. No global build-server shutdown performed. Read-only process inventory attempt was sandbox-denied; not used as evidence about other processes.

## Open Questions And Residual Risks

No blocking open question. Verification is bounded to dormant synthetic Result2, not actual creation-rooted positive campaign authority, full solution or production host lifecycle. New arrival scenarios cannot enter selected two-unit profile; general future profile expansion must retain explicit original-participant and survivor checks. Full repository integration gate remains initiating task responsibility. Historical RED/worker claims were not independently reproduced from development history.

## Verdict

**Ready** for bounded Task016 scope at exact candidate `d4805567716b7acb4d99f5c4bd8b3465df976450`.

## Recommended Next Actions

Initiating task may reconcile this report with its exact-candidate repository gate and remaining authorized review instances, then update acceptance status. Preserve explicit later-task exclusions. No code fix requested.
