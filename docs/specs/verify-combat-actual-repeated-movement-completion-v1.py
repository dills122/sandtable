#!/usr/bin/env python3
"""Private authentic repeated Movement stop, System resolution and owner completion."""
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



def derive(parent,ctx,state,owned):
    c=state['activeCycle'];count=len(owned);w=state['world'];m=state['members'];end=state['movementEnd']
    require(c['ordinal']==2 and end['ordinal']==1 and state['stateVersion']==37+count and 0<=count<=4 and state['positionId']==policy.edge(c)['movementPositionId'],3,'/profile')
    require(len(m)==1 and m[0]['status']=='none' and all(m[0]['history'][k] is None for k in m[0]['history'] if k!='scope') and not state['targetUses'],3,'/member')
    require(len(state['tracks'])==int(count>0) and len(state['receipts'])==len(state['nextCycleProgress'])==count,4,'/movementCause')
    require(all(next(e for e in world.PACK['elements'] if e['elementId']==x['unit']['elementId'])['breakdownVehicleCohort'] is None for x in m),3,'/cohort')
    if count:
        track=state['tracks'][0];require(track['unit']==m[0]['unit'] and len(track['route'])==count+1 and track['route'][-1]==next(e['currentLocationId'] for e in w['elements'] if e['elementId']==m[0]['unit']['elementId']),4,'/track')
        flow=dict(kind='moving',track=copy.deepcopy(track),lastMoveReceiptId=state['receipts'][-1]['receiptId'])
    else:flow=dict(kind='idle')
    h=arm.identity('source',arm.canonical(parent,'Source'));priorproof=dict(contractVersion=1,sourceId='armove.'+h,sourceHash='sha256:'+h,base=copy.deepcopy(ctx['base']),control=copy.deepcopy(state));inherited(lambda:arm.raw(priorproof,'Proof'))
    base=dict(contractVersion=1,dependencyManifestHash='sha256:'+identity('dependency-manifest',dict(sorted(DEPENDENCIES.items()))),movementProof=priorproof);raw(base,'Base')
    require(state['stateVersion']<=2**63-1-(3 if count else 1),7,'/wholeTraceCapacity')
    return dict(base=base,baseHash='sha256:'+identity('base',canonical(base,'Base')),prior=copy.deepcopy(state),cycle=copy.deepcopy(c),cycleId=policy.cycle_id(c,ctx['firstActingSide']),firstActingSide=ctx['firstActingSide'],initialFlow=flow,eventCap=3 if count else 1)
def initial(ctx):
    p=ctx['prior'];return dict(contractVersion=1,baseHash=ctx['baseHash'],movementSourceHash=ctx['base']['movementProof']['sourceHash'],stateVersion=p['stateVersion'],prefix=p['prefix'],positionId=p['positionId'],originalCycle=copy.deepcopy(p['originalCycle']),activeCycle=copy.deepcopy(p['activeCycle']),closure=copy.deepcopy(p['closure']),priorMovementEnd=copy.deepcopy(p['movementEnd']),movementEnd=None,members=copy.deepcopy(p['members']),world=copy.deepcopy(p['world']),randomState=copy.deepcopy(p['randomState']),attackHistory=copy.deepcopy(p['attackHistory']),targetUses=copy.deepcopy(p['targetUses']),nextCycleProgress=copy.deepcopy(p['nextCycleProgress']),tracks=copy.deepcopy(p['tracks']),movementReceipts=copy.deepcopy(p['receipts']),flow=copy.deepcopy(ctx['initialFlow']),interruptContext=None,receipts=[],closed=False)
def admission(data,selection,rounds,paid,continuation,movement,lifecycle):
    verify_dependencies();source=parse(data,'Source');parent,priorctx,state,owned=inherited(lambda:arm.replay(source['movementSourceCanonicalUtf8'].encode('ascii'),selection,rounds,paid,continuation,movement));ctx=derive(parent,priorctx,state,owned)
    require(type(lifecycle) is list and len(lifecycle)<=ctx['eventCap'],1,'/lifecycleLedger');inputs=[parse(raw(x,'Input'),'Input') for x in lifecycle];require(len(encode(inputs))<=LIMITS['lifecycleLedgerBytes'],1,'/lifecycleLedger');require(len(inputs)==len(source['lifecycleEventCanonicalUtf8']),4,'/lifecycleLedger');require(len(inputs)<=ctx['eventCap'],1,'/eventCap');return source,ctx,inputs
