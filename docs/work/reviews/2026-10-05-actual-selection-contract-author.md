# Author explanation: actual-selection executable contract

Author testimony only; no independent readiness verdict. Date2026-10-05 America/Toronto.
Initial base `96596dde066b0d8c9a0110eba50fcfcb01d99a46`, initial implementation checkpoint `3f1dfc967a14dddb98bbe02d4e9b692fd535cb6d`, branch `codex/combat-actual-selection-contract`.

## Research R1 Diagnostic Correction And Current Freeze

Coordinator accepted high research R1 after set1/pass3 Ready at e5645c4. Total3of9 engineering
reviews consumed; recoveryR1ofmax2 used. Alert2 / check111848049126 at that exact head was
triaged as a trusted-name heuristic on fixed oracle inputs; no real secret was identified.
The CI failure remained a delivery blocker. Research did not authorize suppression or merge.
Recovery decision packet: `/private/tmp/s3-codeql-recovery-r1.md`; its substantive decision and
recommended repair are retained here. PR161 remains pending corrected-head review and CI.

Corrected behavior checkpoint `970fa4184b6aed787b545a6c3c7b751010add1fc` changes only the final state_clock_precedence_checks
diagnostic block: output mismatch count only; assertion carries count and fixed message only.
All mismatch comparisons/collection, test loops/counts and nonempty rejection remain unchanged.
No trust-helper rename, payload encoding, suppression/dismissal, CI alteration or successful literal
regeneration. Fixed-input heuristic diagnosis is limited to this private oracle, not future telemetry.

Sentinel-bearing forced mismatch RED exposed the harmless sentinel in stdout/stderr/assertion;
GREEN still asserts failure with count1 and excludes the sentinel from all three channels. AST
extraction executes the actual committed final diagnostic block with a sentinel in label/receipt.
Independent in-memory restoration of the arm/segment defect fails its focused probe (0.440s);
restoration of the state/clock defect fails with910 mismatches (17.137s), count-only stdout and
assertion. Each mutation restores the original transition afterward; disposable tests do not edit
repository behavior. Complete scripts and captured results are retained in the author packet.

Direct `python3 -B docs/specs/verify-combat-actual-selection-v1.py`: PASS exit0,
221.945s; stdout SHA256 `0ec2ecc01734e319a6abd590a7bf1484cf5569d657874981e893a6c93d8cc874`. Successful output/counts exactly match
the prior full run; spec/schema/fixture byte-identical to e5645c4. Static16 admitted pins match,
and exact diff equivalence proves only the recommended diagnostic block changed in the oracle.
Both current primary manifests match file bytes; whitespace checks pass.

```text
PASS: 16 semantic actual-owner traces; selected FA20 and seven Reserve Release fallbacks per owner
PASS: 16 literal traces; {"capacity": 23, "clock": 50, "clock-accepted": 8, "cuts": 152, "entry": 36, "entry-cuts": 6, "entry-leaf": 1512, "event": 4456, "family": 17, "gate-actor-state": 1216, "gate-arm-state": 1216, "gate-candidate-clock": 14, "gate-choice-candidate": 14, "gate-decision-clock": 304, "gate-owner-clock": 304, "gate-participant-clock": 6, "gate-position-clock": 304, "gate-primitive-state": 2432, "gate-segment-state": 1216, "gate-stale-clock": 304, "gate-version-clock": 608, "gate-version-state": 1216, "history": 140, "ledger": 320, "legacy-reject": 6, "no-op": 30, "order": 48, "order-arm": 210, "order-primitive-arm": 70, "order-segment-clock": 16, "order-version-segment": 32, "ownership": 64, "pins": 128, "positive": 2, "privacy": 30, "proof": 8266, "raw": 2760, "retries": 1962, "retry-primitive": 654, "separation": 2, "state-clock-matrix": 6080, "state-clock-public": 276, "trust": 2}; full original actual entry retained; separate trusted ledger; private FA stop only
```

Next fresh review is coordinator-dispatched set2/pass1,total4of9; no count reset. Max9 engineering
reviews/max2 authorized recovery spikes; R1 used. Canonical plan ownership returns to coordinator.
Original predecessor evidence remains reused honestly: separate Breakdown/cycle/Snapshot/outward
pin failures retained and historical timeouts unverified. No new full-suite lease/.NET/Boundary/
format/CI or clean-CodeQL claim. No self-dispatched reviewer, merge or S4. Exact final administrative
head supplied by coordinator dispatch; verify implementation and administrative commits together.

## Durable R1 Tests And Captured Evidence

`s3-r1-sentinel.py`: 1060 bytes; SHA256 `f819951af0ae2f0aa4d7fa3800d74015406150d46159709561c6fa22dc05d457`.

```python
import ast,contextlib,io,json,traceback
from pathlib import Path
p=Path('docs/specs/verify-combat-actual-selection-v1.py')
f=next(n for n in ast.parse(p.read_text()).body if isinstance(n,ast.FunctionDef) and n.name=='state_clock_precedence_checks')
start=next(i for i,n in enumerate(f.body) if isinstance(n,ast.If) and isinstance(n.test,ast.Name) and n.test.id=='failures')
code=compile(ast.fix_missing_locations(ast.Module(body=f.body[start:],type_ignores=[])),str(p),'exec')
sentinel='HARMLESS_S3_SENTINEL_627';out=io.StringIO();err=io.StringIO();message='';failed=False
with contextlib.redirect_stdout(out),contextlib.redirect_stderr(err):
    try:exec(code,{'failures':[(sentinel,('duplicate',sentinel),('error',5))]})
    except AssertionError as e:
        failed=True;message=str(e);traceback.print_exc()
result={'failureDetectable':failed,'stdout':out.getvalue(),'stderr':err.getvalue(),'assertion':message,'sentinelAbsent':all(sentinel not in v for v in [out.getvalue(),err.getvalue(),message])}
print(json.dumps(result,sort_keys=True));assert failed
```

