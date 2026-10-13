#!/usr/bin/env python3
"""Actual repeated Movement completion: importable genuine semantic RED boundary."""
import argparse,copy,hashlib,importlib.util,json,sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent
REPOSITORY=ROOT.parent.parent
INVENTORY=json.loads((ROOT/'combat-actual-repeated-movement-completion-v1.schema.json').read_bytes())
SCHEMA={k:[tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['objects'].items()}
LIMITS=INVENTORY['limits'];DEPENDENCIES=dict(INVENTORY['runtimeSourceHashes'])
def encode(v):return json.dumps(v,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(data):return 'sha256:'+hashlib.sha256(data).hexdigest()
PACKAGED_MANIFEST=encode(dict(sorted(DEPENDENCIES.items())))
class Invalid(ValueError):
    def __init__(self,code,path=''):self.code=f'CMB-RMC-{code:03}';self.path=path;super().__init__(self.code+' '+(path or '/'))
def require(ok,code,path=''):
    if not ok:raise Invalid(code,path)
def consume(path):
    require(path in DEPENDENCIES,9,path)
    try:data=(REPOSITORY/path).read_bytes()
    except OSError as error:raise Invalid(9,path) from error
    require(type(data) is bytes and hashlib.sha256(data).hexdigest()==DEPENDENCIES[path],9,path);return data
OWN_DELIVERY={p:sha(p.read_bytes()) for p in (ROOT/'combat-actual-repeated-movement-completion-v1.schema.json',Path(__file__).resolve())}
def verify_dependencies():
    require(encode(dict(sorted(DEPENDENCIES.items())))==PACKAGED_MANIFEST,9,'/packagedManifest')
    for path,value in DEPENDENCIES.items():
        require(type(path) is str and path.isascii() and not path.startswith('/') and '\\' not in path and all(p not in ('','.','..') for p in path.split('/')) and type(value) is str and len(value)==64 and all(c in '0123456789abcdef' for c in value),9,'/manifest');consume(path)
    for path,expected in OWN_DELIVERY.items():
        try:data=path.read_bytes()
        except OSError as error:raise Invalid(9,'/ownDelivery') from error
        require(type(data) is bytes and sha(data)==expected,9,'/ownDelivery')
verify_dependencies()
spec=importlib.util.spec_from_file_location('actual_movement_predecessor',ROOT/'verify-combat-actual-repeated-movement-v1.py');arm=importlib.util.module_from_spec(spec);spec.loader.exec_module(arm)
policy,mov,world,rel,steps=arm.policy,arm.mov,arm.world,arm.rel,arm.steps
verify_dependencies()
def inherited(call):
    try:return call()
    except (arm.Invalid,arm.pac.Invalid,arm.pac.paid.Invalid,mov.Invalid,policy.Invalid) as error:raise Invalid(int(error.code[-3:]),getattr(error,'path','')) from error
def digest(domain,value):return hashlib.sha256(domain.encode('ascii')+b'\0'+encode(value)).hexdigest()
def identity(kind,value):return digest(INVENTORY['domains'][kind],value)
def byte_limit(kind):return {'Source':LIMITS['sourceBytes'],'Proof':LIMITS['proofBytes'],'Control':LIMITS['controlBytes'],'Event':LIMITS['eventBytes'],'Input':LIMITS['inputBytes'],'Command':LIMITS['inputBytes']}.get(kind,LIMITS['proofBytes'])
def typed(v,kind,path='',depth=0,parent='',field=''):
    require(depth<=LIMITS['depth'],1,path)
    if kind.endswith('?'):
        if v is not None:typed(v,kind[:-1],path,depth,parent,field)
    elif kind in ('Effect','Flow'):
        tags=INVENTORY['effectTags' if kind=='Effect' else 'flowTags'];require(type(v) is dict and type(v.get('kind')) is str,1,path);require(v['kind'] in tags,3,path);typed(v,tags[v['kind']],path,depth)
    elif kind in SCHEMA:
        fields=SCHEMA[kind];require(type(v) is dict and set(v)=={k for k,t in fields},1,path)
        for k,t in fields:typed(v[k],t,path+'/'+k,depth+1,kind,k)
        if kind in INVENTORY['versionRules']:require(v['contractVersion']==INVENTORY['versionRules'][kind],3,path)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v)<=INVENTORY['arrayBounds'].get(parent+'.'+field,LIMITS['arrayItems']),1,path)
        for i,x in enumerate(v):typed(x,kind[:-2],path+'/'+str(i),depth+1)
    elif kind in ('carrier','eventCarrier'):
        require(type(v) is str and v.isascii() and 0<len(v)<=(arm.LIMITS['sourceBytes'] if kind=='carrier' else LIMITS['eventBytes']),1,path)
    else:inherited(lambda:arm.typed(v,INVENTORY['borrowedTypes'].get(kind,kind),path,0))