def expected_position(ctx,kind):return 'land.position.breakdown-stop' if kind=='resolve-breakdown-stop' else ctx['prior']['positionId']
def capability(ctx,s,kind):return 'sha256:'+identity('capability',dict(baseHash=ctx['baseHash'],cycleId=ctx['cycleId'],kind=kind,stateVersion=s['stateVersion'],positionId=s['positionId'],flow=canonical(s['flow'],'Flow'),interruptContext=canonical(s['interruptContext'],'Context?')))
def command(ctx,s,kind):return dict(command=dict(contractVersion=1,kind=kind,baseHash=ctx['baseHash'],cycleId=ctx['cycleId'],expectedPositionId=expected_position(ctx,kind),expectedPriorVersion=s['stateVersion'],capabilityId=capability(ctx,s,kind)),actor='system' if kind=='resolve-breakdown-stop' else ctx['cycle']['actingSide'])
def expiry(ctx,s,rid):
    inp=dict(contractVersion=1,cycle=copy.deepcopy(ctx['cycle']),firstActingSide=ctx['firstActingSide'],priorProof=copy.deepcopy(s['priorMovementEnd']),completionReceiptId=rid,endLocations=policy.locations(s['world']),members=copy.deepcopy(s['members']))
    out=inherited(lambda:policy.expire_movement(inp));require(out['members']==s['members'],4,'/noneHistory');return out