`s3-r1-sensitivity.py`: 1740 bytes; SHA256 `d193bab53e665f22583adce938dd8cd5a24ffeccb95a32b7ca6321a44dfee754`.

```python
import ast,contextlib,importlib.util,io,json,time
from pathlib import Path
p=Path('docs/specs/verify-combat-actual-selection-v1.py').resolve();s=p.read_text()
sp=importlib.util.spec_from_file_location('s3_recovery',p);a=importlib.util.module_from_spec(sp);sp.loader.exec_module(a)
node=next(n for n in ast.parse(s).body if isinstance(n,ast.FunctionDef) and n.name=='transition')
original=ast.get_source_segment(s,node)
segment="    require(cmd['segmentId'] == prior['segmentId'], 4)\n"
arm=original.replace(segment,'');anchor="    for k in ('decisionId', 'fromPositionId', 'expectedPriorVersion', 'choice', 'candidate', 'participant'):"
assert arm.count(anchor)==1;arm=arm.replace(anchor,segment+anchor)
clock=original.replace("        require(inp['admittedAt'] is None and inp['clockAvailable'], 5)\n",'')
anchor="    if kind == 'open-segment':";assert clock.count(anchor)==1
clock=clock.replace(anchor,"    if kind in ('complete-step', 'close-empty-selection'): require(inp['admittedAt'] is None and inp['clockAvailable'], 5)\n"+anchor)
results=[]
for name,source,check in [('arm-before-segment',arm,a.arm_segment_order_checks),('state-before-clock',clock,a.state_clock_precedence_checks)]:
    exec(compile(source,str(p),'exec'),vars(a));counts={};out=io.StringIO();start=time.monotonic();failed=False;message=''
    with contextlib.redirect_stdout(out):
        try:check(counts)
        except AssertionError as e:failed=True;message=str(e)
    results.append({'mutation':name,'rejected':failed,'seconds':round(time.monotonic()-start,3),'stdout':out.getvalue(),'assertion':message,'passedChecksBeforeRejection':counts})
    assert failed,name
    exec(compile(original,str(p),'exec'),vars(a))
print(json.dumps(results,sort_keys=True))
```

`s3-r1-sentinel-red.json`: 1005 bytes; SHA256 `cffef7b1c70e5695ecf18a87270f8bb75e368da3b1b6c802f1df5c0227ed58c2`.

```json
{"assertion": "(1, 'state/clock precedence mismatches', [('HARMLESS_S3_SENTINEL_627', ('duplicate', 'HARMLESS_S3_SENTINEL_627'), ('error', 5))])", "failureDetectable": true, "sentinelAbsent": false, "stderr": "Traceback (most recent call last):\n  File \"/private/tmp/s3-r1-sentinel.py\", line 9, in <module>\n    try:exec(code,{'failures':[(sentinel,('duplicate',sentinel),('error',5))]})\n        ~~~~^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^\n  File \"docs/specs/verify-combat-actual-selection-v1.py\", line 891, in <module>\n    assert not failures,(len(failures),'state/clock precedence mismatches',failures[:4])\n           ^^^^^^^^^^^^\nAssertionError: (1, 'state/clock precedence mismatches', [('HARMLESS_S3_SENTINEL_627', ('duplicate', 'HARMLESS_S3_SENTINEL_627'), ('error', 5))])\n", "stdout": "STATE/CLOCK MISMATCH GROUPS: {'HARMLESS_S3_SENTINEL_627': 1}\nSTATE/CLOCK MISMATCH: ('HARMLESS_S3_SENTINEL_627', ('duplicate', 'HARMLESS_S3_SENTINEL_627'), ('error', 5))\n"}
```

`s3-r1-sentinel-green.json`: 673 bytes; SHA256 `5e9d3d9b57f49ec95811dba8d5932d1c755a009656821b5963817e6b2e085ec0`.

```json
{"assertion": "(1, 'state/clock precedence mismatches')", "failureDetectable": true, "sentinelAbsent": true, "stderr": "Traceback (most recent call last):\n  File \"/private/tmp/s3-r1-sentinel.py\", line 9, in <module>\n    try:exec(code,{'failures':[(sentinel,('duplicate',sentinel),('error',5))]})\n        ~~~~^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^\n  File \"docs/specs/verify-combat-actual-selection-v1.py\", line 888, in <module>\n    assert not failures,(len(failures),'state/clock precedence mismatches')\n           ^^^^^^^^^^^^\nAssertionError: (1, 'state/clock precedence mismatches')\n", "stdout": "STATE/CLOCK MISMATCH COUNT: 1\n"}
```

`s3-r1-sensitivity.json`: 823 bytes; SHA256 `bd5245996e466c4eb1422b5996b0007a5fe25c8cbbfaf9cbe36b70f50dd31ad7`.

```json
[{"assertion": "('order-arm/axis/open-segment/decisionId', 'CMB-ASE-004', 3, '')", "mutation": "arm-before-segment", "passedChecksBeforeRejection": {}, "rejected": true, "seconds": 0.44, "stdout": ""}, {"assertion": "(910, 'state/clock precedence mismatches')", "mutation": "state-before-clock", "passedChecksBeforeRejection": {"gate-actor-state": 1216, "gate-arm-state": 1216, "gate-candidate-clock": 14, "gate-choice-candidate": 14, "gate-decision-clock": 304, "gate-owner-clock": 304, "gate-participant-clock": 6, "gate-position-clock": 304, "gate-primitive-state": 2432, "gate-segment-state": 1216, "gate-stale-clock": 304, "gate-version-clock": 608, "gate-version-state": 1216, "state-clock-matrix": 5352, "state-clock-public": 94}, "rejected": true, "seconds": 17.137, "stdout": "STATE/CLOCK MISMATCH COUNT: 910\n"}]
```

