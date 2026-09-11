#!/usr/bin/env python3
"""Actual Combat-entry empty-selection contract evidence; no runtime admission."""
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('inherited_entry', ROOT/'verify-combat-inherited-breakdown-completion-v1.py')
ibc = importlib.util.module_from_spec(spec); spec.loader.exec_module(ibc)
encode, sha = ibc.encode, ibc.sha
INVENTORY = json.loads((ROOT/'combat-inherited-selection-v1.schema.json').read_text())
FIXTURE = ROOT/'fixtures/combat-inherited-selection-v1.json'
SCHEMA = {k: [tuple(x.split(':')) for x in v.split()] for k, v in INVENTORY['objects'].items()}
TAGS = INVENTORY['effectTags']
WORLD = ibc.lc.im.world
POSITION = ibc.TERMINAL
KINDS = {'open-segment', 'close-empty-selection'}


class Invalid(ValueError):
    def __init__(self, code): self.code = f'CMB-CIS-{code:03}'; super().__init__(self.code)


def require(ok, code):
    if not ok: raise Invalid(code)


def typed(value, kind, depth=0):
    require(depth <= 32, 1)
    if kind.endswith('?'):
        if value is not None: typed(value, kind[:-1], depth)
    elif kind == 'Effect':
        require(type(value) is dict and type(value.get('kind')) is str and value['kind'] in TAGS, 3)
        typed(value, TAGS[value['kind']], depth)
    elif kind in SCHEMA:
        require(type(value) is dict and set(value) == {k for k, _ in SCHEMA[kind]}, 1)
        for key, child in SCHEMA[kind]: typed(value[key], child, depth+1)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value) <= 512, 1)
        for child in value: typed(child, kind[:-2], depth+1)
    elif kind == 'actor':
        require(value in ('axis', 'commonwealth', 'system'), 2)
    else:
        try: ibc.typed(value, kind, depth)
        except ibc.Invalid as error: raise Invalid(1) from error


def canonical(value, kind):
    if kind.endswith('?'): return None if value is None else canonical(value, kind[:-1])
    if kind == 'Effect': return canonical(value, TAGS[value['kind']])
    if kind in SCHEMA: return {k: canonical(value[k], child) for k, child in SCHEMA[kind]}
    if kind.endswith('[]'): return [canonical(child, kind[:-2]) for child in value]
    return ibc.canonical(value, kind)


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


def source_trace(side, moves):
    args = ibc.source_trace(side, moves); before = ibc.initial(*args)
    after, event = ibc._emit(before, ibc.command(before))
    return args, [event], after


def assessment(entry):
    acting = entry['cycle']['actingSide']; defending = 'commonwealth' if acting == 'axis' else 'axis'
    attacker = next(e for e in entry['world']['elements'] if WORLD.SIDES[e['elementId']] == acting)
    defender = next(e for e in entry['world']['elements'] if WORLD.SIDES[e['elementId']] == defending)
    acting_cp = copy.deepcopy(attacker['operationalState']['capabilityPointsExpended'])
    defending_cp = copy.deepcopy(defender['operationalState']['capabilityPointsExpended'])
    normal = bool(entry['operationStageWeather']) and all(w['kind'] == 'normal' for w in entry['operationStageWeather'])
    adjacent = defender['currentLocationId'] in WORLD.GRAPH[attacker['currentLocationId']]
    acting_within = acting_cp['denominator'] == 1 and acting_cp['numerator'] <= 5
    defending_within = defending_cp['denominator'] == 1 and defending_cp['numerator'] <= 7
    eligible = normal and adjacent and acting_within and defending_within
    candidate_ids = [] if not eligible else ['cand.'+digest('sandtable.combat.inherited-selection-candidate.v1',
        encode([attacker['elementId'], defender['elementId'], defender['currentLocationId']]))]
    return dict(actingSide=acting, actingElementId=attacker['elementId'], defendingSide=defending,
        defendingElementId=defender['elementId'], actingLocationId=attacker['currentLocationId'],
        defendingLocationId=defender['currentLocationId'], normalWeather=normal, adjacent=adjacent,
        actingCapabilityPointsExpended=acting_cp, defendingCapabilityPointsExpended=defending_cp,
        actingWithinVoluntaryCeiling=acting_within, defendingWithinVoluntaryCeiling=defending_within,
        candidateIds=candidate_ids)


