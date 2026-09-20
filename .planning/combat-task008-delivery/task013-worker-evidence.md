# Task013 worker evidence — frozen implementation

Authorized after012 acceptance8fcf8b9/metadataf2c0046. Exactly six physical paths (five material plus mechanical fixture link), as task013-dispatch.md. No other production/test edits, agents, review dispatch, fullsuite, commits/push or014 implementation. Root owns docs/fullgate/reviews/acceptance. All owned .NET and format processes closed with exit status collected before root handoff.

## Frozen SHA256 manifest

- `src/Cna.Core/Campaigns/CampaignCombatResolution.cs`: `a8c94104101ea44c649fdd2b74d0a32b2fe58acfe6ebb2b5d2730762a32c55fb`
- `src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs`: `7fe8708c1ead7fc2dd893d9ba0ed852664486139172be8727f2db4d357ac8f2c`
- `src/Cna.Core/Campaigns/CampaignCombatObligations.cs`: `33f23010e09c60efab6a6834140286f8ededae00689fba154881ff1ee614a6e4`
- `src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs`: `fa86081d9a73a6e7f97bdd33b12d2a5c27bc94ea36d674ed7c11eb0f34652285`
- `tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs`: `c695c0e72cdf62feca1e28f8d032878dec146479c6c37fd6948768964682454d`
- `tests/Cna.Core.Tests/Cna.Core.Tests.csproj`: `a63c5962b8aba047aa76e64bfeb2bae37befa2e9f883a06366fd128b0bd70ca9`

## Implementation and invariants

Resolution context replays actual independently trusted C3a inputs/events through accepted012 Round inputs/events. Requires committed step5/two sealed slots/five receipts/one attack and target entry. Context owns paid canonical World extracted from exact serialized RoundState and coupled committed hash; original Base remains unchanged. Input/event/state readers validate closed grammar/bounds/canonical bytes before trusted history access, then regenerate complete causal bytes. Created copied before caller-list Count/index access.

One resolve transition consumes retained RandomState using NextByte, preserves rejected bytes, follows eight pinned role purposes and exactly one conditional capture-share draw. Existing adjudicator produces facts using both preloss roles. Result/final cursor/pending typed World/event/head/receipt form one immutable candidate. World equality enforces paidWorld plus one pending settlement; serialization cannot normalize unrelated typed mutations. No additional payment/loss/movement/Cohesion/custody/closure. Exact command+actor retry precedes committed/resolved status and time checks after primitive/context validation; no-window timers NoOp; all later families reject.

Named resolved-only factory derives hashed settlement identity from exact cmt./res. lowercase64hex IDs, retains existing paid-preloss/opposing-participant/scope checks and null consequence receipts. Legacy constructor unchanged. Result2 recursive profile allows semantic Route arrays/futureTurn1..115, forbids LegacyBrokenVehicleLot, preserves C3a Route rejection and inherited defaults. Local grammar copies16 frozen Result2 descriptors plus inherited Timing and Receipt; Timing uses configHash/highWater, not Round ClockTiming.

## TDD and every failed attempt

- Initial fixture-link edit script incorrectly expected closing None tag despite self-closing existing element; ValueError before any edits. Subsequent test invocation selected no test and exited8; `/tmp/013-red.log`. Mechanical link corrected before meaningful RED. This is setup failure, not behavioral RED.
- Meaningful RED before production: authenticated first paid RoundState matched exact fixture committed bytes; missing CampaignCombatResolution API assertion failed. `/tmp/013-red-transition.log`:1failure,1.274s,exit2. Missing-transition reflection assertion replaced with full behavioral literal test after implementation.
- Parent dev inspection caught cmt/res/set hash prefix spelling during implementation before first literal run; changed to prefix+64hex, matching accepted012 and frozen oracle. No failing runtime attributed to that fix.
- First literal GREEN `/tmp/013-literals.log`:1test covering32 literal resolve events and64 state-hash readbacks,3.638s,exit0.
- `/tmp/013-expanded.log`:compile failed CA1861 at expected-dice constant array argument,exit1. Changed to local typed collection; no production change for this diagnostic.
- Expanded `/tmp/013-expanded2.log`:9/9,11.823s,exit0.
- Final formatted `/tmp/013-frozen.log`:10/10,12.847s,exit0.
- Shared `/tmp/013-shared.log`:100/100,22.330s,exit0 (SelectedRules/RandomStream/CombatWorld/Identity/Steps/Seals/Commit).
- Scoped format and verify logs `/tmp/013-format.log`, `/tmp/013-format-verify.log`:exit0,empty. `git diff --check`:clean.
- First read-only transitive descriptor audit omitted locator from its primitive set and stopped after confirming18/18 local descriptors. Corrected audit primitive list; no source change. Final `/tmp/013-descriptor-audit.log`:18/18 exact local descriptors and29 transitive external objects, all resolved. No oracle execution by worker.

