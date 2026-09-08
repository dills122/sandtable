#!/usr/bin/env python3
"""Actual Breakdown completion into Combat entry; contract evidence, no runtime admission."""
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('breakdown_lifecycle',ROOT/'verify-combat-inherited-movement-lifecycle-v1.py')
lc = importlib.util.module_from_spec(spec); spec.loader.exec_module(lc)
encode,sha=lc.encode,lc.sha
INVENTORY=json.loads((ROOT/'combat-inherited-breakdown-completion-v1.schema.json').read_text())
FIXTURE=ROOT/'fixtures/combat-inherited-breakdown-completion-v1.json'
SCHEMA={k:[tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['objects'].items()}
BOUNDARY=lc.TERMINAL
edge=next(c for c in lc.CATALOG['cycles'] if c['operationStage']==1 and c['playerPhaseSlot']=='first-acting-side')
TERMINAL=next(p for p in lc.CATALOG['positions'] if p['positionId']==edge['combatPositionIds'][0])
KIND='complete-breakdown-segment'
ACTION=sha(encode(dict(contractVersion=1,kind=KIND,operationStage=1)))

class Invalid(ValueError):
    def __init__(self,code): self.code=f'CMB-IBC-{code:03}'; super().__init__(self.code)

def require(ok,code):
    if not ok: raise Invalid(code)

def typed(v,kind,depth=0):
    require(depth<=32,1)
    if kind in SCHEMA:
        require(type(v) is dict and set(v)=={k for k,_ in SCHEMA[kind]},1)
        for k,t in SCHEMA[kind]: typed(v[k],t,depth+1)
    else:
        try: lc.typed(v,kind,depth)
        except lc.Invalid as error: raise Invalid(1) from error

def canonical(v,kind):
    if kind in SCHEMA: return {k:canonical(v[k],t) for k,t in SCHEMA[kind]}
    return lc.canonical(v,kind)

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

def initial(request,created,preamble,weather,stage,reserve,moves,lifecycle):
    try: state=lc.replay(request,created,preamble,weather,stage,reserve,moves,lifecycle)
    except lc.Invalid as error: raise Invalid(4) from error
    require(len(lifecycle)==3 and state['sequencePosition']==BOUNDARY and state['breakdownFlow']==dict(kind='idle')
        and state['interruptContext'] is None and state['movementEnd'] is not None,6)
    state['breakdownCompletionReceiptId']=None
    return canonical(state,'CombatEntryState')

def command(state):
    return dict(command=dict(contractVersion=2,kind=KIND,operationStage=1,actionId=ACTION,
        creationBinding=state['creationBinding'],creationEventHash=state['creationEventHash'],cycleId=state['cycleId'],
        expectedPriorVersion=state['stateVersion'],expectedPositionId=state['sequencePosition']['positionId']),actor='system')

def authorize(state,inp):
    typed(inp,'EntryInput'); cmd=inp['command']
    require(cmd['contractVersion']==2 and cmd['kind']==KIND and cmd['operationStage']==1,3)
    require(inp['actor']=='system',5)
    require(cmd['creationBinding']==state['creationBinding'] and cmd['creationEventHash']==state['creationEventHash']
        and cmd['cycleId']==state['cycleId'],4)
    require(cmd['actionId']==ACTION,5)

def receipt(event):
    typed(event,'EntryEvent')
    unsigned={k:v for k,v in canonical(event,'EntryEvent').items() if k!='receiptId'}
    return 'ibc.'+hashlib.sha256(INVENTORY['domains']['receipt'].encode()+b'\0'+encode(unsigned)).hexdigest()

def _emit(state,inp):
    # Public callers reconstruct the entire prefix before using this private kernel.
    authorize(state,inp)
    require(state['breakdownCompletionReceiptId'] is None and state['sequencePosition']==BOUNDARY
        and state['breakdownFlow']==dict(kind='idle') and state['interruptContext'] is None
        and state['movementEnd'] is not None,6)
    require(inp['command']['expectedPriorVersion']==state['stateVersion']
        and inp['command']['expectedPositionId']==BOUNDARY['positionId'],6)
    require(state['stateVersion']<2**63-1 and len(state['receipts'])<512,7)
    version=state['stateVersion']+1
    event=dict(contractVersion=2,eventType='breakdown-segment-completed',campaignId=state['campaignId'],
        stateVersion=version,priorStateVersion=state['stateVersion'],rulesetHash=state['rulesetHash'],
        fromPositionId=BOUNDARY['positionId'],actionId=inp['command']['actionId'],sequencePosition=copy.deepcopy(TERMINAL),
        breakdownFlowAfter=dict(kind='idle'),sources=copy.deepcopy(BOUNDARY['sources']))
    event.update({k:copy.deepcopy(state[k]) for k in ('configurationHash','creationBinding','creationEventHash',
        'cycleId','openingBaseHash','completionReceiptId')})
    event.update(movementCompletionReceiptId=state['movementEnd']['completionReceiptId'],priorPrefix=state['prefix'],
        input=copy.deepcopy(inp),receiptId='pending')
    event['receiptId']=receipt(event); data=raw(event,'EntryEvent'); after=copy.deepcopy(state)
    after.update(stateVersion=version,sequencePosition=copy.deepcopy(TERMINAL),breakdownCompletionReceiptId=event['receiptId'],
        prefix=lc.im.rd.pre.env.sequence.prefix_event(state['prefix'],data))
    after['receipts'].append(dict(commandHash=sha(raw(inp,'EntryInput')),eventHash=sha(data),receiptId=event['receiptId'],
        actor='system',stateVersion=version))
    return canonical(after,'CombatEntryState'),data

def read_event(data):
    event=parse(data,'EntryEvent')
    require(event['contractVersion']==2 and event['eventType']=='breakdown-segment-completed',3)
    require(event['receiptId']==receipt(event),4)
    return event

def replay(request,created,preamble,weather,stage,reserve,moves,lifecycle,events):
    require(type(events) is list and len(events)<=1,7)
    state=initial(request,created,preamble,weather,stage,reserve,moves,lifecycle)
    for data in events:
        event=read_event(data); state,expected=_emit(state,event['input']); require(data==expected,6)
    return state

def apply(request,created,preamble,weather,stage,reserve,moves,lifecycle,events,inp):
    state=replay(request,created,preamble,weather,stage,reserve,moves,lifecycle,events)
    authorize(state,inp)
    if events:
        accepted=read_event(events[0])['input']
        require(raw(inp,'EntryInput')==raw(accepted,'EntryInput'),6)
        return state,events[0],True
    after,data=_emit(state,inp)
    return after,data,False

def read_state(data,request,created,preamble,weather,stage,reserve,moves,lifecycle,events):
    value=parse(data,'CombatEntryState')
    require(data==raw(replay(request,created,preamble,weather,stage,reserve,moves,lifecycle,events),'CombatEntryState'),6)
    return value

def source_trace(side,moves):
    case=next(c for c in json.loads(lc.FIXTURE.read_text())['cases'] if (c['actor'],c['moves'])==(side,moves))
    result=lc.trace(case); assert lc.goldens(result)==case['goldens']
    args,_,events,_=result
    return (*args,events)

def semantic_red():
    args=source_trace('axis',1); before=initial(*args); after,event=_emit(before,command(before))
    assert (after['sequencePosition']['positionId'],after['stateVersion'],after['prefix']!=before['prefix'])==(
        'land.position.operation-1.first-player.movement-and-combat.combat.position-determination',16,True), 'missing actual Combat-entry/version/prefix effect'
    assert after['world']==before['world'] and after['movementEnd']==before['movementEnd']
    assert after['breakdownCompletionReceiptId'] not in (None,before['completionReceiptId'],before['movementEnd']['completionReceiptId'])


def trace(case):
    args=source_trace(case['actor'],case['moves']); before=initial(*args); inp=command(before)
    after,event,duplicate=apply(*args,[],inp); assert not duplicate; value=read_event(event)
    assert before['stateVersion']==14+case['moves'] and after['stateVersion']==15+case['moves']
    assert before['sequencePosition']==BOUNDARY and after['sequencePosition']==TERMINAL
    assert TERMINAL['positionId']=='land.position.operation-1.first-player.movement-and-combat.combat.position-determination'
    assert lc.CATALOG['positions'][lc.CATALOG['positions'].index(BOUNDARY)+1]==TERMINAL
    assert value['sources']==[dict(sourceId='spi-1979-land-rules',locator=s) for s in ('5.2','7.11','7.14')]
    assert after['sequencePosition']['activeSide'] is None and after['sequencePosition']['actorRole']=='first-acting-side'
    assert after['cycle']['actingSide']==case['actor'] and after['cycle']['ordinal']==1
    assert all(after[k]==before[k] for k in before if k not in (
        'stateVersion','prefix','receipts','sequencePosition','breakdownCompletionReceiptId'))
    unit=before['members'][0]['unit']; element=next(e for e in after['world']['elements'] if e['elementId']==unit['elementId'])
    assert element['operationalState']['capabilityPointsExpended']==lc.im.world.cp(case['expectedCp'])
    assert element['operationalState']['cohesionLevel']==case['expectedCohesion']
    assert len(after['movementEnd']['excludedBefore'])==case['expectedExclusions']
    assert value['movementCompletionReceiptId']==before['movementEnd']['completionReceiptId']
    assert len({after['breakdownCompletionReceiptId'],after['completionReceiptId'],value['movementCompletionReceiptId']})==3
    assert after['breakdownCompletionReceiptId']==value['receiptId']
    assert after['prefix']==lc.im.rd.pre.env.sequence.prefix_event(before['prefix'],event)!=before['prefix']
    assert after['receipts'][:-1]==before['receipts']
    assert after['receipts'][-1]==dict(commandHash=sha(raw(inp,'EntryInput')),eventHash=sha(event),
        receiptId=value['receiptId'],actor='system',stateVersion=15+case['moves'])
    assert read_state(raw(before,'CombatEntryState'),*args,[])==before
    assert read_state(raw(after,'CombatEntryState'),*args,[event])==after
    retried,accepted,duplicate=apply(*args,[event],inp)
    assert duplicate and accepted==event and retried==after
    return args,inp,event,before,after

def goldens(result):
    args,inp,event,before,after=result
    records=[encode(args[0]),args[1]]+[r for group in args[2:] for r in group]
    values=[encode([sha(r) for r in records]),raw(inp,'EntryInput'),event,raw(before,'CombatEntryState'),raw(after,'CombatEntryState')]
    return dict(bytes=[len(v) for v in values],sha256=[sha(v) for v in values])

def rejected(fn):
    try: fn()
    except Invalid: return
    raise AssertionError('expected CMB-IBC rejection')

def verify(result,deep):
    args,inp,event,before,after=result; frozen=copy.deepcopy(result)
    counts=dict(cuts=2,retries=1,mutations=0,raw=0,boundaries=0)
    assert replay(*args,[])==before and replay(*args,[event])==after
    for path,value in ((('actor',),'axis'),(('actor',),'commonwealth'),(('command','operationStage'),2),
        (('command','operationStage'),True),(('command','kind'),'complete-movement-segment'),
        (('command','contractVersion'),1),(('command','actionId'),sha(encode(dict(contractVersion=1,kind=KIND)))),
        (('command','creationBinding'),'foreign.creation'),(('command','creationEventHash'),'sha256:'+'f'*64),
        (('command','cycleId'),'sha256:'+'e'*64),(('command','expectedPriorVersion'),before['stateVersion']-1),
        (('command','expectedPriorVersion'),after['stateVersion']+1),(('command','expectedPositionId'),TERMINAL['positionId'])):
        bad=lc.im.changed(inp,path,value)
        for events in ([],[event]):
            rejected(lambda b=bad,e=events:apply(*args,e,b)); counts['boundaries']+=1
    rejected(lambda:apply(*args,[event],command(after))); counts['boundaries']+=1
    rejected(lambda:replay(*args,[event,event])); counts['boundaries']+=1
    # Missing/unfinished/out-of-order lifecycle prefix never admits the new event.
    for suffix in ([],args[-1][:1],args[-1][:2],args[-1][1:],list(reversed(args[-1])),[args[-1][0],args[-1][2]]):
        bad=list(copy.deepcopy(args)); bad[-1]=suffix
        rejected(lambda a=bad:replay(*a,[event])); counts['boundaries']+=1
    if deep:
        value=read_event(event)
        for path,leaf in lc.im.leaves(value):
            bad=lc.im.changed(value,path,lc.im.different(leaf))
            try:
                if path!=('receiptId',): bad['receiptId']=receipt(bad)
                encoded=raw(bad,'EntryEvent')
            except Invalid: encoded=encode(bad)
            rejected(lambda b=encoded:replay(*args,[b])); counts['mutations']+=1
        for i,state in enumerate((before,after)):
            for path,leaf in lc.im.leaves(state):
                if path[0] not in ('cycle','movementEnd','actualProgressRefs','breakdownFlow','interruptContext',
                    'breakdownCompletionReceiptId','prefix','stateVersion','randomState','members'): continue
                bad=lc.im.changed(state,path,lc.im.different(leaf))
                try: encoded=raw(bad,'CombatEntryState')
                except Invalid: encoded=encode(bad)
                rejected(lambda b=encoded,n=i:read_state(b,*args,[] if n==0 else [event])); counts['mutations']+=1
        # Retained complete World must not be replaced by a plausible CP/ammo/cache rewrite.
        for path,replacement in ((('world','elements',0,'currentLocationId'),'axis-supply'),
            (('world','elements',0,'ammunition','points'),0),(('world','representations',0,'currentLocationId'),'assault-west')):
            old=after
            for key in path: old=old[key]
            if old==replacement: replacement='foreign.location'
            bad=lc.im.changed(after,path,replacement)
            rejected(lambda b=bad:read_state(raw(b,'CombatEntryState'),*args,[event])); counts['mutations']+=1
        for index in range(1,7):
            bad=list(copy.deepcopy(args)); bad[index]=args[index]+b' ' if index==1 else args[index][:-1]
            rejected(lambda a=bad:replay(*a,[event])); counts['boundaries']+=1
        for side,moves in ((before['firstActingSide'],7),('commonwealth' if before['firstActingSide']=='axis' else 'axis',6)):
            foreign=source_trace(side,moves)
            rejected(lambda a=foreign:replay(*a,[event])); counts['boundaries']+=1
            rejected(lambda a=foreign:read_state(raw(after,'CombatEntryState'),*a,[])); counts['boundaries']+=1
        # Forged synthetic opening/request identity cannot be repaired by the new event's hashes.
        bad=list(copy.deepcopy(args)); bad[0]=copy.deepcopy(args[0]); bad[0]['creationBinding']='foreign.creation'
        rejected(lambda:replay(*bad,[event])); counts['boundaries']+=1
        for data,kind in ((event,'EntryEvent'),(raw(inp,'EntryInput'),'EntryInput'),
            (raw(before,'CombatEntryState'),'CombatEntryState'),(raw(after,'CombatEntryState'),'CombatEntryState')):
            value=json.loads(data); first=next(iter(value))
            variants=[data+b' ',b' '+data,b'\xef\xbb\xbf'+data,data.replace(b':',b': ',1),
                b'{"unknown":0,'+data[1:],b'{'+encode(first)+b':null,'+data[1:],
                encode(dict(reversed(list(value.items())))),data.replace(b'contractVersion',b'contractVersio\\u006e',1),
                data.replace(b'"contractVersion":1',b'"contractVersion":1.0',1) if b'"contractVersion":1' in data
                    else data.replace(b'"contractVersion":2',b'"contractVersion":2.0',1),
                data.replace(b'"contractVersion":1',b'"contractVersion":true',1) if b'"contractVersion":1' in data
                    else data.replace(b'"contractVersion":2',b'"contractVersion":true',1)]
            for bad in variants:
                assert bad!=data
                rejected(lambda b=bad,t=kind:parse(b,t)); counts['raw']+=1
        for bad in (b'',b'x'*1048577,b'{"x":'+b'['*1100+b'0'+b']'*1100+b'}',b'NaN',b'null',b'[]'):
            rejected(lambda b=bad:read_event(b)); counts['raw']+=1
        for field,val in (('stateVersion',2**63-1),('receipts',before['receipts']*50)):
            bad=copy.deepcopy(before); bad[field]=val
            rejected(lambda s=bad:_emit(s,command(s))); counts['boundaries']+=1
    assert result==frozen, 'verification mutated accepted inputs/history/state'
    return counts

PIN_PATHS=[
    'docs/specs/combat-inherited-movement-lifecycle-v1.schema.json',
    'docs/specs/verify-combat-inherited-movement-lifecycle-v1.py',
    'docs/specs/fixtures/combat-inherited-movement-lifecycle-v1.json',
    'docs/specs/combat-cycle-sequence-v1.schema.json',
    'src/Cna.Core/Campaigns/CampaignBreakdownLifecycleFactory.cs',
    'src/Cna.Core/Campaigns/CampaignBreakdownLifecycleCodec.cs',
    'src/Cna.Core/Campaigns/CampaignV11BreakdownProjector.cs',
    'src/Cna.Core/Actions/BreakdownActionCandidates.cs',
    'src/Cna.Core/Actions/CampaignActionCandidate.cs',
    'src/Cna.Core/Rules/Cna1979LandSequence.cs',
]

def check_fixture(fixture):
    assert set(fixture)=={'contractVersion','cases','sourcePins'} and fixture['contractVersion']==1
    assert len(fixture['cases'])==8 and {(c['actor'],c['moves']) for c in fixture['cases']}=={
        (s,n) for s in ('axis','commonwealth') for n in (1,5,6,7)}
    for case in fixture['cases']:
        n=case['moves']; assert type(n) is int
        assert set(case)=={'actor','moves','expectedCp','expectedCohesion','expectedExclusions','goldens'}
        assert case['expectedCp']==n*2 and case['expectedCohesion']==-max(0,n*2-10)
        assert case['expectedExclusions']==(1 if n==6 else 0)
    assert [pin['path'] for pin in fixture['sourcePins']]==PIN_PATHS
    for pin in fixture['sourcePins']:
        assert sha((ROOT.parent.parent/pin['path']).read_bytes())==pin['sha256'],'source drift: '+pin['path']

def main():
    fixture=json.loads(FIXTURE.read_text()); check_fixture(fixture); semantic_red()
    total=dict(cuts=0,retries=0,mutations=0,raw=0,boundaries=0)
    for case in fixture['cases']:
        result=trace(case); assert goldens(result)==case['goldens'],str((case['actor'],case['moves']))+' golden drift'
        for k,v in verify(result,case['moves']==6).items(): total[k]+=v
    print('PASS inherited Breakdown completion: '+str(len(fixture['cases']))+' traces/events, '+
        ', '.join(str(v)+' '+k for k,v in total.items())+', '+str(len(fixture['sourcePins']))+' source pins')

if __name__=='__main__': main()
