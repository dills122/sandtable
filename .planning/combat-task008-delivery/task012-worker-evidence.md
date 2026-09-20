# Task012 worker evidence — frozen candidate

Implemented after accepted011 code94a4ecd, metadata6f9b501, under task012-dispatch.md. Exactly five primary paths; no schema/fixture/oracle/shared bridge edits. Root owns docs, fullgate, reviews, commits and acceptance. All worker test/format processes closed, exit statuses collected. No agents, full suite, commits or publication performed.

## Frozen SHA256 manifest

- `src/Cna.Core/Campaigns/CampaignCombatSealedRound.cs` — `904df6123bdd210aac4d9ea1bcc90d52646680498c89ff49fe478c464aba5b3c`
- `src/Cna.Core/Campaigns/CampaignCombatSealedRoundCodec.cs` — `6a771eb6204d607e634db567842719cb42fe69dea0c7c2813e1ff40b1f832a82`
- `src/Cna.Core/Campaigns/CampaignCombatSealedRoundModels.cs` — `89678b635d5d7a897fa3828bb884cb6dce8795548a2321d6f9f3aeb59860020c`
- `tests/Cna.Core.Tests/Campaigns/CombatCommitTests.cs` — `74baa6a3e9fb2ede62e25b0bc09ed13fc3870f59f620c665bbe5be1d67662bb4`
- `tests/Cna.Core.Tests/Campaigns/CombatSealsTests.cs` — `08d06f15caa61d2454120a6913f77a1c03fa3a201fa8d1ea347e047b97f73113`

## Production and authority

Private replay-derived commit transition requires Prepared step5, both sealed role slots, five retained step receipts, unchanged original World, empty attack/target ledgers, pinned selected costs and integer CP+5/+3 <=10 before general ChargeOrdinary. Both ammunition balances10→0, no Cohesion cause/change, directional attack and segment target entries, commitment hash/event/version/prefix update form one immutable candidate. RNG unchanged. Retry identity remains command hash plus separately checked actor, recovered before committed/depleted gates. Fresh opening disabled does not prevent retained commit/retry.

Original immutable Base and owned World bytes remain pre-use evidence. State.World is typed derived World. Codec constructs canonical current CP/ammunition fields from typed World on every serialization and verifies full typed equality against allowed cost-only variation; fabricated non-cost World changes reject rather than normalize. There is no independently mutable paid-byte cache.

## Failure-sensitive evidence and test inventory

- Meaningful first RED before production: valid final frozen commit applied at authenticated Prepared step5 rejected with `JsonException: Round2 commitment requires Task012.` Engine line158, test line17. `/tmp/012-red.log`: 1 failure, 1.203s, exit2. This is only failing run during Task012; no hidden compile/format failures.
- First literal GREEN: four literal commit events and paid states in one test, `/tmp/012-literals.log`:1/1,1.484s,exit0.
- First expanded GREEN: `/tmp/012-expanded.log`:13/13,8.585s,exit0.
- Expanded raw/forgery/ownership GREEN: `/tmp/012-expanded-final.log`:17/17,9.119s,exit0.
- Final formatted focused: `/tmp/012-frozen.log`:17/17,8.854s,exit0 (9 Commit tests +8 accepted Seals tests).
- Shared selection/certification/World-spending regressions: `/tmp/012-shared.log`:55/55,13.236s,exit0.
- Scoped format `/tmp/012-format.log`, scoped verify `/tmp/012-format-verify.log`:both exit0, empty output. `git diff --check`:exit0.

Exact fixture coverage completes10 retained Bases,58 literal events,68 literal canonical state readbacks including initial and paid cuts. Prior01154/64 and six cancelled traces remain intact. Four final paid traces cover both factions and first-seal orders. Paid readback replays original eligibility; it never treats ammunition0 World as a fresh eligible Boundary. Each paid trace tests candidate discard/replay before append, lost-response exact retries for every accepted input, changed metadata, actor rejection, duplicate append, same-round new-version recommit rejection and callback NoOp. Rebuilt CP5/7 supplemental histories use actual010B then011 public replay/apply APIs with independently trusted changed synthetic Boundary and reach CP10/10; no direct fabricated Prepared state.

