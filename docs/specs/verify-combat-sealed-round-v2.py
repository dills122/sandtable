#!/usr/bin/env python3
"""Bounded sealed-round v2 oracle; immutable historical v1 predecessor reader."""
from __future__ import annotations
import copy
import hashlib
import importlib.util
import json
import sys
from functools import lru_cache
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
FIXTURE = ROOT / 'fixtures/combat-sealed-round-v2.json'
spec = importlib.util.spec_from_file_location('historical_round', ROOT / 'verify-combat-sealed-round-v1.py')
legacy = importlib.util.module_from_spec(spec)
spec.loader.exec_module(legacy)
steps = legacy.steps
encode, sha = legacy.encode, legacy.sha
POLICY = 'sandtable.combat.public-opening-clock.v2'


def legacy_base(base):
    return dict(contractVersion=1, boundary=base['boundary'], steps=base['steps'])


def start(side='axis'):
    old_base, old_case = legacy.start()
    boundary = copy.deepcopy(old_base['boundary'])
    boundary['cycle']['actingSide'] = side
    boundary['firstActingSide'] = side
    boundary['position']['activeSide'] = side
    state = steps.initial(steps.boundary(encode(boundary)))
    inputs, events = [], []
    for original in old_case['inputs']:
        inp = copy.deepcopy(original)
        command = inp['command']
        command['segmentId'] = state['segmentId']
        if command['expectedPriorVersion'] is not None:
            command['expectedPriorVersion'] = state['stateVersion']
        if command['decisionId'] is not None:
            command['decisionId'] = state['segmentId'] + '.' + command['decisionId'].rsplit('.',1)[1]
        if command['kind'] == 'choose-selection':
            command['candidate'] = steps.candidate(boundary)
            inp['actor'] = side
        if command['kind'] == 'decline-rba':
            command['participant'] = steps.candidate(boundary)['defender']['unit']
            inp['actor'] = command['participant']['originalSide']
        state, event, _ = steps.transition(boundary, state, inp)
        inputs.append(inp)
        events.append(dict(canonicalUtf8=event.decode()))
    predecessor = dict(boundary=boundary, inputs=inputs, events=events)
    base = dict(contractVersion=2, boundary=boundary, steps=state,
                clockConfiguration=dict(contractVersion=2, parentConfigurationHash=boundary['cycle']['admittedPolicyBundleDigest'],
                    timingPolicyId=POLICY, decisionBudgetMilliseconds=30000))
    return base, predecessor


INVENTORY = json.loads((ROOT / 'combat-sealed-round-v2.schema.json').read_text())
SCHEMA = steps.SCHEMA | {k:[tuple(f.split(':')) for f in v.split()] for k,v in INVENTORY['objects'].items()}


class Invalid(ValueError):
    def __init__(self, code, path=''):
        self.code, self.path = f'CMB-RND2-{code:03}', path
        super().__init__(self.code + (' '+path if path else ''))


def require(ok, code, path=''):
    if not ok:
        raise Invalid(code,path)


def typed(value, kind, depth=0):
    require(depth<=32,1)
    if kind.endswith('?'):
        if value is not None:
            typed(value,kind[:-1],depth)
    elif kind=='RoundEffect':
        require(type(value) is dict and type(value.get('kind')) is str
                and value['kind'] in INVENTORY['effectTags'],3)
        typed(value,INVENTORY['effectTags'][value['kind']],depth)
    elif kind in SCHEMA:
        require(type(value) is dict and set(value)=={k for k,_ in SCHEMA[kind]},1)
        for key,child in SCHEMA[kind]:
            typed(value[key],child,depth+1)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value)<=512,1)
        for child in value:
            typed(child,kind[:-2],depth+1)
    elif kind=='role':
        require(type(value) is str and value in ('attacker','defender'),2)
    else:
        try:
            steps.typed(value,kind,depth=depth)
        except steps.Invalid as error:
            raise Invalid(int(error.code[-3:]),error.path) from error


def canonical(value, kind):
    if kind.endswith('?'):
        return None if value is None else canonical(value,kind[:-1])
    if kind=='RoundEffect':
        return canonical(value,INVENTORY['effectTags'][value['kind']])
    if kind in SCHEMA:
        return {key:canonical(value[key],child) for key,child in SCHEMA[kind]}
    if kind.endswith('[]'):
        return [canonical(child,kind[:-2]) for child in value]
    return value


def raw(value, kind):
    typed(value,kind)
    data = encode(canonical(value,kind))
    require(len(data)<=1048576,1)
    return data


def parse(data, kind):
    require(type(data) is bytes and 0<len(data)<=1048576,1)
    def pairs(items):
        result = {}
        for key,value in items:
            require(key not in result,1)
            result[key] = value
        return result
    try:
        value = json.loads(data.decode('ascii'),object_pairs_hook=pairs,
                           parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError,UnicodeError,RecursionError) as error:
        raise Invalid(1) from error
    require(raw(value,kind)==data,8)
    return value


