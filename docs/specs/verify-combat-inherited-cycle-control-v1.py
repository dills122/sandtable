#!/usr/bin/env python3
"""Creation-rooted released-I guarded repeat/finish composition."""
from __future__ import annotations
import copy, importlib.util, json, sys
from functools import lru_cache
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
FIXTURE = ROOT/'fixtures/combat-inherited-cycle-control-v1.json'
INVENTORY = json.loads((ROOT/'combat-inherited-cycle-control-v1.schema.json').read_text())
SOURCE_PATHS = [
    'docs/specs/combat-inherited-reserve-release-v1.schema.json',
    'docs/specs/verify-combat-inherited-reserve-release-v1.py',
    'docs/specs/fixtures/combat-inherited-reserve-release-v1.json',
    'docs/specs/combat-inherited-armed-continuation-v1.schema.json',
    'docs/specs/verify-combat-inherited-armed-continuation-v1.py',
    'docs/specs/fixtures/combat-inherited-armed-continuation-v1.json',
    'docs/specs/combat-cycle-control-v1.schema.json',
    'docs/specs/verify-combat-cycle-control-v1.py',
    'docs/specs/fixtures/combat-cycle-control-v1.json']

def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None: raise RuntimeError(f'cannot load {path}')
    out = importlib.util.module_from_spec(spec); spec.loader.exec_module(out); return out

iac = module('inherited_armed', ROOT/'verify-combat-inherited-armed-continuation-v1.py')
cyc = module('cycle_control', ROOT/'verify-combat-cycle-control-v1.py')
encode, sha = iac.encode, iac.sha
SCHEMA = {k: [tuple(f.split(':')) for f in v.split()] for k, v in INVENTORY['objects'].items()}

class Invalid(ValueError):
    def __init__(self, code): self.code = f'CMB-ICC-{code:03}'; super().__init__(self.code)
def require(ok, code):
    if not ok: raise Invalid(code)
def translated(call, code=6):
    try: return call()
    except Invalid: raise
    except ValueError as error: raise Invalid(code) from error

def typed(v, kind, depth=0):
    require(depth <= INVENTORY['limits']['depth'], 1)
    if kind.endswith('?'):
        if v is not None: typed(v, kind[:-1], depth)
    elif kind in SCHEMA:
        require(type(v) is dict and set(v) == {k for k, _ in SCHEMA[kind]}, 1)
        for k, t in SCHEMA[kind]: typed(v[k], t, depth+1)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v) <= INVENTORY['limits']['arrayItems'], 1)
        for x in v: typed(x, kind[:-2], depth+1)
    elif kind == 'Proof': translated(lambda: iac.typed(v, kind, depth), 1)
    elif kind == 'CycleControlBase': translated(lambda: cyc.typed(v, kind, depth), 1)
    else: translated(lambda: iac.typed(v, kind, depth), 1)

def canonical(v, kind):
    if kind.endswith('?'): return None if v is None else canonical(v, kind[:-1])
    if kind in SCHEMA: return {k: canonical(v[k], t) for k, t in SCHEMA[kind]}
    if kind.endswith('[]'): return [canonical(x, kind[:-2]) for x in v]
    if kind == 'Proof': return iac.canonical(v, kind)
    if kind == 'CycleControlBase': return cyc.canonical(v, kind)
    return iac.canonical(v, kind)

def raw(v, kind):
    typed(v, kind); data = encode(canonical(v, kind))
    require(len(data) <= INVENTORY['limits']['bytes'], 1); return data
