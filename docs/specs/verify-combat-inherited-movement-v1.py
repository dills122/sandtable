#!/usr/bin/env python3
"""Creation-rooted Move4 contract oracle; no runtime admission or fixture writes."""
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent

def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT/filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

rd = load('inherited_reserve', 'verify-combat-reserve-designation-v1.py')
mov = load('inherited_cost', 'verify-combat-ordinary-movement-v1.py')
world, encode, sha = rd.world, rd.encode, rd.sha
INVENTORY = json.loads((ROOT/'combat-inherited-movement-v1.schema.json').read_text())
FIXTURE = ROOT/'fixtures/combat-inherited-movement-v1.json'
SCHEMA = rd.SCHEMA | {k:[tuple(f.split(':')) for f in v.split()] for k,v in INVENTORY['objects'].items()}

class Invalid(ValueError):
    def __init__(self, code):
        self.code = f'CMB-IMV-{code:03}'
        super().__init__(self.code)

def require(ok, code):
    if not ok: raise Invalid(code)

def typed(v, kind, depth=0):
    require(depth<=32,1)
    if kind=='null': require(v is None,3)
    elif kind=='empty': require(type(v) is list and not v,3)
    elif kind=='OrderedLocations':
        require(type(v) is list and 2<=len(v)<=33,1)
        for x in v: typed(x,'id',depth+1)
    elif kind.endswith('?'):
        if v is not None: typed(v,kind[:-1],depth)
    elif kind in SCHEMA:
        require(type(v) is dict and set(v)=={k for k,_ in SCHEMA[kind]},1)
        for k,t in SCHEMA[kind]: typed(v[k],t,depth+1)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v)<=512,1)
        for item in v: typed(item,kind[:-2],depth+1)
    else:
        try: rd.typed(v,kind,depth)
        except rd.Invalid as error: raise Invalid(1) from error