def digest(kind, value):
    return steps.digest(INVENTORY['domains'][kind],encode(value))


def base_hash(base):
    return 'sha256:'+digest('base',canonical(base,'Base'))


def configuration_hash(base):
    return 'sha256:'+digest('configuration',canonical(base['clockConfiguration'],'ClockConfiguration'))


@lru_cache(maxsize=1)
def predecessor_clock_policy():
    context = steps.env.Context()
    budget = next(x['decisionBudgetMilliseconds'] for x in context.config['windows'] if x['kind']=='force-assignment')
    return context.config_hash,budget


def clock_configuration(base):
    parent_hash,budget = predecessor_clock_policy()
    return dict(contractVersion=2,parentConfigurationHash=parent_hash,
                timingPolicyId=POLICY,decisionBudgetMilliseconds=budget)


def validate_base(base):
    typed(base,'Base')
    require(base['contractVersion']==2 and base['clockConfiguration']==clock_configuration(base),4)
    require(base['clockConfiguration']['parentConfigurationHash']==predecessor_clock_policy()[0],4)


def read_base(data, predecessor):
    base = parse(data,'Base')
    try:
        legacy.read_base(legacy.encode(legacy_base(base)),predecessor)
    except (ValueError,KeyError,TypeError,IndexError) as error:
        raise Invalid(4,'/predecessor') from error
    validate_base(base)
    return base


def initial(base):
    validate_base(base)
    control, boundary = base['steps'], base['boundary']
    return dict(contractVersion=2,baseHash=base_hash(base),clockConfigurationHash=configuration_hash(base),
                opportunityId=None,roundId=None,stateVersion=control['stateVersion'],prefix=control['prefix'],
                stepIndex=3,status='unopened',openingReceiptId=None,timing=None,slots=[],
                stepReceipts=copy.deepcopy(control['stepReceipts']),world=copy.deepcopy(boundary['world']),
                randomState=copy.deepcopy(boundary['randomState']),attackHistory=[],targetUses=[],commitmentId=None,
                cancellationReceiptId=None,receipts=[],closed=False)


def command(base, state, kind, role=None):
    slot = next((s for s in state['slots'] if s['role']==role),None)
    return dict(contractVersion=2,kind=kind,clockConfigurationHash=configuration_hash(base),
                segmentId=base['steps']['segmentId'],roundId=state['roundId'],slotId=slot['slotId'] if slot else None,
                expectedPriorVersion=state['stateVersion'] if kind in ('open-round','complete-step','commit-attack') else None,
                allocation=copy.deepcopy(slot['allocation']) if slot else None)


trusted = legacy.trusted


def validate_timing(base, timing):
    typed(timing,'ClockTiming')
    require(timing['contractVersion']==2 and timing['clockConfigurationHash']==configuration_hash(base)
            and timing['kind']=='force-assignment'
            and timing['decisionBudgetMilliseconds']==base['clockConfiguration']['decisionBudgetMilliseconds']
            and timing['openingFloorUnixMilliseconds']==timing['openedAtUnixMilliseconds']
            and timing['deadlineUnixMilliseconds']==timing['openedAtUnixMilliseconds']+timing['decisionBudgetMilliseconds'],4)


def clock_gate(timing, now, available):
    if not available or now is None or now<timing['openingFloorUnixMilliseconds']:
        return 'unavailable'
    return 'expired' if now>=timing['deadlineUnixMilliseconds'] else 'before-deadline'


