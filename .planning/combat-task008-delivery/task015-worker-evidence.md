# Task015 worker evidence — frozen custody implementation

Seven authorized physical paths only: five material paths and two narrow prior-test cutoff updates. Parent owns documentation, integration, full gate and independent reviews. No commits, full suite, agents or oracle execution by worker. All owned processes closed; final source frozen.

## Behavior and authority

Custody follows actual separately trusted predecessor inputs/events through C3a, Round2 and Result2 replay. Positive pending lots open independent original-Config1 custody windows. Captor ownership and context/choice validation precede fallback; local opening/deadline/high-water govern time, audit maximum does not. Command/actor retry precedes current status/time rejection. Lost/overflow clock opening settles unguarded atomically. Noncapture remains at retreat; relationships/closure remain unsupported.

Named immutable WithResultV2Custody append preserves predecessor receipts and unconditional derived settlement identity checks. Projection reconstructs current post-retreat donor and pending lot from immutable paid context, reconstructs expected custody receipt/assets, and compares complete settlement and complete typed World. Guard transfers one TOE without new CP, retains donor operational/resources/origin/readiness and victim source. Escape retains quantity/source, creates no immediate TOE, uses certified Normal/Clear foot route bounds and exact twelve-stage future calendar. Upkeep/training obligations remain unexecuted. Canonical guard origin-rich fields come from validated owned donor serialization; no caller JSON authority. Serializer status/receipt/window.Kind checks extend existing stage guard.

## Proof inventory

Six new custody facts plus preserved seven loss/retreat and ten resolution facts: final 23/23.

- 32 trusted synthetic contexts; 176 exact literal events and 208 retained STATE SHA cuts, each causal readback. These are hash commitments, not literal whole-state JSON. Original 013 32/64 and 014 144/176 proof retained with only future-family cutoff maintenance.
- 16 positive custody branches: eight guarded, eight escaped. Four CP11 guard instances retain current donor CP. Literal guarded routes cover four one-edge and four two-edge routes; escape routes are zero-edge. Maximum supported route geometry relies on existing certification/World/rule proof, not new literal coverage.
- Exact 200 same-owner paired clock comparisons across four role/seal profiles: seven opening cases per pair and nine choice times × two confidence states × two prior private acceptance histories. Compare gameplay/window outcomes, not private event bytes or history heads. Earlier private acceptance at 10001 versus 11000 does not prevent independent opening at 10500.
- All retained-cut retries, failed/discarded candidate replay, wrong owner, exclusive deadline, stale callback, duplicate append and unsupported future events.
- Rehashed causal receipt/route/donor/source/lot/resource/obligation/entitlement forgeries, immutable source/result/RNG, conservation and no double transfer; full typed World and serializer stage/window mismatches.
- Raw malformed primitive, bounds and noncanonical inputs fail before trusted context; canonical input reaches sentry history. Owned returned buffers and read-only asset collections resist mutation.

## RED and failures

First RED is behavioral, not compilation: authentic 014 pending-lot prefix plus literal custody opening failed with `JsonException: Structural settlement requires null time and available confidence.` Production custody support then made same test pass.

Expanded run's sole failure was test assumption: independently replayed CombatAssaultResult records contain Draws collections whose record equality is reference-based. Corrected assertion to ResultId plus exact SerializeResultPreimage bytes. Production model unchanged. Clock200 passed in that run. No other failures or format changes after final focused freeze.

## Exact commands and results

All dotnet commands used login:false and authorized local IPC escalation. Unique placeholder binlogs retained. Each command redirected both streams to stated log.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatCustodyTests' '-bl:/tmp/015-red-{}.binlog' > /tmp/015-red.log 2>&1
```

exit 2; 1 failed; 1.328s; authentic pending-lot custody opening unsupported.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatCustodyTests' '-bl:/tmp/015-first-green-{}.binlog' > /tmp/015-first-green.log 2>&1
```

exit 0; 1 passed; 1.403s.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatCustodyTests' --filter-class '*CombatLossRetreatTests' --filter-class '*CombatResolutionTests' '-bl:/tmp/015-literals-{}.binlog' > /tmp/015-literals.log 2>&1
```

exit 0; 18 passed; 24.618s.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatCustodyTests' '-bl:/tmp/015-expanded-{}.binlog' > /tmp/015-expanded.log 2>&1
```

