#!/usr/bin/env python3
"""First-Combat Snapshot12 composition; synthetic trusted pre-Combat header, no runtime reader."""
import copy
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('result_contract',ROOT/'verify-combat-result-settlement-v1.py')
r=importlib.util.module_from_spec(spec);spec.loader.exec_module(r)
steps,rnd,env=r.steps,r.rnd,r.env
encode,sha=r.encode,r.sha
FIXTURE=ROOT/'fixtures/combat-snapshot-composition-v1.json'
INVENTORY=json.loads((ROOT/'combat-snapshot-composition-v1.schema.json').read_text())
SCHEMA=r.SCHEMA|{'CreationReceipt':env.SCHEMA['Receipt']}|{k:[tuple(f.split(':')) for f in v.split()] for k,v in INVENTORY['objects'].items()}

class Invalid(ValueError):
    def __init__(self,code):self.code=f'CMB-SNP-{code:03}';super().__init__(self.code)

def require(ok,code):
    if not ok:raise Invalid(code)

def typed(v,kind,depth=0):
    require(depth<=32,1)
    if kind.endswith('?'):
        if v is not None:typed(v,kind[:-1],depth)
    elif kind=='CombatArm':
        require(type(v) is dict and v.get('kind') in INVENTORY['armTags'],3)
        typed(v,INVENTORY['armTags'][v['kind']],depth)
    elif kind in SCHEMA:
        require(type(v) is dict and set(v)=={k for k,_ in SCHEMA[kind]},1)
        for k,t in SCHEMA[kind]:typed(v[k],t,depth+1)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v)<=(4096 if kind=='Cause[]' else 512),1)
        for x in v:typed(x,kind[:-2],depth+1)
    else:
        try:r.typed(v,kind,depth=depth)
        except r.Invalid as e:raise Invalid(int(e.code[-3:])) from e

