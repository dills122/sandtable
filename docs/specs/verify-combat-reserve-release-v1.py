#!/usr/bin/env python3
"""Private Reserve Release arm; isolated ledger probes, no runtime campaign admission."""
import copy
import importlib.util
import json
import math
import sys
from functools import lru_cache
from fractions import Fraction
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('result_contract',ROOT/'verify-combat-result-settlement-v1.py')
r=importlib.util.module_from_spec(spec);spec.loader.exec_module(r)
steps,world=r.steps,r.world
encode,sha=r.encode,r.sha
FIXTURE=ROOT/'fixtures/combat-reserve-release-v1.json'
INVENTORY=json.loads((ROOT/'combat-reserve-release-v1.schema.json').read_text())
SCHEMA=r.SCHEMA|{k:[tuple(f.split(':')) for f in v.split()] for k,v in INVENTORY['objects'].items()}
MAX=2147483647
CONTEXT=r.env.Context()

class Invalid(ValueError):
    def __init__(self,code):self.code=f'CMB-RRL-{code:03}';super().__init__(self.code)

def require(ok,code):
    if not ok:raise Invalid(code)

def typed(v,kind,depth=0):
    require(depth<=32,1)
    if kind.endswith('?'):
        if v is not None:typed(v,kind[:-1],depth)
    elif kind=='ReleaseEffect':
        require(type(v) is dict and v.get('kind') in INVENTORY['effectTags'],3)
        typed(v,INVENTORY['effectTags'][v['kind']],depth)
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
    if kind=='ReleaseEffect':return canonical(v,INVENTORY['effectTags'][v['kind']])
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

def key(u):return u['creationBinding'],u['originalSide'],u['elementId']
def scope(c):return {k:c[k] for k in ('gameTurn','operationStage','playerPhaseSlot','actingSide')}
def digest(kind,v):return steps.digest(INVENTORY['domains'][kind],encode(v))
def cycle_id(b):return sha(steps.seq.identity(b['cycle'],'Authority',b['firstActingSide']))
def release_id(b):return 'rel.'+digest('release',dict(baseHash=sha(raw(b,'ReleaseBase')),cycleId=cycle_id(b),positionId=b['positionId']))
def ceiling(kind,cpa):return cpa if kind=='I' else cpa//2

def choices(status,ordinal):
    require(type(ordinal) is int and 1<=ordinal<=MAX,2)
    if status=='none':return ()
    if status=='I' and ordinal==1:return ('release-I','convert-to-II')
    if status=='II' and ordinal>1:return ('release-II','retain-II')
    raise Invalid(3)

def empty_history(c):
    return dict(scope=scope(c),designationReceiptId=None,conversionReceiptId=None,releasedType=None,releaseReceiptId=None,
        releaseCycle=None,cpaBasis=None,voluntaryCeiling=None,offensiveCommitmentId=None,nextMovement=None)

def validate_base(b):
    typed(b,'ReleaseBase');c=b['cycle'];q=CONTEXT.request()
    require(b['contractVersion']==1 and b['profile'] in ('isolated-ledger','settled-empty-release'),3)
    try:steps.seq.identity(c,'Authority',b['firstActingSide'])
    except steps.seq.Invalid as e:raise Invalid(4) from e
    require(c['campaignId']==q['campaignId'] and c['rulesetHash']==q['rulesetHash'] and c['setupHash']==q['setupHash']
            and c['setupId']==q['setupId'] and c['contentPackId']==q['content']['packId'] and c['scenarioId']==q['content']['scenarioId']
            and c['contentHash']==q['content']['hash'] and c['admittedPolicyBundleDigest']==q['configurationHash'],4)
    edge=next(e for e in steps.seq.expected_catalog()['cycles'] if e['operationStage']==c['operationStage'] and e['playerPhaseSlot']==c['playerPhaseSlot'])
    require(b['positionId']==edge['releasePositionId'] and c['openedAuthorityVersion']<=b['priorVersion']<2**63,4)
    members=b['members'];keys=[key(m['unit']) for m in members]
    require(len(members)<=32 and keys==sorted(set(keys)),4)
    for m in members:
        h=m['history'];u=m['unit'];cp=m['spentCp']
        require(u['originalSide']==c['actingSide'] and u['creationBinding']==r.env.binding(q),4)
        require(1<=m['baseCpa']<=MAX and cp['numerator']>=0 and cp['denominator']>0 and math.gcd(cp['numerator'],cp['denominator'])==1,2)
        require(h['scope']==scope(c),4);choices(m['status'],c['ordinal'])
        if m['status'] in ('I','II'):require(h['designationReceiptId'] is not None,4)
        if m['status']=='I':require(h['conversionReceiptId'] is None,4)
        if m['status']=='II':require(h['conversionReceiptId'] is not None,4)
        if h['releasedType'] is None:
            require(all(h[k] is None for k in ('releaseReceiptId','releaseCycle','cpaBasis','voluntaryCeiling','offensiveCommitmentId','nextMovement')),4)
            if m['status']=='none':require(h['designationReceiptId'] is None and h['conversionReceiptId'] is None,4)
        else:
            require(m['status']=='none' and h['releasedType'] in ('I','II') and h['designationReceiptId'] is not None and h['releaseReceiptId'] is not None,4)
            require(1<=h['releaseCycle']<c['ordinal'] and h['cpaBasis']==m['baseCpa'] and h['voluntaryCeiling']==ceiling(h['releasedType'],m['baseCpa']),4)
            require((h['conversionReceiptId'] is not None)==(h['releasedType']=='II'),4)
            require(h['releaseCycle']==1 if h['releasedType']=='I' else h['releaseCycle']>1,4)
            ex=h['nextMovement'];require(ex is not None and ex['scope']==scope(c) and ex['ordinal']==h['releaseCycle']+1,4)
            require(ex['status']=='expired' and ex['completionReceiptId'] is not None,4)
            if h['offensiveCommitmentId'] is not None:
                matches=[x for x in b['attackHistory'] if x['commitmentId']==h['offensiveCommitmentId'] and x['attacker']==u and (x['gameTurn'],x['operationStage'])==(c['gameTurn'],c['operationStage'])]
                require(len(matches)==1,4)
    return b