def transition(base, prior, inp, admission_enabled=True):
    """Internally derived prior only; persisted input enters through authenticated replay."""
    validate_base(base)
    typed(prior,'RoundState')
    typed(inp,'RoundInput')
    require(type(admission_enabled) is bool,2)
    cmd, actor = inp['command'], inp['actor']
    kind, boundary = cmd['kind'], base['boundary']
    require(cmd['contractVersion']==prior['contractVersion']==2
            and kind in ('open-round','seal-choice','expire-round','controller-unavailable','complete-step','commit-attack'),3)
    require(cmd['segmentId']==base['steps']['segmentId'] and prior['baseHash']==base_hash(base)
            and cmd['clockConfigurationHash']==prior['clockConfigurationHash']==configuration_hash(base),4)
    require(actor in ('axis','commonwealth') if kind=='seal-choice' else actor=='system',4)
    require(cmd['allocation'] is not None and cmd['slotId'] is not None if kind=='seal-choice'
            else cmd['allocation'] is None and cmd['slotId'] is None,3)
    require((cmd['expectedPriorVersion'] is not None)==(kind in ('open-round','complete-step','commit-attack')),3)
    require((cmd['roundId'] is None)==(kind=='open-round'),3)
    command_hash = sha(raw(cmd,'RoundCommand'))
    duplicate = next((r for r in prior['receipts'] if r['commandHash']==command_hash),None)
    if duplicate:
        require(duplicate['actor']==actor,4)
        return copy.deepcopy(prior),None,duplicate['receiptId']
    if kind in ('expire-round','controller-unavailable') and (cmd['roundId']!=prior['roundId'] or prior['status']!='collecting'):
        return copy.deepcopy(prior),None,None
    require(not prior['closed'] and prior['status']!='committed',7)
    require(prior['world']==boundary['world'] and prior['randomState']==boundary['randomState']
            and not prior['attackHistory'] and not prior['targetUses'],4)
    if kind!='open-round':
        require(cmd['roundId']==prior['roundId'],4)
        validate_timing(base,prior['timing'])
    if cmd['expectedPriorVersion'] is not None:
        require(cmd['expectedPriorVersion']==prior['stateVersion'],6)
    require(prior['stateVersion']<2**63-1 and len(prior['receipts'])<16,6)
    state, author, now = copy.deepcopy(prior), actor, inp['admittedAt']
    if kind=='open-round':
        require(admission_enabled,7)
        require(state['status']=='unopened' and state['stepIndex']==3 and not state['receipts'],6)
        require(inp['clockAvailable'] and now is not None,5)
        deadline = now+base['clockConfiguration']['decisionBudgetMilliseconds']
        require(deadline<=253402300799999,5)
        timing = dict(contractVersion=2,clockConfigurationHash=configuration_hash(base),kind='force-assignment',
                      decisionBudgetMilliseconds=base['clockConfiguration']['decisionBudgetMilliseconds'],
                      openedAtUnixMilliseconds=now,deadlineUnixMilliseconds=deadline,openingFloorUnixMilliseconds=now)
        validate_timing(base,timing)
        selection = base['steps']['selection']
        opportunity = dict(baseHash=state['baseHash'],cycleId=legacy.cycle_id(legacy_base(base)),
                           positionId=steps.edge(boundary)['combatPositionIds'][3],candidate=selection,
                           declineReceiptId=base['steps']['declineReceiptId'])
        state['opportunityId'] = 'opp.'+digest('opportunity',opportunity)
        opening = dict(baseHash=state['baseHash'],opportunityId=state['opportunityId'],
                       openingAuthorityVersion=state['stateVersion'],openingHistoryPrefix=state['prefix'],timing=timing)
        state['roundId'] = 'rnd.'+digest('round',opening)
        for role in ('attacker','defender'):
            participant = selection[role]
            state['slots'].append(dict(role=role,owner=participant['unit']['originalSide'],
                slotId='slt.'+digest('slot',dict(roundId=state['roundId'],role=role)),
                allocation=dict(kind='full-close-assault',unit=copy.deepcopy(participant['unit']),
                                componentId=participant['componentIds'][0],committedToe=10),
                sealedReceiptId=None,sealedAt=None))
        state['status'], state['timing'] = 'collecting', timing
        effect = dict(kind='round-opened',baseHash=state['baseHash'],opportunityId=state['opportunityId'],
                      timing=copy.deepcopy(timing),slots=copy.deepcopy(state['slots']))
    elif kind=='seal-choice':
        require(state['status']=='collecting' and state['stepIndex']==3,6)
        slot = next((s for s in state['slots'] if s['slotId']==cmd['slotId']),None)
        require(slot is not None and slot['owner']==actor,4)
        require(slot['sealedReceiptId'] is None,6)
        require(cmd['allocation']==slot['allocation'],4)
        gate = clock_gate(state['timing'],now,inp['clockAvailable'])
        require(gate!='expired',5)
        if gate=='unavailable':
            effect = dict(kind='round-cancelled',cause='clock-unavailable',timing=copy.deepcopy(state['timing']))
            author = 'system'
        else:
            effect = dict(kind='choice-sealed',slotId=slot['slotId'],allocation=copy.deepcopy(slot['allocation']),
                          timing=copy.deepcopy(state['timing']),prepared=any(s['sealedReceiptId'] is not None for s in state['slots']))
    elif kind in ('expire-round','controller-unavailable'):
        gate = clock_gate(state['timing'],now,inp['clockAvailable'])
        if kind=='expire-round' and gate=='before-deadline':
            return copy.deepcopy(prior),None,None
        cause = 'controller-unavailable' if kind=='controller-unavailable' else ('deadline' if gate=='expired' else 'clock-unavailable')
        effect = dict(kind='round-cancelled',cause=cause,timing=copy.deepcopy(state['timing']))
    elif kind=='complete-step':
        require(now is None and inp['clockAvailable'],5)
        require(state['status'] in ('prepared','cancelled') and 3<=state['stepIndex']<=5,6)
        require(state['stepIndex']<5 or state['status']=='cancelled',7)
        positions, index = steps.edge(boundary)['combatPositionIds'], state['stepIndex']
        proofs = [state['cancellationReceiptId']] if state['status']=='cancelled' else [s['sealedReceiptId'] for s in state['slots']]
        require(all(p is not None for p in proofs),6)
        effect = dict(kind='step-completed',fromPositionId=positions[index],
                      toPositionId=positions[index+1] if index<5 else steps.edge(boundary)['releasePositionId'],
                      previousStepReceiptId=state['stepReceipts'][-1],proofReceipts=proofs)
    else:
        require(now is None and inp['clockAvailable'],5)
        require(state['status']=='prepared' and state['stepIndex']==5 and len(state['stepReceipts'])==5,6)
        require(all(s['sealedReceiptId'] is not None for s in state['slots']),6)
        allocations = [copy.deepcopy(s['allocation']) for s in state['slots']]
        commitment = 'cmt.'+digest('commitment',dict(roundId=state['roundId'],priorVersion=state['stateVersion'],
                                                   priorPrefix=state['prefix'],allocations=allocations))
        costs = []
        for slot,cost in zip(state['slots'],(5,3)):
            element = next(e for e in state['world']['elements'] if e['elementId']==slot['allocation']['unit']['elementId'])
            spent = element['operationalState']['capabilityPointsExpended']['numerator']
            require(element['ammunition']['points']==10 and spent+cost<=10,4)
            costs.append(dict(unit=slot['allocation']['unit'],beforeCp=spent,afterCp=spent+cost,beforeAmmo=10,afterAmmo=0))
        effect = dict(kind='attack-committed',commitmentId=commitment,allocations=allocations,costs=costs,
                      preResultRandomState=copy.deepcopy(state['randomState']))
    event = dict(contractVersion=2,eventType=INVENTORY['eventTypes'][effect['kind']],author=author,
                 campaignId=boundary['cycle']['campaignId'],rulesetHash=boundary['cycle']['rulesetHash'],
                 configurationHash=configuration_hash(base),predecessorConfigurationHash=base['clockConfiguration']['parentConfigurationHash'],
                 cycleId=legacy.cycle_id(legacy_base(base)),segmentId=cmd['segmentId'],roundId=state['roundId'],
                 priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],
                 input=canonical(inp,'RoundInput'),effect=canonical(effect,'RoundEffect'))
    receipt = 'cmb.'+digest('receipt',event)
    event['receiptId'] = receipt
    data = raw(event,'RoundEvent')
    tag = effect['kind']
    if tag=='round-opened':
        state['openingReceiptId'] = receipt
    elif tag=='choice-sealed':
        slot = next(s for s in state['slots'] if s['slotId']==effect['slotId'])
        slot['sealedReceiptId'], slot['sealedAt'] = receipt, now
        state['status'] = 'prepared' if effect['prepared'] else 'collecting'
    elif tag=='round-cancelled':
        state['status'], state['cancellationReceiptId'] = 'cancelled', receipt
    elif tag=='step-completed':
        state['stepReceipts'].append(receipt)
        state['stepIndex'] += 1
        state['closed'] = state['stepIndex']==6
    else:
        state['status'], state['commitmentId'] = 'committed', effect['commitmentId']
        for cost in effect['costs']:
            element = next(e for e in state['world']['elements'] if e['elementId']==cost['unit']['elementId'])
            element['operationalState']['capabilityPointsExpended']['numerator'] = cost['afterCp']
            element['ammunition']['points'] = 0
        selection = base['steps']['selection']
        state['attackHistory'].append(dict(commitmentId=effect['commitmentId'],cycleId=event['cycleId'],segmentId=cmd['segmentId'],
            attacker=copy.deepcopy(selection['attacker']['unit']),defender=copy.deepcopy(selection['defender']['unit']),
            targetLocationId=selection['targetLocationId'],gameTurn=boundary['cycle']['gameTurn'],operationStage=boundary['cycle']['operationStage']))
        state['targetUses'].append(dict(commitmentId=effect['commitmentId'],segmentId=cmd['segmentId'],targetLocationId=selection['targetLocationId']))
    state['stateVersion'], state['prefix'] = event['stateVersion'], steps.seq.prefix_event(prior['prefix'],data)
    state['receipts'].append(dict(commandHash=command_hash,eventHash=sha(data),receiptId=receipt,actor=actor,stateVersion=state['stateVersion']))
    raw(state,'RoundState')
    return state,data,receipt