def admit(request, created, preamble, weather, stage, reserve, moves, lifecycle, entry_events):
    require(type(entry_events) is list and len(entry_events) == 1, 6)
    try: entry = ibc.replay(request, created, preamble, weather, stage, reserve, moves, lifecycle, entry_events)
    except ibc.Invalid as error: raise Invalid(4) from error
    require(entry['sequencePosition'] == POSITION and entry['sequencePosition']['activeSide'] is None
        and entry['breakdownFlow'] == {'kind': 'idle'} and entry['interruptContext'] is None
        and entry['breakdownCompletionReceiptId'] is not None, 6)
    result = assessment(entry)
    cp = result['actingCapabilityPointsExpended']
    require(cp['denominator'] == 1 and cp['numerator'] in (12, 14), 5)
    require(result['normalWeather'] and not result['adjacent'] and not result['actingWithinVoluntaryCeiling']
        and result['defendingWithinVoluntaryCeiling'] and result['candidateIds'] == [], 5)
    return canonical(dict(contractVersion=1, entry=entry, assessment=result), 'AdmissionBoundary')


def read_boundary(data, *history):
    value = parse(data, 'AdmissionBoundary'); require(data == raw(admit(*history), 'AdmissionBoundary'), 6)
    return value


def initial(boundary):
    data = raw(boundary, 'AdmissionBoundary')
    return canonical(dict(contractVersion=1, boundary=copy.deepcopy(boundary), boundaryHash=sha(data),
        segmentId='seg.'+digest(INVENTORY['domains']['segment'], data), stateVersion=boundary['entry']['stateVersion'],
        prefix=boundary['entry']['prefix'], stepIndex=0, selectionOutcome='unopened', candidateIds=[],
        openingReceiptId=None, selectionReceiptId=None, receipts=[], selectionClosed=False,
        segmentClosed=False), 'Control')


def command(state, kind):
    require(kind in KINDS, 3)
    return dict(contractVersion=2, kind=kind, segmentId=state['segmentId'],
        expectedPriorVersion=state['stateVersion'], expectedPositionId=POSITION['positionId'],
        openingReceiptId=state['openingReceiptId'] if kind == 'close-empty-selection' else None)


def trusted(cmd, actor='system'):
    return dict(command=cmd, actor=actor)


def authorize(boundary, state, inp):
    typed(inp, 'Input'); cmd = inp['command']; kind = cmd['kind']
    require(cmd['contractVersion'] == 2 and kind in KINDS, 3)
    require(inp['actor'] == 'system', 5)
    require(cmd['segmentId'] == state['segmentId'] and state['boundary'] == boundary
        and state['boundaryHash'] == sha(raw(boundary, 'AdmissionBoundary')), 4)
    require(cmd['expectedPositionId'] == POSITION['positionId'], 4)
    require(cmd['openingReceiptId'] is None if kind == 'open-segment'
        else cmd['openingReceiptId'] == state['openingReceiptId'], 3)


def receipt(event):
    typed(event, 'Event'); unsigned = {k: v for k, v in canonical(event, 'Event').items() if k != 'receiptId'}
    return 'cis.'+digest(INVENTORY['domains']['receipt'], encode(unsigned))


def transition(boundary, prior, inp):
    authorize(boundary, prior, inp); cmd = inp['command']; ch = sha(raw(inp, 'Input'))
    duplicate = next((r for r in prior['receipts'] if r['commandHash'] == ch), None)
    if duplicate:
        require(duplicate['actor'] == inp['actor'], 5)
        return copy.deepcopy(prior), None, duplicate['receiptId']
    require(not prior['selectionClosed'] and not prior['segmentClosed'], 6)
    require(cmd['expectedPriorVersion'] == prior['stateVersion'], 6)
    require(prior['stateVersion'] < 2**63-1 and len(prior['receipts']) < 2, 7)
    state = copy.deepcopy(prior); kind = cmd['kind']
    if kind == 'open-segment':
        require(state['selectionOutcome'] == 'unopened' and not state['receipts']
            and state['openingReceiptId'] is None and state['candidateIds'] == [], 6)
        effect = dict(kind='segment-opened', boundaryHash=state['boundaryHash'],
            assessmentHash=sha(raw(boundary['assessment'], 'CandidateAssessment')),
            candidateCount=0, decisionId=None)
    else:
        require(state['selectionOutcome'] == 'system-no-selection' and len(state['receipts']) == 1
            and state['openingReceiptId'] is not None and state['selectionReceiptId'] is None
            and state['candidateIds'] == [], 6)
        effect = dict(kind='selection-closed', outcome='no-selection', candidate=None,
            openingReceiptId=state['openingReceiptId'])
    entry = boundary['entry']; event = dict(contractVersion=2, eventType='combat-'+effect['kind'],
        campaignId=entry['campaignId'], rulesetHash=entry['rulesetHash'],
        configurationHash=entry['configurationHash'], cycleId=entry['cycleId'],
        segmentId=state['segmentId'], priorVersion=prior['stateVersion'],
        stateVersion=prior['stateVersion']+1, priorPrefix=prior['prefix'], input=copy.deepcopy(inp),
        effect=effect, receiptId='pending')
    event['receiptId'] = receipt(event); data = raw(event, 'Event')
    state['stateVersion'] = event['stateVersion']
    state['prefix'] = ibc.lc.im.rd.pre.env.sequence.prefix_event(prior['prefix'], data)
    state['receipts'].append(dict(commandHash=ch, eventHash=sha(data), receiptId=event['receiptId'],
        actor='system', stateVersion=state['stateVersion']))
    if kind == 'open-segment':
        state.update(selectionOutcome='system-no-selection', openingReceiptId=event['receiptId'])
    else:
        state.update(selectionOutcome='no-selection', selectionReceiptId=event['receiptId'], selectionClosed=True)
    return canonical(state, 'Control'), data, event['receiptId']