`s3-r1-static.json`: 112 bytes; SHA256 `77c338c7b40a77caf6d9e7582aeb33c1ed6d46bd0cdb321be282f1464685ae3f`.

```json
{"dependencyPinsMatched": 16, "specSchemaFixtureUnchanged": true, "onlyRecommendedDiagnosticBlockChanged": true}
```

## Historical Pass2 Reconciliation And Review Freeze

S3 set1/pass2,total2of9 at5e2ef795f89335988d424c37d2648acddd992681 returned **Not ready**
with one P2: active-state006 and required defender decline006 must precede clock005 for
complete-step/close-empty-selection. Coordinator/author **Accept**. Behavior checkpoint
`ce367d0ae4199b6adba1d2d0631e39a5f4b4ec1e` moves the existing untimed guards after those checks. Positive FA completion007
still follows clock005. Specification/schema/fixture are byte-identical to5e2ef795; no regeneration.
The prior pass1 correction remains included. No wider refactor or spec weakening.

Independent documented expected-gate model exercises all8 command kinds over16 traces/152cuts
and five clock inputs:6,080 private transition checks,276 public nonclosed complete/close probes,
and9,154 adjacent-gate checks (15,510 additions). Covers primitive/arm/segment/actor/version/
decision/stale/duplicate/candidate/choice/participant precedence. Expected outcomes do not call
transition or its clock gate. Retained RED:728 matrix+182 public mismatches,910 total; no other
adjacent mismatches. Focused GREEN and direct full oracle pass. Existing counts remain preserved.

Direct command: `python3 -B docs/specs/verify-combat-actual-selection-v1.py`.
Exit0,233.099s; stdout SHA256 `0ec2ecc01734e319a6abd590a7bf1484cf5569d657874981e893a6c93d8cc874`.
This direct result supersedes historical pass1/initial timing for the current freeze.

```text
PASS: 16 semantic actual-owner traces; selected FA20 and seven Reserve Release fallbacks per owner
PASS: 16 literal traces; {"capacity": 23, "clock": 50, "clock-accepted": 8, "cuts": 152, "entry": 36, "entry-cuts": 6, "entry-leaf": 1512, "event": 4456, "family": 17, "gate-actor-state": 1216, "gate-arm-state": 1216, "gate-candidate-clock": 14, "gate-choice-candidate": 14, "gate-decision-clock": 304, "gate-owner-clock": 304, "gate-participant-clock": 6, "gate-position-clock": 304, "gate-primitive-state": 2432, "gate-segment-state": 1216, "gate-stale-clock": 304, "gate-version-clock": 608, "gate-version-state": 1216, "history": 140, "ledger": 320, "legacy-reject": 6, "no-op": 30, "order": 48, "order-arm": 210, "order-primitive-arm": 70, "order-segment-clock": 16, "order-version-segment": 32, "ownership": 64, "pins": 128, "positive": 2, "privacy": 30, "proof": 8266, "raw": 2760, "retries": 1962, "retry-primitive": 654, "separation": 2, "state-clock-matrix": 6080, "state-clock-public": 276, "trust": 2}; full original actual entry retained; separate trusted ledger; private FA stop only
```

Unchanged expensive predecessor checks were reused per coordinator instruction; original separate
Breakdown/cycle/Snapshot/outward pin failures and S1a historical unverified timeouts remain honest.
No new .NET/full-suite/Boundary/format/CI claim. Counter2of9 consumed; next fresh coordinator-owned
review is set1/pass3,total3of9. Third Not ready requires coordinator-owned high research recovery
before set2; max2 authorized recovery spikes/max9 reviews, no count reset. No self-dispatched
review, PR/merge or S4. Canonical plan ownership returns to coordinator at this corrected handoff.

## Durable Pass2 RED And Focused GREEN

`s3-state-clock-red.log`: 2676 bytes; SHA256 `e8646916e3341dfe3886584d6fe6bae787b3e5abe5fd8fe548f8d6a0f6b137cb`.

```text
RED: axis 1 complete-step expected CMB-ASE-006, observed CMB-ASE-005
RED: axis 1 close-empty-selection expected CMB-ASE-006, observed CMB-ASE-005
RED: axis 4 complete-step expected CMB-ASE-006, observed CMB-ASE-005
RED: axis 5 complete-step expected CMB-ASE-006, observed CMB-ASE-005
RED: commonwealth 1 complete-step expected CMB-ASE-006, observed CMB-ASE-005
RED: commonwealth 1 close-empty-selection expected CMB-ASE-006, observed CMB-ASE-005
RED: commonwealth 4 complete-step expected CMB-ASE-006, observed CMB-ASE-005
RED: commonwealth 5 complete-step expected CMB-ASE-006, observed CMB-ASE-005
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-public/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/complete-step', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/complete-step', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/complete-step', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/complete-step', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-public/axis/selected/0/complete-step', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/1/close-empty-selection', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/1/close-empty-selection', ('error', 6), ('error', 5))
Traceback (most recent call last):
  File "<stdin>", line 11, in <module>
  File "/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/docs/specs/verify-combat-actual-selection-v1.py", line 871, in state_clock_precedence_checks
    assert not failures,(len(failures),'state/clock precedence mismatches',failures[:4])
           ^^^^^^^^^^^^
AssertionError: (910, 'state/clock precedence mismatches', [('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5)), ('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5)), ('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5)), ('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5))])
```

`s3-state-clock-matrix-red.log`: 2159 bytes; SHA256 `31c3209336f9b48b45bdfd15f32ad930e1bc315ba97ae2ddc86e4b6d0364001a`.