def read_event(data, base, prior, inp):
    parse(data,'RoundEvent')
    state, expected, _ = transition(base,prior,inp)
    require(expected is not None and expected==data,6)
    return state


def replay(base, inputs, events, length=None):
    require(type(inputs) is list and type(events) is list and len(inputs)==len(events)<=16,1)
    require(length is None or type(length) is int and 0<=length<=len(events),2)
    state = initial(base)
    for inp, data in zip(inputs[:length],events[:length]):
        state = read_event(data,base,state,inp)
    return state


def read_state(data, base, inputs, events, length=None):
    parse(data,'RoundState')
    state = replay(base,inputs,events,length)
    require(data==raw(state,'RoundState'),6)
    return state


def opened(side='axis', now=3000):
    base, predecessor = start(side)
    state = initial(base)
    inp = trusted(command(base,state,'open-round'), now=now)
    after, event, _ = transition(base,state,inp)
    return base, predecessor, after, inp, event


def effect_outcome(call):
    try:
        state, event, receipt = call()
    except ValueError:
        return 'rejected'
    if event is None:
        return 'accepted' if receipt and any(s['sealedReceiptId']==receipt for s in state['slots']) else 'no-op'
    return 'accepted' if json.loads(event)['effect']['kind']=='choice-sealed' else 'cancelled'


