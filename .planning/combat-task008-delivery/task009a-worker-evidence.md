# Task009A worker evidence — 2026-09-20

Implementation delivered after H4 acceptance checkpoint f84b312. Five primary paths only; no
predecessor source/contract/oracle/fixture changes, commits, push, full-suite run or agents.

## Implementation and authority boundary

`CampaignCombatCertification.Admit` consumes trusted request, mandatory independently retained
Created11 and bounded ordered events; it always invokes actual HistoryReplay. Only completed typed
G2 at frozen first Combat position with idle flow, no interrupt and actual Movement-end/Breakdown
proof proceeds. Scope remains turn1/stage1/first slot/ordinal1 and actual six/seven moves. Participant
bindings derive from current `entry.Lifecycle.Movement.World` and trusted Content; normal Weather,
adjacency, CP and both ceilings derive independently. All required empty-profile predicates must
hold. No caller assessment, empty-list hint, cached boundary, synthetic World reset, positive
candidate, event/window/selection/round/opportunity or fresh admission is accepted.

Boundary owns retained history and preserves full typed entry. Codec writes exact existing
AdmissionBoundary order; read checks bounds then replays and compares all bytes. Selected boundary
contract limits every array to512, depth32 and whole bytes1MiB; intentionally does not import H4's
separate4096 World-cause allowance. Exact512 sentinel reaches history;513 nested
entry.world.cohesionCauses rejects before history access.

Participant is immutable current identity only, using existing UnitKey/ComponentKey. Bind checks
creation scope, original side/content element, source parent, owned component IDs, valid location
and unique current independent representation; it grants no combat eligibility or causal authority.
Read compares complete canonical bytes against independently supplied current context and unit.
World7 constructor already rejects impossible missing/ambiguous/duplicate representation coverage
and mismatched representation location; no claim that new tests bypass that invariant to exercise
impossible worlds. New tests do reject missing/foreign unit and component/context bindings and
explicit duplicate component binding. Pure renamed/depleted/swapped-occupant probes do not grant
those hypothetical worlds inherited-profile admission. Existing Contact/Engaged endpoint keys remain
original units; arrival at old location cannot substitute for them.

## TDD and failures

Initial RED command:
`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '-bl:/tmp/task009a-focused-red-{}.binlog' > /tmp/task009a-focused-red.log 2>&1`
Exit1: missing CampaignCombatCertification and CampaignCombatIdentityCodec APIs.
Binlog `/tmp/task009a-focused-red-20260920-063241--7558--WbuW5q.binlog`.

First implemented run `/tmp/task009a-focused-green.log` exit2 exposed test-literal assumption:
opponent location was written as `<side>-front`; frozen actual names are assault-east/assault-west.
Frozen full-boundary length/SHA assertions had already passed before literal comparison. Corrected
only independent literal assertion; production unchanged. Evidence retained. Follow-up
`/tmp/task009a-focused-literal-fix.log` passed1test/fourhistories. Expanded
`/tmp/task009a-focused-negatives.log` passed8tests before final count-capture/identity probes.

## Final verification

Exact focused command, login:false, escalated solely for local .NET IPC:
`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '-bl:/tmp/task009a-focused-final-{}.binlog' > /tmp/task009a-focused-final.log 2>&1`
Exit0,8passed/0failed/0skipped,11.900seconds.
- `/tmp/task009a-focused-final-20260920-063950--7972--AwWa3D.binlog`
- `/tmp/task009a-focused-final-20260920-063958--7972--Rlp2kZ-dotnet-test.binlog`

Formatting command:
`dotnet format whitespace Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatIdentityModels.cs src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs src/Cna.Core/Campaigns/CampaignCombatCertification.cs tests/Cna.Core.Tests/Campaigns/CombatIdentityTests.cs tests/Cna.Core.Tests/Cna.Core.Tests.csproj > /tmp/task009a-format.log 2>&1`
Exit0,emptylog. `git diff --check` exit0. All owned processes exited before return.

Eight cases comprise all-four-boundaries, unsupported/untrusted history, every assessment-field
mutation plus inherited authority/raw bytes, movement/rename/depletion identity continuity,
Contact and Engaged original-participant tests, foreign/duplicate/missing/noncanonical participant,
and early bounds/defensive ownership. Current actual boundary CP/Cohesion/RNG/receipt/prefix survive
without state reset. Wrong authentic head, active Reaction, unfinished Breakdown, synthetic C3a
substitution, unsupported shorter G2 terminal, omission/reorder/duplication/tampering all reject.

## Independent frozen boundary pins

Fixture `combat-inherited-selection-v1.json` contains full-boundary length/SHA, not full literal
Boundary JSON. Tests reconstruct expected complete bytes from accepted G2 entry writer plus an
independently written literal assessment (not production assessor), then independently check these
four frozen oracle hashes/lengths. This is reconstructed full bytes + literal assessment + frozen
whole-boundary digest evidence, not a claim that fixture stores literal full Boundary text.