```text
STATE/CLOCK MISMATCH GROUPS: {'state-clock-matrix': 728, 'state-clock-public': 182}
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-public/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/complete-step', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/complete-step', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/complete-step', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/0/complete-step', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-public/axis/selected/0/complete-step', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/1/close-empty-selection', ('error', 6), ('error', 5))
STATE/CLOCK MISMATCH: ('state-clock-matrix/axis/selected/1/close-empty-selection', ('error', 6), ('error', 5))
Traceback (most recent call last):
  File "<stdin>", line 3, in <module>
  File "/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/docs/specs/verify-combat-actual-selection-v1.py", line 891, in state_clock_precedence_checks
    assert not failures,(len(failures),'state/clock precedence mismatches',failures[:4])
           ^^^^^^^^^^^^
AssertionError: (910, 'state/clock precedence mismatches', [('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5)), ('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5)), ('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5)), ('state-clock-matrix/axis/selected/0/close-empty-selection', ('error', 6), ('error', 5))])
```

`s3-state-clock-green.log`: 631 bytes; SHA256 `5e6c4f7e78b786f8bf12dd01f5e45540fc6fb9ade30f29f68a054d3202acdec6`.

```text
PASS focused: {'state-clock-matrix': 6080, 'gate-segment-state': 1216, 'gate-arm-state': 1216, 'gate-primitive-state': 2432, 'gate-version-state': 1216, 'gate-actor-state': 1216, 'gate-version-clock': 608, 'state-clock-public': 276, 'gate-decision-clock': 304, 'gate-owner-clock': 304, 'gate-position-clock': 304, 'gate-stale-clock': 304, 'gate-candidate-clock': 14, 'gate-choice-candidate': 14, 'gate-participant-clock': 6, 'order-arm': 210, 'order-primitive-arm': 70, 'order-version-segment': 32, 'order-segment-clock': 16, 'clock': 50, 'order': 48, 'clock-accepted': 8, 'positive': 2, 'no-op': 30, 'capacity': 22, 'history': 4}
```

Focused reproduction: import the corrected verifier and call
`state_clock_precedence_checks(collections.Counter())`; complete expected model and vectors
are committed in that function and its helpers. RED above was collected against prior behavior
before moving guards. Both-owner named probes cover pending14, pre-RBA and open-RBA states;
all require006 despite an unavailable clock.

## Historical Pass1 Reconciliation And Review Freeze

S3 set1/pass1,total1of9 at02bb6fc1100370f1757a75da45042d1f69b978c8 returned **Not ready**
with one P2: segment identity004 preceded forbidden command-arm003. **Accept**, as reconciled
by coordinator. Initial48 ordering probes missed the combined failure. No heavy pivot,
spec weakening, count reset or research recovery spike. Next review is coordinator-dispatched
set1/pass2,total2of9, max3sets×3/max2 authorized recovery spikes.

Corrected implementation checkpoint `3e340332a42d64432f2f21f7e4bd0fa5267cd446`. The only behavior edit moves the existing
segment require after the complete allowed-arm loop. New arm_segment_order_checks adds328
combined-error vectors across both owners/all8 command kinds:210 arm-before-segment/actor/clock,
70 primitive-before-arm,32 version/kind-before-segment,16 valid-arm segment-before-clock.
Original reviewer vector uses1000 opening time and now returns003 for both owners. Successful
specification, schema and5.64MB literal fixture bytes remain exactly equal to02bb6fc; no regeneration.

Durable minimal regression reproduction from either frozen checkout (read-only; original
02bb6fc fails, corrected checkpoint passes):

```python
import importlib.util
s=importlib.util.spec_from_file_location('a','docs/specs/verify-combat-actual-selection-v1.py')
a=importlib.util.module_from_spec(s);s.loader.exec_module(a)
failures=[]
for side in ('axis','commonwealth'):
    source=a.original_source(side);state=a.replay(source,[])
    inp=a.trusted(a.command(state,'open-segment',expectedPriorVersion=13),now=1000)
    inp['command'].update(segmentId='foreign',choice='finish-without-attack')
    try: a.apply(source,[],inp)
    except a.Invalid as error:
        print(side,error.code)
        if error.code!='CMB-ASE-003': failures.append(side)
    else: failures.append(side)
assert not failures,failures
```

Semantic RED before moving the check (log SHA256
779e4cbeb354a8f1fff60652e79421b40a5955740dc4033c82cb844c85fe9e28):

```text
RED: axis forbidden choice + foreign segment expected CMB-ASE-003, observed CMB-ASE-004
RED: commonwealth forbidden choice + foreign segment expected CMB-ASE-003, observed CMB-ASE-004
Traceback (most recent call last):
  File "<stdin>", line 13, in <module>
AssertionError: ['axis', 'commonwealth']
```

Focused GREEN plus affected adjacent clock/order/capacity checks:

```text
PASS focused: {'order-arm': 210, 'order-primitive-arm': 70, 'order-version-segment': 32, 'order-segment-clock': 16, 'clock': 50, 'order': 48, 'clock-accepted': 8, 'positive': 2, 'no-op': 30, 'capacity': 22, 'history': 4}
```

New full direct command `python3 -B docs/specs/verify-combat-actual-selection-v1.py` PASS exit0,209.977s;
stdout SHA256 `803f9ad4d393d146176fd5c921a40635910b0c401c312f68e31dc47895bba6f1`. This supersedes initial wrapper-only main evidence as the
current full-oracle result. Exact output:

```text
PASS: 16 semantic actual-owner traces; selected FA20 and seven Reserve Release fallbacks per owner
PASS: 16 literal traces; {"capacity": 23, "clock": 50, "clock-accepted": 8, "cuts": 152, "entry": 36, "entry-cuts": 6, "entry-leaf": 1512, "event": 4456, "family": 17, "history": 140, "ledger": 320, "legacy-reject": 6, "no-op": 30, "order": 48, "order-arm": 210, "order-primitive-arm": 70, "order-segment-clock": 16, "order-version-segment": 32, "ownership": 64, "pins": 128, "positive": 2, "privacy": 30, "proof": 8266, "raw": 2760, "retries": 1962, "retry-primitive": 654, "separation": 2, "trust": 2}; full original actual entry retained; separate trusted ledger; private FA stop only
```

