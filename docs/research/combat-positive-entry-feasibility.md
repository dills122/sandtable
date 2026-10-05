# Positive Combat entry feasibility

Status: R1 research decision proposed for coordinator review, 2026-10-04 America/Toronto.
Base: `907f41403ed159d65024f193f3e1f730a23b9bbb` (merged PR151).
Decision owner: overnight coordinator `01a0c9dc-00bc-78a3-800d-3cb36859e422`.
This report is evidence and a proposed next manifest; canonical specs/implementation plan remain authoritative.

## Decision and four distinct boundaries

**Recommend GO only for a new, entry-only prerequisite:** actual Normal-Weather, ordinary NONE,
zero-move first Movement followed by idle Breakdown completion, both resolved owners. Stop at
Position Determination with a derived supported candidate and a real Movement-end proof. The
existing opening is actual; its proposed two completion events are not yet implemented or retained.
No currently admitted creation-to-positive-selection-to-Result2 path was established.

| Boundary | Conclusion | Evidence class |
| --- | --- | --- |
| Legal geometry under adopted rules | Initial ordinary infantry are adjacent, full TOE/ammo, CP0/0; idle Movement may complete, then idle Breakdown enters Combat. | Documented fact plus inference from the existing idle factories/tests; no new movement/assault rule proposed. |
| Current actual native lineage | Creation→Normal Weather→no designation→Reserve completion reaches Movement at version11. Current NONE no-move completion/positive selection admission is absent from the inspected readers. | Documented fact; Python replay observed and matched frozen goldens. Native existing tests inspected, not rerun. |
| Minimum new entry-only contract | Two commands/events can establish actual Movement/Breakdown completion and positive candidate at version13, with unchanged World/RNG. | Proposed inference, conditional on executable RED/GREEN, review and canonical reconciliation. |
| Consumption by C3a/Round2/Result2 | Current C3a pins seed0 creation while actual Normal opening uses seed1. Additional receipt/position/Weather mapping and source authentication are necessary. Direct reuse is NO-GO. | Documented fact and bounded rejection observations. New selection/round/result bridge remains separately gated. |

This GO fits the approved N2 stop-at-positive-entry boundary if the coordinator accepts an entry
with an authenticated candidate **before selection opens**. It does not satisfy an interpretation
requiring current C3a acceptance or an attack/result event. Under that interpretation N2/N3 are
NO-GO until the distinct compatibility prerequisite is separately planned and reviewed. No heavy
architecture/gameplay pivot is required for the proposed idle entry alone. Combining the later
selection/round/result bridge into this five-file entry slice would change the reviewed scope and
must return to the coordinator; a new gameplay or architectural pivot requires human sync.
Parents017–019, later-II/consumed, repeat, public/Snapshot/transport/host and Runner acceptance remain open.

## Method, bounds and source hierarchy

Question: smallest genuine creation-rooted Movement/Breakdown/selection predecessor capable of
positive Combat, with an eventual separately authenticated Result2 path. Success requires full
request/Created11/predecessor bytes, actual actors/receipts/versions/prefix, legal facts and explicit
unsupported gates. A synthetic terminal or digest-only cache is not a candidate lineage.

Scope: read canonical adopted rules/contracts, native source/tests, and bounded scratch replay in
an isolated worktree. No production/spec/fixture/runtime edits, primary writes, full suite, old pin
changes or implementation authorization. Stop after one narrow source, comparison, field mapping,
five-file manifests and review packet. No external rules information was needed; no web research
or secondary rule interpretation was used.

Worktree: `/Users/dsteele/.codex/worktrees/positive-entry-feasibility/sandtable`, branch
`codex/positive-entry-feasibility`. Serena activated that exact path; generated `.serena/` is local
tooling excluded from retained changes. Codebase Memory project `sandtable-positive-entry-feasibility`,
full generation `2026-10-05T02:54:53Z`, 26174 nodes/139451 edges. Search pages0/100 fully consumed
(155 rows in the Movement/Breakdown/selection query); focused Reaction search exhausted12 rows.
Both-direction `CertifyInitialProfileFacts` trace exhausted5 callers/9 callees at depth1, including
native SelectionSteps, ArmedContinuation and Identity tests. Resolver edges are best-effort discovery,
not authority proof. Exact cited-path coverage returned metadata_match/no_recorded_issue; direct
reads of the actual predicates and Python readers establish the conclusions. This is a bounded
reader audit, not an exhaustive claim about all possible future gameplay.

