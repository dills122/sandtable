# Task010C independent review

Review instance: 1 of 3. Reviewer mode. Base: fe5664d2c0d4ac56b1db8ef21ef850796d625fea. Explicit working-tree target: task010c-source.sha256 (all three paths verified). Documentation scope: current README.md, docs/roadmap/pre-alpha-roadmap.md, tech-design.md. Source/Git read-only; only this report edited.

## Findings

No actionable findings. Reviewed implementation and canonical plan align; independent focused, shared regression and Python checks pass. No fixes requested.

## Preliminary blind ledger — persisted before author/checks packets

No actionable defect found in first source pass. Runtime checks pending boundary marker.

- Exact G2 cut: HistoryReplay passes precisely one completion event to strict Breakdown reader, then slices predecessor at cursor. Selection re-admission cannot recurse through its own tail.
- Complete suffix: two selection events admitted by existing strict reader; every remaining event passed to six-event traversal reader. Extra, omitted, reordered, duplicate, synthetic and cross-history suffixes reject.
- Projection ownership: Selection/NoAttack use immutable get-only State and copied read-only cumulative receipts. Version/prefix/current position derive from appropriate current state; nested controls remain untouched.
- Four histories: new test visits 118 complete-history prefixes, 36 including G2 and 32 new suffix cuts; checks every event hash/version and frozen Event/Control commitments. No count confused with number of new cuts.
- Snapshot12 existing closed projection switch rejects both new variants; old G2 positive restore tested. Full old 368-vector regression still to execute.
- Plan: canonical Task010 refinement permits this bounded actual010A router integration after010B; no positive history, Prepared011, public activation or publication capability claimed. Docs retain parent010 open.
- Coverage follow-up: verify existing010A command retries and retained-root regressions alongside focused new tests. New test does not itself explicitly retry commands; generic router has no command API.

Blind evidence: bootstrap; canonical implementation-plan Task010 refinement; inherited selection/no-attack specs; scoped diff and complete new test; relevant existing strict readers, certification call boundary, retained-history ownership and Snapshot12 switch. Dispatch read for authorized scope only; linked dev notes/proposal and execution history not opened. No cross-session retrieval, other reviews, worker evidence or agents used.

## Plan review

Canonical implementation plan lines875–884 explicitly separates actual010A, synthetic010B and cumulative010C. Implementation follows ordering and bounded ownership: two production files, one focused test file, three scope docs; no schema/fixture/oracle/Snapshot writer changes. Exact G2 reconstruction trades bounded repeated validation for fewer trusted caches. This is appropriate for selected four-history profile. Parent010 acceptance, remaining reviews, full gates and publication remain root-owned; no completion overclaim in reviewed docs. No material plan gap found.

## Author-claim reconciliation

| Claim | Independent evidence | Status |
| --- | --- | --- |
| Recursive admission terminates at G2 | HistoryReplay.cs:63–79; Certification.Admit requires BreakdownCompletion; predecessor slice excludes suffix | Confirmed |
| All tail consumed or rejected | Strict two-event selection and six-event traversal capacities; complete tail passed at line79 | Confirmed by source; runtime pending |
| Cached cumulative receipts cannot diverge via replacement | HistoryModels.cs:66–94; get-only State; read-only owned arrays; immutable inherited state | Confirmed |
| Frozen controls and four-history cut counts | New tests compare independent fixture hashes/lengths and every retained receipt/version; 32/36/118 assertions | Confirmed by inspection; runtime pending |
| World/RNG/resources unchanged, no Reserve release | New projections wrap strict inherited state; nested Boundary byte equality; existing no-attack transition only appends receipts | Confirmed |
| Snapshot12 rejects new families, retains old roots | Existing SerializeState default JsonException; restore composes through same switch; old368 regression available | Confirmed by source; runtime pending |
| Created owned before list callbacks | Copy precedes Capture; adversarial Count callback test | Confirmed |
| Historical RED/GREEN and scoped format results | Author checks packet only; historical logs deliberately not inspected | Unverified historical claims, not used as independent pass evidence |

