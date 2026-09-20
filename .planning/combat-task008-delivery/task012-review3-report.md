# Task012 independent review

Review instance: 3 of 3. Reviewer mode. Source/Git read-only; only this report edited. No agents, CCE, prior reviews, aggregate evidence, worker/developer notes, proposal rationale or execution history consulted. Shell calls use login:false; verification processes set CCE_DISABLED=1.

## Findings

No actionable findings. Review covers frozen dormant Task012 implementation, not production Combat activation or durable host transactions.

## Code Assessment

- `CampaignCombatSealedRound.cs:11,42` reconstructs Base through separate trusted010B input/event history and replays Round inputs independently of retained event.input. Raw event grammar is checked before trusted input indexing. Events and paid roots must equal derived canonical bytes; matching a hash alone is insufficient.
- `CampaignCombatSealedRound.cs:161` requires Prepared at step5, five step receipts and two sealed role slots. Replayed Force Assignment and empty Anti-Armor prove causal suffix. Costs are attacker5/defender3 and ammo10 each; explicit after-CP≤10 guard precedes ordinary-spend helper's broader ceiling. Costs are role ordered regardless of faction or seal order.
- `CampaignCombatSealedRound.cs:213` builds new World/history/target-use, then one version/prefix/receipt result. Original World, pre-use Base, TOE, location, operational provenance, Cohesion and RNG remain unchanged. Commit stays step5 and not closed. No partial mutation escapes candidate construction.
- `CampaignCombatSealedRoundCodec.cs:37,128` separates paid typed World serialization from initial eligibility. Serialization derives cost fields from typed state every time; non-cost World changes reject. State restoration replays original authenticated eligibility before comparing paid bytes. Arbitrary record-with values do not become trusted authority merely because they serialize.
- `CampaignCombatSealedRound.cs:94` recovers exact command/owner receipt before committed/depleted-state gates; malformed primitive/context/actor/field combinations still reject first. Earlier accepted commands remain recoverable after commit. Returned event buffers and collection views are owned.
- `CombatCommitTests.cs:14,48` compares four final committed events/states to retained literals, completing58 events/68 cuts across ten traces. `CombatSealsTests` retains original54/64 assertions. New tests also cover cumulative5/7→10/10, no RNG, replay forgeries, raw-before-context ordering, ownership, early/cancelled/stale rejection and paid World cache drift.

## Plan Review

Canonical `docs/design/combat-cycle-implementation-plan.md:897` Task012 criteria met within five primary files; existing frozen grammar/schema/fixture remain unchanged. No new transport/public API or authority owner introduced. Bounded plan ordering remains011→012→013; this slice does not close Checkpoint F because result/cursor publication remains013.

Cost-resolution design's pre-use/current-state distinction and opportunity design's directional unit history plus distinct segment target use are implemented. Paid ammunition0 restores correctly without asserting a fresh eligible assault or applying surrender. README/roadmap correctly leave implementation/verification underway pending initiating owner's acceptance; tech-design explicitly distinguishes local candidate atomicity from host publication. Canonical plan's next-task wording remains acceptance bookkeeping, not an unsupported completion claim.

Task012 failed-publication evidence is limited to immutable candidate discard/replay, consistent with dormant Core scope and separate HOST-PUB-001 obligation. `CombatCommitTests.cs:66` does not execute a storage transaction, inject a host failure or prove process durability. No credit assigned for those properties.

## Author-Claim Reconciliation

Author/checks packets read only after preliminary ledger below was persisted.

| Claim | Inspected evidence | Status / consequence |
| --- | --- | --- |
| Five primary files; unchanged contract | Base diff, source manifest, schema and oracle | Confirmed; scope bounded. |
| Independent predecessor/Round replay protects commitment | AuthenticateBase, Replay, raw grammar and provenance tests | Confirmed; event.input is not authority. |
| Both costs/history/use form one candidate, no RNG or structural advance | Commit branch/fold, four literal commits | Confirmed for Core; no host publication claim accepted. |
| Original eligibility supports depleted current World and exact retries | WriteWorld/ReadState, all-command retries, typed World mutation tests | Confirmed. |
|58 events/68 cuts, retained54/64, CP5/7 boundary | Focused17 tests independently rebuilt and passed | Confirmed. |
| Worker RED, historical timings, format, shared55 | Checks packet only; independent run uses broader shared84 | Historical claims not independently verified or substituted for reviewer results. |
| Root full2301, Boundary81, remote CI success | Checks packet only | Unverified by reviewer; owner retains aggregate gate/acceptance responsibility. |
| Publication/result/settlement/positive history/Snapshot12 excluded | API boundaries, canonical plan, updated tech-design | Confirmed; explicit residual integration gates. |

## Verification Performed

Working directory `/Users/dsteele/repos/sandtable`. All shell invocations `login:false`; .NET build/test/shutdown use approved IPC escalation. No `--disable-build-servers` passed to dotnet test. SDK resolved10.0.400 under global.json latestFeature; SDK-style net10.0, native MTP, xUnit v3 runner.

Exact commands and outcomes:

