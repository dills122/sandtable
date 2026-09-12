#!/usr/bin/env python3
"""Creation-rooted released-I ordinal-2 atomic Movement."""
from __future__ import annotations
import copy, importlib.util, json, sys
from functools import lru_cache
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
FIXTURE = ROOT/'fixtures/combat-inherited-reserve-movement-v1.json'
INVENTORY = json.loads((ROOT/'combat-inherited-reserve-movement-v1.schema.json').read_text())
SOURCE_PATHS = [
    'docs/specs/combat-inherited-cycle-control-v1.schema.json',
    'docs/specs/verify-combat-inherited-cycle-control-v1.py',
    'docs/specs/fixtures/combat-inherited-cycle-control-v1.json',
    'docs/specs/combat-ordinary-movement-v1.schema.json',
    'docs/specs/verify-combat-ordinary-movement-v1.py',
    'docs/specs/fixtures/combat-ordinary-movement-v1.json']

def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None: raise RuntimeError(f'cannot load {path}')
    out = importlib.util.module_from_spec(spec); spec.loader.exec_module(out); return out

previous = module('inherited_cycle_control', ROOT/'verify-combat-inherited-cycle-control-v1.py')
movement = module('ordinary_movement', ROOT/'verify-combat-ordinary-movement-v1.py')
cycle = previous.cyc
encode, sha = previous.encode, previous.sha
SCHEMA = {k: [tuple(f.split(':')) for f in v.split()] for k, v in INVENTORY['objects'].items()}

class Invalid(ValueError):
    def __init__(self, code): self.code = f'CMB-IRM-{code:03}'; super().__init__(self.code)
def require(ok, code):
    if not ok: raise Invalid(code)
def translated(call, code=6):
    try: return call()
    except Invalid: raise
    except ValueError as error: raise Invalid(code) from error
def provider(kind):
    if kind == 'InheritedCycleControlBase': return previous
    if kind in ('CycleControlInput','CycleControlEvent','ReserveMember','MovementEndProof'): return cycle
    return movement

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
    else: translated(lambda: provider(kind).typed(v, kind, depth), 1)
def canonical(v, kind):
    if kind.endswith('?'): return None if v is None else canonical(v, kind[:-1])
    if kind in SCHEMA: return {k: canonical(v[k], t) for k, t in SCHEMA[kind]}
    if kind.endswith('[]'): return [canonical(x, kind[:-2]) for x in v]
    return provider(kind).canonical(v, kind)
def raw(v, kind):
    typed(v, kind); data = encode(canonical(v, kind)); require(len(data) <= INVENTORY['limits']['bytes'], 1); return data