@lru_cache(maxsize=4)
def settled_source(name,side):
    case=copy.deepcopy(next(c for c in json.loads(r.FIXTURE.read_text())['cases'] if c['name']==name));case['attackerSide']=side
    ctx,s,ins,events=r.trace_case(case);assert r.replay(ctx,ins,events)==s
    return ctx,s

@lru_cache(maxsize=1)
def cycle_template():return r.rnd.start()[0]['boundary']['cycle']

def base_for(case,side='axis',slot='first-acting-side'):
    q=CONTEXT.request();c=copy.deepcopy(cycle_template())
    first=side if slot=='first-acting-side' else ('axis' if side=='commonwealth' else 'commonwealth')
    c.update(actingSide=side,playerPhaseSlot=slot,ordinal=case['ordinal'],openedAuthorityVersion=20)
    world_hash=sha(b'synthetic retained World for isolated ledger probe');rng=q['randomState'];version=100;high=1000;history=[]
    proof='probe.combat-completed';prefix=sha(encode(dict(probe=case['name'],side=side,slot=slot)))
    if case.get('settledCase'):
        require(slot=='first-acting-side',3)
        ctx,s=settled_source(case['settledCase'],side);c=copy.deepcopy(ctx['base']['boundary']['cycle'])
        world_hash=sha(r.raw(s['world'],'World'));rng=s['randomState'];version=s['stateVersion'];high=s['acceptedHighWater'];proof=s['caCompletionReceiptId'];prefix=s['prefix'];history=ctx['committed']['attackHistory']
    members=[]
    element=next(e for e in world.PACK['elements'] if e['sideId']==side)
    for i,status in enumerate(case['statuses']):
        u=dict(creationBinding=r.env.binding(q),originalSide=side,elementId=element['elementId']+('' if i==0 else f'.probe-{i+1}'))
        h=empty_history(c);cpa=case.get('cpa',[10]*len(case['statuses']))[i];spent=case.get('spent',[0]*len(case['statuses']))[i]
        if status in ('I','II'):h['designationReceiptId']=f'probe.designation.{i}'
        if status=='II':h['conversionReceiptId']=f'probe.conversion.{i}'
        if case.get('priorRelease'):
            h.update(designationReceiptId='probe.designation.0',releasedType='I',releaseReceiptId='probe.release.0',releaseCycle=1,cpaBasis=cpa,voluntaryCeiling=cpa,
                nextMovement=dict(scope=scope(c),ordinal=2,status='expired',completionReceiptId='probe.movement-completed'))
        if case.get('settledCase'):
            e=next(e for e in s['world']['elements'] if e['elementId']==u['elementId']);spent=e['operationalState']['capabilityPointsExpended']['numerator']
        members.append(dict(unit=u,status=status,baseCpa=cpa,spentCp=world.cp(spent),history=h))
    edge=next(x for x in steps.seq.expected_catalog()['cycles'] if x['operationStage']==c['operationStage'] and x['playerPhaseSlot']==slot)
    b=dict(contractVersion=1,profile='settled-empty-release' if case.get('settledCase') else 'isolated-ledger',cycle=c,firstActingSide=first,positionId=edge['releasePositionId'],
        priorVersion=version,priorPrefix=prefix,combatCompletionReceiptId=proof,retainedWorldHash=world_hash,randomState=copy.deepcopy(rng),acceptedHighWater=high,members=sorted(members,key=lambda m:key(m['unit'])),attackHistory=copy.deepcopy(history))
    return validate_base(b)

