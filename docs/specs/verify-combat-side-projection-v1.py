#!/usr/bin/env python3
"""004A1 side projection oracle. Synthetic C3 lineage; no runtime activation."""
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


def replay_states(source):
    # Named retained transcripts authenticate this contract experiment. Production must use
    # independently authenticated Chronicle/input history, never caller-asserted actor strings.
    require(type(source) is dict and set(source) == {'name','family','base','inputs','events'})
    expected = next((x for x in source_cases() if x['name'] == source['name']), None)
    require(expected is not None and source['family'] == expected['family']
            and source['base'] == expected['base'] and type(source['events']) is list
            and type(source['inputs']) is list and len(source['events']) == len(source['inputs']))
    count = len(source['events'])
    require(source['events'] == expected['events'][:count]
            and source['inputs'] == expected['inputs'][:count] and count <= len(expected['events']))
    base = source['base']
    if source['family'] == 'steps':
        steps.boundary(steps.encode(base))
        state = steps.initial(base)
        reader = steps.read_event
    else:
        _, predecessor = rnd.start()
        rnd.read_base(rnd.encode(rnd.canonical(base, 'Base')), predecessor)
        state = rnd.initial(base)
        reader = rnd.read_event
    result = [state]
    for inp, event in zip(source['inputs'], source['events']):
        state = reader(event, base, state, inp)
        result.append(state)
    return result


def audience_facts(source, state, audience):
    """Internal declassifier; source/state already authenticated by replay_states."""
    typed(audience, 'side')
    base = source['base'] if source['family'] == 'steps' else source['base']['boundary']
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
    round_open = source['family'] == 'round' and state['status'] != 'unopened'
    if source['family'] == 'steps':
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
    states = replay_states(source)
    result, history, receipts = [], [], []
    revision, previous_facts, round_ref = 0, None, None
    if source['family'] == 'round':
        predecessor = next(x for x in source_cases() if x['name']=='accepted-decline')
        inherited = views(predecessor,audience)[-1]
        history, receipts = copy.deepcopy(inherited['history']), copy.deepcopy(inherited['ownReceipts'])
        revision = inherited['visibleRevision']
        previous_facts = audience_facts(source,states[0],audience)
    for index, state in enumerate(states):
        facts = audience_facts(source, state, audience)
        own_receipt = None
        if index:
            inp = source['inputs'][index-1]
            if inp['actor'] == audience and inp['command']['kind'] in ('choose-selection','decline-rba','seal-choice'):
                previous = result[-1]['decision']
                chosen = inp['command'].get('choice') or ('full-close-assault' if source['family']=='round' else 'decline-retreat-before-assault')
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
        public_cycle = dict(contractVersion=1, campaignId=context['campaignId'], rulesetHash=(source['base'] if source['family']=='steps' else source['base']['boundary'])['cycle']['rulesetHash'],
                            gameTurn=cycle['gameTurn'], operationStage=cycle['operationStage'],
                            playerPhaseSlot=cycle['playerPhaseSlot'], actingSide=cycle['phasingSide'], ordinal=cycle['ordinal'])
        boundary = source['base'] if source['family']=='steps' else source['base']['boundary']
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