## Exact evidence scope

All32 contexts authenticate literal committed012 bytes. All32 first resolve event bytes match literally. All64 initial/resolved serialized state hashes match stateHashes and undergo actual strict ReadState readback. These are HASH cuts; fixture lacks intermediate literal state JSON. Full272events/304cuts remains future014–016.

Draw counters16eight/16nine,12rejection traces,zero literal blockcrossings. Supplemental existing research seed0cursor30 rebuilt through actual C3a+Round APIs: consumed4dd79fabb6070dc7, dice66443222, final38, differential-1, base10/10,no capture. UInt64.MaxValue and MaxValue-3 overflow via authenticated rebuilt contexts reject without changing committed state/cursor. No fake committed cache or alternate seed.

All32 traces test discard/reapply, accepted retry with altered clock metadata, lost-response exact event recovery, wrong actor, malformed time, fresh newer-version reroll rejection, duplicate append, no-window callbacks and every later fixture event/family rejection. No later event accepted for syntax convenience.

Targeted rehashed result/draw/purpose/order/extra-or-missing9th-die/commitment/settlement/config/head forgeries; state/cursor/preloss ammunition changes; missing Round event, wrong independent Round actor and changed Boundary cursor reject. Raw unknown/missing/duplicate/escaped/reordered/whitespace/null/type/oversize/513array/deep values fail before trusted-history sentry. Valid canonical bytes reach sentry. Inherited Timing shape, semantic reversed World arrays, empty Route and futureTurn endpoints reach sentry; C3a Route rejects, legacy broken lots reject. Factory legacy validity/rejection, malformed hashed IDs, same participant, unpaid and wrong scope fail. Removing pending settlement and changing non-settlement paid readiness reject serialization. Owned Created mutation on caller Count access, event copies, draws/consumed/settlement collections and receipt-copy tests pass.

Existing full005 adjudication and009B certification reused rather than copying all outcomes. Shared100 tests include independent table/random/model regressions. No blocker remains within dispatched scope.

## Exact verification commands

