#!/usr/bin/env python3
"""Private Result2 settled control. Earlier Movement trust stays synthetic-pre-combat."""
import copy
import json
import sys
from functools import lru_cache
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
import importlib.util
spec = importlib.util.spec_from_file_location('settled_continuation_for_control', ROOT/'verify-combat-settled-continuation-v1.py')
sct = importlib.util.module_from_spec(spec)
spec.loader.exec_module(sct)
ctl, rel, steps = sct.ctl, sct.rel, sct.steps
encode, sha = sct.encode, sct.sha
INVENTORY = json.loads((ROOT/'combat-settled-control-v1.schema.json').read_bytes())
OBJECTS = {k: [tuple(f.split(':')) for f in v.split()] for k, v in INVENTORY['objects'].items()}
FIXTURE = ROOT/'fixtures/combat-settled-control-v1.json'

class Invalid(ValueError):
    def __init__(self, code):
        self.code = f'CMB-SCC-{code:03}'
        super().__init__(self.code)

def require(ok, code):
    if not ok: raise Invalid(code)

def typed(value, kind, depth=0):
    require(depth <= 32, 1)
    if kind == 'SettledControlState':
        require(type(value) is dict and type(value.get('members')) is list and len(value['members']) <= 32, 1)
    if kind.endswith('?'):
        if value is not None: typed(value, kind[:-1], depth)
    elif kind in OBJECTS:
        require(type(value) is dict and set(value) == {k for k, _ in OBJECTS[kind]}, 1)
        for key, child in OBJECTS[kind]: typed(value[key], child, depth+1)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value) <= 512, 1)
        for item in value: typed(item, kind[:-2], depth+1)
    elif kind == 'utf8':
        require(type(value) is str and 0 < len(value) <= 1048576, 1)
    else:
        try: ctl.typed(value, kind, depth)
        except ValueError as error: raise Invalid(1) from error

def canonical(value, kind):
    if kind.endswith('?'): return None if value is None else canonical(value, kind[:-1])
    if kind in OBJECTS: return {k: canonical(value[k], t) for k, t in OBJECTS[kind]}
    if kind.endswith('[]'): return [canonical(x, kind[:-2]) for x in value]
    if kind == 'utf8': return value
    return ctl.canonical(value, kind)

def raw(value, kind):
    typed(value, kind); data = encode(canonical(value, kind)); require(len(data) <= 1048576, 1)
    return data

