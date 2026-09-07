#!/usr/bin/env python3
"""D2c.2a: actual prospective creation-to-Weather-entry chain, no C# runtime proof."""
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('envelope', ROOT/'verify-combat-authority-envelope-v1.py')
env = importlib.util.module_from_spec(spec)
spec.loader.exec_module(env)
CONTEXT = env.Context()
INVENTORY = json.loads((ROOT/'combat-opening-preamble-v1.schema.json').read_text())
FIXTURE = ROOT/'fixtures/combat-opening-preamble-v1.json'
SCHEMA = env.SCHEMA | {k: [tuple(f.split(':')) for f in v.split()] for k,v in INVENTORY['objects'].items()}
encode, sha = env.encode, env.sha

class Invalid(ValueError):
    def __init__(self, code):
        self.code = f'CMB-PRE-{code:03}'
        super().__init__(self.code)

def require(ok, code):
    if not ok: raise Invalid(code)

def typed(v, kind, depth=0):
    require(depth <= 32, 1)
    if kind.endswith('?'):
        if v is not None: typed(v, kind[:-1], depth)
    elif kind == 'actor': require(type(v) is str and v in ('system','axis','commonwealth'), 1)
    elif kind == 'PreambleEvent':
        require(type(v) is dict and type(v.get('eventType')) is str and v['eventType'] in INVENTORY['eventTags'], 3)
        typed(v, INVENTORY['eventTags'][v['eventType']], depth)
    elif kind in SCHEMA:
        require(type(v) is dict and set(v)=={k for k,_ in SCHEMA[kind]}, 1)
        for k,t in SCHEMA[kind]: typed(v[k],t,depth+1)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v)<=512, 1)
        for item in v: typed(item,kind[:-2],depth+1)
    else:
        try: env.typed(v,kind,depth=depth)
        except env.Invalid as error: raise Invalid(1) from error

