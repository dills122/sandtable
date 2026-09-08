#!/usr/bin/env python3
"""D2a contract oracle; synthetic release/repeat gap, no runtime Movement admission."""
import copy
import importlib.util
import json
import sys
from functools import lru_cache
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('result_contract',ROOT/'verify-combat-result-settlement-v1.py')
r=importlib.util.module_from_spec(spec);spec.loader.exec_module(r)
world,steps=r.world,r.steps
encode,sha=r.encode,r.sha
FIXTURE=ROOT/'fixtures/combat-ordinary-movement-v1.json'
INVENTORY=json.loads((ROOT/'combat-ordinary-movement-v1.schema.json').read_text())
SCHEMA=r.SCHEMA|{k:[tuple(f.split(':')) for f in v.split()] for k,v in INVENTORY['objects'].items()}

class Invalid(ValueError):
    def __init__(self,code):self.code=f'CMB-MOV-{code:03}';super().__init__(self.code)

def require(ok,code):
    if not ok:raise Invalid(code)

def typed(v,kind,depth=0):
    require(depth<=32,1)
    if kind.endswith('?'):
        if v is not None:typed(v,kind[:-1],depth)
    elif kind in SCHEMA:
        require(type(v) is dict and set(v)=={k for k,_ in SCHEMA[kind]},1)
        for k,t in SCHEMA[kind]:typed(v[k],t,depth+1)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v)<=512,1)
        for x in v:typed(x,kind[:-2],depth+1)
    else:
        try:r.typed(v,kind,depth=depth)
        except r.Invalid as e:raise Invalid(int(e.code[-3:])) from e