Negatives cover early/cancelled/stale/malformed commitment, wrong actor/configuration/round/version/time, over-limit/fractional/cohesion/ammo/depleted eligibility, rehashed cost/allocation/commitment/RNG/prefix/event mutations, history/target/root mutations, structural predecessor forgery, nested primitive rejection before sentry trusted input access, typed non-cost World mutation rejection, derived cost-byte changes rejected causally, and owned event/history/target/World collections. Reuse accepted011 grammar/bounds/clock and009B support proof instead of duplicating those complete suites.

## Exact commands

All .NET calls use login:false, approved local IPC, project-native MTP; commands redirect to indicated log. Unique placeholder yields retained build and dotnet-test binlogs.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatCommitTests' '-bl:/tmp/012-red-{}.binlog' > /tmp/012-red.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatCommitTests' '-bl:/tmp/012-literals-{}.binlog' > /tmp/012-literals.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatCommitTests' --filter-class '*CombatSealsTests' '-bl:/tmp/012-expanded-{}.binlog' > /tmp/012-expanded.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatCommitTests' --filter-class '*CombatSealsTests' '-bl:/tmp/012-expanded-final-{}.binlog' > /tmp/012-expanded-final.log 2>&1
dotnet format Sandtable.slnx --no-restore --include src/Cna.Core/Campaigns/CampaignCombatSealedRound.cs src/Cna.Core/Campaigns/CampaignCombatSealedRoundCodec.cs src/Cna.Core/Campaigns/CampaignCombatSealedRoundModels.cs tests/Cna.Core.Tests/Campaigns/CombatCommitTests.cs tests/Cna.Core.Tests/Campaigns/CombatSealsTests.cs > /tmp/012-format.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatCommitTests' --filter-class '*CombatSealsTests' '-bl:/tmp/012-frozen-{}.binlog' > /tmp/012-frozen.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatStepsTests' --filter-class '*CombatIdentityTests' --filter-class '*CombatWorldTests' '-bl:/tmp/012-shared-{}.binlog' > /tmp/012-shared.log 2>&1
dotnet format Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatSealedRound.cs src/Cna.Core/Campaigns/CampaignCombatSealedRoundCodec.cs src/Cna.Core/Campaigns/CampaignCombatSealedRoundModels.cs tests/Cna.Core.Tests/Campaigns/CombatCommitTests.cs tests/Cna.Core.Tests/Campaigns/CombatSealsTests.cs > /tmp/012-format-verify.log 2>&1
git diff --check
```

## Retained binlogs

- `/tmp/012-expanded-20260920-105957--27784--4iyAkW.binlog`
- `/tmp/012-expanded-20260920-110002--27784--WtbSPh-dotnet-test.binlog`
- `/tmp/012-expanded-final-20260920-110348--28059--WqC8yo.binlog`
- `/tmp/012-expanded-final-20260920-110352--28059--gMQaem-dotnet-test.binlog`
- `/tmp/012-frozen-20260920-110444--28126--4eS2rY.binlog`
- `/tmp/012-frozen-20260920-110448--28126--2KtP+L-dotnet-test.binlog`
- `/tmp/012-literals-20260920-105737--27669--va29Qf.binlog`
- `/tmp/012-literals-20260920-105747--27669--lKM_8u-dotnet-test.binlog`
- `/tmp/012-red-20260920-105503--27525--6uC_iy.binlog`
- `/tmp/012-red-20260920-105512--27525--Ho_YlZ-dotnet-test.binlog`
- `/tmp/012-shared-20260920-110520--28146--oZOOQN.binlog`
- `/tmp/012-shared-20260920-110521--28146--2ppy+v-dotnet-test.binlog`

## Frozen authority pins

- `docs/specs/combat-sealed-round-v2.md` — `9fb337bdbfeebf0db367f860aafeb6a37b72af0d602036aec41da82678332bc5`
- `docs/specs/combat-sealed-round-v2.schema.json` — `365283c1c51b9e155aa1651c253759b271bbda75f613f52f511ceeb0dec12c7c`
- `docs/specs/fixtures/combat-sealed-round-v2.json` — `8200354f49bef2fd4a976dd9c24e4e485f8c06ad903d8c3deb06d5a9db509a95`

## Limits

Dormant Core trusted synthetic Boundary mechanism only. Actual creation-rooted path still empty; no positive-history admission, generic HistoryReplay integration, Snapshot12 extension, host transaction/publication recovery, result/dice/settlement, refund or Task013 implementation. Candidate discard and exact retry demonstrate immutable Core recovery, not host database atomicity. Result remains committed step5, closed=false. No outstanding scope blocker; full gate and independent reviews remain root-owned acceptance work.