Primary source index:

- [Reserve designation/opening contract](../specs/combat-reserve-designation-v1.md), its
  [oracle](../specs/verify-combat-reserve-designation-v1.py) and [fixture](../specs/fixtures/combat-reserve-designation-v1.json).
  Native `CampaignCombatReserveOpening.Replay` plus completion/designation readers reconstruct it.
- [Movement lifecycle](../specs/combat-inherited-movement-lifecycle-v1.md),
  [Breakdown completion](../specs/combat-inherited-breakdown-completion-v1.md),
  [held-I no-move cycle](../specs/combat-inherited-reserve-cycle-v1.md), and exact ordered inventories.
- [Reaction trigger](../specs/combat-inherited-reaction-trigger-v1.md),
  [direct closure](../specs/combat-inherited-reaction-closure-v1.md), and native `CampaignCombatReactionClosure.Replay`.
- [Inherited empty selection](../specs/combat-inherited-selection-v1.md), `CampaignCombatCertification.Admit`,
  and `CampaignCombatInheritedSelection`; CP12/14 empty proof only.
- [C3a](../specs/combat-selection-steps-v1.md), `CampaignCombatSelectionSteps.ValidateBoundary`,
  `CampaignCombatSelectionStepsCodec.WriteValidatedBoundary`, and `CombatStepsTests`.
- `CampaignCombatCertification.CertifyInitialProfileFacts`/`ProveSupport` and `CombatIdentityTests`;
  facts certification expressly does not authenticate history.
- `CampaignBreakdownLifecycleFactory.CreateMovementCompletion`/`CreateBreakdownCompletion`, and
  `BreakdownLifecycleTests.MovementCompletionRequiresIdleAndAdvancesOnlyToBreakdown` /
  `BreakdownCompletionAdvancesToFirstCombatAndRejectsForgedAction`. These are older World6/Snapshot11
  fixtures; they support adopted transition semantics, not World7 campaign composition.
- [Round2](../specs/combat-sealed-round-v2.md), [Result2](../specs/combat-result-settlement-v2.md),
  [post-Movement dispatch](../design/combat-cycle-post-movement-dispatch.md),
  [implementation plan](../design/combat-cycle-implementation-plan.md), and latest PR150/151 handoffs.

## Actual source and smallest proposed causal chain

**Observation O1:** `normal-act-first-none` and `normal-act-last-none` reproduce their complete
Reserve fixture goldens. Request seed1; Weather consumes bytes0..1, leaving cursor2. Both share
creation binding `creation.c228f73a59deeb83e45d94e240f42b8fd1d24020d4a90c5fe29e9666216f90d9`.
At first-slot Movement: version11, ten accepted receipts, ordinal1, openedAuthorityVersion11,
null catalog activeSide and owner derived from retained ActFirst/ActLast order. The sole own member
is NONE with null designation receipt; both complete initial units remain NONE, CP0/0, Cohesion0,
ammo10 and TOE10. Axis is at `assault-west`, Commonwealth at `assault-east`, adjacent in Content7.

| Causal record | Actor | Resulting version | Status |
| --- | --- | --- | --- |
| Request→Created11 | Trusted creation ingress |1 | Existing independent source bytes |
| Four opening preamble records | Retained authorized actors |2..5 | Existing replay |
| Weather2 |System |6 | Actual Normal, seed1/cursor2; keep its input, receipt and event hash |
| Organization, arrival, assignment, repair |System |7..10 | Actual explicit-none policy replay |
| Reserve completion2, no designation event |Resolved owner |11 | Actual ordinal1 opening, `rc.` receipt |
| Direct idle `complete-movement-segment` |Resolved owner |12 | **New prerequisite**, no stop/route/resolution record |
| Idle `complete-breakdown-segment` |System |13 | **New prerequisite**, enters Position Determination only |
| Candidate derivation from replayed terminal |No command/actor/time |13 | **New prerequisite**, no selection opening/decision or attack |