def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= 1048576, 1)
    def pairs(items):
        out = {}
        for key, value in items: require(key not in out, 1); out[key] = value
        return out
    try: value = json.loads(data.decode('utf8'), object_pairs_hook=pairs,
                            parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error: raise Invalid(1) from error
    require(raw(value, kind) == data, 8)
    return value

@lru_cache(maxsize=128)
def _admit(data):
    b = parse(data, 'SettledControlBase'); require(b['contractVersion'] == 1, 3)
    try: sct.read_proof(b['proofCanonicalUtf8'].encode(), b['packetCanonicalUtf8'].encode())
    except ValueError as error: raise Invalid(4) from error
    return b['proofCanonicalUtf8'].encode()

def read_base(data):
    require(type(data) is bytes, 1); proof = _admit(data)
    return parse(data, 'SettledControlBase'), sct.parse(proof, 'SettledContinuationProof')

def base_for(row):
    return raw(dict(contractVersion=1, packetCanonicalUtf8=row['packetCanonicalUtf8'],
                    proofCanonicalUtf8=row['proofCanonicalUtf8']), 'SettledControlBase')

def assess(b):
    _, proof = read_base(raw(b, 'SettledControlBase'))
    require(proof['witnessOutcome'] == 'supported' and proof['unsupportedReason'] is None, 3)
    return dict(releaseCompletionReceiptId=proof['releaseCompletionReceiptId'],
                movementCompletionReceiptId=proof['movementEnd']['proof']['completionReceiptId'],
                progress=copy.deepcopy(proof['progress']), witnesses=copy.deepcopy(proof['witnesses']),
                combatAssessment='no-own-ammunition', witnessOutcome=proof['witnessOutcome'],
                unsupportedReason=None, sourceProofId=sct.proof_id(b['proofCanonicalUtf8'].encode()))

def mode(a):
    require(a['witnessOutcome'] == 'supported', 3)
    if not a['witnesses']: return 'no-continuation'
    if not a['progress']: return 'no-material-progress'
    return 'owner-choice'


def basis(b):
    admitted, proof = read_base(raw(b, 'SettledControlBase'))
    packet = sct.parse(admitted['packetCanonicalUtf8'].encode(), 'SettledPacket')
    return rel.parse(packet['releaseBaseCanonicalUtf8'].encode(), 'ReleaseBase'), proof

def control_id(b):
    return 'sctl.'+steps.digest(INVENTORY['domains']['control'], raw(b, 'SettledControlBase'))

def initial(b):
    _, p = basis(b)
    require(len(p['members']) <= 32, 1)
    return dict(contractVersion=1, sourceProofId=sct.proof_id(b['proofCanonicalUtf8'].encode()),
                baseHash=sha(raw(b, 'SettledControlBase')), controlId=control_id(b),
                stateVersion=p['stateVersion'], prefix=p['prefix'], status='unopened', positionId=p['positionId'],
                activeCycle=copy.deepcopy(p['cycle']), closure=None, decisionId=None, timing=None,
                openingClockFailure=False, acceptedHighWater=None, assessment=None, members=copy.deepcopy(p['members']),
                world=copy.deepcopy(p['world']), randomState=copy.deepcopy(p['randomState']),
                attackHistory=copy.deepcopy(p['attackHistory']), targetUses=copy.deepcopy(p['targetUses']),
                nextCycleProgress=[], receipts=[])

def command(b, state, kind):
    rb, _ = basis(b)
    return dict(contractVersion=1, kind=kind, controlId=state['controlId'], cycleId=rel.cycle_id(rb),
                expectedPriorVersion=None if kind in ('expire', 'unavailable') else state['stateVersion'],
                decisionId=None if kind == 'open' else state['decisionId'])

def trusted(cmd, actor='system', now=None, available=True):
    return dict(command=cmd, actor=actor, admittedAt=now, clockAvailable=available)

def _transition(b, prior, inp):
    typed(prior, 'SettledControlState'); typed(inp, 'SettledControlInput'); rb, proof = basis(b); c = rb['cycle']; cmd = inp['command']; kind = cmd['kind']; actor = inp['actor']
    require(prior['contractVersion'] == cmd['contractVersion'] == 1 and kind in ('open', 'repeat', 'finish', 'expire', 'unavailable', 'fallback-step'), 3)
    require(prior['sourceProofId'] == sct.proof_id(b['proofCanonicalUtf8'].encode()), 4)
    require(prior['baseHash'] == sha(raw(b, 'SettledControlBase')) and cmd['controlId'] == prior['controlId'] == control_id(b) and cmd['cycleId'] == rel.cycle_id(rb), 4)
    require(actor == c['actingSide'] if kind in ('repeat', 'finish') else actor == 'system', 4)
    require((cmd['expectedPriorVersion'] is None) == (kind in ('expire', 'unavailable')), 3)
    require(kind != 'open' or cmd['decisionId'] is None, 3)
    ch = sha(raw(cmd, 'SettledControlCommand')); receipt = next((x for x in prior['receipts'] if x['commandHash'] == ch and x['actor'] == actor), None)
    if receipt: return prior, None, receipt['receiptId']
    if kind in ('expire', 'unavailable') and (prior['status'] != 'open' or cmd['decisionId'] != prior['decisionId']): return prior, None, None
    require(prior['status'] in ('unopened', 'open') and len(prior['receipts']) < 2 and prior['stateVersion'] < 2**63-1, 7)
    if kind not in ('expire', 'unavailable'): require(cmd['expectedPriorVersion'] == prior['stateVersion'], 6)
    if kind != 'open': require(cmd['decisionId'] == prior['decisionId'], 6)
    s = copy.deepcopy(prior); now = inp['admittedAt']; author = actor; reason = 'owner-choice'; effkind = None; nxt = None
    if kind == 'open':
        require(prior['status'] == 'unopened', 6); s['assessment'] = assess(b); selected_mode = mode(s['assessment'])
        if selected_mode != 'owner-choice': effkind = 'phase-finished'; reason = selected_mode
        else:
            require(c['ordinal'] < rel.MAX and prior['stateVersion'] <= 2**63-3, 7)
            s['decisionId'] = s['controlId']+'.decision'; lost = not inp['clockAvailable'] or now is None or s['acceptedHighWater'] is not None and now < s['acceptedHighWater']
            try: s['timing'] = None if lost else steps.inputs.make_timing(rel.CONTEXT.config, 'cycle-control', now)
            except steps.inputs.Invalid: s['timing'] = None
            s['openingClockFailure'] = s['timing'] is None
            if s['timing'] is not None: s['acceptedHighWater'] = now
            effkind = 'control-opened'; s['status'] = 'open'
    else:
        require(prior['status'] == 'open', 5); t = s['timing']
        lost = s['openingClockFailure'] or not inp['clockAvailable'] or now is None or s['acceptedHighWater'] is not None and now < s['acceptedHighWater']
        late = t is not None and now is not None and now >= t['deadlineUnixMilliseconds']
        if kind == 'fallback-step': require(s['openingClockFailure'], 5)
        if kind == 'expire' and not lost and not late: return prior, None, None
        if kind in ('repeat', 'finish') and not lost: require(not late, 5)
        if not lost:
            s['acceptedHighWater'] = now; s['timing']['highWaterUnixMilliseconds'] = now
        if lost or kind in ('expire', 'unavailable', 'fallback-step'):
            author = 'system'; effkind = 'phase-finished'; reason = 'opening-clock-unavailable' if s['openingClockFailure'] else 'clock-unavailable' if lost else 'deadline' if kind == 'expire' else 'controller-unavailable'
        else: effkind = 'cycle-repeated' if kind == 'repeat' else 'phase-finished'
    successor = rb['positionId']; expired = []
    if effkind == 'cycle-repeated':
        require(mode(s['assessment']) == 'owner-choice' and c['ordinal'] < rel.MAX, 7)
        nxt = copy.deepcopy(c); nxt.update(ordinal=c['ordinal']+1, openedAuthorityVersion=prior['stateVersion']+1, openingPrefix=prior['prefix'])
        ctl.cycle_id(nxt, rb['firstActingSide']); successor = ctl.edge(c)['movementPositionId']; s['status'] = 'repeated'; s['activeCycle'] = nxt; s['targetUses'] = []; s['nextCycleProgress'] = []
    elif effkind == 'phase-finished':
        successor = ctl.edge(c)['finishPositionId']; s['status'] = 'finished'; s['activeCycle'] = None
        expired = [copy.deepcopy(m['unit']) for m in s['members'] if m['history']['nextMovement'] is not None and m['history']['nextMovement']['status'] == 'pending']
    eff = dict(kind=effkind, reason=reason, assessment=s['assessment'], timing=s['timing'], openingClockFailure=s['openingClockFailure'], successorPositionId=successor, nextCycle=nxt, expiredUnits=expired)
    event = dict(contractVersion=1, sourceProofId=prior['sourceProofId'], eventType={'control-opened':'movement-combat-control-opened','cycle-repeated':'movement-combat-cycle-repeated','phase-finished':'movement-combat-phase-finished'}[effkind], author=author,
        campaignId=c['campaignId'], rulesetHash=c['rulesetHash'], configurationHash=c['admittedPolicyBundleDigest'], cycleId=cmd['cycleId'], positionId=rb['positionId'],
        baseHash=prior['baseHash'], controlId=prior['controlId'], priorVersion=prior['stateVersion'], stateVersion=prior['stateVersion']+1, priorPrefix=prior['prefix'], input=canonical(inp, 'SettledControlInput'), effect=canonical(eff, 'SettledControlEffect'))
    rid = 'scc.'+steps.digest(INVENTORY['domains']['receipt'], encode(event)); event['receiptId'] = rid; data = raw(event, 'SettledControlEvent')
    for m in s['members']:
        if m['unit'] in expired: m['history']['nextMovement'].update(status='expired', completionReceiptId=rid)
    if effkind != 'control-opened': s['closure'] = dict(cycleId=cmd['cycleId'], ordinal=c['ordinal'], outcome=s['status'], receiptId=rid)
    s['positionId'] = successor; s['stateVersion'] = event['stateVersion']; s['prefix'] = steps.seq.prefix_event(prior['prefix'], data)
    s['receipts'].append(dict(commandHash=ch, eventHash=sha(data), receiptId=rid, actor=actor, stateVersion=s['stateVersion']))
    raw(s, 'SettledControlState'); return s, data, rid


def transition(b, prior, inp):
    # Internal pure transition; caller caches are admitted only by replay/apply below.
    try: return _transition(b, prior, inp)
    except (ctl.Invalid, rel.Invalid, steps.seq.Invalid) as error: raise Invalid(7) from error

def replay(base_data, inputs, events):
    require(type(inputs) is list and type(events) is list and len(inputs) == len(events) <= 2, 1)
    b, _ = read_base(base_data); state = initial(b)
    for inp, data in zip(inputs, events):
        parse(data, 'SettledControlEvent')
        state, expected, _ = transition(b, state, inp)
        require(data == expected, 6)
    raw(state, 'SettledControlState')
    return state

def read_state(data, base_data, inputs, events):
    parse(data, 'SettledControlState'); state = replay(base_data, inputs, events)
    require(data == raw(state, 'SettledControlState'), 6)
    return state

def apply(base_data, inputs, events, inp, cached_state=None):
    state = replay(base_data, inputs, events)
    if cached_state is not None: read_state(cached_state, base_data, inputs, events)
    b, _ = read_base(base_data); result, event, receipt = transition(b, state, inp)
    if event is None and receipt is not None:
        index = next(i for i, item in enumerate(state['receipts']) if item['receiptId'] == receipt)
        return result, events[index], receipt
    return result, event, receipt

def rows(): return json.loads(sct.FIXTURE.read_bytes())['traces']

def test_semantic_assessment():
    for row in rows():
        b, proof = read_base(base_for(row)); assessment = assess(b)
        assert mode(assessment) == 'owner-choice', row['sourceId']+' lost native commitment/witness assessment'
        assert assessment['progress'] == proof['progress'] and len(assessment['progress']) == 1
        assert assessment['progress'][0]['receiptId'] != json.loads(json.loads(b['packetCanonicalUtf8'])['source']['committedCanonicalUtf8'])['commitmentId']
        literal = LITERALS[row['sourceId'].rsplit('.', 2)[0]]
        assert assessment['witnesses'] == proof['witnesses'] and len(assessment['witnesses']) == literal[0]
    return 32

# Independent literals: count, resulting CP, break-off, incremental DP, guards, replacements, duties.
LITERALS = {
    'ordinary': (1, 9, 2, 0, 0, 0, 0),
    'zero-retreat': (2, 7, 0, 0, 0, 0, 0),
    'refusal-loss-dp': (1, 9, 2, 0, 0, 0, 0),
    'zero-engaged': (1, 11, 4, 1, 0, 0, 0),
    'defender-capture-guard': (1, 9, 2, 0, 1, 0, 1),
    'defender-capture-escape': (1, 9, 2, 0, 0, 1, 1),
    'attacker-capture-guard-cp-limit': (2, 12, 0, 2, 1, 0, 1),
    'attacker-capture-escape': (2, 7, 0, 0, 0, 1, 1),
}
NOW = 100000

def rejected(call, code=None):
    try: call()
    except Invalid as error:
        if code is not None: assert error.code == f'CMB-SCC-{code:03}', error.code
        return
    raise AssertionError('invalid control admitted')

def trace(row, outcome='repeat', opening='available'):
    data = base_for(row); b, proof = read_base(data); state = initial(b)
    states = [raw(state, 'SettledControlState')]; inputs = []; events = []
    inp = trusted(command(b, state, 'open'), 'system', None if opening == 'missing' else NOW,
                  opening != 'unavailable')
    state, event, _ = apply(data, inputs, events, inp)
    inputs.append(inp); events.append(event); states.append(raw(state, 'SettledControlState'))
    if state['status'] == 'open':
        owner = proof['cycle']['actingSide']; deadline = None if state['timing'] is None else state['timing']['deadlineUnixMilliseconds']
        actor = owner if outcome in ('repeat', 'finish', 'regression', 'lost-owner') else 'system'
        kind = 'repeat' if outcome in ('regression', 'lost-owner') else outcome
        now = deadline if kind == 'expire' else NOW-1 if outcome == 'regression' else NOW+1
        inp = trusted(command(b, state, kind), actor, now, outcome != 'lost-owner')
        state, event, _ = apply(data, inputs, events, inp)
        inputs.append(inp); events.append(event); states.append(raw(state, 'SettledControlState'))
    return dict(sourceId=proof['sourceIdentity']['sourceId'], descriptor=proof['movementEnd']['descriptorId'],
                outcome=outcome, opening=opening, base=data, inputs=inputs, events=events, states=states)

def test_policy_probes():
    # Independent truth table only: no invented admitted no-attack/no-progress lineage.
    for progress, witnesses, expected in (([], [], 'no-continuation'), ([1], [], 'no-continuation'),
                                          ([], [1], 'no-material-progress'), ([1], [1], 'owner-choice')):
        assert mode(dict(progress=progress, witnesses=witnesses, witnessOutcome='supported')) == expected
    rejected(lambda: mode(dict(progress=[1], witnesses=[], witnessOutcome='unsupported')), 3)
    return 5

def test_resolution_literals():
    count = 0
    for row in rows():
        b, proof = read_base(base_for(row)); n, cp, breakoff, dp, guards, replacements, duties = LITERALS[row['sourceId'].rsplit('.', 2)[0]]
        packet = json.loads(b['packetCanonicalUtf8']); committed = json.loads(packet['source']['committedCanonicalUtf8'])
        progress_event = next(e.encode() for e in packet['source']['roundEventCanonicalUtf8'] if json.loads(e)['eventType'] == 'combat-attack-committed')
        assert proof['progress'] == [dict(eventType='combat-attack-committed', receiptId=json.loads(progress_event)['receiptId'], eventHash=sha(progress_event))]
        assert any(r['receiptId'] == proof['progress'][0]['receiptId'] and r['eventHash'] == sha(progress_event) for r in committed['receipts'])
        assert len(proof['witnesses']) == n
        assert all((w['afterCp'], w['breakOffCost'], w['excessCpDp'], w['terrainCost'], w['usesReleaseException']) == (cp, breakoff, dp, 2, False) for w in proof['witnesses'])
        assert [len(proof['world'][k]) for k in ('guards', 'replacementEntitlements', 'futureObligations')] == [guards, replacements, duties]
        for outcome in ('repeat', 'finish'):
            t = trace(row, outcome); first, opened, final = [json.loads(x) for x in t['states']]
            eopen, close = [json.loads(x) for x in t['events']]; c = proof['cycle']
            assert first['stateVersion'] == proof['stateVersion'] and first['prefix'] == proof['prefix']
            assert opened['status'] == 'open' and final['status'] == ('repeated' if outcome == 'repeat' else 'finished')
            assert opened['timing']['deadlineUnixMilliseconds'] == NOW+opened['timing']['decisionBudgetMilliseconds']
            assert opened['assessment']['progress'] == proof['progress'] and opened['assessment']['witnesses'] == proof['witnesses']
            assert final['assessment'] == opened['assessment'] and final['sourceProofId'] == sct.proof_id(b['proofCanonicalUtf8'].encode())
            for state in (first, opened, final):
                for field in ('world', 'randomState', 'members', 'attackHistory'): assert state[field] == proof[field], field
                assert all(m['history']['nextMovement'] is None for m in state['members'])
            assert final['stateVersion'] == proof['stateVersion']+2
            assert final['prefix'] == steps.seq.prefix_event(opened['prefix'], t['events'][-1])
            assert close['priorPrefix'] == opened['prefix'] and close['priorVersion'] == opened['stateVersion']
            assert close['author'] == c['actingSide'] and close['sourceProofId'] == final['sourceProofId']
            assert close['effect']['expiredUnits'] == [] and final['closure']['receiptId'] == close['receiptId']
            assert final['closure']['ordinal'] == c['ordinal']
            if outcome == 'repeat':
                expected = dict(c, ordinal=c['ordinal']+1, openedAuthorityVersion=opened['stateVersion']+1, openingPrefix=opened['prefix'])
                assert final['activeCycle'] == close['effect']['nextCycle'] == expected
                assert final['positionId'] == ctl.edge(c)['movementPositionId'] and final['targetUses'] == [] and final['nextCycleProgress'] == []
                assert ctl.cycle_id(expected, c['actingSide']) != ctl.cycle_id(c, c['actingSide'])
            else:
                assert final['activeCycle'] is None and final['targetUses'] == proof['targetUses']
                assert final['positionId'] == ctl.edge(c)['finishPositionId']
            unsigned = dict(close); unsigned.pop('receiptId')
            assert close['receiptId'] == 'scc.'+steps.digest(INVENTORY['domains']['receipt'], encode(unsigned))
            assert first['controlId'].startswith('sctl.') and first['controlId'] == control_id(b)
            count += 1
    return count

def descriptor_rows(): return json.loads(sct.FIXTURE.read_bytes())['movementDescriptorProbes']

def test_supported_empty_witness():
    for row in descriptor_rows():
        b, proof = read_base(base_for(row)); t = trace(row); final = json.loads(t['states'][-1]); event = json.loads(t['events'][-1])
        assert proof['witnesses'] == [] and proof['witnessOutcome'] == 'supported' and len(proof['progress']) == 1
        assert len(t['events']) == 1 and final['stateVersion'] == proof['stateVersion']+1
        assert final['status'] == 'finished' and final['decisionId'] is None and final['timing'] is None
        assert event['effect']['reason'] == 'no-continuation' and event['author'] == 'system'
        assert final['positionId'] == ctl.edge(proof['cycle'])['finishPositionId']
        assert final['world'] == proof['world'] and final['members'] == proof['members']
        rejected(lambda: apply(t['base'], t['inputs'], t['events'], trusted(command(b, final, 'repeat'), proof['cycle']['actingSide'], NOW+1)))
    return len(descriptor_rows())


def test_timing_authority():
    count = 0
    for side in ('axis', 'commonwealth'):
        row = next(r for r in rows() if r['sourceId'] == 'zero-engaged.'+side+'.attacker')
        data = base_for(row); b, proof = read_base(data); t = trace(row); opened = json.loads(t['states'][1])
        deadline = opened['timing']['deadlineUnixMilliseconds']
        for kind in ('repeat', 'finish'):
            rejected(lambda: apply(data, t['inputs'][:1], t['events'][:1], trusted(command(b, opened, kind), side, deadline)), 5); count += 1
        for kind in ('open', 'expire', 'unavailable', 'fallback-step'):
            rejected(lambda: apply(data, t['inputs'][:1], t['events'][:1], trusted(command(b, opened, kind), side, NOW+1)), 4); count += 1
        other = 'commonwealth' if side == 'axis' else 'axis'
        rejected(lambda: apply(data, t['inputs'][:1], t['events'][:1], trusted(command(b, opened, 'repeat'), other, NOW+1)), 4); count += 1
        for field, value in (('controlId', 'foreign'), ('cycleId', sha(b'foreign')), ('expectedPriorVersion', opened['stateVersion']-1), ('decisionId', 'foreign')):
            cmd = command(b, opened, 'repeat'); cmd[field] = value
            rejected(lambda: apply(data, t['inputs'][:1], t['events'][:1], trusted(cmd, side, NOW+1))); count += 1
        # Stale timer and early timer are no-ops. A rejected owner cannot renew its budget.
        for decision, now in ((opened['decisionId'], deadline-1), ('foreign', deadline)):
            cmd = command(b, opened, 'expire'); cmd['decisionId'] = decision
            state, event, receipt = apply(data, t['inputs'][:1], t['events'][:1], trusted(cmd, 'system', now))
            assert state == opened and event is None and receipt is None; count += 1
        for outcome, opening, reason in (('expire', 'available', 'deadline'), ('unavailable', 'available', 'controller-unavailable'),
            ('regression', 'available', 'clock-unavailable'), ('lost-owner', 'available', 'clock-unavailable'),
            ('fallback-step', 'missing', 'opening-clock-unavailable'), ('fallback-step', 'unavailable', 'opening-clock-unavailable')):
            other_trace = trace(row, outcome, opening); final = json.loads(other_trace['states'][-1]); event = json.loads(other_trace['events'][-1])
            assert final['status'] == 'finished' and final['world'] == proof['world'] and final['randomState'] == proof['randomState']
            assert event['author'] == 'system' and event['effect']['reason'] == reason
            if opening != 'available':
                assert final['timing'] is None and final['openingClockFailure'] and final['acceptedHighWater'] is None
            count += 1
        # Missing/regressed clocks for a timer also deterministically finish.
        for now, available in ((None, True), (NOW-1, True), (NOW+1, False)):
            state, event, _ = apply(data, t['inputs'][:1], t['events'][:1], trusted(command(b, opened, 'expire'), 'system', now, available))
            assert state['status'] == 'finished' and json.loads(event)['author'] == 'system'; count += 1
        rejected(lambda: apply(data, t['inputs'][:1], t['events'][:1], trusted(command(b, opened, 'fallback-step'), 'system', NOW+1)), 5); count += 1
        # A near-maximum UTC opening cannot fabricate deadline Timing.
        first = json.loads(t['states'][0]); maximum = 253402300799999
        state, _, _ = apply(data, [], [], trusted(command(b, first, 'open'), 'system', maximum))
        assert state['timing'] is None and state['openingClockFailure']; count += 1
    return count

def all_traces():
    traces = [trace(row, outcome) for row in rows() for outcome in ('repeat', 'finish')]
    traces += [trace(row) for row in descriptor_rows()]
    for side in ('axis', 'commonwealth'):
        row = next(r for r in rows() if r['sourceId'] == 'zero-engaged.'+side+'.attacker')
        for outcome, opening in (('expire', 'available'), ('unavailable', 'available'), ('regression', 'available'),
                                  ('lost-owner', 'available'), ('fallback-step', 'missing'), ('fallback-step', 'unavailable')):
            traces.append(trace(row, outcome, opening))
    return traces

def test_restart_retry():
    cuts = retries = 0
    for t in all_traces():
        b, _ = read_base(t['base'])
        for cut, state_bytes in enumerate(t['states']):
            state = read_state(state_bytes, t['base'], t['inputs'][:cut], t['events'][:cut]); cuts += 1
            assert raw(state, 'SettledControlState') == state_bytes
            if cut < len(t['events']):
                result, event, _ = apply(t['base'], t['inputs'][:cut], t['events'][:cut], t['inputs'][cut], state_bytes)
                assert event == t['events'][cut] and raw(result, 'SettledControlState') == t['states'][cut+1]
            for index in range(cut):
                result, event, receipt = apply(t['base'], t['inputs'][:cut], t['events'][:cut], t['inputs'][index], state_bytes)
                assert raw(result, 'SettledControlState') == state_bytes and event == t['events'][index] and receipt == json.loads(event)['receiptId']; retries += 1
                # Retry lookup is canonical command + actor; changed clock framing returns original bytes.
                changed = copy.deepcopy(t['inputs'][index]); changed.update(admittedAt=NOW+999999, clockAvailable=False)
                result, event, receipt = apply(t['base'], t['inputs'][:cut], t['events'][:cut], changed)
                assert raw(result, 'SettledControlState') == state_bytes and event == t['events'][index]; retries += 1
                foreign = copy.deepcopy(changed); foreign['actor'] = 'axis' if changed['actor'] != 'axis' else 'commonwealth'
                rejected(lambda: apply(t['base'], t['inputs'][:cut], t['events'][:cut], foreign), 4)
        final = json.loads(t['states'][-1]); cmd = command(b, final, 'expire'); cmd['decisionId'] = 'terminal.foreign'
        state, event, receipt = apply(t['base'], t['inputs'], t['events'], trusted(cmd, 'system', NOW+100000))
        assert state == final and event is None and receipt is None
        rejected(lambda: replay(t['base'], t['inputs']*3, t['events']*3), 1)
        rejected(lambda: replay(t['base'], t['inputs'], list(reversed(t['events'])))) if len(t['events']) == 2 else None
    return dict(cuts=cuts, retries=retries)

def leaves(obj, path=()):
    if type(obj) is dict:
        for key, value in obj.items(): yield from leaves(value, path+(key,))
    elif type(obj) is list:
        for i, value in enumerate(obj): yield from leaves(value, path+(i,))
    elif obj is not None: yield path, obj

def change_leaf(obj, path, value):
    target = obj
    for key in path[:-1]: target = target[key]
    target[path[-1]] = not value if type(value) is bool else value+1 if type(value) is int else sha(b'forged') if type(value) is str and value.startswith('sha256:') else 'forged'

def test_replay_forgeries():
    count = 0
    # Full leaf traversal of representative both-owner Engaged and guard/escape states/events.
    for source in ('zero-engaged.axis.attacker', 'zero-engaged.commonwealth.defender',
                   'defender-capture-guard.axis.defender', 'attacker-capture-escape.commonwealth.attacker'):
        row = next(r for r in rows() if r['sourceId'] == source); t = trace(row)
        for cut in range(1, len(t['states'])):
            original = json.loads(t['states'][cut])
            for path, value in leaves(original):
                changed = copy.deepcopy(original); change_leaf(changed, path, value)
                rejected(lambda: read_state(encode(canonical(changed, 'SettledControlState')), t['base'], t['inputs'][:cut], t['events'][:cut])); count += 1
            for path, value in leaves(json.loads(t['events'][cut-1])):
                changed = json.loads(t['events'][cut-1]); change_leaf(changed, path, value)
                rejected(lambda: replay(t['base'], t['inputs'][:cut], t['events'][:cut-1]+[encode(canonical(changed, 'SettledControlEvent'))])); count += 1
                if path != ('receiptId',):
                    unsigned = canonical(changed, 'SettledControlEvent'); unsigned.pop('receiptId')
                    changed['receiptId'] = 'scc.'+steps.digest(INVENTORY['domains']['receipt'], encode(unsigned))
                    rejected(lambda: replay(t['base'], t['inputs'][:cut], t['events'][:cut-1]+[encode(canonical(changed, 'SettledControlEvent'))])); count += 1
        # Cached projection is never a source of authority.
        changed = json.loads(t['states'][-1]); changed['world']['futureObligations'] = []
        if changed != json.loads(t['states'][-1]):
            rejected(lambda: apply(t['base'], t['inputs'], t['events'], t['inputs'][0], raw(changed, 'SettledControlState'))); count += 1
        foreign = trace(next(r for r in rows() if r['sourceId'] == ('zero-engaged.commonwealth.attacker' if '.axis.' in source else 'zero-engaged.axis.attacker')))
        rejected(lambda: replay(t['base'], foreign['inputs'], foreign['events'])); count += 1
        original = replay(t['base'], [], []); original['world']['elements'].clear(); original['members'].clear()
        assert replay(t['base'], [], [])['world']['elements'] and replay(t['base'], [], [])['members']
    return count


def base_with(b, packet=None, proof=None):
    obj = copy.deepcopy(b)
    if packet is not None: obj['packetCanonicalUtf8'] = encode(sct.canonical(packet, 'SettledPacket')).decode()
    if proof is not None: obj['proofCanonicalUtf8'] = encode(sct.canonical(proof, 'SettledContinuationProof')).decode()
    return raw(obj, 'SettledControlBase')

def test_source_admission_attacks():
    row = next(r for r in rows() if r['sourceId'] == 'zero-engaged.axis.attacker')
    b, proof = read_base(base_for(row)); packet = sct.parse(b['packetCanonicalUtf8'].encode(), 'SettledPacket'); count = 0
    # A validly typed proof cannot create trust, even with regenerated outer identity.
    for path, value in leaves(proof):
        changed = copy.deepcopy(proof); change_leaf(changed, path, value)
        rejected(lambda: read_base(base_with(b, proof=changed)), 4); count += 1
    for mutate in (
        lambda x: x['source'].update(sourceId='foreign', trustLabel='actual-campaign-history'),
        lambda x: x['source']['movementEnd'].update(receiptSource='actual-movement', descriptorId='foreign'),
        lambda x: x['source']['movementEnd']['proof'].update(ordinal=2, completionReceiptId='forged'),
        lambda x: x['source']['movementEnd']['proof']['scope'].update(gameTurn=2),
        lambda x: x['source']['movementEnd']['proof']['endLocations'].pop(),
        lambda x: x['source']['movementEnd']['proof']['excludedBefore'].append(proof['members'][0]['unit']),
        lambda x: x['releaseInputs'][0].update(actor='axis'),
        lambda x: x['releaseInputs'][0].update(clockAvailable=False),
        lambda x: x['releaseInputs'][0].update(admittedAt=1),
        lambda x: x['releaseEventCanonicalUtf8'].reverse(),
        lambda x: x['releaseEventCanonicalUtf8'].pop(),
        lambda x: x['releaseInputs'].pop(),
        lambda x: x['releaseEventCanonicalUtf8'].append(x['releaseEventCanonicalUtf8'][-1]),
        lambda x: x['source']['roundEventCanonicalUtf8'].reverse(),
        lambda x: x['source']['resultEventCanonicalUtf8'].reverse()):
        changed = copy.deepcopy(packet); mutate(changed)
        rejected(lambda: read_base(base_with(b, packet=changed)), 4); count += 1
    for field in ('requestCanonicalUtf8', 'createdCanonicalUtf8', 'baseCanonicalUtf8', 'predecessorCanonicalUtf8',
                  'roundInputsCanonicalUtf8', 'committedCanonicalUtf8', 'resultInputsCanonicalUtf8', 'resultStateCanonicalUtf8'):
        changed = copy.deepcopy(packet); changed['source'][field] = '{}'
        rejected(lambda: read_base(base_with(b, packet=changed)), 4); count += 1
    # Same World with another owner's/seal's source remains a different identity.
    other = next(r for r in rows() if r['sourceId'] == 'zero-engaged.axis.defender')
    rejected(lambda: read_base(raw(dict(b, packetCanonicalUtf8=other['packetCanonicalUtf8']), 'SettledControlBase')), 4); count += 1
    for field, value in (('profile', 'result1-settled-control'), ('progress', []), ('witnesses', []), ('witnessOutcome', 'unsupported')):
        changed = copy.deepcopy(proof); changed[field] = value
        rejected(lambda: read_base(base_with(b, proof=changed)), 4); count += 1
    # Caller booleans/proof-only/hash-only and extra base fields have no admission route.
    for changed in (dict(contractVersion=1, proofCanonicalUtf8=b['proofCanonicalUtf8']),
                    dict(contractVersion=1, packetHash=sha(b['packetCanonicalUtf8'].encode()), proofHash=sha(b['proofCanonicalUtf8'].encode())),
                    dict(b, progress=True), dict(b, repeat=True)):
        rejected(lambda: read_base(encode(changed)), 1); count += 1
    return count

def test_valid_resigned_source_attack():
    row = next(r for r in rows() if r['sourceId'] == 'zero-retreat.axis.attacker')
    b, proof = read_base(base_for(row)); packet = sct.parse(b['packetCanonicalUtf8'].encode(), 'SettledPacket')
    ctx, _ = sct.replay_source(packet['source']); res = sct.res
    state = res.initial(ctx); inputs = []; events = []
    for old in json.loads(packet['source']['resultInputsCanonicalUtf8']):
        now = None if old['admittedAt'] is None else old['admittedAt']+7
        inp = res.trusted(res.command(ctx, state, old['command']['kind'], old['command']['choice']), old['actor'], now, old['clockAvailable'])
        state, event, _ = res.transition(ctx, state, inp); inputs.append(inp); events.append(event)
    assert res.read_state(res.raw(state, 'ResultState'), ctx, inputs, events) == state
    packet['source']['resultInputsCanonicalUtf8'] = encode(inputs).decode()
    packet['source']['resultEventCanonicalUtf8'] = [e.decode() for e in events]
    packet['source']['resultStateCanonicalUtf8'] = res.raw(state, 'ResultState').decode()
    rb = sct.release_basis(ctx, state); release = rel.initial(rb); rinputs = []; revents = []
    for kind in ('open', 'complete'):
        inp = rel.trusted(rel.command(release, kind), 'system', None)
        release, event, _ = rel.transition(rb, release, inp); rinputs.append(inp); revents.append(event)
    replayed_release = rel.initial(rb)
    for inp, event in zip(rinputs, revents): replayed_release = rel.read_event(event, rb, replayed_release, inp)
    assert replayed_release == release
    packet.update(releaseBaseCanonicalUtf8=rel.raw(rb, 'ReleaseBase').decode(), releaseInputs=rinputs,
                  releaseEventCanonicalUtf8=[e.decode() for e in revents])
    proof.update(prefix=release['prefix'], releaseCompletionReceiptId=release['completionReceiptId'])
    proof['sourceIdentity'].update(sourceHash=sha(sct.raw(packet['source'], 'SettledSource')),
                                  resultHash=sha(res.raw(state, 'ResultState')), releaseBaseHash=sha(rel.raw(rb, 'ReleaseBase')),
                                  releaseStateHash=sha(rel.raw(release, 'ReleaseState')))
    # Real upstream signatures and consistent downstream Release/proof do not admit a foreign timing lineage.
    rejected(lambda: read_base(base_with(b, packet=packet, proof=proof)), 4)
    return 1

def test_canonical_capacity():
    row = rows()[0]; data = base_for(row); b, _ = read_base(data); t = trace(row); count = 0
    records = [(data, 'SettledControlBase'), (t['events'][0], 'SettledControlEvent'), (t['states'][1], 'SettledControlState')]
    for original, kind in records:
        value = json.loads(original)
        for bad in (original+b'\n', b'\xef\xbb\xbf'+original, original[:-1], b' '+original,
                    original.replace(b'{', b'{ ', 1), encode(dict(reversed(list(value.items())))),
                    original.replace(b'"contractVersion":1', b'"contractVersion":1,"contractVersion":1', 1),
                    original.replace(b'"contractVersion":1', b'"contractVersion":true', 1),
                    original.replace(b'"contractVersion":1', b'"contractVersion":1.0', 1),
                    original.replace(b'"contractVersion":1', b'"contractVersion":NaN', 1),
                    encode(dict(value, unknown=None)), b'x'*1048577):
            rejected(lambda: parse(bad, kind)); count += 1
        for key in value:
            changed = dict(value); changed.pop(key)
            rejected(lambda: parse(encode(changed), kind)); count += 1
    # Primitive, member, array and nesting capacities are checked before source replay.
    for key, value in (('contractVersion', 2), ('contractVersion', True), ('proofCanonicalUtf8', '{}'),
                       ('packetCanonicalUtf8', '{}'), ('packetCanonicalUtf8', b['packetCanonicalUtf8']+'\n')):
        changed = dict(b); changed[key] = value
        rejected(lambda: read_base(encode(changed))); count += 1
    state = json.loads(t['states'][1])
    for key, value in (('stateVersion', 2**63), ('receipts', state['receipts']*513), ('members', state['members']*33)):
        changed = copy.deepcopy(state); changed[key] = value
        rejected(lambda: read_state(encode(changed), data, t['inputs'][:1], t['events'][:1])); count += 1
    rejected(lambda: parse(b'['*33+b'0'+b']'*33, 'SettledControlState')); count += 1
    rejected(lambda: read_base(bytearray(data)), 1); count += 1
    rejected(lambda: replay(data, t['inputs'][:1], []), 1); count += 1
    # Internal capacity probes, not admitted campaign histories.
    opened = json.loads(t['states'][1]); overflow = dict(opened, stateVersion=2**63-1)
    rejected(lambda: transition(b, overflow, trusted(command(b, overflow, 'repeat'), json.loads(b['proofCanonicalUtf8'])['cycle']['actingSide'], NOW+1)), 7); count += 1
    first = json.loads(t['states'][0]); penultimate = dict(first, stateVersion=2**63-2)
    rejected(lambda: transition(b, penultimate, trusted(command(b, penultimate, 'open'), 'system', NOW)), 7); count += 1
    # A supported forced finish consumes its final available version.
    forced = base_for(descriptor_rows()[0]); fb, _ = read_base(forced); fstate = dict(initial(fb), stateVersion=2**63-2)
    result, event, _ = transition(fb, fstate, trusted(command(fb, fstate, 'open'), 'system', NOW))
    assert result['stateVersion'] == 2**63-1 and result['status'] == 'finished' and event; count += 1
    # Such internal altered authority states cannot pass the public cache/replay entry point.
    rejected(lambda: apply(data, [], [], trusted(command(b, penultimate, 'open'), 'system', NOW), raw(penultimate, 'SettledControlState'))); count += 1
    return count

TESTS = (test_semantic_assessment, test_policy_probes, test_resolution_literals, test_supported_empty_witness,
         test_timing_authority, test_restart_retry, test_replay_forgeries, test_source_admission_attacks,
         test_valid_resigned_source_attack, test_canonical_capacity)

def semantic_tests():
    results = {}
    for test in TESTS:
        results[test.__name__] = test()
        print(test.__name__, results[test.__name__], flush=True)
    return results

SOURCE_PATHS = tuple(dict.fromkeys(sct.SOURCE_PATHS + (
    'combat-settled-continuation-v1.schema.json', 'fixtures/combat-settled-continuation-v1.json',
    'verify-combat-settled-continuation-v1.py', 'combat-settled-control-v1.schema.json')))

def generated_fixture():
    retained = []
    for t in all_traces():
        b, proof = read_base(t['base'])
        retained.append(dict(sourceId=t['sourceId'], descriptor=t['descriptor'], outcome=t['outcome'], opening=t['opening'],
                             evidenceClass='admitted-synthetic-settled-context' if t['descriptor'] == 'boundary-locations' else 'pinned-synthetic-descriptor-probe',
                             baseHash=sha(t['base']), baseByteLength=len(t['base']),
                             packetHash=sha(b['packetCanonicalUtf8'].encode()), proofHash=sha(b['proofCanonicalUtf8'].encode()),
                             sourceProofId=sct.proof_id(b['proofCanonicalUtf8'].encode()), inputs=t['inputs'],
                             eventCanonicalUtf8=[e.decode() for e in t['events']], eventHashes=[sha(e) for e in t['events']],
                             stateCanonicalUtf8=[e.decode() for e in t['states']], stateHashes=[sha(e) for e in t['states']],
                             stateByteLengths=[len(e) for e in t['states']]))
    return dict(contract='combat-settled-control-v1', trustLabel=sct.TRUST,
                sourcePins={path:sha((ROOT/path).read_bytes()) for path in SOURCE_PATHS},
                sourceFixture='fixtures/combat-settled-continuation-v1.json',
                sourceAdmission='Full independent Result2 packet replay; Result1 identity is historical policy reference only.',
                policyProbes=dict(evidenceClass='independent-policy-probes-not-admitted-history',
                                  emptyWitnessProgress='no-continuation', emptyWitnessNoProgress='no-continuation',
                                  witnessNoProgress='no-material-progress', witnessProgress='owner-choice', unsupported='reject-before-open'),
                traces=retained)

def test_fixture():
    expected = (json.dumps(generated_fixture(), indent=2)+'\n').encode()
    assert FIXTURE.read_bytes() == expected, 'new frozen fixture drift'
    return len(json.loads(expected)['traces'])

def main():
    results = semantic_tests(); count = test_fixture()
    print('PASS: settled-control: '+str(len(results))+' semantic groups; '+str(count)+' traces; '+str(len(SOURCE_PATHS))+' source pins')

if __name__ == '__main__': main()
