#!/usr/bin/env python3
"""Private authentic ordinal2 Breakdown completion reference."""
import argparse,copy,hashlib,importlib.util,json,sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent
REPOSITORY=ROOT.parent.parent
INVENTORY=json.loads((ROOT/'combat-actual-repeated-breakdown-completion-v1.schema.json').read_bytes())
SCHEMA={k:[tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['objects'].items()}
LIMITS=INVENTORY['limits'];DEPENDENCIES=dict(INVENTORY['runtimeSourceHashes'])
def encode(v):return json.dumps(v,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(data):return 'sha256:'+hashlib.sha256(data).hexdigest()
PACKAGED_MANIFEST=encode(dict(sorted(DEPENDENCIES.items())))
class Invalid(ValueError):
    def __init__(self,code,path=''):self.code=f'CMB-ABC-{code:03}';self.path=path;super().__init__(self.code+' '+(path or '/'))
def require(ok,code,path=''):
    if not ok:raise Invalid(code,path)
def consume(path):
    require(path in DEPENDENCIES,9,path)
    try:data=(REPOSITORY/path).read_bytes()
    except OSError as error:raise Invalid(9,path) from error
    require(type(data) is bytes and hashlib.sha256(data).hexdigest()==DEPENDENCIES[path],9,path);return data
OWN_DELIVERY={p:sha(p.read_bytes()) for p in (ROOT/'combat-actual-repeated-breakdown-completion-v1.schema.json',Path(__file__).resolve())}
def verify_dependencies():
    require(encode(dict(sorted(DEPENDENCIES.items())))==PACKAGED_MANIFEST,9,'/packagedManifest')
    for path,value in DEPENDENCIES.items():
        require(type(path) is str and path.isascii() and not path.startswith('/') and '\\' not in path and all(p not in ('','.','..') for p in path.split('/')) and type(value) is str and len(value)==64 and all(c in '0123456789abcdef' for c in value),9,'/manifest');consume(path)
    for path,expected in OWN_DELIVERY.items():
        try:data=path.read_bytes()
        except OSError as error:raise Invalid(9,'/ownDelivery') from error
        require(type(data) is bytes and sha(data)==expected,9,'/ownDelivery')
verify_dependencies()
spec=importlib.util.spec_from_file_location('actual_rmc_predecessor',ROOT/'verify-combat-actual-repeated-movement-completion-v1.py');rmc=importlib.util.module_from_spec(spec);spec.loader.exec_module(rmc)
policy,world,steps=rmc.policy,rmc.world,rmc.steps
verify_dependencies()
def inherited(call):
    try:return call()
    except rmc.Invalid as error:raise Invalid(int(error.code[-3:]),getattr(error,'path','')) from error
def digest(domain,value):return hashlib.sha256(domain.encode('ascii')+b'\0'+encode(value)).hexdigest()
def identity(kind,value):return digest(INVENTORY['domains'][kind],value)
def byte_limit(kind):return {'Source':LIMITS['sourceBytes'],'Proof':LIMITS['proofBytes'],'Control':LIMITS['controlBytes'],'Event':LIMITS['eventBytes'],'Input':LIMITS['inputBytes'],'Command':LIMITS['inputBytes']}.get(kind,LIMITS['proofBytes'])
def typed(v,kind,path='',depth=0,parent='',field=''):
    require(depth<=LIMITS['depth'],1,path)
    if kind.endswith('?'):
        if v is not None:typed(v,kind[:-1],path,depth,parent,field)
    elif kind in SCHEMA:
        fields=SCHEMA[kind];require(type(v) is dict and set(v)=={k for k,t in fields},1,path)
        for k,t in fields:typed(v[k],t,path+'/'+k,depth+1,kind,k)
        if kind in INVENTORY['versionRules']:require(v['contractVersion']==INVENTORY['versionRules'][kind],3,path)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v)<=INVENTORY['arrayBounds'].get(parent+'.'+field,LIMITS['arrayItems']),1,path)
        for i,x in enumerate(v):typed(x,kind[:-2],path+'/'+str(i),depth+1)
    elif kind in ('carrier','eventCarrier'):
        require(type(v) is str and v.isascii() and 0<len(v)<=(rmc.LIMITS['sourceBytes'] if kind=='carrier' else LIMITS['eventBytes']),1,path)
    else:inherited(lambda:rmc.typed(v,INVENTORY['borrowedTypes'].get(kind,kind),path,0))