The two new completions add two receipts and extend the actual Chronicle prefix. Movement-end
proof must bind the real new completion receipt, full original-unit locations, scope/ordinal1 and
independently derived empty proximity-exclusion set (distance1). Both completion events add zero
material progress and draw no RNG. Preserve World, all resources, Reserve histories, future duties,
Weather/order/initiative and opening/cycle identity. This is empty-cohort idle Breakdown completion,
not proof of a positive vehicle check. No stop-resolution event is permitted without an actual route.

**Inference I1:** zero moves is minimal among legal histories by number of Movement/Reaction commands
and CP spend; there are no new route, stop, participant, timing, exception or Release dependencies.
It is not minimal because a fake completed state is shorter: every real predecessor is mandatory.
Existing source actors/order make the two owners symmetric, so both literal histories are required.

## Candidate comparison

| Candidate | Actual accepted terminal today | Positive facts / impediment | Decision |
| --- | --- | --- | --- |
| Normal ordinary NONE, no move | Movement11, CP0/0, adjacent; actual opening | Idle entry suffix missing; C3a request compatibility separately missing | Choose entry-only prerequisite; two new completions |
| Ordinary route1/5/6/7 moves | Actual Movement/Breakdown completion; CP2/10/12/14 | Destinations admitted only without adjacent enemy. Six/seven moves support empty selection only | Reject as positive source |
| Rear→assault trigger plus direct player decline | Actual resumed Movement14, CP4/0, adjacent | Route still moving; stop/empty resolution/completion/Breakdown bridge missing; C3a seed gate remains | Feasible larger alternative, no benefit over zero-move source |
| Reaction participant moves once/twice | Actual resumed phasing Movement; enemy at rear/supply | Initial-geometry certification rejects changed location/World; extra route history | Reject for minimal unchanged infantry profile |
| Held-I no move | Actual no-attack traversal/Release, CP0, adjacent | Reserve I cannot be an ordinary attacker. Release/repeat/exception path adds dependencies | Reject for first ordinary positive entry |
| Released-I armed witness /3m ordinal2 | Actual first-I/repeat/move lineage | Armed proof is potential certification, not an attack; ordinal2, released profile and pending/expired exception need separate admission | Excluded; do not reinterpret as consumed offensive history |
| Settled32 catalogue | Full synthetic pre-Combat Result2 suffix | Seed0/case cursor/CP substitutions are explicitly synthetic | Reject as actual lineage |

**Documented fact F1:** `CampaignCombatMovementLifecycle.Replay` requires nonempty moves;
its initial ordinary move reader rejects adjacent destinations before filtering Reaction eligibility.
`CampaignCombatInheritedReserveCycle.Initial` requires version12/11 prior receipts/designated I.
`CampaignCombatCertification.Admit` requires six/seven ordinary moves and CP12/14/nonadjacency.
These readers cannot be widened by a cache, added boolean or substitution of the proposed terminal.

## Exact mapping to freeze in N2

This is a proposed byte specification for the next contract, not existing accepted new goldens.
The entry-only packet has no externally supplied completed state. Replay inputs are Request,
Created11, exact four preamble/one Weather/four stage/one Reserve-completion lists, and zero..two
new suffix events. It admits only the two pinned Normal/NONE literal opening sources above. Source
identity must bind the full ordered predecessor bytes and request; retaining a terminal hash alone
is insufficient. Native replay uses `CampaignCombatReserveOpening.Replay`, not an imported fixture.