exit 2; 3 passed / 1 failed; 19.873s; test record equality assumption.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatCustodyTests' --filter-class '*CombatLossRetreatTests' --filter-class '*CombatResolutionTests' '-bl:/tmp/015-frozen-{}.binlog' > /tmp/015-frozen.log 2>&1
```

exit 0; 23 passed; 30.265s.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatSelectedRulesTests' --filter-class '*RandomStreamTests' --filter-class '*CombatWorldTests' --filter-class '*CombatIdentityTests' --filter-class '*CombatStepsTests' --filter-class '*CombatSealsTests' --filter-class '*CombatCommitTests' '-bl:/tmp/015-shared-{}.binlog' > /tmp/015-shared.log 2>&1
```

exit 0; 100 passed; 17.498s.

```sh
dotnet format Sandtable.slnx --no-restore --include src/Cna.Core/Campaigns/CampaignCombatResolution.cs src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs src/Cna.Core/Campaigns/CampaignCombatObligations.cs src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs tests/Cna.Core.Tests/Campaigns/CombatCustodyTests.cs tests/Cna.Core.Tests/Campaigns/CombatLossRetreatTests.cs tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs > /tmp/015-format.log 2>&1
```

Exit 0; empty log.

```sh
dotnet format Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatResolution.cs src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs src/Cna.Core/Campaigns/CampaignCombatObligations.cs src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs tests/Cna.Core.Tests/Campaigns/CombatCustodyTests.cs tests/Cna.Core.Tests/Campaigns/CombatLossRetreatTests.cs tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs > /tmp/015-format-verify.log 2>&1
```

Exit 0; empty log.

`git diff --check`: exit 0, empty output. All test/format sessions closed before parent full gate.

## Frozen SHA256 manifest

- `1d057ff4c203a0bbe6714912bff1af95fd83f84832e3597a502c1e81b15e15d7` `src/Cna.Core/Campaigns/CampaignCombatResolution.cs`
- `b1659d349f80a38daa5be1e3e89b9a6ddbc719f346374d697c24147cfb0ad109` `src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs`
- `5050baf1379dd7a9a2dda64fbf4c9b751984aeae93d053c6bb18c25411f8b18c` `src/Cna.Core/Campaigns/CampaignCombatObligations.cs`
- `79025b89bd88ce575ece19d01f30676dca7302d2ce5ff1d29fb9c48480fb992c` `src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs`
- `09eb385b489fed9ba27840907de1d3018d5c266dfa21a317b73c22c839339ee7` `tests/Cna.Core.Tests/Campaigns/CombatCustodyTests.cs`
- `d1202a78c87ab6c20c618b9730c38d87fb9f32e71b42b8dd26c65b1e29eab2e9` `tests/Cna.Core.Tests/Campaigns/CombatLossRetreatTests.cs`
- `6594a0b779d5e9b7c536c3b188625d6104d8ec8db23e091cc749803e1840cd47` `tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs`

## Retained binlogs

- `/tmp/015-expanded-20260920-125516--36863--yeSkU9.binlog`
- `/tmp/015-expanded-20260920-125521--36863--oByMNB-dotnet-test.binlog`
- `/tmp/015-first-green-20260920-125231--36684--hP0czj.binlog`
- `/tmp/015-first-green-20260920-125241--36684--3Qfm1k-dotnet-test.binlog`
- `/tmp/015-frozen-20260920-125745--37003--stuvrd.binlog`
- `/tmp/015-frozen-20260920-125753--37003--Kli5Sb-dotnet-test.binlog`
- `/tmp/015-literals-20260920-125316--36756--yliDn3.binlog`
- `/tmp/015-literals-20260920-125321--36756--C3xooF-dotnet-test.binlog`
- `/tmp/015-red-20260920-124947--36511--AuxiZu.binlog`
- `/tmp/015-red-20260920-124955--36511--1Jx1HD-dotnet-test.binlog`
- `/tmp/015-shared-20260920-125957--37190--qfrque.binlog`
- `/tmp/015-shared-20260920-125958--37190--pc5qGF-dotnet-test.binlog`

## Limits

Core mechanism proof starts from independently trusted synthetic C3a Boundary. No authenticated positive creation-rooted history, Snapshot extension, public activation or host transaction/publication claim. Task016 relationship/round/CA closure and future obligation execution remain open. No schema, World7, rules, Round, identity, receipt-model or fixture edits. Root full gate and independent reviews remain required for acceptance.
