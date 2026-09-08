#!/usr/bin/env python3
"""Creation-rooted route lifecycle contract oracle; no runtime or fixture writes."""
import copy
import hashlib
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
# Load locally by exact filename, as the retained executable contracts do.
import importlib.util

def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT/filename)
    out = importlib.util.module_from_spec(spec); spec.loader.exec_module(out)
    return out

im = load('lifecycle_movement', 'verify-combat-inherited-movement-v1.py')
ctl = load('lifecycle_control', 'verify-combat-cycle-control-v1.py')
encode, sha = im.encode, im.sha
INVENTORY = json.loads((ROOT/'combat-inherited-movement-lifecycle-v1.schema.json').read_text())
FIXTURE = ROOT/'fixtures/combat-inherited-movement-lifecycle-v1.json'
SCHEMA = im.SCHEMA | {k:ctl.SCHEMA[k] for k in ('EndLocation','MovementEndProof')} | {
    k:[tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['objects'].items()}
KINDS = ['stop-element-movement','resolve-breakdown-stop','complete-movement-segment']
EVENTS = ['element-movement-stopped','breakdown-stop-resolved','movement-segment-completed']
TYPES = ['StopEvent','ResolveEvent','CompleteEvent']
VERSIONS = [2,2,3]
DOMAINS = [INVENTORY['domains'][k] for k in ('stopReceipt','resolveReceipt','completeReceipt')]
CATALOG = im.rd.pre.env.sequence.expected_catalog()
MOVEMENT = im.rd.opening.position('movementPositionId')
INTERRUPT = next(p for p in CATALOG['interruptPositions'] if p['positionId']=='land.position.breakdown-stop')
TERMINAL = im.rd.opening.position('breakdownPositionId')

class Invalid(ValueError):
    def __init__(self, code):
        self.code=f'CMB-IML-{code:03}'; super().__init__(self.code)

def require(ok, code):
    if not ok: raise Invalid(code)

def variant(value, kind):
    require(type(value) is dict and type(value.get('kind')) is str,1)
    require(value['kind'] in INVENTORY['unions'][kind],3)
    return INVENTORY['unions'][kind][value['kind']]

def typed(v, kind, depth=0):
    require(depth<=32,1)
    if kind in INVENTORY['unions']: typed(v,variant(v,kind),depth)
    elif kind.endswith('?'):
        if v is not None: typed(v,kind[:-1],depth)
    elif kind in SCHEMA:
        require(type(v) is dict and set(v)=={k for k,_ in SCHEMA[kind]},1)
        for k,t in SCHEMA[kind]: typed(v[k],t,depth+1)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v)<=512,1)
        for x in v: typed(x,kind[:-2],depth+1)
    else:
        try: im.typed(v,kind,depth)
        except im.Invalid as error: raise Invalid(1) from error

