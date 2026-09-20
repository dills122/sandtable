# Task014 independent review

Review instance: 3 of 3.

## Preliminary ledger — persisted before author/checks packets

Frozen target: branch codex/combat-task008-reaction-lifecycle; base 6ee5980; observed HEAD e7c9e8f81b6ac5230dc1340ecbc54cbe5c0395ba. All six task014-source.sha256 pins verified. Current README.md, docs/roadmap/pre-alpha-roadmap.md and tech-design.md included. Initial dirty paths: task014-checks.md and task014-evidence.md; neither read during blind pass. CCE disabled throughout. No prior reviews, aggregate evidence, worker/dev notes, proposal rationale or execution history read. Filename inventory exposed names only; no broad content search.

Blind evidence: canonical implementation plan Task013–016/checkpoint G; Result2 spec and oracle transition/projection; six pinned files, relevant paid-settlement constructor context and current doc diff.

No actionable defect identified in preliminary pass. Review concerns retained for verification:

- Confirm independent rebuild and focused tests, including unchanged Task013 result/RNG proof and shared settlement/spending regressions.
- Literal coverage is cumulative 144 events and 176 state HASH cuts over 32 contexts, not literal intermediate JSON or 144 newly introduced transitions.
- Own-window Config1 budget/local clock is independent of audit high-water; shape/owner checks precede fallback; retries precede clock checks; stale timers cannot override accepted intent.
- Simultaneous losses use frozen paid participants; captured TOE splits loss; pending lots preserve original location. Actual retreat projects content-derived path, representation, mandatory CP and incremental DP before capped victory RP.
- Private hashed constructor validates domain-derived identity on every append; full typed World comparison retained. Fixed TOE10, Cohesion0, singleton and integral paid CP assumptions are explicitly admitted scope, not general gameplay support.
- Current tests establish pure Core discard/replay and copy ownership, not host persistence atomicity or process restart. Custody/relationships/closure/CA remain rejected; checkpoint G as a whole remains incomplete.
- Capped RP implementation inspected, but admitted Cohesion0 corpus cannot itself exercise saturation at ten; check relevant shared evidence without expanding scope.

Verification not yet executed. Author/checks packets not yet read. Verdict pending.

## Findings

No actionable findings. Final review covers frozen Task014 dormant Core scope only. No source/Git changes, fixes, commits, agents or additional review instances.

## Code and plan review

- `CampaignCombatResolution.cs:136–155` authenticates paid C3a/Round history before causal Result replay; complete event bytes must match independent inputs. No caller-provided state becomes Apply authority.
- `CampaignCombatResolution.cs:166–218` validates command binding, actor, shape and owner before fallback; exact retained-command retries recover original bytes before clocks/version gates. Accepted intent clears window; later timers cannot replace it.
- `CampaignCombatResolution.cs:234–271` uses original creation Config1 for independent mandatory opening, checked deadline capacity and local timing. Audit maximum remains separate. Unsupported custody, relationships and closure stop after retreat.
- `CampaignCombatLossRetreat.cs:8–43` derives retreat/refusal, both losses and capture shares from original paid participants. Refusal adds defender percentage; loss allocation conserves TOE; 30% threshold yields three DP within admitted TOE10 scope.
- `CampaignCombatLossRetreat.cs:46–103` recomputes causal settlement, retains pending lots at pre-loss origin, charges mandatory retreat through shared spending ledger, and updates element/representation together. Victory RP follows actual evacuation and caps at ten. `RetreatRoute` derives route from trusted content and side supply anchor, then checks retained geometry.
- `CampaignCombatObligations.cs:243–298` enforces hash-derived settlement identity through private constructor on creation and every immutable append; subsequent validation retains original participants, paid state and receipt predecessors. Legacy identities cannot enter hashed append path.
- `CampaignCombatResolutionCodec.cs:102–153` checks status/window/stage and complete allowed typed World before serialization. `CampaignWorldV7.cs:157–163` equality includes every World collection. ReadState authenticates replay and compares complete bytes; serializer is not an alternative authority admission API.
- Task013 proof remains: original 32 literal result events/64 hash cuts, role-ordered eight/nine draws, rejection sampling, cross-block draw vector, cursor overflow, paid state, retries and forgery rejection. Only old future-family cutoff changed; Task014 tests reject remaining future inputs from actual retreat frontier.
- Task014 matches canonical Task014 acceptance and Task013 dependency. Five material implementation/test paths plus narrow sixth existing-test cutoff are justified. Task015/016 and remaining checkpoint G closure stay pending. README/roadmap say under verification; tech-design records dormant behavior without claiming public activation.

## Author-claim reconciliation

Author/checks packets read only after persisted preliminary ledger. No referenced historical logs or aggregate evidence opened.

