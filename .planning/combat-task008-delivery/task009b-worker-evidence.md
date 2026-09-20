# Task009B worker evidence

Base: accepted009A code39dccfe, metadata652c49a. Five primary paths only; no predecessor,
contract/schema/oracle/fixture edits. Root owns docs/fullgate/devreview/three isolated reviews.
No commits, pushes, agents or full suite. All .NET commands login:false with authorized local MTP
IPC escalation; native --project and unique binlogs. This is dormant trusted-facts certification,
not actual positive-history admission, public activation, downstream lifecycle or publication.

## Final behavior

`CertifyInitialProfileFacts` validates retained Created11 against independently trusted request,
checks existing CycleAuthority identity/scope, then compares every typed World fact with initial
World except current integralCP0..10. It preserves actual supplied CP for resource checks. Caller
still authenticates complete history, Breakdown/position, order and Weather receipt/applicability.
Both valid typed applicable Weather kinds are caller-trusted projections, not dice/forecast input.
No current actual G2 positive history exists; inherited `Admit` unchanged and still empty.

Runtime catalogue visits all1296 morale pairs, derives five differential representatives, visits
6480 joint assault coordinates and calls existing Resolve for all8840 capture/refusal cases.
Only afterward it deduplicates full immutable results to637. Selected Rules/Content certification
and source tables are reused, never reimplemented. Private catalogue cache contains only immutable
rules outputs; no caller/current state cached as authority.

Before Candidate return, every distinct result checks conservation, original survival≥7, prisoner
maximum3, guard donation/capacity, loss DP and CP arithmetic, accepted/refused retreat, captor after
retreat versus original capture origin, entered enemy-occupied hex exclusion, guarded≤3hex path
and reunion≤8CP using existing Clear foot movement cost. Content7 proves fixed two-unit Clear line
and absence of hidden blockers. Typed initial equality rejects all unsupported world state first.

Both side orientations at CP5/7 pass the full637-result check. Defender pays3 toCP10, then each
accepted retreat invokes existing mandatory-retreat helper toCP11 with excess DP. Exact helper
CP11/DP/overflow evidence is reused from `CombatWorldTests.cs:90–132`; independent arithmetic proof
remains `CombatSelectedRulesTests.cs:102–171`. No future result/retreat/custody event executes.
Geometry maxima within this line: guarded path2hex when captured attacker relocates to retreated
captor; defender capture relocates1hex. Escape reunion is0hex or1Clear hex/2CP. Both capture roles
and accepted/refused retreat remain covered; zero positive captured TOE would create no custody
branch. Original participant membership never changes to a new guard/current occupant.

Candidate owns immutable Participants and ID strings; reader checks bounds, closed shapes and exact
canonical spelling before trustedExpected access. Null trustedExpected is a sentry: malformed bytes
produce JsonException first; valid canonical bytes reach ArgumentNullException. Trusted Candidate
comparison follows; returned serialization owns buffers. Current opportunity-v2 calculator writes
ordered named typed preimage fields and uses exact v2 domain. All10 current Round2 fixture rows
match literal opportunity IDs; Base2 domain hash separately checked against literal base bytes.
Tests label fixture provenance synthetic-C3a. No hash establishes authenticity or emits outward IDs.

## TDD and focused verification

Exact commands:
```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '-bl:/tmp/task009b-red-{}.binlog' > /tmp/task009b-red.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '-bl:/tmp/task009b-green-{}.binlog' > /tmp/task009b-green.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '-bl:/tmp/task009b-expanded-{}.binlog' > /tmp/task009b-expanded.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '-bl:/tmp/task009b-expanded2-{}.binlog' > /tmp/task009b-expanded2.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '-bl:/tmp/task009b-final-{}.binlog' > /tmp/task009b-final.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '-bl:/tmp/task009b-capacity-{}.binlog' > /tmp/task009b-capacity.log 2>&1
```

- RED exit1: test-first compile failure for missing CertifyInitialProfileFacts, Candidate codecs and
  SupportCoverage APIs. Binlog `/tmp/task009b-red-20260920-072916--12218--9E0Etk.binlog`.
- First GREEN exit0:11passed/0failed/0skipped,11.838s. Includes original10 tests plus initial new test.
- Expanded compile exit1: test typo `CampaignElementReserveStatus.I`; corrected to existing ReserveI.
  Failure log retained; no production defect concealed.
- Expanded2 exit0:14/0/0,12.713s.
- Final expanded hostile-boundary run exit0:14/0/0,12.389s.
- Supplied World cause-capacity probes run exit0:14/0/0,12.606s.1/512/4096 pre-existing causes all
  reject initial profile. No claim that whole future Boundary/root fits follows from this result.