def test_equal_live_slot_clock_outcome():
    for side in ('axis','commonwealth'):
        for first, waiting in (('attacker','defender'),('defender','attacker')):
            base, _, empty, _, _ = opened(side)
            first_input = trusted(command(base,empty,'seal-choice',first),base['steps']['selection'][first]['unit']['originalSide'],4000)
            one, _, _ = transition(base,empty,first_input)
            owner = base['steps']['selection'][waiting]['unit']['originalSide']
            proposal = trusted(command(base,empty,'seal-choice',waiting),owner,3500)
            before = effect_outcome(lambda: transition(base,empty,proposal))
            after = effect_outcome(lambda: transition(base,one,proposal))
            assert before == after == 'accepted', (side,first,before,after,'private seal raised clock floor')


def test_successor_identity():
    base, _, state, inp, event = opened()
    assert state['contractVersion']==inp['command']['contractVersion']==json.loads(event)['contractVersion']==2, 'v2 authority lacks distinct versions'


def test_independent_opening():
    base, _ = start()
    state = initial(base)
    inp = trusted(command(base,state,'open-round'), now=1999)
    try:
        after, _, _ = transition(base,state,inp)
    except ValueError:
        after = None
    assert after is not None, 'private accepted RBA time gates new public opening'


def test_disabled_admission():
    base, _ = start()
    state = initial(base)
    try:
        transition(base,state,trusted(command(base,state,'open-round'),now=3000),admission_enabled=False)
    except ValueError:
        return
    raise AssertionError('disabled admission opened a new round')


def test_old_reader_rejection():
    _, _, _, _, event = opened()
    try:
        legacy.parse(event,'RoundEvent')
    except ValueError:
        return
    raise AssertionError('v2 event accepted by historical v1 reader')


def test_authenticated_readback():
    base, predecessor, state, inp, event = opened()
    checked = read_base(raw(base,'Base'),predecessor)
    assert read_event(event,checked,initial(checked),inp)==state
    assert read_state(raw(state,'RoundState'),checked,[inp],[event])==state
    damaged = copy.deepcopy(state)
    damaged['timing']['openingFloorUnixMilliseconds'] += 1
    rejected(lambda: read_state(raw(damaged,'RoundState'),checked,[inp],[event]))


def rejected(call):
    try:
        call()
    except ValueError:
        return
    raise AssertionError('invalid successor accepted')


# This witness is a test declassifier, not a transport or CON005 allocation.
def live_owner_witness(state, role):
    own = next(slot for slot in state['slots'] if slot['role']==role)
    timing = state['timing']
    return dict(owner=own['owner'],role=role,allocation=own['allocation'],
                sealed=own['sealedReceiptId'] is not None,
                opening=timing['openedAtUnixMilliseconds'],deadline=timing['deadlineUnixMilliseconds'])


CLOCKS = [(now,available) for now in (None,0,2999,3000,3500,3999,4000,4001,32999,33000,33001,253402300799999)
          for available in (True,False)]


def clock_expected(now, available):
    # Literal policy oracle, independent of clock_gate implementation.
    if not available or now is None or now<3000:
        return 'cancelled'
    return 'accepted' if now<33000 else 'rejected'


