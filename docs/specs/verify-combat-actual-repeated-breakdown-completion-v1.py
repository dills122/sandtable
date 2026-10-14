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
    raw(prior,'Control');raw(inp,'Input');cmd=inp['command'];require(cmd['kind']==KIND and cmd['operationStage']==1,3,'/kindOrStage')
    require(cmd['baseHash']==prior['baseHash']==ctx['baseHash'] and cmd['cycleId']==ctx['cycleId'] and cmd['expectedPositionId']==ctx['boundary']['positionId'] and prior['activeCycle']==ctx['cycle'],4,'/context')
    require(inp['actor']=='system',4,'/actor');require(cmd['actionId']==ACTION,5,'/action');ch=sha(raw(cmd,'Command'));old=next((r for r in prior['receipts'] if r['commandHash']==ch and r['actor']=='system'),None)
    if old:return copy.deepcopy(prior),None,old['receiptId']
    require(cmd['expectedPriorVersion']==prior['stateVersion'],6,'/version');require(not prior['closed'] and len(prior['receipts'])<1 and prior['stateVersion']<2**63-1,7,'/closedOrCapacity')
    require(prior['positionId']==ctx['boundary']['positionId'] and prior['flow']==dict(kind='idle') and prior['interruptContext'] is None and prior['breakdownCompletionReceiptId'] is None,6,'/completionFlow');require(cmd['capabilityId']==capability(ctx,prior),5,'/capability')
    effect=dict(kind='breakdown-completed',movementCompletionReceiptId=prior['movementEnd']['completionReceiptId'],sources=copy.deepcopy(ctx['boundary']['sources']),successorPositionId=ctx['terminal']['positionId']);c=ctx['cycle']
    event=dict(contractVersion=1,eventType='combat-actual-repeated-breakdown-completed',author='system',campaignId=c['campaignId'],rulesetHash=c['rulesetHash'],configurationHash=c['admittedPolicyBundleDigest'],repeatedMovementCompletionSourceHash=prior['repeatedMovementCompletionSourceHash'],baseHash=ctx['baseHash'],cycleId=ctx['cycleId'],positionId=prior['positionId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],input=canonical(inp,'Input'),effect=canonical(effect,'Effect'));rid='arbc.'+identity('receipt',event);event['receiptId']=rid;data=raw(event,'Event')
    after=copy.deepcopy(prior);after.update(stateVersion=event['stateVersion'],prefix=steps.seq.prefix_event(prior['prefix'],data),positionId=ctx['terminal']['positionId'],breakdownCompletionReceiptId=rid,closed=True);after['receipts'].append(dict(commandHash=ch,eventHash=sha(data),receiptId=rid,actor='system',stateVersion=after['stateVersion']));raw(after,'Control');return after,data,rid
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
def selected(owner=None,seal=None,moves=None):return [(r,a) for r,a in roots() if (owner is None or r['owner']==owner) and (seal is None or r['firstSeal']==seal) and (moves is None or r['moves']==moves)]
TRACE_CACHE={}
def trace(row,args):
    key=(row['owner'],row['firstSeal'],row['moves'])
    if key in TRACE_CACHE:return copy.deepcopy(TRACE_CACHE[key])
    parent=row['sourceCanonicalUtf8'].encode('ascii');data=source_for(parent);_,ctx,s,_=replay(data,*args,[]);before=raw(s,'Control');inp=command(ctx,s);after,ledger,rid=apply(data,*args,[],inp);_,_,current,_=replay(after,*args,ledger);event=json.loads(after)['breakdownEventCanonicalUtf8'][0].encode('ascii');out=dict(source=after,sources=[data,after],args=args,ctx=ctx,state=current,frames=[before,raw(current,'Control')],ledger=ledger,inputs=[inp],events=[event],rid=rid,parent=parent);TRACE_CACHE[key]=copy.deepcopy(out);return out

def changed_leaf(value,path,new):
    result=copy.deepcopy(value);v=result
    for k in path[:-1]:v=v[k]
    v[path[-1]]=new;return result

def rejected(call,code=None):
    try:call()
    except Invalid as error:
        if code is not None:assert error.code==f'CMB-ABC-{code:03}',(error.code,code,error.path)
        return error.code
    raise AssertionError('Expected rejection')

def preservation_tests(owner=None,seal=None,moves=None):
    for row,args in selected(owner,seal,moves):
        t=trace(row,args);prior=json.loads(row['controlCanonicalUtf8'][-1]);before=parse(t['frames'][0],'Control');after=t['state'];expected=copy.deepcopy(before);event=json.loads(t['events'][0]);unsigned={k:v for k,v in event.items() if k!='receiptId'};rid='arbc.'+identity('receipt',unsigned);assert rid==t['rid'];assert event['input']==t['inputs'][0] and event['effect']==dict(kind='breakdown-completed',movementCompletionReceiptId=prior['movementEnd']['completionReceiptId'],sources=t['ctx']['boundary']['sources'],successorPositionId=t['ctx']['terminal']['positionId']);assert event['positionId']==prior['positionId'] and event['priorVersion']==prior['stateVersion']
        expected.update(stateVersion=prior['stateVersion']+1,prefix=steps.seq.prefix_event(prior['prefix'],t['events'][0]),positionId=t['ctx']['terminal']['positionId'],closed=True,breakdownCompletionReceiptId=rid);expected['receipts']=[dict(commandHash=sha(raw(t['inputs'][0]['command'],'Command')),eventHash=sha(t['events'][0]),receiptId=rid,actor='system',stateVersion=expected['stateVersion'])];assert raw(expected,'Control')==t['frames'][1]
        for field in prior:
            if field not in ('baseHash','stateVersion','prefix','positionId','receipts','closed'):assert after[field]==prior[field],field
        assert after['priorLifecycleReceipts']==prior['receipts'] and len(after['receipts'])==1 and after['randomState']==prior['randomState'] and after['nextCycleProgress']==prior['nextCycleProgress'];assert after['breakdownCompletionReceiptId']!=after['movementEnd']['completionReceiptId']!=after['priorMovementEnd']['completionReceiptId'];assert after['activeCycle']==prior['activeCycle'] and after['closure']==prior['closure'];print('PASS preservation',row['owner'],row['firstSeal'],row['moves'])

def recovery_tests(owner=None,seal=None,moves=None):
    cuts=retries=0
    for row,args in selected(owner,seal,moves):
        t=trace(row,args)
        for n in range(2):
            source,ctx,s,owned=replay(t['sources'][n],*args,t['ledger'][:n]);assert raw(s,'Control')==t['frames'][n];cuts+=1
            claim=proof(t['sources'][n],*args,t['ledger'][:n]);h=identity('source',canonical(source,'Source'));expected=dict(contractVersion=1,sourceId='arbcomp.'+h,sourceHash='sha256:'+h,base=ctx['base'],control=parse(t['frames'][n],'Control'));assert raw(claim,'Proof')==raw(expected,'Proof');assert read_proof(raw(expected,'Proof'),t['sources'][n],*args,t['ledger'][:n])==expected and read_control(t['frames'][n],t['sources'][n],*args,t['ledger'][:n])==s;assert read_source(t['sources'][n],*args,t['ledger'][:n])==source
            claim['control']['world']['elements'].clear();source['breakdownEventCanonicalUtf8'].clear()

            if n==0:after,ledger,rid=apply(t['sources'][0],*args,[],t['inputs'][0]);assert after==t['source'] and ledger==t['ledger'] and rid==t['rid']
        after,ledger,rid=apply(t['source'],*args,t['ledger'],t['inputs'][0]);assert after==t['source'] and ledger==t['ledger'] and rid==t['rid'];retries+=1
        # Returned buffers are owned; caller mutation cannot affect future reconstruction.
        s['world']['relationships'].clear();owned.clear();assert replay(t['source'],*args,t['ledger'])[2]==t['state']
    print('PASS restart cuts/lostreply/exact terminal retry/owned return',cuts,retries)

def adversarial_tests(owner='axis',seal='attacker',moves=0):
    row,args=selected(owner,seal,moves)[0];t=trace(row,args);initialstate=parse(t['frames'][0],'Control');inp=t['inputs'][0];checks=0
    for actor in ['axis','commonwealth']:
        bad=changed_leaf(inp,('actor',),actor)
        for source,ledger in [(t['sources'][0],[]),(t['source'],t['ledger'])]:rejected(lambda:apply(source,*args,ledger,bad),4);checks+=1
    for path,value,code in [(('command','operationStage'),2,3),(('command','kind'),'finish-cycle',3),(('command','actionId'),'sha256:'+'0'*64,5),(('command','baseHash'),'sha256:'+'0'*64,4),(('command','cycleId'),'sha256:'+'0'*64,4),(('command','expectedPositionId'),'land.position.breakdown-stop',4),(('command','expectedPriorVersion'),0,6),(('command','capabilityId'),'sha256:'+'0'*64,5)]:
        bad=changed_leaf(inp,path,value);rejected(lambda:apply(t['sources'][0],*args,[],bad),code);checks+=1
        rejected(lambda:apply(t['source'],*args,t['ledger'],bad),6 if code==5 and path[-1]=='capabilityId' else code);checks+=1
    # Every own effect leaf is re-signed; claimed prefix/receipt cannot create authority.
    event=json.loads(t['events'][0]);changes=[(('effect','kind'),'invented'),(('effect','movementCompletionReceiptId'),'forged'),(('effect','sources',0,'sourceId'),'forged'),(('effect','sources',0,'locator'),'21.24-21.26'),(('effect','successorPositionId'),t['ctx']['boundary']['positionId']), (('priorPrefix',),'sha256:'+'0'*64),(('stateVersion',),event['stateVersion']+1)]
    for path,value in changes:
        e=changed_leaf(event,path,value);e['receiptId']='arbc.'+identity('receipt',{k:v for k,v in e.items() if k!='receiptId'});source=parse(t['source'],'Source');source['breakdownEventCanonicalUtf8']=[raw(e,'Event').decode('ascii')];rejected(lambda:replay(raw(source,'Source'),*args,t['ledger']),6);checks+=1
    claims=[(('movementEnd','completionReceiptId'),'forged'),(('priorMovementEnd','completionReceiptId'),'forged'),(('world','elements',0,'currentLocationId'),'forged'),(('randomState','nextByteCursor'),11),(('members',0,'spentCp'),world.cp(0)),(('breakdownCompletionReceiptId',),'forged'),(('activeCycle','ordinal'),3),(('closed',),False)]
    if moves:claims.append((('nextCycleProgress',0,'receiptId'),'forged'))
    for path,value in claims:
        bad=changed_leaf(t['state'],path,value);rejected(lambda:read_control(raw(bad,'Control'),t['source'],*args,t['ledger']),6);checks+=1
    p=proof(t['source'],*args,t['ledger']);p['base']['repeatedMovementCompletionProof']['control']['movementEnd']['completionReceiptId']='forged';rejected(lambda:read_proof(raw(p,'Proof'),t['source'],*args,t['ledger']),6);checks+=1
    for ledger,code in [(None,1),([None],1),([],4),([inp,inp],1)]:rejected(lambda:replay(t['source'],*args,ledger),code);checks+=1
    bad=changed_leaf(inp,('command','actionId'),'sha256:'+'1'*64);rejected(lambda:replay(t['source'],*args,[bad]),5);checks+=1
    duplicate=parse(t['source'],'Source');duplicate['breakdownEventCanonicalUtf8']*=2;rejected(lambda:raw(duplicate,'Source'),1);checks+=1
    rejected(lambda:apply(t['source'],*args,t['ledger'],command(t['ctx'],t['state'])));checks+=1
    print('PASS adversarial',row['owner'],row['firstSeal'],row['moves'],checks)

def nonterminal_tests():
    checks=0
    for row,args in roots():
        terminal=json.loads(row['sourceCanonicalUtf8']);count=len(row['lifecycleLedger'])
        for n in range(count):
            parent=copy.deepcopy(terminal);parent['lifecycleEventCanonicalUtf8']=parent['lifecycleEventCanonicalUtf8'][:n];data=source_for(rmc.raw(parent,'Source'));ledgers=args[:-1]+[args[-1][:n]];rejected(lambda:replay(data,*ledgers,None),3);checks+=1
    assert checks==52;print('PASS all authentic nonterminal RMC cuts rejected before seventh syntax',checks)

def grammar_tests():
    row,args=roots()[0];data=source_for(row['sourceCanonicalUtf8'].encode('ascii'));_,ctx,state,_=replay(data,*args,[]);inp=command(ctx,state)
    for value,code in [(True,1),(-1,2),(1.5,1),(2**63,2)]:bad=changed_leaf(inp,('command','expectedPriorVersion'),value);rejected(lambda:apply(data,*args,[],bad),code)
    for value,code in [(True,1),(-1,2),(2,3)]:bad=changed_leaf(inp,('command','contractVersion'),value);rejected(lambda:raw(bad,'Input'),code)
    for kind,v in [('Source',parse(data,'Source')),('Control',state),('Input',inp)]:
        for field in v:bad=copy.deepcopy(v);bad.pop(field);rejected(lambda:parse(encode(bad),kind),1)
        good=raw(v,kind)
        for malformed in [good+b'\n',b' '+good,b'\xef\xbb\xbf'+good,good.replace(b'{',b'{"unknown":null,',1)]:rejected(lambda:parse(malformed,kind))
    for malformed in [b'{"contractVersion":1,"contractVersion":1}',b'['*44+b'0'+b']'*44,b'['+b','.join([b'0']*513)+b']',b'{'+b','.join(('"k'+str(i)+'":0').encode() for i in range(129))+b'}']:rejected(lambda:parse(malformed,'Source'),1)
    bad=parse(data,'Source');bad['repeatedMovementCompletionSourceCanonicalUtf8']='{}';rejected(lambda:read_control(b'{}',raw(bad,'Source'),*args,None),1)
    for which in range(6):
        ledgers=copy.deepcopy(args);ledgers[which]=None;rejected(lambda:read_proof(b'{}',data,*ledgers,None),1)
    rejected(lambda:replay(data,*args,None),1);rejected(lambda:replay(data,*args,[None]),1);rejected(lambda:replay(data,*args,[inp]),4)
    overflow=copy.deepcopy(state);overflow['stateVersion']=2**63-1;rejected(lambda:transition(ctx,overflow,command(ctx,overflow)),7)
    wrong=copy.deepcopy(state);wrong['activeCycle']['ordinal']=3;rejected(lambda:transition(ctx,wrong,command(ctx,wrong)),4)
    for key in ['clock','deadline','rng','fallback','checks']:bad=copy.deepcopy(inp);bad[key]=None;rejected(lambda:raw(bad,'Input'),1)
    bad=parse(data,'Source');ancestor=json.loads(bad['repeatedMovementCompletionSourceCanonicalUtf8']);ancestor['contractVersion']=-1;bad['repeatedMovementCompletionSourceCanonicalUtf8']=encode(ancestor).decode('ascii');rejected(lambda:read_proof(b'{}',raw(bad,'Source'),*args,None),2)
    malformed=copy.deepcopy(bad);malformed.pop('contractVersion');rejected(lambda:read_proof(b'{}',encode(malformed),*args,None),1)
    rejected(lambda:read_proof(b'{"contractVersion":-1}',data,*args,None),1)
    for change in ['actor','actionId','capabilityId']:
        bad=copy.deepcopy(inp)
        if change=='actor':bad['actor']='axis'
        else:bad['command'][change]='sha256:'+'0'*64
        rejected(lambda:replay(data,*args,[bad]),4)
    original=dict(DEPENDENCIES)
    try:
        DEPENDENCIES['../unknown']='0'*64;rejected(verify_dependencies,9)
    finally:DEPENDENCIES.clear();DEPENDENCIES.update(original)
    print('PASS closed primitives/canonical/preparse/numeric/unsupported inputs/ancestor-before-seventh-and-claims')

def timestamp_tests():
    row,p,u=rmc.roots()[0];parent=rmc.arm.pac.trace(u,'repeat',2000,2001);four=list(parent['args'])+[parent['ledger']];base=rmc.arm.derive(parent['source'],*four);s=rmc.arm.initial(base);data=rmc.arm.raw(dict(contractVersion=1,continuationSourceCanonicalUtf8=parent['source'].decode('ascii'),movementEventCanonicalUtf8=[]),'Source');move=[]
    data,move,_=rmc.arm.apply(data,*four,move,rmc.arm.command(base,s,row['owner']+'-rear'));five=four+[move];source=rmc.source_for(data);_,ctx,s,_=rmc.replay(source,*five,[]);ledger=[]
    for kind in rmc.INVENTORY['commands']:source,ledger,_=rmc.apply(source,*five,ledger,rmc.command(ctx,s,kind));s=rmc.replay(source,*five,ledger)[2]
    own=source_for(source);_,ctx,before,_=replay(own,*five,ledger,[]);after,inputs,rid=apply(own,*five,ledger,[],command(ctx,before));s=replay(after,*five,ledger,inputs)[2];assert s['stateVersion']==42 and s['movementEnd']==before['movementEnd'] and s['randomState']==before['randomState'] and s['members'][0]['spentCp']==world.cp(9);assert rid==s['breakdownCompletionReceiptId'];print('PASS genuine inherited clock variant through full six-ledger ancestry and seventh completion')

def capacity_tests():
    model=dict(Source=2*rmc.INVENTORY['encodedUpperBounds']['Source']+2*LIMITS['eventBytes']+512,Proof=rmc.INVENTORY['encodedUpperBounds']['Proof']+rmc.INVENTORY['capacityAccounting']['world']+131072,Control=rmc.INVENTORY['capacityAccounting']['world']+131072,Event=16384,Input=1024,BreakdownLedger=1026)
    caps=dict(Source=LIMITS['sourceBytes'],Proof=LIMITS['proofBytes'],Control=LIMITS['controlBytes'],Event=LIMITS['eventBytes'],Input=LIMITS['inputBytes'],BreakdownLedger=LIMITS['breakdownLedgerBytes'])
    for k,n in model.items():assert n<=caps[k],(k,n,caps[k])
    if INVENTORY['closureFrozen']:assert model==INVENTORY['encodedUpperBounds']
    row,args=selected('axis','attacker',4)[0];t=trace(row,args);fields={}
    def upper(v):
        if type(v) is dict:return {k:(None if k in ('world','repeatedMovementCompletionProof') else upper(x)) for k,x in v.items()}
        if type(v) is list:return [upper(x) for x in v]
        if type(v) is str:return 'x'*128
        if type(v) is bool:return False
        if type(v) is int:return -2**63
        return v
    controls=[parse(x,'Control') for x in t['frames']];unit=controls[0]['members'][0]['unit'];padded=[]
    for c in controls:
        c=copy.deepcopy(c)
        for field in ['priorMovementEnd','movementEnd']:c[field]['excludedBefore']=[copy.deepcopy(unit)]*2
        padded.append(c)
    fields['ControlMetadata']=max(len(encode(upper(c))) for c in padded);prototype=dict(contractVersion=1,sourceId='arbcomp.'+'0'*64,sourceHash='sha256:'+'0'*64,base=t['ctx']['base'],control=t['state']);fields['ProofMetadata']=max(len(encode(upper(prototype|dict(control=c)))) for c in padded);fields['EventMetadata']=max(len(encode(upper(json.loads(e)))) for e in t['events']);inflated=copy.deepcopy(t['inputs'][0]);inflated['command']['expectedPositionId']='x'*128;inflated['command']['expectedPriorVersion']=2**63-1;inflated['actor']='commonwealth';fields['InputStructural']=len(encode(inflated))
    assert fields['ControlMetadata']<=131072 and fields['ProofMetadata']<=131072 and fields['EventMetadata']<=16384 and fields['InputStructural']<=1024
    measurements=dict(Source=len(t['source']),Proof=len(raw(prototype,'Proof')),Control=max(len(x) for x in t['frames']),Event=max(len(x) for x in t['events']),Input=len(raw(t['inputs'][0],'Input')),BreakdownLedger=len(encode(t['ledger'])))
    for k,n in measurements.items():assert n<=model[k],(k,n,model[k])
    for kind in ['Source','Proof','Control','Event','Input']:rejected(lambda:parse(b' '*(byte_limit(kind)+1),kind),1)
    carrier=parse(t['sources'][0],'Source');carrier['repeatedMovementCompletionSourceCanonicalUtf8']='x'*(rmc.LIMITS['sourceBytes']+1);rejected(lambda:raw(carrier,'Source'),1)
    assert LIMITS['depth']==rmc.LIMITS['depth']+2 and LIMITS['acceptedEvents']==1 and LIMITS['arrayItems']==rmc.LIMITS['arrayItems'] and LIMITS['objectFields']==rmc.LIMITS['objectFields'];assert len(DEPENDENCIES)==70
    print('PASS own field/carrier/wrapper/array/depth domination',model,fields,measurements);return dict(dominating=model,limits=caps,fieldAccounting=fields,measured=measurements)

def dependency_tests():
    row,args=roots()[0];data=source_for(row['sourceCanonicalUtf8'].encode('ascii'));real=Path.read_bytes;checks=0;paths=list(REPOSITORY/p for p in DEPENDENCIES)+list(OWN_DELIVERY)
    for physical in paths:
        for outcome in ['changed','missing','unreadable','null','drift']:
            hits=[0]
            def altered(p):
                if p.resolve()==physical.resolve():
                    hits[0]+=1
                    if outcome=='drift' and hits[0]==1:return real(p)
                    if outcome=='missing':raise FileNotFoundError(str(p))
                    if outcome=='unreadable':raise PermissionError(str(p))
                    return None if outcome=='null' else b'changed'
                return real(p)
            try:Path.read_bytes=altered;rejected(lambda:replay(data,*args,[]),9);checks+=1
            finally:Path.read_bytes=real
    assert checks==360;print('PASS all72 physical paths missing/changed/unreadable/null/drift',checks);return checks

def closure_tests():
    real=Path.read_bytes;reads=set();runtime=set((REPOSITORY/p).resolve() for p in DEPENDENCIES)|set(p.resolve() for p in OWN_DELIVERY)
    def track(p):reads.add(p.resolve());return real(p)
    try:
        Path.read_bytes=track;spec=importlib.util.spec_from_file_location('abc_cold_measurement',Path(__file__).resolve());fresh=importlib.util.module_from_spec(spec);spec.loader.exec_module(fresh)
    finally:Path.read_bytes=real
    assert reads==runtime,(reads-runtime,runtime-reads);phases={'cold':len(reads)}
    row,args=roots()[0];data=source_for(row['sourceCanonicalUtf8'].encode('ascii'))
    for name,call in [('core',lambda:replay(data,*args,[])),('warm',lambda:replay(data,*args,[])),('adversarial',lambda:replay(data,*args,None))]:
        reads=set()
        try:
            Path.read_bytes=track
            if name=='adversarial':rejected(call,1)
            else:call()
        finally:Path.read_bytes=real
        assert reads==runtime,(name,reads-runtime,runtime-reads);phases[name]=len(reads)
    print('PASS measured cold/core/warm/adversarial runtime70+own2 with zero unknown material paths',phases);return phases

def fixture_artifacts():
    traces=[]
    for row,args in roots():
        t=trace(row,args);p=proof(t['source'],*args,t['ledger']);traces.append(dict(owner=row['owner'],firstSeal=row['firstSeal'],moves=row['moves'],sourceCanonicalUtf8=t['source'].decode('ascii'),breakdownLedger=t['ledger'],controlCanonicalUtf8=[x.decode('ascii') for x in t['frames']],proofCanonicalUtf8=raw(p,'Proof').decode('ascii'),sourceHash=sha(t['source']),proofHash=sha(raw(p,'Proof')),eventHashes=[sha(e) for e in t['events']]))
    return dict(contractVersion=1,dependencyManifestHash='sha256:'+identity('dependency-manifest',dict(sorted(DEPENDENCIES.items()))),traces=traces)
FIXTURE=ROOT/'fixtures/combat-actual-repeated-breakdown-completion-v1.json'
def fixture_tests():assert json.loads(test_bytes('docs/specs/fixtures/combat-actual-repeated-breakdown-completion-v1.json'))==fixture_artifacts();print('PASS retained20traces/20events/40frames literals without regeneration')
def normal_gate():
    semantic_tests();grammar_tests();nonterminal_tests();timestamp_tests();capacity_tests();closure_tests();dependency_tests()
    for owner in ['axis','commonwealth']:
        for seal in ['attacker','defender']:preservation_tests(owner,seal);recovery_tests(owner,seal);adversarial_tests(owner,seal,0);adversarial_tests(owner,seal,4)
    fixture_tests();print('PASS private actual Breakdown contract; no downstream/native/public/parent execution')
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--group',choices=['semantic','preservation','recovery','adversarial','grammar','nonterminal','timestamp','capacity','dependencies','closure','fixture']);parser.add_argument('--owner',choices=['axis','commonwealth']);parser.add_argument('--seal',choices=['attacker','defender']);parser.add_argument('--moves',type=int,choices=range(5));parser.add_argument('--write-fixture',action='store_true');args=parser.parse_args()
    if args.write_fixture:require(INVENTORY['closureFrozen'],7,'/provisionalClosure');FIXTURE.write_text(json.dumps(fixture_artifacts(),indent=2)+'\n')
    elif args.group in ['preservation','recovery']:globals()[args.group+'_tests'](args.owner,args.seal,args.moves)
    elif args.group=='adversarial':adversarial_tests(args.owner or 'axis',args.seal or 'attacker',0 if args.moves is None else args.moves)
    elif args.group:globals()[args.group+'_tests']()
    else:normal_gate()
if __name__=='__main__':main()