| Proposed field / bytes | Exact causal origin / rule |
| --- | --- |
| `creationBinding`, `creationEventHash`, campaign/rules/configuration | Independently retained Request/Created11 plus complete source replay; retain byte values, no seed rewriting |
| cycle, cycleId, openingBaseHash, original Reserve completion ID | Actual Reserve11 opening; openedAuthorityVersion11/openingPrefix remain unchanged through entry |
| Movement CompleteCommand2 / trusted actor | Existing lifecycle CompleteCommand ordered fields: contractVersion, kind, actionId, creationBinding, creationEventHash, cycleId, expectedPriorVersion, expectedPositionId; derive current version11 and resolved owner |
| Movement CompleteEvent3 | Copy the exact 12-field legacy order and appended layout from `combat-inherited-movement-lifecycle-v1.schema.json` CompleteEvent; compute 11→12, catalog Breakdown successor, idle flow, null interrupt; empty progress, real endLocations/excludedUnits |
| Movement receipt / prefix | Preserve existing completion3 framing: `iml.` + SHA256(domain `sandtable.combat.inherited-movement-completion-receipt.v3`, NUL, unsigned canonical event); prefix_event(actual priorPrefix, full event). Old route reader still rejects no-move admission |
| Breakdown EntryCommand2 / input | Exact ordered EntryCommand/EntryInput from inherited Breakdown inventory; current version12, stage1, exact action hash, System actor |
| Breakdown EntryEvent2 | Copy exact 11 legacy fields and suffix from `combat-inherited-breakdown-completion-v1.schema.json`; compute12→13, original predecessor-position sources, real Movement completion receipt distinct from Reserve completion |
| Breakdown receipt / prefix | `ibc.` + SHA256(domain `sandtable.combat.inherited-breakdown-completion-receipt.v2`, NUL, unsigned canonical event); extend actual prefix once |
| Private entry state | Reuse full ordered 27-field `CombatEntryState` inventory; start from Reserve source + tracks=[], actualProgressRefs=[], idle flow, null interrupt/movementEnd/breakdownCompletionReceiptId. Populate the two receipt/proof fields only when those real events are replayed |
| Candidate | At version13 only, native `CertifyInitialProfileFacts(request, independently retained Created11, exact World, actual cycle/owner, replay-derived applicable Weather)`; never supply Normal independently of actual Weather replay |
| New proof envelope | New ordered `contractVersion,sourceId,sourceHash,entry,candidate`; contractVersion1, domain `sandtable.combat.positive-entry-source.v1`, source identity over canonical full ordered Request/Created/predecessor/suffix inventory. `candidate` null before entry, exact full Participant/UnitKey/component provenance after entry; cache read requires full replay |

N2 must freeze the new proof inventory and literal canonical bytes after independent assertions,
not simply regenerate goldens to match code. Reusing compatible event grammar/domain does not
authorize old route/held-I readers to accept this new source. The new reader is the only no-move
NONE admission. Existing artifacts and pins must remain unchanged. If exact legacy mapping cannot
fit without modifying an old reader or sixth primary file, return a scope gate before implementation.

Strict compact ASCII canonical JSON, declared order/mandatory nulls, sorted identity arrays,
unaltered ordered history arrays, 1MiB per individual record/depth32/arrays512 and checked Int64
versions follow predecessor rules. Original owned bytes and every cut/retry must survive; no clock
or deadline belongs to these two untimed completion commands.

## C3a/Round2/Result2 compatibility ledger and remaining gate

**Observation O2:** the actual request binding above and seed1 each independently reject when
substituted into an old synthetic C3a boundary: `CMB-STP-004 /creationBinding` and `CMB-STP-004 /`.
Native `CampaignCombatSelectionSteps.ValidateBoundary` independently hardcodes
`creation.dc1c1bff9db6122758ab2b06360ba2131231fe2b63871780e416c8fd6ba4490b`; its RNG seed must equal
that retained seed0 request. Normal source seed1 cannot pass by just keeping the geometry.
Seed0's actual first Weather is Rainstorm, so calling it Normal would violate real history.

