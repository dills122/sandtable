#!/usr/bin/env python3
"""Private authentic repeated ordinary Movement executable reference."""
import argparse,copy,hashlib,importlib.util,json,sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent
REPOSITORY=ROOT.parent.parent
INVENTORY=json.loads((ROOT/'combat-actual-repeated-movement-v1.schema.json').read_bytes())
SCHEMA={k:[tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['objects'].items()}
LIMITS=INVENTORY['limits'];DEPENDENCIES=dict(INVENTORY['runtimeSourceHashes'])
def encode(v):return json.dumps(v,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(data):return 'sha256:'+hashlib.sha256(data).hexdigest()
PACKAGED_MANIFEST=encode(dict(sorted(DEPENDENCIES.items())))
class Invalid(ValueError):
    def __init__(self,code,path=''):self.code=f'CMB-RMV-{code:03}';self.path=path;super().__init__(self.code+' '+(path or '/'))
def require(ok,code,path=''):
    if not ok:raise Invalid(code,path)
def consume(path):
    require(path in DEPENDENCIES,9,path)
    try:data=(REPOSITORY/path).read_bytes()
    except OSError as error:raise Invalid(9,path) from error
    require(type(data) is bytes and hashlib.sha256(data).hexdigest()==DEPENDENCIES[path],9,path);return data
OWN_DELIVERY={p:sha(p.read_bytes()) for p in (ROOT/'combat-actual-repeated-movement-v1.schema.json',Path(__file__).resolve())}
def verify_dependencies():
    require(encode(dict(sorted(DEPENDENCIES.items())))==PACKAGED_MANIFEST,9,'/packagedManifest')
    for path,value in DEPENDENCIES.items():
        require(type(path) is str and path.isascii() and not path.startswith('/') and '\\' not in path and all(p not in ('','.','..') for p in path.split('/')) and type(value) is str and len(value)==64 and all(c in '0123456789abcdef' for c in value),9,'/manifest');consume(path)
    for path,expected in OWN_DELIVERY.items():
        try:data=path.read_bytes()
        except OSError as error:raise Invalid(9,'/ownDelivery') from error
        require(type(data) is bytes and sha(data)==expected,9,'/ownDelivery')
verify_dependencies()
spec=importlib.util.spec_from_file_location('actual_pac_predecessor',ROOT/'verify-combat-actual-post-assault-continuation-v1.py');pac=importlib.util.module_from_spec(spec);spec.loader.exec_module(pac)
policy,mov,world,rel,steps=pac.policy,pac.mov,pac.world,pac.rel,pac.steps
verify_dependencies()
def inherited(call):
    try:return call()
    except (pac.Invalid,pac.paid.Invalid,mov.Invalid,policy.Invalid) as error:raise Invalid(int(error.code[-3:]),error.path if hasattr(error,'path') else '') from error
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
        if kind=='Effect':require(v['kind']=='ordinary-move',3,path+'/kind')
        if kind in INVENTORY['versionRules']:require(v['contractVersion']==INVENTORY['versionRules'][kind],3,path)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v)<=INVENTORY['arrayBounds'].get(parent+'.'+field,LIMITS['arrayItems']),1,path)
        for i,x in enumerate(v):typed(x,kind[:-2],path+'/'+str(i),depth+1)
    elif kind in ('carrier','eventCarrier'):
        require(type(v) is str and v.isascii() and 0<len(v)<=(pac.LIMITS['sourceBytes'] if kind=='carrier' else LIMITS['eventBytes']),1,path)
    else:inherited(lambda:pac.typed(v,INVENTORY['borrowedTypes'].get(kind,kind),path,0))