def read_event(data, boundary, prior, inp):
    event = parse(data, 'Event'); require(event['receiptId'] == receipt(event), 4)
    state, expected, _ = transition(boundary, prior, inp)
    require(expected is not None and data == expected, 6)
    return state


def replay(request, created, preamble, weather, stage, reserve, moves, lifecycle, entry_events, events):
    require(type(events) is list and len(events) <= 2, 7)
    history = (request, created, preamble, weather, stage, reserve, moves, lifecycle, entry_events)
    boundary = admit(*history); state = initial(boundary)
    for data in events:
        event = parse(data, 'Event'); state = read_event(data, boundary, state, event['input'])
    return state


def apply(*args):
    history, events, inp = args[:-2], args[-2], args[-1]
    boundary = admit(*history); state = replay(*history, events)
    after, data, rid = transition(boundary, state, inp)
    if data is None and rid is not None:
        accepted = next(e for e in events if parse(e, 'Event')['receiptId'] == rid)
        return after, accepted, True
    require(data is not None, 6)
    return after, data, False


def read_control(data, *args):
    value = parse(data, 'Control'); require(data == raw(replay(*args), 'Control'), 6)
    return value


def trace(case):
    args, entry_events, entry = source_trace(case['actor'], case['moves'])
    history = (*args, entry_events); boundary = admit(*history); before = initial(boundary)
    opened, first, duplicate = apply(*history, [], trusted(command(before, 'open-segment')))
    require(not duplicate, 6)
    closed, second, duplicate = apply(*history, [first], trusted(command(opened, 'close-empty-selection')))
    require(not duplicate, 6)
    assert boundary['entry'] == entry and boundary['assessment']['candidateIds'] == []
    assert boundary['assessment']['actingSide'] == case['actor']
    assert boundary['assessment']['actingCapabilityPointsExpended'] == {'numerator': case['expectedCp'], 'denominator': 1}
    acting_element = next(e for e in entry['world']['elements']
        if e['elementId'] == boundary['assessment']['actingElementId'])
    assert acting_element['operationalState']['cohesionLevel'] == case['expectedCohesion']
    assert boundary['assessment']['adjacent'] is False and boundary['assessment']['normalWeather'] is True
    assert boundary['assessment']['actingWithinVoluntaryCeiling'] is False
    assert before['stateVersion'] == 15+case['moves'] and opened['stateVersion'] == 16+case['moves']
    assert closed['stateVersion'] == 17+case['moves'] and closed['stepIndex'] == 0
    assert opened['selectionOutcome'] == 'system-no-selection' and closed['selectionOutcome'] == 'no-selection'
    assert opened['candidateIds'] == closed['candidateIds'] == []
    assert closed['selectionClosed'] and not closed['segmentClosed']
    assert all(s['boundary'] == boundary for s in (before, opened, closed))
    assert [parse(e, 'Event')['effect']['kind'] for e in (first, second)] == ['segment-opened', 'selection-closed']
    assert parse(first, 'Event')['effect']['decisionId'] is None
    assert parse(second, 'Event')['effect']['openingReceiptId'] == opened['openingReceiptId']
    assert len({entry['breakdownCompletionReceiptId'], opened['openingReceiptId'], closed['selectionReceiptId']}) == 3
    assert read_boundary(raw(boundary, 'AdmissionBoundary'), *history) == boundary
    for index, state in enumerate((before, opened, closed)):
        assert read_control(raw(state, 'Control'), *history, [first, second][:index]) == state
    retry, event, duplicate = apply(*history, [first, second], trusted(command(before, 'open-segment')))
    assert duplicate and retry == closed and event == first
    retry, event, duplicate = apply(*history, [first, second], trusted(command(opened, 'close-empty-selection')))
    assert duplicate and retry == closed and event == second
    return history, boundary, before, opened, closed, first, second