def test_full_clock_matrix():
    count = 0
    for side in ('axis','commonwealth'):
        for first, waiting in (('attacker','defender'),('defender','attacker')):
            base, _, empty, _, _ = opened(side)
            first_input = trusted(command(base,empty,'seal-choice',first),
                                  base['steps']['selection'][first]['unit']['originalSide'],4000)
            one, _, _ = transition(base,empty,first_input)
            assert live_owner_witness(empty,waiting)==live_owner_witness(one,waiting)
            for now, available in CLOCKS:
                proposal = trusted(command(base,empty,'seal-choice',waiting),
                                   base['steps']['selection'][waiting]['unit']['originalSide'],now,available)
                for prior in (empty,one):
                    assert effect_outcome(lambda: transition(base,prior,proposal))==clock_expected(now,available), (side,first,now,available)
                    count += 1
                retry = copy.deepcopy(first_input)
                retry.update(admittedAt=now,clockAvailable=available)
                after,event,receipt = transition(base,one,retry)
                assert after==one and event is None and receipt==one['slots'][0 if first=='attacker' else 1]['sealedReceiptId']
                count += 1
            late_regression = trusted(command(base,one,'seal-choice',waiting),
                                     base['steps']['selection'][waiting]['unit']['originalSide'],3500)
            prepared, _, _ = transition(base,one,late_regression)
            assert prepared['timing']==empty['timing']
            assert sorted(s['sealedAt'] for s in prepared['slots'])==[3500,4000]
    return count


def test_invalid_before_clock():
    base, _, empty, _, _ = opened()
    proposal = trusted(command(base,empty,'seal-choice','attacker'),'axis',None,False)
    mutants = []
    for key,value in (('segmentId','foreign'),('roundId','foreign'),('slotId','foreign'),
                      ('clockConfigurationHash','sha256:'+'0'*64),('contractVersion',1)):
        bad = copy.deepcopy(proposal); bad['command'][key] = value; mutants.append(bad)
    bad = copy.deepcopy(proposal); bad['actor']='commonwealth'; mutants.append(bad)
    bad = copy.deepcopy(proposal); bad['command']['allocation']['committedToe']=9; mutants.append(bad)
    for value in (-1,True,3.5,'3500',253402300800000):
        bad = copy.deepcopy(proposal); bad['admittedAt']=value; mutants.append(bad)
    for value in (None,0,'false'):
        bad = copy.deepcopy(proposal); bad['clockAvailable']=value; mutants.append(bad)
    for bad in mutants:
        rejected(lambda: transition(base,empty,bad))
    valid = copy.deepcopy(proposal); valid.update(admittedAt=4000,clockAvailable=True)
    one, _, _ = transition(base,empty,valid)
    for bad in mutants:
        rejected(lambda: transition(base,one,bad))
    return len(mutants)*2


def trace(side, first, terminal):
    base, predecessor = start(side)
    state, inputs, events = initial(base), [], []
    def apply(kind, role=None, now=None, available=True):
        nonlocal state
        actor = base['steps']['selection'][role]['unit']['originalSide'] if role else 'system'
        inp = trusted(command(base,state,kind,role),actor,now,available)
        state,event,_ = transition(base,state,inp)
        assert event is not None
        inputs.append(inp); events.append(event)
    apply('open-round',now=3000)
    if first:
        apply('seal-choice',first,4000)
    if terminal=='committed':
        apply('seal-choice','defender' if first=='attacker' else 'attacker',3500)
    elif terminal=='fault':
        apply('seal-choice','defender' if first=='attacker' else 'attacker',None,False)
    else:
        apply('expire-round',now=33000)
    apply('complete-step'); apply('complete-step')
    apply('commit-attack' if terminal=='committed' else 'complete-step')
    return base,predecessor,inputs,events,state


def test_expiry_recovery():
    count = 0
    for side in ('axis','commonwealth'):
        for first in ('attacker','defender'):
            base,pred,inputs,events,committed = trace(side,first,'committed')
            for cut in range(2,len(events)+1):
                state = replay(base,inputs,events,cut)
                for now,available in CLOCKS:
                    retry = copy.deepcopy(inputs[1]); retry.update(admittedAt=now,clockAvailable=available)
                    after,event,receipt = transition(base,state,retry,admission_enabled=False)
                    assert after==state and event is None and receipt==json.loads(events[1])['receiptId']
                    count += 1
                if state['status']=='prepared':
                    for kind in ('expire-round','controller-unavailable'):
                        after,event,receipt = transition(base,state,trusted(command(base,state,kind),now=None,available=False))
                        assert after==state and event is None and receipt is None
                    recovered = state
                    for inp in inputs[cut:]:
                        recovered,_,_ = transition(base,recovered,inp,admission_enabled=False)
                    assert recovered==committed
            base,pred,inputs,events,closed = trace(side,first,'expired')
            one = replay(base,inputs,events,2)
            before = trusted(command(base,one,'expire-round'),now=32999)
            assert transition(base,one,before)==(one,None,None)
            stale = trusted(command(base,one,'expire-round'),now=None,available=False)
            stale['command']['roundId']='foreign'
            assert transition(base,one,stale)==(one,None,None)
            cancelled = replay(base,inputs,events,3)
            for kind in ('expire-round','controller-unavailable'):
                assert transition(base,cancelled,trusted(command(base,cancelled,kind),now=None,available=False))[1] is None
            # Retained seals recover after cancellation and full empty/partial closure.
            for cut in range(3,len(events)+1):
                state = replay(base,inputs,events,cut)
                retry = copy.deepcopy(inputs[1]); retry.update(admittedAt=None,clockAvailable=False)
                assert transition(base,state,retry)[1] is None
                waiting = 'defender' if first=='attacker' else 'attacker'
                proposal = trusted(command(base,state,'seal-choice',waiting),base['steps']['selection'][waiting]['unit']['originalSide'],3500)
                rejected(lambda: transition(base,state,proposal))
    base,pred,inputs,events,closed = trace('axis',None,'expired')
    for cut in range(2,len(events)+1):
        state = replay(base,inputs,events,cut)
        for role in ('attacker','defender'):
            proposal = trusted(command(base,state,'seal-choice',role),base['steps']['selection'][role]['unit']['originalSide'],3500)
            rejected(lambda: transition(base,state,proposal))
    return count


