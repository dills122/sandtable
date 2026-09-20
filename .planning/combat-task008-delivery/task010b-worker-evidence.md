# Task010B worker evidence

Scope: five authorized primary files implementing dormant C3a trusted-Boundary selection/RBA/no-attack mechanics. Actual positive-history admission,010C routing,011 Prepared/seals, commitment/results/settlement, Snapshot12 extension and publication remain outside scope. Parent owns docs/fullgate/reviews/git. No worker commits, agents, reviews or full suite.

## Implementation and provenance

- New SelectionStepsModels: immutable typed Boundary/Weather/Timing/Window/Command/Input/effect union/Control/Result. Collections own read-only copies; returned event buffers copied. Boundary represents only idle Breakdown/null Reaction. Construction does not establish provenance.
- New SelectionSteps engine: eight command kinds/six effects, independently trusted Boundary plus separate authenticated accepted Inputs/events; exact compatible C2 creationBinding pinned to normative request. Retained Created still independently validated; no runtime fixture dependency or actual positive-history claim.009B supplies current-profile and exhaustive result support certification. Full current CP0…10 preserved, no reset. Separate selection/RBA windows and frozen Timing1 high-water rules; Command-only retry hash with actor checked separately; accepted/duplicate/no-op distinguished.
- New SelectionStepsCodec: all frozen16 local object descriptors, Timing1/Idle plus49-object transitive closure; canonical writers and shape/bounds/canonical-before-trusted readback. Whole Boundary/current World typed validation before CP-only internal JSON serialization mapping. Raw Boundary/Control never becomes authority; equality compares to independently trusted values/replay. Event.input is not used to supply trusted admission.
- IdentityCodec limited named syntax bridge: preserves C3a semantic array order recursively while default inherited ordering unchanged; explicitly rejects C3a Route and LegacyBrokenVehicleLot primitive arms before trusted context. No copied51-object World grammar or global framework.
- New CombatStepsTests,12 facts: two literal Boundaries, five literal traces/41events/five final Controls,46 control restore cuts including starts;10 supplemental both-role mirrors/82events/92 restored cuts including starts. Mirror counts include repeated Axis mechanism evidence, not additional independent original private literals. Both-role finals compare frozen oracle-derived commitments. Exact Command retries with changed valid time/clock metadata keep retained bytes/state, with separate actor authorization.

All selected positive paths stop atFA after three exact closures; no-attack/cancelled paths finish six closures. New pre-window-unavailability completion tests cover null-window opening and pre-RBA cancellation through all remaining no-attack steps without synthetic decline. Frozen Boundary/current World/resources/RNG unchanged throughout. Existing actual010A history and009B all-result proof retained as cumulative evidence, not reclassified as positive C3a provenance.

## TDD and failures retained

All .NET commands native MTP, `--project`, `login:false`, authorized escalated local IPC and unique MSBuild binlogs.

1. Initial RED exit1, `/tmp/task010b-red.log`: exact literal Input roundtrip test failed compilation because new codec absent.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatStepsTests' '-bl:/tmp/task010b-red-{}.binlog' > /tmp/task010b-red.log 2>&1
```

2. First implementation build exit1, `/tmp/task010b-green1.log`: CA1861 constant array in repeatedly called step proof selection and CA1859 dictionary concrete type. Fixed with step switch and Dictionary declaration; no contract changes. Author also corrected PositionBytes wrapper to open/close object before first literal transition run.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatStepsTests' '-bl:/tmp/task010b-green1-{}.binlog' > /tmp/task010b-green1.log 2>&1
```

3. Literal GREEN2/2,2.702s,exit0. All41 exact event bytes and five final Controls matched on first runtime trace attempt; all post-event cuts/retries passed.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatStepsTests' '-bl:/tmp/task010b-literals-{}.binlog' > /tmp/task010b-literals.log 2>&1
```

4. Expanded GREEN8/8,4.046s,exit0: supplemental mirrored oracle finals, clock matrix, provenance, strictness and ownership.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatStepsTests' '-bl:/tmp/task010b-expanded-{}.binlog' > /tmp/task010b-expanded.log 2>&1
```