## Verification performed

Initial and repeated `shasum -a 256 -c .planning/combat-task008-delivery/task010c-source.sha256`: all three OK. Scoped `git diff --check fe5664d -- src/Cna.Core/Campaigns/CampaignCombatHistoryReplay.cs src/Cna.Core/Campaigns/CampaignCombatHistoryModels.cs README.md docs/roadmap/pre-alpha-roadmap.md tech-design.md`: no diagnostics. Native MTP/xUnit v3 configuration read locally; no .NET started before marker. No external browsing or CCE.

Independent oracle: `python3 -B docs/specs/verify-combat-inherited-no-attack-v1.py` exited0: PASS, four traces/24 events,28 cuts,24 retries,956 mutations,36 raw negatives,208 boundaries,7483 fixture bytes,seven source pins. Process completed.

During review HEAD advanced to `9b999a7df5d1a0f54b7ca4ffcf045197734169a7`; all three supplied SHA256 pins rechecked OK. Explicit target remains identical despite root commit.

Boundary marker observed before first .NET call. `dotnet --version`:10.0.400. Focused command (login:false, approved local IPC):

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatStepsHistoryReplayTests' '-bl:/tmp/task010c-review1-focused-{}.binlog'
```

Exit0;7 passed,0 failed,0 skipped;44.161s. Binlog: `/tmp/task010c-review1-focused-20260920-100006--23427--iR+lZq-dotnet-test.binlog` (existence verified). This independently confirms runtime claims concerning suffix routing,32 new cuts/36 with G2/118 prefix visits, forged tails, ownership and new-family Snapshot12 rejection.

Shared regression command:

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatInheritedStepsTests' '*CombatHistoryReplayTests' '*CombatMovementHistoryReplayTests' '*CombatReactionHistoryReplayTests' '*CombatInheritedSnapshotTests' '-bl:/tmp/task010c-review1-regression-{}.binlog'
```

Exit0;147 passed,0 failed,0 skipped;2m26.170s. Binlog: `/tmp/task010c-review1-regression-20260920-100102--23456--Q__Bpp-dotnet-test.binlog` (existence verified). Covers actual inherited retries and controls, pre-cycle/Movement/Reaction history routing and Snapshot12 exact368 roots/286 histories. All runtime-pending claim-ledger entries above are now confirmed by these independent runs; preliminary ledger deliberately preserved as recorded before author packets.

Final three-path SHA256 recheck: all OK at HEAD `9b999a7df5d1a0f54b7ca4ffcf045197734169a7`. Scoped diff check including newly committed test: exit0. Reviewed source identical to initial working-tree target. Working-tree changes outside scope belong to root and were neither read nor altered.

## Open questions and residual risks

- Tests deliberately used existing Debug net10.0/arm64 binaries with `--no-build`; no independent build, full solution gate or formatting run performed. Root owns build/full gate evidence and final acceptance. Focused discovery and execution confirmed all seven new tests present.
- Author historical RED/ownership RED/intermediate correction/scoped format claims not independently reproduced or treated as passing reviewer evidence. Root checks packet was read only after persisted preliminary ledger; no aggregate evidence, prior review, worker/devnotes, proposal rationale or execution history read.
- Bounded repeated predecessor validation remains a performance cost; no broad performance/host/publication claim made. Snapshot12 new families intentionally unsupported. Positive history and Prepared011 remain excluded.
- No blockers or unresolved source questions found within pinned scope.

## Process closure

All reviewer-launched Python/wait processes and both test commands completed. Initial sandbox `ps` attempt denied; approved read-only process listing then confirmed test PIDs23427/23456 and Cna.Core.Tests absent. No review-owned test child remains; no unrelated process stopped. No source/Git edits, commits, agents or extra review instances.

## Verdict

Ready.

## Recommended next actions

Root reconciles this instance1of3 report with its owned gates and remaining allocated reviews. This verdict covers pinned010C implementation and plan only; parent010 acceptance remains root-owned.
