#!/usr/bin/env python3
"""D2c.2d creation-rooted Reserve/first opening; no runtime admission or golden writes."""
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

stage = load('reserve_stage', 'verify-combat-stage-entry-v1.py')
opening = load('reserve_opening', 'verify-combat-inherited-successors-v1.py')
pre, world = stage.pre, opening.world
encode, sha = stage.encode, stage.sha
INVENTORY = json.loads((ROOT/'combat-reserve-designation-v1.schema.json').read_text())
FIXTURE = ROOT/'fixtures/combat-reserve-designation-v1.json'
SCHEMA = opening.SCHEMA | stage.SCHEMA | {
    k: [tuple(f.split(':')) for f in v.split()] for k,v in INVENTORY['objects'].items()}

class Invalid(ValueError):
    def __init__(self, code):
        self.code = f'CMB-RDG-{code:03}'
        super().__init__(self.code)

def require(ok, code):
    if not ok: raise Invalid(code)

def typed(v, kind, depth=0):
    require(depth<=32,1)
    if kind.endswith('?'):
        if v is not None: typed(v,kind[:-1],depth)
    elif kind in SCHEMA:
        require(type(v) is dict and set(v)=={k for k,_ in SCHEMA[kind]},1)
        for k,t in SCHEMA[kind]: typed(v[k],t,depth+1)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v)<=512,1)
        for item in v: typed(item,kind[:-2],depth+1)
    else:
        try: opening.typed(v,kind,depth)
        except opening.Invalid as error: raise Invalid(1) from error

