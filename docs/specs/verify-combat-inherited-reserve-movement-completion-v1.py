#!/usr/bin/env python3
"""Creation-rooted released-I Movement completion contract oracle."""
from __future__ import annotations
import copy, hashlib, importlib.util, json, sys
from functools import lru_cache
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
FIXTURE = ROOT/'fixtures/combat-inherited-reserve-movement-completion-v1.json'
INVENTORY = json.loads((ROOT/'combat-inherited-reserve-movement-completion-v1.schema.json').read_text())
SOURCE_PATHS = [
    'docs/specs/combat-inherited-reserve-movement-v1.schema.json',
    'docs/specs/verify-combat-inherited-reserve-movement-v1.py',
    'docs/specs/fixtures/combat-inherited-reserve-movement-v1.json',
    'docs/specs/combat-inherited-movement-lifecycle-v1.schema.json',
    'docs/specs/verify-combat-inherited-movement-lifecycle-v1.py',
    'docs/specs/fixtures/combat-inherited-movement-lifecycle-v1.json',
    'docs/specs/combat-cycle-control-v1.schema.json',
    'docs/specs/verify-combat-cycle-control-v1.py',
    'docs/specs/fixtures/combat-cycle-control-v1.json',
    'docs/specs/combat-cycle-sequence-v1.schema.json']

def module(name, path):
    spec=importlib.util.spec_from_file_location(name,path)
    if spec is None or spec.loader is None: raise RuntimeError(f'cannot load {path}')
    out=importlib.util.module_from_spec(spec); spec.loader.exec_module(out); return out

previous=module('inherited_reserve_movement',ROOT/'verify-combat-inherited-reserve-movement-v1.py')
lifecycle=module('inherited_movement_lifecycle',ROOT/'verify-combat-inherited-movement-lifecycle-v1.py')
cycle,movement=previous.cycle,previous.movement
encode,sha=previous.encode,previous.sha
SCHEMA={k:[tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['objects'].items()}
KINDS=['stop-element-movement','resolve-breakdown-stop','complete-movement-segment']
EVENTS=['combat-cycle-element-movement-stopped','combat-cycle-breakdown-stop-resolved',
    'combat-cycle-movement-segment-completed']
TYPES=['StopEvent','ResolveEvent','CompleteEvent']; VERSIONS=[1,1,1]
DOMAINS=[INVENTORY['domains'][k] for k in ('stopReceipt','resolveReceipt','completeReceipt')]

class Invalid(ValueError):
    def __init__(self,code): self.code=f'CMB-IRMC-{code:03}'; super().__init__(self.code)
def require(ok,code):
    if not ok: raise Invalid(code)
def translated(call,code=6):
    try: return call()
    except Invalid: raise
    except ValueError as error: raise Invalid(code) from error
def variant(value,kind):
    require(type(value) is dict and type(value.get('kind')) is str,1)
    require(value['kind'] in INVENTORY['unions'][kind],3)
    return INVENTORY['unions'][kind][value['kind']]
def provider(kind):
    if kind=='InheritedReserveMovementBase': return previous
    if kind in cycle.SCHEMA: return cycle
    if kind in lifecycle.SCHEMA: return lifecycle
    return movement
def typed(value,kind,depth=0):
    require(depth<=INVENTORY['limits']['depth'],1)
    if kind in INVENTORY['unions']: typed(value,variant(value,kind),depth)
    elif kind.endswith('?'):
        if value is not None: typed(value,kind[:-1],depth)
    elif kind in SCHEMA:
        require(type(value) is dict and set(value)=={k for k,_ in SCHEMA[kind]},1)
        for k,t in SCHEMA[kind]: typed(value[k],t,depth+1)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value)<=INVENTORY['limits']['arrayItems'],1)
        for x in value: typed(x,kind[:-2],depth+1)
    else: translated(lambda:provider(kind).typed(value,kind,depth),1)