def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= INVENTORY['limits']['bytes'], 1)
    def pairs(items):
        out = {}
        for k, v in items: require(k not in out, 1); out[k] = v
        return out
    try: value = json.loads(data.decode('utf8'), object_pairs_hook=pairs,
        parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error: raise Invalid(1) from error
    require(raw(value, kind) == data, 8); return value

def leaves(v, path=()):
    if type(v) is dict:
        for k, x in v.items(): yield from leaves(x, path+(k,))
    elif type(v) is list:
        for i, x in enumerate(v): yield from leaves(x, path+(i,))
    else: yield path, v
def changed(v, path, replacement):
    out=copy.deepcopy(v); target=out
    for step in path[:-1]: target=target[step]
    target[path[-1]]=replacement; return out
def different(v):
    if v is None: return 'unexpected'
    if type(v) is bool: return not v
    if type(v) is int: return v+1
    if type(v) is str:
        if v.startswith('sha256:'):
            zero='sha256:'+'0'*64; return zero if v != zero else 'sha256:'+'1'*64
        return v+'.changed'
    raise TypeError(type(v))

@lru_cache(maxsize=2)
def cached_base(actor):
    predecessor, states, inputs, events = previous.trace(actor, 'repeat')
    terminal = states[-1]; active = terminal['activeCycle']
    position = copy.deepcopy(next(p for p in movement.steps.seq.expected_catalog()['positions']
        if p['positionId'] == terminal['positionId']))
    position['activeSide'] = actor
    value = dict(contractVersion=1, actor=actor,
        predecessorHash=sha(cycle.raw(terminal, 'CycleControlState')),
        predecessorBase=predecessor, predecessorInputs=inputs,
        predecessorEvents=[json.loads(event) for event in events],
        cycle=copy.deepcopy(active),
        firstActingSide=predecessor['controlBase']['releaseBase']['firstActingSide'],
        position=position, stateVersion=terminal['stateVersion'], prefix=terminal['prefix'],
        world=copy.deepcopy(terminal['world']), randomState=copy.deepcopy(terminal['randomState']),
        attackHistory=copy.deepcopy(terminal['attackHistory']), releaseMember=copy.deepcopy(terminal['members'][0]),
        priorMovementEnd=copy.deepcopy(predecessor['controlBase']['movementEnd']))
    raw(value, 'InheritedReserveMovementBase'); return value
def base_for(actor):
    require(actor in ('axis','commonwealth'), 2); return copy.deepcopy(cached_base(actor))

def read_base(data, expected=None):
    value=parse(data,'InheritedReserveMovementBase'); expected=base_for(value['actor']) if expected is None else expected
    require(data == raw(expected,'InheritedReserveMovementBase'), 4)
    c=expected['cycle']; member=expected['releaseMember']; history=member['history']
    require(c['ordinal']==2 and c['actingSide']==expected['actor']
        and expected['position']['positionId']==expected['predecessorEvents'][-1]['effect']['successorPositionId'],4)
    require(member['status']=='none' and member['spentCp']=={'numerator':0,'denominator':1}
        and history['releasedType']=='I' and history['voluntaryCeiling']==10
        and history['nextMovement']==dict(scope=history['scope'],ordinal=2,status='pending',completionReceiptId=None),5)
    element=next(e for e in expected['world']['elements'] if e['elementId']==member['unit']['elementId'])
    require(element['ammunition']['points']==10 and sum(x['currentToe'] for x in element['components'])==10,5)
    return expected
def initial(base):
    read_base(raw(base,'InheritedReserveMovementBase'),base)
    return dict(contractVersion=1,baseHash=sha(raw(base,'InheritedReserveMovementBase')),
        stateVersion=base['stateVersion'],prefix=base['prefix'],world=copy.deepcopy(base['world']),
        randomState=copy.deepcopy(base['randomState']),attackHistory=copy.deepcopy(base['attackHistory']),
        releaseMember=copy.deepcopy(base['releaseMember']),tracks=[],receipts=[])
def cycle_id(base): return sha(movement.steps.seq.identity(base['cycle'],'Authority',base['firstActingSide']))
def command(base,state,destination):
    unit=state['releaseMember']['unit']; element=next(e for e in state['world']['elements'] if e['elementId']==unit['elementId'])
    return dict(command=dict(contractVersion=1,kind='move',cycleId=cycle_id(base),positionId=base['position']['positionId'],
        expectedPriorVersion=state['stateVersion'],unit=copy.deepcopy(unit),originLocationId=element['currentLocationId'],
        destinationLocationId=destination),actor=base['actor'])

def transition(base, prior, inp):
    typed(prior,'InheritedReserveMovementState'); movement.typed(inp,'MovementInput')
    read_base(raw(base,'InheritedReserveMovementBase'),base)
    cmd=inp['command']; actor=inp['actor']; unit=cmd['unit']; c=base['cycle']
    require(prior['contractVersion']==cmd['contractVersion']==1 and cmd['kind']=='move',3)
    require(prior['baseHash']==sha(raw(base,'InheritedReserveMovementBase')),4)
    require(actor==base['actor']==c['actingSide']==unit['originalSide'],4)
    require(unit==prior['releaseMember']['unit'] and cmd['cycleId']==cycle_id(base)
        and cmd['positionId']==base['position']['positionId'],4)
    command_hash=sha(movement.raw(cmd,'MovementCommand'))
    receipt=next((x for x in prior['receipts'] if x['commandHash']==command_hash and x['actor']==actor),None)
    if receipt: return prior,None,receipt['receiptId']
    require(prior['stateVersion']<2**63-1 and len(prior['receipts'])<32,7)
    require(cmd['expectedPriorVersion']==prior['stateVersion'],6)
    members=[e for e in prior['world']['elements'] if e['elementId']==unit['elementId']]
    require(len(members)==1,4); element=members[0]; op=element['operationalState']
    origin,dest=cmd['originLocationId'],cmd['destinationLocationId']
    require(element['currentLocationId']==origin and origin in movement.world.GRAPH
        and dest in movement.world.GRAPH[origin],5)
    require(element['reserveStatus']=='none' and element['ammunition']['points']==10
        and op['vehicleBreakdownState'] is None and op['movementEnded'] is None,3)
    require((op['ledgerGameTurn'],op['ledgerOperationStage'])==(c['gameTurn'],c['operationStage']),4)
    history=prior['releaseMember']['history']; exception=history['nextMovement']
    require(history['releasedType']=='I' and history['voluntaryCeiling']==10
        and exception==dict(scope=history['scope'],ordinal=2,status='pending',completionReceiptId=None)
        and history['scope']==cycle.rel.scope(c),4)
    require(all(other['currentLocationId']!=dest for other in prior['world']['elements'] if other is not element)
        and all(g['currentLocationId']!=dest for g in prior['world']['guards']),5)
    reps=[x for x in prior['world']['representations'] if x['bindingKind']=='independent-element'
        and x['boundElementIds']==[element['elementId']]]
    require(len(reps)==1 and reps[0]['currentLocationId']==origin,4)
    spent=op['capabilityPointsExpended']['numerator']; require(op['capabilityPointsExpended']['denominator']==1
        and prior['releaseMember']['spentCp']==op['capabilityPointsExpended'],4)
    ended=movement.affected(prior['world']['relationships'],unit)
    terrain=movement.terrain_cost(origin,dest)
    after,dp=movement.cost(10,spent,[x['kind'] for x in ended],'I',terrain)
    cohesion=op['cohesionLevel']; require(cohesion-dp>=-2**31,7)
    costs=dict(terrainCost=terrain,breakOffCost=after-spent-terrain,totalCost=after-spent,
        baseCpa=10,voluntaryCeiling=10,beforeCp=spent,afterCp=after,excessCpDp=dp,
        beforeCohesion=cohesion,afterCohesion=cohesion-dp)
    effect=dict(kind='ordinary-move',unit=copy.deepcopy(unit),representationId=reps[0]['representationId'],
        originLocationId=origin,destinationLocationId=dest,costs=costs,endedMemberships=ended)
    event=dict(contractVersion=1,eventType='combat-cycle-element-moved',author=actor,
        campaignId=c['campaignId'],rulesetHash=c['rulesetHash'],configurationHash=c['admittedPolicyBundleDigest'],
        cycleId=cmd['cycleId'],positionId=cmd['positionId'],baseHash=prior['baseHash'],
        priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],
        input=movement.canonical(inp,'MovementInput'),effect=movement.canonical(effect,'MovementEffect'))
    rid='mov.'+movement.steps.digest(movement.INVENTORY['domains']['receipt'],encode(event))
    event['receiptId']=rid; data=movement.raw(event,'MovementEvent')
    state=copy.deepcopy(prior); world=state['world']
    moved=next(x for x in world['elements'] if x['elementId']==element['elementId'])
    moved['currentLocationId']=dest; moved['operationalState']['capabilityPointsExpended']=movement.world.cp(after)
    moved['operationalState']['cohesionLevel']=cohesion-dp
    state['releaseMember']['spentCp']=movement.world.cp(after)
    next(x for x in world['representations'] if x['representationId']==reps[0]['representationId'])['currentLocationId']=dest
    for relation in world['relationships']:
        if relation['relationId'] in {x['relationId'] for x in ended}:
            relation.update(active=False,endedByReceiptId=rid,endingCause='ordinary-break-off')
    if dp:
        require(len(world['cohesionCauses'])<512,7)
        world['cohesionCauses'].append(dict(causeId=rid+'.dp',ordinal=len(world['cohesionCauses'])+1,
            receiptId=rid,elementId=element['elementId'],gameTurn=c['gameTurn'],operationStage=c['operationStage'],
            kind='ordinary-movement-excess-cp-dp',points=dp,before=cohesion,after=cohesion-dp))
    state['tracks']=[dict(unit=copy.deepcopy(unit),route=[origin,dest])]
    state['stateVersion']=event['stateVersion']; state['prefix']=movement.steps.seq.prefix_event(prior['prefix'],data)
    state['receipts'].append(dict(commandHash=command_hash,eventHash=sha(data),receiptId=rid,
        actor=actor,stateVersion=state['stateVersion']))
    raw(state,'InheritedReserveMovementState'); return state,data,rid