def canonical(v,kind):
    if kind.endswith('?'):return None if v is None else canonical(v,kind[:-1])
    if kind=='CombatArm':return canonical(v,INVENTORY['armTags'][v['kind']])
    if kind in SCHEMA:return {k:canonical(v[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'):return [canonical(x,kind[:-2]) for x in v]
    return v

def raw(v,kind='FullSnapshot'):
    typed(v,kind);data=encode(canonical(v,kind));require(len(data)<=1048576,1);return data

def parse(data):
    require(type(data) is bytes and 0<len(data)<=1048576,1)
    def pairs(items):
        out={}
        for k,v in items:require(k not in out,1);out[k]=v
        return out
    try:v=json.loads(data.decode('utf8'),object_pairs_hook=pairs,parse_constant=lambda _:(_ for _ in ()).throw(ValueError()))
    except (ValueError,UnicodeError,RecursionError) as e:raise Invalid(1) from e
    require(raw(v)==data,8);return v

def header(b):
    """Fabricated probe metadata/receipts, never claimed as emitted historical events."""
    first=b['firstActingSide'];second='axis' if first=='commonwealth' else 'commonwealth'
    return dict(initiativeHolder='axis',operationStageOrders=[dict(contractVersion=1,gameTurn=1,operationStage=1,firstSide=first,secondSide=second)],
        operationStageWeather=[dict(contractVersion=1,gameTurn=1,operationStage=1,determiningSide='axis',season='fall',firstDie=1,secondDie=1,
            kind='normal',scope='none',locationDie=None,affectedAreas=[],fuelWaterReductionSubjectCount=0,restoredWellCount=0,damagedGroundedAircraftCount=0)],
        commandReceipts=[dict(commandHash=sha(f'synthetic-command-{i}'.encode()),eventHash=b['completedBreakdownReceipt'] if i==b['priorVersion'] else sha(f'synthetic-event-{i}'.encode()),
            receiptId=f'probe.receipt.{i}',actor='system',stateVersion=i) for i in range(2,b['priorVersion']+1)])

def check_header(h,b):
    typed(h,'Header');expected=header(b)
    require(h['initiativeHolder']==expected['initiativeHolder'] and h['operationStageOrders']==expected['operationStageOrders']
            and h['operationStageWeather']==expected['operationStageWeather'],4)
    ledger=h['commandReceipts'];require([x['stateVersion'] for x in ledger]==list(range(2,b['priorVersion']+1)),4)
    require(len({x['receiptId'] for x in ledger})==len(ledger) and len({x['commandHash'] for x in ledger})==len(ledger),4)
    require(ledger and ledger[-1]['eventHash']==b['completedBreakdownReceipt'],4)
    require(b['weather']['attackerKind']==b['weather']['defenderKind']=='normal',4)

def compose(h,lane,source,index):
    require(lane in ('selection','round','settlement'),3)
    try:
        if lane=='selection':
            b=steps.boundary(encode(source['boundary']));s=steps.replay(source,index);control=s
            current_world=b['world'];random=b['randomState'];step=s['stepIndex'];history=[];uses=[]
            arm=dict(kind='selection',boundary=b,control=s);ledger=s['receipts']
        else:
            ctx=source['context'];b=ctx['base']['boundary'];control=ctx['base']['steps']
            rnd.read_base(r.raw(ctx['base'],'Base'),ctx['predecessor'])
            pre_round=compose(h,'selection',ctx['predecessor'],len(ctx['predecessor']['inputs']))
            full_hash=sha(raw(pre_round))
            if lane=='round':
                s=rnd.replay(ctx['base'],source['inputs'],source['events'],index)
                current_world=s['world'];random=s['randomState'];step=s['stepIndex'];history=s['attackHistory'];uses=s['targetUses']
                arm=dict(kind='round',base=ctx['base'],preRoundSnapshotHash=full_hash,round=s)
                ledger=control['receipts']+s['receipts']
            else:
                s=r.replay(ctx,source['inputs'],source['events'],index);committed=ctx['committed']
                current_world=s['world'];random=s['randomState'];step=6 if s['closed'] else 5
                history=committed['attackHistory'];uses=committed['targetUses']
                arm=dict(kind='settlement',base=ctx['base'],preRoundSnapshotHash=full_hash,committed=committed,result=s)
                ledger=control['receipts']+committed['receipts']+s['receipts']
    except (steps.Invalid,rnd.Invalid,r.Invalid) as e:raise Invalid(6) from e
    check_header(h,b)
    c=env.Context();request=c.request();created=env.encode(env.canonical(env.make_created(request,c),'Created'))
    initial=env.make_snapshot(created,request,c)
    posid=steps.edge(b)['combatPositionIds'][step] if step<6 else steps.edge(b)['releasePositionId']
    position=copy.deepcopy(next(p for p in steps.seq.expected_catalog()['positions'] if p['positionId']==posid));position['activeSide']=b['cycle']['actingSide']
    value=initial|dict(stateVersion=s['stateVersion'],world=copy.deepcopy(current_world),initiativeHolder=h['initiativeHolder'],
        operationStageOrders=copy.deepcopy(h['operationStageOrders']),operationStageWeather=copy.deepcopy(h['operationStageWeather']),
        randomState=copy.deepcopy(random),currentPosition=dict(kind='sequence',sequencePosition=position),chroniclePrefix=s['prefix'],
        cycleState=dict(kind='combat-boundary',authority=copy.deepcopy(b['cycle']),authorityId=sha(steps.seq.identity(b['cycle'],'Authority',b['firstActingSide'])),
            firstActingSide=b['firstActingSide'],attackHistory=copy.deepcopy(history),targetUses=copy.deepcopy(uses)),
        combatState=copy.deepcopy(arm),commandReceipts=copy.deepcopy(h['commandReceipts']+ledger))
    receipts=value['commandReceipts'];require([x['stateVersion'] for x in receipts]==list(range(2,value['stateVersion']+1)),6)
    require(len({x['receiptId'] for x in receipts})==len(receipts) and len({x['commandHash'] for x in receipts})==len(receipts),6)
    raw(value);return value

def read_snapshot(data,h,lane,source,index):
    parse(data);expected=compose(h,lane,source,index);require(data==raw(expected),6);return expected

def sources():
    sf=json.loads(steps.FIXTURE.read_text())
    for case in sf['cases']:
        source=copy.deepcopy(case);source['boundary']=json.loads(sf['boundaries'][case['boundaryIndex']]['canonicalUtf8'])
        yield case['name'],'selection',source,source['boundary'],len(source['inputs'])
    baseline=r.context(json.loads(r.FIXTURE.read_text())['cases'][0])
    rf=json.loads(rnd.FIXTURE.read_text())
    for case in rf['cases']:
        _,inputs,events=rnd.trace_case(baseline['base'],case)
        yield case['name'],'round',dict(context=baseline,inputs=inputs,events=events),baseline['base']['boundary'],len(inputs)
    for case in json.loads(r.FIXTURE.read_text())['cases']:
        ctx,_,inputs,events=r.trace_case(case)
        yield case['name'],'settlement',dict(context=ctx,inputs=inputs,events=events),ctx['base']['boundary'],len(inputs)

def rejected(call):
    try:call()
    except Invalid:pass
    else:raise AssertionError('invalid composed snapshot accepted')

def compose_tests():
    count=mutations=raw_count=0;frames=[];case_counts=dict(selection=0,round=0,settlement=0)
    for name,lane,source,b,length in sources():
        h=header(b);case_counts[lane]+=1
        for index in range(length+1):
            value=compose(h,lane,source,index);data=raw(value)
            assert read_snapshot(data,h,lane,source,index)==value
            assert value['creationReceipt']['creationBinding']==b['creationBinding']
            assert value['commandReceipts'][:len(h['commandReceipts'])]==h['commandReceipts']
            # Compare malformed candidates to a separately replay-derived expected value. Full-reader
            # replay above is exercised at every cut; avoid re-running identical replay for each mutation.
            def reject_candidate(candidate):
                actual=parse(candidate);require(raw(actual)==data,6)
            for field in ('stateVersion','chroniclePrefix','rulesetHash'):
                v=copy.deepcopy(value);v[field]=v[field]+1 if field=='stateVersion' else ('sha256:' if field=='chroniclePrefix' else '')+'0'*64
                rejected(lambda:reject_candidate(raw(v)));mutations+=1
            for mutate in (
                lambda v:v['world']['elements'][0]['ammunition'].__setitem__('points',9),
                lambda v:v['randomState'].__setitem__('nextByteCursor',v['randomState']['nextByteCursor']+1),
                lambda v:v['commandReceipts'].pop(0),
                lambda v:v['creationReceipt'].__setitem__('creationBinding','creation.'+'0'*64),
                lambda v:v['operationStageOrders'][0].__setitem__('firstSide','invalid'),
                lambda v:v['cycleState'].__setitem__('authorityId','sha256:'+'0'*64)):
                v=copy.deepcopy(value);mutate(v);rejected(lambda:reject_candidate(encode(v)));mutations+=1
            for bad in (data+b'\n',b'\xef\xbb\xbf'+data,data[:-1],data.replace(b'"contractVersion":12',b'"contractVersion":12.0',1)):
                rejected(lambda:parse(bad));raw_count+=1
            frame=dict(case=name,lane=lane,cut=index,bytes=len(data),hash=sha(data))
            if index==length and name in ('ordinary','attacker-capture-guard-cp-limit'):frame['canonicalUtf8']=data.decode()
            frames.append(frame);count+=1
        changed=copy.deepcopy(h);changed['commandReceipts'][0]['commandHash']=sha(b'changed trusted inherited input')
        alternate=compose(changed,lane,source,length)
        assert raw(alternate)!=raw(value)
        rejected(lambda:read_snapshot(raw(alternate),h,lane,source,length));mutations+=1
        if lane!='selection':assert alternate['combatState']['preRoundSnapshotHash']!=value['combatState']['preRoundSnapshotHash']
    assert case_counts==dict(selection=5,round=4,settlement=8),case_counts
    return count,mutations,raw_count,frames

def main():
    f=json.loads(FIXTURE.read_text())
    assert 'sourceHashes' in f and 'frames' in f
    for name,digest in f.get('sourceHashes',{}).items():assert sha((ROOT/name).read_bytes())==digest
    count,mutations,raw_count,frames=compose_tests()
    if 'frames' in f:assert frames==f['frames']
    print(f'PASS: 17 traces/{count} full Snapshot12 cuts, {mutations} mutations, {raw_count} raw rejections; inherited ledger and full pre-round binding preserved. Synthetic pre-Combat lineage only.')

if __name__=='__main__':main()