def canonical(v,kind):
    if kind.endswith('?'):return None if v is None else canonical(v,kind[:-1])
    if kind in ('Effect','Flow'):return canonical(v,INVENTORY['effectTags' if kind=='Effect' else 'flowTags'][v['kind']])
    if kind in SCHEMA:return {k:canonical(v[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'):return [canonical(x,kind[:-2]) for x in v]
    if kind in ('carrier','eventCarrier'):return v
    return arm.canonical(v,INVENTORY['borrowedTypes'].get(kind,kind))
def bounds(v,depth=0):
    require(depth<=LIMITS['depth'],1)
    if type(v) is list:
        require(len(v)<=LIMITS['arrayItems'],1)
        for x in v:bounds(x,depth+1)
    elif type(v) is dict:
        require(len(v)<=LIMITS['objectFields'],1)
        for x in v.values():bounds(x,depth+1)
def raw(v,kind):bounds(v);typed(v,kind);data=encode(canonical(v,kind));require(len(data)<=byte_limit(kind),1);return data
def preparse(data):
    stack=[];quoted=False;escaped=False
    for n in data:
        if quoted:
            if escaped:escaped=False
            elif n==92:escaped=True
            elif n==34:quoted=False
            continue
        if n in (9,10,13,32):continue
        if n==34:
            if stack:stack[-1][2]=True
            quoted=True
        elif n in (123,91):
            if stack:stack[-1][2]=True
            require(len(stack)<LIMITS['depth']+1,1);stack.append([n,0,False])
        elif n in (125,93):
            require(bool(stack) and (stack[-1][0],n) in ((123,125),(91,93)),1)
            tag,commas,occupied=stack.pop();require(commas+int(occupied)<=(LIMITS['arrayItems'] if tag==91 else LIMITS['objectFields']),1)
        elif n==44 and stack:
            stack[-1][1]+=1;require(stack[-1][1]<(LIMITS['arrayItems'] if stack[-1][0]==91 else LIMITS['objectFields']),1)
        elif stack:stack[-1][2]=True
    require(not quoted and not stack,1)
def parse(data,kind):
    require(type(data) is bytes and 0<len(data)<=byte_limit(kind) and data.isascii(),1);preparse(data)
    def pairs(items):
        out={}
        for k,v in items:require(k not in out,1);out[k]=v
        return out
    try:v=json.loads(data.decode('ascii'),object_pairs_hook=pairs,parse_constant=lambda _:(_ for _ in ()).throw(ValueError()))
    except (ValueError,RecursionError,UnicodeError) as error:
        if isinstance(error,Invalid):raise
        raise Invalid(1) from error
    require(raw(v,kind)==data,8);return v


def admission(data,selection,rounds,paid,continuation,movement,lifecycle):
    verify_dependencies();source=parse(data,'Source');parent,ctx,state,owned=inherited(lambda:arm.replay(source['movementSourceCanonicalUtf8'].encode('ascii'),selection,rounds,paid,continuation,movement))
    require(type(lifecycle) is list and len(lifecycle)<=3,1,'/lifecycleLedger');inputs=[parse(raw(x,'Input'),'Input') for x in lifecycle];require(len(encode(inputs))<=LIMITS['lifecycleLedgerBytes'],1,'/lifecycleLedger');require(len(inputs)==len(source['lifecycleEventCanonicalUtf8']),4,'/lifecycleLedger')
    return source,ctx,state,inputs

def apply(data,selection,rounds,paid,continuation,movement,lifecycle,new_input):
    admission(data,selection,rounds,paid,continuation,movement,lifecycle);parse(raw(new_input,'Input'),'Input');raise Invalid(7,'/missing-completion-consumer')
def replay(data,*ledgers):admission(data,*ledgers);raise Invalid(7,'/missing-completion-consumer')
def read_control(claim,data,*ledgers):admission(data,*ledgers);raise Invalid(7,'/missing-completion-consumer')
def test_bytes(path):
    require(path in INVENTORY['testArtifactHashes'],9,'/testArtifact');data=(REPOSITORY/path).read_bytes();require(hashlib.sha256(data).hexdigest()==INVENTORY['testArtifactHashes'][path],9,path);return data
def roots():
    rows=json.loads(test_bytes('docs/specs/fixtures/combat-actual-repeated-movement-v1.json'))['traces'];prior=arm.roots()
    return [(r,*next((p,u) for p,u in prior if (p['owner'],p['firstSeal'])==(r['owner'],r['firstSeal']))) for r in rows]
def root_cut(row,prior,upstream,count):
    parent=json.loads(row['sourceCanonicalUtf8']);parent['movementEventCanonicalUtf8']=parent['movementEventCanonicalUtf8'][:count];data=arm.raw(parent,'Source');ledgers=arm.arguments(prior,upstream)+[copy.deepcopy(row['movementLedger'][:count])];return data,ledgers

def rejected(call,code):
    try:call()
    except Invalid as e:assert e.code==f'CMB-RMC-{code:03}',(e.code,e.path,code);return
    raise AssertionError('invalid input accepted')
def semantic_tests():
    failures=[]
    for row,prior,upstream in roots():
        for count in range(5):
            parent,args=root_cut(row,prior,upstream,count);p,ctx,state,owned=inherited(lambda:arm.replay(parent,*args));assert arm.raw(state,'Control').decode('ascii')==row['controlCanonicalUtf8'][count]
            assert state['stateVersion']==37+count and state['activeCycle']['ordinal']==2 and state['movementEnd']['ordinal']==1 and state['movementEnd']['completionReceiptId'].startswith('iml.')
            assert state['members'][0]['spentCp']==world.cp([5,9,11,13,15][count]) and state['randomState']['seed']==1 and state['randomState']['nextByteCursor']==10 and len(state['tracks'])==int(count>0) and len(state['receipts'])==len(state['nextCycleProgress'])==count
            h=arm.identity('source',arm.canonical(p,'Source'));proof=dict(contractVersion=1,sourceId='armove.'+h,sourceHash='sha256:'+h,base=ctx['base'],control=state);base=dict(contractVersion=1,dependencyManifestHash='sha256:'+identity('dependency-manifest',dict(sorted(DEPENDENCIES.items()))),movementProof=proof);basehash='sha256:'+identity('base',canonical(base,'Base'));cid=policy.cycle_id(state['activeCycle'],ctx['firstActingSide']);kind='stop-element-movement' if count else 'complete-movement-segment';flow=dict(kind='moving',track=copy.deepcopy(state['tracks'][0]),lastMoveReceiptId=state['receipts'][-1]['receiptId']) if count else dict(kind='idle')
            cap='sha256:'+identity('capability',dict(baseHash=basehash,cycleId=cid,kind=kind,stateVersion=state['stateVersion'],positionId=state['positionId'],flow=flow,interruptContext=None))
            inp=dict(command=dict(contractVersion=1,kind=kind,baseHash=basehash,cycleId=cid,expectedPositionId=state['positionId'],expectedPriorVersion=state['stateVersion'],capabilityId=cap),actor=row['owner']);source=raw(dict(contractVersion=1,movementSourceCanonicalUtf8=parent.decode('ascii'),lifecycleEventCanonicalUtf8=[]),'Source')
            rejected(lambda:apply(source,*args,None,inp),1);rejected(lambda:apply(source,*args,[None],inp),1);rejected(lambda:apply(source,*args,[inp],inp),4)
            broken=json.loads(source);broken['movementSourceCanonicalUtf8']='{}';rejected(lambda:read_control(b'{}',raw(broken,'Source'),*args,None),1)
            try:
                after,ledger,rid=apply(source,*args,[],inp);_,_,current,_=replay(after,*args,ledger);ev=json.loads(json.loads(after)['lifecycleEventCanonicalUtf8'][0]);assert ev['stateVersion']==38+count and rid==ev['receiptId'] and len(ledger)==1
                if count:assert ev['effect']['kind']=='movement-stopped' and ev['effect']['interruptContext']['cycle']['ordinal']==2 and current['flow']['kind']=='phasing-stop' and current['interruptContext']['cycle']['ordinal']==2
                else:assert ev['effect']['kind']=='movement-completed' and ev['effect']['successorPositionId']==policy.edge(state['activeCycle'])['breakdownPositionId'] and current['movementEnd']['ordinal']==2 and current['movementEnd']['completionReceiptId']==rid and current['closed']
                print('PASS actual completion boundary',row['owner'],row['firstSeal'],count)
            except Invalid as error:failures.append((row['owner'],row['firstSeal'],count));print('FAIL actual stop/zero completion publication required; actual',error.code,row['owner'],row['firstSeal'],count)
    assert not failures,str(len(failures))+' genuine completion publications failed'
if __name__=='__main__':semantic_tests()