def read_event(data,base,prior,inp):
    movement.parse(data,'MovementEvent'); state,expected,_=transition(base,prior,inp)
    require(data==expected,6); return state
def read_state(data,base,inputs,events,length=None):
    parse(data,'InheritedReserveMovementState')
    require(len(inputs)==len(events)<=1 and (length is None or type(length) is int and 0<=length<=len(events)),1)
    read_base(raw(base,'InheritedReserveMovementBase'),base); state=initial(base)
    for inp,event in zip(inputs[:length],events[:length]): state=read_event(event,base,state,inp)
    require(data==raw(state,'InheritedReserveMovementState'),6); return state
def rejected(call):
    try: call()
    except ValueError: return
    raise AssertionError('invalid inherited Reserve Movement admitted')

def trace(actor):
    base=base_for(actor); first=initial(base); member=first['releaseMember']; unit=member['unit']
    element=next(e for e in first['world']['elements'] if e['elementId']==unit['elementId'])
    origin=element['currentLocationId']
    destination=movement.world.init.content.route(movement.world.GRAPH,origin,movement.world.ANCHORS[actor])[1]
    inp=command(base,first,destination); final,event,receipt=transition(base,first,inp)
    effect=json.loads(event)['effect']; moved=next(e for e in final['world']['elements'] if e['elementId']==unit['elementId'])
    require((effect['costs']['beforeCp'],effect['costs']['afterCp'],effect['costs']['excessCpDp'],
        effect['costs']['voluntaryCeiling'])==(0,2,0,10) and not effect['endedMemberships'],6)
    expected_world=copy.deepcopy(first['world'])
    expected_element=next(e for e in expected_world['elements'] if e['elementId']==unit['elementId'])
    expected_element['currentLocationId']=destination
    expected_element['operationalState']['capabilityPointsExpended']={'numerator':2,'denominator':1}
    next(r for r in expected_world['representations'] if r['boundElementIds']==[unit['elementId']])['currentLocationId']=destination
    expected_member=copy.deepcopy(member); expected_member['spentCp']={'numerator':2,'denominator':1}
    require(final['stateVersion']==28 and moved['currentLocationId']==destination
        and moved['ammunition']['points']==10 and sum(x['currentToe'] for x in moved['components'])==10
        and final['world']==expected_world and final['releaseMember']==expected_member
        and final['releaseMember']['history']==member['history']
        and final['randomState']==first['randomState'] and final['attackHistory']==first['attackHistory']
        and final['releaseMember']['history']['nextMovement']['status']=='pending'
        and final['receipts'][0]['receiptId']==receipt,6)
    return base,[first,final],[inp],[event]