def goldens(result):
    history, boundary, before, opened, closed, first, second = result
    args, entry_events = history[:-1], history[-1]
    records = [encode(args[0]), args[1]]+[r for group in args[2:] for r in group]+entry_events
    values = [encode([sha(r) for r in records]), raw(boundary, 'AdmissionBoundary'), first, second,
        raw(before, 'Control'), raw(opened, 'Control'), raw(closed, 'Control')]
    return dict(bytes=[len(v) for v in values], sha256=[sha(v) for v in values])


def rejected(call):
    try: call()
    except Invalid: return
    raise AssertionError('expected CMB-CIS rejection')


def verify(result, deep):
    history, boundary, before, opened, closed, first, second = result; frozen = copy.deepcopy(result)
    events = [first, second]; counts = dict(cuts=3, retries=2, mutations=0, raw=0, boundaries=0)
    assert replay(*history, []) == before and replay(*history, [first]) == opened
    assert replay(*history, events) == closed
    for state, kind in ((before, 'open-segment'), (opened, 'close-empty-selection')):
        inp = trusted(command(state, kind))
        for path, value in ((('actor',), boundary['assessment']['actingSide']),
            (('command','contractVersion'), 1), (('command','kind'), 'complete-step'),
            (('command','segmentId'), 'seg.foreign'), (('command','expectedPriorVersion'), state['stateVersion']-1),
            (('command','expectedPositionId'), 'land.position.foreign')):
            bad = ibc.lc.im.changed(inp, path, value); rejected(lambda b=bad: transition(boundary, state, b)); counts['boundaries'] += 1
    bad = trusted(command(before, 'open-segment')); bad['command']['openingReceiptId'] = opened['openingReceiptId']
    rejected(lambda: transition(boundary, before, bad)); counts['boundaries'] += 1
    bad = trusted(command(opened, 'close-empty-selection')); bad['command']['openingReceiptId'] = 'cis.'+'0'*64
    rejected(lambda: transition(boundary, opened, bad)); counts['boundaries'] += 1
    rejected(lambda: replay(*history, [first, first])); counts['boundaries'] += 1
    rejected(lambda: replay(*history, [second])); counts['boundaries'] += 1
    rejected(lambda: replay(*history, events+[second])); counts['boundaries'] += 1
    if deep:
        for index, data in enumerate(events):
            value = parse(data, 'Event'); prior = (before, opened)[index]
            for path, leaf in ibc.lc.im.leaves(value):
                bad = ibc.lc.im.changed(value, path, ibc.lc.im.different(leaf))
                try:
                    if path != ('receiptId',): bad['receiptId'] = receipt(bad)
                    encoded = raw(bad, 'Event')
                except Invalid: encoded = encode(bad)
                rejected(lambda b=encoded,p=prior,i=value['input']: read_event(b, boundary, p, i)); counts['mutations'] += 1
        for state, length in ((before, 0), (opened, 1), (closed, 2)):
            for path, leaf in ibc.lc.im.leaves(state):
                if path[0] not in ('boundary','boundaryHash','segmentId','stateVersion','prefix','selectionOutcome',
                    'candidateIds','openingReceiptId','selectionReceiptId','receipts','selectionClosed','segmentClosed'): continue
                bad = ibc.lc.im.changed(state, path, ibc.lc.im.different(leaf))
                try: data = raw(bad, 'Control')
                except Invalid: data = encode(bad)
                rejected(lambda b=data,n=length: read_control(b, *history, events[:n])); counts['mutations'] += 1
        for path, value in ((('assessment','adjacent'), True), (('assessment','normalWeather'), False),
            (('assessment','actingWithinVoluntaryCeiling'), True), (('assessment','candidateIds'), ['cand.forged']),
            (('entry','sequencePosition','activeSide'), boundary['assessment']['actingSide']),
            (('entry','world','elements',0,'currentLocationId'), 'assault-west')):
            bad = ibc.lc.im.changed(boundary, path, value)
            rejected(lambda b=bad: read_boundary(raw(b, 'AdmissionBoundary'), *history)); counts['mutations'] += 1
        for data, kind in ((raw(boundary, 'AdmissionBoundary'), 'AdmissionBoundary'), (first, 'Event'),
            (second, 'Event'), (raw(closed, 'Control'), 'Control')):
            value = json.loads(data); first_key = next(iter(value))
            variants = [data+b' ', b' '+data, b'\xef\xbb\xbf'+data, data.replace(b':', b': ', 1),
                b'{"unknown":0,'+data[1:], b'{'+encode(first_key)+b':null,'+data[1:],
                encode(dict(reversed(list(value.items())))), data.replace(b'contractVersion', b'contractVersio\\u006e', 1),
                data.replace(b'"contractVersion":1', b'"contractVersion":1.0', 1) if b'"contractVersion":1' in data
                    else data.replace(b'"contractVersion":2', b'"contractVersion":2.0', 1),
                data.replace(b'"contractVersion":1', b'"contractVersion":true', 1) if b'"contractVersion":1' in data
                    else data.replace(b'"contractVersion":2', b'"contractVersion":true', 1)]
            for bad in variants:
                assert bad != data; rejected(lambda b=bad,t=kind: parse(b, t)); counts['raw'] += 1
        for bad in (b'', b'x'*1048577, b'NaN', b'null', b'[]'):
            rejected(lambda b=bad: parse(b, 'Event')); counts['raw'] += 1
        bad = json.loads(first); bad['effect']['kind'] = {}
        rejected(lambda: parse(encode(bad), 'Event')); counts['raw'] += 1
        foreign_args, foreign_entry, _ = source_trace('commonwealth', 7)
        rejected(lambda: replay(*foreign_args, foreign_entry, events)); counts['boundaries'] += 1
        short = list(copy.deepcopy(history)); short[-1] = []
        rejected(lambda: replay(*short, events)); counts['boundaries'] += 1
        for field, value in (('stateVersion', 2**63-1), ('receipts', before['receipts']*2+[dict(
            commandHash='sha256:'+'0'*64, eventHash='sha256:'+'1'*64, receiptId='cis.'+'2'*64,
            actor='system', stateVersion=before['stateVersion'])]*2)):
            bad = copy.deepcopy(before); bad[field] = value
            rejected(lambda s=bad: transition(boundary, s, trusted(command(s, 'open-segment')))); counts['boundaries'] += 1
    assert result == frozen, 'verification mutated accepted history/state'
    return counts