New tests cover both roles, CP0/0 and5/7 success;6/7,5/8,10/10 no candidate; each non-Normal Weather
on either role no candidate; fractional/overflow CP, depletion, location/parent/readiness/Reserve,
foreign creation/scope and actual G2 facts rejected. World7 constructor already forbids duplicate
ambiguous representation coverage; no claim that tests construct impossible World7 values.
Candidate malformed/null/missing/duplicate/reordered/escaped/deep/oversized/component bounds reject
before trustedExpected. Each v2 preimage field mutation changes digest. Existing009A tests preserve
four actual frozen Boundary hashes/lengths, parser order, identity continuity and buffer ownership.

## Independent existing-oracle catalogue audit

Read-only Python loads existing frozen source/world oracle functions. It never writes fixtures or
calls production certification. Tuple contains every independent input determining the complete
CombatSelectedResult; its loss records are deterministic from those fields. All8840 rows reduce
to637 tuples:141 accepted retreats,141 refusals,355 no-retreat,45 captured-attacker and125
captured-defender. Capture counts overlap retreat classifications. Source script output retained
in `/tmp/task009b-support-count.log` (8840/637); branch counts recorded from same enumeration.

```sh
python3 -B - <<'PY'
import importlib.util
s=importlib.util.spec_from_file_location('world','docs/specs/verify-combat-world-settlement-v1.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
v=set();count=0
for d in m.source.DIFFS:
 for a in m.source.COORDS:
  for b in m.source.COORDS:
   cap=a//10+a%10 in m.DATA['attacker_capture_sums'][str(d)] or b//10+b%10 in m.DATA['defender_capture_sums'][str(d)]
   for die in range(1,7) if cap else [None]:
    f=m.result_facts(d,a,b,die)
    for ref in range(f['requiredRetreat']+1):
     v.add((d,f['attackerPercent'],f['defenderPercent'],f['defenderPercent']+10*ref,f['rawEngaged'],f['requiredRetreat'],bool(ref),f['capturedRole'],f['captureShare'] if cap else None));count+=1
print(count,len(v),sum(x[5]==1 and not x[6] for x in v),sum(x[6] for x in v),sum(x[5]==0 for x in v),sum(x[7]=='attacker' for x in v),sum(x[7]=='defender' for x in v))
PY
```
Expected output: `8840 637 141 141 355 45 125`.

## Formatting and limits

```sh
dotnet format Sandtable.slnx --no-restore --include src/Cna.Core/Campaigns/CampaignCombatCertification.cs src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs src/Cna.Core/Campaigns/CampaignCombatIdentityModels.cs tests/Cna.Core.Tests/Campaigns/CombatIdentityTests.cs tests/Cna.Core.Tests/Cna.Core.Tests.csproj > /tmp/task009b-format.log 2>&1
dotnet format Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatCertification.cs src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs src/Cna.Core/Campaigns/CampaignCombatIdentityModels.cs tests/Cna.Core.Tests/Campaigns/CombatIdentityTests.cs tests/Cna.Core.Tests/Cna.Core.Tests.csproj > /tmp/task009b-format-verify.log 2>&1
```
Both exit0, empty logs. Full source formatting scoped to five paths. `git diff --check` exit0.

Caller remains responsible for actual Boundary authentication. API receives no current RNG cursor,
complete prior authority head or full root; no cursor/version headroom or whole successor-root
capacity claim.010/011 require genuine selection/decline/Base2;012–016 own events/settlement;
later actual positive assault composition, public privacy and HOST-PUB-001 remain outside scope.
No parent completion claim. Final corrected fullgate and three fresh isolated reviews are root-owned.

## Final formatted-source freeze

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '-bl:/tmp/task009b-frozen-{}.binlog' > /tmp/task009b-frozen.log 2>&1
```
Exit0:14passed/0failed/0skipped,12.331seconds. Binlogs:
`/tmp/task009b-frozen-20260920-074107--12904--oI0f1Y.binlog` and
`/tmp/task009b-frozen-20260920-074111--12904--8Lxs2T-dotnet-test.binlog`.
All worker processes closed before handing root the source freeze. No later code edits.

```text
2d3859c248783d07772753b5105848c4555cd7b7e146ea7cd9d740316cb705da  src/Cna.Core/Campaigns/CampaignCombatCertification.cs
6c189f8f79c532af6892c3071ee34d8ad44267ee6182118fdd2919fad54d9b8f  src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs
c0146b0b44278ff9b7c98ffc1c3c891b6ef24939db5662833ffb480c16268ed5  src/Cna.Core/Campaigns/CampaignCombatIdentityModels.cs
8dbb6656c65863ddce8e842078746be41db754bab115e54f7b740ee63ca32ff7  tests/Cna.Core.Tests/Campaigns/CombatIdentityTests.cs
5765bf3dba7a7f9da11f153630d25007422af109bc1d95326ec447c335c6e7af  tests/Cna.Core.Tests/Cna.Core.Tests.csproj
```
