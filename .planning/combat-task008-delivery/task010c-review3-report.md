# Task010C independent review

Review instance: 3 of 3. Reviewer mode. Base `fe5664d2c0d4ac56b1db8ef21ef850796d625fea`; code head `9b999a7df5d1a0f54b7ca4ffcf045197734169a7`; branch `codex/combat-task008-reaction-lifecycle`.

## Findings

No actionable findings in scoped implementation or canonical Task010C plan. Independent rebuild and 42 focused/regression tests pass. Source manifest matches before and after verification. Independence limitation recorded below remains part of this verdict.

Evidence: `CampaignCombatHistoryReplay.cs:63–80` validates one strict G2 record, passes only that exact predecessor to selection admission, and sends the entire remaining tail to traversal. `CampaignCombatCertification.Admit` admits only completed G2 with supported actual facts. Recursive replay therefore terminates before the new suffix. Existing local readers reconstruct authoritative input/effects and compare complete canonical bytes; partition counts cannot establish authority.

`CampaignCombatHistoryModels.cs:66–95` adds closed projections with get-only State and owned read-only cumulative receipts. Underlying local states are immutable; cached ledger cannot drift after record cloning. New tests compare cumulative versions/event hashes, local golden Controls, nested selection and complete Boundary bytes at every cut. Existing Snapshot12 writer's closed switch rejects both new projection types; Restore reaches the same switch. No current World/RNG/resource reset or Reserve release occurs.

## Plan Review

Canonical `docs/design/combat-cycle-implementation-plan.md:875–883` separates actual empty selection/traversal010A, synthetic timed mechanisms010B and cumulative routing010C. Implementation follows that split without new contracts, fixture changes, public APIs or persistence arms. Accepted selection/no-attack contracts remain governing: exact four histories, immutable nested selection, six transitions ending at same-slot Reserve Release, strict replay and no material mutation.

Three primary source/test paths match authorized slice. Created-before-caller-list capture is a bounded ownership correction exercised by adversarial list test. Repeated predecessor replay adds bounded work but avoids new cache authority; no architecture pivot warranted. README/roadmap correctly leave parent010 open during review; tech-design describes implemented route and persistence exclusion. Prepared011, actual positive history, extended Snapshot12 and HOST-PUB-001 remain separate gates. No naming change requires naming-overview update. Root retains acceptance, publication and budget ownership.

## Author-Claim Reconciliation

Author/checks packets read only after preliminary ledger was persisted.

| Author claim | Independent evidence | Status / consequence |
| --- | --- | --- |
| Exact G2 cut prevents self-admission; no ignored tail | Router lines63–80, strict local replay, new negative tests | Confirmed. |
| Cumulative receipts owned and State replacement impossible | Projection constructors/get-only properties; ownership/reflection test | Confirmed. Local wire Controls unchanged. |
| Four histories,32 new cuts,36 including G2,118 prefix visits | Explicit loop/count assertions and frozen event/Control commitments | Confirmed by passing focused test.118 is visits, not distinct histories. |
| World/RNG/resources and nested selection preserved | Complete Boundary byte comparison and frozen nested Control checks; inherited regression | Confirmed within frozen actual profile. |
| Synthetic C3a and unsupported tails rejected | Synthetic fixture cases, short G2 and Reaction fork negatives | Confirmed. No actual positive adapter claimed. |
| Old368 roots/286 histories retained; new families unsupported | Snapshot regression exact bytes/hashes/restore, new writer/reader rejection test | Confirmed. |
| Historical RED/GREEN, format, fullsuite2284, Boundary81 and CI | Checks packet testimony only; logs/CI not inspected | Unverified independently. Not substituted for reviewer checks; root owns these gates. |

## Verification Performed

All commands ran from repository root with `login:false`; .NET rebuild/tests used authorized local IPC. SDK `dotnet --version`:10.0.400, consistent with global.json latestFeature policy; SDK-style net10.0, native MTP, xUnit package `xunit.v3.mtp-v2`4.0.1 confirmed from configuration.

