#!/usr/bin/env python3
"""A1 / disjoint A2 side projection oracle. Synthetic C3 lineage; no runtime activation."""
from __future__ import annotations
import copy
import hashlib
from functools import lru_cache
import importlib.util
import json
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
FIXTURE = ROOT / 'fixtures/combat-side-projection-v1.json'
INVENTORY = json.loads((ROOT / 'combat-side-projection-v1.schema.json').read_text())
SCHEMA = {k: [tuple(x.split(':')) for x in v.split()] for k, v in INVENTORY['objects'].items()}
spec = importlib.util.spec_from_file_location('side_round', ROOT / 'verify-combat-sealed-round-v1.py')
rnd = importlib.util.module_from_spec(spec)
spec.loader.exec_module(rnd)
steps = rnd.steps
spec2 = importlib.util.spec_from_file_location('side_round_v2', ROOT / 'verify-combat-sealed-round-v2.py')
rnd2 = importlib.util.module_from_spec(spec2)
spec2.loader.exec_module(rnd2)
SIDES = ('axis', 'commonwealth')

def encode(value):
    return json.dumps(value, ensure_ascii=True, separators=(',', ':')).encode('ascii')

class Invalid(ValueError):
    pass

def require(ok):
    if not ok:
        raise Invalid('CMB-SIDE-REJECTED')

def typed(value, kind, depth=0):
    require(depth <= INVENTORY['limits']['depth'])
    if kind.endswith('?'):
        if value is not None:
            typed(value, kind[:-1], depth)
    elif kind == 'Candidate':
        require(type(value) is dict and type(value.get('kind')) is str
                and value['kind'] in INVENTORY['candidateTags'])
        typed(value, INVENTORY['candidateTags'][value['kind']], depth)
    elif kind in SCHEMA:
        require(type(value) is dict and set(value) == {k for k, _ in SCHEMA[kind]})
        for key, child in SCHEMA[kind]:
            typed(value[key], child, depth + 1)
        if kind == 'Observation':
            require(len(value['ownReceipts']) <= 3 and len(value['history']) <= 32)
        if kind == 'Decision':
            require(1 <= len(value['actions']) <= 2)
        if kind == 'Outcome':
            require((value['receipt'] is not None) == (value['status'] == 'accepted'))
    elif kind.endswith('[]'):
        require(type(value) is list and len(value) <= INVENTORY['limits']['arrayItems'])
        for child in value:
            typed(child, kind[:-2], depth + 1)
    elif kind in INVENTORY['enums']:
        require(type(value) is str and value in INVENTORY['enums'][kind])
    elif kind in INVENTORY['integerBounds']:
        lower, upper = INVENTORY['integerBounds'][kind]
        require(type(value) is int and lower <= value <= upper)
    elif kind == 'ref':
        require(type(value) is str and len(value) == 68 and value.startswith('pub.')
                and all(c in '0123456789abcdef' for c in value[4:]))
    elif kind == 'cycleHash':
        require(type(value) is str and value.startswith('sha256:') and len(value)==71
                and all(c in '0123456789abcdef' for c in value[7:]))
    elif kind == 'id':
        require(type(value) is str and 0 < len(value) <= INVENTORY['limits']['idLength']
                and value.isascii() and all(32 <= ord(c) < 127 for c in value))
    else:
        raise Invalid('CMB-SIDE-REJECTED')


def canonical(value, kind):
    if kind.endswith('?'):
        return None if value is None else canonical(value, kind[:-1])
    if kind == 'Candidate':
        return canonical(value, INVENTORY['candidateTags'][value['kind']])
    if kind in SCHEMA:
        return {key: canonical(value[key], child) for key, child in SCHEMA[kind]}
    if kind.endswith('[]'):
        return [canonical(child, kind[:-2]) for child in value]
    return value


def raw(value, kind):
    typed(value, kind)
    data = encode(canonical(value, kind))
    require(len(data) <= INVENTORY['limits']['bytes'])
    return data


