# F4 independent review

Review instance: 3 of 3.

## Preliminary blind ledger

Recorded before author packet, retained execution evidence, or session recall. Bootstrap and task assignment contained expected scope and claimed test totals; these are not independent evidence. CCE canonical-packet discovery inadvertently returned snippets of older contract evidence hash files and F1 dispatch; none was a previous F4 report or author rationale. No previous review report read.

- Scope confirmed: branch `codex/combat-task008-reaction-lifecycle`, HEAD/base `f0cb5ca9aa256eb330a46c0ff86fe8f326174d3c`; five frozen primary file hashes match; supporting documentation edits only.
- Exact F4 source, tests, canonical spec/schema and oracle transition/validation logic inspected. Actual F2 history reconstruction gates first participant move, sole active opportunity, open reactor route and remaining move. Close records reason-specific stop; resolution retains exact stop and restores phasing continuation. No actionable defect identified.
- Tests independently calculate public/action/stop identities, receipts and prefix, compare 32 frozen artifacts, reject field/actor/history/fork/cache corruption, and verify unchanged material and detached buffers.
- Plan dependencies and scope match F2 first-move branch; F5/F6, full Snapshot12 restore, public activation and host scheduling remain explicit future gates.
- Remaining verification: retained log provenance/results, canonical source pins, author claims, required session recall. Golden success not assumed from source inspection.

## Findings

No actionable findings. Frozen F4 delta preserves authority boundary and mandatory stop transition.

`CampaignCombatReactionFallback.Replay` reconstructs complete F2 predecessor from actual creation/preamble/Weather/stage/Reserve/Movement/trigger/participant history. Caller state never substitutes for that reconstruction. Exactly one participant event and at most two fallback events are admitted. Replay re-emits complete canonical events and compares bytes, so a valid receipt alone cannot authorize changed material, lineage, or transition content.

`Apply` authorizes System actor and creation/cycle before consulting accepted inputs. Structural equality covers full command identity, kind, public handle and actor on retries. Alternate reason cannot become a sequential event or duplicate. `Emit` closes the actual reactor route into `ReactorStopClosed`; resolution consumes that recorded stop and resumes its retained phasing continuation. State delegates immutable material to predecessor; writer uses predecessor's whole-World guard. Code introduces no authoritative remote I/O or public registration.

## Plan Review

Reviewed canonical active-fallback spec, ordered schema, fixture, oracle transition/validation logic, Task008 execution index and F execution refinement, execution ledger, and changed README/roadmap/naming/tech documentation. Dependency is F2 first participant move; F3 is delivery order only. Five primary files respect bounded delivery rule. Four owner/reason forks cover eight events, twelve cuts and thirty-two artifacts; no contract/protobuf or accepted predecessor source changes occur.

Plan correctly keeps F5/F6, parent F, full Snapshot12 recovery H, runtime/public registration, positive vehicles, multiple opportunities, host clocks and HOST-PUB-001 separate. Dormant Core delivery needs no storage migration or live rollback plan. Documentation still marks F4 in progress pending lead acceptance; routine status closeout belongs to lead after review, not an implementation defect.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Actual first-move authority14; close15 then mandatory resolution16 | Replay admission, Command guards, Emit; StopsPreserveMaterialAndResumeOnlyAfterMandatoryResolution | Confirmed; no early phasing resume |
| Reason-specific stop, original route, empty checks/lots and unchanged RNG | Emit, SerializeEvent, canonical oracle `_emit`, independent stop hash assertions | Confirmed |
| Both public capabilities differ from persisted IDs; actor before retry | Authorize before Apply retry loop; independent public/action calculations and both-history actor negatives | Confirmed |
| World/resources/tracks/progress stay unchanged | Delegated predecessor state, WriteWorld guard, full unaffected-field comparison and CP/location checks | Confirmed |
| Strict canonical events/caches, forged history and fork rejection | Full re-emission equality; EveryEventAndCacheLeafRejectsRawAndResigned, EveryCommandFieldActorRetryAndCompetingForkRejects, predecessor/buffer/capacity tests | Confirmed |
| Four traces/eight events/twelve cuts/thirty-two artifacts | Fixture inventory, 17 independently checked pins, golden test source, baseline log | Confirmed |
| Focused29/full2080/boundary81 and build pass | Retained logs inspected directly | Confirmed as retained execution evidence, not reviewer reruns |
| Full format passed | Evidence ledger records exit0; retained output file empty | Consistent; exit status not independently recoverable from empty log alone |
| No public activation, general Snapshot12 or durable publication claim | Internal classes, unchanged public paths, explicit old-reader rejection tests and plan gates | Confirmed; larger gates remain open |

Required `session_recall("CampaignCombatReactionFallback authority retained history")` occurred after saved blind ledger. It returned older project decisions, not new implementation evidence. Author/evidence read subsequently disclosed previous round verdicts in evidence ledger; previous reports themselves were not read and verdicts did not substitute for this review.

## Verification Performed

Reviewer executed read-only scope checks: `git status --short`, `git branch --show-current`, `git rev-parse HEAD`, scoped `git diff`, `git diff --check`, and twice `shasum -a 256 -c .planning/combat-task008-delivery/f4-source.sha256`. Correct branch/base, only five primary paths plus listed supporting docs, no whitespace errors, all five hashes match. Inline Python independently hashed all 17 fixture source pins and counted four cases/32 golden artifacts.

Inspected retained executions and their exact recorded commands:

- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-reaction-active-fallback-v1.py`: `/tmp/f4-reaction-baseline.log` reports PASS, 4 traces, 8 events, 12 cuts, 8 retries, 612 mutations, 200 raw negatives, 132 boundaries, 17 source pins.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatReactionFallbackTests' '*CombatReactionLifecycleTests' '*CombatReactionClosureTests' --no-restore '-bl:/tmp/f4-focused-{}.binlog'`: `/tmp/f4-focused.log` shows 29 passed, zero failed/skipped.
- `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/f4-build-{}.binlog'`: `/tmp/f4-build.log` shows success, zero warnings/errors.
- `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/f4-suite-{}.binlog'`: `/tmp/f4-suite.log` shows 2,080 passed, zero failed/skipped.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace' '-bl:/tmp/f4-boundary-{}.binlog'`: `/tmp/f4-boundary.log` shows 81 passed, zero failed/skipped.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: ledger records exit0; `/tmp/f4-format-full.log` is empty, as expected for successful verification.

No builds, tests, oracle reruns, code changes, Git mutations, delegation or further review instances performed. Only this report written.

## Open Questions And Residual Risks

No blocking open question. Review relies on retained local execution logs and frozen source manifest for test execution provenance; it does not independently rerun the binaries. Existing narrow ordinary-infantry profile and two-event reconstruction bound are intentional. Repeated predecessor replay and duplicated canonical writer shape carry maintenance cost but neither exposes a concrete defect at this bounded scale. Public scheduling, broader gameplay, durable recovery and activation remain unproved outside F4's stated scope.

## Verdict

**Ready** for frozen F4 scope. No P0–P3 findings; implementation and plan both support bounded acceptance.

## Recommended Next Actions

Lead may close F4 evidence/status and publish through existing delivery process. Keep F5/F6/H and HOST-PUB-001 open. Review instance 3 of 3 complete; no further instance or conditional experiment initiated by reviewer.