def submit(source, audience, data, now=5000):
    rejected_outcome = dict(contractVersion=1, status='rejected', receipt=None)
    try:
        typed(audience, 'side')  # Authenticated seat precedes attacker-controlled reference lookup.
        proposal = parse(data, 'Submission')
        require(proposal['audience'] == audience)
        projected = views(source, audience)
        # Exact historical action equality supports receipt recovery; never rebind a consumed ID.
        offered_frames = projected
        if source['family'] == 'round':
            predecessor = next(x for x in source_cases() if x['name']=='accepted-decline')
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
        if source['family'] == 'round':
            role = 'attacker' if audience == base['boundary']['cycle']['actingSide'] else 'defender'
            command = rnd.command(base, state, 'seal-choice', role)
            inp = rnd.trusted(command, audience, now)
            _, event, _ = rnd.transition(base, state, inp)
        else:
            if kind in ('select-close-assault','finish-without-attack'):
                command = steps.command(state, 'choose-selection', decisionId=state['selectionWindow']['decisionId'],
                                        choice=kind, candidate=steps.candidate(base) if kind=='select-close-assault' else None)
            else:
                command = steps.command(state, 'decline-rba', decisionId=state['rbaWindow']['decisionId'],
                                        participant=state['selection']['defender']['unit'])
            inp = steps.trusted(command, audience, now)
            _, event, _ = steps.transition(base, state, inp)
        expected_effect = 'choice-sealed' if source['family']=='round' else ('rba-declined' if kind=='decline-retreat-before-assault' else 'selection-closed')
        require(event is not None and json.loads(event)['effect']['kind'] == expected_effect)
        seed = dict(decisionId=proposal['decisionId'], actionId=proposal['actionId'])
        return dict(contractVersion=1, status='accepted', receipt=seed | dict(receiptRef=public_ref('receipt', seed, 'ReceiptSeed')))
    except (Invalid, steps.Invalid, rnd.Invalid, StopIteration, KeyError, TypeError, ValueError):
        return rejected_outcome


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
        require(inputs == case['inputs'] and events == [x['canonicalUtf8'].encode() for x in case['events']]
                and rnd.encode(rnd.canonical(final, 'RoundState')) == case['stateGolden']['canonicalUtf8'].encode())
        result.append(dict(name=case['name'], family='round', base=base, inputs=inputs, events=events))
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
    for stem in ('combat-selection-steps-v1','combat-sealed-round-v1','combat-authority-composition-v1','combat-cycle-sequence-v1'):
        paths.extend([f'docs/specs/{stem}.schema.json', f'docs/specs/fixtures/{stem}.json', f'docs/specs/verify-{stem}.py'])
    paths.append('docs/specs/combat-authority-composition-v1.md')
    for stem in ('combat-opportunity-identity-v1','combat-sealed-decision-protocol-v1',
                 'combat-step-transitions-v1','combat-cost-resolution-order-v1',
                 'combat-settlement-disclosure-v1','continual-cycle-reserve-composition-v1',
                 'combat-cycle-policy-reconciliation'):
        paths.append(f'docs/design/{stem}.md')
    return [dict(path=path, sha256='sha256:'+hashlib.sha256((ROOT.parent.parent/path).read_bytes()).hexdigest()) for path in paths]


def test_source_pins():
    assert len(source_pins()) == 20, 'predecessor/design/handoff sources unpinned'


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
                        outcome = submit(current,audience,encoded,deadline-1)
                        assert outcome['status']=='accepted', (source['name'],audience,cut)
                        raw(outcome,'Outcome')
                        assert submit(current,audience,encoded,deadline)['status']=='rejected'
                        assert submit(current, 'commonwealth' if audience=='axis' else 'axis',encoded)['status']=='rejected'
                        counts['submissions'] += 3
                        for field in ('decisionId','actionSetId','actionId','rulesRef','configRef','roundRef','slotRef'):
                            bad = proposal | {field:'pub.'+'e'*64}
                            assert submit(current,audience,raw(bad,'Submission'))['status']=='rejected'
                            counts['mutations'] += 1
                        bad = proposal | dict(campaignId='foreign-campaign')
                        assert submit(current,audience,raw(bad,'Submission'))['status']=='rejected'
                        counts['mutations'] += 1
                        bad = proposal | dict(openingRevision=proposal['openingRevision']+1)
                        assert submit(current,audience,raw(bad,'Submission'))['status']=='rejected'
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
                            outcome = submit(source,audience,raw(proposal,'Submission'),999999)
                            assert outcome['status'] == ('accepted' if receipt['actionId']==proposal['actionId'] else 'rejected')
                            counts['bindings'] += 1
                        elif projected[-1]['decision'] is None:
                            assert submit(source,audience,raw(proposal,'Submission'))['status']=='rejected'
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
                sourcePins=source_pins(),traces=traces)


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


def main():
    tests = (test_codec, test_selection, test_seals, test_submission, test_tight_bounds, test_source_pins, test_privacy_and_binding, test_explicit_submission_context, test_canonical_cycle_reference, test_round_continues_side_history, test_clock_loss_does_not_accept_choice, test_cycle_scalar_bounds, test_literal_candidate_bytes, test_canonical_edges)
    failures = []
    for test in tests:
        try:
            test()
        except AssertionError as error:
            failures.append(test.__name__)
            print('FAIL:', test.__name__, str(error))
    assert not failures, failures
    counts = verify_matrix()
    require(FIXTURE.is_file())
    require(FIXTURE.read_text() == json.dumps(generated_fixture(), indent=2)+'\n')
    print('PASS: 004A1 semantic contract tests;', counts)

if __name__ == '__main__':
    main()