5. Root dev primitive audit identified inherited Route grammar too broad for C3a. Failure-sensitive RED1failure/1.192s,exit2: complete syntactically shaped World Settlement with empty disposition Route reached null trusted context (`ArgumentNullException`) instead of failing syntax (`JsonException`). Root cause: authority-envelope oracle explicitly forbids Route before World semantic checks. Fix confined to C3a recursive syntax mode; default inherited Route behavior preserved. Test also checks LegacyBrokenVehicleLot rejection.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatStepsTests' --filter-method '*C3aForbiddenRoutesRejectBeforeTrustedBoundaryContext' '-bl:/tmp/task010b-route-red-{}.binlog' > /tmp/task010b-route-red.log 2>&1
```

6. GREEN12/12,3.972s,exit0. Added array-order sentries reaching trusted context for reversed World.elements/Position.sources and unsorted component IDs, rehashed effect proof/participant/receipt substitutions, postclose changed mutation/stale callbacks, full pre-window unavailability completions. Then added initial-control Serialize→Read→exact bytes to literal/mirror start cuts.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatStepsTests' '-bl:/tmp/task010b-route-green-{}.binlog' > /tmp/task010b-route-green.log 2>&1
```

7. Scoped format exit0, empty log; final formatted focused12/12,4.177s,exit0.

```sh
dotnet format Sandtable.slnx --no-restore --include src/Cna.Core/Campaigns/CampaignCombatSelectionStepsModels.cs src/Cna.Core/Campaigns/CampaignCombatSelectionSteps.cs src/Cna.Core/Campaigns/CampaignCombatSelectionStepsCodec.cs src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs tests/Cna.Core.Tests/Campaigns/CombatStepsTests.cs > /tmp/task010b-format.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatStepsTests' '-bl:/tmp/task010b-frozen-{}.binlog' > /tmp/task010b-frozen.log 2>&1
```