def canonical(v,kind):
    if kind in INVENTORY['unions']: return canonical(v,variant(v,kind))
    if kind.endswith('?'): return None if v is None else canonical(v,kind[:-1])
    if kind in SCHEMA: return {k:canonical(v[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'):
        child=kind[:-2]; out=[canonical(x,child) for x in v]
        if child in ('UnitKey','EndLocation'):
            out.sort(key=lambda x:ctl.rel.key(x if child=='UnitKey' else x['unit']))
        elif child in im.rd.pre.env.KEYS: out.sort(key=lambda x:tuple(x[k] for k in im.rd.pre.env.KEYS[child]))
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
    try: v=json.loads(data.decode('utf8'),object_pairs_hook=pairs,
        parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError,UnicodeError,RecursionError) as error: raise Invalid(1) from error
    require(raw(v,kind)==data,8)
    return v

def initial(request,created,preamble,weather,stage,reserve,moves):
    try: state=im.replay(request,created,preamble,weather,stage,reserve,moves)
    except im.Invalid as error: raise Invalid(4) from error
    require(moves and state['breakdownFlow'] is not None,3)
    require(all(m['history']['nextMovement'] is None for m in state['members']),3)
    state.update(interruptContext=None,movementEnd=None)
    return canonical(state,'LifecycleState')

def capability(state,kind):
    value=dict(domain='sandtable.observation.movement-route.v1' if kind==KINDS[0]
        else 'sandtable.action.breakdown-stop.v1',campaignId=state['campaignId'],rulesetHash=state['rulesetHash'],
        stateVersion=state['stateVersion'],audience=state['firstActingSide'] if kind==KINDS[0] else 'system')
    if kind==KINDS[0]:
        require(state['breakdownFlow']['kind']=='moving',6); route=state['breakdownFlow']['route']
        value.update({k:route[k] for k in ('elementId','originLocationId','currentLocationId')})
    return sha(encode(value))

def command(state,kind):
    require(kind in KINDS,3)
    action=dict(contractVersion=1,kind=kind); cmd=dict(contractVersion=2,kind=kind)
    if kind in KINDS[:2]:
        field='routeId' if kind==KINDS[0] else 'stopId'; cap=capability(state,kind)
        action[field]=cap; cmd[field]=cap
    cmd.update(actionId=sha(encode(action)),creationBinding=state['creationBinding'],
        creationEventHash=state['creationEventHash'],cycleId=state['cycleId'],
        expectedPriorVersion=state['stateVersion'],expectedPositionId=state['sequencePosition']['positionId'])
    return dict(command=cmd,actor='system' if kind==KINDS[1] else state['firstActingSide'])

def authorize(state,inp):
    typed(inp,'LifecycleInput'); cmd=inp['command']; kind=cmd['kind']
    require(cmd['contractVersion']==2,3)
    require(inp['actor']==('system' if kind==KINDS[1] else state['cycle']['actingSide']),5)
    require(cmd['creationBinding']==state['creationBinding']
        and cmd['creationEventHash']==state['creationEventHash'] and cmd['cycleId']==state['cycleId'],4)

def receipt(event,index):
    typed(event,TYPES[index])
    unsigned={k:v for k,v in canonical(event,TYPES[index]).items() if k!='receiptId'}
    return 'iml.'+hashlib.sha256(DOMAINS[index].encode()+b'\0'+encode(unsigned)).hexdigest()

def _emit(state,inp):
    # Private transition kernel: public entry points admit only fully replayed state.
    authorize(state,inp); kind=inp['command']['kind']; index=KINDS.index(kind)
    require(state['movementEnd'] is None,6)
    require(inp['command']['expectedPriorVersion']==state['stateVersion']
        and inp['command']['expectedPositionId']==state['sequencePosition']['positionId'],6)
    require(state['stateVersion']<2**63-1 and len(state['receipts'])<512,7)
    require(raw(inp,'LifecycleInput')==raw(command(state,kind),'LifecycleInput'),5)
    version=state['stateVersion']+1; after=copy.deepcopy(state)
    event=dict(contractVersion=VERSIONS[index],eventType=EVENTS[index],campaignId=state['campaignId'],
        stateVersion=version,priorStateVersion=state['stateVersion'],rulesetHash=state['rulesetHash'],
        fromPositionId=state['sequencePosition']['positionId'])
    event.update({k:copy.deepcopy(state[k]) for k in ('configurationHash','creationBinding','creationEventHash',
        'cycleId','openingBaseHash','completionReceiptId')})
    event.update(priorPrefix=state['prefix'],input=copy.deepcopy(inp),receiptId='pending')
    if index==0:
        require(state['sequencePosition']==MOVEMENT and state['interruptContext'] is None
            and state['breakdownFlow']['kind']=='moving',6)
        stop=dict(stopId='pending',recordedStateVersion=version,route=copy.deepcopy(state['breakdownFlow']['route']),
            reason='deliberate',weatherKind='normal',cohortInputs=[])
        preimage=dict(domain='sandtable.breakdown.stop.v1',campaignId=state['campaignId'],rulesetHash=state['rulesetHash'])
        preimage.update({k:v for k,v in stop.items() if k!='stopId'}); stop['stopId']=sha(encode(preimage))
        after.update(breakdownFlow=dict(kind='phasing-stop',stop=stop),sequencePosition=copy.deepcopy(INTERRUPT),
            interruptContext=dict(cycle=copy.deepcopy(state['cycle']),cycleId=state['cycleId'],sequencePosition=copy.deepcopy(MOVEMENT)))
        event.update(actionId=inp['command']['actionId'],actingSide=inp['actor'],submittedRouteId=inp['command']['routeId'])
    elif index==1:
        require(state['sequencePosition']==INTERRUPT and state['breakdownFlow']['kind']=='phasing-stop'
            and state['interruptContext']==dict(cycle=state['cycle'],cycleId=state['cycleId'],sequencePosition=MOVEMENT),6)
        event.update(actionId=inp['command']['actionId'],stop=copy.deepcopy(state['breakdownFlow']['stop']),
            randomStateBefore=copy.deepcopy(state['randomState']),checks=[],createdLots=[],
            randomStateAfter=copy.deepcopy(state['randomState']),
            sources=[dict(sourceId='spi-1979-land-rules',locator='21.24-21.26')])
        after.update(breakdownFlow=dict(kind='idle'),sequencePosition=copy.deepcopy(state['interruptContext']['sequencePosition']),
            interruptContext=None)
    else:
        require(state['sequencePosition']==MOVEMENT and state['breakdownFlow']==dict(kind='idle')
            and state['interruptContext'] is None,6)
        proof=dict(scope=ctl.rel.scope(state['cycle']),ordinal=1,completionReceiptId='pending',
            endLocations=ctl.locations(state['world']),excludedBefore=[])
        proof['excludedBefore']=ctl.excluded(proof,state['cycle']['actingSide'])
        after.update(breakdownFlow=dict(kind='idle'),sequencePosition=copy.deepcopy(TERMINAL),movementEnd=proof)
        event.update(gameTurn=1,operationStage=1,actingSide=inp['actor'],endLocations=copy.deepcopy(proof['endLocations']),
            excludedUnits=copy.deepcopy(proof['excludedBefore']),progress=copy.deepcopy(state['actualProgressRefs']))
    event.update(sequencePosition=copy.deepcopy(after['sequencePosition']),breakdownFlowAfter=copy.deepcopy(after['breakdownFlow']),
        interruptContextAfter=copy.deepcopy(after['interruptContext']))
    event['receiptId']=receipt(event,index); data=raw(event,TYPES[index]); rid=event['receiptId']
    if index==2: after['movementEnd']['completionReceiptId']=rid
    after.update(stateVersion=version,prefix=im.rd.pre.env.sequence.prefix_event(state['prefix'],data))
    after['receipts'].append(dict(commandHash=sha(raw(inp,'LifecycleInput')),eventHash=sha(data),receiptId=rid,
        actor=inp['actor'],stateVersion=version))
    return canonical(after,'LifecycleState'),data

def read_event(data):
    require(type(data) is bytes and 0<len(data)<=1048576,1)
    try: value=json.loads(data)
    except (ValueError,UnicodeError,RecursionError) as error: raise Invalid(1) from error
    require(type(value) is dict and value.get('eventType') in EVENTS,3)
    index=EVENTS.index(value['eventType']); value=parse(data,TYPES[index])
    require(value['contractVersion']==VERSIONS[index] and value['input']['command']['kind']==KINDS[index],3)
    require(value['receiptId']==receipt(value,index),4)
    return value

def replay(request,created,preamble,weather,stage,reserve,moves,events):
    require(type(events) is list and len(events)<=3,7)
    state=initial(request,created,preamble,weather,stage,reserve,moves)
    for data in events:
        event=read_event(data); state,expected=_emit(state,event['input']); require(data==expected,6)
    return state

def apply(request,created,preamble,weather,stage,reserve,moves,events,inp):
    state=replay(request,created,preamble,weather,stage,reserve,moves,events)
    authorize(state,inp)
    for data in events:
        accepted=read_event(data)['input']
        if accepted['command']['expectedPriorVersion']==inp['command']['expectedPriorVersion']:
            require(raw(inp,'LifecycleInput')==raw(accepted,'LifecycleInput'),6)
            return state,data,True
    after,data=_emit(state,inp)
    return after,data,False

def read_state(data,request,created,preamble,weather,stage,reserve,moves,events):
    value=parse(data,'LifecycleState')
    require(data==raw(replay(request,created,preamble,weather,stage,reserve,moves,events),'LifecycleState'),6)
    return value

def source_trace(side,count):
    case=next(c for c in json.loads(im.FIXTURE.read_text())['cases'] if c['expected']['actor']==side)
    args,_=im.source_trace(case['predecessorCase']); state=im.initial(*args); moves=[]
    for dest in case['expected']['route'][1:count+1]:
        state,event,_=im.apply(*args,moves,im.command(state,dest)); moves.append(event)
    return (*args,moves)

def semantic_red():
    args=source_trace('axis',1); before=initial(*args)
    state,event=_emit(before,command(before,KINDS[0]))
    assert (state['breakdownFlow']['kind'],state['sequencePosition']['positionId'],state['stateVersion'])==(
        'phasing-stop','land.position.breakdown-stop',13), 'missing actual stop interrupt/version effect'
    assert state['interruptContext']['sequencePosition']==before['sequencePosition']
    state,_=_emit(state,command(state,KINDS[1]))
    assert state['breakdownFlow']=={'kind':'idle'} and state['sequencePosition']==MOVEMENT
    state,_=_emit(state,command(state,KINDS[2]))
    assert state['sequencePosition']==TERMINAL and state['stateVersion']==15
    assert state['movementEnd']['completionReceiptId']!=state['completionReceiptId']
    assert state['world']==before['world'] and state['randomState']==before['randomState']


def trace(case):
    side,count=case['actor'],case['moves']; args=source_trace(side,count); state=initial(*args)
    before=copy.deepcopy(state); states=[state]; inputs=[]; events=[]
    unit=state['members'][0]['unit']; element=next(e for e in state['world']['elements'] if e['elementId']==unit['elementId'])
    expected=case['expected']
    assert element['currentLocationId']==side+'-'+expected['locationSuffix']
    assert element['operationalState']['capabilityPointsExpended']==im.world.cp(expected['cp'])
    assert element['operationalState']['cohesionLevel']==expected['cohesion']
    for index,kind in enumerate(KINDS):
        inp=command(state,kind); prior=copy.deepcopy(state); state,event,duplicate=apply(*args,events,inp)
        assert not duplicate; inputs.append(inp); events.append(event); states.append(state)
        value=read_event(event)
        assert value['contractVersion']==VERSIONS[index] and value['eventType']==EVENTS[index]
        assert state['stateVersion']==12+count+index and state['prefix']!=prior['prefix']
        assert state['prefix']==im.rd.pre.env.sequence.prefix_event(prior['prefix'],event)
        assert state['receipts'][:-1]==prior['receipts'] and state['receipts'][-1]==dict(
            commandHash=sha(raw(inp,'LifecycleInput')),eventHash=sha(event),receiptId=value['receiptId'],
            actor=side if index!=1 else 'system',stateVersion=12+count+index)
        assert all(state[k]==before[k] for k in before if k not in (
            'stateVersion','prefix','receipts','sequencePosition','breakdownFlow','interruptContext','movementEnd'))
        assert state['sequencePosition']['activeSide'] is None and state['sequencePosition']['contractVersion']==5
        if index==0:
            stop=value['breakdownFlowAfter']['stop']; route=before['breakdownFlow']['route']
            assert stop['route']==route and stop['recordedStateVersion']==12+count
            assert (stop['reason'],stop['weatherKind'],stop['cohortInputs'])==('deliberate','normal',[])
            assert value['submittedRouteId']!=route['routeId'] and state['sequencePosition']==INTERRUPT
            assert state['interruptContext']==dict(cycle=before['cycle'],cycleId=before['cycleId'],sequencePosition=MOVEMENT)
            assert state['movementEnd'] is None
        elif index==1:
            assert value['stop']==read_event(events[0])['breakdownFlowAfter']['stop']
            assert inp['command']['stopId']!=value['stop']['stopId']
            assert value['checks']==value['createdLots']==[] and value['randomStateBefore']==value['randomStateAfter']==before['randomState']
            assert value['sources']==[dict(sourceId='spi-1979-land-rules',locator='21.24-21.26')]
            assert state['sequencePosition']==MOVEMENT and state['breakdownFlow']==dict(kind='idle')
            assert state['interruptContext'] is None and state['movementEnd'] is None
        else:
            assert state['sequencePosition']==TERMINAL and state['interruptContext'] is None
            assert state['breakdownFlow']==dict(kind='idle')
            proof=state['movementEnd']; expected_excluded=[unit] if expected['excluded'] else []
            assert proof['excludedBefore']==value['excludedUnits']==expected_excluded
            assert proof['ordinal']==1 and proof['scope']=={k:before['cycle'][k] for k in (
                'gameTurn','operationStage','playerPhaseSlot','actingSide')}
            assert proof['completionReceiptId']==value['receiptId']!=before['completionReceiptId']
            assert proof['endLocations']==value['endLocations'] and len(proof['endLocations'])==2
            for entry in proof['endLocations']:
                assert entry['unit']['creationBinding']==before['creationBinding']
                actual=next(e for e in before['world']['elements'] if e['elementId']==entry['unit']['elementId'])
                assert entry['locationId']==actual['currentLocationId']
            assert value['progress']==before['actualProgressRefs'] and len(value['progress'])==count
        assert read_state(raw(state,'LifecycleState'),*args,events)==state
    for i,inp in enumerate(inputs):
        retried,old,duplicate=apply(*args,events,inp)
        assert duplicate and old==events[i] and retried==states[-1]
    return args,inputs,events,states

def goldens(result):
    args,inputs,events,states=result
    records=[encode(args[0]),args[1]]+[r for group in args[2:] for r in group]
    values=[encode([sha(r) for r in records])]+[raw(i,'LifecycleInput') for i in inputs]+events+[
        raw(s,'LifecycleState') for s in states]
    return dict(bytes=[len(v) for v in values],sha256=[sha(v) for v in values])

def rejected(fn):
    try: fn()
    except Invalid: return
    raise AssertionError('expected CMB-IML rejection')

def verify(result,deep):
    args,inputs,events,states=result; counts=dict(cuts=0,retries=3,mutations=0,raw=0,boundaries=0)
    original=copy.deepcopy(result)
    for i,state in enumerate(states):
        assert replay(*args,events[:i])==state; counts['cuts']+=1
    # Every forbidden lifecycle order, omitted step, duplicate and unsupported fourth event.
    for bad in ([events[1]],[events[2]],events[1:],[events[0],events[2]],list(reversed(events)),
                [events[0],events[0]],events+[events[-1]],[events[0],events[2],events[1]]):
        rejected(lambda b=bad:replay(*args,b)); counts['boundaries']+=1
    for i,inp in enumerate(inputs):
        for actor in ('axis','commonwealth','system'):
            if actor==inp['actor']: continue
            bad=im.changed(inp,('actor',),actor)
            rejected(lambda b=bad:apply(*args,events,b)); counts['boundaries']+=1
        for path,value in ((('command','creationBinding'),'foreign.creation'),(('command','cycleId'),'sha256:'+'f'*64),
                           (('command','creationEventHash'),'sha256:'+'e'*64),(('command','expectedPriorVersion'),1),
                           (('command','expectedPositionId'),'foreign.position'),(('command','actionId'),'sha256:'+'d'*64)):
            bad=im.changed(inp,path,value)
            rejected(lambda b=bad:apply(*args,events,b)); counts['boundaries']+=1
    # A fresh completion cannot repeat after terminal; valid old completion may retry.
    rejected(lambda:apply(*args,events,command(states[-1],KINDS[2]))); counts['boundaries']+=1
    # Authoritative identities are not public capabilities, even with correctly hashed action IDs.
    for i,field,identity in ((0,'routeId',states[0]['breakdownFlow']['route']['routeId']),
        (1,'stopId',states[1]['breakdownFlow']['stop']['stopId'])):
        bad=copy.deepcopy(inputs[i]); bad['command'][field]=identity
        bad['command']['actionId']=sha(encode(dict(contractVersion=1,kind=KINDS[i],**{field:identity})))
        rejected(lambda b=bad,n=i:apply(*args,events[:n],b)); counts['boundaries']+=1
    if deep:
        # Challenge every leaf of all three event families, re-signing well-typed changes.
        for i,data in enumerate(events):
            event=read_event(data)
            for path,value in im.leaves(event):
                bad=im.changed(event,path,im.different(value))
                try:
                    if path!=('receiptId',): bad['receiptId']=receipt(bad,i)
                    encoded=raw(bad,TYPES[i])
                except (Invalid,KeyError): encoded=encode(bad)
                rejected(lambda b=encoded,n=i:replay(*args,events[:n]+[b])); counts['mutations']+=1
        # Every leaf in pending context, proof and inherited authority/progress cache is checked.
        for i,state in enumerate(states):
            for path,value in im.leaves(state):
                if path[0] not in ('interruptContext','breakdownFlow','movementEnd','cycle','actualProgressRefs',
                                    'prefix','stateVersion','randomState'): continue
                bad=im.changed(state,path,im.different(value))
                try: encoded=raw(bad,'LifecycleState')
                except Invalid: encoded=encode(bad)
                rejected(lambda b=encoded,n=i:read_state(b,*args,events[:n])); counts['mutations']+=1
        # Missing or malformed accepted predecessor authority cannot be replaced by a final cache.
        for i in range(1,7):
            altered=list(copy.deepcopy(args)); altered[i]=args[i]+b' ' if i==1 else args[i][:-1]
            rejected(lambda a=altered:read_state(raw(states[-1],'LifecycleState'),*a,events)); counts['boundaries']+=1
        empty=list(copy.deepcopy(args)); empty[-1]=[]
        rejected(lambda:replay(*empty,[])); counts['boundaries']+=1
        foreign=source_trace('commonwealth' if states[0]['firstActingSide']=='axis' else 'axis',6)
        rejected(lambda:replay(*foreign,events)); counts['boundaries']+=1
        rejected(lambda:read_state(raw(states[-1],'LifecycleState'),*foreign,[])); counts['boundaries']+=1
        changed_moves=list(copy.deepcopy(args)); changed_moves[-1]=list(reversed(args[-1]))
        rejected(lambda:replay(*changed_moves,events)); counts['boundaries']+=1
        # Keep original encoding raw to prove alternate representations and unknown/duplicate keys fail.
        samples=[(data,TYPES[i]) for i,data in enumerate(events)]+[(raw(s,'LifecycleState'),'LifecycleState') for s in states]
        for data,kind in samples:
            value=json.loads(data)
            variants=[data+b' ',b' '+data,b'\xef\xbb\xbf'+data,data.replace(b':',b': ',1),
                b'{"unknown":0,'+data[1:],b'{"contractVersion":12,'+data[1:],
                encode(dict(reversed(list(value.items())))),data.replace(b'"contractVersion":',b'"contractVersio\\u006e":',1),
                data.replace(b'"contractVersion":'+str(value['contractVersion']).encode(),b'"contractVersion":true',1),
                data.replace(b'"contractVersion":'+str(value['contractVersion']).encode(),
                    b'"contractVersion":'+str(value['contractVersion']).encode()+b'.0',1)]
            for bad in variants:
                rejected(lambda b=bad,t=kind:parse(b,t)); counts['raw']+=1
        for bad in (b'',b'x'*1048577,b'{"x":'+b'['*1100+b'0'+b']'*1100+b'}',b'NaN',b'null',b'[]'):
            rejected(lambda b=bad:read_event(b)); counts['raw']+=1
        # Internal guard capacity probes do not claim such states are externally admitted.
        for field,value in (('stateVersion',2**63-1),('receipts',states[0]['receipts']*50)):
            bad=copy.deepcopy(states[0]); bad[field]=value
            rejected(lambda b=bad:_emit(b,command(b,KINDS[0]))); counts['boundaries']+=1
    assert result==original, 'public verification mutated source histories, inputs or states'
    return counts

PIN_PATHS = ['docs/specs/combat-inherited-movement-v1.schema.json', 'docs/specs/verify-combat-inherited-movement-v1.py', 'docs/specs/fixtures/combat-inherited-movement-v1.json', 'docs/specs/combat-cycle-control-v1.schema.json', 'docs/specs/verify-combat-cycle-control-v1.py', 'docs/specs/combat-cycle-sequence-v1.schema.json', 'src/Cna.Core/Campaigns/CampaignBreakdownLifecycleFactory.cs', 'src/Cna.Core/Campaigns/CampaignBreakdownLifecycleCodec.cs', 'src/Cna.Core/Campaigns/CampaignBreakdownStopResolvedFactory.cs', 'src/Cna.Core/Campaigns/CampaignBreakdownStopResolvedCodec.cs', 'src/Cna.Core/Campaigns/CampaignBreakdownCodec.cs', 'src/Cna.Core/Campaigns/CampaignBreakdownRecords.cs', 'src/Cna.Core/Campaigns/CampaignV11BreakdownProjector.cs', 'src/Cna.Core/Actions/CampaignActionCandidate.cs', 'src/Cna.Core/Actions/CampaignMovementActionCandidate.cs', 'src/Cna.Core/Rules/Cna1979BreakdownAdjudication.cs']

def check_fixture(fixture):
    assert set(fixture)=={'contractVersion','cases','sourcePins'} and fixture['contractVersion']==1
    expected={(s,n) for s in ('axis','commonwealth') for n in (1,5,6,7)}
    assert len(fixture['cases'])==8 and {(c['actor'],c['moves']) for c in fixture['cases']}==expected
    assert len({c['name'] for c in fixture['cases']})==8
    for case in fixture['cases']:
        n=case['moves']
        assert set(case)=={'name','actor','moves','expected','goldens'}
        assert case['expected']==dict(cp=n*2,cohesion=-max(0,n*2-10),locationSuffix='supply' if n==6 else 'rear',excluded=n==6)
    assert [p['path'] for p in fixture['sourcePins']]==PIN_PATHS
    assert len({p['path'] for p in fixture['sourcePins']})==len(fixture['sourcePins'])
    for pin in fixture['sourcePins']:
        data=(ROOT.parent.parent/pin['path']).read_bytes()
        assert pin['sha256']==sha(data), 'source drift: '+pin['path']

def main():
    fixture=json.loads(FIXTURE.read_text()); check_fixture(fixture); semantic_red()
    total=dict(cuts=0,retries=0,mutations=0,raw=0,boundaries=0)
    for case in fixture['cases']:
        result=trace(case); assert goldens(result)==case['goldens'],case['name']+' golden drift'
        counts=verify(result,case['moves']==6)
        for k,v in counts.items(): total[k]+=v
    print('PASS inherited Movement lifecycle: '+str(len(fixture['cases']))+' traces,24 events, '+
        ', '.join(str(v)+' '+k for k,v in total.items())+', '+str(len(fixture['sourcePins']))+' source pins')

if __name__=='__main__': main()