def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= INVENTORY['limits']['bytes'], 1)
    def pairs(items):
        out = {}
        for k, v in items: require(k not in out, 1); out[k] = v
        return out
    try: v = json.loads(data.decode('utf8'), object_pairs_hook=pairs,
        parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error: raise Invalid(1) from error
    require(raw(v, kind) == data, 8); return v

def leaves(v, path=()):
    if type(v) is dict:
        for k, x in v.items(): yield from leaves(x, path+(k,))
    elif type(v) is list:
        for i, x in enumerate(v): yield from leaves(x, path+(i,))
    else: yield path, v
def changed(v, path, replacement):
    out = copy.deepcopy(v); target = out
    for step in path[:-1]: target = target[step]
    target[path[-1]] = replacement; return out
def different(v):
    if v is None: return 'unexpected'
    if type(v) is bool: return not v
    if type(v) is int: return v+1
    if type(v) is str:
        if v.startswith('sha256:'):
            zero = 'sha256:'+'0'*64; return zero if v != zero else 'sha256:'+'1'*64
        return v+'.changed'
    raise TypeError(type(v))

@lru_cache(maxsize=2)
def cached_derived(actor):
    result = iac.source_trace(actor); proof = iac.proof_from(actor, result)
    predecessor, inherited, states, release_inputs, release_events = result
    terminal = states[-1]; inherited_control = inherited[2][-1]
    proof_hash = sha(iac.raw(proof, 'Proof'))
    control_base = dict(contractVersion=1,
        profile='inherited-armed-'+proof_hash.removeprefix('sha256:'),
        releaseBase=copy.deepcopy(predecessor['releaseBase']),
        releaseInputs=copy.deepcopy(release_inputs),
        releaseEvents=[json.loads(event) for event in release_events],
        world=copy.deepcopy(inherited_control['base']['world']),
        movementEnd=copy.deepcopy(inherited_control['movementEnd']), progress=[], targetUses=[])
    translated(lambda: cyc.raw(control_base, 'CycleControlBase'), 4)
    base = dict(contractVersion=1, actor=actor,
        releaseControlHash=sha(iac.irr.raw(terminal, 'Control')),
        armedProofHash=proof_hash, armedProof=copy.deepcopy(proof), controlBase=control_base)
    raw(base, 'InheritedCycleControlBase'); return base, result, proof
def derived(actor):
    require(actor in ('axis', 'commonwealth'), 2); return copy.deepcopy(cached_derived(actor))

def validate(base, result, proof):
    predecessor, inherited, states, release_inputs, release_events = result
    terminal = states[-1]; release = terminal['release']; inherited_control = inherited[2][-1]
    cb = base['controlBase']; proof_data = iac.raw(base['armedProof'], 'Proof')
    translated(lambda: iac.read_proof(proof_data, base['actor'], proof), 4)
    require(base['contractVersion'] == 1 and base['releaseControlHash'] == proof['predecessorHash']
        == sha(iac.irr.raw(terminal, 'Control')) and base['armedProofHash'] == sha(proof_data), 4)
    require(cb['profile'] == 'inherited-armed-'+base['armedProofHash'].removeprefix('sha256:')
        and cb['releaseBase'] == predecessor['releaseBase'] and cb['releaseInputs'] == release_inputs
        and cb['releaseEvents'] == [json.loads(e) for e in release_events]
        and cb['world'] == inherited_control['base']['world']
        and cb['movementEnd'] == inherited_control['movementEnd']
        and cb['progress'] == [] and cb['targetUses'] == [], 4)
    require(terminal['closed'] and release['status'] == 'completed' and release['stateVersion'] == 25
        and not release['pending'] and proof['supported'] and proof['nextOrdinal'] == 2
        and proof['currentCycle'] == cb['releaseBase']['cycle']
        and proof['releaseCompletionReceiptId'] == release['completionReceiptId']
        and proof['movementCompletionReceiptId'] == cb['movementEnd']['completionReceiptId']
        and proof['retainedWorldHash'] == release['retainedWorldHash']
        and proof['randomStateHash'] == sha(iac.irr.release.raw(release['randomState'], 'Random')), 5)
    require(proof['member'] == release['members'][0] and proof['progress'] == terminal['progress'][0]
        and (proof['actingAmmunition'], proof['actingToe'], proof['defendingToe']) == (10, 10, 10)
        and len(proof['assessment']['candidateIds']) == 1 and proof['emptyMovementSupported']
        and proof['emptyBreakdownSupported'] and proof['emptyPrestepsSupported']
        and proof['targetUseAvailable'] and proof['offensiveUseAvailable']
        and proof['immediateObligationsClear'], 5)
    return release

def authenticate(base, expected=None):
    typed(base, 'InheritedCycleControlBase')
    expected = derived(base['actor']) if expected is None else expected
    expected_base, result, proof = expected
    require(raw(base, 'InheritedCycleControlBase') == raw(expected_base, 'InheritedCycleControlBase'), 4)
    validate(expected_base, result, proof); return expected
def read_base(data, expected=None):
    value = parse(data, 'InheritedCycleControlBase'); bundle = authenticate(value, expected)
    require(data == raw(bundle[0], 'InheritedCycleControlBase'), 4); return bundle

def assessment(base, expected=None):
    expected_base, result, proof = authenticate(base, expected); release = result[2][-1]['release']
    member = proof['member']; cb = expected_base['controlBase']
    witness = dict(kind='combat', unit=copy.deepcopy(member['unit']),
        destinationLocationId=proof['assessment']['defendingLocationId'], terrainCost=0,
        breakOffCost=0, afterCp=member['spentCp']['numerator'], excessCpDp=0,
        usesReleaseException=False)
    value = dict(releaseCompletionReceiptId=release['completionReceiptId'],
        movementCompletionReceiptId=cb['movementEnd']['completionReceiptId'],
        progress=[copy.deepcopy(proof['progress'])], witnesses=[witness],
        combatAssessment='supported-armed-combat')
    translated(lambda: cyc.raw(value, 'CycleAssessment'), 5)
    require(cyc.control_mode(value) == 'owner-choice', 5); return value

def initial(base, expected=None):
    expected_base, result, proof = authenticate(base, expected); cb = expected_base['controlBase']
    release = result[2][-1]['release']; cycle = cb['releaseBase']['cycle']
    value = dict(contractVersion=1, baseHash=sha(cyc.raw(cb, 'CycleControlBase')),
        controlId=cyc.control_id(cb), stateVersion=release['stateVersion'], prefix=release['prefix'],
        status='unopened', positionId=cb['releaseBase']['positionId'], activeCycle=copy.deepcopy(cycle),
        closure=None, decisionId=None, timing=None, openingClockFailure=False,
        acceptedHighWater=release['acceptedHighWater'], assessment=None,
        members=copy.deepcopy(release['members']),
        world=iac.irr.release.project_world(cb['world'], cb['releaseBase'], release),
        randomState=copy.deepcopy(release['randomState']), attackHistory=copy.deepcopy(release['attackHistory']),
        targetUses=[], nextCycleProgress=[], receipts=[])
    translated(lambda: cyc.raw(value, 'CycleControlState'), 5)
    require(value['stateVersion'] == 25 and cycle['ordinal'] == 1
        and cycle['actingSide'] == base['actor'], 5); return value

def transition(base, prior, inp, expected=None):
    expected_base, result, proof = authenticate(base, expected); cb = expected_base['controlBase']
    frozen = assessment(expected_base, (expected_base, result, proof)); original = cyc.assess
    def inherited_assessment(candidate):
        require(cyc.raw(candidate, 'CycleControlBase') == cyc.raw(cb, 'CycleControlBase'), 4)
        return copy.deepcopy(frozen)
    cyc.assess = inherited_assessment
    try: return translated(lambda: cyc.transition(cb, prior, inp))
    finally: cyc.assess = original
def read_event(data, base, prior, inp, expected=None):
    translated(lambda: cyc.parse(data, 'CycleControlEvent'), 1)
    state, expected_data, _ = transition(base, prior, inp, expected)
    require(data == expected_data, 6); return state
def read_state(data, base, inputs, events, length=None, expected=None):
    translated(lambda: cyc.parse(data, 'CycleControlState'), 1)
    require(len(inputs) == len(events) <= INVENTORY['limits']['eventsPerCase'], 1)
    require(length is None or type(length) is int and 0 <= length <= len(events), 2)
    bundle = authenticate(base, expected); state = initial(bundle[0], bundle)
    for inp, event in zip(inputs[:length], events[:length]):
        state = read_event(event, bundle[0], state, inp, bundle)
    require(data == cyc.raw(state, 'CycleControlState'), 6); return state
def rejected(call, code=None):
    try: call()
    except ValueError as error:
        if code is not None: assert isinstance(error, Invalid) and error.code == f'CMB-ICC-{code:03}'
    else: raise AssertionError('invalid inherited cycle control admitted')

def trace(actor, action):
    require(action in ('repeat', 'finish'), 2); bundle = derived(actor); base = bundle[0]
    state = initial(base, bundle); first = copy.deepcopy(state); states = [state]; inputs = []; events = []
    opened_at = (state['acceptedHighWater'] or 0)+1000
    for kind, owner, now in (('open', False, opened_at), (action, True, opened_at+1)):
        inp = cyc.trusted(cyc.command(base['controlBase'], state, kind), actor if owner else 'system', now)
        state, event, _ = transition(base, state, inp, bundle); require(event is not None, 6)
        inputs.append(inp); events.append(event); states.append(state)
    cycle = base['controlBase']['releaseBase']['cycle']
    require([s['stateVersion'] for s in states] == [25, 26, 27]
        and state['world'] == first['world'] and state['randomState'] == first['randomState']
        and state['attackHistory'] == first['attackHistory'], 6)
    if action == 'repeat':
        require(state['status'] == 'repeated' and state['positionId'] == cyc.edge(cycle)['movementPositionId']
            and state['activeCycle']['ordinal'] == 2
            and iac.irr.release.scope(state['activeCycle']) == iac.irr.release.scope(cycle)
            and state['members'] == first['members'] and not state['targetUses']
            and not state['nextCycleProgress'], 6)
    else:
        require(state['status'] == 'finished' and state['activeCycle'] is None
            and state['positionId'] == cyc.edge(cycle)['finishPositionId'], 6)
        for old, new in zip(first['members'], state['members']):
            expected = copy.deepcopy(old); ex = expected['history']['nextMovement']
            if ex is not None and ex['status'] == 'pending':
                ex.update(status='expired', completionReceiptId=state['closure']['receiptId'])
            require(new == expected, 6)
    return base, states, inputs, events

def source_pins():
    repo = ROOT.parent.parent
    return [dict(path=p, sha256=sha((repo/p).read_bytes())) for p in SOURCE_PATHS]
def golden(result):
    base, states, _, events = result; data = raw(base, 'InheritedCycleControlBase')
    return dict(baseBytes=len(data), baseHash=sha(data),
        nestedBaseHash=sha(cyc.raw(base['controlBase'], 'CycleControlBase')),
        controlId=cyc.control_id(base['controlBase']),
        frames=[dict(cut=i, bytes=len(d), sha256=sha(d)) for i, d in enumerate(
            cyc.raw(s, 'CycleControlState') for s in states)],
        events=[dict(bytes=len(e), sha256=sha(e)) for e in events],
        terminalPositionId=states[-1]['positionId'], terminalPrefix=states[-1]['prefix'],
        terminalReceiptId=states[-1]['receipts'][-1]['receiptId'])
def raw_variants(data):
    return (data+b'\n', b'\xef\xbb\xbf'+data, b' '+data, data[:-1],
        data.replace(b'"contractVersion":1', b'"contractVersion":true', 1),
        data.replace(b'"contractVersion":1', b'"contractVersion":1.0', 1),
        b'{"extra":0,'+data[1:])

def verify_case(case):
    result = trace(case['actor'], case['action']); require(case['golden'] == golden(result), 6)
    base, states, inputs, events = result; bundle = derived(case['actor'])
    read_base(raw(base, 'InheritedCycleControlBase'), bundle)
    counts = dict(readbacks=1, mutations=0, raw=0, retries=0)
    for cut, state in enumerate(states):
        require(read_state(cyc.raw(state, 'CycleControlState'), base, inputs, events, cut, bundle) == state, 6)
        counts['readbacks'] += 1
    prior = states[0]
    for inp, event, state in zip(inputs, events, states[1:]):
        require(read_event(event, base, prior, inp, bundle) == state, 6)
        retry, retry_event, receipt = transition(base, state, inp, bundle)
        require(retry is state and retry_event is None and receipt == state['receipts'][-1]['receiptId'], 6)
        counts['readbacks'] += 1; counts['retries'] += 1; obj = json.loads(event)
        paths = [('stateVersion',), ('priorVersion',), ('priorPrefix',), ('configurationHash',),
            ('cycleId',), ('controlId',), ('receiptId',), ('effect','reason'),
            ('effect','assessment','combatAssessment'), ('effect','assessment','progress',0,'receiptId'),
            ('effect','assessment','witnesses',0,'destinationLocationId')]
        leaf_map = dict(leaves(obj))
        for path in paths:
            altered = changed(obj, path, different(leaf_map[path]))
            if path[0] == 'effect':
                altered.pop('receiptId'); altered['receiptId'] = 'cc.'+cyc.steps.digest(
                    cyc.INVENTORY['domains']['receipt'], encode(altered))
            try: encoded = cyc.raw(altered, 'CycleControlEvent')
            except ValueError: encoded = encode(altered)
            rejected(lambda encoded=encoded: read_event(encoded, base, prior, inp, bundle)); counts['mutations'] += 1
        for bad in raw_variants(event):
            rejected(lambda bad=bad: read_event(bad, base, prior, inp, bundle)); counts['raw'] += 1
        prior = state
    for path, leaf in leaves(base):
        altered = changed(base, path, different(leaf))
        try: data = raw(altered, 'InheritedCycleControlBase')
        except ValueError: data = encode(altered)
        rejected(lambda data=data: read_base(data, bundle)); counts['mutations'] += 1
    for bad in raw_variants(raw(base, 'InheritedCycleControlBase')):
        rejected(lambda bad=bad: read_base(bad, bundle)); counts['raw'] += 1
    final_data = cyc.raw(states[-1], 'CycleControlState')
    rejected(lambda: read_state(final_data, base, inputs, events[::-1], expected=bundle))
    rejected(lambda: read_state(final_data, base, inputs[:-1], events[:-1], expected=bundle)); counts['mutations'] += 2
    retry, event, receipt = transition(base, states[-1], inputs[0], bundle)
    require(retry is states[-1] and event is None and receipt == states[-1]['receipts'][0]['receiptId'], 6)
    counts['retries'] += 1; return counts

def open_state(actor='axis', clock=True):
    bundle = derived(actor); base = bundle[0]; first = initial(base, bundle); now = (first['acceptedHighWater'] or 0)+1000
    inp = cyc.trusted(cyc.command(base['controlBase'], first, 'open'), now=now if clock else None, available=clock)
    opened, event, _ = transition(base, first, inp, bundle); return base, bundle, first, opened, inp, event, now
def boundary_checks():
    n = 0; base, bundle, first, opened, _, _, now = open_state(); deadline = opened['timing']['deadlineUnixMilliseconds']
    for kind in ('repeat', 'finish'):
        inp = cyc.trusted(cyc.command(base['controlBase'], opened, kind), 'axis', deadline)
        rejected(lambda inp=inp: transition(base, opened, inp, bundle)); n += 1
    early = cyc.trusted(cyc.command(base['controlBase'], opened, 'expire'), now=deadline-1)
    require(transition(base, opened, early, bundle) == (opened, None, None), 6); n += 1
    at = copy.deepcopy(early); at['admittedAt'] = deadline
    finished, event, receipt = transition(base, opened, at, bundle)
    require(finished['status'] == 'finished' and json.loads(event)['author'] == 'system'
        and transition(base, finished, at, bundle) == (finished, None, receipt), 6); n += 1
    unavailable = cyc.trusted(cyc.command(base['controlBase'], opened, 'unavailable'), now=None)
    state, event, _ = transition(base, opened, unavailable, bundle)
    require(state['status'] == 'finished' and json.loads(event)['author'] == 'system', 6); n += 1
    regression = cyc.trusted(cyc.command(base['controlBase'], opened, 'repeat'), 'axis', now-1)
    state, event, _ = transition(base, opened, regression, bundle)
    require(state['status'] == 'finished' and json.loads(event)['author'] == 'system', 6); n += 1
    lost_base, lost_bundle, _, lost_open, _, _, _ = open_state(clock=False)
    fallback = cyc.trusted(cyc.command(lost_base['controlBase'], lost_open, 'fallback-step'))
    state, event, _ = transition(lost_base, lost_open, fallback, lost_bundle)
    require(lost_open['openingClockFailure'] and state['status'] == 'finished'
        and json.loads(event)['author'] == 'system', 6); n += 1
    wrong = cyc.trusted(cyc.command(base['controlBase'], opened, 'repeat'), 'commonwealth', now+1)
    rejected(lambda: transition(base, opened, wrong, bundle)); n += 1
    overflow = copy.deepcopy(opened); overflow['stateVersion'] = 2**63-1
    inp = cyc.trusted(cyc.command(base['controlBase'], overflow, 'repeat'), 'axis', now+1)
    rejected(lambda: transition(base, overflow, inp, bundle)); n += 1
    forged = copy.deepcopy(base); forged['armedProof']['assessment']['candidateIds'][0] += '.forged'
    forged['armedProofHash'] = sha(iac.raw(forged['armedProof'], 'Proof'))
    forged['controlBase']['profile'] = 'inherited-armed-'+forged['armedProofHash'].removeprefix('sha256:')
    rejected(lambda: read_base(raw(forged, 'InheritedCycleControlBase'), bundle)); n += 1
    unsupported = copy.deepcopy(base); unsupported['armedProof']['supported'] = False
    rejected(lambda: read_base(raw(unsupported, 'InheritedCycleControlBase'), bundle)); n += 1
    oversized = copy.deepcopy(base); oversized['armedProof']['support'] *= 129
    rejected(lambda: raw(oversized, 'InheritedCycleControlBase'), 1); n += 1
    for bad in (b'x'*(INVENTORY['limits']['bytes']+1), raw(base, 'InheritedCycleControlBase').replace(
            b'"contractVersion":1', b'"contractVersion":1e0', 1)):
        rejected(lambda bad=bad: read_base(bad, bundle)); n += 1
    return n

def generated_fixture():
    cases = [dict(actor=a, action=k, golden=golden(trace(a, k)))
        for a in ('axis','commonwealth') for k in ('repeat','finish')]
    return dict(contract='combat-inherited-cycle-control-v1', cases=cases, sourcePins=source_pins(),
        goldenMethod='canonical ASCII JSON; SHA-256 with sha256: prefix')
def main():
    if '--goldens' in sys.argv: print(json.dumps(generated_fixture(), indent=2)); return
    fixture = json.loads(FIXTURE.read_text())
    require(set(fixture) == {'contract','cases','sourcePins','goldenMethod'}
        and fixture['contract'] == 'combat-inherited-cycle-control-v1', 1)
    require([(c['actor'],c['action']) for c in fixture['cases']] == [('axis','repeat'),('axis','finish'),
        ('commonwealth','repeat'),('commonwealth','finish')], 7)
    require(len(fixture['cases']) == INVENTORY['limits']['cases'] and fixture['sourcePins'] == source_pins(), 4)
    totals = dict(readbacks=0, mutations=0, raw=0, retries=0)
    for case in fixture['cases']:
        for key, value in verify_case(case).items(): totals[key] += value
    bounds = boundary_checks()
    print(f"combat inherited cycle control v1: 4 traces, {totals['readbacks']} readbacks, "
        f"{totals['retries']} retries, {totals['mutations']} mutations, "
        f"{totals['raw']} raw rejects, {bounds} boundary checks")
if __name__ == '__main__': main()