Shared regressions/format verification status appended at handoff. Commands:

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '*CombatInheritedStepsTests' '-bl:/tmp/task010b-shared-regression-{}.binlog' > /tmp/task010b-shared-regression.log 2>&1
dotnet format Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatSelectionStepsModels.cs src/Cna.Core/Campaigns/CampaignCombatSelectionSteps.cs src/Cna.Core/Campaigns/CampaignCombatSelectionStepsCodec.cs src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs tests/Cna.Core.Tests/Campaigns/CombatStepsTests.cs > /tmp/task010b-format-verify.log 2>&1
git diff --check
```

## Mirror oracle provenance

Existing `verify-combat-side-projection-v1.py:388–416 mirror_steps` applied to five original fixture sources for each side; frozen C3a oracle folds its returned trusted Inputs. Output `/tmp/task010b-mirror-oracle.json`; five Commonwealth final hashes retained in test. Original Axis final hashes remain literal fixture expectations. No repo fixture edits. First exploratory command tried nonexistent `mirror_reference.sha`, raised AttributeError; corrected to standard hashlib SHA256, no source/runtime changes.

```sh
python3 -B - <<'PY'
import importlib.util,json,pathlib,hashlib
p=pathlib.Path('docs/specs/verify-combat-side-projection-v1.py');s=importlib.util.spec_from_file_location('mirror_reference',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
f=json.loads(pathlib.Path('docs/specs/fixtures/combat-selection-steps-v1.json').read_text());values=[]
for side in ('axis','commonwealth'):
 for row in f['cases']:
  source={'name':row['name'],'base':json.loads(f['boundaries'][row['boundaryIndex']]['canonicalUtf8']),'inputs':row['inputs']}
  mirror=m.mirror_steps(source,side);state=m.steps.initial(mirror['base'])
  for inp in mirror['inputs']:state,_,_=m.steps.transition(mirror['base'],state,inp)
  result={'name':mirror['name'],'controlHash':'sha256:'+hashlib.sha256(m.encode(state)).hexdigest(),'controlBytes':len(m.encode(state)),'events':len(mirror['events'])};values.append(result)
print(json.dumps(values,indent=2));pathlib.Path('/tmp/task010b-mirror-oracle.json').write_text(json.dumps(values,indent=2)+'\n')
PY
```

## Transitive descriptor/primitive audit

Exact descriptor comparison found49 reachable object descriptors, including all16 local C3a descriptors, Timing and Idle. Primitive closure: LegacyBrokenVehicleLot,Route,actor,bool,futureTurn,hash,id,int,locator,long,null,rawHash,side,string,ulong,utc. Authority-envelope oracle48–78 supplies primitive rules: signed32/signed64/UInt64, futureTurn1…115, UTC0…253402300799999, ASCII bounded IDs/hashes/strings, **unconditionally forbidden Route/LegacyBrokenVehicleLot**. Descriptor equality alone misses forbidden special arms; regression RED/GREEN above covers that distinction. All C3a arrays preserve order, while trusted profile comparison determines semantic validity. Residual maintenance: frozen private descriptors must track any future versioned contract; no automatic universal reader introduced.

```sh
python3 -B - <<'PY'
import importlib.util,pathlib,re
p=pathlib.Path('docs/specs/verify-combat-selection-steps-v1.py');spec=importlib.util.spec_from_file_location('steps_audit',p);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
source='\n'.join(pathlib.Path(p).read_text() for p in ['src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs','src/Cna.Core/Campaigns/CampaignCombatSelectionStepsCodec.cs'])
shapes=dict(re.findall(r'\["([^"]+)"\] = "([^"]+)"',source))
seen=set();primitives=set()
def walk(kind):
 if kind.endswith('?'):return walk(kind[:-1])
 if kind.endswith('[]'):return walk(kind[:-2])
 if kind=='Effect':
  for value in m.TAGS.values():walk(value)
  return
 if kind in seen:return
 if kind not in m.SCHEMA:primitives.add(kind);return
 seen.add(kind);expected=' '.join(k+':'+v for k,v in m.SCHEMA[kind]);assert shapes[kind]==expected,(kind,shapes.get(kind),expected)
 for key,child in m.SCHEMA[kind]:walk(child)
for root in ('Boundary','Input','Event','Control'):walk(root)
print('Exact transitive object descriptors:',len(seen));print('Primitive closure:',','.join(sorted(primitives)));print('Route and LegacyBrokenVehicleLot require explicit C3a rejection; all arrays preserve order.')
PY
```

## Frozen primary SHA256

```text
7e9ed3131d672ff67f02332331ac4aab5f68736e3204a4a2e19933baa5b3a1d8  src/Cna.Core/Campaigns/CampaignCombatSelectionStepsModels.cs
32551f54656c63f69d2517f7ab446fec4951679ff311154a26df84940c009efb  src/Cna.Core/Campaigns/CampaignCombatSelectionSteps.cs
38e7ffdcdf2fcdef2632b5a45f0d87d4fe49729cf0828c2fb902ef88d585cb94  src/Cna.Core/Campaigns/CampaignCombatSelectionStepsCodec.cs
f34ecade7cb4893678097622dd1968d5a8f46e84c2ffd45e8282198b1a56e13f  src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs
690c1bdd49fb067e39c761ed19008a96bbea811517541a053fcb97dca65b5c0c  tests/Cna.Core.Tests/Campaigns/CombatStepsTests.cs
```

No code changes after manifest. Root must perform full gates/dev review/three independent rounds before acceptance. No claim of actual positive-history admission, extended Snapshot12,010C routing,011 Prepared or host publication.

## Final shared regression and retained binary logs

Shared Identity/InheritedSteps regression21/21,41.384s,exit0. Final12-test run includes explicit initial readback assertions for all5 original and10 mirrored starts.

```text
/tmp/task010b-expanded-20260920-090549--19004--8rrb97.binlog
/tmp/task010b-expanded-20260920-090554--19004--K7q9Xs-dotnet-test.binlog
/tmp/task010b-frozen-20260920-091124--19305--cpCvqQ.binlog
/tmp/task010b-frozen-20260920-091132--19305--HD_ZAu-dotnet-test.binlog
/tmp/task010b-green1-20260920-085809--18653--79nyI3.binlog
/tmp/task010b-literals-20260920-090022--18776--dovOVC.binlog
/tmp/task010b-literals-20260920-090031--18776--Pcml9Y-dotnet-test.binlog
/tmp/task010b-red-20260920-085100--18330--PFnuIF.binlog
/tmp/task010b-route-green-20260920-090925--19178--eVZ5eG.binlog
/tmp/task010b-route-green-20260920-090933--19178--GOVJWK-dotnet-test.binlog
/tmp/task010b-route-red-20260920-090758--19105--blfbBN.binlog
/tmp/task010b-route-red-20260920-090803--19105--Dn0CV8-dotnet-test.binlog
/tmp/task010b-shared-regression-20260920-091156--19327--j+Js5f.binlog
/tmp/task010b-shared-regression-20260920-091157--19327--S3eHFP-dotnet-test.binlog
```

Mirror source/output SHA256 pins:

```text
964f4b6a6901e446e5d0e0a6c49e79e778677ae458947681034a4ddd5d848a0d  docs/specs/verify-combat-side-projection-v1.py
dc037282f74c34246d350faebeea55e81677d27dd181de858b8270e4eaa73c86  docs/specs/verify-combat-selection-steps-v1.py
151c8da5037dfd5fa921abf1f675ef1a10211f8b84a4e1fb385d6beb62b1d0de  docs/specs/fixtures/combat-selection-steps-v1.json
c7d7e0a2bdceb4029b99a638183e960da1beaaba559469ae3744baf91a03b5a8  /tmp/task010b-mirror-oracle.json
```

Final scoped format verification exit0, empty log; git diff --check exit0. Every worker-owned test/build/format process closed before frozen handoff. Source hashes unchanged; root notified and cleared for fullgate/review1.