def read_base(data,case,side='axis',slot='first-acting-side'):
    b=parse(data,'ReleaseBase');validate_base(b);expected=base_for(case,side,slot);require(data==raw(expected,'ReleaseBase'),4);return expected

def initial(b):
    return dict(contractVersion=1,baseHash=sha(raw(b,'ReleaseBase')),releaseId=release_id(b),stateVersion=b['priorVersion'],prefix=b['priorPrefix'],status='unopened',
        openingReceiptId=None,decisionId=None,timing=None,openingClockFailure=False,acceptedHighWater=b['acceptedHighWater'],fallbackLocked=False,fallbackReason=None,
        members=copy.deepcopy(b['members']),pending=[],dispositions=[],completionReceiptId=None,retainedWorldHash=b['retainedWorldHash'],randomState=copy.deepcopy(b['randomState']),
        attackHistory=copy.deepcopy(b['attackHistory']),receipts=[])

def command(s,kind,choice=None):
    return dict(contractVersion=1,kind=kind,releaseId=s['releaseId'],expectedPriorVersion=None if kind in ('expire','unavailable') else s['stateVersion'],
        decisionId=None if kind=='open' else s['decisionId'],unit=copy.deepcopy(s['pending'][0]) if kind=='choose' and s['pending'] else None,choice=choice)

def trusted(cmd,actor='system',now=None,available=True):return dict(command=cmd,actor=actor,admittedAt=now,clockAvailable=available)