def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= INVENTORY['limits']['bytes'])
    def pairs(items):
        result = {}
        for key, value in items:
            require(key not in result)
            result[key] = value
        return result
    try:
        value = json.loads(data.decode('ascii'), object_pairs_hook=pairs,
                           parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error:
        raise Invalid('CMB-SIDE-REJECTED') from error
    require(raw(value, kind) == data)
    return value


def public_ref(domain, value, kind=None):
    payload = raw(value, kind) if kind else encode(value)
    return 'pub.' + hashlib.sha256(INVENTORY['domains'][domain].encode('ascii') + b'\0' + payload).hexdigest()


def is_steps(source):
    return source['family'] in ('steps','steps-clock-v2')


def is_round(source):
    return source['family'] in ('round','round-clock-v2')


def corrected(source):
    # Registry membership is authenticated by replay_states before any authority transition.
    return source['family'] in ('steps-clock-v2','round-clock-v2')


def round_reader(source):
    return rnd2 if source['family']=='round-clock-v2' else rnd


def predecessor_source(source):
    side = source['base']['boundary']['cycle']['actingSide']
    name = 'clock-v2.'+side+'.accepted-decline' if corrected(source) else 'accepted-decline'
    return next(x for x in source_catalog() if x['name']==name)


def validate_source(source):
    # Named retained transcripts authenticate this contract experiment. Production must use
    # independently authenticated Chronicle/input history, never caller-asserted actor strings.
    require(type(source) is dict and set(source) == {'name','family','base','inputs','events'})
    expected = next((x for x in source_catalog() if x['name'] == source['name']), None)
    require(expected is not None and source['family'] == expected['family']
            and encode(source['base']) == encode(expected['base']) and type(source['events']) is list
            and type(source['inputs']) is list and len(source['events']) == len(source['inputs']))
    count = len(source['events'])
    require(all(type(event) is bytes for event in source['events']) and source['events'] == expected['events'][:count]
            and encode(source['inputs']) == encode(expected['inputs'][:count]) and count <= len(expected['events']))
    return source['name'],count


def replay_states(source):
    name,count = validate_source(source)
    return copy.deepcopy(cached_states(name,count))


@lru_cache(maxsize=512)
def cached_states(name,count):
    source = prefix(next(x for x in source_catalog() if x['name']==name),count)
    base = source['base']
    if is_steps(source):
        steps.boundary(steps.encode(base))
        state = steps.initial(base)
        reader = steps.read_event
    else:
        codec = round_reader(source)
        if corrected(source):
            predecessor = predecessor_source(source)
            evidence = dict(boundary=predecessor['base'],inputs=predecessor['inputs'],
                            events=[dict(canonicalUtf8=e.decode()) for e in predecessor['events']])
        else:
            _, evidence = rnd.start()
        codec.read_base(codec.encode(codec.canonical(base,'Base')),evidence)
        state = codec.initial(base)
        reader = codec.read_event
    result = [state]
    for inp, event in zip(source['inputs'], source['events']):
        state = reader(event, base, state, inp)
        result.append(state)
    return result


def audience_facts(source, state, audience):
    """Internal declassifier; source/state already authenticated by replay_states."""
    typed(audience, 'side')
    base = source['base'] if is_steps(source) else source['base']['boundary']
    cycle = base['cycle']
    # Approved public policy/version identifiers and the published finite decision budget only.
    context = dict(campaignId=cycle['campaignId'], audience=audience,
                   cycle=dict(gameTurn=cycle['gameTurn'], operationStage=cycle['operationStage'],
                              playerPhaseSlot=cycle['playerPhaseSlot'], phasingSide=cycle['actingSide'],
                              ordinal=cycle['ordinal']),
                   rulesRef=public_ref('rules', dict(rulesetHash=cycle['rulesetHash'], profile='singleton-infantry-close-assault',
                       policy='CMB-POL-006', candidateCodec=1)),
                   configRef=public_ref('config', dict(selectionBudgetMilliseconds=30000,
                       rbaBudgetMilliseconds=30000, assignmentBudgetMilliseconds=30000)))
    if corrected(source):
        context['configRef'] = public_ref('config',dict(selectionBudgetMilliseconds=30000,
            rbaBudgetMilliseconds=30000,assignmentBudgetMilliseconds=30000,
            assignmentClockPolicyId=rnd2.POLICY),'ClockConfigSeed')
    world = state.get('world', base['world'])
    own_element = next(x for x in world['elements'] if x['elementId'] == audience+'-assault-battalion')
    enemy = next(x for x in world['elements'] if x is not own_element)
    unit_seed = dict(campaignId=cycle['campaignId'], audience=audience,
                     elementId=own_element['elementId'], componentId=None)
    own = dict(participantRef=public_ref('participant', unit_seed, 'UnitSeed'),
               componentRef=public_ref('component', unit_seed | dict(componentId=own_element['components'][0]['componentId']), 'UnitSeed'),
               locationId=own_element['currentLocationId'], currentToe=own_element['components'][0]['currentToe'],
               spentCp=own_element['operationalState']['capabilityPointsExpended']['numerator'],
               ammunition=own_element['ammunition']['points'], cohesion=own_element['operationalState']['cohesionLevel'])
    apparent = dict(targetRef=public_ref('target', dict(context=context, locationId=enemy['currentLocationId'])),
                    locationId=enemy['currentLocationId'])
    position = steps.edge(base)['combatPositionIds'][state['stepIndex']] if state['stepIndex'] < 6 else steps.edge(base)['releasePositionId']
    status, decision_kind, deadline, candidates = 'waiting', None, None, []
    round_open = is_round(source) and state['status'] != 'unopened'
    if is_steps(source):
        window = steps.active_window(state)
        if window is not None and window['owner'] == audience:
            decision_kind = 'selection' if state['selectionOutcome'] == 'pending' else 'rba'
            deadline = window['timing']['deadlineUnixMilliseconds']
            if decision_kind == 'selection':
                candidates = [dict(kind='select-close-assault', participantRef=own['participantRef'],
                                   targetRef=apparent['targetRef'], targetLocationId=apparent['locationId']),
                              dict(kind='finish-without-attack')]
            else:
                candidates = [dict(kind='decline-retreat-before-assault', participantRef=own['participantRef'])]
        if state['closed'] or state['selectionOutcome'] == 'cancelled':
            status = 'closed'
    else:
        slot = next((x for x in state['slots'] if x['owner'] == audience), None)
        if state['status'] in ('collecting','prepared') and state['stepIndex'] == 3:
            if slot['sealedReceiptId'] is None:
                decision_kind = 'force-assignment'
                deadline = state['timing']['deadlineUnixMilliseconds']
                candidates = [dict(kind='full-close-assault', participantRef=own['participantRef'],
                                   componentRef=own['componentRef'], committedToe=10)]
            else:
                status = 'own-choice-sealed'
        if state['status'] == 'cancelled' or state['closed']:
            status = 'closed'
    if decision_kind:
        status = 'choice-required'
    return dict(context=context, positionId=position, own=own, apparentEnemy=apparent,
                status=status, decisionKind=decision_kind, deadline=deadline, candidates=candidates,
                roundOpen=round_open)


def views(source, audience):
    typed(audience,'side')
    name,count = validate_source(source)
    return copy.deepcopy(cached_views(name,count,audience))


@lru_cache(maxsize=1024)
def cached_views(name,count,audience):
    source = prefix(next(x for x in source_catalog() if x['name']==name),count)
    states = replay_states(source)
    result, history, receipts = [], [], []
    revision, previous_facts, round_ref = 0, None, None
    if is_round(source):
        predecessor = predecessor_source(source)
        inherited = views(predecessor,audience)[-1]
        history, receipts = copy.deepcopy(inherited['history']), copy.deepcopy(inherited['ownReceipts'])
        revision = inherited['visibleRevision']
        previous_facts = audience_facts(source,states[0],audience)
    for index, state in enumerate(states):
        facts = audience_facts(source, state, audience)
        own_receipt = None
        if index:
            inp = source['inputs'][index-1]
            if inp['actor'] == audience and inp['command']['kind'] in ('choose-selection','decline-rba','seal-choice') and (inp['command']['kind']!='seal-choice' or json.loads(source['events'][index-1])['effect']['kind']=='choice-sealed'):
                previous = result[-1]['decision']
                chosen = inp['command'].get('choice') or ('full-close-assault' if is_round(source) else 'decline-retreat-before-assault')
                action = next(x for x in previous['actions'] if x['candidate']['kind'] == chosen)
                seed = dict(decisionId=previous['decisionId'], actionId=action['actionId'])
                own_receipt = seed | dict(receiptRef=public_ref('receipt', seed, 'ReceiptSeed'))
                receipts.append(own_receipt)
        changed = facts != previous_facts or own_receipt is not None
        if index and changed:
            require(revision < 2**63-1)
            revision += 1
        context = facts['context']
        cycle = context['cycle']
        public_cycle = dict(contractVersion=1, campaignId=context['campaignId'], rulesetHash=(source['base'] if is_steps(source) else source['base']['boundary'])['cycle']['rulesetHash'],
                            gameTurn=cycle['gameTurn'], operationStage=cycle['operationStage'],
                            playerPhaseSlot=cycle['playerPhaseSlot'], actingSide=cycle['phasingSide'], ordinal=cycle['ordinal'])
        boundary = source['base'] if is_steps(source) else source['base']['boundary']
        cycle_ref = 'sha256:'+hashlib.sha256(steps.seq.identity(public_cycle,'Public',boundary['firstActingSide'])).hexdigest()
        seed = dict(context=facts['context'], cycleRef=cycle_ref, positionId=facts['positionId'],
                    openingRevision=revision, kind=facts['decisionKind'] or 'force-assignment',
                    participantRef=facts['own']['participantRef'], targetRef=facts['apparentEnemy']['targetRef'])
        if facts['roundOpen'] and round_ref is None:
            round_ref = public_ref('round', seed | dict(kind='force-assignment'), 'IdentitySeed')
        decision = None
        if facts['decisionKind']:
            if result and result[-1]['decision'] is not None and facts == previous_facts:
                decision = result[-1]['decision']
            else:
                decision_id = public_ref('decision', seed, 'IdentitySeed')
                role = 'attacker' if audience == facts['context']['cycle']['phasingSide'] else 'defender'
                slot_ref = public_ref('slot', dict(roundRef=round_ref, audience=audience, role=role), 'SlotSeed') if round_ref else None
                actions = [dict(actionId=public_ref('action', dict(decisionId=decision_id,candidate=c), 'ActionSeed'),candidate=c) for c in facts['candidates']]
                set_seed = dict(decisionId=decision_id, openingRevision=revision, actions=actions)
                decision = dict(decisionId=decision_id, kind=facts['decisionKind'], openingRevision=revision,
                                slotRef=slot_ref, deadlineUnixMilliseconds=facts['deadline'],
                                actionSetId=public_ref('set', set_seed, 'SetSeed'), actions=actions)
        if changed:
            history.append(dict(visibleRevision=revision, positionId=facts['positionId'], status=facts['status'],
                                ownReceiptRef=own_receipt['receiptRef'] if own_receipt else None))
        view = dict(contractVersion=1, context=facts['context'], cycleRef=cycle_ref,
                    positionId=facts['positionId'], visibleRevision=revision, own=facts['own'],
                    apparentEnemy=facts['apparentEnemy'], roundRef=round_ref, status=facts['status'],
                    decision=decision, ownReceipts=copy.deepcopy(receipts), history=copy.deepcopy(history))
        require(len(history) <= 32 and len(receipts) <= 3)
        raw(view, 'Observation')
        result.append(view)
        previous_facts = facts
    return result


def read_observation(data, source, audience):
    value = parse(data, 'Observation')
    require(value['context']['audience'] == audience and data == raw(views(source, audience)[-1], 'Observation'))
    return value


def submission(view, action_index=0):
    decision = view['decision']
    action = decision['actions'][action_index]
    return dict(contractVersion=1, campaignId=view['context']['campaignId'], rulesRef=view['context']['rulesRef'],
                configRef=view['context']['configRef'], audience=view['context']['audience'], roundRef=view['roundRef'],
                decisionId=decision['decisionId'], slotRef=decision['slotRef'], openingRevision=decision['openingRevision'], actionSetId=decision['actionSetId'],
                actionId=action['actionId'], candidate=action['candidate'])


def submit(source, audience, data, now=5000, available=True):
    rejected_outcome = dict(contractVersion=1, status='rejected', receipt=None)
    try:
        require(type(available) is bool and (now is None or type(now) is int and 0<=now<=253402300799999))
        typed(audience, 'side')  # Authenticated seat precedes attacker-controlled reference lookup.
        proposal = parse(data, 'Submission')
        require(proposal['audience'] == audience)
        projected = views(source, audience)
        # Exact historical action equality supports receipt recovery; never rebind a consumed ID.
        offered_frames = projected
        if is_round(source):
            predecessor = predecessor_source(source)
            offered_frames = views(predecessor,audience) + projected[1:]
        offered = next((v for v in reversed(offered_frames) if v['decision'] is not None
                        and v['decision']['decisionId'] == proposal['decisionId']), None)
        require(offered is not None)
        index = next((i for i, action in enumerate(offered['decision']['actions'])
                      if action['actionId'] == proposal['actionId']), None)
        require(index is not None and data == raw(submission(offered, index), 'Submission'))
        prior_receipt = next((r for r in projected[-1]['ownReceipts'] if r['decisionId'] == proposal['decisionId']), None)
        if prior_receipt:
            require(prior_receipt['actionId'] == proposal['actionId'])
            return dict(contractVersion=1, status='accepted', receipt=prior_receipt)
        require(projected[-1]['decision'] == offered['decision'])
        states = replay_states(source)
        state, base, kind = states[-1], source['base'], proposal['candidate']['kind']
        if is_round(source):
            role = 'attacker' if audience == base['boundary']['cycle']['actingSide'] else 'defender'
            codec = round_reader(source)
            command = codec.command(base,state,'seal-choice',role)
            inp = codec.trusted(command,audience,now,available)
            _, event, _ = codec.transition(base,state,inp)
        else:
            if kind in ('select-close-assault','finish-without-attack'):
                command = steps.command(state, 'choose-selection', decisionId=state['selectionWindow']['decisionId'],
                                        choice=kind, candidate=steps.candidate(base) if kind=='select-close-assault' else None)
            else:
                command = steps.command(state, 'decline-rba', decisionId=state['rbaWindow']['decisionId'],
                                        participant=state['selection']['defender']['unit'])
            inp = steps.trusted(command,audience,now,available)
            _, event, _ = steps.transition(base, state, inp)
        expected_effect = 'choice-sealed' if is_round(source) else ('rba-declined' if kind=='decline-retreat-before-assault' else 'selection-closed')
        require(event is not None and json.loads(event)['effect']['kind'] == expected_effect)
        seed = dict(decisionId=proposal['decisionId'], actionId=proposal['actionId'])
        return dict(contractVersion=1, status='accepted', receipt=seed | dict(receiptRef=public_ref('receipt', seed, 'ReceiptSeed')))
    except (Invalid, steps.Invalid, rnd.Invalid, StopIteration, KeyError, TypeError, ValueError):
        return rejected_outcome


def mirror_steps(source, side):
    boundary = copy.deepcopy(source['base'])
    boundary['cycle']['actingSide'] = side
    boundary['firstActingSide'] = side
    boundary['position']['activeSide'] = side
    if side!='axis':
        elements = boundary['world']['elements']
        spent = [e['operationalState']['capabilityPointsExpended']['numerator'] for e in elements]
        for element,value in zip(elements,reversed(spent)):
            element['operationalState']['capabilityPointsExpended']['numerator'] = value
    state = steps.initial(steps.boundary(encode(boundary)))
    inputs,events = [],[]
    for original in source['inputs']:
        inp = copy.deepcopy(original)
        cmd = inp['command']
        cmd['segmentId'] = state['segmentId']
        if cmd['expectedPriorVersion'] is not None:
            cmd['expectedPriorVersion'] = state['stateVersion']
        if cmd['decisionId'] is not None:
            cmd['decisionId'] = state['segmentId']+'.'+cmd['decisionId'].rsplit('.',1)[1]
        if cmd['kind']=='choose-selection':
            inp['actor'] = side
            if cmd['candidate'] is not None:
                cmd['candidate'] = steps.candidate(boundary)
        if cmd['kind']=='decline-rba':
            cmd['participant'] = steps.candidate(boundary)['defender']['unit']
            inp['actor'] = cmd['participant']['originalSide']
        state,event,_ = steps.transition(boundary,state,inp)
        inputs.append(inp); events.append(event)
    return dict(name='clock-v2.'+side+'.'+source['name'],family='steps-clock-v2',base=boundary,inputs=inputs,events=events)


def corrected_sources(legacy_sources):
    result = [mirror_steps(source,side) for side in SIDES for source in legacy_sources if is_steps(source)]
    rnd2.verify_fixture(rnd2.FIXTURE.read_bytes())
    for case in json.loads(rnd2.FIXTURE.read_bytes())['cases']:
        result.append(dict(name='clock-v2.'+case['name'],family='round-clock-v2',
            base=json.loads(case['baseCanonicalUtf8']),inputs=case['inputs'],
            events=[e.encode() for e in case['eventCanonicalUtf8']]))
    return result


@lru_cache(maxsize=1)
def source_catalog():
    fixture = json.loads(steps.FIXTURE.read_text())
    result = []
    for item in fixture['cases']:
        case = copy.deepcopy(item)
        case['boundary'] = json.loads(fixture['boundaries'][case['boundaryIndex']]['canonicalUtf8'])
        result.append(dict(name=case['name'], family='steps', base=case['boundary'],
                           inputs=case['inputs'], events=[x['canonicalUtf8'].encode() for x in case['events']]))
    base, _ = rnd.start()
    for case in json.loads(rnd.FIXTURE.read_text())['cases']:
        final, inputs, events = rnd.trace_case(base, case)
        require(encode(inputs) == encode(case['inputs']) and events == [x['canonicalUtf8'].encode() for x in case['events']]
                and rnd.encode(rnd.canonical(final, 'RoundState')) == case['stateGolden']['canonicalUtf8'].encode())
        result.append(dict(name=case['name'], family='round', base=base, inputs=inputs, events=events))
    result.extend(corrected_sources(result))
    return result

def source_cases():
    return copy.deepcopy(source_catalog())

def rejected(call):
    try:
        call()
    except Invalid:
        return
    raise AssertionError('invalid side value admitted')

def test_codec():
    good = dict(kind='full-close-assault', participantRef='pub.'+'0'*64,
                componentRef='pub.'+'1'*64, committedToe=10)
    assert parse(raw(good, 'Candidate'), 'Candidate') == good
    for bad in (good | {'committedToe':True}, good | {'committedToe':9}, good | {'private':0}):
        rejected(lambda bad=bad: raw(bad, 'Candidate'))
    data = raw(good, 'Candidate')
    for bad in (b' '+data, data+b'\n', data.replace(b':10', b':1e1'),
                data.replace(b'"kind":', b'"kind":"full-close-assault","kind":')):
        rejected(lambda bad=bad: parse(bad, 'Candidate'))

def test_selection():
    source = next(x for x in source_cases() if x['name']=='accepted-decline')
    owner = views(source, 'axis')
    defender = views(source, 'commonwealth')
    assert len(owner)==8, 'selection needs every authenticated cut'
    assert [a['candidate']['kind'] for a in owner[1]['decision']['actions']]==['select-close-assault','finish-without-attack']
    assert defender[1]['decision'] is None
    assert defender[5]['decision']['actions'][0]['candidate']['kind']=='decline-retreat-before-assault'
    assert owner[5]['decision'] is None
    assert owner[1]['visibleRevision'] < owner[2]['visibleRevision']
    assert defender[1] == defender[2], 'private selection must not number opponent history'

def test_seals():
    source = next(x for x in source_cases() if x['name']=='attacker-first')
    a, d = views(source, 'axis'), views(source, 'commonwealth')
    assert len(a)==7, 'round needs every authenticated cut'
    assert a[1]['status']==d[1]['status']=='choice-required'
    assert a[2]['status']=='own-choice-sealed'
    assert d[1]==d[2], 'opponent seal changed action/revision/bytes'
    assert a[2]==a[3], 'second seal disclosed preparation'
    assert a[2]['ownReceipts'][0]['receiptRef'].startswith('pub.')

def test_submission():
    result = submit(source_cases()[0], 'spectator', b'{}')
    assert result == dict(contractVersion=1,status='rejected',receipt=None), 'foreign audience admitted'

def test_tight_bounds():
    source = next(x for x in source_cases() if x['name']=='attacker-first')
    view = views(source, 'axis')[1]
    too_many = copy.deepcopy(view)
    too_many['decision']['actions'] *= 3
    rejected(lambda: raw(too_many, 'Observation'))
    rejected(lambda: raw(dict(contractVersion=1, status='accepted', receipt=None), 'Outcome'))


def source_pins():
    paths = []
    for stem in ('combat-selection-steps-v1','combat-sealed-round-v1','combat-authority-composition-v1','combat-cycle-sequence-v1','combat-sealed-round-v2'):
        paths.extend([f'docs/specs/{stem}.schema.json', f'docs/specs/fixtures/{stem}.json', f'docs/specs/verify-{stem}.py'])
    paths.append('docs/specs/combat-authority-composition-v1.md')
    for stem in ('combat-opportunity-identity-v1','combat-sealed-decision-protocol-v1',
                 'combat-step-transitions-v1','combat-cost-resolution-order-v1',
                 'combat-settlement-disclosure-v1','continual-cycle-reserve-composition-v1',
                 'combat-cycle-policy-reconciliation'):
        paths.append(f'docs/design/{stem}.md')
    return [dict(path=path, sha256='sha256:'+hashlib.sha256((ROOT.parent.parent/path).read_bytes()).hexdigest()) for path in paths]


def test_source_pins():
    assert len(source_pins()) == 23, 'predecessor/design/handoff sources unpinned'


def prefix(source, length):
    return source | dict(inputs=source['inputs'][:length], events=source['events'][:length])


def changed(value, path, replacement):
    result = copy.deepcopy(value)
    target = result
    for key in path[:-1]:
        target = target[key]
    target[path[-1]] = replacement
    return result


def test_privacy_and_binding():
    catalog = {s['name']: s for s in source_cases()}
    for name, observer in (('attacker-first','commonwealth'),('defender-first','axis')):
        source = catalog[name]
        before, after = prefix(source, 1), prefix(source, 2)
        first, second = views(before, observer)[-1], views(after, observer)[-1]
        assert raw(first, 'Observation') == raw(second, 'Observation')
        data = raw(submission(first), 'Submission')
        assert submit(before, observer, data) == submit(after, observer, data)
        # Hidden mutations below are declassifier-only probes, never admitted authority histories.
        state = replay_states(after)[-1]
        original = audience_facts(after, state, observer)
        hidden = 'axis' if observer == 'commonwealth' else 'commonwealth'
        enemy_index = next(i for i,x in enumerate(state['world']['elements']) if x['elementId'].startswith(hidden))
        probes = [(('prefix',), 'sha256:'+'f'*64), (('stateVersion',), state['stateVersion']+10),
                  (('randomState','seed'), 999), (('randomState','nextByteCursor'), 50),
                  (('world','elements',enemy_index,'elementId'), 'hidden-enemy-renamed'),
                  (('world','elements',enemy_index,'ammunition','points'), 0),
                  (('world','elements',enemy_index,'components',0,'currentToe'), 3),
                  (('world','elements',enemy_index,'operationalState','cohesionLevel'), 4),
                  (('world','elements',enemy_index,'operationalState','capabilityPointsExpended','numerator'), 8)]
        for path, replacement in probes:
            assert audience_facts(after, changed(state,path,replacement), observer) == original
        for path, replacement in ((('base','boundary','priorPrefix'),'sha256:'+'e'*64),
                                  (('base','boundary','world','elements',enemy_index,'ammunition','points'),0)):
            forged = changed(after,path,replacement)
            assert submit(forged, observer, data)['status'] == 'rejected'
            rejected(lambda forged=forged: views(forged, observer))
    for audience in SIDES:
        attacker_first = views(catalog['attacker-first'],audience)
        defender_first = views(catalog['defender-first'],audience)
        assert attacker_first[3] == defender_first[3]
        assert attacker_first[-1] == defender_first[-1]
    empty = views(catalog['empty-expiry'], 'commonwealth')
    partial = views(catalog['partial-expiry'], 'commonwealth')
    assert empty[-1] == partial[-1], 'private accepted-party/cancellation cause leaked'


def verify_matrix():
    counts = dict(traces=0, cuts=0, submissions=0, mutations=0, rawRejects=0, bindings=0)
    for source in source_cases():
        admit = admit_current if corrected(source) else submit
        for audience in SIDES:
            counts['traces'] += 1
            projected = views(source, audience)
            for cut, view in enumerate(projected):
                current = prefix(source, cut)
                data = raw(view, 'Observation')
                assert read_observation(data,current,audience) == view
                counts['cuts'] += 1
                # No raw authority identities/versions or opposing original bindings in bytes.
                for forbidden in (b'"stateVersion"',b'"prefix"',b'"seed"',b'"nextByteCursor"',
                                  b'"creationBinding"', b'cmb.', b'rnd.', b'slt.', b'seg.',
                                  (('axis' if audience=='commonwealth' else 'commonwealth')+'-assault-battalion').encode()):
                    assert forbidden not in data, forbidden
                if view['decision']:
                    for index, action in enumerate(view['decision']['actions']):
                        proposal = submission(view,index)
                        encoded = raw(proposal,'Submission')
                        deadline = view['decision']['deadlineUnixMilliseconds']
                        outcome = admit(current,audience,encoded,deadline-1)
                        assert outcome['status']=='accepted', (source['name'],audience,cut)
                        raw(outcome,'Outcome')
                        assert admit(current,audience,encoded,deadline)['status']=='rejected'
                        assert admit(current, 'commonwealth' if audience=='axis' else 'axis',encoded)['status']=='rejected'
                        if not corrected(source):
                            assert admit_current(current,audience,encoded,deadline-1)['status']=='rejected'
                        counts['submissions'] += 3
                        for field in ('decisionId','actionSetId','actionId','rulesRef','configRef','roundRef','slotRef'):
                            bad = proposal | {field:'pub.'+'e'*64}
                            assert admit(current,audience,raw(bad,'Submission'))['status']=='rejected'
                            counts['mutations'] += 1
                        bad = proposal | dict(campaignId='foreign-campaign')
                        assert admit(current,audience,raw(bad,'Submission'))['status']=='rejected'
                        counts['mutations'] += 1
                        bad = proposal | dict(openingRevision=proposal['openingRevision']+1)
                        assert admit(current,audience,raw(bad,'Submission'))['status']=='rejected'
                        counts['mutations'] += 1
                if cut == len(projected)-1:
                    for path, replacement in ((('visibleRevision',),view['visibleRevision']+1),
                        (('context','audience'),'commonwealth' if audience=='axis' else 'axis'),
                        (('own','currentToe'),0), (('cycleRef',),'sha256:'+'a'*64),
                        (('history',),[]), (('ownReceipts',),[] if view['ownReceipts'] else [dict(decisionId='pub.'+'1'*64,actionId='pub.'+'2'*64,receiptRef='pub.'+'3'*64)])):
                        bad = changed(view,path,replacement)
                        rejected(lambda bad=bad: read_observation(raw(bad,'Observation'),current,audience))
                        counts['mutations'] += 1
                    for bad in (b' '+data,data+b'\n',b'\xef\xbb\xbf'+data,data[:-1],
                        data.replace(b'"contractVersion":1',b'"contractVersion":true',1),
                        data.replace(b'"contractVersion":1',b'"contractVersion":1.0',1),
                        b'{"contractVersion":1,'+data[1:],b'{"authorityHash":"secret",'+data[1:],
                        data.replace(b'"contractVersion":1',b'"contractVersion":2',1),
                        b'"'+b'x'*65537+b'"'):
                        rejected(lambda bad=bad: read_observation(bad,current,audience))
                        counts['rawRejects'] += 1
            # Own accepted action readback survives consumption, later steps and expiry.
            for old_view in projected:
                if old_view['decision']:
                    for index in range(len(old_view['decision']['actions'])):
                        proposal = submission(old_view,index)
                        receipt = next((r for r in projected[-1]['ownReceipts'] if r['decisionId']==proposal['decisionId']),None)
                        if receipt:
                            outcome = admit(source,audience,raw(proposal,'Submission'),999999)
                            assert outcome['status'] == ('accepted' if receipt['actionId']==proposal['actionId'] else 'rejected')
                            counts['bindings'] += 1
                        elif projected[-1]['decision'] is None:
                            assert admit(source,audience,raw(proposal,'Submission'))['status']=='rejected'
                            counts['bindings'] += 1
    return counts


def generated_fixture():
    traces = []
    for source in source_cases():
        for audience in SIDES:
            projected = views(source,audience)
            cuts, goldens, seen = [], [], set()
            for cut, view in enumerate(projected):
                data = raw(view,'Observation')
                cuts.append(dict(cut=cut, bytes=len(data), sha256='sha256:'+hashlib.sha256(data).hexdigest(),
                                 visibleRevision=view['visibleRevision'],status=view['status']))
                category = view['decision']['kind'] if view['decision'] else view['status']
                if category not in seen or cut==len(projected)-1:
                    goldens.append(dict(cut=cut, canonicalJson=data.decode('ascii')))
                    seen.add(category)
            traces.append(dict(source=source['name'],audience=audience,cuts=cuts,goldens=goldens))
    return dict(contractVersion=1,scope='004A1 synthetic C3 projection; CON005 incomplete',
                sourcePins=source_pins(),traces=traces,successor2=fixture2())


def test_explicit_submission_context():
    source = next(x for x in source_cases() if x['name']=='attacker-first')
    view = views(source,'axis')[1]
    proposal = submission(view)
    assert set(('campaignId','rulesRef','configRef','roundRef','slotRef')) <= set(proposal), 'explicit protocol bindings missing'


def test_canonical_cycle_reference():
    source = next(x for x in source_cases() if x['name']=='attacker-first')
    view = views(source,'axis')[1]
    context = view['context']
    cycle = context['cycle']
    expected = dict(contractVersion=1,campaignId=context['campaignId'],rulesetHash=source['base']['boundary']['cycle']['rulesetHash'],
                    gameTurn=cycle['gameTurn'],operationStage=cycle['operationStage'],
                    playerPhaseSlot=cycle['playerPhaseSlot'],actingSide=cycle['phasingSide'],ordinal=cycle['ordinal'])
    expected_id = 'sha256:'+hashlib.sha256(steps.seq.identity(expected,'Public','axis')).hexdigest()
    assert view['cycleRef']==expected_id, 'cycle reference bypassed frozen Public tuple codec'


def test_round_continues_side_history():
    catalog = {s['name']:s for s in source_cases()}
    for audience in SIDES:
        before = views(catalog['accepted-decline'],audience)
        after = views(catalog['attacker-first'],audience)
        assert before[-1] == after[0], 'round reset authorized revision/receipt/history'
        offered = next(v for v in before if v['decision'])
        data = raw(submission(offered),'Submission')
        assert submit(catalog['attacker-first'],audience,data)['status']=='accepted', 'round lost prior own receipt recovery'


def test_clock_loss_does_not_accept_choice():
    source = next(x for x in source_cases() if x['name']=='attacker-first')
    source = prefix(source,1)
    data = raw(submission(views(source,'axis')[-1]),'Submission')
    assert submit(source,'axis',data,None)['status']=='rejected', 'clock-loss cancellation became accepted seal'


def test_cycle_scalar_bounds():
    good = dict(gameTurn=1,operationStage=1,playerPhaseSlot='first-acting-side',phasingSide='axis',ordinal=1)
    rejected(lambda: raw(good | dict(gameTurn=112),'Cycle'))
    rejected(lambda: raw(good | dict(operationStage=4),'Cycle'))


def test_literal_candidate_bytes():
    # Independently assembled from literal UnitSeed payloads and SHA256(domain + NUL + payload),
    # without calling the projection, reference builder, or candidate codec.
    literals = (
        b'{"kind":"full-close-assault","participantRef":"pub.41a4ce2e1f9012835c1d9f8bdcd22a2f51045c70c7054a09688a9ed3e05285f3","componentRef":"pub.1870cbc967a6f784699759d00dcff63d3939713d2b1ae38a0a27debdd45f53e6","committedToe":10}',
        b'{"kind":"full-close-assault","participantRef":"pub.73888fcb9015997811989c310a4b646c4f252412b8ae23c0e231e0b4eaf708f1","componentRef":"pub.dca67f50d44259b1ed0595f3c66d326fcd9d15b2204649876dd40493240656bc","committedToe":10}')
    source = next(x for x in source_cases() if x['name']=='attacker-first')
    for audience, literal in zip(SIDES,literals):
        candidate = views(source,audience)[1]['decision']['actions'][0]['candidate']
        assert raw(candidate,'Candidate') == literal


def test_canonical_edges():
    source = next(x for x in source_cases() if x['name']=='accepted-decline')
    current = prefix(source,1)
    view = views(current,'axis')[-1]
    candidate = view['decision']['actions'][0]['candidate']
    encoded = raw(candidate,'Candidate')
    reversed_candidate = dict(reversed(list(candidate.items())))
    rejected(lambda: parse(encode(reversed_candidate),'Candidate'))
    rejected(lambda: raw(candidate | dict(kind='unknown-combat-choice'),'Candidate'))
    rejected(lambda: raw({k:v for k,v in candidate.items() if k!='targetRef'},'Candidate'))
    rejected(lambda: raw(candidate | dict(targetRef='pub.'+'g'*64),'Candidate'))
    rejected(lambda: parse(encoded.replace(b'"kind":',b'"kind":null,"kind":'),'Candidate'))
    for actions in (list(reversed(view['decision']['actions'])),[view['decision']['actions'][0]]*2):
        bad = copy.deepcopy(view)
        bad['decision']['actions'] = actions
        seed = dict(decisionId=bad['decision']['decisionId'],openingRevision=bad['decision']['openingRevision'],actions=actions)
        bad['decision']['actionSetId'] = public_ref('set',seed,'SetSeed')
        rejected(lambda bad=bad: read_observation(raw(bad,'Observation'),current,'axis'))
    for field, values in (('history',view['history']*33),('ownReceipts',[dict(decisionId='pub.'+'1'*64,actionId='pub.'+'2'*64,receiptRef='pub.'+'3'*64)]*4)):
        rejected(lambda field=field,values=values: raw(view | {field:values},'Observation'))
    nested = 'x'
    for _ in range(17):
        nested = [nested]
    rejected(lambda: raw(nested,'id'+'[]'*17))
    for kind in ('Candidate','Submission','Observation'):
        rejected(lambda kind=kind: parse(b'\xff',kind))


def admit_current(source, audience, data, now=5000, available=True):
    try:
        validate_source(source)
        require(corrected(source))
        return submit(source,audience,data,now,available)
    except (ValueError,KeyError,TypeError):
        return dict(contractVersion=1,status='rejected',receipt=None)


def test_current_profile_boundary():
    catalog = {source['name']:source for source in source_cases()}
    assert 'clock-v2.axis.attacker.committed' in catalog, 'corrected authority profile absent'
    for name in ('attacker-first','defender-first'):
        source = prefix(catalog[name],1)
        for audience in SIDES:
            proposal = raw(submission(views(source,audience)[-1]),'Submission')
            assert admit_current(source,audience,proposal)['status']=='rejected', 'historical profile entered current admission'


def test_current_rejects_legacy():
    source = prefix(next(x for x in source_cases() if x['name']=='attacker-first'),1)
    data = raw(submission(views(source,'axis')[-1]),'Submission')
    assert admit_current(source,'axis',data)['status']=='rejected', 'historical profile entered current admission'


def test_corrected_clock_privacy():
    catalog = {s['name']:s for s in source_cases()}
    comparisons = 0
    for side in SIDES:
        for first in ('attacker','defender'):
            source = catalog['clock-v2.'+side+'.'+first+'.committed']
            owner = side if first=='defender' else ('commonwealth' if side=='axis' else 'axis')
            before,after = prefix(source,1),prefix(source,2)
            view = views(before,owner)[-1]
            assert raw(view,'Observation')==raw(views(after,owner)[-1],'Observation')
            data = raw(submission(view),'Submission')
            for now,available in rnd2.CLOCKS:
                outcomes = [admit_current(s,owner,data,now,available) for s in (before,after)]
                expected = 'accepted' if available and now is not None and 3000<=now<33000 else 'rejected'
                assert outcomes[0]==outcomes[1] and outcomes[0]['status']==expected,(side,first,now,available,outcomes)
                comparisons += 2
            sealed_owner = 'commonwealth' if owner=='axis' else 'axis'
            seal = raw(submission(views(before,sealed_owner)[-1]),'Submission')
            for cut in range(2,len(source['events'])+1):
                for now,available in rnd2.CLOCKS:
                    result = admit_current(prefix(source,cut),sealed_owner,seal,now,available)
                    assert result['status']=='accepted'
                    comparisons += 1
            for now,available in ((True,True),(3500.0,True),(3500,0),(3500,None),(-1,True)):
                assert admit_current(after,owner,data,now,available)['status']=='rejected'
    return comparisons


def test_corrected_context_and_history():
    catalog = {s['name']:s for s in source_cases()}
    for side in SIDES:
        predecessor = catalog['clock-v2.'+side+'.accepted-decline']
        round_source = catalog['clock-v2.'+side+'.attacker.committed']
        for audience in SIDES:
            before,after = views(predecessor,audience),views(round_source,audience)
            assert raw(before[-1],'Observation')==raw(after[0],'Observation'), 'profile context/history changed at unnumbered handoff'
            assert len({v['context']['configRef'] for v in before+after})==1
            assert before[0]['context']['configRef']!=views(catalog['accepted-decline'],audience)[0]['context']['configRef']
            for old in before:
                if old['decision']:
                    receipt = next((r for r in after[-1]['ownReceipts'] if r['decisionId']==old['decision']['decisionId']),None)
                    if receipt:
                        proposal = raw(submission(old),'Submission')
                        assert admit_current(round_source,audience,proposal,None,False)['status']=='accepted'
            legacy = views(catalog['attacker-first'],audience)[1]
            assert admit_current(prefix(round_source,1),audience,raw(submission(legacy),'Submission'))['status']=='rejected'


def test_cancelled_seal_has_no_receipt():
    for side in SIDES:
        source = next(s for s in source_cases() if s['name']=='clock-v2.'+side+'.defender.fault')
        for audience in SIDES:
            frames = views(source,audience)
            assert frames[2]['ownReceipts']==frames[3]['ownReceipts'], 'System clock cancellation fabricated own seal receipt'
            assert frames[3]['status']=='closed'


def test_current_source_integrity():
    source = next(s for s in source_cases() if s['name']=='clock-v2.axis.attacker.committed')
    for altered in (changed(source,('base','contractVersion'),2.0),
                    changed(source,('inputs',0,'admittedAt'),3000.0),
                    changed(source,('inputs',0,'clockAvailable'),1)):
        rejected(lambda: views(altered,'axis'))
    view = views(prefix(source,1),'axis')[-1]
    original = raw(view,'Observation')
    view['own']['currentToe']=0
    assert raw(views(prefix(source,1),'axis')[-1],'Observation')==original, 'cached projection mutated by caller'
    proposal = raw(submission(views(prefix(source,1),'axis')[-1]),'Submission')
    for malformed in ({},None,source | dict(name='foreign-profile')):
        assert admit_current(malformed,'axis',proposal)['status']=='rejected'


def test_corrected_declassifier_probes():
    for side in SIDES:
        for first in ('attacker','defender'):
            source = prefix(next(s for s in source_cases() if s['name']=='clock-v2.'+side+'.'+first+'.committed'),2)
            observer = side if first=='defender' else ('commonwealth' if side=='axis' else 'axis')
            state = replay_states(source)[-1]
            facts = audience_facts(source,state,observer)
            enemy_index = next(i for i,e in enumerate(state['world']['elements']) if not e['elementId'].startswith(observer))
            # These mutations are declassifier probes, never authenticated history evidence.
            for path,value in ((('prefix',),'sha256:'+'f'*64),(('stateVersion',),state['stateVersion']+1),
                (('randomState','seed'),999),(('randomState','nextByteCursor'),50),
                (('world','elements',enemy_index,'ammunition','points'),0),
                (('world','elements',enemy_index,'components',0,'currentToe'),3),
                (('world','elements',enemy_index,'operationalState','cohesionLevel'),4)):
                assert audience_facts(source,changed(state,path,value),observer)==facts
            forged = changed(source,('base','boundary','world','elements',enemy_index,'ammunition','points'),0)
            rejected(lambda: views(forged,observer))


def test_literal_corrected_configuration():
    payload = b'{"selectionBudgetMilliseconds":30000,"rbaBudgetMilliseconds":30000,"assignmentBudgetMilliseconds":30000,"assignmentClockPolicyId":"sandtable.combat.public-opening-clock.v2"}'
    expected = 'pub.'+hashlib.sha256(b'sandtable.observation.combat.config.v1\0'+payload).hexdigest()
    for source in source_cases():
        if corrected(source):
            for audience in SIDES:
                assert views(source,audience)[0]['context']['configRef']==expected


def verify_fixture(data):
    expected = (json.dumps(generated_fixture(),indent=2)+'\n').encode('utf-8')
    require(type(data) is bytes and data==expected)


def test_fixture_integrity():
    original = json.loads(FIXTURE.read_bytes())
    floating = copy.deepcopy(original); floating['traces'][0]['cuts'][0]['cut']=0.0
    boolean = copy.deepcopy(original); boolean['traces'][0]['cuts'][0]['cut']=False
    for data in ((json.dumps(floating,indent=2)+'\n').encode(),
                 (json.dumps(boolean,indent=2)+'\n').encode(),
                 FIXTURE.read_bytes().replace(b'"contractVersion": 1,',b'"contractVersion": 1, "contractVersion": 1,',1),
                 FIXTURE.read_bytes().replace(b'\n',b'\r\n')):
        rejected(lambda: verify_fixture(data))


# A2 uses a disjoint codec/profile; accepted A1 surface above stays unchanged.
A1_SCHEMA_HASH='baa45489e006c0cf5ab7cc4cea2b879e6f3cb478127ea0b579bc305dd62dcf5c'
A1_FIXTURE_HASH='659d4fd09b56120ef0562248dedc27fc84f1c696d268a2861dd65d76d32893fd'


# A2 is a separate typed profile over exact composed C3a / Round2 / Result2 evidence.
result_spec = importlib.util.spec_from_file_location('side_result_v2', ROOT / 'verify-combat-result-settlement-v2.py')
res2 = importlib.util.module_from_spec(result_spec)
result_spec.loader.exec_module(res2)
SCHEMA2 = {k: [tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['objects2'].items()}


def typed2(value,kind,depth=0):
    require(depth <= INVENTORY['limits2']['depth'])
    if kind.endswith('?'):
        if value is not None: typed2(value,kind[:-1],depth)
    elif kind == 'Candidate2':
        require(type(value) is dict and type(value.get('kind')) is str
                and value['kind'] in INVENTORY['candidateTags2']['Candidate2'])
        typed2(value,INVENTORY['candidateTags2']['Candidate2'][value['kind']],depth)
    elif kind in SCHEMA2:
        require(type(value) is dict and set(value)=={k for k,_ in SCHEMA2[kind]})
        for key,child in SCHEMA2[kind]: typed2(value[key],child,depth+1)
        if kind=='Observation2': require(len(value['history'])<=64 and len(value['ownReceipts'])<=8)
        if kind=='Decision2': require(1<=len(value['actions'])<=2)
        if kind=='Outcome2': require((value['receipt'] is not None)==(value['status']=='accepted'))
        if 'route' in value: require(len(value['route'])<=9)
        if kind=='Custody2': require(len(value['guardRoute'])<=4)
        if kind in ('RetreatChoice2','Retreat2'): require(1<=len(value['route'])<=2)
        if kind=='Settlement2':
            require(all(len(value[k])<=1 for k in ('ownGuards','ownEntitlements','ownObligations')))
    elif kind.endswith('[]'):
        require(type(value) is list and len(value)<=64)
        for child in value: typed2(child,kind[:-2],depth+1)
    elif kind in INVENTORY['integerBounds2']:
        lo,hi=INVENTORY['integerBounds2'][kind];require(type(value) is int and lo<=value<=hi)
    elif kind in INVENTORY['enums2']:
        require(type(value) is str and value in INVENTORY['enums2'][kind])
    elif kind=='bool2': require(type(value) is bool)
    else: typed(value,kind,depth)


def canonical2(value,kind):
    if kind.endswith('?'): return None if value is None else canonical2(value,kind[:-1])
    if kind=='Candidate2': return canonical2(value,INVENTORY['candidateTags2']['Candidate2'][value['kind']])
    if kind in SCHEMA2: return {k:canonical2(value[k],child) for k,child in SCHEMA2[kind]}
    if kind.endswith('[]'): return [canonical2(v,kind[:-2]) for v in value]
    return canonical(value,kind)


def raw2(value,kind):
    typed2(value,kind);data=encode(canonical2(value,kind));require(len(data)<=65536);return data


def parse2(data,kind):
    require(type(data) is bytes and 0<len(data)<=65536)
    def pairs(items):
        value={}
        for key,child in items: require(key not in value);value[key]=child
        return value
    try: value=json.loads(data.decode('ascii'),object_pairs_hook=pairs,parse_constant=lambda _:require(False))
    except (ValueError,UnicodeError,RecursionError) as error: raise Invalid('CMB-SIDE-REJECTED') from error
    require(raw2(value,kind)==data);return value


def public_ref2(domain,value,kind=None):
    payload=raw2(value,kind) if kind else encode(value)
    return 'pub.'+hashlib.sha256(INVENTORY['domains2'][domain].encode()+b'\0'+payload).hexdigest()


def context_bytes2(ctx):
    res2.verify_context(ctx)
    return encode(dict(base=ctx['base'],predecessor=ctx['predecessor'],roundInputs=ctx['roundInputs'],
                       roundEvents=[e.decode('ascii') for e in ctx['roundEvents']],committed=ctx['committed']))


def composed_source2(name,ctx,inputs,events):
    pre=ctx['predecessor']
    return dict(name=name,family='settlement-v2',base=ctx,
                inputs=copy.deepcopy(pre['inputs']+ctx['roundInputs']+inputs),
                events=[e['canonicalUtf8'].encode() for e in pre['events']]+ctx['roundEvents']+events)


@lru_cache(maxsize=1)
def source_catalog2():
    res2.verify_fixture(res2.FIXTURE.read_bytes())
    sources=[]
    for trace in json.loads(res2.FIXTURE.read_bytes())['traces']:
        ctx=dict(base=json.loads(trace['baseCanonicalUtf8']),predecessor=trace['predecessor'],
                 roundInputs=trace['roundInputs'],roundEvents=[e.encode() for e in trace['roundEventCanonicalUtf8']],
                 committed=json.loads(trace['committedCanonicalUtf8']))
        res2.verify_context(ctx)
        sources.append(composed_source2('settlement-v2.'+trace['name'],ctx,trace['resultInputs'],
                                       [e.encode() for e in trace['resultEventCanonicalUtf8']]))
    # Actual authenticated same-owner prior-time forks, not altered state probes.
    for source in list(sources):
        if not source['name'].startswith('settlement-v2.attacker-capture-escape.'): continue
        ctx=source['base'];owner=ctx['base']['steps']['selection']['defender']['unit']['originalSide']
        for accepted in (10001,11000):
            state=res2.initial(ctx);inputs=[];events=[]
            for kind,actor,now,choice in (('resolve','system',None,None),('advance','system',10000,None),
                ('choose',owner,accepted,'retreat'),('advance','system',None,None),('advance','system',None,None),
                ('advance','system',10500,None),('choose',owner,10600,'relocate-and-guard')):
                inp=res2.trusted(res2.command(ctx,state,kind,choice),actor,now)
                state,event,_=res2.transition(ctx,state,inp);inputs.append(inp);events.append(event)
            sources.append(composed_source2(source['name']+'.prior-time-'+str(accepted),ctx,inputs,events))
    # Retain clock-loss fallbacks with the true owner initiator and System author.
    # Result context remains the exact committed predecessor; only later live-window input forks.
    for source in list(sources):
        if not source['name'].startswith('settlement-v2.attacker-capture-escape.') or '.prior-time-' in source['name']: continue
        ctx=source['base'];offset=len(ctx['predecessor']['inputs'])+len(ctx['roundInputs'])
        state=res2.initial(ctx)
        for i,(original,event) in enumerate(zip(source['inputs'][offset:],source['events'][offset:])):
            if original['command']['kind']=='choose':
                inp=copy.deepcopy(original);inp['admittedAt']=None;inp['clockAvailable']=False
                _,fallback,_=res2.transition(ctx,state,inp)
                sources.append(composed_source2(source['name']+'.fallback-'+state['window']['kind'],ctx,
                    source['inputs'][offset:offset+i]+[inp],source['events'][offset:offset+i]+[fallback]))
            state=res2.read_event(event,ctx,state,original)
    return sources


def source_cases2(): return copy.deepcopy(source_catalog2())


def validate_source2(source):
    require(type(source) is dict and set(source)=={'name','family','base','inputs','events'})
    expected=next((s for s in source_catalog2() if s['name']==source['name']),None)
    require(expected is not None and source['family']=='settlement-v2'
            and context_bytes2(source['base'])==context_bytes2(expected['base']))
    require(type(source['inputs']) is list and type(source['events']) is list
            and len(source['inputs'])==len(source['events'])<=len(expected['events']))
    count=len(source['events'])
    require(all(type(e) is bytes for e in source['events']) and source['events']==expected['events'][:count]
            and encode(source['inputs'])==encode(expected['inputs'][:count]))
    return source['name'],count


@lru_cache(maxsize=1024)
def cached_frames2(name,count):
    source=next(s for s in source_catalog2() if s['name']==name)
    ctx=source['base'];pre=ctx['predecessor'];boundary=pre['boundary']
    state=steps.initial(boundary);family='steps';frames=[(family,state)]
    a=len(pre['inputs']);b=a+len(ctx['roundInputs'])
    for i,(inp,event) in enumerate(zip(source['inputs'][:count],source['events'][:count])):
        if i<a: state=steps.read_event(event,boundary,state,inp)
        elif i<b:
            if i==a: state=rnd2.initial(ctx['base']);family='round'
            state=rnd2.read_event(event,ctx['base'],state,inp)
        else:
            if i==b: state=res2.initial(ctx);family='result'
            state=res2.read_event(event,ctx,state,inp)
        frames.append((family,state))
    return frames


def replay_frames2(source): return copy.deepcopy(cached_frames2(*validate_source2(source)))


def audience_facts2(source,family,state,audience,round_ref=None):
    """Allowlist only. Called on replayed frames; probe callers cannot authenticate altered state."""
    ctx=source['base'];boundary=ctx['base']['boundary'];cycle=boundary['cycle']
    visible_world=state.get('world',boundary['world'])
    enemy=next(e for e in visible_world['elements'] if e['elementId']!=audience+'-assault-battalion')
    representations=[r for r in visible_world['representations'] if r['boundElementIds']==[enemy['elementId']]]
    require(len(representations)==1 and representations[0]['bindingKind']=='independent-element'
            and representations[0]['currentLocationId']==enemy['currentLocationId'])
    # Established observation policy projects this current apparent representation location.
    # A1 fact extraction is pure; its numeric codec is not used for A2 state.
    local=dict(family='steps-clock-v2' if family=='steps' else 'round-clock-v2',
               base=boundary if family=='steps' else ctx['base'])
    if family=='result':
        synthetic=copy.deepcopy(ctx['committed']);synthetic['world']=state['world']
        synthetic['closed']=state['closed'];synthetic['stepIndex']=6 if state['closed'] else 5
        facts=audience_facts(local,synthetic,audience)
    else: facts=audience_facts(local,state,audience)
    context=facts['context']
    context['rulesRef']=public_ref2('rules',dict(rulesetHash=cycle['rulesetHash'],profile='singleton-infantry-close-assault',policy='CMB-POL-006',candidateCodec=2))
    context['configRef']=public_ref2('config',dict(selectionBudgetMilliseconds=30000,rbaBudgetMilliseconds=30000,
        assignmentBudgetMilliseconds=30000,retreatBudgetMilliseconds=30000,custodyBudgetMilliseconds=30000,
        assignmentClockPolicyId=rnd2.POLICY,resultClockPolicyId=res2.POLICY,candidateCodec=2),'ClockConfigSeed2')
    facts['apparentEnemy']['targetRef']=public_ref2('target',dict(context=context,locationId=facts['apparentEnemy']['locationId']))
    facts['own']['spentCp']=dict(numerator=facts['own']['spentCp'],denominator=1)
    for candidate in facts['candidates']:
        candidate['contractVersion']=2
        if 'targetRef' in candidate: candidate['targetRef']=facts['apparentEnemy']['targetRef']
    facts['settlement']=None
    if family!='result': return facts
    own=facts['own'];world=state['world'];element=audience+'-assault-battalion'
    settlement_ref=public_ref2('settlement',dict(context=context,roundRef=round_ref,participantRef=own['participantRef']))
    def asset(kind): return public_ref2(kind,dict(settlementRef=settlement_ref,participantRef=own['participantRef']))
    st=world['settlements'][0] if world['settlements'] else None
    result=dict(settlementRef=settlement_ref,ownLoss=None,ownRetreat=None,ownCohesionCauses=[],
                ownCustody=None,ownGuards=[],ownEntitlements=[],ownObligations=[],relation=None)
    if st and st['losses']:
        loss=next(v for v in st['losses']['roles'] if v['component']['unit']['originalSide']==audience)
        result['ownLoss']=dict(componentRef=own['componentRef'],**{k:loss[k] for k in ('committedToe','lossToe','capturedToe','otherLossToe','remainingToe')})
    if st and st['disposition'] and st['defender']['originalSide']==audience and st['disposition']['kind']!='not-required':
        disposition=st['disposition'];retreat=st['retreat']
        result['ownRetreat']=dict(choice=disposition['kind'],route=disposition['route'],plannedDistance=disposition['plannedDistance'],
            completedDistance=retreat['completedDistance'] if retreat else None,beforeCp=retreat['beforeCp'] if retreat else None,
            afterCp=retreat['afterCp'] if retreat else None,excessCpDp=retreat['excessCpDp'] if retreat else None)
    causes=[c for c in world['cohesionCauses'] if c['elementId']==element]
    for index,cause in enumerate(causes):
        result['ownCohesionCauses'].append(dict(causeRef=public_ref2('cause',dict(settlementRef=settlement_ref,ownOrdinal=index,kind=cause['kind'])),
            **{k:cause[k] for k in ('kind','points','before','after')},scope=dict(gameTurn=cause['gameTurn'],operationStage=cause['operationStage'])))
    for lot in world['custodyLots']:
        if lot['captor']['originalSide']!=audience: continue
        custody=st['custody'];guarded=lot['status']=='guarded'
        result['ownCustody']=dict(custodyRef=asset('custody'),quantity=lot['quantity'],originSide=lot['originalComponent']['unit']['originalSide'],
            prisonerClass='infantry',originLocationId=lot['originLocationId'],currentLocationId=lot['currentLocationId'],status=lot['status'],
            guardRef=asset('guard') if guarded else None,guardRoute=custody['route'] if guarded else [],
            donorToeBefore=custody['donorToeBefore'] if custody else None,donorToeAfter=custody['donorToeAfter'] if custody else None)
    subjects={}
    for guard in world['guards']:
        if guard['originComponent']['unit']['originalSide']!=audience: continue
        subjects[guard['guardId']]=asset('guard')
        readiness={k:guard['readiness'][k] for k in ('gameTurn','operationStage','waterStatus','storesStatus','pinned')}
        result['ownGuards'].append(dict(guardRef=asset('guard'),custodyRef=asset('custody'),donorComponentRef=own['componentRef'],
            locationId=guard['currentLocationId'],currentToe=guard['toe'],baseCapabilityPoints=guard['baseCapabilityPointAllowance'],
            offensiveRating=guard['offensiveCloseAssaultRating'],defensiveRating=guard['defensiveCloseAssaultRating'],
            spentCp=guard['operationalState']['capabilityPointsExpended'],cohesion=guard['operationalState']['cohesionLevel'],
            ammunition=guard['ammunition']['points'],readiness=readiness))
    for entitlement in world['replacementEntitlements']:
        if entitlement['originalComponent']['unit']['originalSide']!=audience: continue
        subjects[entitlement['entitlementId']]=asset('entitlement')
        result['ownEntitlements'].append(dict(entitlementRef=asset('entitlement'),componentRef=own['componentRef'],
            **{k:entitlement[k] for k in ('quantity','reunionLocationId','earnedScope','delayOperationStages','eligibleScope','status')}))
    for obligation in world['futureObligations']:
        if obligation['subjectId'] not in subjects: continue
        result['ownObligations'].append(dict(obligationRef=asset('obligation'),subjectRef=subjects[obligation['subjectId']],
            status='pending',kind='replacement-training' if obligation['kind']=='replacement-training-gate' else obligation['kind'],
            **{k:obligation[k] for k in ('earnedScope','eligibleScope')}))
    for relation in world['relationships']:
        if relation['active'] and {relation['attacker']['elementId'],relation['defender']['elementId']}=={e['elementId'] for e in world['elements']}:
            result['relation']=dict(relationRef=asset('relation'),kind=relation['kind'],targetRef=facts['apparentEnemy']['targetRef'])
    window=state['window']
    if window and window['owner']==audience:
        facts.update(status='choice-required',decisionKind=window['kind'],deadline=window['timing']['deadlineUnixMilliseconds'])
        if window['kind']=='retreat':
            route=res2.projected_world(ctx,state,'disposition',retreat='retreat')['settlements'][0]['disposition']['route']
            facts['candidates']=[dict(contractVersion=2,kind='retreat',participantRef=own['participantRef'],route=route,distance=1,cpCost=1,
                excessCpDp=max(0,own['spentCp']['numerator']+1-10)),dict(contractVersion=2,kind='refuse-retreat',participantRef=own['participantRef'])]
        else:
            route=res2.projected_world(ctx,state,'custody',custody='relocate-and-guard')['settlements'][0]['custody']['route']
            facts['candidates']=[dict(contractVersion=2,kind='relocate-and-guard',custodyRef=asset('custody'),donorComponentRef=own['componentRef'],route=route,guardToe=1),
                dict(contractVersion=2,kind='leave-unguarded',custodyRef=asset('custody'))]
    if any(v for k,v in result.items() if k!='settlementRef') or (window and window['owner']==audience): facts['settlement']=result
    return facts


def successful_choice2(inp,event,audience):
    value=json.loads(event);effect=value['effect'];author=value.get('author',inp['actor']);kind=inp['command']['kind']
    if inp['actor']!=audience or author!=audience: return None
    if kind=='choose-selection' and effect['kind']=='selection-closed': return inp['command']['choice']
    if kind=='decline-rba' and effect['kind']=='rba-declined': return 'decline-retreat-before-assault'
    if kind=='seal-choice' and effect['kind']=='choice-sealed': return 'full-close-assault'
    if kind=='choose' and effect['kind'] in ('disposition-recorded','custody-settled') and effect['reason']=='owner-choice': return inp['command']['choice']
    return None


def receipt2(decision_id,action_id):
    seed=dict(decisionId=decision_id,actionId=action_id)
    return dict(contractVersion=2,**seed,receiptRef=public_ref2('receipt',seed))


@lru_cache(maxsize=2048)
def cached_views2(name,count,audience):
    source=next(s for s in source_catalog2() if s['name']==name)
    frames=cached_frames2(name,count);boundary=source['base']['base']['boundary']
    history=[];receipts=[];views_out=[];revision=0;previous=None;round_ref=None
    for index,(family,state) in enumerate(frames):
        facts=audience_facts2(source,family,state,audience,round_ref)
        own_receipt=None
        if index:
            chosen=successful_choice2(source['inputs'][index-1],source['events'][index-1],audience)
            if chosen:
                decision=views_out[-1]['decision'];require(decision is not None)
                action=next(a for a in decision['actions'] if a['candidate']['kind']==chosen)
                own_receipt=receipt2(decision['decisionId'],action['actionId']);receipts.append(own_receipt)
        changed=encode(facts)!=encode(previous) or own_receipt is not None
        if index and changed: revision+=1
        context=facts['context'];cycle=context['cycle']
        public_cycle=dict(contractVersion=1,campaignId=context['campaignId'],rulesetHash=boundary['cycle']['rulesetHash'],
            gameTurn=cycle['gameTurn'],operationStage=cycle['operationStage'],playerPhaseSlot=cycle['playerPhaseSlot'],actingSide=cycle['phasingSide'],ordinal=cycle['ordinal'])
        cycle_ref='sha256:'+hashlib.sha256(steps.seq.identity(public_cycle,'Public',boundary['firstActingSide'])).hexdigest()
        seed=dict(context=context,cycleRef=cycle_ref,positionId=facts['positionId'],openingRevision=revision,
            kind=facts['decisionKind'] or 'force-assignment',participantRef=facts['own']['participantRef'],targetRef=facts['apparentEnemy']['targetRef'])
        if facts['roundOpen'] and round_ref is None: round_ref=public_ref2('round',seed|dict(kind='force-assignment'),'IdentitySeed2')
        decision=None
        if facts['decisionKind']:
            if views_out and views_out[-1]['decision'] and not changed: decision=views_out[-1]['decision']
            else:
                decision_id=public_ref2('decision',seed,'IdentitySeed2');slot_ref=None
                if facts['decisionKind']=='force-assignment':
                    role='attacker' if audience==cycle['phasingSide'] else 'defender'
                    slot_ref=public_ref2('slot',dict(roundRef=round_ref,audience=audience,role=role))
                actions=[dict(contractVersion=2,actionId=public_ref2('action',dict(decisionId=decision_id,candidate=c),'ActionSeed2'),candidate=c) for c in facts['candidates']]
                decision=dict(contractVersion=2,decisionId=decision_id,kind=facts['decisionKind'],openingRevision=revision,
                    slotRef=slot_ref,settlementRef=facts['settlement']['settlementRef'] if facts['settlement'] else None,
                    deadlineUnixMilliseconds=facts['deadline'],actionSetId=public_ref2('set',dict(decisionId=decision_id,openingRevision=revision,actions=actions),'SetSeed2'),actions=actions)
        if changed: history.append(dict(visibleRevision=revision,positionId=facts['positionId'],status=facts['status'],ownReceiptRef=own_receipt['receiptRef'] if own_receipt else None))
        view=dict(contractVersion=2,context=context,cycleRef=cycle_ref,positionId=facts['positionId'],visibleRevision=revision,
            own=facts['own'],apparentEnemy=facts['apparentEnemy'],roundRef=round_ref,status=facts['status'],decision=decision,
            settlement=facts['settlement'],ownReceipts=copy.deepcopy(receipts),history=copy.deepcopy(history))
        raw2(view,'Observation2');views_out.append(view);previous=facts
    return views_out


def views2(source,audience):
    typed(audience,'side');name,count=validate_source2(source)
    return copy.deepcopy(cached_views2(name,count,audience))


def read_observation2(data,source,audience):
    value=parse2(data,'Observation2');require(value['context']['audience']==audience and data==raw2(views2(source,audience)[-1],'Observation2'));return value


def submission2(view,index=0):
    decision=view['decision'];action=decision['actions'][index]
    return dict(contractVersion=2,campaignId=view['context']['campaignId'],rulesRef=view['context']['rulesRef'],configRef=view['context']['configRef'],
        audience=view['context']['audience'],roundRef=view['roundRef'],settlementRef=decision['settlementRef'],decisionId=decision['decisionId'],
        slotRef=decision['slotRef'],openingRevision=decision['openingRevision'],actionSetId=decision['actionSetId'],actionId=action['actionId'],candidate=action['candidate'])


def attempt2(source,audience,proposal,now,available):
    family,state=replay_frames2(source)[-1];ctx=source['base'];kind=proposal['candidate']['kind']
    a=len(ctx['predecessor']['inputs']);b=a+len(ctx['roundInputs']);count=len(source['events'])
    # A fragment handoff occurs at same public cut, without resetting history.
    if count==a: family='round';state=rnd2.initial(ctx['base'])
    if count==b: family='result';state=res2.initial(ctx)
    if family=='steps':
        if kind in ('select-close-assault','finish-without-attack'):
            cmd=steps.command(state,'choose-selection',decisionId=state['selectionWindow']['decisionId'],choice=kind,
                candidate=steps.candidate(ctx['base']['boundary']) if kind=='select-close-assault' else None)
        else: cmd=steps.command(state,'decline-rba',decisionId=state['rbaWindow']['decisionId'],participant=state['selection']['defender']['unit'])
        inp=steps.trusted(cmd,audience,now,available);after,event,_=steps.transition(ctx['base']['boundary'],state,inp)
    elif family=='round':
        role='attacker' if audience==ctx['base']['boundary']['cycle']['actingSide'] else 'defender'
        inp=rnd2.trusted(rnd2.command(ctx['base'],state,'seal-choice',role),audience,now,available)
        after,event,_=rnd2.transition(ctx['base'],state,inp)
    else:
        inp=res2.trusted(res2.command(ctx,state,'choose',kind),audience,now,available);after,event,_=res2.transition(ctx,state,inp)
    return inp,after,event


def admit_a2(source,audience,data,now=5000,available=True):
    reject=dict(contractVersion=2,status='rejected',receipt=None)
    try:
        require(type(available) is bool and (now is None or type(now) is int and 0<=now<=253402300799999));typed(audience,'side')
        proposal=parse2(data,'Submission2');require(proposal['audience']==audience)
        projected=views2(source,audience)
        offered=next((v for v in reversed(projected) if v['decision'] and v['decision']['decisionId']==proposal['decisionId']),None)
        require(offered is not None)
        index=next((i for i,a in enumerate(offered['decision']['actions']) if a['actionId']==proposal['actionId']),None)
        require(index is not None and raw2(submission2(offered,index),'Submission2')==data)
        receipt=next((r for r in projected[-1]['ownReceipts'] if r['decisionId']==proposal['decisionId']),None)
        if receipt:
            require(receipt['actionId']==proposal['actionId']);return dict(contractVersion=2,status='accepted',receipt=receipt)
        require(projected[-1]['decision']==offered['decision'])
        inp,_,event=attempt2(source,audience,proposal,now,available)
        require(event is not None and successful_choice2(inp,event,audience)==proposal['candidate']['kind'])
        return dict(contractVersion=2,status='accepted',receipt=receipt2(proposal['decisionId'],proposal['actionId']))
    except (Invalid,steps.Invalid,rnd2.Invalid,res2.Invalid,KeyError,TypeError,ValueError,StopIteration): return reject



def test_a2_codec_boundary():
    assert 'Observation2' in INVENTORY.get('objects2',{}), 'version2 observation codec absent'
    assert 'Candidate2' in INVENTORY.get('candidateTags2',{}), 'version2 candidate codec absent'


def test_a2_rejects_a1():
    source=prefix(next(x for x in source_cases() if x['name']=='clock-v2.axis.attacker.committed'),1)
    data=raw(submission(views(source,'axis')[-1]),'Submission')
    assert admit_a2(source,'axis',data)['status']=='rejected', 'A1 profile entered A2 admission'


def test_a1_preserved():
    original={k:v for k,v in INVENTORY.items() if not k.endswith('2')}
    assert hashlib.sha256(encode(original)).hexdigest()==A1_SCHEMA_HASH, 'A1 schema changed'
    fixture=json.loads(FIXTURE.read_bytes());fixture.pop('successor2',None)
    assert hashlib.sha256(encode(fixture)).hexdigest()==A1_FIXTURE_HASH, 'A1 literal fixture changed'


def test_a2_source_and_handoff():
    sources=source_cases2();assert len(sources)==48
    assert len([s for s in sources if '.prior-time-' not in s['name'] and '.fallback-' not in s['name']])==32
    for source in sources:
        ctx=source['base'];a=len(ctx['predecessor']['inputs']);b=a+len(ctx['roundInputs'])
        for side in SIDES:
            projected=views2(source,side);first=projected[0]['context']
            assert all(v['context']==first for v in projected), 'A2 context rotates inside profile'
            assert first['configRef']!=views(next(s for s in source_cases() if s['name']=='clock-v2.'+ctx['base']['boundary']['cycle']['actingSide']+'.accepted-decline'),side)[0]['context']['configRef']
            frames=replay_frames2(source)
            left=audience_facts2(source,*frames[a],side,projected[a]['roundRef'])
            right=audience_facts2(source,'round',rnd2.initial(ctx['base']),side,projected[a]['roundRef'])
            assert encode(left)==encode(right), 'C3a handoff adds facts'
            left=audience_facts2(source,*frames[b],side,projected[b]['roundRef'])
            right=audience_facts2(source,'result',res2.initial(ctx),side,projected[b]['roundRef'])
            assert encode(left)==encode(right), 'Result2 handoff changes facts'
            assert projected[b]==projected[b+1], 'private resolution disclosed'
            assert all(r in projected[-1]['ownReceipts'] for r in projected[a]['ownReceipts'])
    return len(sources)*2


def test_a2_game_facts():
    assert 'activationGate' not in INVENTORY['objects2']['Obligation2']
    assert INVENTORY['enums2']['obligationStatus2']==['pending']
    assert INVENTORY['enums2']['obligationKind2']==['guard-priority-upkeep','replacement-training']
    causes=set();choices=set();limits=dict(receipts=0,history=0,bytes=0);observations=0
    for source in source_cases2():
        final_state=replay_frames2(source)[-1][1]
        for side in SIDES:
            projected=views2(source,side);final=projected[-1]
            element=next(e for e in final_state['world']['elements'] if e['elementId']==side+'-assault-battalion')
            assert final['own']['currentToe']==element['components'][0]['currentToe']
            assert final['own']['spentCp']==element['operationalState']['capabilityPointsExpended']
            assert final['own']['cohesion']==element['operationalState']['cohesionLevel']
            for view in projected:
                observations+=1;limits['receipts']=max(limits['receipts'],len(view['ownReceipts']));limits['history']=max(limits['history'],len(view['history']));limits['bytes']=max(limits['bytes'],len(raw2(view,'Observation2')))
                settlement=view['settlement']
                if settlement:
                    causes.update(c['kind'] for c in settlement['ownCohesionCauses'])
                    for entitlement in settlement['ownEntitlements']:
                        assert entitlement['delayOperationStages']==12 and entitlement['eligibleScope']==dict(gameTurn=5,operationStage=1)
                        assert view['own']['currentToe']==settlement['ownLoss']['remainingToe'], 'escape credited immediate TOE'
                    for guard in settlement['ownGuards']:
                        assert guard['currentToe']==1 and guard['ammunition']==0 and (guard['offensiveRating'],guard['defensiveRating'])==(0,1)
                        assert settlement['ownCustody']['donorToeBefore']-settlement['ownCustody']['donorToeAfter']==1
                    for obligation in settlement['ownObligations']: assert obligation['status']=='pending'
                if view['decision']:
                    kinds=[a['candidate']['kind'] for a in view['decision']['actions']];choices.update(kinds)
                    expected={'selection':['select-close-assault','finish-without-attack'],'rba':['decline-retreat-before-assault'],
                              'force-assignment':['full-close-assault'],'retreat':['retreat','refuse-retreat'],'custody':['relocate-and-guard','leave-unguarded']}
                    assert kinds==expected[view['decision']['kind']]
    assert causes=={'loss-dp','retreat-excess-dp','assault-victory-rp'}
    assert choices==set(INVENTORY['candidateTags2']['Candidate2']) and limits['receipts']==4
    return dict(observations=observations,maximums=limits)


def test_a2_privacy_pairs():
    comparisons=0;clock_checks=0
    sources=source_cases2()
    for source in sources:
        ctx=source['base'];a=len(ctx['predecessor']['inputs']);b=a+len(ctx['roundInputs'])
        first=ctx['roundInputs'][1]['actor'];other=next(s for s in SIDES if s!=first)
        before,after=prefix(source,a+1),prefix(source,a+2)
        assert views2(before,other)[-1]==views2(after,other)[-1];comparisons+=1
        proposal=raw2(submission2(views2(before,other)[-1]),'Submission2')
        for now in (None,0,2999,3000,3499,3500,3999,4000,4001,32999,33000,33001,253402300799999):
            for available in (False,True):
                x=admit_a2(before,other,proposal,now,available);y=admit_a2(after,other,proposal,now,available)
                assert x==y,(source['name'],now,available);clock_checks+=1
        for side in SIDES:
            projected=views2(source,side)
            for i in range(b+1,len(projected)):
                event=json.loads(source['events'][i-1]);effect=event['effect']
                if effect['kind']=='choice-opened' and effect['window']['owner']!=side:
                    assert projected[i]==projected[i-1], 'opponent window disclosed';comparisons+=1
                if effect['kind']=='disposition-recorded' and source['inputs'][i-1]['actor']!=side:
                    assert projected[i]==projected[i-1], 'opponent retreat intent disclosed';comparisons+=1
                if effect['kind']=='custody-settled' and source['inputs'][i-1]['actor']!=side and effect['payload']['kind']=='relocate-and-guard':
                    assert projected[i]==projected[i-1], 'guard disclosed to captive victim';comparisons+=1
                if effect['kind']=='custody-settled' and source['inputs'][i-1]['actor']!=side and effect['payload']['kind']=='leave-unguarded':
                    assert projected[i]!=projected[i-1] and projected[i]['settlement']['ownEntitlements'], 'authorized escape entitlement absent';comparisons+=1
        if '.prior-time-10001' in source['name']:
            paired=next(s for s in sources if s['name']==source['name'].replace('10001','11000'))
            for side in SIDES:
                assert views2(source,side)==views2(paired,side), 'prior accepted time entered own history';comparisons+=1
            owner=ctx['base']['steps']['selection']['defender']['unit']['originalSide']
            left,right=prefix(source,len(source['events'])-1),prefix(paired,len(paired['events'])-1)
            offered=views2(left,owner)[-1];assert offered['decision']['kind']=='custody'
            for index in (0,1):
                proposal=raw2(submission2(offered,index),'Submission2')
                for now in (None,0,10499,10500,10600,11000,40499,40500,40501,253402300799999):
                    for available in (False,True):
                        assert admit_a2(left,owner,proposal,now,available)==admit_a2(right,owner,proposal,now,available);clock_checks+=1
    # Same visible pre-resolution history across distinct private RNG coordinates.
    ordinary=next(s for s in sources if s['name']=='settlement-v2.ordinary.axis.attacker')
    other=next(s for s in sources if s['name']=='settlement-v2.zero-retreat.axis.attacker')
    b=len(ordinary['base']['predecessor']['inputs'])+len(ordinary['base']['roundInputs'])
    for side in SIDES: assert views2(prefix(ordinary,b+1),side)==views2(prefix(other,b+1),side);comparisons+=1
    return dict(equalHistoryPairs=comparisons,clockOutcomes=clock_checks)


def test_a2_codec_strictness():
    for kind,good,bad in (('Cp2',dict(numerator=2**63-1,denominator=1),dict(numerator=2**63,denominator=1)),
                          ('cohesion2',-2**31,-2**31-1),('cohesion2',10,11)):
        raw2(good,kind);rejected(lambda:raw2(bad,kind))
    for value in (True,False,0.0,-1): rejected(lambda:raw2(dict(numerator=value,denominator=1),'Cp2'))
    rejected(lambda:raw2(dict(numerator=1,denominator=2),'Cp2'))
    count=0
    source=source_cases2()[0]
    for side in SIDES:
        for view in views2(source,side):
            data=raw2(view,'Observation2');assert parse2(data,'Observation2')==view
            for bad in (b' '+data,data+b'\n',data.replace(b'"contractVersion":2',b'"contractVersion":2.0',1),
                        data.replace(b'"contractVersion":2',b'"contractVersion":true',1),
                        data.replace(b'"contractVersion":2',b'"contractVersion":2,"contractVersion":2',1),encode(dict(reversed(list(view.items()))))):
                rejected(lambda:parse2(bad,'Observation2'));count+=1
            rejected(lambda:parse(data,'Observation'));count+=1
            if view['decision']:
                for action in view['decision']['actions']:
                    candidate=action['candidate'];encoded=raw2(candidate,'Candidate2')
                    assert parse2(encoded,'Candidate2')==candidate
                    for mutate in (candidate|{'contractVersion':1},candidate|{'private':0},candidate|{'kind':'unknown'}): rejected(lambda:raw2(mutate,'Candidate2'));count+=1
                    missing=copy.deepcopy(candidate);missing.pop('contractVersion');rejected(lambda:raw2(missing,'Candidate2'));count+=1
    view=views2(source,'axis')[-1]
    for key,n in (('history',65),('ownReceipts',9)):
        bad=copy.deepcopy(view);bad[key]=[bad[key][0]]*n;rejected(lambda:raw2(bad,'Observation2'));count+=1
    rejected(lambda:parse2(b'['*21+b'0'+b']'*21,'Observation2'));count+=1
    rejected(lambda:parse2(b' '*65537,'Observation2'));count+=1
    return count


def test_a2_admission_matrix():
    counts=dict(cuts=0,actions=0,clockOutcomes=0,bindings=0,retries=0,fallbacks=0)
    for source in source_cases2():
        for side in SIDES:
            projected=views2(source,side)
            for cut,view in enumerate(projected):
                current=prefix(source,cut);data=raw2(view,'Observation2')
                assert read_observation2(data,current,side)==view;counts['cuts']+=1
                if not view['decision']: continue
                deadline=view['decision']['deadlineUnixMilliseconds'];opening=deadline-30000
                for index,action in enumerate(view['decision']['actions']):
                    proposal=submission2(view,index);encoded=raw2(proposal,'Submission2')
                    assert admit_a2(current,side,encoded,opening)['status']=='accepted';counts['actions']+=1
                    assert admit_current(current,side,encoded,opening)['status']=='rejected'
                    assert admit_a2(current,next(s for s in SIDES if s!=side),encoded,opening)['status']=='rejected'
                    for now in (None,0,opening-1,opening,opening+1,deadline-1,deadline,deadline+1,253402300799999):
                        for available in (False,True):
                            outcome=admit_a2(current,side,encoded,now,available)
                            assert outcome['status']==('accepted' if available and now is not None and opening<=now<deadline else 'rejected')
                            raw2(outcome,'Outcome2');counts['clockOutcomes']+=1
                    for field in ('campaignId','rulesRef','configRef','roundRef','settlementRef','decisionId','slotRef','openingRevision','actionSetId','actionId'):
                        bad=copy.deepcopy(proposal);bad[field]=bad[field]+1 if field=='openingRevision' else 'wrong' if field=='campaignId' else 'pub.'+'f'*64
                        assert admit_a2(current,side,raw2(bad,'Submission2'),opening)['status']=='rejected';counts['bindings']+=1
                    for field,value in proposal['candidate'].items():
                        bad=copy.deepcopy(proposal)
                        if type(value) is int: changed=value+1
                        elif type(value) is list: changed=list(reversed(value)) if len(value)>1 else ['foreign-location']
                        else: changed='pub.'+'f'*64 if field.endswith('Ref') else 'unrecognized'
                        bad['candidate'][field]=changed
                        assert admit_a2(current,side,encode(canonical2(bad,'Submission2')) if field!='kind' else encode(bad),opening)['status']=='rejected';counts['bindings']+=1
                    # Every later cut must recover only an actually accepted matching own receipt.
                    for later in range(cut+1,len(projected)):
                        if projected[later]['decision']==view['decision']: continue
                        accepted=any(r['decisionId']==proposal['decisionId'] and r['actionId']==proposal['actionId'] for r in projected[later]['ownReceipts'])
                        outcome=admit_a2(prefix(source,later),side,encoded,None,False)
                        assert outcome['status']==('accepted' if accepted else 'rejected');counts['retries']+=1
                    # Valid owner proposal may produce System fallback; it never earns public receipt.
                    if view['decision']['kind'] in ('force-assignment','retreat','custody'):
                        inp,after,event=attempt2(current,side,proposal,None,False)
                        assert json.loads(event)['author']=='system' and successful_choice2(inp,event,side) is None
                        assert admit_a2(current,side,encoded,None,False)['receipt'] is None;counts['fallbacks']+=1
    return counts


def test_a2_input_authentication():
    source=prefix(source_cases2()[0],2);count=0
    for mutate in ('name','family','base','float','bool','suffix','extra','order','event'):
        bad=copy.deepcopy(source)
        if mutate=='name':bad['name']='not-retained'
        elif mutate=='family':bad['family']='round-clock-v2'
        elif mutate=='base':bad['base']['committed']['clockConfigurationHash']='0'*64
        elif mutate in ('float','bool'):bad['inputs'][0]['admittedAt']=1000.0 if mutate=='float' else True
        elif mutate=='suffix':bad['inputs']=bad['inputs'][1:];bad['events']=bad['events'][1:]
        elif mutate=='extra':bad['private']=0
        elif mutate=='order':bad['events'].reverse()
        else:bad['events'][0]+=b'\n'
        try:views2(bad,'axis')
        except (Invalid,res2.Invalid,rnd2.Invalid):pass
        else:raise AssertionError('forged A2 source accepted: '+mutate)
        count+=1
    before=raw2(views2(source,'axis')[-1],'Observation2');v=views2(source,'axis');v[-1]['own']['currentToe']=0
    assert raw2(views2(source,'axis')[-1],'Observation2')==before
    # Self-consistent action list edits remain invalid against replayed offer.
    source=prefix(source_cases2()[0],1);view=views2(source,'axis')[-1]
    for actions in (view['decision']['actions'][::-1],[view['decision']['actions'][0]]*2):
        bad=copy.deepcopy(view);bad['decision']['actions']=actions
        d=bad['decision'];d['actionSetId']=public_ref2('set',dict(decisionId=d['decisionId'],openingRevision=d['openingRevision'],actions=actions),'SetSeed2')
        rejected(lambda:read_observation2(raw2(bad,'Observation2'),source,'axis'));count+=1
    return count


def test_a2_retained_fixture():
    fixture=json.loads(FIXTURE.read_bytes())
    assert 'successor2' in fixture, 'A2 retained vectors absent'
    retained=fixture['successor2'];assert len(retained['traces'])==96
    assert retained['resultClockPolicyId']==res2.POLICY
    for value in (True,0.0):
        mutant=copy.deepcopy(fixture);mutant['successor2']['traces'][0]['cuts'][0]['visibleRevision']=value
        rejected(lambda:verify_fixture((json.dumps(mutant,indent=2)+'\n').encode()))



def test_a2_fallback_receipts():
    sources=[s for s in source_cases2() if '.fallback-' in s['name']]
    assert len(sources)==8, 'authenticated fallback histories absent'
    for source in sources:
        before=prefix(source,len(source['events'])-1);owner=source['inputs'][-1]['actor']
        view=views2(before,owner)[-1];after=views2(source,owner)[-1]
        assert after['ownReceipts']==view['ownReceipts'], 'System fallback minted own receipt'
        for index in range(len(view['decision']['actions'])):
            proposal=raw2(submission2(view,index),'Submission2')
            assert admit_a2(source,owner,proposal,10600)['status']=='rejected', 'fallback ledger recovered as owner acceptance'
    return len(sources)



def test_a2_apparent_representation():
    trace=json.loads(res2.FIXTURE.read_bytes())['traces'][0]
    ctx=dict(base=json.loads(trace['baseCanonicalUtf8']),committed=json.loads(trace['committedCanonicalUtf8']))
    source=dict(base=ctx);state=json.loads(trace['stateCanonicalUtf8'])
    for side in SIDES:
        bad=copy.deepcopy(state)
        enemy=next(e for e in bad['world']['elements'] if e['elementId']!=side+'-assault-battalion')
        rep=next(r for r in bad['world']['representations'] if enemy['elementId'] in r['boundElementIds'])
        rep['currentLocationId']='changed-location'
        rejected(lambda:audience_facts2(source,'result',bad,side,'pub.'+'0'*64))
    return 2



def test_a2_literal_candidates():
    p='pub.'+'0'*64;c='pub.'+'1'*64
    values=[
        (dict(contractVersion=2,kind='full-close-assault',participantRef=p,componentRef=c,committedToe=10),
         '{"contractVersion":2,"kind":"full-close-assault","participantRef":"'+p+'","componentRef":"'+c+'","committedToe":10}'),
        (dict(contractVersion=2,kind='retreat',participantRef=p,route=['assault-east','commonwealth-rear'],distance=1,cpCost=1,excessCpDp=1),
         '{"contractVersion":2,"kind":"retreat","participantRef":"'+p+'","route":["assault-east","commonwealth-rear"],"distance":1,"cpCost":1,"excessCpDp":1}'),
        (dict(contractVersion=2,kind='refuse-retreat',participantRef=p),
         '{"contractVersion":2,"kind":"refuse-retreat","participantRef":"'+p+'"}'),
        (dict(contractVersion=2,kind='relocate-and-guard',custodyRef=p,donorComponentRef=c,route=['assault-east','assault-west'],guardToe=1),
         '{"contractVersion":2,"kind":"relocate-and-guard","custodyRef":"'+p+'","donorComponentRef":"'+c+'","route":["assault-east","assault-west"],"guardToe":1}'),
        (dict(contractVersion=2,kind='leave-unguarded',custodyRef=p),
         '{"contractVersion":2,"kind":"leave-unguarded","custodyRef":"'+p+'"}')]
    for value,expected in values: assert raw2(value,'Candidate2')==expected.encode()
    return len(values)



def test_a2_disclosure_boundary():
    probes=pairs=0;sources=source_cases2()
    forbidden=('"stateVersion"','"acceptedHighWater"','"resultId"','"settlementId"','"creationBinding"','"elementId"',
               '"componentId"','"lossDp"','"tablePercent"','"differential"','"draws"','"seed"','"prefix"',
               '"activationGate"','retained-unimplemented','replacement-training-gate')
    for source in sources:
        frames=replay_frames2(source)
        for side in SIDES:
            views_out=views2(source,side)
            for view in views_out:
                data=raw2(view,'Observation2').decode()
                assert not any(token in data for token in forbidden)
            family,state=frames[-1];modified=copy.deepcopy(state)
            enemy=next(e for e in modified['world']['elements'] if e['elementId']!=side+'-assault-battalion')
            enemy['components'][0]['currentToe']=9
            enemy['operationalState']['capabilityPointsExpended']['numerator']=123
            enemy['operationalState']['cohesionLevel']=-123
            enemy['ammunition']['points']=9
            modified['acceptedHighWater']=123456;modified['stateVersion']+=123;modified['prefix']='sha256:'+'0'*64
            # These are pure non-admission probes. No altered state is accepted as a source.
            left=audience_facts2(source,family,state,side,views_out[-1]['roundRef'])
            right=audience_facts2(source,family,modified,side,views_out[-1]['roundRef'])
            assert encode(left)==encode(right);probes+=1
        if source['name'].endswith('.attacker'):
            paired=next(s for s in sources if s['name']==source['name'][:-len('attacker')]+'defender')
            b=len(source['base']['predecessor']['inputs'])+len(source['base']['roundInputs'])
            for side in SIDES:
                assert views2(source,side)[b:]==views2(paired,side)[b:];pairs+=1
    return dict(nonAdmissionProbes=probes,authenticatedSealOrderPairs=pairs)


def fixture2():
    traces=[]
    for source in source_cases2():
        for audience in SIDES:
            cuts=[];goldens=[];candidates=[];seen=set()
            for cut,view in enumerate(views2(source,audience)):
                data=raw2(view,'Observation2');cuts.append(dict(cut=cut,bytes=len(data),sha256='sha256:'+hashlib.sha256(data).hexdigest(),visibleRevision=view['visibleRevision'],status=view['status']))
                category=(view['decision']['kind'] if view['decision'] else view['status'],bool(view['settlement']))
                if category not in seen or cut==len(source['events']):goldens.append(dict(cut=cut,canonicalJson=data.decode()));seen.add(category)
                if view['decision']:
                    for index,action in enumerate(view['decision']['actions']):
                        if not any(v['actionId']==action['actionId'] for v in candidates): candidates.append(dict(actionId=action['actionId'],candidateCanonicalJson=raw2(action['candidate'],'Candidate2').decode(),submissionCanonicalJson=raw2(submission2(view,index),'Submission2').decode()))
            traces.append(dict(source=source['name'],audience=audience,cuts=cuts,goldens=goldens,candidates=candidates))
    return dict(contractVersion=2,scope='004A2 synthetic C3a / Round2 / Result2; Reserve/cycle incomplete',
        assignmentClockPolicyId=rnd2.POLICY,resultClockPolicyId=res2.POLICY,
        sourcePins={name:'sha256:'+hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in ('combat-result-settlement-v2.schema.json','verify-combat-result-settlement-v2.py','fixtures/combat-result-settlement-v2.json')},traces=traces)


def main():
    require(FIXTURE.is_file())
    tests = (test_codec, test_selection, test_seals, test_submission, test_tight_bounds, test_source_pins, test_privacy_and_binding, test_explicit_submission_context, test_canonical_cycle_reference, test_round_continues_side_history, test_clock_loss_does_not_accept_choice, test_cycle_scalar_bounds, test_literal_candidate_bytes, test_canonical_edges, test_current_profile_boundary, test_current_rejects_legacy, test_corrected_clock_privacy, test_corrected_context_and_history, test_cancelled_seal_has_no_receipt, test_current_source_integrity, test_fixture_integrity, test_corrected_declassifier_probes, test_literal_corrected_configuration, test_a2_codec_boundary, test_a2_rejects_a1, test_a1_preserved, test_a2_source_and_handoff, test_a2_game_facts, test_a2_privacy_pairs, test_a2_codec_strictness, test_a2_admission_matrix, test_a2_input_authentication, test_a2_retained_fixture, test_a2_fallback_receipts, test_a2_apparent_representation, test_a2_literal_candidates, test_a2_disclosure_boundary)
    failures, results = [], {}
    for test in tests:
        try:
            results[test.__name__] = test()
        except AssertionError as error:
            failures.append(test.__name__)
            print('FAIL:', test.__name__, str(error))
    assert not failures, failures
    counts = verify_matrix()
    require(FIXTURE.is_file())
    verify_fixture(FIXTURE.read_bytes())
    print(f"PASS: 004A1 preserved; {results['test_corrected_clock_privacy']} corrected clock comparisons/retries;", counts)
    print(f"PASS: {len(tests)} total semantic groups; 004A2", {k:v for k,v in results.items() if k.startswith('test_a2_')})

if __name__ == '__main__':
    main()