def canonical(v,kind):
    if kind.endswith('?'):return None if v is None else canonical(v,kind[:-1])
    if kind in SCHEMA:return {k:canonical(v[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'):return [canonical(x,kind[:-2]) for x in v]
    if kind in ('carrier','eventCarrier'):return v
    return rmc.canonical(v,INVENTORY['borrowedTypes'].get(kind,kind))
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

KIND='complete-breakdown-segment'
ACTION=sha(encode(dict(contractVersion=1,kind=KIND,operationStage=1)))
def derive(parent,priorctx,state,owned):
    c=state['activeCycle'];edge=policy.edge(c);end=state['movementEnd']
    require(c['gameTurn']==1 and c['operationStage']==1 and c['playerPhaseSlot']=='first-acting-side' and c['ordinal']==2,3,'/profile')
    require(state['closed'] and state['flow']==dict(kind='idle') and state['interruptContext'] is None and state['positionId']==edge['breakdownPositionId'],3,'/terminalRMC')
    require(end is not None and end['ordinal']==2 and end['scope']==state['priorMovementEnd']['scope'] and end['completionReceiptId']==state['receipts'][-1]['receiptId'],4,'/currentMovementProof')
    require(len(owned)==(3 if state['tracks'] else 1),4,'/completedLifecycle')
    require(all(next(e for e in world.PACK['elements'] if e['elementId']==m['unit']['elementId'])['breakdownVehicleCohort'] is None for m in state['members']),3,'/cohort')
    catalog=steps.seq.expected_catalog()['positions'];boundary=next(p for p in catalog if p['positionId']==edge['breakdownPositionId']);terminal=next(p for p in catalog if p['positionId']==edge['combatPositionIds'][0]);require(catalog.index(terminal)==catalog.index(boundary)+1,4,'/catalogAdjacency')
    h=rmc.identity('source',rmc.canonical(parent,'Source'));p=dict(contractVersion=1,sourceId='armcomp.'+h,sourceHash='sha256:'+h,base=copy.deepcopy(priorctx['base']),control=copy.deepcopy(state));inherited(lambda:rmc.raw(p,'Proof'))
    base=dict(contractVersion=1,dependencyManifestHash='sha256:'+identity('dependency-manifest',dict(sorted(DEPENDENCIES.items()))),repeatedMovementCompletionProof=p);raw(base,'Base');require(state['stateVersion']<2**63-1,7,'/wholeTraceCapacity')
    return dict(base=base,baseHash='sha256:'+identity('base',canonical(base,'Base')),prior=copy.deepcopy(state),cycle=copy.deepcopy(c),cycleId=priorctx['cycleId'],firstActingSide=priorctx['firstActingSide'],boundary=boundary,terminal=terminal)
def initial(ctx):
    s=copy.deepcopy(ctx['prior']);s['baseHash']=ctx['baseHash'];s['repeatedMovementCompletionSourceHash']=ctx['base']['repeatedMovementCompletionProof']['sourceHash'];s['priorLifecycleReceipts']=s.pop('receipts');s['receipts']=[];s['breakdownCompletionReceiptId']=None;s['closed']=False;return canonical(s,'Control')
def admission(data,selection,rounds,paid,continuation,movement,lifecycle,breakdown):
    verify_dependencies();source=parse(data,'Source');parent,priorctx,state,owned=inherited(lambda:rmc.replay(source['repeatedMovementCompletionSourceCanonicalUtf8'].encode('ascii'),selection,rounds,paid,continuation,movement,lifecycle));ctx=derive(parent,priorctx,state,owned)
    require(type(breakdown) is list and len(breakdown)<=1,1,'/breakdownLedger');inputs=[parse(raw(x,'Input'),'Input') for x in breakdown];require(len(encode(inputs))<=LIMITS['breakdownLedgerBytes'],1,'/breakdownLedger');require(len(inputs)==len(source['breakdownEventCanonicalUtf8']),4,'/breakdownLedger');return source,ctx,inputs
def capability(ctx,s):return 'sha256:'+identity('capability',dict(baseHash=ctx['baseHash'],cycleId=ctx['cycleId'],kind=KIND,stateVersion=s['stateVersion'],positionId=s['positionId'],movementCompletionReceiptId=s['movementEnd']['completionReceiptId']))
def command(ctx,s):return dict(command=dict(contractVersion=1,kind=KIND,operationStage=1,actionId=ACTION,baseHash=ctx['baseHash'],cycleId=ctx['cycleId'],expectedPositionId=ctx['boundary']['positionId'],expectedPriorVersion=s['stateVersion'],capabilityId=capability(ctx,s)),actor='system')
def transition(ctx,prior,inp):
    raw(prior,'Control');raw(inp,'Input')
    require(False,7,'/unsupportedBreakdownPublication')
def replay(data,selection,rounds,paid,continuation,movement,lifecycle,breakdown):
    source,ctx,owned=admission(data,selection,rounds,paid,continuation,movement,lifecycle,breakdown);s=initial(ctx)
    for n,(text,inp) in enumerate(zip(source['breakdownEventCanonicalUtf8'],owned)):
        event=text.encode('ascii');parse(event,'Event');s,expected,_=transition(ctx,s,inp);require(expected is not None and event==expected,6,'/event/'+str(n))
    verify_dependencies();return source,ctx,s,owned
def apply(data,selection,rounds,paid,continuation,movement,lifecycle,breakdown,new_input):
    source,ctx,s,owned=replay(data,selection,rounds,paid,continuation,movement,lifecycle,breakdown);after,event,rid=transition(ctx,s,new_input)
    if event is None:verify_dependencies();return raw(source,'Source'),copy.deepcopy(owned),rid
    source['breakdownEventCanonicalUtf8'].append(event.decode('ascii'));ledger=owned+[parse(raw(new_input,'Input'),'Input')];require(len(encode(ledger))<=LIMITS['breakdownLedgerBytes'],1);result=raw(source,'Source');verify_dependencies();return result,ledger,rid
def read_source(data,*ledgers):
    result=copy.deepcopy(replay(data,*ledgers)[0]);require(INVENTORY['closureFrozen'],7,'/provisionalClosure');return result
def proof(data,*ledgers):
    source,ctx,s,_=replay(data,*ledgers);require(INVENTORY['closureFrozen'],7,'/provisionalClosure');h=identity('source',canonical(source,'Source'));out=dict(contractVersion=1,sourceId='arbcomp.'+h,sourceHash='sha256:'+h,base=ctx['base'],control=s);return parse(raw(out,'Proof'),'Proof')
def read_proof(claim,data,*ledgers):
    expected=proof(data,*ledgers);value=parse(claim,'Proof');require(claim==raw(expected,'Proof'),6,'/proof');return value
def read_control(claim,data,*ledgers):
    state=replay(data,*ledgers)[2];require(INVENTORY['closureFrozen'],7,'/provisionalClosure');value=parse(claim,'Control');require(claim==raw(state,'Control'),6,'/control');return value

def test_bytes(path):
    require(path in INVENTORY['testArtifactHashes'],9,'/testArtifact');data=(REPOSITORY/path).read_bytes();require(hashlib.sha256(data).hexdigest()==INVENTORY['testArtifactHashes'][path],9,path);return data
def roots():
    fixture=json.loads(test_bytes('docs/specs/fixtures/combat-actual-repeated-movement-completion-v1.json'))['traces'];out=[]
    for row,p,u in rmc.roots():
        for n in range(5):
            saved=next(t for t in fixture if (t['owner'],t['firstSeal'],t['moves'])==(row['owner'],row['firstSeal'],n));_,args=rmc.root_cut(row,p,u,n);out.append((saved,args+[copy.deepcopy(saved['lifecycleLedger'])]))
    return out
def source_for(parent):return raw(dict(contractVersion=1,repeatedMovementCompletionSourceCanonicalUtf8=parent.decode('ascii'),breakdownEventCanonicalUtf8=[]),'Source')
def semantic_tests():
    failures=[]
    for row,args in roots():
        parent=row['sourceCanonicalUtf8'].encode('ascii');p,ctx,s,owned=inherited(lambda:rmc.replay(parent,*args));assert rmc.raw(s,'Control').decode('ascii')==row['controlCanonicalUtf8'][-1];assert s['stateVersion']==(38 if row['moves']==0 else 40+row['moves']) and s['activeCycle']['ordinal']==s['movementEnd']['ordinal']==2 and s['closed'];assert s['movementEnd']['completionReceiptId'].startswith('armc.') and s['randomState']['seed']==1 and s['randomState']['nextByteCursor']==10
        data=source_for(parent);_,newctx,state,_=replay(data,*args,[]);inp=command(newctx,state)
        try:
            after,ledger,rid=apply(data,*args,[],inp);_,_,current,_=replay(after,*args,ledger);ev=json.loads(json.loads(after)['breakdownEventCanonicalUtf8'][0]);assert ev['stateVersion']==s['stateVersion']+1 and rid==ev['receiptId'] and rid.startswith('arbc.') and len(ledger)==1;assert current['positionId']==newctx['terminal']['positionId'] and current['closed'] and current['activeCycle']['ordinal']==2;assert ev['effect']['sources']==newctx['boundary']['sources'] and current['movementEnd']==s['movementEnd'] and current['priorMovementEnd']==s['priorMovementEnd'];print('PASS authentic Breakdown publication',row['owner'],row['firstSeal'],row['moves'])
        except Invalid as error:failures.append((row['owner'],row['firstSeal'],row['moves']));print('FAIL authentic terminal version',s['stateVersion'],'requires Breakdown publication; actual',error.code,error.path,row['owner'],row['firstSeal'],row['moves'])
    assert not failures,str(len(failures))+' genuine Breakdown publications failed'
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--group',choices=['semantic']);args=parser.parse_args();semantic_tests()
if __name__=='__main__':main()