1. `shasum -a 256 -c .planning/combat-task008-delivery/task010c-source.sha256` — all three files OK before/after checks.
2. `dotnet build tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore -t:Rebuild -p:UseSharedCompilation=false -nodeReuse:false '-bl:/tmp/task010c-review3-rebuild-{}.binlog'` — exit0, zero warnings/errors,7.95s. Independently rebuilt before tests.
3. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatStepsHistoryReplayTests' '-bl:/tmp/task010c-review3-focused-{}.binlog'` — exit0,7 passed/0 failed/0 skipped,44.684s.
4. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatInheritedStepsTests' --filter-class '*CombatInheritedSnapshotTests' --filter-class '*CombatMovementHistoryReplayTests' '-bl:/tmp/task010c-review3-regressions-{}.binlog'` — exit0,35 passed/0 failed/0 skipped,59.580s. Includes inherited all-cut retries, old368 roots/286 histories and Movement/G2 regressions.
5. `git diff --check fe5664d2c0d4ac56b1db8ef21ef850796d625fea HEAD -- src/Cna.Core/Campaigns/CampaignCombatHistoryReplay.cs src/Cna.Core/Campaigns/CampaignCombatHistoryModels.cs tests/Cna.Core.Tests/Campaigns/CombatStepsHistoryReplayTests.cs README.md docs/roadmap/pre-alpha-roadmap.md tech-design.md` — exit0, no whitespace errors.
6. `git diff --quiet 9b999a7df5d1a0f54b7ca4ffcf045197734169a7 -- src/Cna.Core/Campaigns/CampaignCombatHistoryReplay.cs src/Cna.Core/Campaigns/CampaignCombatHistoryModels.cs tests/Cna.Core.Tests/Campaigns/CombatStepsHistoryReplayTests.cs README.md docs/roadmap/pre-alpha-roadmap.md tech-design.md` — no scoped working-tree diff. HEAD/branch match dispatch. Existing dirty execution/checks metadata untouched.
7. `dotnet build-server shutdown` — exit0, compiler/MSBuild servers shut down. All build/test sessions exited0. `pgrep -fl 'dotnet|Cna.Core.Tests|MSBuild|VBCSCompiler' || test $? -eq 1` — exit0, no remaining matching processes before final report.

Verified binlogs exist:

- `/tmp/task010c-review3-rebuild-20260920-101106--24249--h0gLyx.binlog`
- `/tmp/task010c-review3-focused-20260920-101122--24286--9CNQAY-dotnet-test.binlog`
- `/tmp/task010c-review3-regressions-20260920-101217--24328--WeTOw8-dotnet-test.binlog`

## Open Questions And Residual Risks

No blocking scope question. Full solution, Boundary suite, full format, Python oracles and remote CI not independently rerun or inspected. No new tests or source edits made. Repeated predecessor reconstruction remains deliberate bounded cost; no performance benchmark claimed. Historical review snippets accidentally surfaced by initial docs search limit perfect blindness, as disclosed in original preliminary ledger; no Task010C review report or verdict was read or used. Full historical report files, aggregate evidence, worker/devnotes, proposal rationale, execution history and cross-session retrieval remained excluded.

## Verdict

**Ready** for scoped Task010C implementation and plan. Root decides overall acceptance using its separately owned gates.

## Recommended Next Actions

Return this report to root for acceptance reconciliation. Instance3of3 exhausted; no further review, agent or workstream started. Source/Git unchanged; only own report written, apart from authorized build/test artifacts and temporary binlogs.

## Preliminary ledger — persisted before author/checks packets

No actionable defect found in blind source/test/canonical-plan pass. Runtime verification pending; no readiness claim yet.

| Concern | Blind evidence | Preliminary assessment |
| --- | --- | --- |
| Recursive predecessor admission | HistoryReplay exact one-event G2 partition, predecessor copy ending at cursor; Certification.Admit requires BreakdownCompletion | Recursion terminates at exact G2; complete outer suffix cannot authenticate itself. |
| Tail loss or false authority | Two-event Selection strict replay; all remaining events passed to bounded NoAttack replay | Counts partition only; byte regeneration and admission supply authority; excess/forged/reordered tails covered. |
| Cumulative ledger ownership | HistoryModels.Selection/NoAttack copy concatenated receipts into read-only arrays; State get-only; inherited states immutable | Cumulative ledger includes predecessor exactly once, local Controls unchanged. |
| State continuity | FourHistoriesPreserveAllPrefixesLedgersAndFrozenLocalControls checks frozen hashes, nested selection and complete Boundary bytes | 32 new cuts, 36 including G2, 118 total visits distinguished; World/RNG/resource continuity retained through boundary equality. |
| Retry semantics | Existing CombatInheritedStepsTests retries every retained input at every local cut | Existing strict modules own retries; generic router remains replay-only. Run regression. |
| Snapshot compatibility | Existing SerializeState closed switch defaults to JsonException; Restore calls Compose | New projection families fail closed; run old 368-root test. |
| Plan and docs | Canonical combat-cycle-implementation-plan Task010 refinement; inherited selection/no-attack contracts; scoped README/roadmap/tech-design diff | Small integration slice fits 010C, leaves Prepared011, positive history, public activation and publication outside scope. |
| Source identity | All three task010c-source.sha256 entries pass | HEAD matches dispatch; source clean. Existing dirty execution.md/checks.md metadata preserved. |

Independence limitation: initial broad `rg` over docs accidentally returned excerpts from historical review files. Those snippets were not used as evidence; subsequent reads restricted to named canonical/source/test files. No Task010C prior report opened, no cross-session retrieval, aggregate evidence, worker/devnotes or proposal rationale read. Dispatch file contained prior acceptance metadata; treated only as scope authorization, not validation evidence.

Next: independent rebuild, focused new/inherited/snapshot tests, then author/checks claim reconciliation. Only this report may be edited; no fixes, commits or agents.