def transition(b,prior,inp):
    """Internal transition; untrusted persisted bases/states must use replay readers."""
    typed(prior,'ReleaseState');typed(inp,'ReleaseInput');cmd=inp['command'];kind=cmd['kind'];actor=inp['actor'];c=b['cycle']
    require(prior['contractVersion']==cmd['contractVersion']==1 and kind in ('open','choose','complete-release','complete','expire','unavailable','fallback-step'),3)
    require(prior['baseHash']==sha(raw(b,'ReleaseBase')) and cmd['releaseId']==prior['releaseId']==release_id(b),4)
    require(actor==c['actingSide'] if kind in ('choose','complete-release') else actor=='system',4)
    require((cmd['unit'] is not None)==(kind=='choose') and (cmd['choice'] is not None)==(kind=='choose'),3)
    require((cmd['expectedPriorVersion'] is None)==(kind in ('expire','unavailable')),3)
    require(cmd['decisionId'] is None if kind=='open' else True,3)
    ch=sha(raw(cmd,'ReleaseCommand'));receipt=next((x for x in prior['receipts'] if x['commandHash']==ch and x['actor']==actor),None)
    if receipt:return prior,None,receipt['receiptId']
    if kind in ('expire','unavailable') and (prior['status']!='open' or cmd['decisionId']!=prior['decisionId'] or prior['decisionId'] is None):return prior,None,None
    require(prior['status']!='completed' and len(prior['receipts'])<34 and prior['stateVersion']<2**63-1,7)
    if kind not in ('expire','unavailable'):require(cmd['expectedPriorVersion']==prior['stateVersion'],6)
    require(cmd['decisionId']==prior['decisionId'] if kind!='open' else True,6)
    s=copy.deepcopy(prior);effect=None;author=actor;now=inp['admittedAt']
    def complete(reason):
        nonlocal effect,author
        require(not s['pending'] or c['ordinal']>1,5)
        effect=dict(kind='release-completed',reason=reason,retainedUnits=copy.deepcopy(s['pending']),dispositionReceipts=[x['receiptId'] for x in s['dispositions']],
            timing=copy.deepcopy(s['timing']),fallbackLocked=s['fallbackLocked'])
        s['pending']=[];s['status']='completed';author='system'
    def dispose(choice,reason):
        nonlocal effect
        require(bool(s['pending']),5);u=s['pending'][0];m=next(x for x in s['members'] if x['unit']==u)
        require(choice in choices(m['status'],c['ordinal']),5)
        after='none' if choice.startswith('release-') else 'II';released=choice.startswith('release-')
        require(not released or c['ordinal']<MAX,7)
        ex=dict(scope=scope(c),ordinal=c['ordinal']+1,status='pending',completionReceiptId=None) if released else None
        effect=dict(kind='unit-disposition',unit=copy.deepcopy(u),choice=choice,beforeStatus=m['status'],afterStatus=after,reason=reason,cpaBasis=m['baseCpa'],
            voluntaryCeiling=ceiling(m['status'],m['baseCpa']) if released else None,nextMovement=ex,timing=copy.deepcopy(s['timing']),fallbackLocked=s['fallbackLocked'])
        m['status']=after;s['pending'].pop(0)
    def fallback(reason):
        nonlocal author
        s['fallbackLocked']=True;s['fallbackReason']=s['fallbackReason'] or reason;author='system'
        if c['ordinal']==1 and s['pending']:dispose('convert-to-II',s['fallbackReason'])
        else:complete(s['fallbackReason'])
    if kind=='open':
        require(s['status']=='unopened',6)
        s['pending']=[copy.deepcopy(m['unit']) for m in s['members'] if choices(m['status'],c['ordinal'])]
        require(not s['pending'] or c['ordinal']<MAX,7)
        if s['pending']:
            s['decisionId']=s['releaseId']+'.decision';lost=not inp['clockAvailable'] or now is None or s['acceptedHighWater'] is not None and now<s['acceptedHighWater']
            try:t=None if lost else steps.inputs.make_timing(CONTEXT.config,'reserve-release',now)
            except steps.inputs.Invalid:t=None
            s['timing']=t;s['openingClockFailure']=t is None
            if t is not None:s['acceptedHighWater']=now
        s['status']='open';effect=dict(kind='release-opened',decisionId=s['decisionId'],timing=copy.deepcopy(s['timing']),openingClockFailure=s['openingClockFailure'],pending=copy.deepcopy(s['pending']))
    elif kind=='complete':
        require(s['status']=='open' and not s['pending'],5);complete(s['fallbackReason'] if s['fallbackLocked'] else 'units-resolved' if s['members'] else 'empty-membership')
    elif kind=='fallback-step':
        require(s['status']=='open' and (s['fallbackLocked'] or s['openingClockFailure']),5)
        fallback(s['fallbackReason'] or 'opening-clock-unavailable')
    else:
        require(s['status']=='open' and not s['fallbackLocked'],5)
        if kind in ('choose','complete-release'):require(bool(s['pending']),5)
        if kind=='choose':
            require(cmd['unit']==s['pending'][0],4)
            member=next(m for m in s['members'] if m['unit']==cmd['unit']);require(cmd['choice'] in choices(member['status'],c['ordinal']),5)
        if kind=='complete-release':require(c['ordinal']>1,5)
        t=s['timing'];lost=s['openingClockFailure'] or not inp['clockAvailable'] or now is None or s['acceptedHighWater'] is not None and now<s['acceptedHighWater']
        late=t is not None and now is not None and now>=t['deadlineUnixMilliseconds']
        if kind=='expire' and not lost and not late:return prior,None,None
        if kind in ('choose','complete-release') and not lost:require(not late,5)
        if not lost:
            s['acceptedHighWater']=max(s['acceptedHighWater'] or 0,now)
            s['timing']['highWaterUnixMilliseconds']=s['acceptedHighWater']
        if kind in ('expire','unavailable') or lost:
            fallback('opening-clock-unavailable' if s['openingClockFailure'] else 'clock-unavailable' if lost else 'controller-unavailable' if kind=='unavailable' else 'deadline')
        elif kind=='complete-release':complete('owner-complete-release')
        else:dispose(cmd['choice'],'owner-choice')
    event=dict(contractVersion=1,eventType={'release-opened':'reserve-release-opened','unit-disposition':'reserve-unit-disposition-recorded','release-completed':'reserve-release-completed'}[effect['kind']],
        author=author,campaignId=c['campaignId'],rulesetHash=c['rulesetHash'],configurationHash=c['admittedPolicyBundleDigest'],cycleId=cycle_id(b),positionId=b['positionId'],baseHash=prior['baseHash'],
        releaseId=prior['releaseId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],input=canonical(inp,'ReleaseInput'),effect=canonical(effect,'ReleaseEffect'))
    rid='rr.'+digest('receipt',event);event['receiptId']=rid;data=raw(event,'ReleaseEvent')
    if effect['kind']=='release-opened':s['openingReceiptId']=rid
    elif effect['kind']=='release-completed':s['completionReceiptId']=rid
    else:
        m=next(x for x in s['members'] if x['unit']==effect['unit']);h=m['history'];choice=effect['choice']
        if choice=='convert-to-II':h['conversionReceiptId']=rid
        elif choice.startswith('release-'):
            h.update(releasedType=effect['beforeStatus'],releaseReceiptId=rid,releaseCycle=c['ordinal'],cpaBasis=effect['cpaBasis'],voluntaryCeiling=effect['voluntaryCeiling'],nextMovement=copy.deepcopy(effect['nextMovement']))
        s['dispositions'].append(dict(receiptId=rid,unit=copy.deepcopy(effect['unit']),choice=choice,beforeStatus=effect['beforeStatus'],afterStatus=effect['afterStatus']))
    s['stateVersion']=event['stateVersion'];s['prefix']=steps.seq.prefix_event(prior['prefix'],data)
    s['receipts'].append(dict(commandHash=ch,eventHash=sha(data),receiptId=rid,actor=actor,stateVersion=s['stateVersion']))
    raw(s,'ReleaseState');return s,data,rid

def read_event(data,b,prior,inp):
    parse(data,'ReleaseEvent');state,expected,_=transition(b,prior,inp);require(data==expected,6);return state

def replay(b,case,ins,events,length=None,side='axis',slot='first-acting-side'):
    require(type(ins) is list and type(events) is list and len(ins)==len(events)<=34,1)
    require(length is None or type(length) is int and 0<=length<=len(events),2)
    read_base(raw(b,'ReleaseBase'),case,side,slot);s=initial(b)
    for inp,event in zip(ins[:length],events[:length]):s=read_event(event,b,s,inp)
    return s

def read_state(data,b,case,ins,events,length=None,side='axis',slot='first-acting-side'):
    parse(data,'ReleaseState');expected=replay(b,case,ins,events,length,side,slot);require(data==raw(expected,'ReleaseState'),6);return expected

def project_world(before,b,s):
    require(sha(r.raw(before,'World'))==b['retainedWorldHash']==s['retainedWorldHash'],4)
    own=[e for e in before['elements'] if world.SIDES[e['elementId']]==b['cycle']['actingSide']]
    require({e['elementId'] for e in own}=={m['unit']['elementId'] for m in b['members']},4)
    after=copy.deepcopy(before)
    for old,new in zip(b['members'],s['members']):
        require(old['unit']==new['unit'] and old['baseCpa']==new['baseCpa'] and old['spentCp']==new['spentCp'],4)
        e=next(e for e in own if e['elementId']==old['unit']['elementId'])
        require(e['reserveStatus']==old['status'] and e['operationalState']['capabilityPointsExpended']==old['spentCp'],4)
        next(e for e in after['elements'] if e['elementId']==new['unit']['elementId'])['reserveStatus']=new['status']
    return after

def rejected(call,code=None):
    try:call()
    except Invalid as e:
        if code is not None:assert e.code==f'CMB-RRL-{code:03}',(e.code,code)
    else:raise AssertionError('invalid release contract accepted')

def trace(case,side='axis',slot='first-acting-side'):
    b=base_for(case,side,slot);s=initial(b);ins=[];events=[];opened=(b['acceptedHighWater'] or 1000)+1000
    def send(kind,choice=None,now=None,available=True):
        nonlocal s
        actor=side if kind in ('choose','complete-release') else 'system';inp=trusted(command(s,kind,choice),actor,now,available)
        prior=s;before=copy.deepcopy(prior);s,data,_=transition(b,prior,inp);assert prior==before and data is not None
        ins.append(inp);events.append(data)
    send('open',now=opened if case.get('openingClock',True) else None,available=case.get('openingClock',True))
    for index,action in enumerate(case['actions']):
        now=opened+100*(index+1)
        if action in ('expire','unavailable'):send(action,now=s['timing']['deadlineUnixMilliseconds'] if s['timing'] and action=='expire' else now)
        elif action=='regressed-choice':send('choose',choices(next(m for m in s['members'] if m['unit']==s['pending'][0])['status'],b['cycle']['ordinal'])[0],now=opened)
        elif action=='complete-release':send(action,now=now)
        else:send('choose',action,now=now)
    while s['status']!='completed':send('fallback-step' if s['fallbackLocked'] or s['openingClockFailure'] else 'complete')
    assert [m['status'] for m in s['members']]==case['expectedStatuses']
    assert [m['history']['releasedType'] for m in s['members']]==case['expectedReleased'] and s['fallbackLocked']==case['expectedFallback']
    assert s['completionReceiptId'] is not None and s['retainedWorldHash']==b['retainedWorldHash'] and s['randomState']==b['randomState'] and s['attackHistory']==b['attackHistory']
    for before,after in zip(b['members'],s['members']):
        assert before['unit']==after['unit'] and before['baseCpa']==after['baseCpa'] and before['spentCp']==after['spentCp']
        if before['status']=='none':assert before==after
        if after['history']['releasedType'] and not before['history']['releasedType']:
            h=after['history'];assert h['voluntaryCeiling']==(after['baseCpa'] if h['releasedType']=='I' else after['baseCpa']//2)
            assert h['nextMovement']==dict(scope=scope(b['cycle']),ordinal=b['cycle']['ordinal']+1,status='pending',completionReceiptId=None)
    if case.get('settledCase'):
        _,settled=settled_source(case['settledCase'],side);assert project_world(settled['world'],b,s)==settled['world']
    return b,s,ins,events

def checkpoint_checks(f):
    cuts=mutations=raws=0;goldens=[]
    for case in f['cases']:
        for side in ('axis','commonwealth'):
            for slot in ('first-acting-side',) if case.get('settledCase') else ('first-acting-side','second-acting-side'):
                b,final,ins,events=trace(case,side,slot);prior=initial(b)
                assert read_state(raw(prior,'ReleaseState'),b,case,ins,events,0,side,slot)==prior;cuts+=1
                frames=[dict(cut=0,hash=sha(raw(prior,'ReleaseState')),bytes=len(raw(prior,'ReleaseState')))]
                for i,(inp,data) in enumerate(zip(ins,events)):
                    before=copy.deepcopy(prior);after=read_event(data,b,prior,inp)
                    assert read_state(raw(after,'ReleaseState'),b,case,ins,events,i+1,side,slot)==after;cuts+=1
                    retry,new,rid=transition(b,after,inp);assert retry is after and new is None and rid==after['receipts'][-1]['receiptId']
                    for field in ('priorVersion','stateVersion','priorPrefix','configurationHash','baseHash','cycleId','receiptId'):
                        v=json.loads(data);v[field]=v[field]+1 if type(v[field]) is int else ('rr.' if field=='receiptId' else 'sha256:')+'0'*64
                        rejected(lambda:read_event(encode(v),b,prior,inp));mutations+=1
                    # Compare to the independently replay-derived cut above; avoid replaying identical
                    # suffixes for every malformed candidate. Full reader is exercised at every cut.
                    expected=raw(after,'ReleaseState')
                    def reject_state(candidate):parse(candidate,'ReleaseState');require(candidate==expected,6)
                    for field in ('prefix','retainedWorldHash','baseHash'):
                        v=copy.deepcopy(after);v[field]='sha256:'+'0'*64;rejected(lambda:reject_state(raw(v,'ReleaseState')));mutations+=1
                    for mutate in (lambda v:v['randomState'].__setitem__('nextByteCursor',v['randomState']['nextByteCursor']+1),
                        lambda v:v.__setitem__('fallbackLocked',not v['fallbackLocked']),lambda v:v['receipts'].clear()):
                        v=copy.deepcopy(after);mutate(v);rejected(lambda:reject_state(raw(v,'ReleaseState')));mutations+=1
                    if after['members']:
                        for mutate in (lambda v:v['members'].pop(0),lambda v:v['members'][0]['spentCp'].__setitem__('numerator',999),
                            lambda v:v['members'][0]['history'].__setitem__('designationReceiptId','forged.designation')):
                            v=copy.deepcopy(after);mutate(v);rejected(lambda:reject_state(raw(v,'ReleaseState')));mutations+=1
                    if after['timing']:
                        v=copy.deepcopy(after);v['timing']['deadlineUnixMilliseconds']+=1;rejected(lambda:reject_state(raw(v,'ReleaseState')));mutations+=1
                    for bad in (data+b'\n',b'\xef\xbb\xbf'+data,data[:-1],data.replace(b'"contractVersion":1',b'"contractVersion":1.0',1),
                        b'{"extra":0,'+data[1:],data.replace(b'"author":',b'"author":"system","author":',1)):
                        rejected(lambda:read_event(bad,b,prior,inp));raws+=1
                    assert prior==before;prior=after
                    frames.append(dict(cut=i+1,hash=sha(expected),bytes=len(expected)))
                for inp in ins:
                    retry,new,_=transition(b,final,inp);assert retry is final and new is None
                stale=trusted(command(final,'expire'),now=999999);same,new,rid=transition(b,final,stale)
                assert same is final and new is None and (rid is None or rid in {x['receiptId'] for x in final['receipts']})
                rejected(lambda:read_state(raw(final,'ReleaseState'),b,case,ins[:-1],events[:-1],side=side,slot=slot),6)
                rejected(lambda:replay(b,case,ins+ins[-1:],events+events[-1:],side=side,slot=slot))
                # A forged header cannot be authenticated by replacing its internal hashes.
                v=copy.deepcopy(b);v['combatCompletionReceiptId']='forged.combat-completion'
                rejected(lambda:read_base(raw(v,'ReleaseBase'),case,side,slot),4);mutations+=1
                goldens.append(dict(case=case['name'],side=side,slot=slot,baseHash=sha(raw(b,'ReleaseBase')),frames=frames,eventHashes=[sha(d) for d in events],
                    terminalEvent=events[-1].decode()))
                if not (side=='axis' and slot=='first-acting-side' and case['name'] in ('first-timeout-midway','later-complete-intent','settled-guard-empty')):
                    goldens[-1].pop('terminalEvent')
    return cuts,mutations,raws,goldens

def timing_checks(f):
    case=next(c for c in f['cases'] if c['name']=='first-timeout-midway');b=base_for(case);s=initial(b);checks=0
    s,_,_=transition(b,s,trusted(command(s,'open'),now=2000));deadline=s['timing']['deadlineUnixMilliseconds']
    # One accepted choice is durable before timeout; remaining fallback never changes it.
    inp=trusted(command(s,'choose','release-I'),'axis',2100);one,_,_=transition(b,s,inp)
    assert one['timing']['deadlineUnixMilliseconds']==deadline;checks+=1
    early=trusted(command(one,'expire'),now=deadline-1);assert transition(b,one,early)==(one,None,None);checks+=1
    late=trusted(command(one,'choose','release-I'),'axis',deadline)
    before=copy.deepcopy(one);rejected(lambda:transition(b,one,late),5);assert one==before;checks+=1
    fallback,ev,_=transition(b,one,trusted(command(one,'expire'),now=deadline));assert fallback['fallbackLocked'] and json.loads(ev)['author']=='system';checks+=1
    assert fallback['members'][0]==one['members'][0] and fallback['members'][1]['status']=='II';checks+=1
    rejected(lambda:transition(b,fallback,trusted(command(fallback,'choose','release-II'),'axis',deadline+1)));checks+=1
    done,_,_=transition(b,fallback,trusted(command(fallback,'fallback-step')));assert done['completionReceiptId'] and done['fallbackLocked'];checks+=1
    for now,available in ((2099,True),(None,False),(2200,False)):
        command_input=trusted(command(one,'choose','release-I'),'axis',now,available);after,event,_=transition(b,one,command_input)
        assert after['fallbackLocked'] and after['members'][0]==one['members'][0] and after['members'][1]['status']=='II'
        assert json.loads(event)['author']=='system';checks+=1
    unavailable,_,_=transition(b,one,trusted(command(one,'unavailable'),now=2200));assert unavailable['fallbackLocked'];checks+=1
    # Locked fallback with more than one mandatory unit drains in order and rejects owner interleaving.
    b3=base_for(case|dict(statuses=['I','I','I']));s3=initial(b3);s3,_,_=transition(b3,s3,trusted(command(s3,'open'),now=2000))
    s3,_,_=transition(b3,s3,trusted(command(s3,'unavailable'),now=2100))
    assert len(s3['pending'])==2
    bad=trusted(command(s3,'choose','release-I'),'axis',2200);rejected(lambda:transition(b3,s3,bad),5);checks+=1
    while s3['status']!='completed':s3,_,_=transition(b3,s3,trusted(command(s3,'fallback-step')))
    assert [m['status'] for m in s3['members']]==['II']*3;checks+=1
    # Deadline edge and unavailable opening never manufacture a renewed or clamped budget.
    budget=next(w['decisionBudgetMilliseconds'] for w in CONTEXT.config['windows'] if w['kind']=='reserve-release')
    for now,available in ((None,False),(999,True),(253402300799999-budget+1,True)):
        prior=initial(b);opened,_,_=transition(b,prior,trusted(command(prior,'open'),now=now,available=available))
        assert opened['timing'] is None and opened['openingClockFailure'] and not opened['fallbackLocked']
        opened,_,_=transition(b,opened,trusted(command(opened,'fallback-step')));assert opened['fallbackLocked'];checks+=1
    # Recovery cut after last owner disposition but before completion: a due timer completes,
    # without repeating disposition or discarding its original receipt/history.
    solo=base_for(f['cases'][0]);ss=initial(solo);ss,_,_=transition(solo,ss,trusted(command(ss,'open'),now=2000))
    ss,_,_=transition(solo,ss,trusted(command(ss,'choose','release-I'),'axis',2100));ssbefore=copy.deepcopy(ss)
    due=ss['timing']['deadlineUnixMilliseconds']
    assert transition(solo,ss,trusted(command(ss,'expire'),now=due-1))==(ss,None,None);checks+=1
    for kind in ('expire','unavailable'):
        done,event,_=transition(solo,ss,trusted(command(ss,kind),now=due))
        assert done['status']=='completed' and done['fallbackLocked'] and done['members']==ss['members'] and done['dispositions']==ss['dispositions']
        assert json.loads(event)['effect']['kind']=='release-completed' and ss==ssbefore;checks+=1
    # An obsolete timer cannot consume current work.
    timer=trusted(command(one,'expire'),now=deadline);timer['command']['decisionId']='other.decision'
    assert transition(b,one,timer)==(one,None,None);checks+=1
    return checks

def boundary_checks(f):
    case=f['cases'][0];b=base_for(case);count=0
    for mutate in (lambda v:v['members'][0].__setitem__('status','II'),lambda v:v['cycle'].__setitem__('ordinal',2),
        lambda v:v['members'][0]['history'].__setitem__('designationReceiptId',None),
        lambda v:v['members'][0]['unit'].__setitem__('originalSide','commonwealth'),lambda v:v['members'].append(copy.deepcopy(v['members'][0])),
        lambda v:v['members'][0]['spentCp'].__setitem__('denominator',2),lambda v:v['members'][0].__setitem__('baseCpa',0),
        lambda v:v.__setitem__('positionId','wrong.position')):
        v=copy.deepcopy(b);mutate(v);rejected(lambda:validate_base(v));count+=1
    later=base_for(next(c for c in f['cases'] if c['name']=='later-release-retain'))
    v=copy.deepcopy(later);v['members'][0]['history']['conversionReceiptId']=None;rejected(lambda:validate_base(v),4);count+=1
    # Explicit complete is optional-II only; never a way to skip first I or revisit converted II.
    s=initial(b);s,_,_=transition(b,s,trusted(command(s,'open'),now=2000))
    rejected(lambda:transition(b,s,trusted(command(s,'complete-release'),'axis',2100)),5);count+=1
    rejected(lambda:transition(b,s,trusted(command(s,'complete'))),5);count+=1
    converted,_,_=transition(b,s,trusted(command(s,'choose','convert-to-II'),'axis',2100))
    bad=trusted(command(converted,'choose','release-II'),'axis',2200);bad['command']['unit']=copy.deepcopy(b['members'][0]['unit'])
    rejected(lambda:transition(b,converted,bad),5);count+=1
    # Existing expiry/CPA/commitment provenance cannot be reset by opening/completion.
    old=base_for(next(c for c in f['cases'] if c.get('priorRelease')))
    for mutate in (lambda v:v['members'][0]['history'].__setitem__('voluntaryCeiling',99),
        lambda v:v['members'][0]['history']['nextMovement'].__setitem__('status','pending'),
        lambda v:v['members'][0]['history'].__setitem__('releaseCycle',2)):
        v=copy.deepcopy(old);mutate(v);rejected(lambda:validate_base(v),4);count+=1
    used=copy.deepcopy(old);member=used['members'][0];member['history']['offensiveCommitmentId']='probe.offensive-commitment'
    enemy=member['unit']|dict(originalSide='commonwealth',elementId='probe.enemy')
    used['attackHistory']=[dict(commitmentId='probe.offensive-commitment',cycleId=cycle_id(used),segmentId='probe.combat',attacker=member['unit'],defender=enemy,
        targetLocationId='probe.target',gameTurn=1,operationStage=1)]
    validate_base(used);us=initial(used);us,_,_=transition(used,us,trusted(command(us,'open'),now=2000));us,_,_=transition(used,us,trusted(command(us,'complete')))
    assert us['members'][0]['history']==member['history'] and us['attackHistory']==used['attackHistory'];count+=1
    bad=copy.deepcopy(used);bad['attackHistory']=[];rejected(lambda:validate_base(bad),4);count+=1
    # Overflow is an atomic fault before a release offer; never auto-retain or phase-finish.
    v=copy.deepcopy(later);v['cycle']['ordinal']=MAX;validate_base(v);smax=initial(v);before=copy.deepcopy(smax)
    rejected(lambda:transition(v,smax,trusted(command(smax,'open'),now=2000)),7);assert smax==before;count+=1
    smax=initial(b);smax['stateVersion']=2**63-1;before=copy.deepcopy(smax)
    rejected(lambda:transition(b,smax,trusted(command(smax,'open'),now=2000)),7);assert smax==before;count+=1
    # Exactly32 units closes in34 events without extending the deadline;33 refuses admission.
    many=case|dict(statuses=['I']*32);bm=base_for(many);sm=initial(bm);sm,_,_=transition(bm,sm,trusted(command(sm,'open'),now=2000))
    sm,_,_=transition(bm,sm,trusted(command(sm,'unavailable'),now=2100))
    while sm['status']!='completed':sm,_,_=transition(bm,sm,trusted(command(sm,'fallback-step')))
    assert len(sm['receipts'])==34 and len(sm['dispositions'])==32 and len(raw(sm,'ReleaseState'))<1048576;count+=1
    rejected(lambda:base_for(case|dict(statuses=['I']*33)),4);count+=1
    # Rational CP input stays reduced, and I/II ceiling checks use cumulative expenditure.
    rows=[('I',10,Fraction(3),Fraction(7),True),('I',10,Fraction(3),Fraction(8),False),('II',9,Fraction(3),Fraction(1),True),
        ('II',9,Fraction(3),Fraction(2),False),('II',9,Fraction(5),Fraction(1),False),('II',9,Fraction(7,2),Fraction(1,2),True)]
    for kind,cpa,spent,cost,expected in rows:assert (spent+cost<=ceiling(kind,cpa))==expected;count+=1
    return count

def main():
    f=json.loads(FIXTURE.read_text());assert 'goldens' in f and 'sourceHashes' in f and len(f['cases'])==13
    for name,digest_value in f['sourceHashes'].items():assert sha((ROOT/name).read_bytes())==digest_value
    cuts,mutations,raws,goldens=checkpoint_checks(f);timers=timing_checks(f);bounds=boundary_checks(f)
    assert goldens==f['goldens']
    print(f'PASS: {len(f["cases"])} literal cases/{len(goldens)} side-slot traces; {cuts} cuts, {mutations} mutations, {raws} raw rejects, {timers} timing and {bounds} boundary checks. Private Release arm; inherited lineage/runtime remain gated.')
if __name__=='__main__':main()