| Current C3a field | Actual entry value / exact incompatibility |
| --- | --- |
| creationBinding / Request | Must retain seed1 `creation.c228…`; current reader pins seed0 `creation.dc1c…`. Needs separately versioned request-bound admission |
| cycle / priorVersion / priorPrefix | Copy actual opening cycle unchanged; new terminal13 and actual final prefix. Do not borrow synthetic openingPrefix/version/cycleId |
| position | Actual catalog Position Determination retains activeSide=null. C3a Boundary expects materialized activeSide=derived owner. This representational mapping must be explicit; it is not a new authority event or a supplied owner |
| completedBreakdownReceipt | C3a schema type is `hash`, while actual event receipt is `ibc.<digest>` type id. No direct copy or receipt-ID/event-hash conflation. Future contract must bind both actual receiptId and canonical eventHash and define any legacy compatibility projection |
| World | Zero-move NONE World exactly equals its own Created11 initial World; retains actual creation binding. Existing certification permits only current integral CP differences; a moved/reacted World cannot borrow this rule |
| randomState | Actual seed1/cursor2; C3a pins seed0 and Result2 catalogue uses synthetic case cursors. Retain actual cursor, never select a desired branch cursor |
| applicable Weather | Both actual applicable kinds Normal from retained Weather2; bind actual canonical Weather event hash and its distinct receipt ID. C3a supplied weather/hash alone currently authenticates no history |
| Movement-end proof | Not represented in C3a Boundary. New actual source envelope must retain it and own complete original-unit coverage for later continuation; cannot reconstruct from post-result World |
| selection/round/result source | No new selection event is retained here. Later adapter must authenticate the new entry packet, actual selection opening/choice/accepted RBA decline, then Round2 inputs/events and Result2 inputs/events. Current Result2 cannot accept a source merely because its arithmetic is supported |

**Observation O3 (scratch arithmetic only):** existing RNG/rules code at seed1/cursor2 yields dice
`5,2,6,1,6,2,2,2`, cursor10; morale coordinates52/61, differential0, assault coordinates62/22,
attacker0%/defender15%, no raw Engaged/retreat/capture. This does not emit or authenticate any
commitment/result. It suggests a narrow ordinary non-mandatory-window branch after a future real
commitment (ordinary charges CP5/3, full allocation ammo10→0, then rounded defender loss), rather
than any promised zero-Engaged settled source. Both owners share RNG role order but own identities
must be replayed independently. The32 synthetic settlement contexts are not promoted or copied.

**Unknown U1:** exact positive selection/request/Weather bridge and downstream Result2 source
contract/native manifests are not certified by this report. Their acceptance may require a new
versioned selection source or explicit native consumer extension; preserve old C3a/Round2/Result2
bytes. Do not dispatch that later work from this report, silently remove hardcoded pins, or change
a fixture's trust label. A bounded entry-only GO creates no claim of complete result/control/repeat.

## Exact five-file delivery manifests

Names are proposed new paths; coordinator freezes task IDs/ownership only after R1 review and N1
reconciliation. Each slice has five primary files. Handoff/review evidence is ancillary, not a
sixth production/spec file disguised as documentation. No broad README/design/roadmap changes are
needed for this private entry packet; the canonical implementation-plan row records its boundary.

N2 executable contract:

1. `docs/specs/combat-positive-entry-v1.md`
2. `docs/specs/combat-positive-entry-v1.schema.json`
3. `docs/specs/fixtures/combat-positive-entry-v1.json`
4. `docs/specs/verify-combat-positive-entry-v1.py`
5. `docs/design/combat-cycle-implementation-plan.md` (coordinator schedules ownership)

N3 native adapter after reviewed/merged N2:

1. `src/Cna.Core/Campaigns/CampaignCombatPositiveEntry.cs` (engine plus closed private models/source capture)
2. `src/Cna.Core/Campaigns/CampaignCombatPositiveEntryCodec.cs`
3. `tests/Cna.Core.Tests/Campaigns/CombatPositiveEntryTests.cs`
4. `tests/Cna.Core.Tests/Cna.Core.Tests.csproj` (new fixture link)
5. `docs/design/combat-cycle-implementation-plan.md`

No modifications to `CampaignCombatSelectionSteps`, certification, Snapshot, host, public action
catalogs or predecessor readers belong to either manifest. The native engine reuses existing
Reserve opening and facts certification only. The output is a new actual positive-entry envelope,
not a falsely accepted `CombatStepsBoundary`. Separate downstream compatibility work remains open.