| Owner / moves | Bytes | Frozen SHA256 |
| --- | ---: | --- |
| Axis /6 |15809|4b195573569de66562b19d05e62841f82548c01133f4fbd2b3f393e0e0544560|
| Axis /7 |16474|6ded79961a1cc04e7a1fa227ed626c8b18081180911a1d22e667b1d1847df2f6|
| Commonwealth /6 |16049|f9fe2ee5f6ceb68df3ec263529a05d49c9b09c0d3bf3f84c52501cf8bfba01a6|
| Commonwealth /7 |16722|5385427ac1c45a2c9b62b99734dc532a30aa0004099c2b98a0e43ebceb9d671f|

## Source freeze

```text
49459dcfa95b6c96ebc1c6e4a80acee0883c01355914304de4369e801d569a3d  src/Cna.Core/Campaigns/CampaignCombatIdentityModels.cs
f5af6c8c16ecd812a4b22265c91799522767e6418840df2a0952c44d0be29236  src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs
474f3efcd86b63b58b297a99df941fd940380927e753c043161a485cf81156c0  src/Cna.Core/Campaigns/CampaignCombatCertification.cs
7d7602af909c1c1d939aa44673e621320029554c98d2eb64c71a2b81cd4d12ac  tests/Cna.Core.Tests/Campaigns/CombatIdentityTests.cs
ed305238756985d1c8e8c9190fe4fee2b0df30669ba5c912b9b207eae8275440  tests/Cna.Core.Tests/Cna.Core.Tests.csproj
```

## Remaining gates

Parent009 remains open.009B owns positive all-result geometry/capacity certification, Candidate and
opportunity-v2 mechanism.010/011 own actual selection/decline/Base2/final opportunity;020 owns public
privacy. No Task010 event/control implementation or broader scenario validation duplicated here.
Full gate and developer/three independent reviews are root-owned. Later actual positive assault,
full28runtime closure and HOST-PUB-001 remain outside claim.

## Review1 accepted P2 fix — later freeze supersedes original codec/test hashes

Root accepted Participant validation-order finding from review1. Only IdentityCodec and
CombatIdentityTests changed. Regression was written first while root's original2252-test fullgate
ran against its frozen binary; no worker .NET/build/format started until explicit root clearance.

RED:
`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' --filter-method '*ParticipantShapeAndCanonicalBytesRejectBeforeTrustedBindingContextIsRead' '-bl:/tmp/task009a-review1-red-{}.binlog' > /tmp/task009a-review1-red.log 2>&1`
Exit2,1failed. Expected JsonException; actual ArgumentNullException(request) from BindParticipant
proved malformed Participant reached trusted context before rejection.

Fix: after bounds, parse closed Participant/Unit fields, reject missing/unknown/duplicate fields
and wrong/null types, construct bounded immutable values, compare exact canonical bytes, and only
then invoke independent trusted binding. Parsed values are syntax evidence, not trusted authority.
No wire bytes, admitted profile or Boundary behavior changed. Regression covers malformed/null,
missing, unknown, duplicate, wrong-type, whitespace/BOM, escaped-key and reordered inputs with null
trusted-context sentries; well-formed canonical syntax reaches binder's null-context guard.

GREEN:
`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '-bl:/tmp/task009a-review1-green-{}.binlog' > /tmp/task009a-review1-green.log 2>&1`
Exit0,9passed/0failed/0skipped,11.551seconds.

`dotnet format whitespace Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs tests/Cna.Core.Tests/Campaigns/CombatIdentityTests.cs > /tmp/task009a-review1-format.log 2>&1`
Exit0,emptylog; git diff --check exit0. All worker processes closed before return. Root owns rerun
fullgate and remaining fresh reviews against corrected candidate.

Final source SHA256 (unchanged files included):
```text
49459dcfa95b6c96ebc1c6e4a80acee0883c01355914304de4369e801d569a3d  src/Cna.Core/Campaigns/CampaignCombatIdentityModels.cs
3d1ed7b30af8ec1258fea374d0d81059c78ee4b29e90e4b1c5f2f372df66e4e6  src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs
474f3efcd86b63b58b297a99df941fd940380927e753c043161a485cf81156c0  src/Cna.Core/Campaigns/CampaignCombatCertification.cs
addf626dab8bc7ca738c5b6ddaa40af510577e91273fd5442f9786e7053b2c30  tests/Cna.Core.Tests/Campaigns/CombatIdentityTests.cs
ed305238756985d1c8e8c9190fe4fee2b0df30669ba5c912b9b207eae8275440  tests/Cna.Core.Tests/Cna.Core.Tests.csproj
```

## Review2 accepted P2 fix — final source freeze

Root accepted Boundary validation ordering finding. Only IdentityCodec/tests changed in production
scope. Regression preceded implementation; .NET began only after root confirmed its prior full
suite2253/0/0 finished. Generic bounds now precede private frozen syntax/canonical parsing, then
actual-history `Admit`, then exact replay-derived whole-byte comparison. Syntax recognizes all
transitive G2 entry shapes but supplies no trusted state and broadens no admitted profile.