def canonical(v,kind):
    if kind.endswith('?'):return None if v is None else canonical(v,kind[:-1])
    if kind in SCHEMA:return {k:canonical(v[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'):return [canonical(x,kind[:-2]) for x in v]
    return v

def raw(v,kind):
    typed(v,kind);data=encode(canonical(v,kind));require(len(data)<=1048576,1);return data

def parse(data,kind):
    require(type(data) is bytes and 0<len(data)<=1048576,1)
    def pairs(items):
        out={}
        for k,v in items:require(k not in out,1);out[k]=v
        return out
    try:v=json.loads(data.decode('utf8'),object_pairs_hook=pairs,parse_constant=lambda _:(_ for _ in ()).throw(ValueError()))
    except (ValueError,UnicodeError,RecursionError) as e:raise Invalid(1) from e
    require(raw(v,kind)==data,8);return v

def cost(cpa,spent,kinds,release=None,terrain=1):
    require(type(cpa) is int and 1<=cpa<=2147483647 and type(spent) is int and 0<=spent<2**63,2)
    require(type(kinds) is list and len(kinds)<=512 and all(k in ('contact','engaged') for k in kinds),3)
    require(release in (None,'I','II') and type(terrain) is int and terrain in (1,2),3)
    ceiling=3*cpa//2 if release is None else cpa if release=='I' else cpa//2
    total=terrain+max(({'contact':2,'engaged':4}[k] for k in kinds),default=0)
    after=spent+total;require(after<2**63,7);require(after<=ceiling,5)
    return after,max(0,after-cpa)-max(0,spent-cpa)

def terrain_cost(origin,dest):
    edges=[e for e in world.PACK['edges'] if {e['firstLocationId'],e['secondLocationId']}=={origin,dest}]
    nodes=[n for n in world.PACK['locations'] if n['locationId']==dest]
    require(len(edges)==1 and not edges[0]['features'] and len(nodes)==1 and nodes[0]['terrainId']=='land.terrain.clear',3)
    return 2

def key(unit):return unit['creationBinding'],unit['originalSide'],unit['elementId']

def affected(relations,unit):
    """Pair membership kernel; topology does not enroll new endpoints."""
    typed(unit,'UnitKey');require(type(relations) is list and len(relations)<=512,1)
    ids=set();out=[]
    for rel in relations:
        typed(rel,'Relationship');require(rel['relationId'] not in ids,4);ids.add(rel['relationId'])
        require(rel['kind'] in ('contact','engaged') and rel['attacker']['originalSide']!=rel['defender']['originalSide'],3)
        require(rel['attacker']['creationBinding']==rel['defender']['creationBinding']==unit['creationBinding'],4)
        require((rel['endedByReceiptId'] is None and rel['endingCause'] is None) if rel['active'] else
                (rel['endedByReceiptId'] is not None and rel['endingCause'] is not None),4)
        if rel['active'] and unit in (rel['attacker'],rel['defender']):
            out.append({k:copy.deepcopy(rel[k]) for k in ('relationId','creationReceiptId','kind','attacker','defender')}|dict(endingCause='ordinary-break-off'))
    return sorted(out,key=lambda x:x['relationId'])

@lru_cache(maxsize=32)
def source_base(result_name,pre_cp,side):
    case=copy.deepcopy(next(x for x in json.loads(r.FIXTURE.read_text())['cases'] if x['name']==result_name))
    if pre_cp is not None:case['preAssaultCp']=list(pre_cp)
    case['attackerSide']=side
    ctx,settled,inputs,events=r.trace_case(case)
    assert r.replay(ctx,inputs,events)==settled
    b=ctx['base']['boundary'];cycle=copy.deepcopy(b['cycle'])
    # Explicit test-only stand-in for the unimplemented release/repeat suffix; not a canonical event.
    gap=sha(b'isolated-unimplemented-release-repeat\0'+r.raw(settled,'ResultState'))
    cycle.update(ordinal=2,openedAuthorityVersion=settled['stateVersion']+10,openingPrefix=gap)
    position=copy.deepcopy(next(p for p in steps.seq.expected_catalog()['positions'] if p['positionId']==steps.edge(b)['movementPositionId']))
    position['activeSide']=side
    base=dict(contractVersion=1,profile='isolated-next-movement',settledStateHash=sha(r.raw(settled,'ResultState')),
        settlementCaReceiptId=settled['caCompletionReceiptId'],cycle=cycle,firstActingSide=side,position=position,
        stateVersion=cycle['openedAuthorityVersion'],prefix=gap,world=copy.deepcopy(settled['world']),
        randomState=copy.deepcopy(settled['randomState']),attackHistory=copy.deepcopy(ctx['committed']['attackHistory']))
    raw(base,'MovementBase');return base

def base_for(case,side='axis'):
    return copy.deepcopy(source_base(case['resultCase'],None if case['preAssaultCp'] is None else tuple(case['preAssaultCp']),side))

def read_base(data,case,side='axis'):
    parse(data,'MovementBase');expected=base_for(case,side);require(data==raw(expected,'MovementBase'),4);return expected

def cycle_id(base):
    try:return sha(steps.seq.identity(base['cycle'],'Authority',base['firstActingSide']))
    except steps.seq.Invalid as e:raise Invalid(4) from e

def initial(base):
    raw(base,'MovementBase');require(base['contractVersion']==1 and base['profile']=='isolated-next-movement',3)
    return dict(contractVersion=1,baseHash=sha(raw(base,'MovementBase')),stateVersion=base['stateVersion'],prefix=base['prefix'],
        world=copy.deepcopy(base['world']),randomState=copy.deepcopy(base['randomState']),attackHistory=copy.deepcopy(base['attackHistory']),tracks=[],receipts=[])

def command(base,state,unit,dest):
    e=next(e for e in state['world']['elements'] if e['elementId']==unit['elementId'])
    return dict(command=dict(contractVersion=1,kind='move',cycleId=cycle_id(base),positionId=base['position']['positionId'],
        expectedPriorVersion=state['stateVersion'],unit=copy.deepcopy(unit),originLocationId=e['currentLocationId'],destinationLocationId=dest),actor=unit['originalSide'])

def transition(base,prior,inp):
    """Internal transition on replay-derived state. Persisted inputs use read_base/read_state."""
    typed(prior,'MovementState');typed(inp,'MovementInput');cmd=inp['command'];actor=inp['actor'];unit=cmd['unit'];cycle=base['cycle']
    require(prior['contractVersion']==cmd['contractVersion']==1 and cmd['kind']=='move',3)
    require(prior['baseHash']==sha(raw(base,'MovementBase')),4)
    require(actor==cycle['actingSide']==unit['originalSide'] and actor in ('axis','commonwealth'),4)
    require(unit['creationBinding']==prior['world']['creationBinding'] and cmd['cycleId']==cycle_id(base)
            and cmd['positionId']==base['position']['positionId'],4)
    ch=sha(raw(cmd,'MovementCommand'))
    receipt=next((x for x in prior['receipts'] if x['commandHash']==ch and x['actor']==actor),None)
    if receipt:return prior,None,receipt['receiptId']
    require(prior['stateVersion']<2**63-1 and len(prior['receipts'])<32,7)
    require(cmd['expectedPriorVersion']==prior['stateVersion'],6)
    matches=[e for e in prior['world']['elements'] if e['elementId']==unit['elementId'] and world.SIDES.get(e['elementId'])==actor]
    require(len(matches)==1,4);e=matches[0];op=e['operationalState'];origin,dest=cmd['originLocationId'],cmd['destinationLocationId']
    require(e['currentLocationId']==origin and origin in world.GRAPH and dest in world.GRAPH[origin],5)
    facts=next(x for x in world.PACK['elements'] if x['elementId']==e['elementId'])
    require(facts['baseCapabilityPointAllowance']==10 and facts['breakdownVehicleCohort'] is None,3)
    require(e['reserveStatus']=='none' and op['vehicleBreakdownState'] is None and op['movementEnded'] is None,3)
    require((op['ledgerGameTurn'],op['ledgerOperationStage'])==(cycle['gameTurn'],cycle['operationStage']),4)
    require(e['components'][0]['currentToe']>0 and e['ammunition']['points']==0,3)
    require(not prior['world']['brokenVehicleLots'] and all(x['relationships'] is not None for x in prior['world']['settlements']),3)
    require(all(other['currentLocationId']!=dest for other in prior['world']['elements'] if other is not e)
            and all(g['currentLocationId']!=dest for g in prior['world']['guards']),5)
    reps=[x for x in prior['world']['representations'] if x['bindingKind']=='independent-element' and x['boundElementIds']==[e['elementId']]]
    require(len(reps)==1 and reps[0]['currentLocationId']==origin,4)
    require(op['capabilityPointsExpended']['denominator']==1,3);spent=op['capabilityPointsExpended']['numerator']
    ends=affected(prior['world']['relationships'],unit)
    require(all((x['gameTurn'],x['operationStage'])==(cycle['gameTurn'],cycle['operationStage']) for x in prior['world']['relationships'] if x['active']),4)
    terrain=terrain_cost(origin,dest);after,dp=cost(10,spent,[x['kind'] for x in ends],terrain=terrain);cohesion=op['cohesionLevel'];require(cohesion-dp>=-2**31,7)
    costs=dict(terrainCost=terrain,breakOffCost=after-spent-terrain,totalCost=after-spent,baseCpa=10,voluntaryCeiling=15,
        beforeCp=spent,afterCp=after,excessCpDp=dp,beforeCohesion=cohesion,afterCohesion=cohesion-dp)
    effect=dict(kind='ordinary-move',unit=copy.deepcopy(unit),representationId=reps[0]['representationId'],originLocationId=origin,
        destinationLocationId=dest,costs=costs,endedMemberships=ends)
    event=dict(contractVersion=1,eventType='combat-cycle-element-moved',author=actor,campaignId=cycle['campaignId'],rulesetHash=cycle['rulesetHash'],
        configurationHash=cycle['admittedPolicyBundleDigest'],cycleId=cmd['cycleId'],positionId=cmd['positionId'],baseHash=prior['baseHash'],
        priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],input=canonical(inp,'MovementInput'),effect=canonical(effect,'MovementEffect'))
    rid='mov.'+steps.digest(INVENTORY['domains']['receipt'],encode(event));event['receiptId']=rid;data=raw(event,'MovementEvent')
    state=copy.deepcopy(prior);w=state['world'];moved=next(x for x in w['elements'] if x['elementId']==e['elementId'])
    moved['currentLocationId']=dest;moved['operationalState']['capabilityPointsExpended']=world.cp(after);moved['operationalState']['cohesionLevel']=cohesion-dp
    next(x for x in w['representations'] if x['representationId']==reps[0]['representationId'])['currentLocationId']=dest
    for rel in w['relationships']:
        if rel['relationId'] in {x['relationId'] for x in ends}:rel.update(active=False,endedByReceiptId=rid,endingCause='ordinary-break-off')
    if dp:
        require(len(w['cohesionCauses'])<512,7)
        w['cohesionCauses'].append(dict(causeId=rid+'.dp',ordinal=len(w['cohesionCauses'])+1,receiptId=rid,elementId=e['elementId'],
            gameTurn=cycle['gameTurn'],operationStage=cycle['operationStage'],kind='ordinary-movement-excess-cp-dp',points=dp,before=cohesion,after=cohesion-dp))
    track=next((t for t in state['tracks'] if t['unit']==unit),None)
    if track:require(track['route'][-1]==origin,6);track['route'].append(dest)
    else:state['tracks'].append(dict(unit=copy.deepcopy(unit),route=[origin,dest]));state['tracks'].sort(key=lambda t:key(t['unit']))
    state['stateVersion']=event['stateVersion'];state['prefix']=steps.seq.prefix_event(prior['prefix'],data)
    state['receipts'].append(dict(commandHash=ch,eventHash=sha(data),receiptId=rid,actor=actor,stateVersion=state['stateVersion']))
    raw(state,'MovementState');return state,data,rid

def read_event(data,base,prior,inp):
    parse(data,'MovementEvent');after,expected,_=transition(base,prior,inp);require(data==expected,6);return after

def replay(base,case,inputs,events,length=None,side='axis'):
    require(type(inputs) is list and type(events) is list and len(inputs)==len(events)<=32,1)
    require(length is None or type(length) is int and 0<=length<=len(events),2)
    read_base(raw(base,'MovementBase'),case,side);state=initial(base)
    for inp,event in zip(inputs[:length],events[:length]):state=read_event(event,base,state,inp)
    return state

def read_state(data,base,case,inputs,events,length=None,side='axis'):
    parse(data,'MovementState');expected=replay(base,case,inputs,events,length,side);require(data==raw(expected,'MovementState'),6);return expected

def rejected(call,code=None):
    try:call()
    except Invalid as e:
        if code is not None:assert e.code==f'CMB-MOV-{code:03}',(e.code,code)
    else:raise AssertionError('invalid movement admitted')

def trace(case,side='axis'):
    base=base_for(case,side);state=initial(base);ins=[];events=[]
    mover=next(e for e in state['world']['elements'] if world.SIDES[e['elementId']]==side);unit=world.unit(mover,state['world']['creationBinding'])
    for i,expected in enumerate(case['expected']):
        current=next(e for e in state['world']['elements'] if e['elementId']==unit['elementId'])
        # First move toward own supply anchor; second reverses that edge, proving inactive pair stays inactive.
        dest=world.init.content.route(world.GRAPH,current['currentLocationId'],world.ANCHORS[side])[1] if i==0 else ins[0]['command']['originLocationId']
        inp=command(base,state,unit,dest);before=copy.deepcopy(state);after,event,rid=transition(base,state,inp);eff=json.loads(event)['effect']
        assert eff['costs']['afterCp']==expected['afterCp'] and eff['costs']['excessCpDp']==expected['dp']
        assert len(eff['endedMemberships'])==expected['ended']
        assert state==before and after['randomState']==before['randomState'] and after['attackHistory']==before['attackHistory']
        for field in ('settlements','custodyLots','guards','replacementEntitlements','futureObligations','brokenVehicleLots'):
            assert after['world'][field]==before['world'][field]
        changed=next(x for x in after['world']['elements'] if x['elementId']==unit['elementId'])
        for field in ('components','ammunition','readiness','reserveStatus','sourceParentFormationId','currentParentFormationId'):
            assert changed[field]==current[field]
        assert changed['operationalState']['cohesionLevel']==current['operationalState']['cohesionLevel']-expected['dp']
        assert changed['currentLocationId']==dest
        assert [e for e in after['world']['elements'] if e['elementId']!=unit['elementId']]==[e for e in before['world']['elements'] if e['elementId']!=unit['elementId']]
        assert after['world']['cohesionCauses'][:len(before['world']['cohesionCauses'])]==before['world']['cohesionCauses']
        assert len(after['world']['cohesionCauses'])==len(before['world']['cohesionCauses'])+int(expected['dp']>0)
        assert all(x['endedByReceiptId']==rid for x in after['world']['relationships'] if x['relationId'] in {e['relationId'] for e in eff['endedMemberships']})
        ins.append(inp);events.append(event);state=after
    return base,state,ins,events

def kernel_tests(f):
    for v in f['costVectors']:assert cost(v['cpa'],v['spent'],v['relations'],v['release'],v['terrainCost'])==tuple(v['expected'])
    for args in f['rejectedCosts']:rejected(lambda:cost(*args),5)
    for args in [(True,0,[],None),(10,-1,[],None),(10,1.0,[],None),(10,0,['unknown'],None)]:rejected(lambda:cost(*args))
    # Enumerate against the independent retained source arithmetic, including duplicated/mixed memberships.
    checked=0
    for spent in range(21):
        for kinds in ([],['contact'],['engaged'],['contact','engaged'],['engaged','engaged']):
            for release in (None,'I','II'):
                for terrain in (1,2):
                    try:expected=world.source.move_cost(spent,kinds,terrain=terrain,release=release)
                    except ValueError:rejected(lambda:cost(10,spent,kinds,release,terrain),5)
                    else:assert cost(10,spent,kinds,release,terrain)==expected
                    checked+=1
    creation='creation.'+'1'*64
    a=dict(creationBinding=creation,originalSide='axis',elementId='unit.a');b=dict(creationBinding=creation,originalSide='commonwealth',elementId='unit.b')
    c=a|dict(elementId='unit.c');d=b|dict(elementId='unit.d')
    def rel(n,kind,x,y):return dict(relationId='relation.'+n,creationReceiptId='receipt.'+n,kind=kind,attacker=x,defender=y,gameTurn=1,operationStage=1,active=True,endedByReceiptId=None,endingCause=None)
    relations=[rel('a','contact',a,b),rel('b','engaged',a,d),rel('c','contact',c,d)]
    before=copy.deepcopy(relations);ends=affected(relations,a)
    assert [x['relationId'] for x in ends]==['relation.a','relation.b'] and cost(10,5,[x['kind'] for x in ends])==(10,0)
    for x in relations:
        if x['relationId'] in {e['relationId'] for e in ends}:x.update(active=False,endedByReceiptId='move.1',endingCause='ordinary-break-off')
    assert relations[2]==before[2] and not affected(relations,a) and not affected(relations,b)
    assert len(affected(relations,d))==1 and affected(relations,a|dict(elementId='new-arrival'))==[]
    rejected(lambda:affected(before+[before[0]],a),4)
    return checked

def checks(f):
    cuts=mutations=raws=0;goldens=[]
    for case in f['cases']:
        for side in ('axis','commonwealth'):
            base,final,ins,events=trace(case,side);prior=initial(base)
            assert read_state(raw(prior,'MovementState'),base,case,ins,events,0,side)==prior;cuts+=1
            for i,(inp,data) in enumerate(zip(ins,events)):
                before=copy.deepcopy(prior);after=read_event(data,base,prior,inp)
                assert read_state(raw(after,'MovementState'),base,case,ins,events,i+1,side)==after;cuts+=1
                retry,new,rid=transition(base,after,inp);assert retry is after and new is None and rid==after['receipts'][-1]['receiptId']
                for field in ('priorVersion','stateVersion','priorPrefix','baseHash','cycleId','receiptId','configurationHash'):
                    v=json.loads(data);v[field]=v[field]+1 if type(v[field]) is int else ('mov.' if field=='receiptId' else 'sha256:')+'0'*64
                    rejected(lambda:read_event(encode(v),base,prior,inp));mutations+=1
                for field in ('breakOffCost','afterCp','excessCpDp','afterCohesion'):
                    v=json.loads(data);v['effect']['costs'][field]+=1;rejected(lambda:read_event(encode(v),base,prior,inp));mutations+=1
                for mutate in (lambda v:v['world']['elements'][0]['ammunition'].__setitem__('points',9),
                    lambda v:v['randomState'].__setitem__('nextByteCursor',v['randomState']['nextByteCursor']+1),
                    lambda v:v['attackHistory'].clear(),lambda v:v['receipts'].clear(),lambda v:v['tracks'].clear(),
                    lambda v:v['world']['elements'][0]['operationalState']['capabilityPointsExpended'].__setitem__('numerator',0),
                    lambda v:v['world']['representations'][0].__setitem__('currentLocationId','wrong.location'),
                    lambda v:v['world']['elements'][0]['components'][0].__setitem__('currentToe',11)):
                    v=copy.deepcopy(after);mutate(v);rejected(lambda:read_state(raw(v,'MovementState'),base,case,ins,events,i+1,side));mutations+=1
                for field in ('relationships','cohesionCauses','futureObligations','replacementEntitlements','guards'):
                    if after['world'][field]:
                        v=copy.deepcopy(after);v['world'][field].clear();rejected(lambda:read_state(raw(v,'MovementState'),base,case,ins,events,i+1,side));mutations+=1
                for bad in (data+b'\n',b'\xef\xbb\xbf'+data,data[:-1],data.replace(b'"contractVersion":1',b'"contractVersion":1.0',1),b'{"extra":0,'+data[1:],data.replace(b'"author":',b'"author":"axis","author":',1)):
                    rejected(lambda:read_event(bad,base,prior,inp));raws+=1
                assert prior==before;prior=after
            retry,new,_=transition(base,final,ins[0]);assert retry is final and new is None
            rejected(lambda:replay(base,case,ins,events+[events[0]],side=side),1)
            rejected(lambda:read_state(raw(final,'MovementState'),base,case,ins[:-1],events[:-1],side=side),6)
            v=copy.deepcopy(base);v['world']['elements'][0]['ammunition']['points']=1;rejected(lambda:read_base(raw(v,'MovementBase'),case,side),4)
            for mutate in (lambda v:v.__setitem__('actor','system'),lambda v:v['command'].__setitem__('expectedPriorVersion',base['stateVersion']-1),
                lambda v:v['command'].__setitem__('destinationLocationId',v['command']['originLocationId']),
                lambda v:v['command']['unit'].__setitem__('creationBinding','creation.'+'0'*64)):
                v=copy.deepcopy(ins[0]);mutate(v);rejected(lambda:transition(base,initial(base),v));mutations+=1
            goldens.append(dict(case=case['name'],side=side,baseHash=sha(raw(base,'MovementBase')),events=[d.decode() for d in events],
                finalHash=sha(raw(final,'MovementState')),finalBytes=len(raw(final,'MovementState'))))
    return cuts,mutations,raws,goldens

def guard_checks(f):
    case=f['cases'][1];base,final,ins,events=trace(case);first=initial(base);unit=ins[0]['command']['unit'];checks=0
    def moving(s):return next(e for e in s['world']['elements'] if e['elementId']==unit['elementId'])
    for mutate,code in (
        (lambda s:s.__setitem__('stateVersion',2**63-1),7),
        (lambda s:moving(s)['operationalState'].__setitem__('cohesionLevel',-2**31),7),
        (lambda s:moving(s).__setitem__('reserveStatus','I'),3),
        (lambda s:moving(s)['operationalState']['capabilityPointsExpended'].__setitem__('denominator',2),3),
        (lambda s:moving(s)['operationalState']['capabilityPointsExpended'].__setitem__('numerator',10),5)):
        s=copy.deepcopy(first);mutate(s);before=copy.deepcopy(s);inp=copy.deepcopy(ins[0]);inp['command']['expectedPriorVersion']=s['stateVersion']
        rejected(lambda:transition(base,s,inp),code);assert s==before;checks+=1
    # Opponent occupancy, nonneighbor, and wrong cycle cannot mutate the admitted prior.
    opponent=next(e for e in first['world']['elements'] if e['elementId']!=unit['elementId'])
    for dest in (opponent['currentLocationId'],'missing.location'):
        inp=copy.deepcopy(ins[0]);inp['command']['destinationLocationId']=dest
        rejected(lambda:transition(base,first,inp),5);checks+=1
    inp=copy.deepcopy(ins[0]);inp['command']['cycleId']='sha256:'+'0'*64
    rejected(lambda:transition(base,first,inp),4);checks+=1
    # Reject a tampered effect even when its checksum is recomputed consistently.
    event=json.loads(events[0]);event['effect']['costs']['terrainCost']=1;event.pop('receiptId')
    event['receiptId']='mov.'+steps.digest(INVENTORY['domains']['receipt'],encode(event))
    rejected(lambda:read_event(raw(event,'MovementEvent'),base,first,ins[0]),6);checks+=1
    for length in (-1,True,len(events)+1):
        rejected(lambda:replay(base,case,ins,events,length),2);checks+=1
    rejected(lambda:read_state(raw(final,'MovementState'),base,case,list(reversed(ins)),list(reversed(events))),6);checks+=1
    # Real cut at ceiling refuses further spend, retaining the accepted first move exactly.
    cap=f['cases'][3];b,s,inputs,_=trace(cap);before=copy.deepcopy(s)
    inp=command(b,s,inputs[0]['command']['unit'],inputs[0]['command']['originLocationId'])
    rejected(lambda:transition(b,s,inp),5);assert s==before;checks+=1
    # Source bindings confirm selected Clear/featureless topology and current published rule values.
    for edge in world.PACK['edges']:
        assert terrain_cost(edge['firstLocationId'],edge['secondLocationId'])==2
    source=(ROOT.parent.parent/'src/Cna.Core/Rules/Cna1979Movement.cs').read_text()
    assert 'Terrain("land.terrain.clear", NonMotorizedMobilityId, 2, 6)' in source
    assert 'Terrain("land.terrain.clear", MotorizedMobilityId, 2, 6)' in source
    return checks

def main():
    f=json.loads(FIXTURE.read_text());kernel=kernel_tests(f);cuts,mutations,raws,goldens=checks(f);guards=guard_checks(f)
    assert 'sourceHashes' in f and 'goldens' in f and goldens==f['goldens']
    if 'sourceHashes' in f:
        for name,digest in f['sourceHashes'].items():assert sha((ROOT/name).read_bytes())==digest
    print(f'PASS: 8 literal cases/both sides; {cuts} movement cuts, {mutations} mutations, {raws} raw rejections; {kernel} source arithmetic coordinates, membership kernel and {guards} atomic/overflow guards; contract-only synthetic repeat boundary.')
if __name__=='__main__':main()