## Required semantic tests and next gates

N2 RED must replay each real Normal/NONE opening and fail because actual idle entry/proof is
missing, after predecessor success; no syntax/import or false failing placeholder counts.
GREEN must independently assert version11→12→13, actors, exact positions/sources/action IDs,
World/CP/ammo/TOE/history/RNG equality, actual Movement completion receipt/proof, positive candidate
only at final cut, and two new zero-progress events. Freeze all canonical bytes and source pins only
after those assertions. No prose-TDD claim applies to this research.

Require every restart cut and original-byte retry after terminal. Reject wrong owner/System role,
missing/reordered/duplicate predecessor or suffix, altered seed/Weather/order/cycle/slot/ordinal,
held-I/designated/released/converted/vehicle/moved/Reaction/pending-stop histories, fabricated
completed receipt/proof/foreign prefix, cross-owner entry, changed consumed input, re-signed
World/resource/event/proof leaves, cache-only admission, alternate canonical bytes and capacity
edges. Prove correct full source capture against caller mutation/adversarial lists in native tests.
No early completion, positive selection opening, clock window, attack, Release, repeat or synthetic
catalogue source may be smuggled into the packet.

N2 checks: new oracle + unchanged Reserve designation, stage-entry, inherited Movement lifecycle,
Breakdown, empty selection, C3a, Round2 and Result2 oracles; syntax/JSON/links/pins/diff; proportionate
repository regression gate serialized by coordinator; fresh independent review and final-head CI.
N3 requires focused/native predecessor oracles, full `just check` lease, Boundary/build/format,
source byte parity, full cut/retry/adversarial suite and independent final-head review/CI.
Use repository native MTP `--project`/`--solution` and run-tests/binlog skills for executable work.

Immediate coordinator action: obtain independent high R1 verdict, reconcile N1, explicitly record
whether entry-before-selection is the accepted N2 outcome, then freeze these manifests. If current
C3a/Result2 consumption is required, stop N2/N3 and retain this report as NO-GO evidence instead.

## Executed evidence, limits and reproduction

Final bounded probe at the recorded base: Python3.14.6 (`/opt/homebrew/bin/python3`), exit0,8.60s.
It replays/matches both Normal/NONE frozen goldens, both actual direct-decline goldens, both CP12
empty-entry traces, ten expected rejection probes and seed1/cursor2 scratch result arithmetic.
No new authoritative completion, selection, commitment or result is generated. Two early script
attempts incorrectly treated the fixture's descriptive `fixtureBoundary` string as the boundary
object; they failed with TypeError/JSONDecodeError. Corrected probe reads the retained
`boundaries[0].canonicalUtf8`; final results above come from the corrected run. This is probe repair,
not a semantic RED/GREEN development cycle. No .NET or full-suite run claimed; unchanged gate
ownership stays with N1/coordinator.

Reproduce the following standalone scratch script from any exact-base checkout. Save the fenced
code to `/private/tmp/r1-positive-entry-probe.py`, then run:

```sh
python3 -B /private/tmp/r1-positive-entry-probe.py /absolute/path/to/sandtable
```

The retained code below has no source writes/monkeypatches; imports existing exact-base oracles.

