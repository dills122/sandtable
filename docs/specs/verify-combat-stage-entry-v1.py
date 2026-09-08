#!/usr/bin/env python3
"""D2c.2c complete Weather-to-Reserve-entry contract; no C# activation or golden regeneration."""
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('stage_weather', ROOT/'verify-combat-weather-v1.py')
weather = importlib.util.module_from_spec(spec)
spec.loader.exec_module(weather)
pre = weather.pre
INVENTORY = json.loads((ROOT/'combat-stage-entry-v1.schema.json').read_text())
FIXTURE = ROOT/'fixtures/combat-stage-entry-v1.json'
SCHEMA = weather.SCHEMA | {k: [tuple(f.split(':')) for f in v.split()] for k,v in INVENTORY['objects'].items()}
encode, sha = weather.encode, weather.sha

class Invalid(ValueError):
    def __init__(self, code):
        self.code = f'CMB-STE-{code:03}'
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
        try: weather.typed(v,kind,depth)
        except weather.Invalid as error: raise Invalid(1) from error

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

def require_policy(policy):
    typed(policy,'StageEntryPolicy')
    require(policy==INVENTORY['stageEntryPolicy'],3)

def initial(request,created,preamble,weather_events):
    require(type(weather_events) is list and len(weather_events)==1,6)
    require_policy(pre.CONTEXT.setup.get('stageEntry'))
    try: state=weather.replay(request,created,preamble,weather_events)
    except weather.Invalid as error: raise Invalid(4) from error
    require(state['stateVersion']==6 and state['sequencePosition']==pre.position(5)
        and len(state['receipts'])==5 and len(state['operationStageWeather'])==1,4)
    return canonical(state,'StageEntryState')

def command(state):
    index=state['stateVersion']-6
    require(0<=index<4,6)
    row=INVENTORY['events'][index]
    return dict(command=dict(contractVersion=row['commandVersion'],kind=row['command'],
        creationBinding=state['creationBinding'],creationEventHash=state['creationEventHash'],
        expectedPriorVersion=state['stateVersion'],expectedPositionId=state['sequencePosition']['positionId']),actor='system')

def authorize(inp):
    typed(inp,'StageEntryInput')
    rows=[r for r in INVENTORY['events'] if r['command']==inp['command']['kind']]
    require(len(rows)==1 and inp['command']['contractVersion']==rows[0]['commandVersion'],3)
    require(inp['actor']=='system',5)

def receipt(event):
    unsigned={k:v for k,v in canonical(event,'StageEntryEvent').items() if k!='receiptId'}
    return 'ste.'+hashlib.sha256(INVENTORY['domains']['receipt'].encode()+b'\0'+encode(unsigned)).hexdigest()

def _emit(state,inp):
    # Internal kernel consumes replay-produced state only; a supplied projection is never authority.
    authorize(inp)
    index=state['stateVersion']-6; require(0<=index<4,6)
    row=INVENTORY['events'][index]
    require_policy(pre.CONTEXT.setup['stageEntry'])
    require(state['sequencePosition']==pre.position(row['fromPositionIndex']) and inp==command(state),4)
    event=dict(contractVersion=row['eventVersion'],eventType=row['eventType'],campaignId=state['campaignId'],
        rulesetHash=state['rulesetHash'],configurationHash=state['configurationHash'],creationBinding=state['creationBinding'],
        creationEventHash=state['creationEventHash'],priorVersion=state['stateVersion'],stateVersion=state['stateVersion']+1,
        priorPrefix=state['prefix'],fromPositionId=state['sequencePosition']['positionId'],input=copy.deepcopy(inp),
        gameTurn=1,operationStage=1,sequencePosition=pre.position(row['toPositionIndex']),
        sources=pre.sources(pre.CONTEXT.setup['stageEntry']['sources'],pre.land(row['locator'])),receiptId='pending')
    event['receiptId']=receipt(event); data=raw(event,'StageEntryEvent'); after=copy.deepcopy(state)
    after.update(stateVersion=event['stateVersion'],prefix=pre.env.sequence.prefix_event(state['prefix'],data),
        sequencePosition=copy.deepcopy(event['sequencePosition']))
    after['receipts'].append(dict(commandHash=sha(raw(inp,'StageEntryInput')),eventHash=sha(data),
        receiptId=event['receiptId'],actor=inp['actor'],stateVersion=event['stateVersion']))
    return after,data