def test_replay_tamper():
    cuts = mutations = raw_rejections = 0
    for side in ('axis','commonwealth'):
        for first,terminal in (('attacker','committed'),('defender','committed'),
                               (None,'expired'),('attacker','expired'),('defender','fault')):
            base,pred,inputs,events,final = trace(side,first,terminal)
            checked = read_base(raw(base,'Base'),pred)
            rejected(lambda: legacy.parse(raw(base,'Base'),'Base'))
            for cut in range(len(events)+1):
                state = replay(checked,inputs,events,cut)
                data = raw(state,'RoundState')
                assert read_state(data,checked,inputs,events,cut)==state
                resumed = state
                for inp,event in zip(inputs[cut:],events[cut:]):
                    resumed = read_event(event,checked,resumed,inp)
                assert resumed==final
                cuts += 1
                for key,value in (('stateVersion',state['stateVersion']+1),('prefix','sha256:'+'0'*64),
                                  ('clockConfigurationHash','sha256:'+'0'*64),('contractVersion',1)):
                    bad = copy.deepcopy(state); bad[key]=value
                    rejected(lambda: read_state(raw(bad,'RoundState'),checked,inputs,events,cut)); mutations += 1
                if len(state['receipts'])>=2:
                    bad = copy.deepcopy(state); bad['receipts'].reverse()
                    rejected(lambda: read_state(raw(bad,'RoundState'),checked,inputs,events,cut)); mutations += 1
                if state['slots']:
                    bad = copy.deepcopy(state); bad['slots'].reverse()
                    rejected(lambda: read_state(raw(bad,'RoundState'),checked,inputs,events,cut)); mutations += 1
                for bad in (b' '+data,data+b'\n',data.replace(b'{',b'{"extra":0,',1),
                            data.replace(b'"contractVersion":2',b'"contractVersion":2,"contractVersion":2',1),
                            encode(dict(reversed(list(json.loads(data).items()))))):
                    rejected(lambda: parse(bad,'RoundState')); raw_rejections += 1
            for index,event in enumerate(events):
                prior = replay(checked,inputs,events,index)
                original = json.loads(event)
                for key,value in (('configurationHash','sha256:'+'0'*64),('predecessorConfigurationHash','sha256:'+'0'*64),
                                  ('author','commonwealth' if original['author']!='commonwealth' else 'axis'),
                                  ('stateVersion',original['stateVersion']+1)):
                    bad = copy.deepcopy(original); bad[key]=value
                    # Self-consistent local receipt hash cannot replace causal replay.
                    del bad['receiptId']; bad['receiptId']='cmb.'+digest('receipt',bad)
                    rejected(lambda: read_event(raw(bad,'RoundEvent'),checked,prior,inputs[index])); mutations += 1
            rejected(lambda: replay(checked,inputs*4,events*4))
            rejected(lambda: replay(checked,inputs,events,True))
            rejected(lambda: parse(b' '*1048577,'RoundState'))
            rejected(lambda: typed([None]*513,'id[]'))
            rejected(lambda: typed('x','id',33))
    return cuts,mutations,raw_rejections