def source_pins():
    repo=ROOT.parent.parent
    return [dict(path=p,sha256=sha((repo/p).read_bytes())) for p in SOURCE_PATHS]
def golden(result):
    base,states,_,events=result; base_data=raw(base,'InheritedReserveMovementBase')
    return dict(baseBytes=len(base_data),baseHash=sha(base_data),
        frames=[dict(cut=i,bytes=len(d),sha256=sha(d)) for i,d in enumerate(
            raw(s,'InheritedReserveMovementState') for s in states)],
        eventBytes=len(events[0]),eventHash=sha(events[0]),terminalPrefix=states[-1]['prefix'],
        receiptId=states[-1]['receipts'][0]['receiptId'])
def generated_fixture():
    return dict(contract='combat-inherited-reserve-movement-v1',
        cases=[dict(actor=a,golden=golden(trace(a))) for a in ('axis','commonwealth')],
        sourcePins=source_pins(),goldenMethod='canonical ASCII JSON; SHA-256 with sha256: prefix')

def verify_case(case):
    base,states,inputs,events=trace(case['actor']); require(case['golden']==golden((base,states,inputs,events)),6)
    expected=base_for(case['actor']); read_base(raw(base,'InheritedReserveMovementBase'),expected)
    require(read_state(raw(states[0],'InheritedReserveMovementState'),base,inputs,events,0)==states[0]
        and read_event(events[0],base,states[0],inputs[0])==states[1]
        and read_state(raw(states[1],'InheritedReserveMovementState'),base,inputs,events,1)==states[1],6)
    retry,event,receipt=transition(base,states[1],inputs[0]); require(retry is states[1] and event is None
        and receipt==states[1]['receipts'][0]['receiptId'],6)
    counts=dict(readbacks=4,retries=1,mutations=0,raw=0)
    for path,leaf in leaves(base):
        altered=changed(base,path,different(leaf))
        try: data=raw(altered,'InheritedReserveMovementBase')
        except ValueError: data=encode(altered)
        rejected(lambda data=data: read_base(data,expected)); counts['mutations']+=1
    event_obj=json.loads(events[0]); event_leaves=dict(leaves(event_obj))
    for path in [('priorVersion',),('stateVersion',),('priorPrefix',),('baseHash',),('cycleId',),
            ('configurationHash',),('receiptId',),('effect','costs','voluntaryCeiling'),
            ('effect','costs','afterCp'),('effect','destinationLocationId')]:
        altered=changed(event_obj,path,different(event_leaves[path]))
        if path[0]=='effect':
            altered.pop('receiptId'); altered['receiptId']='mov.'+movement.steps.digest(
                movement.INVENTORY['domains']['receipt'],encode(altered))
        try: data=movement.raw(altered,'MovementEvent')
        except ValueError: data=encode(altered)
        rejected(lambda data=data: read_event(data,base,states[0],inputs[0])); counts['mutations']+=1
    for mutate in (lambda s:s['releaseMember']['spentCp'].__setitem__('numerator',3),
            lambda s:s['releaseMember']['history']['nextMovement'].__setitem__('status','expired'),
            lambda s:s['world']['elements'][0]['ammunition'].__setitem__('points',9),
            lambda s:s['randomState'].__setitem__('nextByteCursor',s['randomState']['nextByteCursor']+1),
            lambda s:s['tracks'].clear(),lambda s:s['receipts'].clear()):
        altered=copy.deepcopy(states[1]); mutate(altered)
        rejected(lambda altered=altered: read_state(raw(altered,'InheritedReserveMovementState'),
            base,inputs,events,1)); counts['mutations']+=1
    for data,reader in [(raw(base,'InheritedReserveMovementBase'),lambda d:read_base(d,expected)),
            (events[0],lambda d:read_event(d,base,states[0],inputs[0]))]:
        for bad in (data+b'\n',b'\xef\xbb\xbf'+data,b' '+data,data[:-1],
                data.replace(b'"contractVersion":1',b'"contractVersion":1.0',1),b'{"extra":0,'+data[1:]):
            rejected(lambda bad=bad,reader=reader:reader(bad)); counts['raw']+=1
    rejected(lambda:read_state(raw(states[1],'InheritedReserveMovementState'),base,[],[])); counts['mutations']+=1
    return counts