| Claim | Independently inspected evidence | Status / consequence |
| --- | --- | --- |
| 144 literal events / 176 state-hash cuts across 32 contexts | `AllRetreatPrefixesMatch144LiteralEventsAnd176StateHashCuts`; focused run | Confirmed; cumulative prefixes, not literal intermediate state JSON |
| Original 013 proof retained | Diff plus `CombatResolutionTests`; focused run | Confirmed; no replacement by weaker settlement-only proof |
| Independent opening and accepted-intent stability | Engine plus 20-opening / 56-choice matrix | Confirmed for retreat; subsequent custody isolation belongs to 015 |
| Simultaneous losses, refusal, captures, pending origin and CP10→11 | Helper, typed constructors, conservation assertions | Confirmed across retained branches and both acting sides/seal orders |
| Capped victory RP | Projection plus shared `VictoryRecoveryCauseAddsCohesionAndCapsAtTen` | Confirmed implementation and shared cap invariant; end-to-end Task014 starts at Cohesion0 |
| Immutable append, full World equality, provenance and ownership | Constructor, projection, codec, shared equality and negative tests | Confirmed within trusted replay boundary |
| Core candidate discard/retry atomicity | Every-cut discard/retry test and pure transition construction | Confirmed; does not prove host transaction, durable publication or process restart |
| Full suite/format/Boundary/CI passed | Checks packet testimony only | Not independently verified; unnecessary to duplicate root acceptance work |

## Verification performed

All .NET commands used `login:false`, approved local IPC, SDK 10.0.400 selected by global.json roll-forward, native MTP with xUnit v3, and unique binlogs. Tests followed independent rebuild. No `--disable-build-servers` passed to `dotnet test`.

1. `shasum -a 256 -c .planning/combat-task008-delivery/task014-source.sha256` — six matches before and after verification.
2. `dotnet build tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --disable-build-servers -t:Rebuild -m:1 -p:UseSharedCompilation=false '-bl:/tmp/task014-review3-rebuild-{}.binlog'` — exit 0; zero warnings/errors; 8.43 seconds.
3. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --no-restore --filter-class '*CombatLossRetreatTests' --filter-class '*CombatResolutionTests' '-bl:/tmp/task014-review3-focused-{}.binlog'` — 17 passed, zero failed/skipped; 22.522 seconds.
4. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --no-restore --filter-class '*CombatWorldTests' --filter-class '*CombatWorldInitialCodecTests' --filter-class '*CombatIdentityTests' --filter-class '*CombatSealsTests' --filter-class '*CombatCommitTests' '-bl:/tmp/task014-review3-shared-{}.binlog'` — 63 passed, zero failed/skipped; 16.059 seconds.
5. `git diff --check 6ee5980 -- src/Cna.Core/Campaigns/CampaignCombatResolution.cs src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs src/Cna.Core/Campaigns/CampaignCombatObligations.cs src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs tests/Cna.Core.Tests/Campaigns/CombatLossRetreatTests.cs tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs README.md tech-design.md docs/roadmap/pre-alpha-roadmap.md` — exit 0, empty output.
6. `dotnet build-server shutdown` — both servers shut down successfully. `ps -axo pid,ppid,comm | rg '(dotnet|Cna.Core.Tests|MSBuild|VBCSCompiler)'` — no matches (rg exit 1). All review sessions exited.

Binlogs verified present:

- `/tmp/task014-review3-rebuild-20260920-124522--36155--JpkulC.binlog`
- `/tmp/task014-review3-focused-20260920-124544--36249--FED7UE-dotnet-test.binlog`
- `/tmp/task014-review3-shared-20260920-124620--36274--sPnZd6-dotnet-test.binlog`

Final HEAD unchanged: `e7c9e8f81b6ac5230dc1340ecbc54cbe5c0395ba`. Current docs SHA-256:

- README.md: `824abeb2f9577ed9f230c61998675304ceb7013614e632b69316495529b73f6c`
- docs/roadmap/pre-alpha-roadmap.md: `49b5b726c0772a745dbde478e3d98b45d76b25a152a3c3f9b71771afe6a542b5`
- tech-design.md: `0c4712c23d5cd4a0788055c575fbcc50d49c3a7a1f23b76f0f63796839d11556`

Read-only discovery had several missing guessed filenames; exact filename inventory corrected them. No build/test failures occurred. Only own report intentionally written, aside from authorized generated build/test artifacts and temporary binlogs.

## Open questions and residual risks

No unresolved Task014 blocker. Generalized formations/geometry, nonzero initial Cohesion and broader TOE allocations are outside admitted contract. Core replay uses authenticated synthetic C3a boundary, not actual positive campaign history. Actual durable host transaction, extended snapshot, public/fog-of-war activation, custody and terminal closure remain later gates. Reference oracle/schema inspected, not rerun; no full solution build, full suite, full format or remote CI independently executed. Root owns broader gate and acceptance.

## Verdict

Ready — frozen Task014 dormant loss/retreat scope.

## Recommended next actions

Return report to root for acceptance. Review instance 3 of 3 exhausted; no further review instance started or requested. Root owns remaining budget, acceptance and subsequent Task015 dependency gate.