Exact focused commands (login:false; escalated only for local MTP IPC):
```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' --filter-method '*BoundaryShapeAndCanonicalBytesRejectBeforeTrustedHistoryIsRead' '-bl:/tmp/task009a-review2-red-{}.binlog' > /tmp/task009a-review2-red.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '-bl:/tmp/task009a-review2-green-{}.binlog' > /tmp/task009a-review2-green.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '-bl:/tmp/task009a-review2-green2-{}.binlog' > /tmp/task009a-review2-green2.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '-bl:/tmp/task009a-review2-final-{}.binlog' > /tmp/task009a-review2-final.log 2>&1
```
RED exit2,1failed/0passed,2.237s: JsonException expected, actual sentry History.Count
InvalidOperationException proves premature history access. First GREEN build exit1 on CA1859
IReadOnlyDictionary declaration; changed private field to concrete Dictionary, preserved log.
GREEN2 exit0,10/10,11.855s. Final expanded focused run exit0,10passed/0failed/0skipped,11.891s.
Final binlogs `/tmp/task009a-review2-final-20260920-070542--10158--BJsNUN.binlog` and
`/tmp/task009a-review2-final-20260920-070546--10158--FUKGsj-dotnet-test.binlog`.

Regression traverses every populated nested object for missing/unknown/wrong-type/reordered
fields with inaccessible-history sentry; also malformed roots, duplicate fields, BOM/whitespace,
escaped key, noncanonical integer spelling, keyed-array order, bad IDs/hashes/enums and overflow.
Canonical512 candidate IDs reach history;513 reject first. UInt64 max cursor, signed minima,
nullable cycle and moving union arm reach history without gaining authority; wrong/nonempty
cohort and unsupported tag reject first. Four frozen actual Boundary byte lengths/hashes and
ownership tests remain green. No fixture/schema/oracle edits or invented expected boundaries.

Exact independent descriptor audit (read-only, no oracle fixture generation):
```sh
python3 -B - <<'PY'
import importlib.util,re
s=importlib.util.spec_from_file_location('x','docs/specs/verify-combat-inherited-selection-v1.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
schema=m.ibc.lc.SCHEMA|m.ibc.SCHEMA|m.SCHEMA;unions=m.ibc.lc.INVENTORY['unions'];seen=set();expected={}
def visit(t):
 t=t.removesuffix('?').removesuffix('[]')
 if t in seen:return
 seen.add(t)
 if t in schema:
  expected[t]=' '.join(k+':'+v for k,v in schema[t]);[visit(v) for _,v in schema[t]]
 elif t in unions:[visit(v) for v in unions[t].values()]
visit('AdmissionBoundary')
actual=dict(re.findall(r'\["([^"\n]+)"\] = "([^"\n]+)",',open('src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs').read()))
assert actual==expected,(actual.keys()-expected.keys(),expected.keys()-actual.keys())
print('PASS: all51 private object descriptors exactly match frozen transitive AdmissionBoundary closure; no extra or missing object shapes.')
PY
```
Exit0, exact PASS message. Primitive delegation follows inherited lifecycle→reserve→result/round→
authority-envelope, including signed Int32/Int64, UInt64, ASCII IDs/text/hash, nullable union arms.
Lifecycle canonical keyed arrays match frozen keys; Route/OrderedLocations and assessment IDs retain
semantic order. Maintenance cost: private51-object syntax inventory must be audited against any
future frozen-contract revision. It is deliberately local, not a reusable schema framework.

Formatting commands:
```sh
dotnet format whitespace Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs tests/Cna.Core.Tests/Campaigns/CombatIdentityTests.cs > /tmp/task009a-review2-format.log 2>&1
dotnet format whitespace Sandtable.slnx --no-restore --include src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs tests/Cna.Core.Tests/Campaigns/CombatIdentityTests.cs > /tmp/task009a-review2-format-fix.log 2>&1
dotnet format whitespace Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs tests/Cna.Core.Tests/Campaigns/CombatIdentityTests.cs > /tmp/task009a-review2-format-final.log 2>&1
```
Initial verify exit2 for whitespace only; scoped fix exit0; final verify exit0/empty log.
Final source edits after10/10 test were whitespace only. `git diff --check` exit0.
All worker test/format processes completed before return; no full suite, commits, pushes or agents.
Root owns corrected fullgate and third independent review.009B research checkpoint saved separately
in `task009b-dispatch-proposal.md`; no009B implementation performed.

Final five-path SHA256:
```text
49459dcfa95b6c96ebc1c6e4a80acee0883c01355914304de4369e801d569a3d  src/Cna.Core/Campaigns/CampaignCombatIdentityModels.cs
30265c57d86317fbd6ec8f5bef3d50b0fd729ac8b9bda7f4ddc1f161219f3c4e  src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs
474f3efcd86b63b58b297a99df941fd940380927e753c043161a485cf81156c0  src/Cna.Core/Campaigns/CampaignCombatCertification.cs
3380a076ba3e9f70ee87153250d821b095247b4738519a22f63534338675d25e  tests/Cna.Core.Tests/Campaigns/CombatIdentityTests.cs
ed305238756985d1c8e8c9190fe4fee2b0df30669ba5c912b9b207eae8275440  tests/Cna.Core.Tests/Cna.Core.Tests.csproj
```
