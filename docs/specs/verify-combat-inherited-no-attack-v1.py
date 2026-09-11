#!/usr/bin/env python3
"""Actual inherited no-attack traversal evidence; no runtime admission."""
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('inherited_selection', ROOT/'verify-combat-inherited-selection-v1.py')
cis = importlib.util.module_from_spec(spec); spec.loader.exec_module(cis)
encode, sha = cis.encode, cis.sha
INVENTORY = json.loads((ROOT/'combat-inherited-no-attack-v1.schema.json').read_text())
FIXTURE = ROOT/'fixtures/combat-inherited-no-attack-v1.json'
SCHEMA = {k: [tuple(x.split(':')) for x in v.split()] for k, v in INVENTORY['objects'].items()}
TAGS = INVENTORY['effectTags']
SEQ = cis.ibc.lc.im.rd.pre.env.sequence
KINDS = {'complete-step'}


class Invalid(ValueError):
    def __init__(self, code): self.code = f'CMB-CIN-{code:03}'; super().__init__(self.code)


def require(ok, code):
    if not ok: raise Invalid(code)


def typed(value, kind, depth=0):
    require(depth <= 32, 1)
    if kind == 'Effect':
        require(type(value) is dict and type(value.get('kind')) is str and value['kind'] in TAGS, 3)
        typed(value, TAGS[value['kind']], depth)
    elif kind == 'SelectionControl':
        try: cis.typed(value, 'Control', depth)
        except cis.Invalid as error: raise Invalid(1) from error
    elif kind in SCHEMA:
        require(type(value) is dict and set(value) == {k for k, _ in SCHEMA[kind]}, 1)
        for key, child in SCHEMA[kind]: typed(value[key], child, depth+1)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value) <= 512, 1)
        for child in value: typed(child, kind[:-2], depth+1)
    else:
        try: cis.typed(value, kind, depth)
        except cis.Invalid as error: raise Invalid(1) from error


def canonical(value, kind):
    if kind == 'Effect': return canonical(value, TAGS[value['kind']])
    if kind == 'SelectionControl': return cis.canonical(value, 'Control')
    if kind in SCHEMA: return {k: canonical(value[k], child) for k, child in SCHEMA[kind]}
    if kind.endswith('[]'): return [canonical(child, kind[:-2]) for child in value]
    return cis.canonical(value, kind)


def raw(value, kind):
    typed(value, kind); data = encode(canonical(value, kind)); require(len(data) <= 1048576, 1)
    return data