```python
import copy, importlib.util, json, sys
from pathlib import Path
root=Path(sys.argv[1]); specs=root/'docs/specs'
def load(name):
    spec=importlib.util.spec_from_file_location(name,specs/f'verify-combat-{name}-v1.py')
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
rd=load('reserve-designation'); steps=load('selection-steps')
held=load('inherited-reserve-cycle'); lifecycle=load('inherited-movement-lifecycle')
closure=load('inherited-reaction-closure'); selection=load('inherited-selection')
cases=json.loads(rd.FIXTURE.read_text())['cases']; template=json.loads(json.loads(steps.FIXTURE.read_text())['boundaries'][0]['canonicalUtf8'])
print('BASE',root, 'PYTHON',sys.version.split()[0])
print('C3A_REQUEST',steps.env.binding(steps.env.Context().request()), 'seed',steps.env.Context().request()['randomState']['seed'])
def reject(label,fn):
    try: fn()
    except ValueError as error: print('REJECT',label,str(error));return
    raise AssertionError('unexpected acceptance: '+label)
for choice in ('act-first','act-last'):
    case=next(c for c in cases if c['name']==f'normal-{choice}-none')
    result=rd.trace(case);assert rd.goldens(result)==case['goldens']
    q,created,preamble,weather,stage,inputs,reserve,states=result; state=states[-1]
    history=(q,created,preamble,weather,stage,reserve)
    actor=state['firstActingSide'];world=state['world'];a=next(e for e in world['elements'] if rd.world.SIDES[e['elementId']]==actor)
    d=next(e for e in world['elements'] if rd.world.SIDES[e['elementId']]!=actor)
    assert d['currentLocationId'] in rd.world.GRAPH[a['currentLocationId']]
    print('OPENING',actor,'seed',q['randomState']['seed'],'cursor',state['randomState']['nextByteCursor'],'version',state['stateVersion'],'receipts',len(state['receipts']),'binding',state['creationBinding'])
    print('PINS',actor,json.dumps({k:case['goldens'][k] for k in ('request','creation','event-1','state-11')},sort_keys=True))
    print('FACTS',actor,a['currentLocationId'],d['currentLocationId'],'cp',a['operationalState']['capabilityPointsExpended'],d['operationalState']['capabilityPointsExpended'],'weather',state['operationStageWeather'][0]['kind'],'members',[(m['status'],m['history']['designationReceiptId']) for m in state['members']])
    # Candidate-only scratch has no invented completion event or receipt; no admission claim.
    diagnostic={'cycle':state['cycle'],'world':world,'creationBinding':state['creationBinding'],'weather':{'attackerKind':'normal','defenderKind':'normal'}}
    candidate=steps.candidate(diagnostic);assert candidate is not None
    print('SCRATCH_CANDIDATE',actor,json.dumps(candidate,sort_keys=True))
    reject(actor+' held-I reader on actual NONE',lambda:held.initial(state))
    reject(actor+' route lifecycle without moves',lambda:lifecycle.initial(*history,[]))
    reject(actor+' missing Reserve parent',lambda:rd.replay(q,created,preamble,weather,stage,[]+[b'{}']))
    # One-field diagnostic substitution in old synthetic boundary demonstrates exact request coupling.
    bound=copy.deepcopy(template);bound['creationBinding']=state['creationBinding']
    reject(actor+' C3a actual creation binding only',lambda:steps.boundary(steps.encode(bound)))
    bound=copy.deepcopy(template);bound['randomState']['seed']=q['randomState']['seed']
    reject(actor+' C3a actual seed only',lambda:steps.boundary(steps.encode(bound)))
for case in json.loads(closure.FIXTURE.read_text())['cases']:
    if case['kind']!='decline-reaction-window': continue
    result=closure.trace(case);assert closure.goldens(result)==case['goldens']
    state=result[-1];actor=state['firstActingSide'];own=next(e for e in state['world']['elements'] if rd.world.SIDES[e['elementId']]==actor)
    enemy=next(e for e in state['world']['elements'] if rd.world.SIDES[e['elementId']]!=actor)
    print('ACTUAL_REACTION_DECLINE',actor,'version',state['stateVersion'],'cp',own['operationalState']['capabilityPointsExpended']['numerator'],enemy['operationalState']['capabilityPointsExpended']['numerator'],'adjacent',enemy['currentLocationId'] in rd.world.GRAPH[own['currentLocationId']],'flow',state['breakdownFlow']['kind'],'cursor',state['randomState']['nextByteCursor'])
for actor in ('axis','commonwealth'):
    args,events,entry=selection.source_trace(actor,6);boundary=selection.admit(*args,events)
    assert boundary['assessment']['candidateIds']==[]
    print('ACTUAL_EMPTY_SELECTION',actor,'cp',boundary['assessment']['actingCapabilityPointsExpended']['numerator'],'adjacent',boundary['assessment']['adjacent'],'candidates',boundary['assessment']['candidateIds'])
print('PASS bounded probes; no completion/selection/round/result successor fabricated')
spec=importlib.util.spec_from_file_location('result2',specs/'verify-combat-result-settlement-v2.py')
result2=importlib.util.module_from_spec(spec);spec.loader.exec_module(result2)
labels,dice,cursors,_,_,role=result2.rng.trace(1,2)
rules=result2.rules_inputs();lookup={v['coordinate']:v['adjustment'] for v in rules['morale']}
am,dm=10*dice[0]+dice[1],10*dice[2]+dice[3];ac,dc=10*dice[4]+dice[5],10*dice[6]+dice[7]
facts=result2.world.result_facts(lookup[am]-lookup[dm],ac,dc,dice[-1] if role else None)
assert tuple(dice)==(5,2,6,1,6,2,2,2) and cursors[-1]==10
assert facts['differential']==0 and facts['attackerPercent']==0 and facts['defenderPercent']==15 and not facts['rawEngaged'] and facts['requiredRetreat']==0 and facts['capturedRole'] is None
print('SCRATCH_RESULT seed1 cursor2 dice',dice,'endCursor',cursors[-1],json.dumps(facts,sort_keys=True))
```