def boundary_checks():
    n=0; base,states,inputs,_=trace('axis'); first=states[0]
    opponent=next(e for e in first['world']['elements'] if e['elementId']!=first['releaseMember']['unit']['elementId'])
    for mutate in (lambda i:i.__setitem__('actor','commonwealth'),
            lambda i:i['command'].__setitem__('cycleId',sha(b'wrong')),
            lambda i:i['command'].__setitem__('expectedPriorVersion',26),
            lambda i:i['command'].__setitem__('destinationLocationId',opponent['currentLocationId']),
            lambda i:i['command']['unit'].__setitem__('creationBinding','creation.'+'0'*64)):
        altered=copy.deepcopy(inputs[0]); mutate(altered); rejected(lambda altered=altered:transition(base,first,altered)); n+=1
    for mutate in (lambda b:b['releaseMember']['history'].__setitem__('releasedType','II'),
            lambda b:b['releaseMember']['history']['nextMovement'].__setitem__('ordinal',3),
            lambda b:b['cycle'].__setitem__('ordinal',3),
            lambda b:b['world']['elements'][0]['ammunition'].__setitem__('points',9)):
        altered=copy.deepcopy(base); mutate(altered); rejected(lambda altered=altered:read_base(raw(altered,'InheritedReserveMovementBase'),base)); n+=1
    overflow=copy.deepcopy(first); overflow['stateVersion']=2**63-1
    altered=copy.deepcopy(inputs[0]); altered['command']['expectedPriorVersion']=2**63-1
    rejected(lambda:transition(base,overflow,altered)); n+=1
    oversized=copy.deepcopy(base); oversized['predecessorEvents']*=257
    rejected(lambda:raw(oversized,'InheritedReserveMovementBase')); n+=1
    return n

def main():
    if '--goldens' in sys.argv: print(json.dumps(generated_fixture(),indent=2)); return
    fixture=json.loads(FIXTURE.read_text())
    require(set(fixture)=={'contract','cases','sourcePins','goldenMethod'}
        and fixture['contract']=='combat-inherited-reserve-movement-v1',1)
    require([c['actor'] for c in fixture['cases']]==['axis','commonwealth']
        and fixture['sourcePins']==source_pins(),4)
    totals=dict(readbacks=0,retries=0,mutations=0,raw=0)
    for case in fixture['cases']:
        for key,value in verify_case(case).items(): totals[key]+=value
    boundaries=boundary_checks()
    print(f"combat inherited Reserve Movement v1: 2 traces, {totals['readbacks']} readbacks, "
        f"{totals['retries']} retries, {totals['mutations']} mutations, {totals['raw']} raw rejects, "
        f"{boundaries} boundary rejects")
if __name__=='__main__': main()
