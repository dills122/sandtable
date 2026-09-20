# Task010A worker evidence

Scope: direct actual-history two-event empty selection and six-event no-attack traversal. Parent010 remains open for010B timed synthetic mechanism and010C generic HistoryReplay routing. No new Snapshot12 writer, positive actual-history admission, Prepared011, release action or host publication claim. Worker changed exactly five primary paths; root owns other documentation changes, full gates and reviews.

## Implementation

- `CampaignCombatInheritedSelection.cs`: immutable local Control, Command/Input, owned Result; Replay/Apply/ReadControl always require retained Created plus complete actual G2 predecessor. Private transition only. Two local events; exact prior retries retain original bytes without version advance.
- `CampaignCombatInheritedNoAttack.cs`: same causal authority for six structural completions to same-slot Reserve Release. Frozen nested SelectionControl retains stepIndex0/segmentClosedfalse even when outer traversal closes. Cached Control optional/advisory, strict syntax then whole-byte equality to actual replay. Cached input byte bound checked before clone.
- `CampaignCombatIdentityCodec.cs`: frozen local writers and grammar reuse existing 51-object Boundary closure. Separate local descriptors, no generalized schema framework. Effect unions checked before trusted history. Receipt domains and input hashes remain family-specific. Canonical read order: bounds/closed shape/primitive syntax/canonical bytes, then trusted replay, then full bytes. Local arrays preserve semantic order; nested inherited external keyed arrays preserve frozen sorting.
- `CombatInheritedStepsTests.cs`: 7 focused facts. Four actual terminal G2 histories, actors axis/commonwealth after6/7 moves. Reconstruct exact typed bytes and compare independent frozen hash/length commitments:28 selection +60 traversal =88. Fixture contains commitment bytes/sha256, not literal Control JSON. History commitment includes serialized request, retained Created and entire predecessor events. Traversal first commitment hashes reconstructed selection commitment object.
- Core test csproj adds only inherited-no-attack-v1 fixture link; selection fixture already linked by009A.

## Runtime evidence

All four actual histories: predecessor20/21 events; add2+6 events -> cumulative28/29 events and authority29/30. Restores all40 Control cuts (12 selection +28 traversal), retries every previously applied local input at every later cut (12 selection +84 traversal retries). All32 emitted local events included in golden checks. Full serialized Boundary and frozen nested SelectionControl remain equal at every cut, covering World/RNG/CP/TOE/ammunition/receipt/progress proof invariance. Exact routes end at Reserve Release; no release/reset occurs.

Negatives cover mandatory Created/full G2, missing/reordered/duplicate local tails, incomplete selection, postclose advance, foreign lawful predecessor histories, changed/stale/unauthorized commands, altered domains/head/assessment/receipt/step/disposition proofs including recomputed receipts, forged Control and advisory cache, root/nested shape/primitive/canonical spelling, unknown effect union, oversized/deep/dotted-key513 arrays,2/6 local capacities before indexing, defensive result/state bytes. Sentry histories distinguish malformed syntax from trusted replay access; valid canonical bytes reach sentry. Actual ownership regression proves local list Count/index cannot mutate caller Created used downstream: capture retains Created first and passes same owned span into predecessor replay.

## TDD and exact commands

All test commands run with `login:false`, authorized escalated local IPC, unique MSBuild binlogs. No full suite run by worker.

1. Initial RED, exit1: missing new modules/SerializeSelectionControl, expected compile failure. `/tmp/task010a-red.log`.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatInheritedStepsTests' '-bl:/tmp/task010a-red-{}.binlog' > /tmp/task010a-red.log 2>&1
```

2. First implementation build exit1: two CA1826 errors, FirstOrDefault/LastOrDefault on IReadOnlyList. Fixed indexed access; failure retained `/tmp/task010a-green.log`.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatInheritedStepsTests' '-bl:/tmp/task010a-green-{}.binlog' > /tmp/task010a-green.log 2>&1
```

3. Initial runtime GREEN1/1,3.555s,exit0. This run only proved initial behavior, not88 commitments.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatInheritedStepsTests' '-bl:/tmp/task010a-green2-{}.binlog' > /tmp/task010a-green2.log 2>&1
```

4. Expanded GREEN6/6,32.437s,exit0. All88 commitments/strict causal negatives included.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatInheritedStepsTests' '-bl:/tmp/task010a-expanded-{}.binlog' > /tmp/task010a-expanded.log 2>&1
```

5. Ownership RED1failure,3.243s,exit2. Local list index zeroed caller Created, predecessor replay threw `Created11 differs from canonical trusted creation evidence.` Fixed capture to retain owned Created before any local-list Count/index and pass owned span downstream. Test later strengthened to mutate from Count as well.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatInheritedStepsTests' --filter-method '*CreatedIsCapturedBeforeAdversarialLocalTailIndexing' '-bl:/tmp/task010a-ownership-red-{}.binlog' > /tmp/task010a-ownership-red.log 2>&1
```

6. Ownership/full focused GREEN7/7,34.427s,exit0, before final Count mutation strengthening/format.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatInheritedStepsTests' '-bl:/tmp/task010a-final-{}.binlog' > /tmp/task010a-final.log 2>&1
```

7. Scoped format,exit0,empty log.

```sh
dotnet format Sandtable.slnx --no-restore --include src/Cna.Core/Campaigns/CampaignCombatInheritedSelection.cs src/Cna.Core/Campaigns/CampaignCombatInheritedNoAttack.cs src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs tests/Cna.Core.Tests/Campaigns/CombatInheritedStepsTests.cs tests/Cna.Core.Tests/Cna.Core.Tests.csproj > /tmp/task010a-format.log 2>&1
```