Unchanged expensive predecessor commands were not rerun in this bounded correction: no
predecessor source/schema/fixture/runtime byte changed, and coordinator explicitly requested
no repetition without reason. Their prior separate pass/fail evidence remains historical and
unchanged; Breakdown/cycle/Snapshot/outward remain failures, S1a timeouts remain unverified.
No current .NET/full/Boundary/format/CI or independent Ready claim. The five-primary manifest
below is refreshed to this corrected checkpoint; initial chronology/probe passages below remain
historical evidence of3f1dfc9/02bb6fc rather than assertions of complete pre-fix error ordering.

## Intent, Plan And Flow

S3 implements the reviewed private actual-entry→selection bridge, stopping at the useful
FA boundary before allocation. Two exact original owner sources and all seven fallback
variants are executable, with independently supplied trusted selection ledger and new identities.
All five authorized primary paths are used; no sixth primary path, old admission widening,
synthetic promotion or production service change. Native S4, round/result/repeat and public
activation remain deferred.

Flow: check16 frozen dependency digests before imports/replay/cache → own canonical source
and separate ledger → replay complete019E0 source at13 → derive new provenance-rich boundary →
replay each new event against corresponding independently trusted input → exact Control/proof
comparison. Apply fully replays retained history before applying an owned current input. Retries
recover original bytes from the complete source; no digest-only or event-derived-ledger API exists.

## File Responsibilities And Choices

Spec owns scope/trust/error order/dependencies/remaining gates. Inventory owns closed ordered
records, effect arms, domains and bounds. Fixture owns16 literal traces, all inputs/events,
152 Control/proof cuts and complete original sources. Oracle owns strict codecs, source admission,
new-family bounded mechanics/readbacks and adversarial evidence. Plan owns Task019F0 progress
and required next gates. Dated packets keep review bootstrap separate from this explanation.

Selection mechanism is a local copy of the frozen C3a kernel with actual boundary/event/segment/
receipt framing. Unchanged syntax/facts/clock/catalog helpers are reused. Legacy boundary/transition/
replay is not invoked. Candidate validation moves before clock in this new family as explicitly
required by S2; old C3a is untouched. Sharing/extracting old private kernels or widening seed/owner/
position gates was rejected because it would alter frozen trust boundaries outside manifest.

Complete original byte keys cache at most two admitted entry proofs. A separate bounded128-proof
cache keys complete source plus every canonical trusted Input byte sequence after full replay.
All16 dependency digests are checked before either cache. Cache values are immutable canonical
bytes; decoded projections are fresh. This avoids repeatedly redoing identical proof replay in
thousands of mutations. It provides no digest-only authority or persistence authenticator.

## Invariants And Limits

Positive path uses seven events13→20, accepts real defender decline at19 and stops FA with three
step receipts/closed=false. Fallbacks reach the catalog's same-slot Reserve Release and remain
attack/decline/resource/RNG-free. Twelve original receipts/full MovementEnd/Weather/creation/cycle
provenance persist; actual activeSide=null and seed1/cursor2 remain unchanged. Original content
origin labels stay synthetic. No allocation, seal, committed assault or result is produced.

Separate trusted ledger is a caller authentication precondition. Eight event actor/time substitutions
and all other event leaves reject relative to unchanged ledger; a coherent replaced ledger+chain
can replay, proving consistency rather than seat/clock/store authentication. No intelligence,
observation, public action, Snapshot, transport or provider record is introduced. Smuggled fields
reject in closed private grammar; owner checks are not full outward fog-of-war evidence.

Fixed deadline budgets/exclusive equality, below-floor/unavailable semantics, structural gates,
command+actor retries before clock/status, primitive checks before retry, stale timer no-op,
full history order, 1MiB/depth32/arrays512/events-inputs-receipts16 and checked Int64 remain explicit.

## Verification And Corrections

Semantic RED was written/run before implementation or literal freeze:16 owner/variant traces
failed at the missing actual-selection admission. The retained stub calls actual entry replay
before refusing selection. New grammar/framing then implemented; semantic GREEN completed all16
traces. Literal fixture was frozen once afterward; normal oracle main never generates/repairs it.
A second RED observed wrong-candidate+deadline-equality returning005; required004 now passes.

During GREEN wiring, missing env binding and inherited LifecycleFlow validator selection were
localized/fixed before semantic acceptance. A later focused clock test had a one-millisecond
UTC maximum-opening arithmetic mistake: valid maximum is253402300769999, not770000. Correct
implementation rejected the overflowing test value; test corrected. Initial full oracle run
failed this test-only expectation and is not claimed passing. Final run below supersedes it.
No original failure, timeout or literal mismatch was hidden or waived.

Final timed main invocation (exact performed entry point, not falsely reported as a direct shell
script run) imported docs/specs/verify-combat-actual-selection-v1.py, invoked main and retained
stdout. It exited0 in207.469s; standard script __main__ invokes that same function.

```python
import importlib.util,time,contextlib
from pathlib import Path
s=importlib.util.spec_from_file_location('a','docs/specs/verify-combat-actual-selection-v1.py')
a=importlib.util.module_from_spec(s);s.loader.exec_module(a);start=time.monotonic()
with Path('/private/tmp/s3-final-oracle.log').open('w') as log:
    with contextlib.redirect_stdout(log): a.main()
print('RESULT: PASS; seconds='+str(round(time.monotonic()-start,3)))
```

Final exact deterministic stdout:

```text
PASS: 16 semantic actual-owner traces; selected FA20 and seven Reserve Release fallbacks per owner
PASS: 16 literal traces; {"capacity": 23, "clock": 50, "clock-accepted": 8, "cuts": 152, "entry": 36, "entry-cuts": 6, "entry-leaf": 1512, "event": 4456, "family": 17, "history": 140, "ledger": 320, "legacy-reject": 6, "no-op": 30, "order": 48, "ownership": 64, "pins": 128, "positive": 2, "privacy": 30, "proof": 8266, "raw": 2760, "retries": 1962, "retry-primitive": 654, "separation": 2, "trust": 2}; full original actual entry retained; separate trusted ledger; private FA stop only
```

Original checks used python3 -B with240-second bounds, two-worker process pool, explicit S3 workdir.
All completed; no new timeout. These eight original commands are separate outcomes:

| Exact command | Outcome | Seconds | stdout/stderr SHA256 |
| --- | --- | --- | --- |
| `python3 -B docs/specs/verify-combat-positive-entry-v1.py` | PASS exit0 | 120.095 | `37988e248972aed56c4b2f9112a3cb8b55e94c023add34a1635e4e31b4359199` |
| `python3 -B docs/specs/verify-combat-selection-steps-v1.py` | PASS exit0 | 3.628 | `a6d1fb28ec2d5487bfd23e5cd0c4dc208c4bda1147d027ad9ded9c0639777691` |
| `python3 -B docs/specs/verify-combat-sealed-round-v2.py` | PASS exit0 | 17.487 | `7497cdcac2a85a116e5b2eaaf43be2313d90522fddc1da852312b388eedd527c` |
| `python3 -B docs/specs/verify-combat-result-settlement-v2.py` | PASS exit0 | 91.325 | `2a034604581df3937100289a18e23f5b3270ba57d6ff345cb0f5c2e9f3b400bb` |
| `python3 -B docs/specs/verify-combat-inherited-breakdown-completion-v1.py` | FAIL exit1 | 0.373 | `53763fd8940f5a72d343f67ae7ef1c41175d0df64abfa414ecd532309674f748` |
| `python3 -B docs/specs/verify-combat-cycle-sequence-v1.py` | FAIL exit1 | 0.049 | `2b37c9479b23cf9201a18ce0fae319c542b09c20f23e85a7105be3cc474dbdcf` |
| `python3 -B docs/specs/verify-combat-inherited-snapshot-v1.py` | FAIL exit1 | 1.533 | `3dbdcdc29d0a06b51cb07e39075f7934ea519e393f4f301bf4293905c27c8952` |
| `python3 -B docs/specs/verify-combat-outward-composition-v1.py` | FAIL exit1 | 5.017 | `e5685d6e457510e7a77feaafe834a601d3b3d2248d3cc54005a6204c21576c3d` |


Breakdown/cycle fail sequence-source pin; Snapshot fails recursive same pin. Outward rejects
its independent unchanged pin gate (Content-document drift established in S1a). S1a's12/60s
timeouts remain historical/unverified; later fresh completed checks do not rewrite them.
No .NET build/native/Boundary/full suite or format gate, hosted CI, public privacy proof,
independent review or merge is claimed. Git staged/diff whitespace checks passed.

Supplemental old-family and correctly locally re-signed original entry probes passed4+4;
source script is durably retained below. Focused clock/trust/capacity/order/separation checks
passed before full main. Pin tamper covers all16 digests across8 authority/readback calls,
with warm entry/proof-cache sentinel; every mismatch rejects009 before access. Cold separation
blocks historical Breakdown fixture main/check_fixture and Snapshot/outward files, yet both
sources admit.

## Costs, Risks And Challenge Points

New frozen family duplicates roughly130 lines of mechanics, creating future parity/maintenance
cost. Fixture is5.64MB because it retains full source and all cuts literally; individual private
packets are bounded1MiB. Public API trust, canonical ownership, and cache exact source+ledger
binding deserve skepticism. Challenge all actor/clock/error-order and historical-family cases,
positive no-completion and no false Result2 reachability. Source pins certify the frozen16 inputs;
they do not certify every future transitive dependency or production authentication subsystem.
Fresh S4 byte parity and native duplicate-mechanism review remain required. No .NET file changed,
so scoped Python checks were used without consuming coordinator full-suite lease.

## Retained Primary Manifest

| Primary path | Bytes | SHA256 |
| --- | --- | --- |
| `docs/specs/combat-actual-selection-v1.md` | 11255 | `a81394f4e58c582a7beabccd1aeb4ba1bdb3475ebdc14378c95ac3fbc97fda82` |
| `docs/specs/combat-actual-selection-v1.schema.json` | 5689 | `da6256deb94bb6061e8e98e2448f915c4474373b7f0de6bb76eded2ae88c39b1` |
| `docs/specs/fixtures/combat-actual-selection-v1.json` | 5640101 | `019d1a3ff0f121d83f377ddfb19d274b4a8aa89172bad8aeb289c3b98228e604` |
| `docs/specs/verify-combat-actual-selection-v1.py` | 65112 | `f37a7cfa26b60469168d9f4424465b14f1fe45666f4222d69c840c2527d2e66f` |
| `docs/design/combat-cycle-implementation-plan.md` | 215265 | `2fccd02eb8a4e37598e7bd7eded2860f917f9cbadc8310c6bc98f496f33d5255` |


## Retained RED Reproduction

Exact pre-implementation source SHA256 `d49fe4e818c739facd9d703235c95d2c2964c9b3b5e38c4dd65db23ee45d3ec7`; RED stdout/stderr
SHA256 `d38ced0260de17b5106c0c9fba6e3ae58746e84ad62f0278cde2cb90a8dd29f7`. Extract S3_RED_SOURCE block to memory, verify digest, execute
with __file__ set to this checkout's docs/specs/verify-combat-actual-selection-v1.py and
__name__='__main__'. This preserves ROOT resolution while writing only a temporary reproduction
file if desired. Expected AssertionError16, not a passing oracle. Source contains semantic
expectations for all16 traces before the implementation; no fixture repair occurs.