def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= 1048576, 1)
    def pairs(items):
        value = {}
        for key, child in items: require(key not in value, 1); value[key] = child
        return value
    try:
        value = json.loads(data.decode('utf8'), object_pairs_hook=pairs,
            parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error: raise Invalid(1) from error
    require(raw(value, kind) == data, 8)
    return value


def digest(domain, data):
    return hashlib.sha256(domain.encode('ascii')+b'\0'+data).hexdigest()


def edge(selection):
    cycle = selection['boundary']['entry']['cycle']
    return next(x for x in SEQ.expected_catalog()['cycles'] if x['operationStage'] == cycle['operationStage']
        and x['playerPhaseSlot'] == cycle['playerPhaseSlot'])


def positions(selection):
    item = edge(selection); ids = item['combatPositionIds']+[item['releasePositionId']]
    catalog = SEQ.expected_catalog()['positions']
    return [copy.deepcopy(next(p for p in catalog if p['positionId'] == position_id)) for position_id in ids]


def validate(state):
    typed(state, 'Control'); selection = state['selection']; route = positions(selection)
    require(state['selectionHash'] == sha(cis.raw(selection, 'Control')), 4)
    require(selection['selectionClosed'] and not selection['segmentClosed'] and selection['stepIndex'] == 0
        and selection['selectionOutcome'] == 'no-selection' and selection['candidateIds'] == []
        and len(selection['receipts']) == 2 and selection['openingReceiptId'] is not None
        and selection['selectionReceiptId'] is not None, 5)
    require(0 <= state['stepIndex'] <= 6 and state['position'] == route[state['stepIndex']]
        and state['closed'] == (state['stepIndex'] == 6), 6)
    require(state['stateVersion'] == selection['stateVersion']+state['stepIndex']
        and len(state['stepReceipts']) == len(state['receipts']) == state['stepIndex'], 6)
    require(state['stepReceipts'] == [r['receiptId'] for r in state['receipts']]
        and all(r['actor'] == 'system' and r['stateVersion'] == selection['stateVersion']+index+1
            for index, r in enumerate(state['receipts'])), 6)
    require(len({r['commandHash'] for r in state['receipts']}) == len(state['receipts']), 6)
    if state['stepIndex'] == 0: require(state['prefix'] == selection['prefix'], 4)


def initial(selection):
    typed(selection, 'SelectionControl')
    state = canonical(dict(contractVersion=1, selection=copy.deepcopy(selection),
        selectionHash=sha(cis.raw(selection, 'Control')), stateVersion=selection['stateVersion'],
        prefix=selection['prefix'], position=positions(selection)[0], stepIndex=0, stepReceipts=[],
        receipts=[], closed=False), 'Control')
    validate(state); return state


def command(state):
    return dict(contractVersion=2, kind='complete-step', segmentId=state['selection']['segmentId'],
        expectedPriorVersion=state['stateVersion'], fromPositionId=state['position']['positionId'],
        dispositionReceiptId=state['selection']['selectionReceiptId'])


def trusted(cmd, actor='system'):
    return dict(command=cmd, actor=actor)


def receipt(event):
    typed(event, 'Event'); unsigned = {k: v for k, v in canonical(event, 'Event').items() if k != 'receiptId'}
    return 'cin.'+digest(INVENTORY['domains']['receipt'], encode(unsigned))


def authorize(prior, inp):
    validate(prior); typed(inp, 'Input'); cmd = inp['command']
    require(cmd['contractVersion'] == 2 and cmd['kind'] in KINDS, 3)
    require(inp['actor'] == 'system', 5)
    require(cmd['segmentId'] == prior['selection']['segmentId'], 4)
    require(cmd['dispositionReceiptId'] == prior['selection']['selectionReceiptId'], 5)


def _transition(prior, inp):
    """Pure transition over a Control reconstructed by replay; not an authority boundary."""
    authorize(prior, inp); cmd = inp['command']; ch = sha(raw(inp, 'Input'))
    duplicate = next((r for r in prior['receipts'] if r['commandHash'] == ch), None)
    if duplicate:
        require(duplicate['actor'] == inp['actor'], 5)
        return copy.deepcopy(prior), None, duplicate['receiptId']
    require(not prior['closed'] and prior['stepIndex'] < 6, 6)
    require(len(prior['receipts']) < 6 and prior['stateVersion'] < 2**63-1, 7)
    require(cmd['expectedPriorVersion'] == prior['stateVersion']
        and cmd['fromPositionId'] == prior['position']['positionId'], 6)
    state = copy.deepcopy(prior); route = positions(state['selection']); step = state['stepIndex']
    effect = dict(kind='step-completed', fromPositionId=route[step]['positionId'],
        toPositionId=route[step+1]['positionId'],
        previousStepReceiptId=state['stepReceipts'][-1] if state['stepReceipts']
            else state['selection']['openingReceiptId'],
        dispositionReceiptId=state['selection']['selectionReceiptId'], proofKind='no-attack')
    entry = state['selection']['boundary']['entry']
    event = dict(contractVersion=2, eventType='combat-step-completed', campaignId=entry['campaignId'],
        rulesetHash=entry['rulesetHash'], configurationHash=entry['configurationHash'],
        cycleId=entry['cycleId'], segmentId=state['selection']['segmentId'],
        priorVersion=prior['stateVersion'], stateVersion=prior['stateVersion']+1,
        priorPrefix=prior['prefix'], input=copy.deepcopy(inp), effect=effect, receiptId='pending')
    event['receiptId'] = receipt(event); data = raw(event, 'Event')
    state['stateVersion'] = event['stateVersion']; state['prefix'] = SEQ.prefix_event(prior['prefix'], data)
    state['position'] = route[step+1]; state['stepIndex'] += 1
    state['stepReceipts'].append(event['receiptId'])
    state['receipts'].append(dict(commandHash=ch, eventHash=sha(data), receiptId=event['receiptId'],
        actor='system', stateVersion=state['stateVersion']))
    state['closed'] = state['stepIndex'] == 6
    require(state['selection'] == prior['selection'], 5); validate(state)
    return canonical(state, 'Control'), data, event['receiptId']


def read_event(data, prior, inp):
    event = parse(data, 'Event'); require(event['receiptId'] == receipt(event), 4)
    state, expected, _ = _transition(prior, inp)
    require(expected is not None and data == expected, 6)
    return state


def replay(history, selection_events, events):
    require(type(events) is list and len(events) <= 6, 7)
    try: selected = cis.replay(*history, selection_events)
    except cis.Invalid as error: raise Invalid(5) from error
    state = initial(selected)
    for data in events:
        event = parse(data, 'Event'); state = read_event(data, state, event['input'])
    return state


def apply(history, selection_events, events, inp, cached_control=None):
    state = replay(history, selection_events, events)
    if cached_control is not None:
        parse(cached_control, 'Control')
        require(cached_control == raw(state, 'Control'), 6)
    after, data, rid = _transition(state, inp)
    if data is None and rid is not None:
        accepted = next((event for event in events if parse(event, 'Event')['receiptId'] == rid), None)
        require(accepted is not None, 6)
        return after, accepted, True
    require(data is not None, 6)
    return after, data, False


def read_control(data, history, selection_events, events):
    value = parse(data, 'Control'); require(data == raw(replay(history, selection_events, events), 'Control'), 6)
    return value


def trace(case):
    predecessor = cis.trace(case)
    history, _, _, _, selected, first, second = predecessor
    selection_events = [first, second]; before = initial(selected); state = before
    states = [before]; events = []; inputs = []
    for _ in range(6):
        inp = trusted(command(state))
        state, data, duplicate = apply(history, selection_events, events, inp, raw(state, 'Control'))
        require(not duplicate, 6); inputs.append(inp); events.append(data); states.append(state)
    route = positions(selected)
    assert [s['position'] for s in states] == route
    assert [parse(e, 'Event')['effect']['proofKind'] for e in events] == ['no-attack']*6
    assert [parse(e, 'Event')['effect']['fromPositionId'] for e in events] == [p['positionId'] for p in route[:-1]]
    assert [parse(e, 'Event')['effect']['toPositionId'] for e in events] == [p['positionId'] for p in route[1:]]
    effects = [parse(e, 'Event')['effect'] for e in events]
    assert effects[0]['previousStepReceiptId'] == selected['openingReceiptId']
    assert [e['previousStepReceiptId'] for e in effects[1:]] == states[-1]['stepReceipts'][:-1]
    assert {e['dispositionReceiptId'] for e in effects} == {selected['selectionReceiptId']}
    assert state['closed'] and state['stepIndex'] == 6 and state['selection'] == selected
    assert state['position']['positionId'] == edge(selected)['releasePositionId']
    assert state['stateVersion'] == selected['stateVersion']+6
    assert read_control(raw(state, 'Control'), history, selection_events, events) == state
    return history, selection_events, predecessor, states, inputs, events


def goldens(result):
    _, _, predecessor, states, _, events = result
    values = ([encode(cis.goldens(predecessor)), cis.raw(states[0]['selection'], 'Control')]+events+
        [raw(state, 'Control') for state in states])
    return dict(bytes=[len(value) for value in values], sha256=[sha(value) for value in values])


def rejected(call):
    try: call()
    except Invalid: return
    raise AssertionError('expected CMB-CIN rejection')


def verify(result, deep):
    history, selection_events, _, states, inputs, events = result; frozen = copy.deepcopy(result)
    counts = dict(cuts=7, retries=6, mutations=0, raw=0, boundaries=0)
    for index, state in enumerate(states):
        assert replay(history, selection_events, events[:index]) == state
        assert read_control(raw(state, 'Control'), history, selection_events, events[:index]) == state
    final = states[-1]
    for inp, data in zip(inputs, events):
        same, accepted, duplicate = apply(history, selection_events, events, inp, raw(final, 'Control'))
        assert same == final and duplicate and accepted == data
    for state, inp in zip(states[:-1], inputs):
        side = state['selection']['boundary']['assessment']['actingSide']
        for path, value in ((('actor',), side), (('command','contractVersion'), 1),
            (('command','kind'), 'open-rba'), (('command','segmentId'), 'seg.foreign'),
            (('command','expectedPriorVersion'), state['stateVersion']-1),
            (('command','fromPositionId'), 'land.position.foreign'),
            (('command','dispositionReceiptId'), state['selection']['openingReceiptId'])):
            bad = cis.ibc.lc.im.changed(inp, path, value)
            rejected(lambda b=bad,s=state: _transition(s, b)); counts['boundaries'] += 1
    for sequence in (events[1:], [events[0], events[0]], list(reversed(events)), events+[events[-1]]):
        rejected(lambda e=sequence: replay(history, selection_events, e)); counts['boundaries'] += 1
    other = cis.trace(dict(actor='commonwealth' if states[0]['selection']['boundary']['assessment']['actingSide'] == 'axis'
        else 'axis', moves=6, expectedCp=12, expectedCohesion=-2))
    rejected(lambda: replay(other[0], [other[5], other[6]], events)); counts['boundaries'] += 1
    rejected(lambda: replay(history, selection_events[:1], [])); counts['boundaries'] += 1
    positive = copy.deepcopy(states[0]['selection']); positive['candidateIds'] = ['cand.forged']
    rejected(lambda: initial(positive)); counts['boundaries'] += 1
    for path in (('prefix',), ('receipts', 0, 'eventHash')):
        forged = cis.ibc.lc.im.changed(states[1], path, 'sha256:'+'0'*64)
        rejected(lambda b=raw(forged, 'Control'): apply(history, selection_events, events[:1], inputs[1], b))
        counts['boundaries'] += 1
    post = trusted(command(final)); post['command']['expectedPriorVersion'] = final['stateVersion']
    post['command']['fromPositionId'] = final['position']['positionId']
    rejected(lambda: apply(history, selection_events, events, post, raw(final, 'Control')))
    counts['boundaries'] += 1
    if deep:
        for index, data in enumerate(events):
            value = parse(data, 'Event'); prior = states[index]
            for path, leaf in cis.ibc.lc.im.leaves(value):
                bad = cis.ibc.lc.im.changed(value, path, cis.ibc.lc.im.different(leaf))
                try:
                    if path != ('receiptId',): bad['receiptId'] = receipt(bad)
                    encoded = raw(bad, 'Event')
                except Invalid: encoded = encode(bad)
                rejected(lambda b=encoded,p=prior,i=inputs[index]: read_event(b, p, i)); counts['mutations'] += 1
        for state, length in ((states[0], 0), (states[-1], 6)):
            for path, leaf in cis.ibc.lc.im.leaves(state):
                bad = cis.ibc.lc.im.changed(state, path, cis.ibc.lc.im.different(leaf))
                try: data = raw(bad, 'Control')
                except Invalid: data = encode(bad)
                rejected(lambda b=data,n=length: read_control(b, history, selection_events, events[:n])); counts['mutations'] += 1
        for data, kind in ((raw(states[0], 'Control'), 'Control'), (events[0], 'Event'),
            (raw(states[-1], 'Control'), 'Control')):
            value = json.loads(data); first_key = next(iter(value))
            version = b'"contractVersion":1' if b'"contractVersion":1' in data else b'"contractVersion":2'
            variants = [data+b' ', b' '+data, b'\xef\xbb\xbf'+data, data.replace(b':', b': ', 1),
                b'{"unknown":0,'+data[1:], b'{'+encode(first_key)+b':null,'+data[1:],
                encode(dict(reversed(list(value.items())))), data.replace(b'contractVersion', b'contractVersio\\u006e', 1),
                data.replace(version, version+b'.0', 1), data.replace(version, b'"contractVersion":true', 1)]
            for bad in variants:
                assert bad != data; rejected(lambda b=bad,t=kind: parse(b, t)); counts['raw'] += 1
        for bad in (b'', b'x'*1048577, b'NaN', b'null', b'[]'):
            rejected(lambda b=bad: parse(b, 'Event')); counts['raw'] += 1
        bad = json.loads(events[0]); bad['effect']['kind'] = {}
        rejected(lambda: parse(encode(bad), 'Event')); counts['raw'] += 1
    assert result == frozen, 'verification mutated accepted history/state'
    return counts


PIN_PATHS = [
    'docs/specs/combat-inherited-selection-v1.md',
    'docs/specs/combat-inherited-selection-v1.schema.json',
    'docs/specs/verify-combat-inherited-selection-v1.py',
    'docs/specs/fixtures/combat-inherited-selection-v1.json',
    'docs/specs/combat-selection-steps-v1.schema.json',
    'docs/specs/verify-combat-selection-steps-v1.py',
    'docs/design/combat-step-transitions-v1.md',
]


def check_fixture(fixture):
    assert set(fixture) == {'contractVersion','cases','sourcePins'} and fixture['contractVersion'] == 1
    assert len(fixture['cases']) == 4 and {(c['actor'], c['moves']) for c in fixture['cases']} == {
        (side, moves) for side in ('axis','commonwealth') for moves in (6,7)}
    for case in fixture['cases']:
        assert set(case) == {'actor','moves','expectedCp','expectedCohesion','goldens'}
        assert case['expectedCp'] == case['moves']*2
        assert case['expectedCohesion'] == -max(0, case['expectedCp']-10)
        assert set(case['goldens']) == {'bytes','sha256'} and len(case['goldens']['bytes']) == 15
        assert len(case['goldens']['sha256']) == 15
        assert all(type(value) is int and value > 0 for value in case['goldens']['bytes'])
        assert all(type(value) is str and value.startswith('sha256:') and len(value) == 71
            for value in case['goldens']['sha256'])
    assert [pin['path'] for pin in fixture['sourcePins']] == PIN_PATHS
    for pin in fixture['sourcePins']:
        assert sha((ROOT.parent.parent/pin['path']).read_bytes()) == pin['sha256'], 'source drift: '+pin['path']


def semantic_gate(result):
    _, _, _, states, _, events = result
    assert states[0]['selection']['selectionClosed'] and not states[0]['selection']['segmentClosed']
    assert states[0]['selection']['selectionOutcome'] == 'no-selection'
    assert states[-1]['closed'] and len(events) == 6
    assert states[-1]['selection'] == states[0]['selection']


def main():
    fixture = json.loads(FIXTURE.read_text()); check_fixture(fixture)
    total = dict(cuts=0, retries=0, mutations=0, raw=0, boundaries=0); computed = []
    for case in fixture['cases']:
        result = trace(case); semantic_gate(result)
        computed.append(dict(actor=case['actor'], moves=case['moves'], goldens=goldens(result)))
        if case['goldens']: assert goldens(result) == case['goldens'], str((case['actor'],case['moves']))+' golden drift'
        for key, value in verify(result, case['actor'] == 'axis' and case['moves'] == 6).items(): total[key] += value
    if '--goldens' in sys.argv:
        print(json.dumps(computed, indent=2)); return
    assert all(case['goldens'] for case in fixture['cases']), 'literal goldens missing'
    print('PASS inherited Combat no-attack traversal: '+str(len(fixture['cases']))+' traces/'+
        str(len(fixture['cases'])*6)+' events, '+', '.join(str(v)+' '+k for k,v in total.items())+', '+
        str(len(FIXTURE.read_bytes()))+' fixture bytes, '+str(len(PIN_PATHS))+' source pins')


if __name__ == '__main__': main()