def transition(ctx,prior,inp):
    raw(prior,'Control');raw(inp,'Input');cmd=inp['command'];kind=cmd['kind'];require(kind in INVENTORY['commands'],3,'/kind');c=ctx['cycle'];actor=inp['actor']
    require(cmd['baseHash']==prior['baseHash']==ctx['baseHash'] and cmd['cycleId']==ctx['cycleId'] and cmd['expectedPositionId']==expected_position(ctx,kind) and prior['activeCycle']==c,4,'/context')
    require(actor==('system' if kind=='resolve-breakdown-stop' else c['actingSide']),4,'/actor');ch=sha(raw(cmd,'Command'));old=next((r for r in prior['receipts'] if r['commandHash']==ch and r['actor']==actor),None)
    if old:return copy.deepcopy(prior),None,old['receiptId']
    require(not prior['closed'] and len(prior['receipts'])<ctx['eventCap'] and prior['stateVersion']<2**63-1,7,'/closedOrCapacity');require(cmd['expectedPriorVersion']==prior['stateVersion'] and cmd['expectedPositionId']==prior['positionId'],6,'/versionOrPosition');require(cmd['capabilityId']==capability(ctx,prior,kind),5,'/capability')
    s=copy.deepcopy(prior);flow=prior['flow'];move=ctx['prior']['positionId'];context=dict(cycle=copy.deepcopy(c),cycleId=ctx['cycleId'],suspendedPositionId=move)
    if kind=='stop-element-movement':
        require(flow['kind']=='moving' and prior['interruptContext'] is None and prior['movementEnd'] is None and prior['positionId']==move,6,'/stopFlow');require(flow==ctx['initialFlow'] and flow['track']==prior['tracks'][0] and flow['lastMoveReceiptId']==prior['movementReceipts'][-1]['receiptId'],4,'/stopCause')
        descriptor=dict(recordedStateVersion=prior['stateVersion']+1,track=copy.deepcopy(flow['track']),lastMoveReceiptId=flow['lastMoveReceiptId'],reason='deliberate',weatherKind='normal',cohortInputs=[]);sid='armstop.'+identity('stop',dict(baseHash=ctx['baseHash'],cycleId=ctx['cycleId'],descriptor=descriptor));stop=dict(stopId=sid,**descriptor)
        s['flow']=dict(kind='phasing-stop',stop=stop);s['interruptContext']=context;s['positionId']='land.position.breakdown-stop';effect=dict(kind='movement-stopped',stop=copy.deepcopy(stop),interruptContext=copy.deepcopy(context),successorPositionId=s['positionId'])
    elif kind=='resolve-breakdown-stop':
        require(flow['kind']=='phasing-stop' and prior['interruptContext']==context and prior['positionId']=='land.position.breakdown-stop' and prior['movementEnd'] is None,6,'/resolutionFlow')
        stop=flow['stop'];require(stop['track']==ctx['initialFlow'].get('track') and stop['lastMoveReceiptId']==ctx['initialFlow'].get('lastMoveReceiptId') and not stop['cohortInputs'] and stop['recordedStateVersion']==prior['stateVersion'],4,'/pendingStop')
        s['flow']=dict(kind='idle');s['interruptContext']=None;s['positionId']=move;effect=dict(kind='stop-resolved',stopId=stop['stopId'],beforeRandomState=copy.deepcopy(prior['randomState']),afterRandomState=copy.deepcopy(prior['randomState']),checks=[],createdLots=[],sources=['spi-1979-land-rules:21.24-21.26'],successorPositionId=move)
    else:
        require(flow==dict(kind='idle') and prior['interruptContext'] is None and prior['movementEnd'] is None and prior['positionId']==move,6,'/completionFlow')
        if ctx['eventCap']==3:require(len(prior['receipts'])==2,6,'/requiredResolution')
        preview=expiry(ctx,prior,'pending');s['positionId']=policy.edge(c)['breakdownPositionId'];s['closed']=True;effect=dict(kind='movement-completed',endLocations=copy.deepcopy(preview['proof']['endLocations']),excludedUnits=copy.deepcopy(preview['proof']['excludedBefore']),progress=copy.deepcopy(prior['nextCycleProgress']),successorPositionId=s['positionId'])
    event=dict(contractVersion=1,eventType='combat-actual-repeated-'+effect['kind'],author=actor,campaignId=c['campaignId'],rulesetHash=c['rulesetHash'],configurationHash=c['admittedPolicyBundleDigest'],movementSourceHash=prior['movementSourceHash'],baseHash=ctx['baseHash'],cycleId=ctx['cycleId'],positionId=prior['positionId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],input=canonical(inp,'Input'),effect=canonical(effect,'Effect'));rid='armc.'+identity('receipt',event);event['receiptId']=rid;data=raw(event,'Event')
    if effect['kind']=='movement-completed':out=expiry(ctx,prior,rid);s['movementEnd']=out['proof'];s['members']=out['members']
    s['stateVersion']=event['stateVersion'];s['prefix']=steps.seq.prefix_event(prior['prefix'],data);s['receipts'].append(dict(commandHash=ch,eventHash=sha(data),receiptId=rid,actor=actor,stateVersion=s['stateVersion']));raw(s,'Control');return s,data,rid
def replay(data,selection,rounds,paid,continuation,movement,lifecycle):
    source,ctx,owned=admission(data,selection,rounds,paid,continuation,movement,lifecycle);s=initial(ctx)
    for n,(text,inp) in enumerate(zip(source['lifecycleEventCanonicalUtf8'],owned)):
        event=text.encode('ascii');parse(event,'Event');s,expected,_=transition(ctx,s,inp);require(expected is not None and event==expected,6,'/event/'+str(n))
    verify_dependencies();return source,ctx,s,owned
def apply(data,selection,rounds,paid,continuation,movement,lifecycle,new_input):
    source,ctx,s,owned=replay(data,selection,rounds,paid,continuation,movement,lifecycle);after,event,rid=transition(ctx,s,new_input)
    if event is None:verify_dependencies();return raw(source,'Source'),copy.deepcopy(owned),rid
    source['lifecycleEventCanonicalUtf8'].append(event.decode('ascii'));ledger=owned+[parse(raw(new_input,'Input'),'Input')];require(len(encode(ledger))<=LIMITS['lifecycleLedgerBytes'],1);result=raw(source,'Source');verify_dependencies();return result,ledger,rid
def read_source(data,*ledgers):
    result=copy.deepcopy(replay(data,*ledgers)[0]);require(INVENTORY['closureFrozen'],7,'/provisionalClosure');return result
def proof(data,*ledgers):
    source,ctx,s,_=replay(data,*ledgers);require(INVENTORY['closureFrozen'],7,'/provisionalClosure');h=identity('source',canonical(source,'Source'));out=dict(contractVersion=1,sourceId='armcomp.'+h,sourceHash='sha256:'+h,base=ctx['base'],control=s);return parse(raw(out,'Proof'),'Proof')
def read_proof(claim,data,*ledgers):
    expected=proof(data,*ledgers);value=parse(claim,'Proof');require(claim==raw(expected,'Proof'),6,'/proof');return value
def read_control(claim,data,*ledgers):
    state=replay(data,*ledgers)[2];require(INVENTORY['closureFrozen'],7,'/provisionalClosure');value=parse(claim,'Control');require(claim==raw(state,'Control'),6,'/control');return value

def test_bytes(path):
    require(path in INVENTORY['testArtifactHashes'],9,'/testArtifact');data=(REPOSITORY/path).read_bytes();require(hashlib.sha256(data).hexdigest()==INVENTORY['testArtifactHashes'][path],9,path);return data
def roots():
    rows=json.loads(test_bytes('docs/specs/fixtures/combat-actual-repeated-movement-v1.json'))['traces'];prior=arm.roots()
    return [(r,*next((p,u) for p,u in prior if (p['owner'],p['firstSeal'])==(r['owner'],r['firstSeal']))) for r in rows]
def root_cut(row,prior,upstream,count):
    parent=json.loads(row['sourceCanonicalUtf8']);parent['movementEventCanonicalUtf8']=parent['movementEventCanonicalUtf8'][:count];data=arm.raw(parent,'Source');ledgers=arm.arguments(prior,upstream)+[copy.deepcopy(row['movementLedger'][:count])];return data,ledgers

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

def source_for(parent):return raw(dict(contractVersion=1,movementSourceCanonicalUtf8=parent.decode('ascii'),lifecycleEventCanonicalUtf8=[]),'Source')
def selected(owner=None,seal=None,moves=None):return [(r,p,u,n) for r,p,u in roots() for n in range(5) if (owner is None or r['owner']==owner) and (seal is None or r['firstSeal']==seal) and (moves is None or n==moves)]
TRACE_CACHE={}
def trace(row,prior,upstream,count):
    key=(row['owner'],row['firstSeal'],count)
    if key in TRACE_CACHE:return copy.deepcopy(TRACE_CACHE[key])
    parent,args=root_cut(row,prior,upstream,count);data=source_for(parent);source,ctx,state,_=replay(data,*args,[]);frames=[raw(state,'Control')];sources=[data];inputs=[];events=[];ledger=[]
    kinds=INVENTORY['commands'] if count else ['complete-movement-segment']
    for kind in kinds:
        inp=command(ctx,state,kind);data,ledger,rid=apply(data,*args,ledger,inp);state=replay(data,*args,ledger)[2];event=json.loads(data)['lifecycleEventCanonicalUtf8'][-1].encode('ascii');assert json.loads(event)['receiptId']==rid;inputs.append(inp);events.append(event);sources.append(data);frames.append(raw(state,'Control'))
    out=dict(source=data,ledger=ledger,args=args,ctx=ctx,state=state,frames=frames,sources=sources,inputs=inputs,events=events,parent=parent,moves=count);TRACE_CACHE[key]=copy.deepcopy(out);return out
def preservation_tests(owner=None,seal=None,moves=None):
    traces=events=frames=0
    for row,p,u,n in selected(owner,seal,moves):
        t=trace(row,p,u,n);before=initial(t['ctx']);base=t['ctx']['prior'];c=t['ctx']['cycle'];prefix=base['prefix'];traces+=1;events+=len(t['events']);frames+=len(t['frames'])
        assert t['state']['stateVersion']==(38 if n==0 else 40+n) and t['state']['positionId']==policy.edge(c)['breakdownPositionId'] and t['state']['closed'] and t['state']['flow']==dict(kind='idle') and t['state']['interruptContext'] is None
        for i,frame in enumerate(t['frames']):
            s=parse(frame,'Control');assert s['world']==base['world'] and s['members']==base['members'] and s['randomState']==base['randomState']
            for k in ('originalCycle','activeCycle','closure','attackHistory','targetUses','nextCycleProgress','tracks'):assert s[k]==base[k],k
            assert s['priorMovementEnd']==base['movementEnd'] and s['movementReceipts']==base['receipts'] and len(s['receipts'])==i
            if i<len(t['events']):assert s['movementEnd'] is None and not s['closed']
        for i,text in enumerate(t['events']):
            e=parse(text,'Event');assert e['priorPrefix']==prefix and e['priorVersion']==37+n+i and e['stateVersion']==38+n+i and e['receiptId']=='armc.'+identity('receipt',{k:v for k,v in e.items() if k!='receiptId'});prefix=steps.seq.prefix_event(prefix,text)
            if e['effect']['kind']=='movement-stopped':assert e['author']==row['owner'] and e['effect']['stop']['cohortInputs']==[] and e['effect']['interruptContext']==dict(cycle=c,cycleId=t['ctx']['cycleId'],suspendedPositionId=base['positionId']) and parse(t['frames'][1],'Control')['positionId']=='land.position.breakdown-stop'
            if e['effect']['kind']=='stop-resolved':assert e['author']=='system' and e['effect']['checks']==e['effect']['createdLots']==[] and e['effect']['beforeRandomState']==e['effect']['afterRandomState']==base['randomState'] and e['effect']['sources']==['spi-1979-land-rules:21.24-21.26'] and parse(t['frames'][2],'Control')['positionId']==base['positionId']
        last=json.loads(t['events'][-1]);assert t['state']['prefix']==prefix and last['effect']['progress']==base['nextCycleProgress']
        endpoints=policy.locations(base['world']);expected=dict(scope=rel.scope(c),ordinal=2,completionReceiptId=last['receiptId'],endLocations=endpoints,excludedBefore=policy.excluded(base['movementEnd'],c['actingSide']));expected['excludedBefore']=policy.excluded(expected,c['actingSide']);assert t['state']['movementEnd']==expected and last['effect']['excludedUnits']==expected['excludedBefore'];assert len(expected['excludedBefore'])==int(n in (2,4))
        relation=base['world']['relationships'][0];assert relation['active']==(n==0)
    print('PASS complete genuine lifecycle/preservation/proof/proximity',traces,events,frames)
def recovery_tests(owner=None,seal=None,moves=None):
    cuts=retries=0
    for row,p,u,n in selected(owner,seal,moves):
        t=trace(row,p,u,n)
        for cut in range(len(t['events'])+1):
            data=t['sources'][cut];ledger=t['ledger'][:cut];args=t['args']+[ledger];source,ctx,state,owned=replay(data,*args);assert raw(state,'Control')==t['frames'][cut];cuts+=1
            for j in range(cut):
                result,retained,rid=apply(data,*args,t['inputs'][j]);assert result==data and retained==ledger and rid==json.loads(t['events'][j])['receiptId'];retries+=1
            if INVENTORY['closureFrozen']:
                claim=proof(data,*args);assert read_proof(raw(claim,'Proof'),data,*args)==claim and read_control(t['frames'][cut],data,*args)==state;claim['control']['world']['elements'].clear();source['lifecycleEventCanonicalUtf8'].clear();owned.clear();assert raw(proof(data,*args)['control'],'Control')==t['frames'][cut]
    print('PASS all suffix cuts/all prior retries/owned readback',cuts,retries)
def changed_leaf(value,path,new):
    out=copy.deepcopy(value);parent=out
    for p in path[:-1]:parent=parent[p]
    parent[path[-1]]=new;return out
def rejected(call,code=None):
    try:call()
    except Invalid as error:
        if code is not None:assert error.code==f'CMB-RMC-{code:03}',(error.code,error.path,code)
        return error
    raise AssertionError('invalid input accepted')
def adversarial_tests(owner='axis',seal='attacker',moves=None):
    checks=0
    for row,p,u,n in selected(owner,seal,moves):
        t=trace(row,p,u,n);ctx=t['ctx'];initialState=parse(t['frames'][0],'Control');args=t['args'];data=t['sources'][0]
        for kind in (['stop-element-movement','resolve-breakdown-stop'] if n==0 else ['resolve-breakdown-stop','complete-movement-segment']):rejected(lambda:apply(data,*args,[],command(ctx,initialState,kind)),6);checks+=1
        if n:
            pending=parse(t['frames'][1],'Control');rejected(lambda:apply(t['sources'][1],*args,t['ledger'][:1],command(ctx,pending,'complete-movement-segment')),6);checks+=1
            rejected(lambda:apply(t['sources'][1],*args,t['ledger'][:1],command(ctx,pending,'stop-element-movement')),6);checks+=1
        for i,inp in enumerate(t['inputs']):
            source=t['sources'][i];ledger=t['ledger'][:i];s=parse(t['frames'][i],'Control')
            wrong=changed_leaf(inp,('actor',),'system' if inp['actor']!='system' else row['owner']);rejected(lambda:apply(source,*args,ledger,wrong),4);checks+=1
            for path,value,code in [(('command','baseHash'),'sha256:'+'0'*64,4),(('command','cycleId'),'sha256:'+'0'*64,4),(('command','expectedPositionId'),'wrong',4),(('command','expectedPriorVersion'),0,6),(('command','capabilityId'),'sha256:'+'0'*64,5),(('command','kind'),'repeat',3)]:
                bad=changed_leaf(inp,path,value);rejected(lambda:apply(source,*args,ledger,bad),code);checks+=1
            ev=json.loads(t['events'][i]);bad=changed_leaf(ev,('stateVersion',),ev['stateVersion']+1);bad['receiptId']='armc.'+identity('receipt',{k:v for k,v in bad.items() if k!='receiptId'});carrier=json.loads(t['source']);carrier['lifecycleEventCanonicalUtf8'][i]=raw(bad,'Event').decode('ascii');rejected(lambda:replay(raw(carrier,'Source'),*args,t['ledger']),6);checks+=1
            duplicate=changed_leaf(inp,('actor',),'system' if inp['actor']!='system' else row['owner']);rejected(lambda:apply(t['source'],*args,t['ledger'],duplicate),4);checks+=1
        for ledger in [t['ledger'][:-1],t['ledger']+[t['ledger'][0]],list(reversed(t['ledger']))]:
            if ledger==t['ledger']:continue
            rejected(lambda:replay(t['source'],*args,ledger));checks+=1
        rejected(lambda:apply(t['source'],*args,t['ledger'],command(ctx,t['state'],'complete-movement-segment')),7);checks+=1
        if INVENTORY['closureFrozen']:
            claim=proof(t['source'],*args,t['ledger'])
            for path,value in [(('control','movementEnd','ordinal'),1),(('control','movementEnd','completionReceiptId'),'fake'),(('control','priorMovementEnd','ordinal'),2),(('control','nextCycleProgress'),[])]:
                if path[-1]=='nextCycleProgress' and n==0:continue
                bad=changed_leaf(claim,path,value);rejected(lambda:read_proof(raw(bad,'Proof'),t['source'],*args,t['ledger']),6);checks+=1
    print('PASS zero/positive/order/actor004/context/capability/causality/claim rejection',checks)
def grammar_tests():
    row,p,u=roots()[0];parent,args=root_cut(row,p,u,0);data=source_for(parent);source,ctx,state,_=replay(data,*args,[]);inp=command(ctx,state,'complete-movement-segment')
    for value,code in [(True,1),(-1,2),(1.5,1),(2**63,2)]:bad=changed_leaf(inp,('command','expectedPriorVersion'),value);rejected(lambda:apply(data,*args,[],bad),code)
    for value,code in [(True,1),(-1,2),(2,3)]:bad=changed_leaf(inp,('command','contractVersion'),value);rejected(lambda:raw(bad,'Input'),code)
    for kind,v in [('Source',source),('Control',state),('Input',inp)]:
        for field in v:bad=copy.deepcopy(v);bad.pop(field);rejected(lambda:parse(encode(bad),kind),1)
        good=raw(v,kind)
        for malformed in [good+b'\n',b' '+good,b'\xef\xbb\xbf'+good,good.replace(b'{',b'{"unknown":null,',1)]:rejected(lambda:parse(malformed,kind))
        rejected(lambda:parse(b' '*(byte_limit(kind)+1),kind),1)
    for malformed in [b'{"contractVersion":1,"contractVersion":1}',b'['*42+b'0'+b']'*42,b'['+b','.join([b'0']*513)+b']',b'{'+b','.join(('"k'+str(i)+'":0').encode() for i in range(129))+b'}']:rejected(lambda:parse(malformed,'Source'),1)
    bad=copy.deepcopy(state);bad['flow']=dict(kind='unknown');rejected(lambda:raw(bad,'Control'),3)
    broken=copy.deepcopy(source);broken['movementSourceCanonicalUtf8']='{}';rejected(lambda:read_control(b'{}',raw(broken,'Source'),*args,None),1)
    for which in range(5):
        allargs=copy.deepcopy(args);allargs[which]=None;rejected(lambda:read_control(b'{}',data,*allargs,None),1)
    print('PASS closed primitives/versions/arms/canonical/preparse/ancestor-before-sixth-claims')
def policy_tests():
    row,p,u=roots()[0];t=trace(row,p,u,0);ctx=t['ctx'];s=initial(ctx);old=copy.deepcopy(s['priorMovementEnd']);own=copy.deepcopy(s['members'][0]['unit']);old['excludedBefore']=[own];probe=copy.deepcopy(s);probe['priorMovementEnd']=old;out=expiry(ctx,probe,'isolated-union-probe');assert out['proof']['excludedBefore']==[own] and out['members']==s['members']
    overflow=copy.deepcopy(s);overflow['stateVersion']=2**63-1;inp=command(ctx,overflow,'complete-movement-segment');rejected(lambda:transition(ctx,overflow,inp),7)
    wrong=copy.deepcopy(s);wrong['activeCycle']['ordinal']=3;rejected(lambda:transition(ctx,wrong,command(ctx,wrong,'complete-movement-segment')),4)
    forged=json.loads(t['source']);parent=json.loads(forged['movementSourceCanonicalUtf8']);parent['movementEventCanonicalUtf8']=['{}'];forged['movementSourceCanonicalUtf8']=arm.raw(parent,'Source').decode('ascii');rejected(lambda:replay(raw(forged,'Source'),*t['args'],t['ledger']))
    print('PASS isolated historical exclusion union/overflow/actual causal source rejection; probes not authority')
def dependency_tests():
    row,p,u=roots()[0];parent,args=root_cut(row,p,u,0);data=source_for(parent);real=Path.read_bytes;checks=0
    for path in DEPENDENCIES:
        physical=(REPOSITORY/path).resolve()
        for outcome in ['changed','missing','unreadable','null']:
            def altered(p):
                if p.resolve()==physical:
                    if outcome=='missing':raise FileNotFoundError(path)
                    if outcome=='unreadable':raise PermissionError(path)
                    return None if outcome=='null' else b'changed'
                return real(p)
            try:Path.read_bytes=altered;rejected(lambda:replay(data,*args,[]),9);checks+=1
            finally:Path.read_bytes=real
        hits=[0]
        def drift(p):
            if p.resolve()==physical:
                hits[0]+=1
                if hits[0]>1:return b'changed-afterfirstread'
            return real(p)
        try:Path.read_bytes=drift;rejected(lambda:replay(data,*args,[]),9);checks+=1
        finally:Path.read_bytes=real
    for path in OWN_DELIVERY:
        def altered(p):return b'ownchanged' if p.resolve()==path.resolve() else real(p)
        try:Path.read_bytes=altered;rejected(lambda:replay(data,*args,[]),9);checks+=1
        finally:Path.read_bytes=real
    print('PASS external before/warm/missing/changed/unreadable/null/drift and own deliveries',len(DEPENDENCIES),checks)

def capacity_tests():
    model=dict(Source=2*arm.INVENTORY['encodedUpperBounds']['Source']+3*2*LIMITS['eventBytes']+512,Proof=arm.INVENTORY['encodedUpperBounds']['Proof']+arm.pac.paid.INVENTORY['encodedUpperBounds']['World']+131072,Control=arm.pac.paid.INVENTORY['encodedUpperBounds']['World']+131072,Event=16384,Input=1024,LifecycleLedger=3*1024+4)
    caps=dict(Source=LIMITS['sourceBytes'],Proof=LIMITS['proofBytes'],Control=LIMITS['controlBytes'],Event=LIMITS['eventBytes'],Input=LIMITS['inputBytes'],LifecycleLedger=LIMITS['lifecycleLedgerBytes'])
    for k,n in model.items():assert n<=caps[k],(k,n,caps[k])
    if INVENTORY['closureFrozen']:assert model==INVENTORY['encodedUpperBounds']
    row,p,u=roots()[0];t=trace(row,p,u,4);measurements={};fields={}
    def upper(v):
        if type(v) is dict:return {k:(None if k in ('world','movementProof') else upper(x)) for k,x in v.items()}
        if type(v) is list:return [upper(x) for x in v]
        if type(v) is str:return 'x'*128
        if type(v) is bool:return False
        if type(v) is int:return -2**63
        return v
    controls=[parse(x,'Control') for x in t['frames']];unit=controls[0]['members'][0]['unit']
    padded=[]
    for c in controls:
        c=copy.deepcopy(c);c['priorMovementEnd']['excludedBefore']=[copy.deepcopy(unit)]*2
        if c['movementEnd'] is not None:c['movementEnd']['excludedBefore']=[copy.deepcopy(unit)]*2
        padded.append(c)
    fields['ControlMetadata']=max(len(encode(upper(c))) for c in padded)
    prototype=dict(contractVersion=1,sourceId='armcomp.'+'0'*64,sourceHash='sha256:'+'0'*64,base=t['ctx']['base'],control=t['state']);fields['ProofMetadata']=max(len(encode(upper(prototype|dict(control=c)))) for c in padded)
    eventValues=[json.loads(x) for x in t['events']]
    for e in eventValues:
        if e['effect']['kind']=='movement-completed':e['effect']['excludedUnits']=[copy.deepcopy(unit)]*2
    fields['EventMetadata']=max(len(encode(upper(e))) for e in eventValues)
    inflated=copy.deepcopy(t['inputs'][-1]);inflated['command']['expectedPositionId']='x'*128;inflated['command']['expectedPriorVersion']=2**63-1;inflated['actor']='commonwealth';fields['InputStructural']=len(encode(inflated))
    assert fields['ControlMetadata']<=131072 and fields['ProofMetadata']<=131072 and fields['EventMetadata']<=16384 and fields['InputStructural']<=1024
    measurements=dict(Source=len(t['source']),Proof=len(raw(prototype,'Proof')),Control=max(len(x) for x in t['frames']),Event=max(len(x) for x in t['events']),Input=max(len(raw(x,'Input')) for x in t['inputs']),LifecycleLedger=len(encode(t['ledger'])))
    for k,n in measurements.items():assert n<=model[k],(k,n,model[k])
    for kind in ['Source','Proof','Control','Event','Input']:rejected(lambda:parse(b' '*(byte_limit(kind)+1),kind),1)
    assert LIMITS['depth']==arm.LIMITS['depth']+2 and LIMITS['acceptedEvents']==3
    print('PASS own field/carrier/wrapper/array/depth domination',model,fields,measurements);return dict(dominating=model,limits=caps,fieldAccounting=fields,measured=measurements)
def timestamp_tests():
    row,p,u=roots()[0];parent=arm.pac.trace(u,'repeat',2000,2001);four=list(parent['args'])+[parent['ledger']];base=arm.derive(parent['source'],*four);s=arm.initial(base);data=arm.raw(dict(contractVersion=1,continuationSourceCanonicalUtf8=parent['source'].decode('ascii'),movementEventCanonicalUtf8=[]),'Source');move=[]
    data,move,_=arm.apply(data,*four,move,arm.command(base,s,row['owner']+'-rear'));args=four+[move];source=source_for(data);_,ctx,s,_=replay(source,*args,[]);ledger=[]
    for kind in INVENTORY['commands']:source,ledger,_=apply(source,*args,ledger,command(ctx,s,kind));s=replay(source,*args,ledger)[2]
    assert s['closed'] and s['movementEnd']['ordinal']==2 and s['stateVersion']==41 and s['members'][0]['spentCp']==world.cp(9)
    print('PASS genuine inherited clock variant/native-free actual lifecycle ancestry')
def context_tests():
    row,p,u=roots()[0];t=trace(row,p,u,4);ctx=t['ctx'];pending=parse(t['frames'][1],'Control')
    bad=copy.deepcopy(pending);bad['interruptContext']['cycle']['ordinal']=3;rejected(lambda:transition(ctx,bad,command(ctx,bad,'resolve-breakdown-stop')),6)
    for path,value in [(('flow','stop','track','route',-1),'axis-front'),(('flow','stop','lastMoveReceiptId'),'fake'),(('flow','stop','recordedStateVersion'),0)]:
        bad=changed_leaf(pending,path,value);rejected(lambda:transition(ctx,bad,command(ctx,bad,'resolve-breakdown-stop')),4)
    # Exact own-source events remain independently caused; a re-signed effect cannot assert an arbitrary context.
    carrier=json.loads(t['source']);event=json.loads(carrier['lifecycleEventCanonicalUtf8'][0]);event['effect']['interruptContext']['cycle']['ordinal']=3;event['receiptId']='armc.'+identity('receipt',{k:v for k,v in event.items() if k!='receiptId'});carrier['lifecycleEventCanonicalUtf8'][0]=raw(event,'Event').decode('ascii');rejected(lambda:replay(raw(carrier,'Source'),*t['args'],t['ledger']),6)
    print('PASS captured context/track/lastreceipt/recordedversion and coherent own-event forgery')

def fixture_artifacts():
    traces=[]
    for row,p,u,n in selected():
        t=trace(row,p,u,n);claim=proof(t['source'],*t['args'],t['ledger']);traces.append(dict(owner=row['owner'],firstSeal=row['firstSeal'],moves=n,sourceCanonicalUtf8=t['source'].decode('ascii'),lifecycleLedger=t['ledger'],controlCanonicalUtf8=[x.decode('ascii') for x in t['frames']],proofCanonicalUtf8=raw(claim,'Proof').decode('ascii'),sourceHash=sha(t['source']),proofHash=sha(raw(claim,'Proof')),eventHashes=[sha(x) for x in t['events']]))
    return dict(contractVersion=1,dependencyManifestHash='sha256:'+identity('dependency-manifest',dict(sorted(DEPENDENCIES.items()))),traces=traces)
FIXTURE=ROOT/'fixtures/combat-actual-repeated-movement-completion-v1.json'
def fixture_tests():assert json.loads(test_bytes('docs/specs/fixtures/combat-actual-repeated-movement-completion-v1.json'))==fixture_artifacts();print('PASS retained20traces/52events/72frames literals; no regeneration')
def normal_gate():
    semantic_tests();grammar_tests();policy_tests();timestamp_tests();context_tests();capacity_tests();dependency_tests()
    for owner in ['axis','commonwealth']:
        for seal in ['attacker','defender']:preservation_tests(owner,seal);recovery_tests(owner,seal);adversarial_tests(owner,seal,0);adversarial_tests(owner,seal,4)
    fixture_tests();print('PASS private actual completion contract; no downstream/native/public/parent execution')
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--group',choices=['semantic','preservation','recovery','adversarial','grammar','policy','dependencies','fixture','capacity','timestamp','context']);parser.add_argument('--owner',choices=['axis','commonwealth']);parser.add_argument('--seal',choices=['attacker','defender']);parser.add_argument('--moves',type=int,choices=range(5));parser.add_argument('--write-fixture',action='store_true');args=parser.parse_args()
    if args.write_fixture:require(INVENTORY['closureFrozen'],7,'/provisionalClosure');FIXTURE.write_text(json.dumps(fixture_artifacts(),indent=2)+'\n')
    elif args.group=='semantic':semantic_tests()
    elif args.group=='policy':policy_tests()
    elif args.group=='dependencies':dependency_tests()
    elif args.group in ['grammar','fixture','capacity','timestamp','context']:globals()[args.group+'_tests']()
    elif args.group:globals()[args.group+'_tests'](args.owner,args.seal,args.moves)
    else:normal_gate()
if __name__=='__main__':main()