<!-- S3_RED_SOURCE:start -->
```python
#!/usr/bin/env python3
"""Private actual selection executable contract; independently supplied trusted ledger."""
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent

def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module); return module

pe = load('actual_entry', 'verify-combat-positive-entry-v1.py')
st = pe.steps
encode, sha, digest = st.encode, st.sha, st.digest
command, trusted, edge = st.command, st.trusted, st.edge

def replay(source, trusted_inputs):
    # Pre-implementation admission has no actual-selection consumer.
    pe.replay(json.loads(source['positiveEntrySourceCanonicalUtf8']))
    raise ValueError('CMB-ASE-004: actual-selection admission unimplemented')

def original_source(side):
    row = next(c for c in json.loads(pe.FIXTURE.read_text())['cases'] if c['actor'] == side)
    return dict(contractVersion=1, positiveEntrySourceCanonicalUtf8=row['sourceCanonicalUtf8'],
                selectionEventCanonicalUtf8=[])


def trace(side, variant='selected'):
    source = original_source(side); ledger = []; sources = [copy.deepcopy(source)]
    control = replay(source, ledger); controls = [control]; inputs = []; events = []
    b = boundary(source)
    def accept(kind, actor='system', now=None, available=True, **fields):
        nonlocal source, control
        inp = trusted(command(control, kind, **fields), actor, now, available)
        control, event, receipt = apply(source, ledger, inp)
        assert event is not None and receipt == json.loads(event)['receiptId']
        ledger.append(copy.deepcopy(inp)); inputs.append(copy.deepcopy(inp)); events.append(event)
        source['selectionEventCanonicalUtf8'].append(event.decode('ascii'))
        sources.append(copy.deepcopy(source)); controls.append(copy.deepcopy(control))
    accept('open-segment', now=None if variant == 'opening-unavailable' else 1000,
           available=variant != 'opening-unavailable', expectedPriorVersion=13)
    if variant == 'opening-unavailable':
        accept('close-empty-selection', expectedPriorVersion=14)
    elif variant == 'finish':
        accept('choose-selection', actor=side, now=1100, decisionId=control['segmentId']+'.selection',
               choice='finish-without-attack')
    elif variant == 'selection-expired':
        accept('expire-window', now=31000, decisionId=control['segmentId']+'.selection')
    elif variant == 'selection-unavailable':
        accept('controller-unavailable', available=False, decisionId=control['segmentId']+'.selection')
    else:
        accept('choose-selection', actor=side, now=1100, decisionId=control['segmentId']+'.selection',
               choice='select-close-assault', candidate=b['candidate'])
    for step in range(6):
        if step == 2 and control['selectionOutcome'] == 'selected':
            if variant == 'rba-before-open-unavailable':
                accept('controller-unavailable', available=False, decisionId=control['segmentId']+'.rba')
            else:
                accept('open-rba', now=2000, expectedPriorVersion=control['stateVersion'],
                       fromPositionId=edge(b)['combatPositionIds'][2])
                if variant == 'rba-expired':
                    accept('expire-window', now=32000, decisionId=control['segmentId']+'.rba')
                elif variant == 'rba-unavailable':
                    accept('controller-unavailable', available=False, decisionId=control['segmentId']+'.rba')
                else:
                    accept('decline-rba', actor=b['candidate']['defender']['unit']['originalSide'], now=2100,
                           decisionId=control['segmentId']+'.rba', participant=b['candidate']['defender']['unit'])
        accept('complete-step', expectedPriorVersion=control['stateVersion'], fromPositionId=edge(b)['combatPositionIds'][step])
        if variant == 'selected' and step == 2: break
    return sources, controls, inputs, events


VARIANTS = ('selected', 'opening-unavailable', 'finish', 'selection-expired', 'selection-unavailable',
            'rba-before-open-unavailable', 'rba-expired', 'rba-unavailable')


def semantic_expectations(side, variant):
    sources, controls, ledger, events = trace(side, variant)
    b = boundary(sources[0]); entry = pe.replay(json.loads(sources[0]['positiveEntrySourceCanonicalUtf8']))
    assert b['world'] == entry['world'] and b['randomState'] == entry['randomState']
    assert b['position']['activeSide'] is None and len(entry['receipts']) == 12
    assert b['priorVersion'] == 13 and b['priorPrefix'] == entry['prefix']
    assert controls[0]['stateVersion'] == 13 and controls[1]['stateVersion'] == 14
    assert [s['stateVersion'] for s in controls] == list(range(13, 14+len(events)))
    if variant == 'selected':
        assert len(events) == 7 and [s['selectionOutcome'] for s in controls[:3]] == ['unopened','pending','selected']
        assert controls[-2]['stateVersion'] == 19 and controls[-2]['declineReceiptId']
        assert controls[-1]['stateVersion'] == 20 and controls[-1]['stepIndex'] == 3 and not controls[-1]['closed']
        assert len(controls[-1]['stepReceipts']) == 3
        assert ledger[1]['actor'] == side and ledger[5]['actor'] != side and ledger[5]['actor'] != 'system'
        assert controls[-1]['selectionWindow']['timing']['deadlineUnixMilliseconds'] == 31000
        assert controls[-1]['rbaWindow']['timing']['deadlineUnixMilliseconds'] == 32000
        assert controls[-1]['selectionWindow']['timing']['highWaterUnixMilliseconds'] == 1100
        assert controls[-1]['rbaWindow']['timing']['highWaterUnixMilliseconds'] == 2100
    else:
        assert controls[-1]['stepIndex'] == 6 and controls[-1]['closed'] and len(controls[-1]['stepReceipts']) == 6
        assert controls[-1]['declineReceiptId'] is None
        assert json.loads(events[-1])['effect']['toPositionId'] == edge(b)['releasePositionId']
    for source in sources:
        assert source['positiveEntrySourceCanonicalUtf8'] == sources[0]['positiveEntrySourceCanonicalUtf8']
        p = proof(source, ledger[:len(source['selectionEventCanonicalUtf8'])])
        assert p['boundary'] == b and p['positiveEntryProof']['entry']['world'] == entry['world']
        assert p['positiveEntryProof']['entry']['randomState'] == entry['randomState']
        assert len(p['positiveEntryProof']['entry']['receipts']) == 12
    return sources, controls, ledger, events


def semantic_red():
    failures = []
    for side in ('axis','commonwealth'):
        for variant in VARIANTS:
            try: semantic_expectations(side, variant)
            except ValueError as error: failures.append((side,variant,str(error)))
    if failures:
        for row in failures: print('RED:', *row)
        raise AssertionError(f'{len(failures)} semantic actual-owner traces failed before implementation')
    print('PASS: 16 semantic actual-owner traces; selected FA20 and seven Reserve Release fallbacks per owner')

if __name__ == "__main__": semantic_red()
```
<!-- S3_RED_SOURCE:end -->

