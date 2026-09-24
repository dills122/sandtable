# Task018A verification

Baseline54029a502859a270cea0846e99fc397ea9b00d54 (merged PR141). Branch codex/combat-cycle-movement-plan.
Two new source/test paths plus approved delivery/canonical plans; README/roadmap/tech/naming administrative summaries. Unrelated .serena/ and old handoff excluded.

## Executed checks

- Initial RED: one compiled acceptance test failed at explicit unimplemented assessment. /tmp/task018a-red.log.
- Expanded tests initially failed compilation because public xUnit method exposed internal enum; changed test parameters to int, retaining typed enum casts at API boundary. No runtime contract changed.
- Expanded RED:19 compiled cases failed against stubs. /tmp/task018a-red-compiling.log.
- Initial GREEN:48 new/existing World cases passed. /tmp/task018a-green.log.
- Final focused: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatCycleMovementRulesTests' --filter-class '*CombatWorldTests' /bl:/tmp/task018a-focused-{}.binlog`:49passed,0failed/skipped,1.302s (20 new rules cases +29 existing World cases).
- Build: `dotnet build Sandtable.slnx --no-restore /bl:/tmp/task018a-build-{}.binlog`:0warnings/errors,4.74s.
- Format: `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: passed.
- Boundary: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace' /bl:/tmp/task018a-boundary-{}.binlog`:81passed,0failed/skipped,12.557s.
- Frozen oracle: `python3 -B docs/specs/verify-combat-ordinary-movement-v1.py`: PASS8cases/both sides,36cuts,486mutations,120raw rejections,630arithmetic coordinates,14atomic/overflow guards. No fixture changes.
- Independent review1: Ready with non-blocking follow-ups, no actionable findings; full regression remains delivery gate. See review-report.md.
- Full regression: `dotnet test --solution Sandtable.slnx --no-build /bl:/tmp/task018a-full-{}.binlog`:2,397passed,0failed/skipped,11m09.989s. Log /tmp/task018a-full.log.
- Reviewed source/test SHA256 unchanged; independent review delivery condition satisfied. Local acceptance complete; no GitHub CI claim.

## Reviewed file hashes

- `src/Cna.Core/Campaigns/CampaignCombatCycleMovementRules.cs`: `1326fa848b8588190c9ee13fef41831b3d0cdac89f23db504ae3f2046add8e41`
- `tests/Cna.Core.Tests/Campaigns/CombatCycleMovementRulesTests.cs`: `dd118f26e1f64810fa0843d52e4170af171ad81d16787908bffacccf69d21efd`

## Proof limits

Literal and rejection tests cover typed rule behavior only; existing frozen oracle is separate contract evidence. No native movement events, World updates, receipt ending of relationships, current campaign authority, Reserve history/exception admission, repeat or public activation. All 017–019 parents remain open. Later018B must audit historical C3c/current Result2 boundary before claiming golden parity.