def canonical(v,kind):
    if kind.endswith('?'): return None if v is None else canonical(v,kind[:-1])
    if kind in SCHEMA: return {k:canonical(v[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'):
        child=kind[:-2]; out=[canonical(x,child) for x in v]
        if child in pre.env.KEYS: out.sort(key=lambda x:tuple(x[k] for k in pre.env.KEYS[child]))
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

def initial(request,created,preamble,weather_events,stage_events):
    require(type(stage_events) is list and len(stage_events)==4,6)
    try: state=stage.replay(request,created,preamble,weather_events,stage_events)
    except (stage.Invalid, stage.weather.Invalid, pre.Invalid, pre.env.Invalid,
            pre.env.init.Invalid, pre.env.world.Invalid, pre.env.inputs.Invalid,
            pre.env.sequence.Invalid) as error: raise Invalid(4) from error
    require(state['stateVersion']==10 and len(state['receipts'])==9
        and state['sequencePosition']==opening.position('entryFromPositionId'),4)
    side=state['operationStageOrders'][0]['firstSide']
    scope=dict(gameTurn=1,operationStage=1,playerPhaseSlot='first-acting-side',actingSide=side)
    members=[]
    for e in state['world']['elements']:
        if world.SIDES[e['elementId']]!=side: continue
        require(e['reserveStatus']=='none' and e['operationalState']['capabilityPointsExpended']==world.cp(0),4)
        members.append(dict(unit=world.unit(e,state['creationBinding']),status='none',baseCpa=10,
            spentCp=world.cp(0),history=opening.rel.empty_history(scope)))
    require(len(members)==1,4)
    state.update(firstActingSide=side,members=members,cycle=None,cycleId=None,
        openingBaseHash=None,completionReceiptId=None)
    return canonical(state,'ReserveState')

def _base(request,state):
    # Structural compatibility carrier; never an input to public replay/restore.
    return dict(contractVersion=1,profile='isolated-first-opening',creationRequest=copy.deepcopy(request),
        firstActingSide=state['firstActingSide'],position=copy.deepcopy(state['sequencePosition']),
        priorVersion=state['stateVersion'],priorPrefix=state['prefix'],world=copy.deepcopy(state['world']),
        randomState=copy.deepcopy(state['randomState']),members=copy.deepcopy(state['members']))

def designation(state):
    return dict(command=dict(contractVersion=2,kind='designate-reserve-element',
        creationBinding=state['creationBinding'],creationEventHash=state['creationEventHash'],
        expectedPriorVersion=state['stateVersion'],expectedPositionId=state['sequencePosition']['positionId'],
        elementId=state['members'][0]['unit']['elementId']),actor=state['firstActingSide'])

def completion(request,state): return opening.command(_base(request,state))

def input_kind(inp):
    require(type(inp) is dict and type(inp.get('command')) is dict,1)
    kind=inp['command'].get('kind')
    require(kind in ('designate-reserve-element','complete-reserve-designation'),3)
    return 'DesignationInput' if kind=='designate-reserve-element' else 'OpeningInput'

def authorize(inp,side):
    kind=input_kind(inp); typed(inp,kind)
    require(inp['command']['contractVersion']==2,3)
    require(inp['actor']==side,5)
    return kind

def receipt(event):
    unsigned={k:v for k,v in canonical(event,'DesignationEvent').items() if k!='receiptId'}
    return 'rd.'+hashlib.sha256(INVENTORY['domains']['receipt'].encode()+b'\0'+encode(unsigned)).hexdigest()

def _completion_event(b,inp):
    # Same frozen D2c.1 wire projection; provenance is validated by this reader's replay.
    c=opening.authority(b)
    event=dict(contractVersion=2,eventType='reserve-designation-completed',campaignId=c['campaignId'],
        rulesetHash=c['rulesetHash'],configurationHash=c['admittedPolicyBundleDigest'],
        baseHash=sha(opening.raw(b,'OpeningBase')),priorVersion=b['priorVersion'],stateVersion=c['openedAuthorityVersion'],
        priorPrefix=b['priorPrefix'],fromPositionId=b['position']['positionId'],
        sources=copy.deepcopy(opening.INVENTORY['completionSources']),input=copy.deepcopy(inp),
        sequencePosition=opening.position('movementPositionId'),cycle=c,
        cycleId=sha(opening.steps.seq.identity(c,'Authority',b['firstActingSide'])),receiptId='pending')
    unsigned={k:v for k,v in opening.canonical(event,'OpeningEvent').items() if k!='receiptId'}
    event['receiptId']='rc.'+opening.steps.digest(opening.INVENTORY['domains']['receipt'],encode(unsigned))
    return event

def _emit(request,state,inp):
    kind=authorize(inp,state['firstActingSide'])
    require(state['cycle'] is None and state['stateVersion'] in (10,11)
        and state['sequencePosition']==opening.position('entryFromPositionId'),6)
    after=copy.deepcopy(state)
    if kind=='DesignationInput':
        require(state['stateVersion']==10 and state['members'][0]['status']=='none',6)
        require(inp==designation(state),4)
        event=dict(contractVersion=2,eventType='reserve-element-designated',campaignId=state['campaignId'],
            rulesetHash=state['rulesetHash'],configurationHash=state['configurationHash'],
            creationBinding=state['creationBinding'],creationEventHash=state['creationEventHash'],
            priorVersion=10,stateVersion=11,priorPrefix=state['prefix'],
            fromPositionId=state['sequencePosition']['positionId'],input=copy.deepcopy(inp),gameTurn=1,
            operationStage=1,actingSide=state['firstActingSide'],elementId=inp['command']['elementId'],
            priorStatus='none',resultingStatus='I',sequencePosition=copy.deepcopy(state['sequencePosition']),
            sources=copy.deepcopy(INVENTORY['designationSources']),receiptId='pending')
        event['receiptId']=receipt(event); data=raw(event,'DesignationEvent')
        for e in after['world']['elements']:
            if e['elementId']==event['elementId']: e['reserveStatus']='I'
        after['members'][0]['status']='I'
        after['members'][0]['history']['designationReceiptId']=event['receiptId']
    else:
        b=_base(request,state)
        require(inp==opening.command(b),4)
        event=_completion_event(b,inp); data=opening.raw(event,'OpeningEvent')
        after.update(cycle=copy.deepcopy(event['cycle']),cycleId=event['cycleId'],
            openingBaseHash=event['baseHash'],completionReceiptId=event['receiptId'])
    after.update(stateVersion=event['stateVersion'],prefix=pre.env.sequence.prefix_event(state['prefix'],data),
        sequencePosition=copy.deepcopy(event['sequencePosition']))
    after['receipts'].append(dict(commandHash=sha(raw(inp,kind)),eventHash=sha(data),receiptId=event['receiptId'],
        actor=inp['actor'],stateVersion=event['stateVersion']))
    return canonical(after,'ReserveState'),data

def replay(request,created,preamble,weather_events,stage_events,events):
    require(type(events) is list and len(events)<=2,6)
    state=initial(request,created,preamble,weather_events,stage_events)
    for data in events:
        event=read_event(data)
        state,expected=_emit(request,state,event['input'])
        require(data==expected,6)
    return state

def read_event(data):
    # Parse closed alternatives independently, rather than trusting untyped event tags.
    for kind,name in [('DesignationEvent','reserve-element-designated'),('OpeningEvent','reserve-designation-completed')]:
        try:
            event=parse(data,kind)
            require(event['eventType']==name and event['contractVersion']==2,3)
            return event
        except Invalid: pass
    raise Invalid(1)

def apply(request,created,preamble,weather_events,stage_events,events,inp):
    state=replay(request,created,preamble,weather_events,stage_events,events)
    kind=authorize(inp,state['firstActingSide'])
    for data in events:
        accepted=read_event(data)['input']
        if inp['command']['expectedPriorVersion']==accepted['command']['expectedPriorVersion']:
            require(kind==input_kind(accepted) and raw(inp,kind)==raw(accepted,kind),6)
            return state,data,True
    after,data=_emit(request,state,inp)
    return after,data,False

def read_state(data,request,created,preamble,weather_events,stage_events,events):
    value=parse(data,'ReserveState')
    expected=replay(request,created,preamble,weather_events,stage_events,events)
    require(data==raw(expected,'ReserveState'),6)
    return value

def rejected(fn):
    try: fn()
    except Invalid: return
    raise AssertionError('expected CMB-RDG rejection')

def trace(case):
    row=next(c for c in json.loads(stage.FIXTURE.read_text())['cases'] if c['seed']==case['seed'])
    predecessor=stage.trace(row,case['choice'])
    assert stage.goldens(predecessor)==row['goldens'][case['choice']]
    q,created,preamble,w,_,s,_=predecessor
    state=initial(q,created,preamble,w,s); states=[state]; inputs=[]; events=[]
    if case['reserve']=='I':
        inp=designation(state);state,data,duplicate=apply(q,created,preamble,w,s,events,inp)
        assert not duplicate; inputs.append(inp);events.append(data);states.append(state)
    inp=completion(q,state);state,data,duplicate=apply(q,created,preamble,w,s,events,inp)
    assert not duplicate; inputs.append(inp);events.append(data);states.append(state)
    # Assertions encode literal rules and source-derived expectations before golden comparison.
    expected=case['expected']; before=states[0]; final=states[-1]
    assert before['stateVersion']==10 and len(before['receipts'])==9
    assert final['stateVersion']==expected['stateVersion'] and len(final['receipts'])==expected['receiptCount']
    assert final['cycle']['ordinal']==expected['ordinal']==1
    assert final['cycle']['openedAuthorityVersion']==expected['stateVersion']
    assert final['cycle']['actingSide']==final['firstActingSide']==expected['actor']
    assert final['cycle']['playerPhaseSlot']=='first-acting-side'
    assert final['sequencePosition']['positionId']==expected['movementPositionId']
    assert final['sequencePosition']['activeSide'] is None and final['sequencePosition']['actorRole']=='first-acting-side'
    assert final['operationStageWeather'][0]['kind']==expected['weatherKind']
    assert final['randomState']['nextByteCursor']==expected['weatherCursor']
    assert final['cycle']['openingPrefix']==states[-2]['prefix']
    assert final['completionReceiptId']==read_event(events[-1])['receiptId']
    assert final['members'][0]['status']==case['reserve']
    expected_world=copy.deepcopy(before['world'])
    for e in expected_world['elements']:
        if world.SIDES[e['elementId']]==expected['actor']: e['reserveStatus']=case['reserve']
    assert final['world']==expected_world and final['world']==states[-2]['world']
    assert all(e['ammunition']['points']==10 and e['operationalState']['capabilityPointsExpended']==world.cp(0)
        for e in final['world']['elements'])
    history=copy.deepcopy(before['members'][0]['history'])
    if case['reserve']=='I': history['designationReceiptId']=read_event(events[0])['receiptId']
    assert final['members'][0]['history']==history
    for state in states:
        assert all(state[k]==before[k] for k in ('randomState','operationStageWeather','operationStageOrders',
            'initiativeHolder','campaignId','rulesetHash','configurationHash','creationBinding','creationEventHash'))
        assert state['receipts'][:9]==before['receipts']
    assert all(s['cycle'] is None and s['cycleId'] is None and s['openingBaseHash'] is None
        and s['completionReceiptId'] is None for s in states[:-1])
    return q,created,preamble,w,s,inputs,events,states

def goldens(result):
    q,created,preamble,w,s,inputs,events,states=result
    values=[('request',pre.env.encode(q)),('creation',created),
        ('predecessor-records',encode([d.decode('ascii') for d in [created]+preamble+w+s]))]
    for i,inp in enumerate(inputs): values.append((f'input-{i+1}',raw(inp,input_kind(inp))))
    for i,event in enumerate(events): values.append((f'event-{i+1}',event))
    for i,state in enumerate(states): values.append((f'state-{i+10}',raw(state,'ReserveState')))
    values.append(('opening-base',opening.raw(_base(q,states[-2]),'OpeningBase')))
    return {k:dict(bytes=len(v),sha256=sha(v)) for k,v in values}

def verify_sources(f):
    repo=ROOT.parent.parent
    for path,digest in f['sourceHashes'].items(): require(sha((repo/path).read_bytes())==digest,9)
    declarations=opening.INVENTORY['inheritedEvents']
    row=next(r for r in declarations if r['eventType']=='reserve-element-designated')
    require(row['currentVersion']==INVENTORY['designation']['currentEventVersion']==1
        and row['successorVersion']==INVENTORY['designation']['eventVersion']==2,9)
    require(INVENTORY['designation']['commandVersion']==2 and INVENTORY['designation']['currentCommandVersion']==1
        and INVENTORY['designation']['priorStatus']=='none' and INVENTORY['designation']['resultingStatus']=='I',9)
    source=(repo/'src/Cna.Core/Campaigns/CampaignReserveEvents.cs').read_text()
    body=source.split('internal sealed record ReserveElementDesignated',1)[1].split('public ReserveElementDesignated(',1)[0]
    require(all('new("'+r['sourceId']+'", "'+r['locator']+'")' in body for r in INVENTORY['designationSources']),9)
    require('world[0].ReserveStatus != CampaignElementReserveStatus.None' in source
        and 'FirstActingSideResolver.Resolve(snapshot) != actingSide' in source,9)
    require('version is not (3 or 4)' in (repo/'src/Cna.Core/Campaigns/CampaignV11PreambleCodec.cs').read_text(),9)
    require(INVENTORY['completion']==dict(commandKind='complete-reserve-designation',commandType='OpeningInput',
        eventType='OpeningEvent',eventVersion=2,baseType='OpeningBase',baseProfile='isolated-first-opening'),9)
    for name in ('OpeningBase','OpeningInput','OpeningCommand','OpeningEvent'):
        require(SCHEMA[name]==opening.SCHEMA[name],9)



def verify_parent_boundaries(args):
    q,c,p,w,s=args; count=0
    for bad in (None,{},dict(q,contractVersion=2),dict(q,setupId='unknown'),dict(q,content=None),
            dict(q,randomState=dict(q['randomState'],algorithmId='wrong')),
            dict(q,randomState=dict(q['randomState'],seed=True))):
        rejected(lambda:initial(bad,c,p,w,s));count+=1
    for index in range(1,5):
        for bad in (None,{},[None],['bad'],[b'\xff']):
            altered=list(args);altered[index]=bad
            rejected(lambda:initial(*altered));count+=1
    # Isolated trusted-context guard probes do not admit new Setup identities.
    setup=copy.deepcopy(pre.CONTEXT.setup)
    for field in ('organization','navalConvoyArrival','fleetAssignment','fleetRepair'):
        try:
            pre.CONTEXT.setup['stageEntry'][field]='has-obligations'
            rejected(lambda:initial(*args));count+=1
        finally:pre.CONTEXT.setup=copy.deepcopy(setup)
    for field in ('weather','openingPreamble','initialInitiative'):
        try:
            pre.CONTEXT.setup[field]=dict(unsupported=True)
            rejected(lambda:initial(*args));count+=1
        finally:pre.CONTEXT.setup=copy.deepcopy(setup)
    return count

def main():
    f=json.loads(FIXTURE.read_text());verify_sources(f)
    assert {(c['seed'],c['choice'],c['reserve']) for c in f['cases']}=={
        (seed,choice,reserve) for seed in range(4) for choice in ('act-first','act-last') for reserve in ('none','I')}
    assert len(f['cases'])==16
    assert {c['expected']['weatherKind'] for c in f['cases']}=={'normal','hot','sandstorm','rainstorm'}
    results=[trace(case) for case in f['cases']]
    cuts=mutations=raw_rejects=parity=0
    boundaries=verify_parent_boundaries(results[0][:5])
    for case,result in zip(f['cases'],results):
        assert goldens(result)==case['goldens']
        q,created,preamble,w,s,inputs,events,states=result; retained=copy.deepcopy(result)
        args=(q,created,preamble,w,s)
        for i,state in enumerate(states):
            history=events[:i]; encoded=raw(state,'ReserveState')
            assert replay(*args,history)==state and read_state(encoded,*args,history)==state;cuts+=1
            for j in range(i):
                assert apply(*args,history,inputs[j])==(state,events[j],True);boundaries+=1
            # All leaves on representative empty/I traces for both actors; all four Weather traces retain full goldens.
            if case['seed']==0:
                for path,old in pre.leaves(state):
                    bad=pre.changed(state,path,old+1 if type(old) is int else 'wrong')
                    rejected(lambda:read_state(encode(bad),*args,history));mutations+=1
            for field,value in [('receipts',state['receipts'][:-1]),('receipts',state['receipts']*2),
                    ('members',[]),('members',state['members']*2),('operationStageWeather',[]),
                    ('operationStageOrders',[]),('world',states[0]['world']),('prefix',sha(b'invented')),
                    ('firstActingSide','axis' if state['firstActingSide']=='commonwealth' else 'commonwealth')]:
                if value==state[field]: continue
                rejected(lambda:read_state(raw(dict(state,**{field:value}),'ReserveState'),*args,history));boundaries+=1
            for inp in inputs:
                changes=[(('actor',),'system'),(('actor',),'axis' if inp['actor']=='commonwealth' else 'commonwealth'),
                    (('command','contractVersion'),1),(('command','expectedPriorVersion'),9),
                    (('command','expectedPriorVersion'),13),(('command','expectedPositionId'),'wrong'),
                    (('command','expectedPriorVersion'),True),(('command','expectedPriorVersion'),2**63)]
                if input_kind(inp)=='DesignationInput':
                    changes.extend([(('command','elementId'),'unknown'),(('command','elementId'),
                        next(e['elementId'] for e in state['world']['elements'] if world.SIDES[e['elementId']]!=inp['actor'])),
                        (('command','creationBinding'),'creation.forged'),(('command','creationEventHash'),sha(b'forged'))])
                else: changes.append((('command','baseHash'),sha(b'forged')))
                for path,value in changes:
                    rejected(lambda:apply(*args,history,pre.changed(inp,path,value)));boundaries+=1
            for data,kind in [(encoded,'ReserveState')]:
                for bad in (b'\xef\xbb\xbf'+data,data+b' ',data[:-1]+b',"unknown":null}',
                        encode(dict(reversed(list(state.items())))),data.replace(b'"contractVersion":1',b'"contractVersion":1.0',1)):
                    rejected(lambda:parse(bad,kind));raw_rejects+=1
        for i,data in enumerate(events):
            event=read_event(data);kind='DesignationEvent' if event['eventType']=='reserve-element-designated' else 'OpeningEvent'
            assert event['priorPrefix']==states[i]['prefix']
            assert states[i+1]['prefix']==pre.env.sequence.prefix_event(states[i]['prefix'],data)
            assert states[i+1]['receipts'][-1]==dict(commandHash=sha(raw(inputs[i],input_kind(inputs[i]))),
                eventHash=sha(data),receiptId=event['receiptId'],actor=case['expected']['actor'],stateVersion=11+i)
            if kind=='DesignationEvent':
                assert event['sources']==INVENTORY['designationSources'] and event['priorStatus']=='none' and event['resultingStatus']=='I'
            else:
                assert event['sources']==opening.INVENTORY['completionSources']
                # Prove adapter byte-for-byte parity with unchanged isolated implementation where its profile admits real history.
                if case['seed']==0:
                    b=_base(q,states[i]); expected_state,expected_data=opening.generate(b,inputs[i])
                    assert expected_data==data and opening.raw(b,'OpeningBase')==raw(b,'OpeningBase')
                    for key in ('world','randomState','members','prefix','stateVersion','cycle'):
                        assert expected_state[key]==states[i+1][key]
                    parity+=1
            if case['seed']==0:
                for path,old in pre.leaves(event):
                    bad=pre.changed(event,path,old+1 if type(old) is int else 'wrong')
                    rejected(lambda:replay(*args,events[:i]+[encode(bad)]));mutations+=1
            # Correctly re-signed forks still cannot substitute for accepted provenance.
            for field,value in [('priorPrefix',sha(b'synthetic-prefix')),('stateVersion',99),
                    ('sequencePosition',opening.position('entryFromPositionId') if kind=='OpeningEvent'
                        else opening.position('movementPositionId'))]:
                bad=dict(event,**{field:value})
                if kind=='DesignationEvent': bad['receiptId']=receipt(bad)
                else:
                    unsigned={k:v for k,v in opening.canonical(bad,kind).items() if k!='receiptId'}
                    bad['receiptId']='rc.'+opening.steps.digest(opening.INVENTORY['domains']['receipt'],encode(unsigned))
                rejected(lambda:replay(*args,events[:i]+[raw(bad,kind)]));boundaries+=1
            inp=inputs[i]
            for value,k in [(event,kind),(inp,input_kind(inp))]:
                encoded=raw(value,k)
                for bad in (b'\xef\xbb\xbf'+encoded,encoded+b' ',b' '+encoded,encoded[:-1]+b',"unknown":null}',
                        encode(dict(reversed(list(value.items())))),b'\xff',b'{"contractVersion":NaN}',
                        b'['*40+b'0'+b']'*40,encoded.replace(b'"contractVersion":2',b'"contractVersion":2.0',1),
                        encoded.replace(b'"contractVersion":2',b'"contractVersion":true',1),
                        encoded.replace(b'"contractVersion":2',b'"contractVersion":2,"contractVersion":2',1)):
                    rejected(lambda:parse(bad,k));raw_rejects+=1
        for bad in (events+[events[-1]],events*2,[events[-1]]*2,list(reversed(events))):
            if bad==events: continue
            rejected(lambda:replay(*args,bad));boundaries+=1
        if len(events)==2:
            rejected(lambda:replay(*args,[events[-1]]));boundaries+=1
            rejected(lambda:apply(*args,events[:1],designation(states[1])));boundaries+=1
        for inp in (designation(states[-1]),completion(q,states[-1])):
            rejected(lambda:apply(*args,events,inp));boundaries+=1
        for bad in ([],s[:-1],s[1:],list(reversed(s)),s+[s[-1]]):
            rejected(lambda:replay(q,created,preamble,w,bad,events));boundaries+=1
        for bad in ([],preamble[:-1],list(reversed(preamble)),preamble+[preamble[-1]]):
            rejected(lambda:replay(q,created,bad,w,s,events));boundaries+=1
        for bad in ([],w*2,[w[0]+b' ']):
            rejected(lambda:replay(q,created,preamble,bad,s,events));boundaries+=1
        for bad in (b'',created+b' ',created.replace(b'"contractVersion":11',b'"contractVersion":10',1)):
            rejected(lambda:replay(q,bad,preamble,w,s,events));boundaries+=1
        for path,value in [(('campaignId',),'reserve.other'),(('rulesetHash',),'0'*64),
                (('configurationHash',),sha(b'wrong')),(('randomState','seed'),(case['seed']+1)%4)]:
            rejected(lambda:replay(pre.changed(q,path,value),created,preamble,w,s,events));boundaries+=1
        other=next(r for c,r in zip(f['cases'],results) if c['seed']==case['seed']
            and c['choice']!=case['choice'] and c['reserve']==case['reserve'])
        rejected(lambda:replay(*other[:5],events));boundaries+=1
        rejected(lambda:read_state(raw(states[-1],'ReserveState'),*other[:5],other[6]));boundaries+=1
        alternate=next(r for c,r in zip(f['cases'],results) if c['seed']!=case['seed']
            and c['choice']==case['choice'] and c['reserve']==case['reserve'])
        rejected(lambda:replay(*alternate[:5],events));boundaries+=1
        assert result==retained
    for kind in ('DesignationInput','DesignationEvent','OpeningInput','OpeningEvent','ReserveState'):
        rejected(lambda:parse(b' '*1048577,kind));raw_rejects+=1
    print(f'PASS Reserve designation: {len(results)} creation-rooted traces, {cuts} replay/state cuts, '
        f'{mutations} leaf mutations, {raw_rejects} raw rejections, {boundaries} boundary/retry checks, '
        f'{parity} frozen-kernel parity cases, {len(f["sourceHashes"])} source pins. '
        'Atomic ordinal1 contract only; no runtime/Snapshot12 admission.')

if __name__=='__main__': main()