Exact retained RED stdout/stderr:

```text
RED: axis selected CMB-ASE-004: actual-selection admission unimplemented
RED: axis opening-unavailable CMB-ASE-004: actual-selection admission unimplemented
RED: axis finish CMB-ASE-004: actual-selection admission unimplemented
RED: axis selection-expired CMB-ASE-004: actual-selection admission unimplemented
RED: axis selection-unavailable CMB-ASE-004: actual-selection admission unimplemented
RED: axis rba-before-open-unavailable CMB-ASE-004: actual-selection admission unimplemented
RED: axis rba-expired CMB-ASE-004: actual-selection admission unimplemented
RED: axis rba-unavailable CMB-ASE-004: actual-selection admission unimplemented
RED: commonwealth selected CMB-ASE-004: actual-selection admission unimplemented
RED: commonwealth opening-unavailable CMB-ASE-004: actual-selection admission unimplemented
RED: commonwealth finish CMB-ASE-004: actual-selection admission unimplemented
RED: commonwealth selection-expired CMB-ASE-004: actual-selection admission unimplemented
RED: commonwealth selection-unavailable CMB-ASE-004: actual-selection admission unimplemented
RED: commonwealth rba-before-open-unavailable CMB-ASE-004: actual-selection admission unimplemented
RED: commonwealth rba-expired CMB-ASE-004: actual-selection admission unimplemented
RED: commonwealth rba-unavailable CMB-ASE-004: actual-selection admission unimplemented
Traceback (most recent call last):
  File "/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/docs/specs/verify-combat-actual-selection-v1.py", line 123, in <module>
    if __name__ == "__main__": semantic_red()
                               ~~~~~~~~~~~~^^
  File "/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/docs/specs/verify-combat-actual-selection-v1.py", line 120, in semantic_red
    raise AssertionError(f'{len(failures)} semantic actual-owner traces failed before implementation')
AssertionError: 16 semantic actual-owner traces failed before implementation
```

Semantic GREEN and freeze stdout:

```text
PASS: 16 semantic actual-owner traces; selected FA20 and seven Reserve Release fallbacks per owner
PASS: 16 semantic actual-owner traces; selected FA20 and seven Reserve Release fallbacks per owner
FROZEN: 16 literal traces after semantic GREEN; oracle has no generation/repair switch
```

Supplement reproduction: extract S3_SUPPLEMENT to temporary .py; exact SHA256
`f5ccd09c1e5a0e6a73e5db1338d952ce3c5499f115687aa27881de82e6decfc9`. Run python3 -B temporary-file against the frozen S3 path.
Expected PASS supplemental: {'old-family':4,'entry-resigned':4}. It changes no repository bytes.

<!-- S3_SUPPLEMENT:start -->
```python
import importlib.util,json
from pathlib import Path
root=Path('/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable')
s=importlib.util.spec_from_file_location('a',root/'docs/specs/verify-combat-actual-selection-v1.py');a=importlib.util.module_from_spec(s);s.loader.exec_module(a)
c={}
for family,key in (('sealed-round-v2','cases'),('result-settlement-v2','traces')):
 f=json.loads((a.ROOT/'fixtures'/('combat-'+family+'.json')).read_text());data=f[key][0]['baseCanonicalUtf8'].encode('ascii')
 a.reject('old-family/'+family,lambda:a.read_source(data,[]),c)
 bad=a.original_source('axis');bad['positiveEntrySourceCanonicalUtf8']=data.decode('ascii')
 a.reject('old-family/launder-'+family,lambda:a.replay(bad,[]),c,4)
for side in ('axis','commonwealth'):
 source=a.original_source(side);packet=json.loads(source['positiveEntrySourceCanonicalUtf8'])
 for index,(domain,prefix) in enumerate(((a.pe.lc.INVENTORY['domains']['completeReceipt'],'iml.'),(a.pe.bd.INVENTORY['domains']['receipt'],'ibc.'))):
  value=json.loads(packet['entryEventCanonicalUtf8'][index]);unsigned={k:v for k,v in value.items() if k!='receiptId'}
  assert prefix+a.digest(domain,a.encode(unsigned))==value['receiptId'],(side,index)
  value['stateVersion']+=1;unsigned={k:v for k,v in value.items() if k!='receiptId'};value['receiptId']=prefix+a.digest(domain,a.encode(unsigned))
  forged=a.copy.deepcopy(packet);forged['entryEventCanonicalUtf8'][index]=a.encode(value).decode('ascii');bad=a.copy.deepcopy(source);bad['positiveEntrySourceCanonicalUtf8']=a.pe.packet_bytes(forged).decode('ascii')
  a.reject('entry-resigned/'+side+str(index),lambda:a.replay(bad,[]),c,4)
print('PASS supplemental:',c)
```
<!-- S3_SUPPLEMENT:end -->