def canonical(value,kind):
    if kind in INVENTORY['unions']: return canonical(value,variant(value,kind))
    if kind.endswith('?'): return None if value is None else canonical(value,kind[:-1])
    if kind in SCHEMA: return {k:canonical(value[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'):
        child=kind[:-2]; out=[canonical(x,child) for x in value]
        if child=='UnitKey': out.sort(key=cycle.rel.key)
        elif child=='EndLocation': out.sort(key=lambda x:cycle.rel.key(x['unit']))
        return out
    return provider(kind).canonical(value,kind)
def raw(value,kind):
    typed(value,kind); data=encode(canonical(value,kind)); require(len(data)<=INVENTORY['limits']['bytes'],1); return data
def parse(data,kind):
    require(type(data) is bytes and 0<len(data)<=INVENTORY['limits']['bytes'],1)
    def pairs(items):
        out={}
        for k,v in items: require(k not in out,1); out[k]=v
        return out
    try: value=json.loads(data.decode('utf8'),object_pairs_hook=pairs,
        parse_constant=lambda _:(_ for _ in ()).throw(ValueError()))
    except (ValueError,UnicodeError,RecursionError) as error: raise Invalid(1) from error
    require(raw(value,kind)==data,8); return value
def leaves(value,path=()):
    if type(value) is dict:
        for k,v in value.items(): yield from leaves(v,path+(k,))
    elif type(value) is list:
        for i,v in enumerate(value): yield from leaves(v,path+(i,))
    else: yield path,value
def changed(value,path,replacement):
    out=copy.deepcopy(value); target=out
    for step in path[:-1]: target=target[step]
    target[path[-1]]=replacement; return out
def different(value):
    if value is None: return 'unexpected'
    if type(value) is bool: return not value
    if type(value) is int: return value+1
    if type(value) is str:
        if value.startswith('sha256:'):
            zero='sha256:'+'0'*64; return zero if value!=zero else 'sha256:'+'1'*64
        return value+'.changed'
    raise TypeError(type(value))

@lru_cache(maxsize=2)
def cached_base(actor):
    predecessor,states,inputs,events=previous.trace(actor); terminal=states[-1]
    value=dict(contractVersion=1,actor=actor,
        predecessorHash=sha(previous.raw(terminal,'InheritedReserveMovementState')),
        predecessorBase=predecessor,predecessorInputs=inputs,
        predecessorEvents=[json.loads(event) for event in events],
        movementPosition=copy.deepcopy(predecessor['position']),
        interruptPosition=copy.deepcopy(lifecycle.INTERRUPT),
        breakdownPosition=copy.deepcopy(lifecycle.TERMINAL))
    raw(value,'InheritedReserveMovementCompletionBase'); return value
def base_for(actor):
    require(actor in ('axis','commonwealth'),2); return copy.deepcopy(cached_base(actor))
def predecessor_state(base):
    predecessor=base['predecessorBase']; inputs=base['predecessorInputs']
    events=[movement.raw(event,'MovementEvent') for event in base['predecessorEvents']]
    expected=previous.trace(base['actor'])[1][-1]
    return previous.read_state(previous.raw(expected,'InheritedReserveMovementState'),
        predecessor,inputs,events,len(events))
def read_base(data,expected=None):
    value=parse(data,'InheritedReserveMovementCompletionBase')
    expected=base_for(value['actor']) if expected is None else expected
    require(data==raw(expected,'InheritedReserveMovementCompletionBase'),4)
    terminal=predecessor_state(expected); history=terminal['releaseMember']['history']
    require(expected['predecessorHash']==sha(previous.raw(terminal,'InheritedReserveMovementState'))
        and terminal['stateVersion']==28 and len(terminal['tracks'])==len(terminal['receipts'])==1,4)
    require(history['nextMovement']==dict(scope=history['scope'],ordinal=2,status='pending',completionReceiptId=None),5)
    require(expected['movementPosition']==expected['predecessorBase']['position']
        and expected['interruptPosition']==lifecycle.INTERRUPT
        and expected['breakdownPosition']==lifecycle.TERMINAL,4)
    return expected
def cycle_id(base):
    source=base['predecessorBase']
    return sha(movement.steps.seq.identity(source['cycle'],'Authority',source['firstActingSide']))
def initial(base):
    read_base(raw(base,'InheritedReserveMovementCompletionBase'),base); terminal=predecessor_state(base)
    return canonical(dict(contractVersion=1,baseHash=sha(raw(base,'InheritedReserveMovementCompletionBase')),
        stateVersion=terminal['stateVersion'],prefix=terminal['prefix'],
        cycle=copy.deepcopy(base['predecessorBase']['cycle']),sequencePosition=copy.deepcopy(base['movementPosition']),
        world=copy.deepcopy(terminal['world']),randomState=copy.deepcopy(terminal['randomState']),
        attackHistory=copy.deepcopy(terminal['attackHistory']),releaseMember=copy.deepcopy(terminal['releaseMember']),
        tracks=copy.deepcopy(terminal['tracks']),breakdownFlow=dict(kind='moving',
            track=copy.deepcopy(terminal['tracks'][0]),movementReceiptId=terminal['receipts'][0]['receiptId']),
        interruptContext=None,movementEnd=None,receipts=[]),'InheritedReserveMovementCompletionState')

def capability(base,state,kind):
    c=state['cycle']; value=dict(domain='sandtable.observation.movement-route.v1' if kind==KINDS[0]
        else 'sandtable.action.breakdown-stop.v1',campaignId=c['campaignId'],rulesetHash=c['rulesetHash'],
        stateVersion=state['stateVersion'],audience=base['actor'] if kind==KINDS[0] else 'system',baseHash=state['baseHash'])
    if kind==KINDS[0]:
        require(state['breakdownFlow']['kind']=='moving',6)
        value.update(track=copy.deepcopy(state['breakdownFlow']['track']),
            movementReceiptId=state['breakdownFlow']['movementReceiptId'])
    else:
        require(state['breakdownFlow']['kind']=='phasing-stop',6)
        value['recordedStateVersion']=state['breakdownFlow']['stop']['recordedStateVersion']
    return sha(encode(value))
def command(base,state,kind):
    require(kind in KINDS,3)
    cmd=dict(contractVersion=1,kind=kind,baseHash=state['baseHash'],cycleId=cycle_id(base),
        positionId=state['sequencePosition']['positionId'],expectedPriorVersion=state['stateVersion'])
    if kind in KINDS[:2]:
        field='routeId' if kind==KINDS[0] else 'stopId'; cmd[field]=capability(base,state,kind)
    cmd['actionId']=sha(encode(copy.deepcopy(cmd)))
    return dict(command=cmd,actor='system' if kind==KINDS[1] else base['actor'])
def event_receipt(event,index):
    typed(event,TYPES[index]); unsigned={k:v for k,v in canonical(event,TYPES[index]).items() if k!='receiptId'}
    return 'irmc.'+hashlib.sha256(DOMAINS[index].encode()+b'\0'+encode(unsigned)).hexdigest()
def expiry_input(base,state,completion_receipt_id,end_locations):
    source=base['predecessorBase']
    return dict(contractVersion=1,cycle=copy.deepcopy(state['cycle']),firstActingSide=source['firstActingSide'],
        priorProof=copy.deepcopy(source['priorMovementEnd']),completionReceiptId=completion_receipt_id,
        endLocations=copy.deepcopy(end_locations),members=[copy.deepcopy(state['releaseMember'])])

def transition(base,prior,inp):
    typed(prior,'InheritedReserveMovementCompletionState'); typed(inp,'CompletionInput')
    read_base(raw(base,'InheritedReserveMovementCompletionBase'),base)
    require(prior['contractVersion']==inp['command']['contractVersion']==1
        and prior['baseHash']==sha(raw(base,'InheritedReserveMovementCompletionBase')),4)
    kind=inp['command']['kind']; require(kind in KINDS,3); index=KINDS.index(kind)
    actor='system' if index==1 else base['actor']; require(inp['actor']==actor,5)
    command_hash=sha(raw(inp,'CompletionInput'))
    accepted=next((r for r in prior['receipts'] if r['commandHash']==command_hash and r['actor']==actor),None)
    if accepted is not None: return prior,None,accepted['receiptId']
    require(prior['stateVersion']<2**63-1 and len(prior['receipts'])<32,7)
    require(raw(inp,'CompletionInput')==raw(command(base,prior,kind),'CompletionInput'),5)
    cmd=inp['command']; require(cmd['baseHash']==prior['baseHash'] and cmd['cycleId']==cycle_id(base)
        and cmd['positionId']==prior['sequencePosition']['positionId']
        and cmd['expectedPriorVersion']==prior['stateVersion'],6)
    version=prior['stateVersion']+1; after=copy.deepcopy(prior); c=prior['cycle']
    event=dict(contractVersion=VERSIONS[index],eventType=EVENTS[index],author=actor,campaignId=c['campaignId'],
        rulesetHash=c['rulesetHash'],configurationHash=c['admittedPolicyBundleDigest'],cycleId=cmd['cycleId'],
        positionId=cmd['positionId'],baseHash=prior['baseHash'],priorVersion=prior['stateVersion'],
        stateVersion=version,priorPrefix=prior['prefix'],input=copy.deepcopy(inp))
    if index==0:
        flow=prior['breakdownFlow']
        require(prior['sequencePosition']==base['movementPosition'] and flow['kind']=='moving'
            and prior['interruptContext'] is None and prior['movementEnd'] is None
            and flow['track']==prior['tracks'][0]
            and flow['movementReceiptId']==base['predecessorEvents'][0]['receiptId'],6)
        stop=dict(stopId='pending',recordedStateVersion=version,track=copy.deepcopy(flow['track']),
            movementReceiptId=flow['movementReceiptId'],reason='deliberate',weatherKind='normal',cohortInputs=[])
        preimage=dict(domain='sandtable.breakdown.stop.v1',campaignId=c['campaignId'],rulesetHash=c['rulesetHash'],
            **{k:v for k,v in stop.items() if k!='stopId'}); stop['stopId']=sha(encode(preimage))
        context=dict(cycle=copy.deepcopy(c),cycleId=cmd['cycleId'],sequencePosition=copy.deepcopy(base['movementPosition']))
        after.update(sequencePosition=copy.deepcopy(base['interruptPosition']),
            breakdownFlow=dict(kind='phasing-stop',stop=copy.deepcopy(stop)),interruptContext=context)
        event.update(stop=copy.deepcopy(stop),sequencePosition=copy.deepcopy(after['sequencePosition']),
            breakdownFlowAfter=copy.deepcopy(after['breakdownFlow']),interruptContextAfter=copy.deepcopy(context))
    elif index==1:
        context=prior['interruptContext']
        require(prior['sequencePosition']==base['interruptPosition']
            and prior['breakdownFlow']['kind']=='phasing-stop'
            and context==dict(cycle=prior['cycle'],cycleId=cmd['cycleId'],sequencePosition=base['movementPosition'])
            and prior['movementEnd'] is None,6)
        stop=copy.deepcopy(prior['breakdownFlow']['stop'])
        after.update(sequencePosition=copy.deepcopy(base['movementPosition']),breakdownFlow=dict(kind='idle'),interruptContext=None)
        event.update(stop=stop,randomStateBefore=copy.deepcopy(prior['randomState']),checks=[],createdLots=[],
            randomStateAfter=copy.deepcopy(prior['randomState']),
            sources=[dict(sourceId='spi-1979-land-rules',locator='21.24-21.26')],
            sequencePosition=copy.deepcopy(after['sequencePosition']),breakdownFlowAfter=copy.deepcopy(after['breakdownFlow']),
            interruptContextAfter=None)
    else:
        require(prior['sequencePosition']==base['movementPosition'] and prior['breakdownFlow']==dict(kind='idle')
            and prior['interruptContext'] is None and prior['movementEnd'] is None,6)
        end_locations=cycle.locations(prior['world'])
        preview=translated(lambda:cycle.expire_movement(expiry_input(base,prior,'pending',end_locations)),4)
        moved=base['predecessorEvents'][0]
        progress=[dict(eventType=moved['eventType'],receiptId=moved['receiptId'],
            eventHash=sha(movement.raw(moved,'MovementEvent')))]
        after.update(sequencePosition=copy.deepcopy(base['breakdownPosition']),breakdownFlow=dict(kind='idle'),interruptContext=None)
        event.update(gameTurn=c['gameTurn'],operationStage=c['operationStage'],actingSide=base['actor'],
            endLocations=copy.deepcopy(end_locations),excludedUnits=copy.deepcopy(preview['proof']['excludedBefore']),
            progress=progress,sequencePosition=copy.deepcopy(after['sequencePosition']),
            breakdownFlowAfter=copy.deepcopy(after['breakdownFlow']),interruptContextAfter=None)
    event['receiptId']='pending'; event['receiptId']=event_receipt(event,index); data=raw(event,TYPES[index])
    receipt_id=event['receiptId']
    if index==2:
        result=translated(lambda:cycle.expire_movement(expiry_input(base,prior,receipt_id,event['endLocations'])),4)
        after['releaseMember']=result['members'][0]; after['movementEnd']=result['proof']
    after['stateVersion']=version; after['prefix']=movement.steps.seq.prefix_event(prior['prefix'],data)
    after['receipts'].append(dict(commandHash=command_hash,eventHash=sha(data),receiptId=receipt_id,
        actor=actor,stateVersion=version))
    return canonical(after,'InheritedReserveMovementCompletionState'),data,receipt_id

def read_event(data,base,prior,inp):
    require(type(data) is bytes and 0<len(data)<=INVENTORY['limits']['bytes'],1)
    try: value=json.loads(data)
    except (ValueError,UnicodeError,RecursionError) as error: raise Invalid(1) from error
    require(type(value) is dict and value.get('eventType') in EVENTS,3); index=EVENTS.index(value['eventType'])
    event=parse(data,TYPES[index]); require(event['contractVersion']==VERSIONS[index]
        and event['input']['command']['kind']==KINDS[index] and event['receiptId']==event_receipt(event,index),4)
    state,expected,_=transition(base,prior,inp); require(data==expected,6); return state
def replay(base,inputs,events,length=None):
    require(len(inputs)==len(events)<=INVENTORY['limits']['eventsPerCase']
        and (length is None or type(length) is int and 0<=length<=len(events)),1)
    state=initial(base); limit=len(events) if length is None else length
    for inp,event in zip(inputs[:limit],events[:limit]): state=read_event(event,base,state,inp)
    return state
def read_state(data,base,inputs,events,length=None):
    value=parse(data,'InheritedReserveMovementCompletionState'); expected=replay(base,inputs,events,length)
    require(data==raw(expected,'InheritedReserveMovementCompletionState'),6); return value

def trace(actor):
    base=base_for(actor); state=initial(base); before=copy.deepcopy(state); states=[state]; inputs=[]; events=[]
    for index,kind in enumerate(KINDS):
        inp=command(base,state,kind); prior=copy.deepcopy(state); state,event,_=transition(base,state,inp)
        require(event is not None,6); inputs.append(inp); events.append(event); states.append(state)
        require(read_event(event,base,prior,inp)==state and state['stateVersion']==29+index
            and state['prefix']==movement.steps.seq.prefix_event(prior['prefix'],event),6)
        require(all(state[k]==before[k] for k in ('cycle','world','randomState','attackHistory','tracks')),6)
    stop_event,resolve_event,complete_event=map(json.loads,events)
    require(states[1]['sequencePosition']==base['interruptPosition']
        and states[1]['breakdownFlow']['stop']['movementReceiptId']==base['predecessorEvents'][0]['receiptId']
        and states[1]['interruptContext']['sequencePosition']==base['movementPosition'],6)
    require(states[2]['sequencePosition']==base['movementPosition'] and states[2]['breakdownFlow']==dict(kind='idle')
        and states[2]['interruptContext'] is None and resolve_event['checks']==resolve_event['createdLots']==[]
        and resolve_event['randomStateBefore']==resolve_event['randomStateAfter'],6)
    terminal=states[-1]; member=terminal['releaseMember']; exception=member['history']['nextMovement']; proof=terminal['movementEnd']
    require(terminal['sequencePosition']==base['breakdownPosition'] and terminal['breakdownFlow']==dict(kind='idle')
        and exception['status']=='expired' and exception['completionReceiptId']==complete_event['receiptId']
        and proof['ordinal']==2 and proof['completionReceiptId']==complete_event['receiptId']
        and proof['endLocations']==complete_event['endLocations'] and proof['excludedBefore']==complete_event['excludedUnits'],6)
    projected=translated(lambda:cycle.expire_movement(
        expiry_input(base,states[2],complete_event['receiptId'],complete_event['endLocations'])),4)
    moved=base['predecessorEvents'][0]
    require(projected==dict(contractVersion=1,members=[member],proof=proof)
        and complete_event['progress']==[dict(eventType=moved['eventType'],receiptId=moved['receiptId'],
            eventHash=sha(movement.raw(moved,'MovementEvent')))]
        and stop_event['stop']==states[1]['breakdownFlow']['stop'],6)
    element=next(e for e in terminal['world']['elements'] if e['elementId']==member['unit']['elementId'])
    require(member['spentCp']==dict(numerator=2,denominator=1)
        and element['operationalState']['capabilityPointsExpended']==member['spentCp']
        and element['ammunition']['points']==10 and sum(x['currentToe'] for x in element['components'])==10,6)
    return base,states,inputs,events

def source_pins():
    repo=ROOT.parent.parent; return [dict(path=p,sha256=sha((repo/p).read_bytes())) for p in SOURCE_PATHS]
def golden(result):
    base,states,_,events=result; base_data=raw(base,'InheritedReserveMovementCompletionBase')
    return dict(baseBytes=len(base_data),baseHash=sha(base_data),frames=[dict(cut=i,bytes=len(data),sha256=sha(data))
        for i,data in enumerate(raw(s,'InheritedReserveMovementCompletionState') for s in states)],
        events=[dict(eventType=json.loads(data)['eventType'],bytes=len(data),sha256=sha(data),
            receiptId=json.loads(data)['receiptId']) for data in events],terminalPrefix=states[-1]['prefix'],
        completionReceiptId=states[-1]['movementEnd']['completionReceiptId'])
def generated_fixture():
    return dict(contract='combat-inherited-reserve-movement-completion-v1',
        cases=[dict(actor=a,golden=golden(trace(a))) for a in ('axis','commonwealth')],
        sourcePins=source_pins(),goldenMethod='canonical ASCII JSON; SHA-256 with sha256: prefix')
def rejected(call,label=''):
    try: call()
    except ValueError: return
    raise AssertionError('invalid inherited Reserve Movement completion admitted: '+label)

def verify_case(case):
    result=trace(case['actor']); base,states,inputs,events=result; require(case['golden']==golden(result),6)
    expected=base_for(case['actor']); read_base(raw(base,'InheritedReserveMovementCompletionBase'),expected)
    counts=dict(readbacks=1,retries=0,mutations=0,raw=0,boundaries=0)
    for i,state in enumerate(states):
        require(read_state(raw(state,'InheritedReserveMovementCompletionState'),base,inputs,events,i)==state,6)
        counts['readbacks']+=1
    for i,event in enumerate(events):
        require(read_event(event,base,states[i],inputs[i])==states[i+1],6); counts['readbacks']+=1
    for inp in inputs:
        state,event,rid=transition(base,states[-1],inp)
        expected_rid=next(r['receiptId'] for r in states[-1]['receipts'] if r['commandHash']==sha(raw(inp,'CompletionInput')))
        require(state is states[-1] and event is None and rid==expected_rid,6); counts['retries']+=1
    for path,leaf in leaves(base):
        altered=changed(base,path,different(leaf))
        try: data=raw(altered,'InheritedReserveMovementCompletionBase')
        except ValueError: data=encode(altered)
        rejected(lambda data=data:read_base(data,expected)); counts['mutations']+=1
    common=[('priorVersion',),('stateVersion',),('priorPrefix',),('baseHash',),('cycleId',),
        ('configurationHash',),('receiptId',)]
    for i,data in enumerate(events):
        event=json.loads(data); event_leaves=dict(leaves(event)); paths=common[:]
        if i==0: paths.append(('stop','movementReceiptId'))
        elif i==1: paths.append(('randomStateAfter','nextByteCursor'))
        else: paths.extend([('endLocations',0,'locationId'),('progress',0,'receiptId'),('excludedUnits',)])
        for path in paths:
            replacement=([copy.deepcopy(states[-1]['releaseMember']['unit'])]
                if path==('excludedUnits',) else different(event_leaves[path]))
            altered=changed(event,path,replacement)
            try:
                if path!=('receiptId',):
                    altered['receiptId']='pending'; altered['receiptId']=event_receipt(altered,i)
                encoded=raw(altered,TYPES[i])
            except ValueError: encoded=encode(altered)
            rejected(lambda encoded=encoded,i=i:read_event(encoded,base,states[i],inputs[i]),
                f'event {i} path {path}'); counts['mutations']+=1
    terminal_mutations=[
        lambda s:s['releaseMember']['history']['nextMovement'].__setitem__('status','pending'),
        lambda s:s['releaseMember']['history']['nextMovement'].__setitem__('completionReceiptId',None),
        lambda s:s['movementEnd'].__setitem__('ordinal',1),
        lambda s:s['movementEnd'].__setitem__('completionReceiptId','foreign.receipt'),
        lambda s:s['world']['elements'][0]['ammunition'].__setitem__('points',9),
        lambda s:s['randomState'].__setitem__('nextByteCursor',s['randomState']['nextByteCursor']+1),
        lambda s:s.__setitem__('tracks',[])]
    for mutate in terminal_mutations:
        altered=copy.deepcopy(states[-1]); mutate(altered)
        try: data=raw(altered,'InheritedReserveMovementCompletionState')
        except ValueError: data=encode(altered)
        rejected(lambda data=data:read_state(data,base,inputs,events,3)); counts['mutations']+=1
    for bad_events in ([events[1]],[events[2]],events[1:],[events[0],events[2]],list(reversed(events)),
            [events[0],events[0]],events+[events[-1]]):
        bad_inputs=[json.loads(event)['input'] for event in bad_events]
        rejected(lambda ins=bad_inputs,evs=bad_events:replay(base,ins,evs)); counts['boundaries']+=1
    for i,inp in enumerate(inputs):
        for actor in ('axis','commonwealth','system'):
            if actor==inp['actor']: continue
            altered=changed(inp,('actor',),actor)
            rejected(lambda altered=altered,i=i:transition(base,states[i],altered)); counts['boundaries']+=1
        for path,replacement in ((('command','baseHash'),'sha256:'+'f'*64),
            (('command','cycleId'),'sha256:'+'e'*64),(('command','positionId'),'foreign.position'),
            (('command','expectedPriorVersion'),1),(('command','actionId'),'sha256:'+'d'*64)):
            altered=changed(inp,path,replacement)
            rejected(lambda altered=altered,i=i:transition(base,states[i],altered)); counts['boundaries']+=1
    rejected(lambda:transition(base,states[-1],command(base,states[-1],KINDS[2]))); counts['boundaries']+=1
    samples=[(raw(base,'InheritedReserveMovementCompletionBase'),'InheritedReserveMovementCompletionBase')]
    samples += [(data,TYPES[i]) for i,data in enumerate(events)]
    samples.append((raw(states[-1],'InheritedReserveMovementCompletionState'),'InheritedReserveMovementCompletionState'))
    for data,kind in samples:
        value=json.loads(data); variants=[data+b' ',b' '+data,b'\xef\xbb\xbf'+data,
            data.replace(b':',b': ',1),b'{"unknown":0,'+data[1:],encode(dict(reversed(list(value.items()))))]
        for malformed in variants:
            rejected(lambda malformed=malformed,kind=kind:parse(malformed,kind)); counts['raw']+=1
    return counts

def check_fixture(fixture):
    require(set(fixture)=={'contract','cases','sourcePins','goldenMethod'}
        and fixture['contract']=='combat-inherited-reserve-movement-completion-v1'
        and [case['actor'] for case in fixture['cases']]==['axis','commonwealth']
        and fixture['sourcePins']==source_pins(),4)
def main():
    if '--fixture' in sys.argv: print(json.dumps(generated_fixture(),indent=2)); return
    fixture=json.loads(FIXTURE.read_text()); check_fixture(fixture)
    total=dict(readbacks=0,retries=0,mutations=0,raw=0,boundaries=0)
    for case in fixture['cases']:
        for k,v in verify_case(case).items(): total[k]+=v
    print(f"combat inherited Reserve Movement completion v1: {len(fixture['cases'])} traces, 6 events, "
        f"{total['readbacks']} readbacks, {total['retries']} retries, {total['mutations']} mutations, "
        f"{total['raw']} raw rejects, {total['boundaries']} boundary rejects")
if __name__=='__main__': main()