## Frozen candidate verification

Final source frozen. Focused7/7,36.320s,exit0; existing Identity regression14/14,12.104s,exit0. Root must run full gates/fresh reviews before accepting010A.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatInheritedStepsTests' '-bl:/tmp/task010a-frozen-{}.binlog' > /tmp/task010a-frozen.log 2>&1
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatIdentityTests' '-bl:/tmp/task010a-identity-regression-{}.binlog' > /tmp/task010a-identity-regression.log 2>&1
dotnet format Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatInheritedSelection.cs src/Cna.Core/Campaigns/CampaignCombatInheritedNoAttack.cs src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs tests/Cna.Core.Tests/Campaigns/CombatInheritedStepsTests.cs tests/Cna.Core.Tests/Cna.Core.Tests.csproj > /tmp/task010a-format-verify.log 2>&1
git diff --check
```

SHA256 manifest:

```text
2fb6e84ba31978aa2069493a156435cf46bf878db45dc23604bd043aadf58a23  src/Cna.Core/Campaigns/CampaignCombatInheritedSelection.cs
c72d83ad8b8eae48ff1532b5f1c62b1c631180d75902fde83e99c08aa29d216c  src/Cna.Core/Campaigns/CampaignCombatInheritedNoAttack.cs
c71ebec62358cf4cf7d8115b32432482441a45f61ed005119240d9709c2a6d86  src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs
bf615e98cf11befcb7aadb099cd38718d861915ba8d5ebafa41db4d21396b2e2  tests/Cna.Core.Tests/Campaigns/CombatInheritedStepsTests.cs
8d5fc10e3fa74e26d6aadb0c7859fc1d5333d0fb99ac20429fa2f99e9f85816a  tests/Cna.Core.Tests/Cna.Core.Tests.csproj
```

## Descriptor audit

Exact local descriptor comparison returned `Exact frozen descriptor matches: 13`:7 selection objects and6 traversal objects (shared Receipt counted per contract). Existing CandidateAssessment and AdmissionBoundary skipped as unchanged prior audited grammar. Effect tags manually agree with frozen effectTags. Residual maintenance cost: private descriptor mirrors must change with any future wire contract; independent88 frozen commitments and strict syntax sentries guard current shape/order, and prior Identity tests protect Boundary delegation.

```sh
python3 - <<'PY'
import json,re,pathlib
code=pathlib.Path('src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs').read_text()
shapes=dict(re.findall(r'\["([^"]+)"\] = "([^"]+)"',code))
selection={'Command':'InheritedSelectionCommand','Input':'InheritedSelectionInput','Event':'InheritedSelectionEvent','OpenEffect':'InheritedSelectionOpen','CloseEffect':'InheritedSelectionClose','Receipt':'PreambleReceipt','Control':'InheritedSelectionControl','Effect':'InheritedSelectionEffect'}
traversal={'Command':'InheritedTraversalCommand','Input':'InheritedTraversalInput','Event':'InheritedTraversalEvent','StepEffect':'InheritedTraversalEffect','Receipt':'PreambleReceipt','Control':'InheritedTraversalControl','Effect':'InheritedTraversalEffect','SelectionControl':'InheritedSelectionControl'}
count=0
for path,mapping in [('combat-inherited-selection-v1.schema.json',selection),('combat-inherited-no-attack-v1.schema.json',traversal)]:
 for name, shape in json.loads(pathlib.Path('docs/specs',path).read_text())['objects'].items():
  if name not in mapping: print('external existing object',name);continue
  def convert(field):
   key,kind=field.split(':');suffix='[]' if kind.endswith('[]') else '?' if kind.endswith('?') else '';base=kind[:-len(suffix)] if suffix else kind
   return key+':'+mapping.get(base,base)+suffix
  expected=' '.join(map(convert,shape.split()))
  actual=shapes[mapping[name]]
  assert expected==actual,(name,expected,actual)
  count+=1
print('Exact frozen descriptor matches:',count)
PY
```

## Retained binary logs

```text
/tmp/task010a-expanded-20260920-081635--15581--ox4lso.binlog
/tmp/task010a-expanded-20260920-081644--15581--vQl26u-dotnet-test.binlog
/tmp/task010a-final-20260920-081846--15732--OcIHmo.binlog
/tmp/task010a-final-20260920-081855--15732--u232uT-dotnet-test.binlog
/tmp/task010a-frozen-20260920-082015--15839--cOTVSa.binlog
/tmp/task010a-frozen-20260920-082023--15839--lPMP0W-dotnet-test.binlog
/tmp/task010a-green-20260920-081020--15208--28_GhL.binlog
/tmp/task010a-green2-20260920-081316--15415--rnqISm.binlog
/tmp/task010a-green2-20260920-081324--15415--AT63d+-dotnet-test.binlog
/tmp/task010a-identity-regression-20260920-082201--15915--+csBy5-dotnet-test.binlog
/tmp/task010a-identity-regression-20260920-082201--15915--zARzig.binlog
/tmp/task010a-ownership-red-20260920-081758--15684--aFSjfm.binlog
/tmp/task010a-ownership-red-20260920-081803--15684--8dcDNN-dotnet-test.binlog
/tmp/task010a-red-20260920-080627--15033--hbUqwP.binlog
```

Final format verification exit0, empty log; git diff --check exit0. All worker-owned test/build/format sessions completed before handoff. No source changes after frozen SHA manifest. Root notified and cleared to begin integration/full gates/review1.