All test/format processes use login:false and approved local IPC. Unique binlogs retained below.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatResolutionTests' '-bl:/tmp/013-red-{}.binlog' > /tmp/013-red.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatResolutionTests' '-bl:/tmp/013-red-transition-{}.binlog' > /tmp/013-red-transition.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatResolutionTests' '-bl:/tmp/013-literals-{}.binlog' > /tmp/013-literals.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatResolutionTests' '-bl:/tmp/013-expanded-{}.binlog' > /tmp/013-expanded.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatResolutionTests' '-bl:/tmp/013-expanded2-{}.binlog' > /tmp/013-expanded2.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatResolutionTests' '-bl:/tmp/013-frozen-{}.binlog' > /tmp/013-frozen.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatSelectedRulesTests' --filter-class '*RandomStreamTests' --filter-class '*CombatWorldTests' --filter-class '*CombatIdentityTests' --filter-class '*CombatStepsTests' --filter-class '*CombatSealsTests' --filter-class '*CombatCommitTests' '-bl:/tmp/013-shared-{}.binlog' > /tmp/013-shared.log 2>&1
dotnet format Sandtable.slnx --no-restore --include src/Cna.Core/Campaigns/CampaignCombatResolution.cs src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs src/Cna.Core/Campaigns/CampaignCombatObligations.cs src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs tests/Cna.Core.Tests/Cna.Core.Tests.csproj > /tmp/013-format.log 2>&1
dotnet format Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatResolution.cs src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs src/Cna.Core/Campaigns/CampaignCombatObligations.cs src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs tests/Cna.Core.Tests/Cna.Core.Tests.csproj > /tmp/013-format-verify.log 2>&1
git diff --check
```

Descriptor audit extracts dictionary entries with regex, compares exact object mapping with frozen Result2 objects plus rules-inputs Timing and selection-steps Receipt, then recursively follows field types into existing Identity descriptors, recognizing id/hash/rawHash/int/long/ulong/bool/side/actor/utc/null/string/locator/futureTurn/Route/LegacyBrokenVehicleLot primitives and ResultEffect tagged union. Result2-local count18; external closure29: Ammunition,Cause,Component,ComponentKey,Cp,Custody,Disposition,Element,Entitlement,FutureScope,Guard,Losses,Lot,Obligation,Operational,Origin,Random,Readiness,Reference,RelationsReceipt,Relationship,Representation,Result,Retreat,RoleLoss,Scope,Settlement,UnitKey,World. Special primitives/profile semantics tested separately. Maintenance cost remains explicit copied frozen local descriptors and small typed World projection; future settlement transitions must deliberately extend factory/projection, not relax legacy rules.

## Binlogs

- `/tmp/013-expanded-20260920-114131--31025--ZUxJKM.binlog`
- `/tmp/013-expanded2-20260920-114330--31113--T9w6Mt.binlog`
- `/tmp/013-expanded2-20260920-114336--31113--Nvmk1G-dotnet-test.binlog`
- `/tmp/013-frozen-20260920-114516--31226--9M8Sma.binlog`
- `/tmp/013-frozen-20260920-114525--31226--zZoPYz-dotnet-test.binlog`
- `/tmp/013-literals-20260920-113746--30879--1ILEEz.binlog`
- `/tmp/013-literals-20260920-113755--30879--J2Pn+s-dotnet-test.binlog`
- `/tmp/013-red-20260920-113149--30507--kJ3RRJ.binlog`
- `/tmp/013-red-20260920-113157--30507--NLuV+f-dotnet-test.binlog`
- `/tmp/013-red-transition-20260920-113215--30549--pXax1F.binlog`
- `/tmp/013-red-transition-20260920-113220--30549--3KAH10-dotnet-test.binlog`
- `/tmp/013-shared-20260920-114557--31256--Zjw66Y.binlog`
- `/tmp/013-shared-20260920-114558--31256--Q0datp-dotnet-test.binlog`

## Frozen source authority

- `docs/specs/combat-result-settlement-v2.md`: `a040dbd69c8c5e978bbce79b8765088ab2efdc33e8ef95ced22a5f35680fbc68`
- `docs/specs/combat-result-settlement-v2.schema.json`: `719634c4720b4a92669b2e83e60df58cd32d0e556ec9ef66c546aed1f9c7e9d6`
- `docs/specs/fixtures/combat-result-settlement-v2.json`: `6a1f6cda74424680669539d73583fa3ba5affc612380b3a3efe2e8986a09804a`
- `docs/specs/verify-combat-result-settlement-v2.py`: `a6a781976f26b78a9dc098ebfbb3b516284aa1d5873744d96d8fa23aa50d83a2`

## Limits

Dormant trusted synthetic Boundary only. Actual creation-rooted campaign path has no positive opportunity; no positive-history admission, generic HistoryReplay, Snapshot successor, public actions/privacy activation or host durable transaction proof. Discard/lost-response tests prove Core immutable candidate/replay behavior only. Result remains intermediate resolved with pending mandatory consequences, no window/closure/CA advancement.014–016 untouched. Root fullgate/independent reviews still determine acceptance.
