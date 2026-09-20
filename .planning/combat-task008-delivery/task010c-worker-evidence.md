# Task010C worker evidence — frozen handoff

Authorized dispatch after010B762468e/fe5664d. Own HistoryReplay.cs, HistoryModels.cs and new CombatStepsHistoryReplayTests.cs only, plus this evidence. No commits, push, agents, full suite, schema/fixture/Snapshot writer edits. Root owns docs/integration/full gate/reviews.

Implementation: consume one strictly validated G2 completion, retain exact predecessor cut, replay actual selection up to two events, then pass entire remaining suffix into strict bounded no-attack reader. Exact G2 returns before successor admission, terminating recursive009A/010A predecessor replay. New Selection/NoAttack projections cache cumulative read-only receipts from accepted immutable local states; State and ledger get-only. Local Controls/receipts unchanged. Bounded Created copied before caller history Count/index; capture routine otherwise unchanged.

TDD commands use native .NET10 MTP/xUnit v3, login:false, authorized local IPC. Commands below run from repository root; each redirects stdout/stderr to matching /tmp stem.log. Each '-bl:/tmp/STEM-{}.binlog' produces unique build/test binary logs.

1. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatStepsHistoryReplayTests' '-bl:/tmp/010c-red-{}.binlog'`: expected RED, exit2,1failed/0passed,3.986s. Existing G2 reader rejects valid complete010A suffix: `Breakdown completion requires three actual lifecycle records and at most one completion.` /tmp/010c-red.log.
2. Same command with010c-green1: exit0,1passed,4.227s. Bounded router/models implemented.
3. Same command with010c-expanded: exit2,5passed/1failed,41.009s. All118 prefix visits/32 newcuts/36 withG2 and golden Control readbacks passed. Failure was test assumption that opposite-side histories had different Created bytes; all share fixed creation evidence. Corrected negative to alter canonical campaignId binding, not production behavior. /tmp/010c-expanded.log retained.
4. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatStepsHistoryReplayTests' --filter-method '*RetainedBoundsRejectBeforeIndexingAndCreatedIsOwnedBeforeListAccess' '-bl:/tmp/010c-ownership-red-{}.binlog'`: expected RED,exit2,1failed,4.023s. Caller list Count mutates original Created before existing capture clones it; strict creation reader rejects. After RED, router validates Created length then owns bytes before capture/list access. Scope remains three paths.
5. Expanded focused GREEN2 passed; final results below.

Acceptance matrix implemented: four complete actual G2 histories (six/seven moves, both sides),118 total prefix visits,36 cuts at/afterG2,32 new cuts; all head versions/prefix folds/event hashes/ledger ordinals; owned exact cumulative concatenation; same frozen local Control/event SHA256+byte lengths and strict fragment readback; unchanged complete Boundary includes World/RNG/weather/end/progress. Nested selection remains step0/notclosed while traversal reaches Reserve Release. Reject missing/duplicate/reordered/foreign/legal other-branch suffixes, rehashed effect/context mutations, extra tail, all literal synthetic C3a cases, unsupported G2 movement scopes and Reaction tail.009A exactG2 still admits, full successor stream rejects wrong family. Snapshot12 oldG2 exact restore remains valid; new families fail closed for Serialize/Restore, fresh admission disabled. Returned buffers/ledgers immutable and State replacement impossible. Top512/per1MiB/aggregate16MiB retained bounds tested plus local excess tail.

No actual positive admission, synthetic010B generic routing, Snapshot12 extension, publication, Prepared011 or downstream completion claim. Four local fixture Controls are reconstructed from accepted typed APIs and checked against independent frozen hashes/lengths; fixtures do not contain literal Control JSON. Existing010A88 commitments remain cumulative evidence; new test does not claim all88 as new checks.

GREEN2 completed exit0,7passed/0failed/0skipped,45.499s. Scoped format command:
`dotnet format Sandtable.slnx --no-restore --include src/Cna.Core/Campaigns/CampaignCombatHistoryReplay.cs src/Cna.Core/Campaigns/CampaignCombatHistoryModels.cs tests/Cna.Core.Tests/Campaigns/CombatStepsHistoryReplayTests.cs`
completed exit0, empty `/tmp/010c-format.log`. Scoped `git diff --check` exit0. Final focused/shared/format verification completed below.

Frozen-after-format primary SHA256 (no later code changes planned):
- HistoryReplay.cs: `be962da53664ae41ae4ef9ee9ffb158a84e708ba4eaa2075185b93b90d1ab7fd`
- HistoryModels.cs: `4356254833ef3fa0d317fc913a453c332858a37047a7f1a5d5b8a15effefa88d`
- CombatStepsHistoryReplayTests.cs: `7f5333ee2b4841346d25ff29906701d840769c6724d6137caadf0b260bfec523`

Final focused post-format command:
`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatStepsHistoryReplayTests' '-bl:/tmp/010c-final-{}.binlog'`
exit0,7passed/0failed/0skipped,44.705s; `/tmp/010c-final.log`.
Binary logs: `/tmp/010c-final-20260920-094633--22263--WX_527.binlog`, `/tmp/010c-final-20260920-094633--22263--HK8Amu-dotnet-test.binlog`.

Shared command:
`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatMovementHistoryReplayTests' --filter-class '*CombatInheritedStepsTests' --filter-class '*CombatIdentityTests' --filter-class '*CombatInheritedSnapshotTests' '-bl:/tmp/010c-shared-{}.binlog'`
Result exit0,49passed/0failed/0skipped,1m07.838s; includes all368 legacy Snapshot12 vectors/286histories. Binary logs `/tmp/010c-shared-20260920-094736--22321--Ip3jqL.binlog`, `/tmp/010c-shared-20260920-094736--22321--HMa6BP-dotnet-test.binlog`; `/tmp/010c-shared.log`.


Final scoped format verification:
`dotnet format Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatHistoryReplay.cs src/Cna.Core/Campaigns/CampaignCombatHistoryModels.cs tests/Cna.Core.Tests/Campaigns/CombatStepsHistoryReplayTests.cs`
exit0, empty `/tmp/010c-format-verify.log`. Final scoped diff check exit0. SHA256 manifest rechecked unchanged after final checks. All worker test/build/format sessions finished and closed; root notified it may start fullgate/reviewer .NET. No source changes after finalfocused. Root fullsuite/reviews remain parent-owned and pending at handoff.

Exact first-RED logs: `/tmp/010c-red-20260920-094025--21780--5Qea+c.binlog`, `/tmp/010c-red-20260920-094034--21780--3IXotJ-dotnet-test.binlog`.
Ownership RED logs: `/tmp/010c-ownership-red-20260920-094420--21999--yfaonw.binlog`, `/tmp/010c-ownership-red-20260920-094424--21999--TaicMb-dotnet-test.binlog`.
Expanded test-assumption failure logs: `/tmp/010c-expanded-20260920-094324--21935--OLZ7Tu.binlog`, `/tmp/010c-expanded-20260920-094329--21935--6GIxVB-dotnet-test.binlog`.
GREEN2 logs: `/tmp/010c-green2-20260920-094450--22043--MOvTjB.binlog`, `/tmp/010c-green2-20260920-094458--22043--xwPeS5-dotnet-test.binlog`.