def replay(request,created,preamble,weather_events,events):
    require(type(events) is list and len(events)<=4,6)
    state=initial(request,created,preamble,weather_events)
    for data in events:
        value=parse(data,'StageEntryEvent'); state,expected=_emit(state,value['input'])
        require(data==expected,6)
    return state

def apply(request,created,preamble,weather_events,events,inp):
    state=replay(request,created,preamble,weather_events,events); authorize(inp)
    for data in events:
        accepted=parse(data,'StageEntryEvent')['input']
        if accepted['command']['expectedPriorVersion']==inp['command']['expectedPriorVersion']:
            require(raw(inp,'StageEntryInput')==raw(accepted,'StageEntryInput'),6)
            return state,data,True
    after,data=_emit(state,inp)
    return after,data,False

def read_state(data,request,created,preamble,weather_events,events):
    value=parse(data,'StageEntryState'); expected=replay(request,created,preamble,weather_events,events)
    require(data==raw(expected,'StageEntryState'),6)
    return value

def rejected(fn):
    try: fn()
    except Invalid: return
    raise AssertionError('expected CMB-STE rejection')

def trace(case,choice):
    predecessor=json.loads(weather.FIXTURE.read_text())
    row=next(c for c in predecessor['cases'] if c['seed']==case['seed'])
    result=weather.trace(row,choice)
    assert weather.goldens(result)==row['goldens'][choice]
    q,created,preamble,_,w,_,before=result
    assert before['operationStageWeather'][0]['kind']==case['weatherKind']
    assert before['randomState']['nextByteCursor']==case['weatherCursor']
    state=initial(q,created,preamble,[w]);states=[state];inputs=[];events=[]
    for index in range(4):
        inp=command(state);state,data,dup=apply(q,created,preamble,[w],events,inp)
        assert not dup;inputs.append(inp);events.append(data);states.append(state)
    return q,created,preamble,[w],inputs,events,states

def goldens(result):
    _,created,preamble,w,inputs,events,states=result
    values=[('creation',created)]+[(f'preamble-{i+1}',d) for i,d in enumerate(preamble)]+[('weather',w[0])]
    for i,inp in enumerate(inputs): values.append((f'input-{i+1}',raw(inp,'StageEntryInput')))
    for i,data in enumerate(events): values.append((f'event-{i+1}',data))
    for i,state in enumerate(states): values.append((f'state-{i+6}',raw(state,'StageEntryState')))
    return {k:dict(bytes=len(v),sha256=sha(v)) for k,v in values}

def verify_sources(f):
    for name,digest in f['sourceHashes'].items(): require(sha((ROOT.parent.parent/name).read_bytes())==digest,9)
    inherited=json.loads((ROOT/'combat-inherited-successors-v1.schema.json').read_text())['inheritedEvents']
    names=['organization','naval-convoy-arrival','fleet-assignment','fleet-repair']
    assert len(INVENTORY['events'])==4
    for i,(row,name) in enumerate(zip(INVENTORY['events'],names)):
        declaration=next(x for x in inherited if x['eventType']=='no-obligation-'+name+'-resolved')
        require(row['eventType']==declaration['eventType'] and row['currentEventVersion']==declaration['currentVersion']==1
            and row['eventVersion']==declaration['successorVersion']==2,9)
        require(row['command']=='resolve-no-obligation-'+name and row['currentCommandVersion']==1
            and row['commandVersion']==2 and row['actor']=='system' and row['priorVersion']==6+i
            and row['fromPositionIndex']==5+i and row['toPositionIndex']==6+i,9)
    require('version is not (3 or 4)' in (ROOT.parent.parent/'src/Cna.Core/Campaigns/CampaignV11PreambleCodec.cs').read_text(),9)