def canonical(v,kind):
    if kind.endswith('?'):return None if v is None else canonical(v,kind[:-1])
    if kind in SCHEMA:return {k:canonical(v[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'):return [canonical(x,kind[:-2]) for x in v]
    if kind in ('carrier','eventCarrier'):return v
    return pac.canonical(v,INVENTORY['borrowedTypes'].get(kind,kind))
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

def derive(data,selection,rounds,paid,continuation):
    verify_dependencies()
    source,prior_ctx,s,_=inherited(lambda:pac.replay(data,selection,rounds,paid,continuation))
    c=s['activeCycle'];w=s['world'];m=s['members'];end=s['movementEnd'];original=s['originalCycle']
    require(s['status']=='repeated' and s['closed'] and s['stateVersion']==37 and c is not None and s['closure'] is not None and s['closure']['outcome']=='repeated',3,'/repeatProfile')
    require((c['gameTurn'],c['operationStage'],c['playerPhaseSlot'],c['ordinal'])==(1,1,'first-acting-side',2) and original['ordinal']==1,3,'/cycle')
    require(c['openedAuthorityVersion']==37 and s['closure']['ordinal']==1 and s['closure']['receiptId']==s['receipts'][-1]['receiptId'] and s['positionId']==policy.edge(c)['movementPositionId'],4,'/repeatBinding')
    require(not s['targetUses'] and not s['nextCycleProgress'] and len(m)==1 and len(end['endLocations'])==2 and len(end['excludedBefore'])<=2 and end['ordinal']==1 and end==prior_ctx['movementEnd'],4,'/originalMovementProof')
    require(s['randomState']['seed']==1 and s['randomState']['nextByteCursor']==10 and len(s['attackHistory'])==1 and len(w['settlements'])==1 and len(w['relationships'])==1 and len(w['cohesionCauses'])==0,3,'/profile')
    require(not any(w[k] for k in ('guards','custodyLots','replacementEntitlements','brokenVehicleLots')),3,'/pendingWork')
    member=m[0];u=member['unit'];e=next(e for e in w['elements'] if world.unit(e,w['creationBinding'])==u);op=e['operationalState']
    require(u['originalSide']==c['actingSide'] and member['status']==e['reserveStatus']=='none' and member['baseCpa']==10 and member['spentCp']==op['capabilityPointsExpended']==dict(numerator=5,denominator=1),3,'/ordinaryMember')
    require(all(member['history'][k] is None for k in member['history'] if k!='scope') and member['history']['scope']==rel.scope(c),3,'/history')
    require(op['cohesionLevel']==0 and op['vehicleBreakdownState'] is None and op['movementEnded'] is None and e['ammunition']['points']==0 and e['components'][0]['currentToe']==10,3,'/ordinaryProfile')
    require(all(x[k] is not None for x in w['settlements'] for k in ('disposition','losses','retreat','relationships')),3,'/settlement')
    h=pac.identity('source',pac.canonical(source,'Source'));proof=dict(contractVersion=1,sourceId='apcont.'+h,sourceHash='sha256:'+h,base=copy.deepcopy(prior_ctx['base']),control=copy.deepcopy(s))
    inherited(lambda:pac.raw(proof,'Proof'))
    basis=dict(contractVersion=1,dependencyManifestHash='sha256:'+identity('dependency-manifest',dict(sorted(DEPENDENCIES.items()))),continuationProof=proof);raw(basis,'Base')
    return dict(base=basis,baseHash='sha256:'+identity('base',canonical(basis,'Base')),prior=copy.deepcopy(s),cycle=copy.deepcopy(c),firstActingSide=prior_ctx['boundary']['firstActingSide'])
def initial(ctx):
    p=ctx['prior']
    return dict(contractVersion=1,baseHash=ctx['baseHash'],continuationSourceHash=ctx['base']['continuationProof']['sourceHash'],stateVersion=p['stateVersion'],prefix=p['prefix'],positionId=p['positionId'],originalCycle=copy.deepcopy(p['originalCycle']),activeCycle=copy.deepcopy(p['activeCycle']),closure=copy.deepcopy(p['closure']),movementEnd=copy.deepcopy(p['movementEnd']),members=copy.deepcopy(p['members']),world=copy.deepcopy(p['world']),randomState=copy.deepcopy(p['randomState']),attackHistory=copy.deepcopy(p['attackHistory']),targetUses=[],nextCycleProgress=[],tracks=[],receipts=[])
def command(ctx,s,dest):
    u=s['members'][0]['unit'];e=next(e for e in s['world']['elements'] if e['elementId']==u['elementId'])
    return dict(command=dict(contractVersion=1,kind='move',baseHash=ctx['baseHash'],cycleId=policy.cycle_id(ctx['cycle'],ctx['firstActingSide']),positionId=s['positionId'],expectedPriorVersion=s['stateVersion'],unit=copy.deepcopy(u),originLocationId=e['currentLocationId'],destinationLocationId=dest),actor=ctx['cycle']['actingSide'])
def transition(ctx,prior,inp):
    raw(prior,'Control');raw(inp,'Input');cmd=inp['command'];actor=inp['actor'];u=cmd['unit'];c=ctx['cycle'];w=prior['world']
    require(cmd['kind']=='move',3,'/kind')
    require(cmd['baseHash']==prior['baseHash']==ctx['baseHash'] and cmd['cycleId']==policy.cycle_id(c,ctx['firstActingSide']) and cmd['positionId']==prior['positionId']==ctx['prior']['positionId'] and prior['activeCycle']==c,4,'/context')
    require(actor==c['actingSide']==u['originalSide'] and u['creationBinding']==w['creationBinding'] and u==ctx['prior']['members'][0]['unit'],4,'/actorOrUnit')
    ch=sha(raw(cmd,'Command'));accepted=next((r for r in prior['receipts'] if r['commandHash']==ch and r['actor']==actor),None)
    if accepted:return copy.deepcopy(prior),None,accepted['receiptId']
    require(prior['stateVersion']<2**63-1,7,'/versionCapacity');require(cmd['expectedPriorVersion']==prior['stateVersion'],6,'/priorVersion')
    e=next(x for x in w['elements'] if world.unit(x,w['creationBinding'])==u);op=e['operationalState'];origin=cmd['originLocationId'];dest=cmd['destinationLocationId'];member=prior['members'][0]
    require(e['currentLocationId']==origin and origin in world.GRAPH and dest in world.GRAPH[origin],5,'/topology')
    # Reaction discovery is required even for ammunition-empty opposing elements.
    require(not any(r['currentLocationId'] in world.GRAPH[dest] for r in w['representations'] if any(world.SIDES[x]!=actor for x in r['boundElementIds'])),3,'/reactionBoundary')
    require(u not in policy.excluded(prior['movementEnd'],actor),5,'/originalEligibility')
    require(all(x['currentLocationId']!=dest for x in w['elements'] if x is not e) and all(x['currentLocationId']!=dest for x in w['guards']),5,'/occupancy')
    require(member['status']==e['reserveStatus']=='none' and member['baseCpa']==10 and member['history']==ctx['prior']['members'][0]['history'] and member['spentCp']==op['capabilityPointsExpended'] and member['spentCp']['denominator']==1,3,'/ordinaryMember')
    facts=next(x for x in world.PACK['elements'] if x['elementId']==e['elementId'])
    require(facts['baseCapabilityPointAllowance']==10 and facts['breakdownVehicleCohort'] is None and op['vehicleBreakdownState'] is None and op['movementEnded'] is None and e['ammunition']['points']==0 and e['components'][0]['currentToe']==10,3,'/ordinaryResources')
    require((op['ledgerGameTurn'],op['ledgerOperationStage'])==(c['gameTurn'],c['operationStage']),4,'/stage')
    reps=[r for r in w['representations'] if r['bindingKind']=='independent-element' and r['boundElementIds']==[e['elementId']]];require(len(reps)==1 and reps[0]['currentLocationId']==origin,4,'/representation')
    require(all((r['gameTurn'],r['operationStage'])==(c['gameTurn'],c['operationStage']) for r in w['relationships'] if r['active']),4,'/relationshipScope')
    ends=inherited(lambda:mov.affected(w['relationships'],u));terrain=inherited(lambda:mov.terrain_cost(origin,dest));spent=member['spentCp']['numerator'];after,dp=inherited(lambda:mov.cost(10,spent,[r['kind'] for r in ends],terrain=terrain));cohesion=op['cohesionLevel']
    require(cohesion-dp>=-2**31,7,'/cohesion');require(len(prior['receipts'])<LIMITS['acceptedEvents'],7,'/eventCapacity')
    costs=dict(terrainCost=terrain,breakOffCost=after-spent-terrain,totalCost=after-spent,baseCpa=10,voluntaryCeiling=15,beforeCp=spent,afterCp=after,excessCpDp=dp,beforeCohesion=cohesion,afterCohesion=cohesion-dp)
    effect=dict(kind='ordinary-move',unit=copy.deepcopy(u),representationId=reps[0]['representationId'],originLocationId=origin,destinationLocationId=dest,costs=costs,endedMemberships=ends)
    event=dict(contractVersion=1,eventType='combat-actual-repeated-element-moved',author=actor,campaignId=c['campaignId'],rulesetHash=c['rulesetHash'],configurationHash=c['admittedPolicyBundleDigest'],continuationSourceHash=prior['continuationSourceHash'],baseHash=ctx['baseHash'],cycleId=cmd['cycleId'],positionId=cmd['positionId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],input=canonical(inp,'Input'),effect=canonical(effect,'Effect'))
    rid='armv.'+identity('receipt',event);event['receiptId']=rid;data=raw(event,'Event');require(len(rid+'.dp')<=128,7,'/causeId')
    state=copy.deepcopy(prior);out=state['world'];moved=next(x for x in out['elements'] if x['elementId']==e['elementId']);moved['currentLocationId']=dest;moved['operationalState'].update(capabilityPointsExpended=world.cp(after),cohesionLevel=cohesion-dp)
    state['members'][0]['spentCp']=world.cp(after);next(r for r in out['representations'] if r['representationId']==reps[0]['representationId'])['currentLocationId']=dest
    for r in out['relationships']:
        if r['relationId'] in {end['relationId'] for end in ends}:r.update(active=False,endedByReceiptId=rid,endingCause='ordinary-break-off')
    if dp:
        require(len(out['cohesionCauses'])<3,7,'/causeCapacity');out['cohesionCauses'].append(dict(causeId=rid+'.dp',ordinal=len(out['cohesionCauses'])+1,receiptId=rid,elementId=e['elementId'],gameTurn=c['gameTurn'],operationStage=c['operationStage'],kind='ordinary-movement-excess-cp-dp',points=dp,before=cohesion,after=cohesion-dp))
    if state['tracks']:require(state['tracks'][0]['route'][-1]==origin,6,'/route');state['tracks'][0]['route'].append(dest)
    else:state['tracks']=[dict(unit=copy.deepcopy(u),route=[origin,dest])]
    state['stateVersion']=event['stateVersion'];state['prefix']=steps.seq.prefix_event(prior['prefix'],data);state['receipts'].append(dict(commandHash=ch,eventHash=sha(data),receiptId=rid,actor=actor,stateVersion=state['stateVersion']));state['nextCycleProgress'].append(dict(eventType=event['eventType'],receiptId=rid,eventHash=sha(data)))
    raw(state,'Control');return state,data,rid
def replay(data,selection,rounds,paid,continuation,movement):
    verify_dependencies();source=parse(data,'Source');ctx=derive(source['continuationSourceCanonicalUtf8'].encode('ascii'),selection,rounds,paid,continuation)
    require(type(movement) is list and len(movement)<=4,1,'/movementLedger');owned=[parse(raw(x,'Input'),'Input') for x in movement];require(len(encode(owned))<=LIMITS['movementLedgerBytes'],1,'/movementLedger');require(len(owned)==len(source['movementEventCanonicalUtf8']),4,'/movementLedger')
    state=initial(ctx)
    for n,(text,inp) in enumerate(zip(source['movementEventCanonicalUtf8'],owned)):
        event=text.encode('ascii');parse(event,'Event');state,expected,_=transition(ctx,state,inp);require(expected is not None and event==expected,6,'/event/'+str(n))
    verify_dependencies();return source,ctx,state,owned
def apply(data,selection,rounds,paid,continuation,movement,new_input):
    source,ctx,s,owned=replay(data,selection,rounds,paid,continuation,movement);after,event,rid=transition(ctx,s,new_input)
    if event is None:verify_dependencies();return raw(source,'Source'),copy.deepcopy(owned),rid
    source['movementEventCanonicalUtf8'].append(event.decode('ascii'));ledger=owned+[parse(raw(new_input,'Input'),'Input')];require(len(encode(ledger))<=LIMITS['movementLedgerBytes'],1,'/movementLedger');result=raw(source,'Source');verify_dependencies();return result,ledger,rid
def read_source(data,*ledgers):
    out=copy.deepcopy(replay(data,*ledgers)[0]);require(INVENTORY['closureFrozen'],7,'/provisionalClosure');return out
def proof(data,*ledgers):
    source,ctx,s,_=replay(data,*ledgers);require(INVENTORY['closureFrozen'],7,'/provisionalClosure');h=identity('source',canonical(source,'Source'));out=dict(contractVersion=1,sourceId='armove.'+h,sourceHash='sha256:'+h,base=ctx['base'],control=s);return parse(raw(out,'Proof'),'Proof')
def read_proof(claim,data,*ledgers):
    expected=proof(data,*ledgers);value=parse(claim,'Proof');require(claim==raw(expected,'Proof'),6,'/proof');return value
def read_control(claim,data,*ledgers):
    state=replay(data,*ledgers)[2];require(INVENTORY['closureFrozen'],7,'/provisionalClosure');value=parse(claim,'Control');require(claim==raw(state,'Control'),6,'/control');return value

def semantic_tests():
    failures=[]
    for row,upstream in roots():
        args=[upstream['selectionLedger'],upstream['roundLedger'],upstream['paidLedger'],row['continuationLedger']]
        prior_source,ctx,terminal,_=pac.replay(row['sourceCanonicalUtf8'].encode('ascii'),*args)
        assert terminal['stateVersion']==37 and terminal['status']=='repeated' and terminal['activeCycle']['ordinal']==2
        assert terminal['members'][0]['spentCp']==dict(numerator=5,denominator=1) and not terminal['nextCycleProgress'] and not terminal['targetUses']
        assert terminal['randomState']['seed']==1 and terminal['randomState']['nextByteCursor']==10 and terminal['movementEnd']['completionReceiptId'].startswith('iml.')
        pins=pac.DEPENDENCIES|{str(p.relative_to(ROOT.parent.parent)):hashlib.sha256(p.read_bytes()).hexdigest() for p in pac.OWN_DELIVERY}
        h=pac.identity('source',pac.canonical(prior_source,'Source'));prior_proof=dict(contractVersion=1,sourceId='apcont.'+h,sourceHash='sha256:'+h,base=ctx['base'],control=terminal)
        manifest='sha256:'+pac.digest('sandtable.combat.actual-repeated-movement.dependency-manifest.v1',dict(sorted(pins.items())))
        basis=dict(contractVersion=1,dependencyManifestHash=manifest,continuationProof=prior_proof)
        base_hash='sha256:'+pac.digest('sandtable.combat.actual-repeated-movement.base.v1',basis)
        source=encode(dict(contractVersion=1,continuationSourceCanonicalUtf8=row['sourceCanonicalUtf8'],movementEventCanonicalUtf8=[]))
        inp=dict(command=dict(contractVersion=1,kind='move',baseHash=base_hash,cycleId=pac.policy.cycle_id(terminal['activeCycle'],ctx['boundary']['firstActingSide']),positionId=terminal['positionId'],expectedPriorVersion=37,unit=copy.deepcopy(terminal['members'][0]['unit']),originLocationId=next(e['currentLocationId'] for e in terminal['world']['elements'] if e['elementId']==terminal['members'][0]['unit']['elementId']),destinationLocationId=row['owner']+'-rear'),actor=row['owner'])
        try:
            successor,ledger,receipt=apply(source,*args,[],inp);event=json.loads(json.loads(successor)['movementEventCanonicalUtf8'][0]);assert event['stateVersion']==38 and receipt==event['receiptId'] and len(ledger)==1
            assert event['effect']['costs']['afterCp']==9 and event['effect']['costs']['excessCpDp']==0 and len(event['effect']['endedMemberships'])==1
            print('PASS actual move publication',row['owner'],row['firstSeal'])
        except Invalid as error:
            failures.append((row['owner'],row['firstSeal']));print('FAIL actual move38/rear/CP9/member/progress/event/receipt expected; actual',error.code,row['owner'],row['firstSeal'])
    assert not failures,str(len(failures))+' genuine move publications failed'

def test_bytes(path):
    require(path in INVENTORY['testArtifactHashes'],9,'/testArtifact')
    data=(REPOSITORY/path).read_bytes();require(hashlib.sha256(data).hexdigest()==INVENTORY['testArtifactHashes'][path],9,path);return data
def roots():
    rows=json.loads(test_bytes('docs/specs/fixtures/combat-actual-post-assault-continuation-v1.json'))['traces'];paid=json.loads(test_bytes('docs/specs/fixtures/combat-actual-paid-assault-v1.json'))['cases']
    return [(r,next(p for p in paid if (p['owner'],p['firstSeal'])==(r['owner'],r['firstSeal']))) for r in rows if r['outcome']=='repeat']
def selected(owner=None,seal=None):return [(r,p) for r,p in roots() if (owner is None or r['owner']==owner) and (seal is None or r['firstSeal']==seal)]
def arguments(row,upstream):return [upstream['selectionLedger'],upstream['roundLedger'],upstream['paidLedger'],row['continuationLedger']]
def source_for(row):return raw(dict(contractVersion=1,continuationSourceCanonicalUtf8=row['sourceCanonicalUtf8'],movementEventCanonicalUtf8=[]),'Source')
TRACE_CACHE={}
def trace(row,upstream):
    key=(row['owner'],row['firstSeal'])
    if key in TRACE_CACHE:return copy.deepcopy(TRACE_CACHE[key])
    args=arguments(row,upstream);source=source_for(row);ledger=[];ctx=derive(row['sourceCanonicalUtf8'].encode('ascii'),*args);state=initial(ctx);frames=[raw(state,'Control')];sources=[source];events=[];inputs=[]
    for destination in [row['owner']+'-'+x for x in ['rear','supply','rear','supply']]:
        inp=command(ctx,state,destination);source,ledger,rid=apply(source,*args,ledger,inp);state=replay(source,*args,ledger)[2];event=json.loads(source)['movementEventCanonicalUtf8'][-1].encode('ascii');assert rid==json.loads(event)['receiptId'];inputs.append(inp);events.append(event);frames.append(raw(state,'Control'));sources.append(source)
    out=dict(source=source,ledger=ledger,args=args,ctx=ctx,state=state,frames=frames,sources=sources,inputs=inputs,events=events);TRACE_CACHE[key]=copy.deepcopy(out);return out
def rejected(call,code=None):
    try:call()
    except Invalid as error:
        if code is not None:assert error.code==f'CMB-RMV-{code:03}',(error.code,error.path,code)
        return error
    raise AssertionError('invalid input accepted')
def preservation_tests(owner=None,seal=None):
    for row,upstream in selected(owner,seal):
        t=trace(row,upstream);prior=t['ctx']['prior'];before=initial(t['ctx']);eid=before['members'][0]['unit']['elementId'];prefix=prior['prefix']
        for n,frame in enumerate(t['frames']):
            state=parse(frame,'Control');w=state['world'];e=next(x for x in w['elements'] if x['elementId']==eid);assert state['stateVersion']==37+n
            assert state['members'][0]['spentCp']==e['operationalState']['capabilityPointsExpended']==world.cp([5,9,11,13,15][n]);assert e['operationalState']['cohesionLevel']==[0,0,-1,-3,-5][n]
            for k in ('movementEnd','originalCycle','activeCycle','closure','randomState','attackHistory'):assert state[k]==prior[k],k
            assert state['movementEnd']['ordinal']==1 and state['activeCycle']['ordinal']==2 and not state['targetUses'] and state['members'][0]['history']==prior['members'][0]['history']
            assert len(state['receipts'])==len(state['nextCycleProgress'])==n and len(w['cohesionCauses'])==max(0,n-1)
            assert state['nextCycleProgress']==[dict(eventType=json.loads(text)['eventType'],receiptId=json.loads(text)['receiptId'],eventHash=sha(text)) for text in t['events'][:n]]
            for k in w:
                if k not in ('elements','representations','relationships','cohesionCauses'):assert w[k]==before['world'][k],k
            for element in w['elements']:
                original=next(x for x in before['world']['elements'] if x['elementId']==element['elementId']);restored=copy.deepcopy(element)
                if element['elementId']==eid:
                    restored['currentLocationId']=original['currentLocationId']
                    for k in ('capabilityPointsExpended','cohesionLevel'):restored['operationalState'][k]=original['operationalState'][k]
                assert restored==original
            for rep in w['representations']:
                original=next(x for x in before['world']['representations'] if x['representationId']==rep['representationId']);restored=copy.deepcopy(rep)
                if rep['boundElementIds']==[eid]:assert rep['currentLocationId']==e['currentLocationId'];restored['currentLocationId']=original['currentLocationId']
                assert restored==original
            if n:
                assert state['tracks']==[dict(unit=state['members'][0]['unit'],route=[t['inputs'][0]['command']['originLocationId']]+[row['owner']+'-'+x for x in ['rear','supply','rear','supply'][:n]])]
                relation=w['relationships'][0];old=before['world']['relationships'][0];assert not relation['active'] and relation['endedByReceiptId']==json.loads(t['events'][0])['receiptId'] and relation['endingCause']=='ordinary-break-off';restored=copy.deepcopy(relation);restored.update({k:old[k] for k in ('active','endedByReceiptId','endingCause')});assert restored==old
            else:assert not state['tracks'] and w['relationships']==before['world']['relationships']
        for n,text in enumerate(t['events']):
            event=parse(text,'Event');cost=event['effect']['costs'];assert (cost['afterCp'],cost['excessCpDp'],cost['breakOffCost'],len(event['effect']['endedMemberships']))==([9,11,13,15][n],[0,1,2,2][n],[2,0,0,0][n],int(n==0));assert event['priorPrefix']==prefix and event['receiptId']=='armv.'+identity('receipt',{k:v for k,v in event.items() if k!='receiptId'});prefix=steps.seq.prefix_event(prefix,text)
        assert t['state']['prefix']==prefix
    print('PASS genuine CP/DP/Contact/member/progress/preservation',owner,seal)
def recovery_tests(owner=None,seal=None):
    cuts=retries=0
    for row,upstream in selected(owner,seal):
        t=trace(row,upstream)
        for n in range(5):
            data=t['sources'][n];ledger=t['ledger'][:n];args=t['args']+[ledger];source,ctx,state,owned=replay(data,*args);assert raw(state,'Control')==t['frames'][n];cuts+=1
            for j in range(n):
                unchanged,retained,rid=apply(data,*args,t['inputs'][j]);assert unchanged==data and retained==ledger and rid==json.loads(t['events'][j])['receiptId'];retries+=1
            if INVENTORY['closureFrozen']:
                claim=proof(data,*args);assert read_proof(raw(claim,'Proof'),data,*args)==claim and read_control(t['frames'][n],data,*args)==state
                claim['control']['world']['elements'].clear();state['world']['elements'].clear();owned.clear();source['movementEventCanonicalUtf8'].clear();assert raw(proof(data,*args)['control'],'Control')==t['frames'][n]
        enabled=pac.paid.FRESH_ENABLED
        try:pac.paid.FRESH_ENABLED=False;assert replay(t['source'],*t['args'],t['ledger'])[2]['stateVersion']==41
        finally:pac.paid.FRESH_ENABLED=enabled
    print('PASS every cut/every prior command retry/owned readback',cuts,retries)
def changed_leaf(value,path,new):
    out=copy.deepcopy(value);parent=out
    for p in path[:-1]:parent=parent[p]
    parent[path[-1]]=new;return out
def adversarial_tests(owner=None,seal=None):
    count=0
    for row,upstream in selected(owner or 'axis',seal or 'attacker'):
        t=trace(row,upstream);ctx=t['ctx'];args=t['args'];before=initial(ctx);base=source_for(row);inp=command(ctx,before,row['owner']+'-rear');snapshot=raw(before,'Control')
        for path,value,code in [(('actor',),'commonwealth' if row['owner']=='axis' else 'axis',4),(('command','baseHash'),'sha256:'+'0'*64,4),(('command','cycleId'),'sha256:'+'0'*64,4),(('command','positionId'),'wrong',4),(('command','expectedPriorVersion'),36,6),(('command','unit','creationBinding'),'sha256:'+'0'*64,4),(('command','destinationLocationId'),row['owner']+'-supply',5),(('command','kind'),'stop',3)]:
            bad=changed_leaf(inp,path,value);rejected(lambda:apply(base,*args,[],bad),code);assert raw(before,'Control')==snapshot;count+=1
        rear=parse(t['frames'][1],'Control');rejected(lambda:apply(t['sources'][1],*args,t['ledger'][:1],command(ctx,rear,t['inputs'][0]['command']['originLocationId'])),3);assert raw(rear,'Control')==t['frames'][1];count+=1
        rejected(lambda:apply(t['source'],*args,t['ledger'],command(ctx,t['state'],row['owner']+'-rear')),5);assert raw(t['state'],'Control')==t['frames'][-1];count+=1
        for n in range(4):
            event=json.loads(t['events'][n])
            for path,value in [(('effect','costs','afterCp'),event['effect']['costs']['afterCp']-1),(('effect','costs','excessCpDp'),event['effect']['costs']['excessCpDp']+1),(('effect','costs','afterCohesion'),event['effect']['costs']['afterCohesion']-1),(('priorPrefix',),'sha256:'+'0'*64),(('stateVersion',),event['stateVersion']+1),(('effect','destinationLocationId'),'assault')]:
                forged=changed_leaf(event,path,value);forged['receiptId']='armv.'+identity('receipt',{k:v for k,v in forged.items() if k!='receiptId'});source=json.loads(t['source']);source['movementEventCanonicalUtf8'][n]=raw(forged,'Event').decode('ascii');rejected(lambda:replay(raw(source,'Source'),*args,t['ledger']),6);count+=1
        for j in range(4):
            for path,value,code in [(('actor',),'commonwealth' if row['owner']=='axis' else 'axis',4),(('command','baseHash'),'sha256:'+'0'*64,4),(('command','expectedPriorVersion'),True,1),(('command','kind'),'stop',3)]:
                duplicate=changed_leaf(t['inputs'][j],path,value);rejected(lambda:apply(t['source'],*args,t['ledger'],duplicate),code);count+=1
        for which in range(4):
            bad=copy.deepcopy(args);bad[which][0]['actor']='commonwealth' if bad[which][0]['actor']!='commonwealth' else 'axis';rejected(lambda:replay(t['source'],*bad,t['ledger']));count+=1
        for bad in [t['ledger'][:-1],t['ledger']+[t['ledger'][0]],list(reversed(t['ledger']))]:rejected(lambda:replay(t['source'],*args,bad));count+=1
        broken=json.loads(base);broken['continuationSourceCanonicalUtf8']='{}';rejected(lambda:read_control(b'{}',raw(broken,'Source'),*args,None),1);count+=1
        if INVENTORY['closureFrozen']:
            claim=proof(t['source'],*args,t['ledger'])
            for path,value in [(('control','nextCycleProgress'),[]),(('control','movementEnd','ordinal'),2),(('control','members',0,'spentCp','numerator'),5)]:bad=changed_leaf(claim,path,value);rejected(lambda:read_proof(raw(bad,'Proof'),t['source'],*args,t['ledger']),6);count+=1
        blocked=copy.deepcopy(before);blocked['movementEnd']['excludedBefore']=[blocked['members'][0]['unit']];rejected(lambda:transition(ctx,blocked,inp),5);count+=1
    print('PASS causal forgeries/context/cost/Reaction/fifth/source-first',count)
def capacity_tests():
    model={'Source':2*pac.INVENTORY['encodedUpperBounds']['Source']+4*2*LIMITS['eventBytes']+512,'Control':pac.paid.INVENTORY['encodedUpperBounds']['World']+65536,'Proof':pac.INVENTORY['encodedUpperBounds']['Proof']+pac.paid.INVENTORY['encodedUpperBounds']['World']+131072,'Event':16384,'Input':1024,'MovementLedger':4*1024+5}
    caps={'Source':LIMITS['sourceBytes'],'Control':LIMITS['controlBytes'],'Proof':LIMITS['proofBytes'],'Event':LIMITS['eventBytes'],'Input':LIMITS['inputBytes'],'MovementLedger':LIMITS['movementLedgerBytes']}
    for k,n in model.items():assert n<=caps[k],(k,n,caps[k])
    row,upstream=roots()[0];t=trace(row,upstream)
    def upper(v):
        if type(v) is dict:return {k:(None if k in ('world','continuationProof') else upper(x)) for k,x in v.items()}
        if type(v) is list:return [upper(x) for x in v]
        if type(v) is str:return 'x'*128
        if type(v) is bool:return False
        if type(v) is int:return -2**63
        return v
    first=json.loads(t['events'][0]);last=json.loads(t['events'][-1]);terminal=copy.deepcopy(t['state']);terminal['movementEnd']['excludedBefore']=[copy.deepcopy(terminal['members'][0]['unit'])]*2;p=dict(contractVersion=1,sourceId='armove.'+'0'*64,sourceHash='sha256:'+'0'*64,base=t['ctx']['base'],control=terminal)
    fields={'ControlMetadata':len(encode(upper(terminal))),'ProofMetadata':len(encode(upper(p))),'Event':max(len(encode(upper(first))),len(encode(upper(last))))}
    assert fields['ControlMetadata']<=65536 and fields['ProofMetadata']<=131072 and fields['Event']<=16384
    # Exact authenticated Command uses frozen position/topology/unit labels; arbitrary ID padding is not an accepted move.
    maxima={};maxima['Input']=max(len(raw(command(t['ctx'],parse(frame,'Control'),dest),'Input')) for frame in t['frames'] for dest in world.GRAPH)
    inflated=copy.deepcopy(t['inputs'][0]);cmd=inflated['command'];cmd['expectedPriorVersion']=2**63-1;cmd['unit']['creationBinding']='x'*128;cmd['unit']['elementId']=max(world.SIDES,key=len);cmd['unit']['originalSide']=inflated['actor']='commonwealth';cmd['originLocationId']=cmd['destinationLocationId']=max(world.GRAPH,key=len)
    cmd['positionId']='x'*128
    maxima['InputStructural']=len(encode(inflated));assert maxima['Input']<=maxima['InputStructural']<=1024
    measured={'Source':len(t['source']),'Control':max(len(x) for x in t['frames']),'Proof':len(raw(p|dict(control=t['state']),'Proof')),'Event':max(len(x) for x in t['events']),'Input':max(len(raw(x,'Input')) for x in t['inputs']),'MovementLedger':len(encode(t['ledger']))}
    for kind in ('Source','Control','Proof','Event','Input'):rejected(lambda:parse(b' '*(byte_limit(kind)+1),kind),1);assert measured[kind]<=model[kind]
    assert LIMITS['acceptedEvents']==4 and LIMITS['depth']==pac.LIMITS['depth']+2 and len(t['state']['world']['cohesionCauses'])==3
    print('PASS structural byte/depth/field domination',model,fields,maxima,measured)
    return dict(dominating=model,limits=caps,fieldAccounting=fields,acceptedInputMaxima=maxima,measured=measured)
def grammar_tests():
    row,upstream=roots()[0];args=arguments(row,upstream);source=source_for(row);ctx=derive(row['sourceCanonicalUtf8'].encode('ascii'),*args);inp=command(ctx,initial(ctx),row['owner']+'-rear')
    for value,code in [(True,1),(-1,2),(1.5,1),(2**63,2)]:bad=changed_leaf(inp,('command','expectedPriorVersion'),value);rejected(lambda:apply(source,*args,[],bad),code)
    for data in [b'{}',b'{"contractVersion":1,"contractVersion":1}',b' '+source,source+b' ',b'['*40+b']'*40,b'{"x":NaN}',bytes([255]),b'0'*(LIMITS['sourceBytes']+1)]:rejected(lambda:parse(data,'Source'))
    cost=dict(terrainCost=2,breakOffCost=0,totalCost=2,baseCpa=10,voluntaryCeiling=15,beforeCp=11,afterCp=13,excessCpDp=2,beforeCohesion=-1,afterCohesion=-3);assert parse(raw(cost,'Cost'),'Cost')==cost
    for value,code in [(True,1),(1.5,1),(-2**31-1,2),(2**31,2)]:bad=cost|dict(afterCohesion=value);rejected(lambda:raw(bad,'Cost'),code)
    for kind,v in [('Input',inp),('Source',json.loads(source))]:bad=copy.deepcopy(v);bad['unknown']=None;rejected(lambda:raw(bad,kind),1)
    for kind,value in [('Source',json.loads(source)),('Control',initial(ctx)),('Event',json.loads(trace(row,upstream)['events'][0])),('Input',inp)]:
        for field in value:bad=copy.deepcopy(value);bad.pop(field);rejected(lambda:parse(encode(bad),kind),1)
        good=raw(value,kind)
        for malformed in [good+b'\n',b'\xef\xbb\xbf'+good,good.replace(b':',b': ',1),good.replace(b'{',b'{"unknown":null,',1)]:rejected(lambda:parse(malformed,kind))
    event=json.loads(trace(row,upstream)['events'][0]);event['effect']['kind']='unknown';rejected(lambda:raw(event,'Event'),3)
    event['effect']['kind']=None;rejected(lambda:raw(event,'Event'),1)
    bad=json.loads(source);bad['movementEventCanonicalUtf8']=['{}']*5;rejected(lambda:raw(bad,'Source'),1)
    for malformed in [b'['+b','.join([b'0']*513)+b']',b'{'+b','.join(('"k'+str(i)+'":0').encode() for i in range(129))+b'}']:rejected(lambda:parse(malformed,'Source'),1)
    rejected(lambda:replay(source,*args,None),1)
    rejected(lambda:replay(source,*args,[inp]),4)
    accepted=trace(row,upstream);rejected(lambda:replay(accepted['sources'][1],*args,[]),4)
    print('PASS closed canonical/numeric/signed/arms/preparse and independent Movement-ledger grammar')
def timestamp_tests():
    row,upstream=roots()[0];t=pac.trace(upstream,'repeat',2000,2001);ctx=derive(t['source'],*t['args'],t['ledger']);assert ctx['prior']['acceptedHighWater']==2001
    source=raw(dict(contractVersion=1,continuationSourceCanonicalUtf8=t['source'].decode('ascii'),movementEventCanonicalUtf8=[]),'Source');out,ledger,_=apply(source,*t['args'],t['ledger'],[],command(ctx,initial(ctx),row['owner']+'-rear'));assert replay(out,*t['args'],t['ledger'],ledger)[2]['members'][0]['spentCp']==world.cp(9)
    finish=pac.trace(upstream,'finish');rejected(lambda:derive(finish['source'],*finish['args'],finish['ledger']),3);print('PASS authentic clock variant and finish rejection')
def dependency_tests():
    row,upstream=roots()[0];source=source_for(row);args=arguments(row,upstream);real_read=Path.read_bytes;count=0
    for path in DEPENDENCIES:
        physical=(REPOSITORY/path).resolve()
        for outcome in ('changed','missing','unreadable','null'):
            def altered(p):
                if p.resolve()==physical:
                    if outcome=='missing':raise FileNotFoundError(path)
                    if outcome=='unreadable':raise PermissionError(path)
                    if outcome=='null':return None
                    return b'changed'
                return real_read(p)
            try:Path.read_bytes=altered;rejected(lambda:replay(source,*args,[]),9);count+=1
            finally:Path.read_bytes=real_read
        hits=[0]
        def after_first(p):
            if p.resolve()==physical:
                hits[0]+=1
                if hits[0]>1:return b'changed-after-first-read'
            return real_read(p)
        try:Path.read_bytes=after_first;rejected(lambda:replay(source,*args,[]),9);count+=1
        finally:Path.read_bytes=real_read
    for path in OWN_DELIVERY:
        def altered(p):return b'changed-own' if p.resolve()==path.resolve() else real_read(p)
        try:Path.read_bytes=altered;rejected(lambda:replay(source,*args,[]),9);count+=1
        finally:Path.read_bytes=real_read
    key=next(iter(DEPENDENCIES));old=DEPENDENCIES[key]
    try:DEPENDENCIES[key]='0'*64;rejected(verify_dependencies,9)
    finally:DEPENDENCIES[key]=old
    print('PASS physical warm/changed/missing/unreadable/null/afterfirstread',len(DEPENDENCIES),count)
def fixture_artifacts():
    traces=[]
    for row,upstream in roots():
        t=trace(row,upstream);claim=proof(t['source'],*t['args'],t['ledger']);traces.append(dict(owner=row['owner'],firstSeal=row['firstSeal'],sourceCanonicalUtf8=t['source'].decode('ascii'),movementLedger=t['ledger'],controlCanonicalUtf8=[x.decode('ascii') for x in t['frames']],proofCanonicalUtf8=raw(claim,'Proof').decode('ascii'),sourceHash=sha(t['source']),proofHash=sha(raw(claim,'Proof')),eventHashes=[sha(x) for x in t['events']]))
    return dict(contractVersion=1,dependencyManifestHash='sha256:'+identity('dependency-manifest',dict(sorted(DEPENDENCIES.items()))),traces=traces)
FIXTURE=ROOT/'fixtures/combat-actual-repeated-movement-v1.json'
def fixture_tests():assert json.loads(test_bytes('docs/specs/fixtures/combat-actual-repeated-movement-v1.json'))==fixture_artifacts();print('PASS retained literals4traces/20frames/16events')
def normal_gate():
    semantic_tests();grammar_tests();timestamp_tests();capacity_tests();dependency_tests()
    for owner in ('axis','commonwealth'):
        for seal in ('attacker','defender'):preservation_tests(owner,seal);recovery_tests(owner,seal);adversarial_tests(owner,seal)
    fixture_tests();print('PASS complete private actual repeated Movement; completion/native/public/parent remain open')
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--group',choices=['semantic','preservation','recovery','adversarial','grammar','timestamp','dependencies','fixture','capacity']);parser.add_argument('--owner',choices=['axis','commonwealth']);parser.add_argument('--seal',choices=['attacker','defender']);parser.add_argument('--write-fixture',action='store_true');args=parser.parse_args()
    if args.write_fixture:require(INVENTORY['closureFrozen'],7,'/provisionalClosure');FIXTURE.write_text(json.dumps(fixture_artifacts(),indent=2)+'\n')
    elif args.group=='dependencies':dependency_tests()
    elif args.group in ('semantic','grammar','timestamp','fixture','capacity'):globals()[args.group+'_tests']()
    elif args.group:globals()[args.group+'_tests'](args.owner,args.seal)
    else:normal_gate()
if __name__=='__main__':main()