def test_predecessor_and_clock_tamper():
    base,pred,state,inp,event = opened()
    for predecessor in ({},None,dict(boundary=pred['boundary'],inputs=None,events=[])):
        rejected(lambda: read_base(raw(base,'Base'),predecessor))
    for key,value in (('timingPolicyId','sandtable.combat.fixed-deadline.v1'),
                      ('decisionBudgetMilliseconds',30001),('parentConfigurationHash','sha256:'+'0'*64),('contractVersion',1)):
        bad = copy.deepcopy(base); bad['clockConfiguration'][key]=value
        rejected(lambda: read_base(raw(bad,'Base'),pred))
    for key in ('openingFloorUnixMilliseconds','openedAtUnixMilliseconds','deadlineUnixMilliseconds'):
        bad = json.loads(event); bad['effect']['timing'][key]+=1
        del bad['receiptId']; bad['receiptId']='cmb.'+digest('receipt',bad)
        rejected(lambda: read_event(raw(bad,'RoundEvent'),base,initial(base),inp))
    old_base,old_pred = legacy.start()
    rejected(lambda: read_base(legacy.encode(old_base),old_pred))
    rejected(lambda: legacy.parse(raw(state,'RoundState'),'RoundState'))
    for now,available in ((None,True),(3000,False),(253402300799999,True)):
        rejected(lambda: transition(base,initial(base),trusted(command(base,initial(base),'open-round'),now=now,available=available)))


def test_fixture_integrity():
    original = FIXTURE.read_bytes()
    floating = json.loads(original)
    floating['cases'][0]['inputs'][0]['admittedAt'] = 3000.0
    boolean = json.loads(original)
    boolean['clockOutcomes'][1]['clockAvailable'] = 0
    duplicate = original.replace(b'"contract": "combat-sealed-round-v2",',
                                 b'"contract": "combat-sealed-round-v2", "contract": "combat-sealed-round-v2",',1)
    failures = []
    for name,mutant in (('float',json.dumps(floating,indent=2).encode()+b'\n'),
                        ('boolean-as-integer',json.dumps(boolean,indent=2).encode()+b'\n'),
                        ('duplicate-key',duplicate)):
        try:
            rejected(lambda: verify_fixture(mutant))
        except AssertionError:
            failures.append(name)
    assert not failures, 'retained fixture accepted: '+', '.join(failures)



def behavior_tests():
    failures, results = [], {}
    for test in (test_equal_live_slot_clock_outcome,test_successor_identity,test_independent_opening,
                 test_disabled_admission,test_old_reader_rejection,test_authenticated_readback,
                 test_full_clock_matrix,test_invalid_before_clock,test_expiry_recovery,test_replay_tamper,
                 test_predecessor_and_clock_tamper,test_fixture_integrity):
        try:
            results[test.__name__] = test()
        except (AssertionError,NameError,KeyError,TypeError) as error:
            failures.append(test.__name__)
            print('FAIL:',test.__name__,str(error))
    assert not failures, failures
    return results


SOURCE_FILES = (
    'combat-sealed-round-v1.schema.json','verify-combat-sealed-round-v1.py','fixtures/combat-sealed-round-v1.json',
    'combat-selection-steps-v1.schema.json','verify-combat-selection-steps-v1.py','fixtures/combat-selection-steps-v1.json',
)


def retained_vectors():
    cases = []
    for side in ('axis','commonwealth'):
        for first,terminal in (('attacker','committed'),('defender','committed'),
                               (None,'expired'),('attacker','expired'),('defender','fault')):
            base,predecessor,inputs,events,final = trace(side,first,terminal)
            states = [raw(replay(base,inputs,events,cut),'RoundState').decode() for cut in range(len(events)+1)]
            cases.append(dict(name=side+'.'+str(first)+'.'+terminal,
                              provenance='synthetic-C3a-authenticated-decline',
                              baseCanonicalUtf8=raw(base,'Base').decode(),predecessor=predecessor,
                              clockConfigurationHash=configuration_hash(base),inputs=inputs,
                              eventCanonicalUtf8=[event.decode() for event in events],stateCanonicalUtf8=states))
    return dict(contract='combat-sealed-round-v2',clockPolicyId=POLICY,
                sources={name:sha((ROOT/name).read_bytes()) for name in SOURCE_FILES},
                clockOutcomes=[dict(admittedAt=now,clockAvailable=available,outcome=clock_expected(now,available)) for now,available in CLOCKS],
                cases=cases)


def verify_fixture(data):
    expected = (json.dumps(retained_vectors(),indent=2)+'\n').encode('utf-8')
    require(type(data) is bytes and data==expected,6,'/retained-vectors')


def main():
    require(FIXTURE.is_file(),1,'/required-retained-fixture')
    results = behavior_tests()
    verify_fixture(FIXTURE.read_bytes())
    cuts,mutations,raw_rejects = results['test_replay_tamper']
    print(f"PASS: sealed-round v2: {len(results)} semantic groups, 10 retained traces, {cuts} cuts, "
          f"{mutations} replay mutations, {raw_rejects} raw rejects, "
          f"{results['test_full_clock_matrix']} clock comparisons/retries, "
          f"{results['test_expiry_recovery']} lifecycle retries, {results['test_invalid_before_clock']} invalid proposals")


if __name__ == '__main__':
    main()