def main():
    f=json.loads(FIXTURE.read_text());verify_sources(f)
    assert {c['weatherKind'] for c in f['cases']}=={'normal','hot','sandstorm','rainstorm'}
    assert {c['seed'] for c in f['cases']}=={0,1,2,3,81,2**64-1}
    cuts=mutations=raw_rejects=boundaries=0
    for case in f['cases']:
        for choice in ('act-first','act-last'):
            result=trace(case,choice);q,created,preamble,w,inputs,events,states=result
            assert goldens(result)==case['goldens'][choice];retained=copy.deepcopy(result)
            before=states[0]
            for i,state in enumerate(states):
                history=events[:i];encoded=raw(state,'StageEntryState')
                assert replay(q,created,preamble,w,history)==state
                assert read_state(encoded,q,created,preamble,w,history)==state;cuts+=1
                assert state['stateVersion']==6+i and state['sequencePosition']==f['positions'][i]
                assert all(state[k]==before[k] for k in ('world','randomState','operationStageWeather',
                    'operationStageOrders','initiativeHolder','campaignId','rulesetHash','configurationHash',
                    'creationBinding','creationEventHash'))
                assert state['receipts'][:5]==before['receipts'] and len(state['receipts'])==5+i
                for path,old in pre.leaves(state):
                    bad=pre.changed(state,path,old+1 if type(old) is int else 'wrong')
                    rejected(lambda:read_state(encode(bad),q,created,preamble,w,history));mutations+=1
                for prior in range(i):
                    assert apply(q,created,preamble,w,history,inputs[prior])==(state,events[prior],True);boundaries+=1
                # Stale accepted inputs must still be authenticated before duplicate lookup.
                for j,inp in enumerate(inputs):
                    for path,value in [(('actor',),'axis'),(('actor',),'commonwealth'),
                            (('command','contractVersion'),1),(('command','kind'),'resolve-weather'),
                            (('command','expectedPriorVersion'),5),(('command','expectedPriorVersion'),10),
                            (('command','expectedPositionId'),'land.position.operation-1.weather'),
                            (('command','creationBinding'),'creation.forged'),(('command','creationEventHash'),sha(b'wrong'))]:
                        rejected(lambda:apply(q,created,preamble,w,history,pre.changed(inp,path,value)));boundaries+=1
                    if j>i: rejected(lambda:apply(q,created,preamble,w,history,inp));boundaries+=1
                for field,value in [('receipts',state['receipts'][:-1]),('receipts',state['receipts']*2),
                        ('operationStageWeather',[]),('operationStageWeather',state['operationStageWeather']*2),
                        ('operationStageOrders',[]),('operationStageOrders',state['operationStageOrders']*2)]:
                    bad=dict(state,**{field:value})
                    rejected(lambda:read_state(raw(bad,'StageEntryState'),q,created,preamble,w,history));boundaries+=1
            assert states[-1]['sequencePosition']['activeSide'] is None
            assert states[-1]['sequencePosition']['actorRole']=='first-acting-side'
            assert states[-1]['operationStageOrders'][0]['firstSide']==('axis' if choice=='act-first' else 'commonwealth')
            rejected(lambda:command(states[-1]));boundaries+=1
            for i,data in enumerate(events):
                event=parse(data,'StageEntryEvent')
                assert event['contractVersion']==2 and event['gameTurn']==event['operationStage']==1
                assert event['priorPrefix']==states[i]['prefix']
                assert states[i+1]['prefix']==pre.env.sequence.prefix_event(states[i]['prefix'],data)
                assert event['sources']==f['eventSources'][i]
                assert states[i+1]['receipts'][-1]==dict(commandHash=sha(raw(inputs[i],'StageEntryInput')),
                    eventHash=sha(data),receiptId=event['receiptId'],actor='system',stateVersion=7+i)
                for path,old in pre.leaves(event):
                    bad=pre.changed(event,path,old+1 if type(old) is int else 'wrong')
                    rejected(lambda:replay(q,created,preamble,w,events[:i]+[encode(bad)]));mutations+=1
                for field,value in [('gameTurn',2),('operationStage',2),('priorPrefix',sha(b'invented')),
                        ('sequencePosition',f['positions'][(i+2)%5]),('sources',event['sources'][:-1])]:
                    forged=dict(event,**{field:value});forged['receiptId']=receipt(forged)
                    rejected(lambda:replay(q,created,preamble,w,events[:i]+[raw(forged,'StageEntryEvent')]));boundaries+=1
                # Four same-shaped events still cannot substitute for one another.
                for other in events:
                    if other!=data: rejected(lambda:replay(q,created,preamble,w,events[:i]+[other]));boundaries+=1
                for sources in ([],event['sources']*2,list(reversed(event['sources']))):
                    rejected(lambda:replay(q,created,preamble,w,events[:i]+[encode(dict(event,sources=sources))]));boundaries+=1
                for kind,value in (('StageEntryEvent',event),('StageEntryInput',inputs[i]),('StageEntryState',states[i+1])):
                    encoded=raw(value,kind)
                    for bad in (b'\xef\xbb\xbf'+encoded,encoded+b' ',encoded[:-1]+b',"extra":null}',
                            encode(dict(reversed(list(value.items())))),b'{"contractVersion":true,'+encoded[1:],
                            encoded.replace(b'"contractVersion":1',b'"contractVersion":1.0',1) if kind=='StageEntryState'
                            else encoded.replace(b'"contractVersion":2',b'"contractVersion":2.0',1),
                            b'\xff',b'{"contractVersion":NaN}',b'['*40+b'0'+b']'*40):
                        rejected(lambda:parse(bad,kind));raw_rejects+=1
            for bad in ([],[w[0],w[0]],[preamble[-1]],[w[0]+b' ']):
                rejected(lambda:replay(q,created,preamble,bad,events));boundaries+=1
            for bad in (preamble[:-1],preamble[1:],list(reversed(preamble)),preamble+[preamble[-1]]):
                rejected(lambda:replay(q,created,bad,w,events));boundaries+=1
            for bad in (list(reversed(events)),events+[events[-1]],events[1:],events[:2]+events[3:],
                    [events[0]]*4,events+[w[0]]):
                rejected(lambda:replay(q,created,preamble,w,bad));boundaries+=1
            for bad in (b'',created+b' ',created.replace(b'"contractVersion":11',b'"contractVersion":10',1)):
                rejected(lambda:replay(q,bad,preamble,w,events));boundaries+=1
            for path,value in [(('campaignId',),'stage.other'),(('rulesetHash',),'0'*64),
                    (('configurationHash',),sha(b'wrong')),(('randomState','seed'),(case['seed']+1)%2**64)]:
                rejected(lambda:replay(pre.changed(q,path,value),created,preamble,w,events));boundaries+=1
            opposite=trace(case,'act-last' if choice=='act-first' else 'act-first')
            assert opposite[3]!=w and opposite[6][0]['operationStageWeather']==before['operationStageWeather']
            rejected(lambda:replay(q,created,opposite[2],opposite[3],events));boundaries+=1
            rejected(lambda:read_state(raw(states[-1],'StageEntryState'),q,created,opposite[2],opposite[3],opposite[5]));boundaries+=1
            assert result==retained
    policy=copy.deepcopy(pre.CONTEXT.setup['stageEntry'])
    require_policy(policy)
    for field in ('organization','navalConvoyArrival','fleetAssignment','fleetRepair'):
        for value in ('has-obligations','unknown',None):
            rejected(lambda:require_policy(dict(policy,**{field:value})));boundaries+=1
    for bad in (None,{},dict(policy,gameTurn=2),dict(policy,operationStage=2),dict(policy,contractVersion=2),
            dict(policy,contractVersion=True),dict(policy,sources=[]),dict(policy,sources=policy['sources']*2)):
        rejected(lambda:require_policy(bad));boundaries+=1
    # Isolated gate probe: context changes are not admitted new Setup7 identities.
    for field in ('organization','navalConvoyArrival','fleetAssignment','fleetRepair'):
        try:
            pre.CONTEXT.setup['stageEntry'][field]='has-obligations'
            rejected(lambda:initial(q,created,preamble,w));boundaries+=1
        finally: pre.CONTEXT.setup['stageEntry']=copy.deepcopy(policy)
    for kind in ('StageEntryInput','StageEntryEvent','StageEntryState'):
        rejected(lambda:parse(b' '*1048577,kind));raw_rejects+=1
    print(f'PASS Stage entry: {len(f["cases"])*2} creation-rooted traces, {cuts} replay/state cuts, '
        f'{mutations} leaf mutations, {raw_rejects} raw rejections, {boundaries} boundary/retry checks. '
        'Reserve-entry contract only; no runtime/Snapshot12 admission.')

if __name__=='__main__': main()
