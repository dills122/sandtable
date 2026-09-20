# H1 pre-cycle ordered retained-history routing

Base4139675fc9b775c5e7c0d355c06756c17ea1f2f9 accepted H0 and recorded remote-gate recovery; same main-based PR136.
H0 passed dev+three independent Ready rounds and all15predecessor oracles. Read canonical Task008
Initial H/H execution refinement, accepted combat-inherited-snapshot-v1 contract and
h1-candidate-scope.md. Original user asks continued autonomous implementation with dev+three fresh
reviews per bounded slice. No H2 code before H1 acceptance.

Own exactly five primary paths:
- src/Cna.Core/Campaigns/CampaignCombatRetainedHistory.cs
- src/Cna.Core/Campaigns/CampaignCombatHistoryModels.cs
- src/Cna.Core/Campaigns/CampaignCombatHistoryReplay.cs
- tests/Cna.Core.Tests/Campaigns/CombatHistoryReplayTests.cs
- tests/Cna.Core.Tests/Cna.Core.Tests.csproj (H0 fixture link only if needed)
No existing production/predecessor contract/oracle edits. Root owns docs/reviews/full integration/git.
You are not alone in codebase; don't revert others' work. Coordinate if file ownership/scope needs
refinement. Do not commit/push/createPR, edit .planning/docs, or start independent review yourself.

Implement an internal dormant pre-cycle API that requires trusted request, independently retained
canonical Created11, and ONE complete ordered accepted event list. It derives causal partitions and
replays accepted readers through creation/preamble/Weather/stage/optionalReserve designation and
same-event atomic first-cycle completion. Every legal prefix including emptyhistory is supported.
Do not regenerate missing Created11. No caller family discriminator/cache/supplied partition gets
trusted. Each exact next event validated by actual causal reader, not merely eventType/version.
Unsupported event/extra tail after firstopening rejects, never truncates. H2/H3 later extend routing.

Use bounded defensive ownership before copying:512events,16MiB aggregate,eachnonempty≤1MiB,
Creatednonempty≤1MiB. Concrete IReadOnlyList input; rejectcount before enumerating/indexing. Check
aggregate/elementbounds before expensivehistorycopy/replay, and isolate buffers from source/result
mutations. Existing C# immutable trusted request/context validation reused; no opaque JSON authority.
Result retains typed actual family projection and complete owned history/header/prefix evidence;
choose small closed type representation that futureH2/H3/H4 can extend without speculative registry.
No public registration/actions, host/persistence, runtimefullSnapshot12codec or admissionflag claim.
Full-root literal readback and actual freshadmissiondisabled seam remain H4.

TDD: first focused meaningful failure proves missingbehavior/types, then implement and expand:
- every precycle prefix/selected seeds/owners/Weather/stage/Reserve variants; match H0 expected
  creation/eventhashes and actual causal state/header/receipt evidence plus predecessor goldens;
- omit/duplicate/reorder/noncanonical bytes/wrongsuccessor/version/context/creation, forgedcanonical
  events, wrong legalfork against trustedprojection if applicable, unsupported valid futuretail;
- count/aggregate/elementbytes/null/bufferownership boundaries, no generatedcreationfallback;
- preserve all accepted readers and legacycreationSnapshot strict behavior.
Matching only version/prefix is not fullSnapshot12parity; state that limit. Avoid a test mirroring
router's branching. Existing reviewed familytest Chain/Predecessor helpers guide actualinputs.

Use required TDD/run-tests/platform/binlog skills. .NET10 nativeMTP; login:false. Known sandbox IPC
requires require_escalated (skip alreadyknown sandbox probe); exact focused command uses
--project tests/Cna.Core.Tests/Cna.Core.Tests.csproj, --filter-class '*CombatHistoryReplayTests',
--no-restore and unique '-bl:/tmp/h1-focused-{}.binlog'. Capture RED/GREEN logs in /tmp/h1-*.log.
Root runs full build/suite/format/boundary after freeze; do not duplicate. Scoped format allowed.
No unrelatedrefactor/fixtureschurn. Return exactfiles/hashes/commands/results/failures/choices/limits.
Before final handoff finish owned processes or explicitly record logs and status; agent-local
exec sessions cannot be transferred by id. No workerprocess left running after final response.