PIN_PATHS = [
    'docs/specs/combat-inherited-breakdown-completion-v1.schema.json',
    'docs/specs/verify-combat-inherited-breakdown-completion-v1.py',
    'docs/specs/fixtures/combat-inherited-breakdown-completion-v1.json',
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
    assert [pin['path'] for pin in fixture['sourcePins']] == PIN_PATHS
    for pin in fixture['sourcePins']:
        assert sha((ROOT.parent.parent/pin['path']).read_bytes()) == pin['sha256'], 'source drift: '+pin['path']


def semantic_red():
    args, entry_events, entry = source_trace('axis', 6); boundary = admit(*args, entry_events)
    before = initial(boundary); opened, first, _ = transition(boundary, before, trusted(command(before, 'open-segment')))
    closed, second, _ = transition(boundary, opened, trusted(command(opened, 'close-empty-selection')))
    assert boundary['entry'] == entry and boundary['assessment']['candidateIds'] == []
    assert (parse(first, 'Event')['effect']['candidateCount'], parse(first, 'Event')['effect']['decisionId']) == (0, None)
    assert parse(second, 'Event')['effect']['outcome'] == 'no-selection'
    assert closed['selectionClosed'] and not closed['segmentClosed'] and closed['stepIndex'] == 0
    assert closed['boundary']['entry']['world'] == entry['world'] and closed['boundary']['entry']['prefix'] == entry['prefix']


def main():
    fixture = json.loads(FIXTURE.read_text()); check_fixture(fixture); semantic_red()
    total = dict(cuts=0, retries=0, mutations=0, raw=0, boundaries=0)
    computed = []
    for case in fixture['cases']:
        result = trace(case); computed.append(dict(actor=case['actor'], moves=case['moves'], goldens=goldens(result)))
        if case['goldens']:
            assert goldens(result) == case['goldens'], str((case['actor'],case['moves']))+' golden drift'
        for key, value in verify(result, case['actor'] == 'axis' and case['moves'] == 6).items(): total[key] += value
    if '--goldens' in sys.argv:
        print(json.dumps(computed, indent=2)); return
    assert all(case['goldens'] for case in fixture['cases']), 'literal goldens missing'
    print('PASS inherited Combat empty selection: '+str(len(fixture['cases']))+' traces/'+
        str(len(fixture['cases'])*2)+' events, '+', '.join(str(v)+' '+k for k,v in total.items())+', '+
        str(len(fixture['sourcePins']))+' source pins')


if __name__ == '__main__': main()
