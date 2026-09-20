# Task014 independent review

Review instance: 2 of 3. Reviewer mode. Base `6ee5980`; branch `codex/combat-task008-reaction-lifecycle`; observed head `e7c9e8f81b6ac5230dc1340ecbc54cbe5c0395ba`.

## Findings

No actionable findings. Frozen implementation satisfies bounded Task014 loss/retreat scope. Independent focused and shared checks passed. This assessment does not close remaining Checkpoint G or runtime publication gates.

## Preliminary blind ledger — persisted before author/checks packets

Six entries in `task014-source.sha256` verified. Source committed without pin changes. README, `docs/roadmap/pre-alpha-roadmap.md`, and tech-design inspected as current documentation. Initial dirty state: author-side checks and evidence packets only. No CCE, previous review, aggregate evidence, worker/dev notes, proposal rationale, or execution history consulted. No agents launched. Only this report written.

Inspected canonical Task014/Checkpoint G and neighboring dependency gates; Result2 specification, ordered schema and oracle transition; six frozen source/test paths and relevant shared spending/settlement validators. No actionable code defect identified on blind inspection.

| Concern | Blind evidence / preliminary disposition |
| --- | --- |
| Evidence strength | New test checks 144 cumulative literal event bytes and 176 hash cuts across 32 contexts; intermediate states are not independent literal JSON snapshots. Execution pending. |
| Clock isolation | Opening takes Config1 retreat budget, local opening/high-water; audit maximum never gates it. Owner, shape and context precede fallback. Retry receipt lookup precedes clock handling; accepted intent leaves no window for timers. |
| Loss/retreat effects | Both roles derive from original paid elements. Refusal modifier, asymmetric rounding, capture partition, 30% threshold, pre-loss lot origin, content route, representation movement, CP10→11/incremental DP and evacuation-only RP present. |
| Authority | Replay rebuilds C3a/Round predecessor before Result transitions; typed projection recomputes receipts and compares complete World equality. Hashed constructor validates all factory/append paths. No world cache added. |
| Regression and scope | Original 32-event/64-hash Task013 proof retained. Narrow future-family assertion moves boundary through retreat; new tests reject post-retreat continuation. Custody, relationships, closure, CA completion and Reserve Release stay out of scope. |
| Atomicity/ownership | Candidate discard and exact retry tests demonstrate immutable Core behavior, not host transaction publication. Readback is replay-bound; collections and returned bytes owned. |
| Plan | Bounded Task014 implementation fits checkpoint order. Six physical paths versus nominal 3–5 include narrow prior-test adjustment. Entire Checkpoint G still requires015/016; positive campaign history, Snapshot/public/host gates remain open. |

Pending: author-claim reconciliation, focused/native-MTP and shared checks, oracle execution, final pin/process check and single verdict. No readiness verdict issued at preliminary stage.

## Code Review

- `CampaignCombatResolution.cs:129` reconstructs independently supplied predecessor and paid Round history before Result replay. Original seals, five steps, commitment, attack/target history and open round remain prerequisites. Transition validates context and primitive/command/actor shape before effects; replay compares complete event bytes. No caller-supplied state enters Apply authority.
- `CampaignCombatResolution.cs:176` recovers exact command/actor receipt before timing checks; no new charge, loss, cursor draw or deadline. Window removal protects accepted intent from late callbacks. Required opening uses original creation Config1 at `:238`; independent instant initializes local timing even below audit high-water. Lost clock, overflow opening, exclusive deadline and system fallback match canonical oracle.
- `CampaignCombatLossRetreat.cs:19` derives simultaneous role losses from immutable pre-loss participants; defender refusal modifier, attacker ceiling/defender floor rounding, capture partition and 30% threshold remain exact. `Project` at `:44` reconstructs causal receipt chain before applying effects. Pending captive lots retain original victim location, including attacker-capture/defender-retreat branches.
- Retreat route at `CampaignCombatLossRetreat.cs:116` comes from authenticated content graph and defender supply anchor; geometry requires actual evacuation away from attacker. Element and representation move together. Mandatory spending reuses shared incremental excess-DP logic and permits CP10→11; attacker RP applies only after completed retreat and caps at ten. Constants/singleton assumptions match certified full10 infantry/Cohesion0 scope, not general combat support.
- `CampaignCombatObligations.cs:260` append methods enforce stage and private constructor validates domain-hashed commitment/result/settlement identity at `:297`, plus existing paid participants and receipt chain. Legacy public constructor behavior remains intact. Projection rejects forged routes even if append-model syntax is valid.
- `CampaignCombatResolutionCodec.cs:103` checks status/window/receipt stage, derives allowed World and compares full typed equality before serializing. `CampaignWorldV7.cs:157` equality covers every world collection, not merely touched fields. ReadState requires canonical raw shape before history and compares full reconstructed bytes. No cache authority introduced.
- New tests cover ownership, locally rehashed events, forged world/RNG, raw-before-context rejection, all retained retries/discards and immutable append stages. Task013's 32 literal resolve events/64 hashes and original draw/payment tests remain; only obsolete later-family cutoff changes. Task014's terminal `retreat` state rejects015/016 onward. Discard tests prove Core candidate immutability, not durable host transactions.

## Plan Review

Canonical `docs/design/combat-cycle-implementation-plan.md:906` places014 after accepted012/013 and before015 custody/016 relationships and closure. Implementation follows that sequence and covers named014 acceptance criteria: intent/refusal, simultaneous loss/capture conservation, mandatory CP and incremental DP, evacuation-only victory RP and every retained event cut.

