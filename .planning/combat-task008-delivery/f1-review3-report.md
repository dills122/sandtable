# F1 independent review

Review instance: 3 of 3.

## Preliminary ledger — before author packet

Verified HEAD/base b9cb26f2c4f2b46b8ac8f369e0e773f5f7e2ed80 and branch codex/combat-task008-reaction-trigger. Five primary file SHA-256 checks pass. Diff has four new C# files, fixture-link csproj change, synchronized project docs and F/H execution-plan refinement; no existing runtime implementation changed.

Read canonical trigger spec/schema, source, tests, E1 predecessor and F/H plan before opening author/evidence packets. CCE broad discovery inadvertently returned short leading snippets of author/evidence and prior review2 report; no full prior reports or conclusions read. Blind pass therefore has this limited retrieval contamination. No session recall used because historical review decisions could contaminate independence.

Preliminary assessment: no actionable defect found. Replay reconstructs exact one-move E1 prefix; fresh and duplicate commands require original owner, full identity and version12; generated effects are compared byte-for-byte. Actual World updates moved unit/representation and CP2→4. Persisted identities use committed version13 and independent representation; same phasing route suspended with null reactor route. E1 remains unchanged and rejects positive adjacency. Tests exercise both owners, all eight fixture artifacts, exact retry, canonical mutations, re-signed event/cache attempts, buffer independence and resource-World forgery.

Questions to settle before verdict: confirm source oracle adjacency/eligibility matches bounded fixed Content7 assumptions; confirm test totals against retained logs; reconcile F3–F6 and progressive H with original checkpoint D and selected authority composition. Typed wrapper constructors are internal and not general admission; arbitrary forged metadata must not be mistaken for supported restoration. No request to broaden this packet.

## Findings

No actionable findings. Closed profile matches canonical oracle: adjacency discovery precedes eligibility; ordinary E1 history already enforces Normal/NONE and symbolic Movement; exact command comparison pins owner, rear-to-assault route and version12. ReadState rejects pretrigger-only state just as oracle does. New internal models leave old materialized-position and Move2/3 contracts untouched.

## Plan Review

F1 has correct causal predecessor: exactly first E1 move, not G2 terminal. F2 lifecycle and F3 direct closure branch from F1; F4 fallback and F5 second move branch from first participant-move cut; F6 completion follows F5. Authority-composition selected table explicitly requires six direct-closure, four active-fallback and two second-move-completion traces, so F3–F6 are necessary retained scope. No unsupported expansion found.

Progressive H is consistent with existing Checkpoint D wording at docs/design/combat-cycle-implementation-plan.md:839 and Task009 dependency on005–008: restore implemented handler cuts first, then extend through later lifecycle owners. Retained selected 28-trace target, fresh-admission-disabled restore, and HOST-PUB-001 stay open. This avoids a later-owner dependency inversion without declaring parent completion. Five primary files preserve bounded delivery size. F2 feasibility remains a later explicit check, with causal split permitted by plan.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Full creation-rooted first E1 move required | ReactionTrigger.Replay/Initial, E1 Replay/Initial, predecessor negatives | Confirmed; no synthetic G2 state admitted |
| Actual return Move4 produces CP4/version13 and one window | Emit, canonical schema/oracle, both-owner golden assertions | Confirmed |
| Persisted IDs and suspended route preserved | CampaignReactionIdentity calls, CheckIdentities, route/flow assertions | Confirmed; public handle claims absent |
| Strict raw/event/cache/retry boundaries | Replay re-emission equality, ReadState equality, Authorize and mutation tests | Confirmed for bounded profile |
| Current World is guarded rather than delegated to E1 writer | ExpectedWorld, WriteWorld, typed opponent-CP forgery test | Confirmed; E1 positive-adjacency rejection unchanged |
| Eight artifacts / four cuts | Two tests compare prefix/input/event/state hashes and lengths; pretrigger/posttrigger replay paths | Confirmed |
| Focused15, build zero warnings/errors, full2,051 | Retained final/build/suite logs inspected | Confirmed as retained execution evidence; not independently rerun |
| Format passes | Empty retained format log plus lead exit0 record | Lead-reported success, no independent exit status available |
| F1 closes broader runtime or restore scope | Author explicitly excludes participants, closure, public activation, general Snapshot12 and publication | No overclaim |

Author packet includes stale earlier pending wording but subsequent evidence explicitly supplies final integration results. This is chronological evidence, not missing execution.

## Verification Performed

Reviewer executed:

- `git status --short`, `git branch --show-current`, `git rev-parse HEAD`: expected working-tree target/base/branch.
- `shasum -a 256 -c .planning/combat-task008-delivery/f1-source.sha256`: all five primary files OK, at start and end of review.
- `git diff --check`: exit0.
- Inspected full new source and tests, project fixture-link diff, supporting documentation diffs, canonical trigger spec/schema, oracle initial/emit/read_state/trace/golden behavior, E1 replay/guards, selected authority-composition trace table, existing Checkpoint D and Task009 dependency.
- Read `/tmp/f1-reaction-baseline.log`: CMB-IRT PASS, 2 traces, 2 triggers, 4 cuts, 2 retries, 270 mutations, 60 raw rejections, 30 boundaries, 11 source pins. No concern warranted repeating unchanged baseline.
- Read `/tmp/f1-final.log`: 15 succeeded, 0 failed/skipped.
- Read `/tmp/f1-build.log`: build succeeded, 0 warnings/errors.
- Read `/tmp/f1-suite.log`: 2,051 succeeded, 0 failed/skipped, 3m25s719ms.
- Read `/tmp/f1-format.log`: no output; lead records exit0.

Retained execution commands, not rerun by reviewer:

`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatReactionTriggerTests' --filter-class '*CombatInheritedMovementTests' --no-restore '-bl:/tmp/f1-final-{}.binlog'`

`dotnet build Sandtable.slnx --no-restore '-bl:/tmp/f1-build-{}.binlog'`

`dotnet format Sandtable.slnx --verify-no-changes --no-restore`

`dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/f1-suite-{}.binlog'`

`PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-reaction-trigger-v1.py`

No builds, code changes, commits, delegation, or runtime activation performed. Only this report written.

## Open Questions And Residual Risks

No blocking open question. Bounded World serialization duplicates layout and replays short trusted history; later extension must preserve strict equality and avoid silently omitting newly supported fields. Internal typed objects are not arbitrary caller admission. Tests and review establish selected fixed Content7 profile, not generalized eligibility, multiple opportunities, positive vehicles, participant closure, public handles, durable publication, or whole28 runtime restoration. Review independence has limited CCE snippet contamination disclosed above; no prior verdict used.

## Verdict

Ready.

## Recommended Next Actions

Lead may accept F1 and record final review/status evidence. Continue F2–F6 and H only through existing dependency gates. Review instance3of3 complete; no further review initiated here.