def canonical(v, kind):
    if kind.endswith('?'): return None if v is None else canonical(v,kind[:-1])
    if kind=='PreambleEvent': return canonical(v,INVENTORY['eventTags'][v['eventType']])
    if kind in SCHEMA: return {k:canonical(v[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'):
        child=kind[:-2]; out=[canonical(x,child) for x in v]
        if child in env.KEYS: out.sort(key=lambda x:tuple(x[k] for k in env.KEYS[child]))
        elif kind=='id[]': out.sort()
        return out
    return v

def raw(v, kind):
    typed(v,kind); data=encode(canonical(v,kind)); require(len(data)<=1048576,1)
    return data

def parse(data, kind):
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

def position(index): return copy.deepcopy(env.sequence.expected_catalog()['positions'][index])
def sources(*groups): return sorted([copy.deepcopy(x) for group in groups for x in group],key=lambda x:(x['sourceId'],x['locator']))
def land(*locators): return [dict(sourceId='spi-1979-land-rules',locator=x) for x in locators]

def created_for(request): return env.encode(env.canonical(env.make_created(request,CONTEXT),'Created'))

def initial(request, created):
    try:
        env.typed(request,'Request')
        q=env.read_request(env.encode(env.canonical(request,'Request')),CONTEXT)
        state=env.make_snapshot(created,q,CONTEXT)
    except env.Invalid as error: raise Invalid(4) from error
    return dict(contractVersion=1,campaignId=q['campaignId'],rulesetHash=q['rulesetHash'],
        configurationHash=q['configurationHash'],creationBinding=env.binding(q),creationEventHash=sha(created),
        stateVersion=1,prefix=state['chroniclePrefix'],sequencePosition=copy.deepcopy(state['currentPosition']['sequencePosition']),
        initiativeHolder=None,operationStageOrders=[],world=copy.deepcopy(state['world']),
        randomState=copy.deepcopy(state['randomState']),receipts=[])

def command(state, choice=None):
    require(1<=state['stateVersion']<=4,6)
    row=INVENTORY['events'][state['stateVersion']-1]
    declaration=row['command']=='declare-initiative-order'
    return dict(command=dict(contractVersion=row['commandVersion'],kind=row['command'],creationBinding=state['creationBinding'],
        creationEventHash=state['creationEventHash'],expectedPriorVersion=state['stateVersion'],
        expectedPositionId=state['sequencePosition']['positionId'],operationStage=1 if declaration else None,
        declaringSide=state['initiativeHolder'] if declaration else None,choice=choice),
        actor=state['initiativeHolder'] if declaration else 'system')

def _authorize(state, inp):
    typed(inp,'PreambleInput')
    rows=[row for row in INVENTORY['events'] if row['command']==inp['command']['kind']]
    require(len(rows)==1 and inp['command']['contractVersion']==rows[0]['commandVersion'],3)
    owner=state['initiativeHolder'] if rows[0]['actor']=='initiative-holder' else 'system'
    require(inp['actor']==owner,5)

def _emit(state, inp):
    # Internal kernel: state is produced only by initial/replay, never a caller's saved projection.
    _authorize(state,inp)
    index=state['stateVersion']-1
    require(0<=index<4,6)
    require(state['sequencePosition']==position(index),4)
    choice=inp['command']['choice']
    require(choice in ('act-first','act-last') if index==3 else choice is None,3)
    require(inp==command(state,choice),4)
    row=INVENTORY['events'][index]; cursor=state['randomState']['nextByteCursor']
    event=dict(contractVersion=row['eventVersion'],eventType=row['eventType'],campaignId=state['campaignId'],
        rulesetHash=state['rulesetHash'],configurationHash=state['configurationHash'],creationBinding=state['creationBinding'],
        creationEventHash=state['creationEventHash'],priorVersion=state['stateVersion'],stateVersion=state['stateVersion']+1,
        priorPrefix=state['prefix'],fromPositionId=state['sequencePosition']['positionId'],input=copy.deepcopy(inp))
    if index==0:
        policy=CONTEXT.setup['initialInitiative']
        require(policy==dict(kind='predetermined',holder='axis'),3)
        event.update(outcome=copy.deepcopy(policy),randomAlgorithmId=state['randomState']['algorithmId'],
            randomCursorBefore=cursor,randomCursorAfter=cursor)
        refs=sources(CONTEXT.setup['sources'],land('7.12','7.15'))
    elif index in (1,2):
        policy=CONTEXT.setup['openingPreamble']
        require(policy==dict(contractVersion=1,kind='no-opening-naval-convoy-obligations',
            sources=[dict(sourceId='sandtable-rules-lab',locator='opening-preamble.no-naval-convoy-obligations.v1')]),3)
        require(state['initiativeHolder']=='axis',4)
        refs=sources(policy['sources'],land('5.2'))
    else:
        first=state['initiativeHolder'] if choice=='act-first' else 'commonwealth'
        second='commonwealth' if first=='axis' else 'axis'
        event.update(operationStage=1,declaringHolder=state['initiativeHolder'],firstSide=first,secondSide=second)
        refs=land('5.2','7.11','7.14','7.16')
    event.update(sequencePosition=position(index+1),sources=refs,receiptId='pending')
    unsigned={k:v for k,v in canonical(event,'PreambleEvent').items() if k!='receiptId'}
    event['receiptId']='pre.'+hashlib.sha256(INVENTORY['domains']['receipt'].encode()+b'\0'+encode(unsigned)).hexdigest()
    data=raw(event,'PreambleEvent'); after=copy.deepcopy(state)
    after.update(stateVersion=event['stateVersion'],prefix=env.sequence.prefix_event(state['prefix'],data),
        sequencePosition=copy.deepcopy(event['sequencePosition']))
    if index==0: after['initiativeHolder']=event['outcome']['holder']
    if index==3:
        after['operationStageOrders'].append(dict(contractVersion=1,gameTurn=1,operationStage=1,
            firstSide=event['firstSide'],secondSide=event['secondSide']))
    after['receipts'].append(dict(commandHash=sha(raw(inp,'PreambleInput')),eventHash=sha(data),
        receiptId=event['receiptId'],actor=inp['actor'],stateVersion=event['stateVersion']))
    return after,data

def replay(request, created, events):
    state=initial(request,created)
    require(type(events) is list and len(events)<=4,6)
    for data in events:
        value=parse(data,'PreambleEvent')
        state,expected=_emit(state,value['input'])
        require(data==expected,6)
    return state

def apply(request, created, events, inp):
    state=replay(request,created,events)
    _authorize(state,inp)
    for data in events:
        accepted=parse(data,'PreambleEvent')['input']
        if accepted['command']['expectedPriorVersion']==inp['command']['expectedPriorVersion']:
            require(raw(inp,'PreambleInput')==raw(accepted,'PreambleInput'),6)
            return state,data,True
    after,data=_emit(state,inp)
    return after,data,False

def read_state(data, request, created, events):
    value=parse(data,'PreambleState'); expected=replay(request,created,events)
    require(data==raw(expected,'PreambleState'),6)
    return value

def rejected(fn):
    try: fn()
    except Invalid: return
    raise AssertionError('expected CMB-PRE rejection')

def leaves(value,path=()):
    if type(value) is dict:
        for k,v in value.items(): yield from leaves(v,path+(k,))
    elif type(value) is list:
        for k,v in enumerate(value): yield from leaves(v,path+(k,))
    else: yield path,value

def changed(value,path,replacement):
    out=copy.deepcopy(value); node=out
    for k in path[:-1]: node=node[k]
    node[path[-1]]=replacement
    return out

def trace(case):
    q=CONTEXT.request(seed=case['seed']); created=created_for(q)
    state=initial(q,created); states=[copy.deepcopy(state)]; events=[]; inputs=[]
    assert state['prefix']==env.sequence.prefix_creation(created)
    for i in range(4):
        inp=command(state,case['choice'] if i==3 else None)
        state,data,duplicate=apply(q,created,events,inp)
        assert not duplicate and state['stateVersion']==i+2 and len(state['receipts'])==i+1
        event=parse(data,'PreambleEvent')
        assert event['priorPrefix']==states[-1]['prefix'] and state['prefix']==env.sequence.prefix_event(states[-1]['prefix'],data)
        assert event['stateVersion']==states[-1]['stateVersion']+1
        assert state['world']==states[0]['world'] and state['randomState']==q['randomState']
        assert state['initiativeHolder']=='axis' and state['randomState']['nextByteCursor']==0
        events.append(data); inputs.append(inp); states.append(copy.deepcopy(state))
    assert [s['sequencePosition']['positionId'] for s in states]==case['positions']
    assert state['operationStageOrders']==[dict(contractVersion=1,gameTurn=1,operationStage=1,
        firstSide=case['expectedFirst'],secondSide=case['expectedSecond'])]
    assert [json.loads(e)['contractVersion'] for e in events]==[3,2,2,2]
    return q,created,inputs,events,states

def verify_sources(f):
    for name,digest in f['sourceHashes'].items(): require(sha((ROOT.parent.parent/name).read_bytes())==digest,9)
    require(sha(encode(INVENTORY['events']))==f['declarationsHash'],9)
    prior=json.loads((ROOT/'combat-inherited-successors-v1.schema.json').read_text())['inheritedEvents']
    expected={r['eventType']:r['successorVersion'] for r in prior}
    require(all(row['eventVersion']==expected[row['eventType']] for row in INVENTORY['events']),9)
    require(len(INVENTORY['events'])==len(INVENTORY['eventTags'])==4,9)
    source=(ROOT.parent.parent/'src/Cna.Core/Campaigns/CampaignV11PreambleCodec.cs').read_text()
    require('version is not (3 or 4)' in source,9)

def goldens(created,events,states):
    values=[('creation',created)]+[(f'event-{i+1}',v) for i,v in enumerate(events)]+[
        (f'state-{i}',raw(v,'PreambleState')) for i,v in enumerate(states)]
    return {k:dict(bytes=len(v),sha256=sha(v)) for k,v in values}

def main():
    f=json.loads(FIXTURE.read_text()); verify_sources(f)
    assert len(f['cases'])==6 and {(c['seed'],c['choice']) for c in f['cases']}=={
        (seed,choice) for seed in (0,12345,18446744073709551615) for choice in ('act-first','act-last')}
    cuts=event_mutations=state_mutations=raw_rejects=boundaries=0
    for case in f['cases']:
        q,created,inputs,events,states=trace(case)
        assert goldens(created,events,states)==case['goldens']
        for i,state in enumerate(states):
            suffix=events[:i]; data=raw(state,'PreambleState')
            assert replay(q,created,suffix)==state and read_state(data,q,created,suffix)==state; cuts+=1
            for path,old in leaves(state):
                replacement=old+1 if type(old) is int else ('axis' if old=='commonwealth' else 'wrong')
                bad=changed(state,path,replacement)
                rejected(lambda:read_state(encode(bad),q,created,suffix)); state_mutations+=1
            if i:
                for j,inp in enumerate(inputs[:i]):
                    assert apply(q,created,suffix,inp)==(state,events[j],True)
                    rejected(lambda:apply(q,created,suffix,dict(inp,actor='commonwealth'))); boundaries+=1
        for i,data in enumerate(events):
            event=json.loads(data)
            for path,old in leaves(event):
                replacement=old+1 if type(old) is int else ('axis' if old=='commonwealth' else 'wrong')
                bad=changed(event,path,replacement)
                rejected(lambda:replay(q,created,events[:i]+[encode(bad)])); event_mutations+=1
            for bad in (b'\xef\xbb\xbf'+data,data+b' ',data.replace(b'"contractVersion":',b'"contractVersion":true,"contractVersion":',1),
                    data[:-1]+b',"unknown":null}',encode(dict(reversed(list(event.items())))),
                    encode(dict(event,eventType=[])),encode(dict(event,contractVersion=2.0)),
                    data.replace(b'"priorVersion":',b'"priorVersion":null,"priorVersion":',1)):
                rejected(lambda:replay(q,created,events[:i]+[bad])); raw_rejects+=1
        for bad_created in (b'',created.replace(b'"contractVersion":11',b'"contractVersion":10',1),created+b' '):
            rejected(lambda:replay(q,bad_created,events)); boundaries+=1
        for suffix in (events[1:],list(reversed(events)),events+[events[-1]],events[:2]+[events[1]]):
            rejected(lambda:replay(q,created,suffix)); boundaries+=1
        last=inputs[-1]
        for path,value in [(('command','choice'),'act-middle'),(('command','operationStage'),2),
                (('command','contractVersion'),1),(('command','expectedPriorVersion'),3),
                (('command','declaringSide'),'commonwealth'),(('command','creationBinding'),'creation.forged')]:
            rejected(lambda:apply(q,created,events[:-1],changed(last,path,value))); boundaries+=1
        opposite='act-last' if case['choice']=='act-first' else 'act-first'
        rejected(lambda:apply(q,created,events,changed(last,('command','choice'),opposite))); boundaries+=1
        fork=dict(q,campaignId='preamble.other-campaign'); fork_created=created_for(fork)
        rejected(lambda:replay(fork,fork_created,events)); boundaries+=1
        rejected(lambda:replay(fork,created,events)); boundaries+=1
        fork_after,fork_event,_=apply(q,created,events[:-1],changed(last,('command','choice'),opposite))
        assert fork_after['prefix']!=states[-1]['prefix'] and fork_after['operationStageOrders']!=states[-1]['operationStageOrders']
        rejected(lambda:read_state(raw(states[-1],'PreambleState'),q,created,events[:-1]+[fork_event])); boundaries+=1
        for path,value in [(('rulesetHash',),'0'*64),(('configurationHash',),sha(b'wrong')),
                (('content','schemaVersion'),6),(('randomState','seed'),-1),(('randomState','seed'),2**64),
                (('randomState','seed'),True),(('randomState','nextByteCursor'),1)]:
            rejected(lambda:replay(changed(q,path,value),created,events)); boundaries+=1
        assert (q,created,inputs,events,states)==trace(case)  # Caller objects unchanged after every rejection.
    print(f'PASS opening preamble: 6 creation-rooted traces, {cuts} cuts, {event_mutations} event mutations, '
        f'{state_mutations} state mutations, {raw_rejects} raw rejections, {boundaries} boundary/retry checks. '
        'Weather-entry contract evidence only; no runtime replay.')

if __name__=='__main__': main()