## Base source pins and static verification

The exact-base sources below were hashed without modifying any file. Frozen source goldens in O1
were independently regenerated by the corrected probe; their request/Created/opening/state pins
are also printed by the embedded reproduction script. `git diff --exit-code 907f414 -- src tests
docs/specs docs/design docs/roadmap` passed; all protected tracked paths remain byte-identical.
Static verification passed four Markdown artifacts/19 local links/embedded Python AST, and
`git diff --check` passed. Untracked Markdown whitespace was checked separately before review.

| Evidence path at base907f414 | SHA256 |
| --- | --- |
| `src/Cna.Core/Campaigns/CampaignCombatReserveOpening.cs` | `fb01d607de3db44a97ef94ea6ce617d8ca811d5d438a47207cd2afd18251d7f1` |
| `src/Cna.Core/Campaigns/CampaignCombatInheritedReserveCycle.cs` | `03f6e559c2d061f13fe10cf29a9b1e691f602b38d6a62c9e62c6f5f50368e791` |
| `src/Cna.Core/Campaigns/CampaignCombatMovementLifecycle.cs` | `e63a4d0225f5347ce6a70a855c34272110ce75a3b258591b50115c3ca5fb07aa` |
| `src/Cna.Core/Campaigns/CampaignCombatBreakdownCompletion.cs` | `fe24d5a524383c1e5137b3d9e8a0050fc21fd381c4dc7e59812d68dc3a24dc42` |
| `src/Cna.Core/Campaigns/CampaignCombatCertification.cs` | `2d3859c248783d07772753b5105848c4555cd7b7e146ea7cd9d740316cb705da` |
| `src/Cna.Core/Campaigns/CampaignCombatSelectionSteps.cs` | `32551f54656c63f69d2517f7ab446fec4951679ff311154a26df84940c009efb` |
| `src/Cna.Core/Campaigns/CampaignCombatSelectionStepsCodec.cs` | `90ef43ea5e10be1c8473c0991c591304c16df218ef3f593117ea64b436e09d30` |
| `src/Cna.Core/Campaigns/CampaignBreakdownLifecycleFactory.cs` | `07b97600a212bf2b255004bafdeba7da0804624198be354814d25ede5f0d4413` |
| `docs/specs/verify-combat-reserve-designation-v1.py` | `fa40ec6b10d0d5ecc113aefba47ff4df44ae616851acdc827e9f676f2412e465` |
| `docs/specs/fixtures/combat-reserve-designation-v1.json` | `6c8abb2038bb61c739e3bd3cab5c8adf00689c265ec3c6bc6ea29bebe66b12c2` |
| `docs/specs/verify-combat-selection-steps-v1.py` | `dc037282f74c34246d350faebeea55e81677d27dd181de858b8270e4eaa73c86` |
| `docs/specs/verify-combat-result-settlement-v2.py` | `a6a781976f26b78a9dc098ebfbb3b516284aa1d5873744d96d8fa23aa50d83a2` |