Six physical code/test paths versus nominal 3–5: five material implementation/test paths plus narrow existing test cutoff. No contract, host, transport or public-action expansion. Current README/roadmap describe implementation under verification; tech-design describes new dormant responsibilities and later gates. Canonical historical status still says014 next; root owns acceptance/status update after review gate. No false claim that all Checkpoint G or runtime activation is complete.

## Author-Claim Reconciliation

Author/checks packets read only after preliminary ledger above was written.

| Author claim | Evidence inspected | Status / consequence |
| --- | --- | --- |
| 144 literal events/176 hash cuts,32 contexts; original01332/64 retained | `AllRetreatPrefixesMatch144LiteralEventsAnd176StateHashCuts`, original Resolution test and independent17-test run | Confirmed. Cumulative counts include resolve; not176 independently stored JSON states. |
|20 independent openings/56 owner-clock choices | `IndependentRetreatOpeningAndLocalClockMatrixPreserveAcceptedIntent`, engine and oracle | Confirmed; opening0/1000 below audit3000, null/unavailable/overflow, local regressions/deadline equality and retry/timer immunity. |
| Simultaneous losses, original pending-lot location, CP10→11 and actual-retreat RP | Typed projection,32-context conservation test and shared World tests | Confirmed. Cap-ten boundary exercised by shared model tests; Task014 Cohesion0 fixtures cannot reach that cap. |
| Safe hashed evolution and complete World authority | Private constructor, append guards, projection equality, forgery and shared regression tests | Confirmed. Syntax-valid receipts alone cannot bypass causal projection. |
| Core atomicity and ownership, not host durability | Every-cut discard/retry test; readback and copied collections/buffers | Confirmed within dormant Core. No crash/restart or publication transaction tested. |
| Full2318 tests, Boundary81, build/format and exact-head CI green | `task014-checks.md` only | Author-reported; not independently rerun or remote-verified. Not counted as reviewer execution evidence. |
| Positive history, extended Snapshot12, HOST-PUB-001 and activation remain open | Canonical plan/spec, current docs, no host changes | Confirmed. Synthetic boundary/cursor provenance preserved. |

## Verification Performed

All shell calls used `login:false`; .NET test calls used approved escalation for local IPC, native MTP `--project`, cleared existing build via `--no-build`, unique binlogs and no `--disable-build-servers`. SDK observed: `10.0.400`; global runner MTP, SDK-style net10.0, xUnit v3 MTP v2 package.

1. `shasum -a 256 -c .planning/combat-task008-delivery/task014-source.sha256` — all six OK before review and after test runs. Branch/head unchanged.
2. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatLossRetreatTests Cna.Core.Tests.Campaigns.CombatResolutionTests '-bl:/tmp/task014-review2-focused-{}.binlog'` — exit0,17 passed,0 failed,0 skipped;22.777s. Binlog `/tmp/task014-review2-focused-20260920-124015--35661--31TUtJ-dotnet-test.binlog` exists.
3. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatWorldTests Cna.Core.Tests.Campaigns.CombatCommitTests Cna.Core.Tests.Campaigns.CombatSealsTests '-bl:/tmp/task014-review2-shared-{}.binlog'` — exit0,46 passed,0 failed,0 skipped;8.684s. Binlog `/tmp/task014-review2-shared-20260920-124118--35704--BElkaW-dotnet-test.binlog` exists.
4. `python3 -B docs/specs/verify-combat-result-settlement-v2.py` — exit0;10 semantic groups,32 traces,304 cuts,3728 mutations,1360 raw rejects,384 timing cases,200 same-owner prior-time isolation comparisons. These are full reference-contract results, including later families; they do not expand C#014 proof beyond retreat.
5. `git diff --check 6ee5980 -- src/Cna.Core/Campaigns/CampaignCombatResolution.cs src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs src/Cna.Core/Campaigns/CampaignCombatObligations.cs src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs tests/Cna.Core.Tests/Campaigns/CombatLossRetreatTests.cs tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs README.md docs/roadmap/pre-alpha-roadmap.md tech-design.md` — exit0, empty output.
6. Process inspection after tests: no .NET test host, dotnet, MSBuild or compiler server remained; oracle also exited0. All reviewer verification sessions closed. No process termination needed.

Documentation SHA-256 at final check:

```text
824abeb2f9577ed9f230c61998675304ceb7013614e632b69316495529b73f6c  README.md
49b5b726c0772a745dbde478e3d98b45d76b25a152a3c3f9b71771afe6a542b5  docs/roadmap/pre-alpha-roadmap.md
0c4712c23d5cd4a0788055c575fbcc50d49c3a7a1f23b76f0f63796839d11556  tech-design.md
```

## Open Questions And Residual Risks

No blocking question for bounded014. Existing build reused under explicit clearance; no fresh reviewer build, full suite, full format, Boundary or CI run. Runtime activation, authentic positive campaign history, full Snapshot12, custody/relationship/closure and host crash/publication remain outside reviewed deliverable. Route and arithmetic generalization beyond certified singleton geometry/full10/Cohesion0 requires future scope and evidence.

## Verdict

**Ready** — bounded Task014 frozen source and current scope documentation.

## Recommended Next Actions

Return to root for acceptance and dependency/status updates. Root retains review budget and acceptance authority. No fixes, commits, agents, new workstreams or additional reviews created.