def canonical(v,kind):
    if kind.endswith('?'): return None if v is None else canonical(v,kind[:-1])
    if kind in SCHEMA: return {k:canonical(v[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'):
        child=kind[:-2]; out=[canonical(x,child) for x in v]
        if child in rd.pre.env.KEYS: out.sort(key=lambda x:tuple(x[k] for k in rd.pre.env.KEYS[child]))
        elif kind=='id[]': out.sort()
        return out
    return v

def raw(v,kind):
    typed(v,kind); data=encode(canonical(v,kind)); require(len(data)<=1048576,1)
    return data

def parse(data,kind):
    require(type(data) is bytes and 0<len(data)<=1048576,1)
    def pairs(items):
        out={}
        for k,v in items: require(k not in out,1); out[k]=v
        return out
    try: value=json.loads(data.decode('utf8'),object_pairs_hook=pairs,
        parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError,UnicodeError,RecursionError) as error: raise Invalid(1) from error
    require(raw(value,kind)==data,8)
    return value

def initial(request,created,preamble,weather_events,stage_events,reserve_events):
    try: state=rd.replay(request,created,preamble,weather_events,stage_events,reserve_events)
    except (rd.Invalid, rd.stage.Invalid, rd.stage.weather.Invalid, rd.pre.Invalid,
            rd.pre.env.Invalid, rd.pre.env.init.Invalid, rd.pre.env.world.Invalid,
            rd.pre.env.inputs.Invalid, rd.pre.env.sequence.Invalid, rd.opening.Invalid) as error:
        raise Invalid(4) from error
    require(state['cycle'] is not None and state['completionReceiptId'] is not None,6)
    require(state['cycle']['ordinal']==1 and state['cycle']['gameTurn']==1
        and state['cycle']['operationStage']==1 and state['cycle']['playerPhaseSlot']=='first-acting-side',3)
    require(state['sequencePosition']==rd.opening.position('movementPositionId')
        and state['cycle']['actingSide']==state['firstActingSide'],4)
    require(state['operationStageWeather'][0]['kind']=='normal'
        and all(e['reserveStatus']=='none' for e in state['world']['elements']),3)
    state.update(tracks=[],actualProgressRefs=[],breakdownFlow=None)
    return canonical(state,'InheritedState')

def command(state,destination):
    unit=state['members'][0]['unit']
    e=next(e for e in state['world']['elements'] if e['elementId']==unit['elementId'])
    return dict(command=dict(contractVersion=2,kind='move-element',creationBinding=state['creationBinding'],
        creationEventHash=state['creationEventHash'],cycleId=state['cycleId'],expectedPriorVersion=state['stateVersion'],
        expectedPositionId=state['sequencePosition']['positionId'],unit=copy.deepcopy(unit),
        originLocationId=e['currentLocationId'],destinationLocationId=destination),actor=state['firstActingSide'])

def authorize(state,inp):
    typed(inp,'InheritedInput'); cmd=inp['command']
    require(cmd['contractVersion']==2 and cmd['kind']=='move-element',3)
    require(inp['actor']==state['cycle']['actingSide']==state['firstActingSide']
        and cmd['unit']==state['members'][0]['unit'],5)
    require(cmd['creationBinding']==state['creationBinding'] and cmd['creationEventHash']==state['creationEventHash']
        and cmd['cycleId']==state['cycleId'] and cmd['expectedPositionId']==state['sequencePosition']['positionId'],4)

def receipt(event):
    unsigned={k:v for k,v in canonical(event,'InheritedEvent').items() if k!='receiptId'}
    return 'imv.'+hashlib.sha256(INVENTORY['domains']['receipt'].encode()+b'\0'+encode(unsigned)).hexdigest()

def _emit(state,inp):
    # Private kernel: public entry points supply only fully reconstructed state.
    authorize(state,inp); cmd=inp['command']; unit=cmd['unit']; w=state['world']; actor=inp['actor']
    require(cmd['expectedPriorVersion']==state['stateVersion'],6)
    require(state['stateVersion']<2**63-1 and len(state['actualProgressRefs'])<32,7)
    e=next(e for e in w['elements'] if e['elementId']==unit['elementId']); op=e['operationalState']
    origin,dest=cmd['originLocationId'],cmd['destinationLocationId']
    require(origin==e['currentLocationId'] and origin in world.GRAPH and dest in world.GRAPH[origin],5)
    facts=next(e for e in world.PACK['elements'] if e['elementId']==unit['elementId'])
    require(facts['mobilityId']=='land.mobility.non-motorized' and facts['breakdownVehicleCohort'] is None
        and facts['baseCapabilityPointAllowance']==10 and facts['placementMode']=='independent'
        and facts['combatClassificationId']=='land.combat-classification.combat-unit'
        and all(c['componentClassId']=='land.combat-component.infantry' for c in facts['components']),3)
    require(e['reserveStatus']=='none' and op['vehicleBreakdownState'] is None and op['movementEnded'] is None
        and all(c['currentToe']>0 for c in e['components']) and e['ammunition']['points']==10,3)
    require(not any(w[k] for k in ('relationships','guards','brokenVehicleLots','futureObligations','settlements')),3)
    require((op['ledgerGameTurn'],op['ledgerOperationStage'])==(1,1)
        and op['capabilityPointsExpended']['denominator']==1,4)
    require(all(x['currentLocationId']!=dest for x in w['elements'] if x is not e)
        and all(x['currentLocationId']!=dest for x in w['representations']),5)
    reps=[r for r in w['representations'] if r['bindingKind']=='independent-element' and r['boundElementIds']==[unit['elementId']]]
    require(len(reps)==1 and reps[0]['currentLocationId']==origin,4); rep=reps[0]
    # Window opens on adjacency BEFORE reaction eligibility filtering. Closed profile admits no such destination.
    require(not any(r['currentLocationId'] in world.GRAPH[dest]
        for r in w['representations'] if any(world.SIDES[u]!=actor for u in r['boundElementIds'])),3)
    try:
        terrain=mov.terrain_cost(origin,dest)
        spent=op['capabilityPointsExpended']['numerator']; after_cp,dp=mov.cost(10,spent,[],terrain=terrain)
    except mov.Invalid as error: raise Invalid(int(error.code[-3:])) from error
    cohesion=op['cohesionLevel']; require(-2**31<=cohesion-dp<2**31,7)
    require(state['members'][0]['spentCp']==world.cp(spent),4)
    version=state['stateVersion']+1
    if state['breakdownFlow'] is None:
        route=dict(routeId='pending',firstMoveStateVersion=version,elementId=unit['elementId'],
            representationId=rep['representationId'],owner=actor,originLocationId=origin,currentLocationId=dest,cohortIds=[])
        identity=dict(domain=INVENTORY['domains']['route'],campaignId=state['campaignId'],rulesetHash=state['rulesetHash'])
        identity.update({k:v for k,v in route.items() if k not in ('routeId','currentLocationId')})
        route['routeId']=sha(encode(identity))
    else:
        route=copy.deepcopy(state['breakdownFlow']['route'])
        require(route['elementId']==unit['elementId'] and route['currentLocationId']==origin,4)
        route['currentLocationId']=dest
    sources=[dict(sourceId='spi-1979-map-a',locator='8.37')]
    event=dict(contractVersion=4,eventType='element-moved',campaignId=state['campaignId'],
        rulesetHash=state['rulesetHash'],stateVersion=version,priorStateVersion=state['stateVersion'],
        fromPositionId=state['sequencePosition']['positionId'],gameTurn=1,operationStage=1,actingSide=actor,
        elementId=unit['elementId'],representationId=rep['representationId'],originLocationId=origin,destinationLocationId=dest,
        mobilityId=facts['mobilityId'],mobilitySources=sources,
        cost=dict(destinationTerrainId='land.terrain.clear',destinationTerrainCost=world.cp(terrain),
            destinationTerrainSources=copy.deepcopy(sources),routeAdjustment=None,crossedHexsideCosts=[],totalCost=world.cp(terrain)),
        capabilityPointsExpendedBefore=world.cp(spent),capabilityPointsExpendedAfter=world.cp(after_cp),
        cohesionBefore=cohesion,cohesionAfter=cohesion-dp,movementEndedAfter=None,
        sequencePosition=copy.deepcopy(state['sequencePosition']),openedReactionWindow=None,breakdownAccounting=[],
        breakdownFlowAfter=dict(kind='moving',route=route),configurationHash=state['configurationHash'],
        creationBinding=state['creationBinding'],creationEventHash=state['creationEventHash'],cycleId=state['cycleId'],
        openingBaseHash=state['openingBaseHash'],completionReceiptId=state['completionReceiptId'],priorPrefix=state['prefix'],
        input=copy.deepcopy(inp),receiptId='pending')
    event['receiptId']=receipt(event); data=raw(event,'InheritedEvent'); rid=event['receiptId']
    after=copy.deepcopy(state); aw=after['world']; moved=next(x for x in aw['elements'] if x['elementId']==unit['elementId'])
    moved['currentLocationId']=dest; moved['operationalState'].update(capabilityPointsExpended=world.cp(after_cp),cohesionLevel=cohesion-dp)
    next(x for x in aw['representations'] if x['representationId']==rep['representationId'])['currentLocationId']=dest
    if dp:
        aw['cohesionCauses'].append(dict(causeId=rid+'.dp',ordinal=len(aw['cohesionCauses'])+1,receiptId=rid,
            elementId=unit['elementId'],gameTurn=1,operationStage=1,kind='ordinary-movement-excess-cp-dp',
            points=dp,before=cohesion,after=cohesion-dp))
    after['members'][0]['spentCp']=world.cp(after_cp)
    if after['tracks']:
        require(after['tracks'][0]['route'][-1]==origin,4); after['tracks'][0]['route'].append(dest)
    else: after['tracks'].append(dict(unit=copy.deepcopy(unit),route=[origin,dest]))
    after.update(stateVersion=version,prefix=rd.pre.env.sequence.prefix_event(state['prefix'],data),
        breakdownFlow=copy.deepcopy(event['breakdownFlowAfter']))
    after['receipts'].append(dict(commandHash=sha(raw(inp,'InheritedInput')),eventHash=sha(data),receiptId=rid,
        actor=actor,stateVersion=version))
    after['actualProgressRefs'].append(dict(eventType='element-moved',receiptId=rid,eventHash=sha(data)))
    return canonical(after,'InheritedState'),data

def read_event(data):
    event=parse(data,'InheritedEvent')
    require(event['contractVersion']==4 and event['eventType']=='element-moved'
        and event['breakdownFlowAfter']['kind']=='moving',3)
    require(event['receiptId']==receipt(event),4)
    return event

def replay(request,created,preamble,weather_events,stage_events,reserve_events,events):
    require(type(events) is list and len(events)<=32,7)
    state=initial(request,created,preamble,weather_events,stage_events,reserve_events)
    for data in events:
        event=read_event(data); state,expected=_emit(state,event['input']); require(data==expected,6)
    return state

def apply(request,created,preamble,weather_events,stage_events,reserve_events,events,inp):
    state=replay(request,created,preamble,weather_events,stage_events,reserve_events,events)
    authorize(state,inp)
    for data in events:
        accepted=read_event(data)['input']
        if accepted['command']['expectedPriorVersion']==inp['command']['expectedPriorVersion']:
            require(raw(inp,'InheritedInput')==raw(accepted,'InheritedInput'),6)
            return state,data,True
    after,data=_emit(state,inp)
    return after,data,False

def read_state(data,request,created,preamble,weather_events,stage_events,reserve_events,events):
    value=parse(data,'InheritedState')
    require(data==raw(replay(request,created,preamble,weather_events,stage_events,reserve_events,events),'InheritedState'),6)
    return value


def source_trace(name):
    case=next(c for c in json.loads(rd.FIXTURE.read_text())['cases'] if c['name']==name)
    result=rd.trace(case); assert rd.goldens(result)==case['goldens']
    q,created,preamble,w,s,_,events,states=result
    # Fixture is independently pinned and its semantic assertions run above.
    return (q,created,preamble,w,s,events),states[-1]

def semantic_red():
    args,opening=source_trace('normal-act-first-none')
    before=initial(*args); inp=command(before,'axis-rear'); after,event=_emit(before,inp)
    e=next(e for e in after['world']['elements'] if e['elementId']=='axis-assault-battalion')
    assert (e['currentLocationId'],e['operationalState']['capabilityPointsExpended'],after['stateVersion'],
        after['prefix']!=opening['prefix'])==('axis-rear',{'numerator':2,'denominator':1},12,True), 'missing real destination/CP/version/prefix effect'

def trace(case):
    args,opened=source_trace(case['predecessorCase']); state=initial(*args)
    states=[state]; inputs=[]; events=[]; expected=case['expected']
    unit=state['members'][0]['unit']; actor=unit['originalSide']; element_id=unit['elementId']
    assert actor==expected['actor']
    for index,dest in enumerate(expected['route'][1:]):
        before=state; inp=command(state,dest); state,event,duplicate=apply(*args,events,inp)
        assert not duplicate; inputs.append(inp); events.append(event); states.append(state)
        e=read_event(event); count=index+1; cp=expected['cp'][index]; cohesion=expected['cohesion'][index]
        moved=next(x for x in state['world']['elements'] if x['elementId']==element_id)
        assert moved['currentLocationId']==dest and moved['operationalState']['capabilityPointsExpended']==world.cp(cp)
        assert moved['operationalState']['cohesionLevel']==cohesion
        assert e['cost']==dict(destinationTerrainId='land.terrain.clear',destinationTerrainCost=world.cp(2),
            destinationTerrainSources=[dict(sourceId='spi-1979-map-a',locator='8.37')],routeAdjustment=None,
            crossedHexsideCosts=[],totalCost=world.cp(2))
        assert e['capabilityPointsExpendedBefore']==world.cp(cp-2) and e['capabilityPointsExpendedAfter']==world.cp(cp)
        assert e['movementEndedAfter'] is None and e['openedReactionWindow'] is None and e['breakdownAccounting']==[]
        assert e['breakdownFlowAfter']['kind']=='moving'
        assert state['stateVersion']==11+count and len(state['receipts'])==10+count
        assert state['prefix']==rd.pre.env.sequence.prefix_event(before['prefix'],event)!=before['prefix']
        assert state['sequencePosition']['contractVersion']==5 and state['sequencePosition']['activeSide'] is None
        assert state['cycle']['actingSide']==actor and state['cycle']['ordinal']==1
        assert state['tracks']==[dict(unit=unit,route=expected['route'][:count+1])]
        expected_members=copy.deepcopy(opened['members']); expected_members[0]['spentCp']=world.cp(cp)
        assert state['members']==expected_members
        ew=copy.deepcopy(opened['world']); em=next(x for x in ew['elements'] if x['elementId']==element_id)
        em['currentLocationId']=dest; em['operationalState'].update(capabilityPointsExpended=world.cp(cp),cohesionLevel=cohesion)
        next(r for r in ew['representations'] if r['boundElementIds']==[element_id])['currentLocationId']=dest
        ew['cohesionCauses']=[dict(causeId=read_event(events[j])['receiptId']+'.dp',ordinal=j-4,
            receiptId=read_event(events[j])['receiptId'],elementId=element_id,gameTurn=1,operationStage=1,
            kind='ordinary-movement-excess-cp-dp',points=2,before=0 if j==5 else -2,after=-2 if j==5 else -4)
            for j in range(5,count)]
        assert state['world']==ew
        assert all(state[k]==opened[k] for k in opened if k not in ('stateVersion','prefix','world','members','receipts'))
        assert state['receipts'][:10]==opened['receipts']
        assert state['actualProgressRefs']==[dict(eventType='element-moved',receiptId=read_event(d)['receiptId'],eventHash=sha(d)) for d in events]
        route=e['breakdownFlowAfter']['route']
        assert route['firstMoveStateVersion']==12 and route['originLocationId']==expected['route'][0]
        assert route['currentLocationId']==dest and route['owner']==actor and route['cohortIds']==[]
        assert route['routeId']==read_event(events[0])['breakdownFlowAfter']['route']['routeId']
        assert read_state(raw(state,'InheritedState'),*args,events)==state
    for index,inp in enumerate(inputs):
        retried,old,duplicate=apply(*args,events,inp)
        assert duplicate and old==events[index] and retried==states[-1]
    return args,inputs,events,states

def goldens(result):
    args,inputs,events,states=result
    q,created,pre,w,s,res=args
    values=[('request',encode(q)),('predecessor-records',encode([d.decode('ascii') for d in [created]+pre+w+s+res]))]
    values += [(f'input-{i+1}',raw(v,'InheritedInput')) for i,v in enumerate(inputs)]
    values += [(f'event-{i+1}',v) for i,v in enumerate(events)]
    values += [(f'state-{i+11}',raw(v,'InheritedState')) for i,v in enumerate(states)]
    return {k:dict(bytes=len(v),sha256=sha(v)) for k,v in values}

def rejected(fn):
    try: fn()
    except Invalid: return
    raise AssertionError('expected CMB-IMV rejection')

def changed(value,path,replacement):
    value=copy.deepcopy(value); node=value
    for key in path[:-1]: node=node[key]
    node[path[-1]]=replacement
    return value

def leaves(value,path=()):
    if type(value) is dict:
        for k,v in value.items(): yield from leaves(v,path+(k,))
    elif type(value) is list:
        if not value: yield path,value
        for i,v in enumerate(value): yield from leaves(v,path+(i,))
    else: yield path,value

def different(value):
    if value is None: return 'unexpected'
    if type(value) is bool: return not value
    if type(value) is int: return value+1
    if type(value) is list: return ['unexpected']
    if value.startswith('sha256:'): return 'sha256:'+'f'*64 if value!='sha256:'+'f'*64 else 'sha256:'+'e'*64
    return value+'x'

def verify(result):
    args,inputs,events,states=result; count=dict(cuts=0,retries=len(inputs),mutations=0,raw=0,boundaries=0)
    for i,state in enumerate(states):
        assert replay(*args,events[:i])==state; count['cuts']+=1
    frozen=raw(states[-1],'InheritedState')
    actor=states[0]['firstActingSide']; other='commonwealth' if actor=='axis' else 'axis'
    # Each challenge below runs once per owner, not once per every historical cut.
    cases=[(('actor',),other),(('actor',),'system'),(('command','contractVersion'),1),
        (('command','kind'),'complete-movement-segment'),(('command','kind'),'resolve-breakdown'),
        (('command','kind'),'react-move'),(('command','unit','elementId'),other+'-assault-battalion'),
        (('command','unit','originalSide'),other),(('command','unit','creationBinding'),'creation.foreign'),
        (('command','originLocationId'),'axis-supply'),(('command','destinationLocationId'),'missing-location'),
        (('command','destinationLocationId'),other+'-supply'),
        (('command','destinationLocationId'),'assault-east' if actor=='axis' else 'assault-west'),
        (('command','expectedPriorVersion'),10),(('command','expectedPriorVersion'),12),
        (('command','cycleId'),'sha256:'+'f'*64),(('command','creationEventHash'),'sha256:'+'f'*64),
        (('command','creationBinding'),'creation.foreign'),(('command','expectedPositionId'),'wrong.position')]
    for path,value in cases:
        rejected(lambda p=path,v=value:apply(*args,[],changed(inputs[0],p,v)));count['boundaries']+=1
    # Return-to-origin adjacency triggers Reaction even with no admitted positive family.
    back=command(states[1],inputs[0]['command']['originLocationId'])
    rejected(lambda:apply(*args,events[:1],back));count['boundaries']+=1
    # Eighth leg is structurally allowed (32 records), rejected by ordinary CP15 ceiling.
    eighth=command(states[-1],actor+'-supply')
    rejected(lambda:apply(*args,events,eighth));count['boundaries']+=1
    rejected(lambda:mov_cost_ceiling());count['boundaries']+=1
    conflict=changed(inputs[0],('command','destinationLocationId'),actor+'-supply')
    rejected(lambda:apply(*args,events,conflict));count['boundaries']+=1
    rejected(lambda:apply(*args,events,changed(inputs[0],('actor',),other)));count['boundaries']+=1
    rejected(lambda:replay(*args,events+[events[0]]));count['boundaries']+=1
    # Representative first/DP/last event cuts; every field leaf on first event is challenged.
    for i in (0,5,6):
        event=read_event(events[i]); paths=list(leaves(event))
        if i: paths=[(p,v) for p,v in paths if p[0] in ('capabilityPointsExpendedAfter','cohesionAfter','breakdownFlowAfter','priorPrefix','receiptId')]
        for path,value in paths:
            bad=changed(event,path,different(value))
            # A re-signed structurally valid alteration still cannot establish authority.
            try:
                if path!=('receiptId',): bad['receiptId']=receipt(bad)
                data=raw(bad,'InheritedEvent')
            except Invalid: data=encode(bad)
            rejected(lambda d=data,n=i:replay(*args,events[:n]+[d])); count['mutations']+=1
    # Independent cache cannot replace full history or patch a valid projection.
    paths=[p for p,_ in leaves(states[-1]) if p[0] in ('cycle','members','actualProgressRefs','tracks','breakdownFlow')]
    paths += [('world','elements',0,'currentLocationId'),('world','representations',0,'currentLocationId'),
        ('world','cohesionCauses',0,'points'),('randomState','nextByteCursor'),('prefix',),('stateVersion',)]
    for path in paths:
        node=states[-1]
        for key in path: node=node[key]
        bad=changed(states[-1],path,different(node))
        try: data=raw(bad,'InheritedState')
        except Invalid: data=encode(bad)
        rejected(lambda d=data:read_state(d,*args,events));count['mutations']+=1
    # Missing creation-to-opening records, and altered canonical predecessor records.
    for index in (1,2,3,4,5):
        bad=list(copy.deepcopy(args))
        if index==1: bad[index]=args[index]+b' '
        else: bad[index]=bad[index][:-1]
        rejected(lambda a=bad:replay(*a,events));count['boundaries']+=1
    for index in (2,3,4,5):
        bad=list(copy.deepcopy(args)); value=json.loads(bad[index][-1]);value['stateVersion']+=1
        bad[index][-1]=encode(value)
        rejected(lambda a=bad:replay(*a,events));count['boundaries']+=1
    for kind,value in [('InheritedEvent',read_event(events[0])),('InheritedInput',inputs[0]),('InheritedState',states[-1])]:
        data=raw(value,kind)
        variants=[data+b' ',b' '+data,b'\xef\xbb\xbf'+data,b'\xff',data[:-1],b'{}',
            b'{"unknown":0,'+data[1:],b'{"contractVersion":1,'+data[1:],
            encode(dict(reversed(list(value.items())))),data.replace(b'"contractVersion":',b'"contractVersion": ',1),
            data.replace(b'"contractVersion":'+str(value['contractVersion']).encode(),b'"contractVersion":true',1)
            if 'contractVersion' in value else data.replace(b'"command":',b'"command":null,"ignored":',1)]
        for bad in variants:
            rejected(lambda d=bad,k=kind:parse(d,k));count['raw']+=1
    assert frozen==raw(states[-1],'InheritedState')
    return count

def mov_cost_ceiling():
    try: mov.cost(10,14,[],terrain=2)
    except mov.Invalid as error: raise Invalid(5) from error

def fixture_shape(fixture):
    require(fixture['contractVersion']==1 and fixture['contract']=='combat-inherited-movement-v1',9)
    require(len(fixture['cases'])==2 and {c['name'] for c in fixture['cases']}==
        {'axis-ordinary-clear-route','commonwealth-ordinary-clear-route'},9)
    require({c['expected']['actor'] for c in fixture['cases']}=={'axis','commonwealth'},9)
    require({c['predecessorCase'] for c in fixture['cases']}=={'normal-act-first-none','normal-act-last-none'},9)

def main():
    fixture=json.loads(FIXTURE.read_text()); fixture_shape(fixture); repo=ROOT.parent.parent
    for path,digest in fixture['sourceHashes'].items(): require(sha((repo/path).read_bytes())==digest,9)
    totals=dict(cuts=0,retries=0,mutations=0,raw=0,boundaries=0); results=[]
    for case in fixture['cases']:
        result=trace(case); results.append(result); assert goldens(result)==case['goldens']
        for k,v in verify(result).items(): totals[k]+=v
    # Admission rejects complete real nonnormal Weather and Reserve-I histories.
    for name in ('rainstorm-act-first-none','hot-act-first-none','sandstorm-act-first-none','normal-act-first-I','normal-act-last-I'):
        args,_=source_trace(name); rejected(lambda a=args:initial(*a));totals['boundaries']+=1
    for i,(args,inputs,events,states) in enumerate(results):
        foreign=results[1-i]
        rejected(lambda:replay(*args,foreign[2])); totals['boundaries']+=1
        rejected(lambda:read_state(raw(foreign[3][-1],'InheritedState'),*args,events)); totals['boundaries']+=1
        bad=list(copy.deepcopy(args)); bad[0]['campaignId']='foreign.campaign'
        rejected(lambda:replay(*bad,events)); totals['boundaries']+=1
        bad=list(copy.deepcopy(args)); opening=rd.read_event(bad[-1][-1]); opening['cycle']['ordinal']=2
        opening['cycleId']=sha(rd.opening.steps.seq.identity(opening['cycle'],'Authority',states[0]['firstActingSide']))
        unsigned={k:v for k,v in rd.opening.canonical(opening,'OpeningEvent').items() if k!='receiptId'}
        opening['receiptId']='rc.'+rd.opening.steps.digest(rd.opening.INVENTORY['domains']['receipt'],encode(unsigned))
        bad[-1][-1]=rd.opening.raw(opening,'OpeningEvent')
        rejected(lambda:replay(*bad,events)); totals['boundaries']+=1
    semantic_red()
    print('CMB-IMV PASS',json.dumps(dict(traces=len(results),moves=sum(len(r[2]) for r in results),
        sourcePins=len(fixture['sourceHashes']),**totals),sort_keys=True))

if __name__=='__main__': main()