```sh
shasum -a 256 -c .planning/combat-task008-delivery/task012-source.sha256
env CCE_DISABLED=1 dotnet --version
env CCE_DISABLED=1 dotnet build tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --no-incremental --disable-build-servers -p:UseSharedCompilation=false -m:1 '-bl:/tmp/task012-review3-build-{}.binlog'
env CCE_DISABLED=1 dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --no-restore --filter-class '*CombatCommitTests' '*CombatSealsTests' '-bl:/tmp/task012-review3-focused-{}.binlog'
env CCE_DISABLED=1 dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --no-restore --filter-class '*CombatWorldTests' '*CombatIdentityTests' '*CombatStepsTests' '*CombatStepsHistoryReplayTests' '*CombatWorldInitialCodecTests' '*CombatSelectedRulesTests' '-bl:/tmp/task012-review3-regressions-{}.binlog'
env CCE_DISABLED=1 python3 -B docs/specs/verify-combat-sealed-round-v2.py
git diff --check 6f9b5013ed5ab5f9fdfc3c5b7e6cac064a809405 -- src/Cna.Core/Campaigns/CampaignCombatSealedRound.cs src/Cna.Core/Campaigns/CampaignCombatSealedRoundCodec.cs src/Cna.Core/Campaigns/CampaignCombatSealedRoundModels.cs tests/Cna.Core.Tests/Campaigns/CombatCommitTests.cs tests/Cna.Core.Tests/Campaigns/CombatSealsTests.cs README.md tech-design.md docs/roadmap/pre-alpha-roadmap.md
env CCE_DISABLED=1 dotnet build-server shutdown
```

All exit0. Source hashes checked twice, five/five match. Build8.62s, zero warnings/errors. Focused17 passed/0 failed/0 skipped,9.005s total. Shared84 passed/0 failed/0 skipped,50.424s total. Oracle:12 semantic groups,10 traces,68 cuts,610 replay mutations,340 raw rejects,288 clock comparisons/retries,480 lifecycle retries,30 invalid proposals. Scoped diff check clean. Shutdown confirmed both MSBuild/compiler servers closed; process inspection found no remaining .NET test/build or oracle processes.

Unique binlogs verified present:

- `/tmp/task012-review3-build-20260920-112634--30157--sve073.binlog`
- `/tmp/task012-review3-focused-20260920-112701--30211--vEZQ3l-dotnet-test.binlog`
- `/tmp/task012-review3-regressions-20260920-112743--30251--0E2Q7H-dotnet-test.binlog`

Final HEAD unchanged `8fcf8b9099909d970bfa942cf6dda869de8af38e`. Navigation-doc SHA256:

```text
5993debf8f77ecc2ada9964dff7966841e205e2fb0e888da76d3bc559860704a  README.md
7e2a77898a9b1fb80bdfdcd7e45d6f6d693e87ba90c2bea3fb8e9b9288d2dd57  docs/roadmap/pre-alpha-roadmap.md
6b619f787bd547f07e4c9c0a5b8acf2d9cc8364ad79c2a4c5d8d4b04f107f502  tech-design.md
```

## Open Questions And Residual Risks

No unresolved question blocks this bounded slice. Synthetic trusted Boundary is deliberately not actual creation-to-positive-Combat history. Durable publication, host lost acknowledgement/crash recovery, result/settlement continuation, broader forces, public fog-safe projection and Snapshot12 composition remain unproved here. Reviewer did not run full solution suite, full format, Boundary trait gate or remote CI; those belong to initiating owner's aggregate acceptance. Verification generated ordinary build/test artifacts and unique /tmp binlogs; no source/Git changes.

## Verdict

**Ready** — frozen dormant Task012 scope.

## Recommended Next Actions

Return report to initiating owner for acceptance against its remaining gates. Review instance3of3 exhausted; no further review, fixes, commits, agents or workstreams started.

## Preliminary blind ledger — persisted before author/checks packets

Target: branch `codex/combat-task008-reaction-lifecycle`; base `6f9b5013ed5ab5f9fdfc3c5b7e6cac064a809405`; observed HEAD `8fcf8b9099909d970bfa942cf6dda869de8af38e`. All five `task012-source.sha256` entries match. Reviewed current README, roadmap and tech-design delta. Existing dirty execution/checks/dev-notes files excluded from blind pass. Source already committed without pin drift.

Blind authorities: canonical implementation plan Task012/Checkpoint E–F and HOST-PUB-001 distinction; sealed-round-v2 spec/schema/oracle; cost-resolution and opportunity-identity designs. Inspected five pinned files, selection boundary authentication, positive-profile certification and shared spending implementation.

No actionable defect identified in blind pass. Provisional concerns checked:

- Commit requires Prepared, five structural receipts, both seals and exact independently replayed predecessor; role order determines CP5/3, each ammunition10→0. Shared ordinary spending ceiling15 is narrowed to10 before charge. Integer cumulative5/7 boundary has synthetic predecessor rebuilt through real transitions, not a forged state shortcut.
- Original Base/World remains eligibility-at-use evidence; current typed World owns paid balances. Serialization derives permitted cost fields each time and rejects unrelated typed changes; persisted paid roots require exact causal replay. Rehashed event/input testimony cannot authenticate itself.
- Four new commit events/cuts complete frozen58 events/68 cuts across ten traces; original54/64 regression assertions remain. Both factions and both seal orders exercised. Commit retains step5/open structural state and unchanged RNG.
- Duplicate command/owner recovery precedes committed-state and clock gates. Fresh malformed/foreign/stale/early/cancelled commands reject. Returned buffers and history/target collections are owned.
- Discard/retry tests demonstrate immutable Core candidate semantics only. No actual durable publication transaction, process crash or host acknowledgement ambiguity is proved. Canonical plan preserves that separate gate; docs explicitly state limitation.

Independent verification at ledger time: SDK10.0.400; nonincremental Core.Tests rebuild passed, zero warnings/errors; CombatCommit/CombatSeals17/17 passed. Oracle running; shared regressions pending. Author/checks packets not yet read. Final verdict pending checks and claim reconciliation.
