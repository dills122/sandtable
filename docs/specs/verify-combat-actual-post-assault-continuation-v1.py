#!/usr/bin/env python3
"""Private actual-source Release and guarded repeat/finish executable reference."""
import argparse
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent
REPOSITORY=ROOT.parent.parent
INVENTORY=json.loads((ROOT/'combat-actual-post-assault-continuation-v1.schema.json').read_bytes())
SCHEMA={k:[tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['objects'].items()}
LIMITS=INVENTORY['limits']
DEPENDENCIES=dict(INVENTORY['runtimeSourceHashes'])
PACKAGED_MANIFEST=json.dumps(dict(sorted(DEPENDENCIES.items())),ensure_ascii=True,separators=(',',':')).encode('ascii')
def encode(v):return json.dumps(v,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(data):return 'sha256:'+hashlib.sha256(data).hexdigest()
class Invalid(ValueError):
    def __init__(self,code,path=''):self.code=f'CMB-PAC-{code:03}';self.path=path;super().__init__(self.code+' '+(path or '/'))
def require(ok,code,path=''):
    if not ok:raise Invalid(code,path)
def consume(path):
    require(path in DEPENDENCIES,9,path)
    try:data=(REPOSITORY/path).read_bytes()
    except OSError as error:raise Invalid(9,path) from error
    require(hashlib.sha256(data).hexdigest()==DEPENDENCIES[path],9,path);return data
OWN_DELIVERY={p:sha(p.read_bytes()) for p in (ROOT/'combat-actual-post-assault-continuation-v1.schema.json',Path(__file__).resolve())}
def verify_dependencies():
    require(encode(dict(sorted(DEPENDENCIES.items())))==PACKAGED_MANIFEST,9,'/packagedManifest')
    for path,value in DEPENDENCIES.items():
        require(type(path) is str and path.isascii() and not path.startswith('/') and '\\' not in path and all(p not in ('','.','..') for p in path.split('/')) and type(value) is str and len(value)==64 and all(c in '0123456789abcdef' for c in value),9,'/manifest')
        consume(path)
    for path,expected in OWN_DELIVERY.items():
        try:data=path.read_bytes()
        except OSError as error:raise Invalid(9,'/ownDelivery') from error
        require(sha(data)==expected,9,'/ownDelivery')
verify_dependencies()
spec=importlib.util.spec_from_file_location('actual_paid_predecessor',ROOT/'verify-combat-actual-paid-assault-v1.py')
paid=importlib.util.module_from_spec(spec);spec.loader.exec_module(paid)
# Policy kernels are already in paid's fixed62 transitive map; no synthetic source reader.
spec=importlib.util.spec_from_file_location('actual_continuation_policy',ROOT/'verify-combat-cycle-control-v1.py')
policy=importlib.util.module_from_spec(spec);spec.loader.exec_module(policy)
mov,rel,world,steps=policy.mov,policy.rel,policy.world,policy.steps
verify_dependencies()
def digest(domain,value):return hashlib.sha256(domain.encode('ascii')+b'\0'+encode(value)).hexdigest()
def identity(kind,value):return digest(INVENTORY['domains'][kind],value)
def byte_limit(kind):return {'Source':LIMITS['sourceBytes'],'Proof':LIMITS['proofBytes'],'Control':LIMITS['controlBytes'],'Event':LIMITS['eventBytes'],'Input':LIMITS['inputBytes'],'Command':LIMITS['inputBytes']}.get(kind,LIMITS['proofBytes'])
def typed(v,kind,path='',depth=0,parent='',field=''):
    require(depth<=LIMITS['depth'],1,path)
    if kind.endswith('?'):
        if v is not None:typed(v,kind[:-1],path,depth,parent,field)
    elif kind=='Effect':
        require(type(v) is dict and type(v.get('kind')) is str,1,path);require(v['kind'] in INVENTORY['effectTags'],3,path);typed(v,INVENTORY['effectTags'][v['kind']],path,depth)
    elif kind in SCHEMA:
        fields=SCHEMA[kind];require(type(v) is dict and set(v)=={k for k,t in fields},1,path)
        for k,t in fields:typed(v[k],t,path+'/'+k,depth+1,kind,k)
        if kind in INVENTORY['versionRules']:require(v['contractVersion']==INVENTORY['versionRules'][kind],3,path)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v)<=INVENTORY['arrayBounds'].get(parent+'.'+field,LIMITS['arrayItems']),1,path)
        for i,x in enumerate(v):typed(x,kind[:-2],path+'/'+str(i),depth+1)
    elif kind in ('carrier','eventCarrier'):
        require(type(v) is str and v.isascii() and 0<len(v)<= (paid.LIMITS['sourceBytes'] if kind=='carrier' else LIMITS['eventBytes']),1,path)
    elif kind in ('int','long','utc'):
        require(type(v) is int,1,path);require(0<=v<=({'int':2**31-1,'long':2**63-1,'utc':paid.MAX_UTC}[kind]),2,path)
    else:
        try:paid.typed(v,kind,path,0)
        except paid.Invalid as error:raise Invalid(int(error.code[-3:]),error.path) from error

def canonical(v,kind):
    if kind.endswith('?'):return None if v is None else canonical(v,kind[:-1])
    if kind=='Effect':return canonical(v,INVENTORY['effectTags'][v['kind']])
    if kind in SCHEMA:return {k:canonical(v[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'):return [canonical(x,kind[:-2]) for x in v]
    if kind in ('carrier','eventCarrier'):return v
    return paid.canonical(v,kind)
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

def derive(data,selection,rounds,pay):
    verify_dependencies()
    try:source,ctx,state,owned=paid.replay(data,selection,rounds,pay)
    except paid.Invalid as error:raise Invalid(9 if error.code.endswith('009') else int(error.code[-3:]),error.path) from error
    c=ctx['cycle'];b=ctx['boundary'];p=ctx['prior'];w=state['world']
    require((c['gameTurn'],c['operationStage'],c['playerPhaseSlot'],c['ordinal'])==(1,1,'first-acting-side',1),3,'/profile')
    require(state['status']=='closed' and state['closed'] and state['stateVersion']==33 and state['window'] is None and state['roundClosureReceiptId'] is not None and state['caCompletionReceiptId'] is not None,4,'/paidTerminal')
    require(state['randomState']['seed']==1 and state['randomState']['nextByteCursor']==10 and len(state['result']['draws'])==8 and len(state['receipts'])==len(owned)==8,3,'/ordinaryTerminal')
    require(len(w['settlements'])==1 and w['settlements'][0]['relationships']['kind']=='contact' and not w['custodyLots'] and not w['guards'] and not w['replacementEntitlements'] and not w['brokenVehicleLots'],3,'/ordinaryTerminal')
    selection_proof=ctx['base']['admissionProof']['actualRoundProof']['base']['actualSelectionProof']
    entry=selection_proof['positiveEntryProof']['entry'];members=copy.deepcopy(entry['members']);end=copy.deepcopy(b['movementEnd'])
    originals=[world.unit(e,w['creationBinding']) for e in w['elements'] if world.unit(e,w['creationBinding'])['originalSide']==c['actingSide']]
    require([rel.key(m['unit']) for m in members]==sorted(rel.key(u) for u in originals) and len(members)<=32,4,'/membership')
    require(len(members)==1 and end['completionReceiptId'].startswith('iml.') and end==entry['movementEnd'],4,'/movementEnd')
    try:policy.validate_proof(end,c,[world.unit(e,w['creationBinding']) for e in w['elements']],1)
    except policy.Invalid as error:raise Invalid(4,'/movementEnd') from error
    for member in members:
        e=next(e for e in w['elements'] if world.unit(e,w['creationBinding'])==member['unit']);op=e['operationalState']
        require(member['status']==e['reserveStatus']=='none' and member['history']['scope']==rel.scope(c) and member['baseCpa']==10,4,'/member')
        require(member['history']==next(m['history'] for m in entry['members'] if m['unit']==member['unit']),4,'/history')
        require(all(member['history'][k] is None for k in member['history'] if k!='scope'),3,'/ordinaryHistory')
        require(e['ammunition']['points']==0 and e['components'][0]['currentToe']==10 and op['capabilityPointsExpended']==dict(numerator=5,denominator=1) and (op['ledgerGameTurn'],op['ledgerOperationStage'])==(1,1) and op['vehicleBreakdownState'] is None and op['movementEnded'] is None,3,'/ordinaryMember')
        member['spentCp']=copy.deepcopy(op['capabilityPointsExpended'])
    events=[(json.loads(text),text.encode('ascii')) for text in source['paidEventCanonicalUtf8']]
    commits=[(e,t) for e,t in events if e['eventType']=='combat-actual-paid-attack-committed'];require(len(commits)==1,4,'/progress')
    event,text=commits[0];h=state['attackHistory'];require(event['effect']['commitmentId']==state['commitmentId'] and event['receiptId']!=state['commitmentId'] and len(h)==1 and h[0]['commitmentId']==state['commitmentId'] and h[0]['attacker']==members[0]['unit'] and (h[0]['gameTurn'],h[0]['operationStage'])==(1,1),4,'/progress')
    require(any(r['receiptId']==event['receiptId'] and r['eventHash']==sha(text) for r in state['receipts']),4,'/progress')
    last=events[-1][0];require(last['receiptId']==state['caCompletionReceiptId'] and last['effect']['kind']=='ca-completed' and last['effect']['roundClosureReceiptId']==state['roundClosureReceiptId'] and last['effect']['previousStepReceiptId']==p['stepReceipts'][-1] and last['effect']['toPositionId']==policy.edge(c)['releasePositionId'],4,'/caReceipt')
    paid_digest=paid.digest('source',paid.canonical(source,'PaidSource'))
    proof=dict(contractVersion=1,sourceId='apass.'+paid_digest,sourceHash='sha256:'+paid_digest,base=copy.deepcopy(ctx['base']),control=copy.deepcopy(state));paid.raw(proof,'PaidProof')
    basis=dict(contractVersion=1,dependencyManifestHash='sha256:'+identity('dependency-manifest',dict(sorted(DEPENDENCIES.items()))),paidProof=proof,controlClockPolicyId=INVENTORY['controlClockPolicyId'])
    raw(basis,'Base');return dict(base=basis,baseHash='sha256:'+identity('base',canonical(basis,'Base')),cycle=copy.deepcopy(c),boundary=copy.deepcopy(b),paid=copy.deepcopy(state),members=members,movementEnd=end,progress=[dict(eventType=event['eventType'],receiptId=event['receiptId'],eventHash=sha(text))],config=ctx['config'])
def base_for(data,selection,rounds,pay):return copy.deepcopy(derive(data,selection,rounds,pay)['base'])
def initial(ctx):
    c=ctx['cycle'];p=ctx['paid'];base=ctx['baseHash'];pos=policy.edge(c)['releasePositionId']
    release='aprel.'+identity('release',dict(baseHash=base,cycleId=ctx['boundary']['cycleId'],positionId=pos));control='apctl.'+identity('control',dict(baseHash=base,releaseId=release))
    return dict(contractVersion=1,baseHash=base,paidSourceHash=ctx['base']['paidProof']['sourceHash'],controlId=control,releaseId=release,stateVersion=p['stateVersion'],prefix=p['prefix'],status='release-unopened',positionId=pos,originalCycle=copy.deepcopy(c),activeCycle=copy.deepcopy(c),closure=None,releaseOpeningReceiptId=None,releaseCompletionReceiptId=None,decisionId=None,timing=None,openingClockFailure=False,acceptedHighWater=None,assessment=None,movementEnd=copy.deepcopy(ctx['movementEnd']),members=copy.deepcopy(ctx['members']),world=copy.deepcopy(p['world']),randomState=copy.deepcopy(p['randomState']),attackHistory=copy.deepcopy(p['attackHistory']),targetUses=copy.deepcopy(p['targetUses']),nextCycleProgress=copy.deepcopy(ctx['progress']),receipts=[],closed=False)
def destination_blocked(w,element,destination):
    return any(x['currentLocationId']==destination for x in w['elements'] if x is not element) or any(x['currentLocationId']==destination for x in w['guards'])
def assessment(ctx,s):
    require(s['releaseCompletionReceiptId'] is not None and s['status']=='release-completed',4,'/releaseCompletion');w=s['world'];c=ctx['cycle']
    require(w==ctx['paid']['world'] and s['movementEnd']==ctx['movementEnd'] and s['members']==ctx['members'],4,'/assessmentSource')
    require(all(x['relationships'] is not None and all(x[k] is not None for k in ('disposition','losses','retreat')) for x in w['settlements']) and not w['brokenVehicleLots'],3,'/pendingWork')
    ineligible=policy.excluded(ctx['movementEnd'],c['actingSide']);witnesses=[]
    for m in s['members']:
        u=m['unit'];e=next(e for e in w['elements'] if world.unit(e,w['creationBinding'])==u);op=e['operationalState']
        require(e['ammunition']['points']==0 and e['components'][0]['currentToe']>0 and m['spentCp']==op['capabilityPointsExpended'] and m['spentCp']['denominator']==1,3,'/capability')
        require(m['status']=='none' and m['history']['nextMovement'] is None,3,'/ordinaryMember')
        reps=[x for x in w['representations'] if x['bindingKind']=='independent-element' and x['boundElementIds']==[u['elementId']]];require(len(reps)==1 and reps[0]['currentLocationId']==e['currentLocationId'],4,'/representation')
        if u in ineligible:continue
        try:affected=mov.affected(w['relationships'],u)
        except mov.Invalid as error:raise Invalid(3,'/relationships') from error
        require(all((x['gameTurn'],x['operationStage'])==(c['gameTurn'],c['operationStage']) for x in w['relationships'] if x['active']),4,'/relationshipScope')
        origin=e['currentLocationId']
        for destination in sorted(world.GRAPH[origin]):
            if destination_blocked(w,e,destination):continue
            try:terrain=mov.terrain_cost(origin,destination);after,dp=mov.cost(m['baseCpa'],m['spentCp']['numerator'],[x['kind'] for x in affected],m['history']['releasedType'],terrain)
            except mov.Invalid as error:
                if error.code=='CMB-MOV-005':continue
                raise Invalid(3,'/movement') from error
            require(op['cohesionLevel']-dp>=-2**31,7,'/cohesion')
            witnesses.append(dict(kind='movement',unit=copy.deepcopy(u),destinationLocationId=destination,terrainCost=terrain,breakOffCost=after-m['spentCp']['numerator']-terrain,afterCp=after,excessCpDp=dp,usesReleaseException=False))
    return dict(releaseCompletionReceiptId=s['releaseCompletionReceiptId'],movementCompletionReceiptId=ctx['movementEnd']['completionReceiptId'],progress=copy.deepcopy(ctx['progress']),witnesses=witnesses,combatAssessment='no-own-ammunition',witnessOutcome='supported',unsupportedReason=None)
def mode(a):
    require(a['witnessOutcome']=='supported' and a['unsupportedReason'] is None,3,'/witnessOutcome')
    return 'no-continuation' if not a['witnesses'] else 'no-material-progress' if not a['progress'] else 'owner-choice'
def command(ctx,s,kind):return dict(contractVersion=1,kind=kind,baseHash=ctx['baseHash'],cycleId=ctx['boundary']['cycleId'],expectedPriorVersion=None if kind in ('expire','unavailable') else s['stateVersion'],decisionId=None if kind in ('release-open','release-complete','control-open') else s['decisionId'])
def trusted(cmd,actor='system',now=None,available=True):return dict(command=copy.deepcopy(cmd),actor=actor,admittedAt=now,clockAvailable=available)
def transition(ctx,prior,inp):
    typed(prior,'Control');typed(inp,'Input');raw(inp,'Input');cmd=inp['command'];kind=cmd['kind'];actor=inp['actor'];c=ctx['cycle']
    require(kind in INVENTORY['commands'],3,'/kind');require(cmd['baseHash']==prior['baseHash']==ctx['baseHash'] and cmd['cycleId']==ctx['boundary']['cycleId'] and prior['originalCycle']==c,4,'/context')
    require(actor==(c['actingSide'] if kind in ('repeat','finish') else 'system'),4,'/actor');require((cmd['expectedPriorVersion'] is None)==(kind in ('expire','unavailable')),3,'/expectedPriorVersion')
    require(kind not in ('release-open','release-complete','control-open') or cmd['decisionId'] is None,3,'/decisionId')
    if kind.startswith('release-'):require(inp['admittedAt'] is None and inp['clockAvailable'],3,'/releaseClock')
    ch=sha(raw(cmd,'Command'));receipt=next((r for r in prior['receipts'] if r['commandHash']==ch and r['actor']==actor),None)
    if receipt:return copy.deepcopy(prior),None,receipt['receiptId']
    if kind in ('expire','unavailable') and (prior['status']!='control-open' or cmd['decisionId']!=prior['decisionId']):return copy.deepcopy(prior),None,None
    require(not prior['closed'] and len(prior['receipts'])<4 and prior['stateVersion']<2**63-1,7,'/closedOrCapacity')
    if kind not in ('expire','unavailable'):require(cmd['expectedPriorVersion']==prior['stateVersion'],6,'/priorVersion')
    s=copy.deepcopy(prior);author=actor;effect=None;now=inp['admittedAt'];next_cycle=None;outcome=None
    if kind=='release-open':
        require(s['status']=='release-unopened',6);s['status']='release-open';effect=dict(kind='release-opened',releaseId=s['releaseId'],pending=[],decisionId=None,timing=None)
    elif kind=='release-complete':
        require(s['status']=='release-open' and s['releaseOpeningReceiptId'] is not None,6);s['status']='release-completed';effect=dict(kind='release-completed',releaseId=s['releaseId'],openingReceiptId=s['releaseOpeningReceiptId'],reason='units-resolved',retainedUnits=[],dispositionReceipts=[],timing=None)
    else:
        reason='owner-choice'
        if kind=='control-open':
            require(s['status']=='release-completed',6);s['assessment']=assessment(ctx,s);selected=mode(s['assessment'])
            if selected!='owner-choice':outcome='phase-finished';reason=selected
            else:
                require(c['ordinal']<2**31-1 and s['stateVersion']<=2**63-3,7,'/openingCapacity');s['decisionId']=s['controlId']+'.decision';require(len(s['decisionId'])<=128,7)
                lost=not inp['clockAvailable'] or now is None
                try:s['timing']=None if lost else steps.inputs.make_timing(ctx['config'],'cycle-control',now)
                except steps.inputs.Invalid:s['timing']=None
                s['openingClockFailure']=s['timing'] is None
                if s['timing'] is not None:s['acceptedHighWater']=now
                outcome='control-opened';s['status']='control-open'
        else:
            require(s['status']=='control-open' and cmd['decisionId']==s['decisionId'],6,'/decisionId');t=s['timing']
            lost=s['openingClockFailure'] or not inp['clockAvailable'] or now is None or s['acceptedHighWater'] is not None and now<s['acceptedHighWater'];late=t is not None and now is not None and now>=t['deadlineUnixMilliseconds']
            if kind=='fallback-step':require(s['openingClockFailure'],5)
            if kind=='expire' and not lost and not late:return copy.deepcopy(prior),None,None
            if kind in ('repeat','finish') and not lost:require(not late,5,'/deadline')
            if not lost:s['acceptedHighWater']=now;s['timing']['highWaterUnixMilliseconds']=now
            if lost or kind in ('expire','unavailable','fallback-step'):
                author='system';outcome='phase-finished';reason='opening-clock-unavailable' if s['openingClockFailure'] else 'clock-unavailable' if lost else 'deadline' if kind=='expire' else 'controller-unavailable'
            else:outcome='cycle-repeated' if kind=='repeat' else 'phase-finished'
        successor=s['positionId']
        if outcome=='cycle-repeated':
            require(mode(s['assessment'])=='owner-choice' and c['ordinal']<2**31-1,7)
            next_cycle=copy.deepcopy(c);next_cycle.update(ordinal=c['ordinal']+1,openedAuthorityVersion=prior['stateVersion']+1,openingPrefix=prior['prefix'])
            policy.cycle_id(next_cycle,ctx['boundary']['firstActingSide']);successor=policy.edge(c)['movementPositionId'];s['activeCycle']=next_cycle;s['targetUses']=[];s['nextCycleProgress']=[];s['status']='repeated';s['closed']=True
        elif outcome=='phase-finished':successor=policy.edge(c)['finishPositionId'];s['activeCycle']=None;s['status']='finished';s['closed']=True
        effect=dict(kind=outcome,reason=reason,assessment=copy.deepcopy(s['assessment']),timing=copy.deepcopy(s['timing']),openingClockFailure=s['openingClockFailure'],successorPositionId=successor,nextCycle=next_cycle,expiredUnits=[]);s['positionId']=successor
    event=dict(contractVersion=1,eventType='combat-actual-post-assault-'+effect['kind'],author=author,campaignId=c['campaignId'],rulesetHash=c['rulesetHash'],configurationHash=c['admittedPolicyBundleDigest'],paidSourceHash=prior['paidSourceHash'],baseHash=ctx['baseHash'],cycleId=ctx['boundary']['cycleId'],positionId=prior['positionId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],input=canonical(inp,'Input'),effect=canonical(effect,'Effect'))
    rid='apc.'+identity('receipt',event);event['receiptId']=rid;data=raw(event,'Event')
    if effect['kind']=='release-opened':s['releaseOpeningReceiptId']=rid
    elif effect['kind']=='release-completed':s['releaseCompletionReceiptId']=rid
    elif effect['kind']!='control-opened':s['closure']=dict(cycleId=ctx['boundary']['cycleId'],ordinal=c['ordinal'],outcome=s['status'],receiptId=rid)
    s['stateVersion']=event['stateVersion'];s['prefix']=steps.seq.prefix_event(prior['prefix'],data);s['receipts'].append(dict(commandHash=ch,eventHash=sha(data),receiptId=rid,actor=actor,stateVersion=s['stateVersion']));raw(s,'Control');return s,data,rid

def ingress(data,selection,rounds,pay,continuation):
    verify_dependencies();source=parse(data,'Source')
    ctx=derive(source['paidSourceCanonicalUtf8'].encode('ascii'),selection,rounds,pay)
    require(type(continuation) is list and len(continuation)<=4,1,'/continuationLedger');owned=[]
    for x in continuation:owned.append(parse(raw(x,'Input'),'Input'))
    require(len(encode(owned))<=LIMITS['continuationLedgerBytes'],1,'/continuationLedger');require(len(owned)==len(source['continuationEventCanonicalUtf8']),4,'/continuationLedger')
    return source,ctx,owned
def replay(data,selection,rounds,pay,continuation):
    source,ctx,owned=ingress(data,selection,rounds,pay,continuation);s=initial(ctx)
    for n,(text,inp) in enumerate(zip(source['continuationEventCanonicalUtf8'],owned)):
        event=text.encode('ascii');parse(event,'Event');s,expected,_=transition(ctx,s,inp);require(expected is not None and event==expected,6,'/event/'+str(n))
    verify_dependencies();return source,ctx,s,owned
def apply(data,selection,rounds,pay,continuation,new_input):
    source,ctx,s,owned=replay(data,selection,rounds,pay,continuation);after,event,rid=transition(ctx,s,new_input)
    if event is None:verify_dependencies();return raw(source,'Source'),copy.deepcopy(owned),rid
    source['continuationEventCanonicalUtf8'].append(event.decode('ascii'));ledger=owned+[parse(raw(new_input,'Input'),'Input')];require(len(encode(ledger))<=LIMITS['continuationLedgerBytes'],1);result=raw(source,'Source');verify_dependencies();return result,ledger,rid
def read_source(data,selection,rounds,pay,continuation):
    result=copy.deepcopy(replay(data,selection,rounds,pay,continuation)[0]);require(INVENTORY['closureFrozen'],7,'/provisionalClosure');return result
def proof(data,selection,rounds,pay,continuation):
    source,ctx,s,_=replay(data,selection,rounds,pay,continuation);require(INVENTORY['closureFrozen'],7,'/provisionalClosure');h=identity('source',canonical(source,'Source'));out=dict(contractVersion=1,sourceId='apcont.'+h,sourceHash='sha256:'+h,base=ctx['base'],control=s);return parse(raw(out,'Proof'),'Proof')
def read_proof(claim,data,selection,rounds,pay,continuation):
    expected=proof(data,selection,rounds,pay,continuation);value=parse(claim,'Proof');require(claim==raw(expected,'Proof'),6,'/proof');return value
def read_control(claim,data,selection,rounds,pay,continuation):
    s=replay(data,selection,rounds,pay,continuation)[2];require(INVENTORY['closureFrozen'],7,'/provisionalClosure');value=parse(claim,'Control');require(claim==raw(s,'Control'),6,'/control');return value

def source_for(row):
    return encode(dict(contractVersion=1,paidSourceCanonicalUtf8=row['sourceCanonicalUtf8'],continuationEventCanonicalUtf8=[]))
def semantic_tests():
    cases=json.loads((ROOT/'fixtures/combat-actual-paid-assault-v1.json').read_bytes())['cases'];failures=[]
    for row in cases:
        label=row['owner']+'/'+row['firstSeal'];selection=row['selectionLedger'];rounds=row['roundLedger'];pay=row['paidLedger']
        _,ctx,state,_=paid.replay(row['sourceCanonicalUtf8'].encode('ascii'),selection,rounds,pay)
        assert state['stateVersion']==33 and state['closed'] and state['window'] is None
        assert state['randomState']['seed']==1 and state['randomState']['nextByteCursor']==10
        own=next(e for e in state['world']['elements'] if e['elementId']==ctx['selection']['attacker']['unit']['elementId'])
        assert own['ammunition']['points']==0 and own['components'][0]['currentToe']==10
        assert own['operationalState']['capabilityPointsExpended']==dict(numerator=5,denominator=1)
        assert ctx['boundary']['movementEnd']['completionReceiptId'].startswith('iml.')
        base=base_for(row['sourceCanonicalUtf8'].encode('ascii'),selection,rounds,pay)
        inp=dict(command=dict(contractVersion=1,kind='release-open',baseHash='sha256:'+digest('sandtable.combat.actual-post-assault-continuation.base.v1',base),cycleId=ctx['boundary']['cycleId'],expectedPriorVersion=33,decisionId=None),actor='system',admittedAt=None,clockAvailable=True)
        try:
            after,ledger,rid=apply(source_for(row),selection,rounds,pay,[],inp)
            event=json.loads(json.loads(after)['continuationEventCanonicalUtf8'][0]);assert event['stateVersion']==34 and rid==event['receiptId'] and len(ledger)==1
            print('PASS semantic actual Release open',label)
        except Invalid as error:
            failures.append(label);print('FAIL semantic actual Release publication',label,'expected version34/event/receipt/prefix with unchanged World; actual',error.code)
    assert not failures, str(len(failures))+' genuine successor publications failed: '+','.join(failures)


def cases():return json.loads((ROOT/'fixtures/combat-actual-paid-assault-v1.json').read_bytes())['cases']
def rejected(call,code=None):
    try:call()
    except Invalid as error:
        if code is not None:assert error.code==f'CMB-PAC-{code:03}',(error.code,error.path,code)
        return error.code
    raise AssertionError('invalid input accepted')
def arguments(row):return row['selectionLedger'],row['roundLedger'],row['paidLedger']
def trace(row,outcome='repeat',opening=1000,choice=1001,available=True):
    args=arguments(row);data=source_for(row);ledger=[];frames=[];events=[];inputs=[];ctx=derive(row['sourceCanonicalUtf8'].encode('ascii'),*args);state=initial(ctx);frames.append(raw(state,'Control'))
    for kind,actor,now,confidence in [('release-open','system',None,True),('release-complete','system',None,True),('control-open','system',opening,available),(outcome,row['owner'],choice,True)]:
        inp=trusted(command(ctx,state,kind),actor,now,confidence);data,ledger,rid=apply(data,*args,ledger,inp);source=json.loads(data);event=source['continuationEventCanonicalUtf8'][-1].encode('ascii');state=replay(data,*args,ledger)[2]
        assert rid==json.loads(event)['receiptId'];inputs.append(inp);events.append(event);frames.append(raw(state,'Control'))
    return dict(row=row,args=args,source=data,ledger=ledger,frames=frames,events=events,inputs=inputs,ctx=ctx,state=state)
def preservation_tests(owner=None):
    count=0
    for row in cases():
        if owner is not None and row['owner']!=owner:continue
        for outcome in ('repeat','finish'):
            t=trace(row,outcome);ctx=t['ctx'];p=ctx['paid'];s=t['state'];c=ctx['cycle'];before=initial(ctx)
            assert [json.loads(x)['stateVersion'] for x in t['events']]==[34,35,36,37]
            assert [json.loads(x)['effect']['kind'] for x in t['events']]==['release-opened','release-completed','control-opened','cycle-repeated' if outcome=='repeat' else 'phase-finished']
            for data in t['frames']:
                frame=parse(data,'Control');assert frame['world']==p['world'] and frame['randomState']==p['randomState'] and frame['members']==ctx['members'] and frame['attackHistory']==p['attackHistory'] and frame['movementEnd']==ctx['movementEnd']
            a=s['assessment'];assert a['witnessOutcome']=='supported' and a['unsupportedReason'] is None and len(a['witnesses'])==len(a['progress'])==1
            witness=a['witnesses'][0];assert (witness['terrainCost'],witness['breakOffCost'],witness['afterCp'],witness['excessCpDp'],witness['usesReleaseException'])==(2,2,9,0,False)
            assert witness['destinationLocationId']==row['owner']+'-rear' and witness['unit']==ctx['members'][0]['unit']
            assert a['progress']==ctx['progress'] and a['progress'][0]['eventType']=='combat-actual-paid-attack-committed' and a['progress'][0]['receiptId']!=p['commitmentId']
            assert a['movementCompletionReceiptId']==ctx['movementEnd']['completionReceiptId'] and a['releaseCompletionReceiptId']==json.loads(t['events'][1])['receiptId']
            assert s['closed'] and s['closure']['ordinal']==1 and s['closure']['receiptId']==json.loads(t['events'][-1])['receiptId']
            if outcome=='repeat':
                assert s['activeCycle']['ordinal']==2 and s['activeCycle']['openingPrefix']==json.loads(t['events'][-1])['priorPrefix'] and s['activeCycle']['openedAuthorityVersion']==37
                assert s['positionId']==policy.edge(c)['movementPositionId'] and not s['targetUses'] and not s['nextCycleProgress']
                for k in c:
                    if k not in ('ordinal','openedAuthorityVersion','openingPrefix'):assert s['activeCycle'][k]==c[k]
            else:assert s['activeCycle'] is None and s['positionId']==policy.edge(c)['finishPositionId'] and s['targetUses']==p['targetUses']
            prefix=p['prefix']
            for event in t['events']:
                value=json.loads(event);assert value['priorPrefix']==prefix
                unsigned={k:v for k,v in value.items() if k!='receiptId'};assert value['receiptId']=='apc.'+identity('receipt',unsigned);prefix=steps.seq.prefix_event(prefix,event)
            assert s['prefix']==prefix;count+=1
    print('PASS literal source/Release/control/World/RNG/prospective witness preservation',count)
def recovery_tests(owner=None):
    cuts=retries=0
    for row in cases():
        if owner is not None and row['owner']!=owner:continue
        t=trace(row);args=t['args'];whole=json.loads(t['source'])
        for n in range(5):
            source=copy.deepcopy(whole);source['continuationEventCanonicalUtf8']=source['continuationEventCanonicalUtf8'][:n];data=raw(source,'Source');ledger=t['ledger'][:n]
            source2,ctx,state,owned=replay(data,*args,ledger);assert raw(state,'Control')==t['frames'][n];cuts+=1
            for j in range(n):
                duplicate=copy.deepcopy(t['inputs'][j])
                if not duplicate['command']['kind'].startswith('release-'):duplicate.update(admittedAt=None,clockAvailable=False)
                unchanged,retained,rid=apply(data,*args,ledger,duplicate);assert unchanged==data and retained==ledger and rid==json.loads(t['events'][j])['receiptId'];retries+=1
            cache=proof(data,*args,ledger);assert read_proof(raw(cache,'Proof'),data,*args,ledger)==cache and read_control(t['frames'][n],data,*args,ledger)==state
            cache['control']['world']['elements'].clear();owned.clear();source2['continuationEventCanonicalUtf8'].clear();assert raw(proof(data,*args,ledger)['control'],'Control')==t['frames'][n]
        original=paid.FRESH_ENABLED
        try:paid.FRESH_ENABLED=False;assert replay(t['source'],*args,t['ledger'])[2]['stateVersion']==37
        finally:paid.FRESH_ENABLED=original
    print('PASS every suffix cut, owned public readback and lost reply retries',cuts,retries)
def timing_tests(owner='axis'):
    row=next(x for x in cases() if x['owner']==owner);t=trace(row);ctx=t['ctx'];s=parse(t['frames'][3],'Control');data=json.loads(t['source']);data['continuationEventCanonicalUtf8']=data['continuationEventCanonicalUtf8'][:3];data=raw(data,'Source');ledger=t['ledger'][:3];args=t['args'];deadline=s['timing']['deadlineUnixMilliseconds']
    assert s['acceptedHighWater']==1000 and ctx['paid']['acceptedHighWater']>1000
    for now in (deadline,deadline+1):rejected(lambda:apply(data,*args,ledger,trusted(command(ctx,s,'repeat'),owner,now)),5)
    for now,available in [(None,True),(999,True),(1001,False)]:
        result,ins,_=apply(data,*args,ledger,trusted(command(ctx,s,'repeat'),owner,now,available));terminal=replay(result,*args,ins)[2];assert terminal['status']=='finished' and json.loads(json.loads(result)['continuationEventCanonicalUtf8'][-1])['author']=='system'
    for kind,now in [('expire',deadline),('unavailable',1001)]:
        result,ins,_=apply(data,*args,ledger,trusted(command(ctx,s,kind),'system',now));assert replay(result,*args,ins)[2]['status']=='finished'
    unchanged,retained,rid=apply(data,*args,ledger,trusted(command(ctx,s,'expire'),'system',deadline-1));assert unchanged==data and retained==ledger and rid is None
    for now,available in [(None,True),(1000,False),(paid.MAX_UTC,True)]:
        lost=trace(row,'repeat',now,1001,available);assert lost['state']['status']=='finished' and lost['state']['openingClockFailure'] and lost['state']['timing'] is None
    cmd=command(ctx,s,'repeat');wrong='commonwealth' if owner=='axis' else 'axis';rejected(lambda:apply(data,*args,ledger,trusted(cmd,wrong,None)),4)
    for malformed,code in [(True,1),(-1,2),(1.5,1),(paid.MAX_UTC+1,2)]:rejected(lambda:apply(data,*args,ledger,trusted(cmd,owner,malformed)),code)
    lost=trace(row,'finish',None,None);open_state=parse(lost['frames'][3],'Control');after,event,_=transition(lost['ctx'],open_state,trusted(command(lost['ctx'],open_state,'fallback-step'),'system',None));assert after['status']=='finished' and json.loads(event)['effect']['reason']=='opening-clock-unavailable'
    rejected(lambda:transition(ctx,s,trusted(command(ctx,s,'fallback-step'),'system',1001)),5)
    closed=t['state'];timer=trusted(command(ctx,closed,'expire'),'system',None);_,event,rid=transition(ctx,closed,timer);assert event is rid is None
    rejected(lambda:apply(t['source'],*args,t['ledger'],trusted(command(ctx,closed,'repeat'),owner,1002)),7)
    print('PASS independent control clocks, exact deadline, fallback, retry-context and terminal timers',owner)
def changed_leaf(value,path,new):
    out=copy.deepcopy(value);parent=out
    for p in path[:-1]:parent=parent[p]
    parent[path[-1]]=new;return out
def adversarial_tests(owner='axis'):
    row=next(x for x in cases() if x['owner']==owner);t=trace(row);args=t['args'];source=json.loads(t['source']);ledger=t['ledger'];ctx=t['ctx'];count=0
    # Coherently re-sign plausible effects: authentication still regenerates the exact independent cause.
    for n in range(4):
        event=json.loads(source['continuationEventCanonicalUtf8'][n])
        alterations=[(('author',),'commonwealth' if event['author']!='commonwealth' else 'axis'),(('stateVersion',),event['stateVersion']+1),(('priorPrefix',),'sha256:'+'0'*64)]
        if n>=2:alterations.extend([(('effect','assessment','progress',0,'receiptId'),ctx['paid']['commitmentId']),(('effect','assessment','progress',0,'eventHash'),'sha256:'+'0'*64),(('effect','assessment','witnesses',0,'afterCp'),8)])
        for path,new in alterations:
            altered=changed_leaf(event,path,new);altered['receiptId']='apc.'+identity('receipt',{k:v for k,v in altered.items() if k!='receiptId'});forged=copy.deepcopy(source);forged['continuationEventCanonicalUtf8'][n]=raw(altered,'Event').decode('ascii');rejected(lambda:read_source(raw(forged,'Source'),*args,ledger),6);count+=1
    for malformed in [ledger[:-1],ledger+[ledger[0]],list(reversed(ledger))]:rejected(lambda:read_source(t['source'],*args,malformed));count+=1
    for n in range(4):
        bad=copy.deepcopy(ledger);bad[n]['actor']='commonwealth' if bad[n]['actor']!='commonwealth' else 'axis';rejected(lambda:read_source(t['source'],*args,bad),4);count+=1
    for which in range(3):
        bad=[copy.deepcopy(x) for x in args];bad[which][0]['admittedAt']=123456789;rejected(lambda:read_source(t['source'],*bad,ledger));count+=1
    paid_source=json.loads(source['paidSourceCanonicalUtf8'])
    for cut in range(8):
        partial=copy.deepcopy(paid_source);partial['paidEventCanonicalUtf8']=partial['paidEventCanonicalUtf8'][:cut];forged=copy.deepcopy(source);forged['paidSourceCanonicalUtf8']=paid.raw(partial,'PaidSource').decode('ascii');partial_args=[args[0],args[1],args[2][:cut]];rejected(lambda:read_source(raw(forged,'Source'),*partial_args,ledger));count+=1
    other=next(x for x in cases() if x['owner']!=owner);rejected(lambda:read_source(t['source'],*arguments(other),ledger));count+=1
    claim=proof(t['source'],*args,ledger)
    for path,new in [(('control','world','elements',0,'components',0,'currentToe'),0),(('control','members',0,'spentCp','numerator'),0),(('control','movementEnd','completionReceiptId'),'iml.forged'),(('control','assessment','progress',0,'receiptId'),ctx['paid']['commitmentId']),(('control','activeCycle','ordinal'),3)]:
        forged=changed_leaf(claim,path,new);rejected(lambda:read_proof(raw(forged,'Proof'),t['source'],*args,ledger),6);count+=1
    for which in (0,1):
        bad=[copy.deepcopy(x) for x in args]
        for value,code in [(True,1),(-1,2)]:bad[which][0]['admittedAt']=value;rejected(lambda:read_proof(b'not-json',t['source'],*bad,ledger),code);count+=1
        bad=[copy.deepcopy(x) for x in args];bad[which][0]['command']['contractVersion']=2;rejected(lambda:read_source(t['source'],*bad,ledger),3);count+=1
    # Duplicate retry bytes cannot bypass primitive/family/context validation.
    duplicate=copy.deepcopy(ledger[-1]);duplicate['admittedAt']=True;rejected(lambda:apply(t['source'],*args,ledger,duplicate),1)
    duplicate=copy.deepcopy(ledger[-1]);duplicate['command']['baseHash']='sha256:'+'0'*64;rejected(lambda:apply(t['source'],*args,ledger,duplicate),4)
    duplicate=copy.deepcopy(ledger[-1]);duplicate['command']['kind']='execute-movement';rejected(lambda:apply(t['source'],*args,ledger,duplicate),3)
    print('PASS independent inputs and coherent source/claim/context forgeries',owner,count)
def timestamp_variant_tests():
    row=copy.deepcopy(cases()[0]);args=arguments(row);original=json.loads(row['sourceCanonicalUtf8']);admission=json.loads(original['admissionSourceCanonicalUtf8']);are=paid.aad.are;round_source=json.loads(admission['actualRoundSourceCanonicalUtf8']);round_source['roundEventCanonicalUtf8']=[];new_round=[]
    for inp in args[1]:
        inp=copy.deepcopy(inp)
        if inp['command']['kind']=='seal-choice':inp['admittedAt']+=17
        _,event,_=are.apply(round_source,args[0],new_round,inp);round_source['roundEventCanonicalUtf8'].append(event.decode('ascii'));new_round.append(inp)
    admission['actualRoundSourceCanonicalUtf8']=are.source_bytes(round_source).decode('ascii');data=paid.raw(dict(contractVersion=1,admissionSourceCanonicalUtf8=paid.aad.raw(admission,'AdmissionSource').decode('ascii'),paidEventCanonicalUtf8=[]),'PaidSource');new_paid=[]
    for n in range(8):
        _,base=paid.admitted(data,args[0],new_round,new_paid);ctx=paid.context(base);s=paid.replay(data,args[0],new_round,new_paid)[2]
        kind='commit' if n==0 else 'resolve' if n==1 else 'advance';inp=paid.trusted(paid.command(ctx,s,kind));data,new_paid,_=paid.apply(data,args[0],new_round,new_paid,inp)
    assert data!=row['sourceCanonicalUtf8'].encode('ascii');row['sourceCanonicalUtf8']=data.decode('ascii');row['roundLedger']=new_round;row['paidLedger']=new_paid
    t=trace(row);assert t['state']['status']=='repeated' and t['state']['world']==t['ctx']['paid']['world'];print('PASS genuine inherited seal timestamp variant without fixture allowlist')
def policy_probe_tests():
    row=cases()[0];ctx=derive(row['sourceCanonicalUtf8'].encode('ascii'),*arguments(row));s=initial(ctx);s['status']='release-completed';s['releaseCompletionReceiptId']='apc.isolated-policy-probe';a=assessment(ctx,s)
    # Isolated controls have no Source/PaidProof publication; they test pure policy only.
    assert mode(a)=='owner-choice'
    for field,reason in [('witnesses','no-continuation'),('progress','no-material-progress')]:
        isolated=copy.deepcopy(a);isolated[field]=[];assert mode(isolated)==reason
    bad=copy.deepcopy(a);bad.update(witnessOutcome='unsupported',unsupportedReason='profile');rejected(lambda:mode(bad),3)
    assert mov.cost(10,9,['contact'],None,2)==(13,3)
    assert mov.cost(10,9,['engaged'],None,2)==(15,5)
    assert mov.cost(10,5,['contact','engaged'],None,2)==(11,1)
    try:mov.cost(10,10,['engaged'],None,2)
    except mov.Invalid as error:assert error.code=='CMB-MOV-005'
    else:raise AssertionError('CP16 accepted')
    try:mov.cost(10,2**63-1,['contact'],None,2)
    except mov.Invalid as error:assert error.code=='CMB-MOV-007'
    else:raise AssertionError('overflow accepted')
    for field in ('world','members','movementEnd'):
        changed=copy.deepcopy(s)
        if field=='world':changed['world']['elements'][0]['ammunition']['points']=10
        elif field=='members':changed['members'].clear()
        else:changed['movementEnd']['completionReceiptId']='synthetic.movement-completed'
        rejected(lambda:assessment(ctx,changed),4)
    for version in (2**63-2,2**63-1):
        changed=copy.deepcopy(s);changed['stateVersion']=version;rejected(lambda:transition(ctx,changed,trusted(command(ctx,changed,'control-open'),'system',1000)),7)
    changed=copy.deepcopy(s);changed['stateVersion']=35;ctx_bad=copy.deepcopy(ctx);ctx_bad['cycle']['ordinal']=2**31-1;changed['originalCycle']=copy.deepcopy(ctx_bad['cycle']);rejected(lambda:transition(ctx_bad,changed,trusted(command(ctx_bad,changed,'control-open'),'system',1000)),7)
    for field,reason in [('progress','no-material-progress'),('movementEnd','no-continuation')]:
        probe=copy.deepcopy(ctx);state=copy.deepcopy(s);state['stateVersion']=35
        if field=='progress':probe['progress']=[]
        else:
            probe['movementEnd']['excludedBefore']=[copy.deepcopy(probe['members'][0]['unit'])];state['movementEnd']=copy.deepcopy(probe['movementEnd'])
        terminal,event,_=transition(probe,state,trusted(command(probe,state,'control-open'),'system',None))
        assert terminal['status']=='finished' and terminal['decisionId'] is None and terminal['timing'] is None and json.loads(event)['effect']['reason']==reason
    element={'currentLocationId':'axis-front'};geometry={'elements':[element,{'currentLocationId':'commonwealth-front'}],'guards':[{'currentLocationId':'axis-rear'}]}
    assert destination_blocked(geometry,element,'commonwealth-front') and destination_blocked(geometry,element,'axis-rear') and not destination_blocked(geometry,element,'axis-front')
    future=copy.deepcopy(ctx);future['paid']['world']['futureObligations']=[{'isolatedFutureDuty':'not-source-admission'}];state=copy.deepcopy(s);state['world']=copy.deepcopy(future['paid']['world']);assert assessment(future,state)['witnesses']==a['witnesses']
    pending=copy.deepcopy(ctx);pending['paid']['world']['settlements'][0]['relationships']=None;state=copy.deepcopy(s);state['world']=copy.deepcopy(pending['paid']['world']);rejected(lambda:assessment(pending,state),3)
    print('PASS isolated forced finish/proximity/guards/future versus immediate work, CP/DP ceilings and overflow')


def capacity_tests():
    model={'Source':2*paid.LIMITS['sourceBytes']+4*2*LIMITS['eventBytes']+512,'Control':paid.INVENTORY['encodedUpperBounds']['World']+65536,'Event':16384,'Input':1024,'Proof':paid.INVENTORY['encodedUpperBounds']['PaidProof']+paid.INVENTORY['encodedUpperBounds']['World']+131072,'ContinuationLedger':4*1024+5}
    caps={'Source':LIMITS['sourceBytes'],'Control':LIMITS['controlBytes'],'Event':LIMITS['eventBytes'],'Input':LIMITS['inputBytes'],'Proof':LIMITS['proofBytes'],'ContinuationLedger':LIMITS['continuationLedgerBytes']}
    for k,n in model.items():assert n<=caps[k],(k,n,caps[k])
    assert LIMITS['acceptedEvents']==4 and LIMITS['depth']==paid.LIMITS['depth']+2
    row=cases()[0];t=trace(row);p=proof(t['source'],*t['args'],t['ledger']);measured={}
    for kind,value in [('Source',json.loads(t['source'])),('Control',t['state']),('Proof',p),('Event',json.loads(t['events'][-1])),('Input',t['inputs'][-1])]:
        measured[kind]=len(raw(value,kind));assert measured[kind]<=model[kind];rejected(lambda:parse(b' '*(byte_limit(kind)+1),kind),1)
    measured['ContinuationLedger']=len(encode(t['ledger']))
    def upper(v):
        # Every non-World/paid carrier field is a fixed label or source/generated ASCII identifier;
        # no user free text is admitted. Pad all to128 bytes and all integers to19 digits.
        if type(v) is dict:return {k:(None if k in ('world','paidProof') else upper(x)) for k,x in v.items()}
        if type(v) is list:return [upper(x) for x in v]
        if type(v) is str:return 'x'*128
        if type(v) is bool:return False
        if type(v) is int:return 2**63-1
        return v
    fields={'ControlMetadata':len(encode(upper(t['state']))),'ProofMetadata':len(encode(upper(p))),'Event':len(encode(upper(json.loads(t['events'][-1])))),'Input':len(encode(upper(t['inputs'][-1])))}
    assert fields['ControlMetadata']<=65536 and fields['ProofMetadata']<=131072
    assert fields['Event']<=model['Event'] and fields['Input']<=model['Input']
    print('PASS structural capacity domination',model,'field-accounting',fields,'measured',measured)
    return dict(dominating=model,limits=caps,fieldAccounting=fields,measured=measured)
def grammar_tests():
    row=cases()[0];t=trace(row);args=t['args'];source=json.loads(t['source']);count=0
    for kind,value in [('Source',source),('Control',t['state']),('Event',json.loads(t['events'][-1])),('Input',t['inputs'][-1])]:
        good=raw(value,kind)
        for encoded in [good+b'\n',b'\xef\xbb\xbf'+good,good.replace(b':',b': ',1),good.replace(b'{',b'{"unknown":null,',1),good.replace(b'{',b'{"contractVersion":1,',1)]:rejected(lambda:parse(encoded,kind));count+=1
        for field in value:
            missing=copy.deepcopy(value);missing.pop(field);rejected(lambda:parse(encode(missing),kind),1);count+=1
        if 'contractVersion' in value:
            for version,code in [(True,1),(1.0,1),(-1,2),(2,3)]:
                bad=copy.deepcopy(value);bad['contractVersion']=version;rejected(lambda:parse(encode(bad),kind),code);count+=1
    for admitted,code in [(True,1),(-1,2),(1.0,1),(paid.MAX_UTC+1,2)]:
        bad=copy.deepcopy(t['inputs'][-1]);bad['admittedAt']=admitted;rejected(lambda:parse(encode(bad),'Input'),code);count+=1
    bad=json.loads(t['events'][-1]);bad['effect']['kind']='unknown-arm';rejected(lambda:parse(encode(bad),'Event'),3)
    bad=copy.deepcopy(source);bad['continuationEventCanonicalUtf8']*=2;rejected(lambda:parse(encode(bad),'Source'),1)
    for blob in [b'['*38+b'0'+b']'*38,b'['+b','.join([b'0']*513)+b']',b'{'+b','.join(('"k'+str(i)+'":0').encode() for i in range(129))+b'}']:rejected(lambda:parse(blob,'Source'),1)
    bad=copy.deepcopy(t['ledger']);bad[0]['admittedAt']=True;rejected(lambda:read_proof(b'{}',t['source'],*args,bad),1)
    clone=read_source(t['source'],*args,t['ledger']);clone['continuationEventCanonicalUtf8'].clear();assert len(read_source(t['source'],*args,t['ledger'])['continuationEventCanonicalUtf8'])==4
    print('PASS closed primitives/versions/arms/canonical/preparse/claim ordering',count)
def dependency_tests():
    row=cases()[0];source=source_for(row);args=arguments(row);count=0;real_read=Path.read_bytes;replay(source,*args,[])
    for path in DEPENDENCIES:
        physical=(REPOSITORY/path).resolve()
        for fault in ('changed','missing','unreadable'):
            def altered(p):
                if p.resolve()==physical:
                    if fault=='changed':return b'changed-pinned-byte'
                    raise FileNotFoundError(path) if fault=='missing' else PermissionError(path)
                return real_read(p)
            try:Path.read_bytes=altered;rejected(lambda:read_proof(b'not-json',source,*args,[]),9);count+=1
            finally:Path.read_bytes=real_read
        hits=[0]
        def after_first(p):
            if p.resolve()==physical:
                hits[0]+=1
                if hits[0]>1:return b'changed-after-first-read'
            return real_read(p)
        try:Path.read_bytes=after_first;rejected(lambda:read_source(source,*args,[]),9);count+=1
        finally:Path.read_bytes=real_read
    for path in OWN_DELIVERY:
        def altered(p):return b'changed-owned-delivery' if p.resolve()==path.resolve() else real_read(p)
        try:Path.read_bytes=altered;rejected(lambda:read_source(source,*args,[]),9);count+=1
        finally:Path.read_bytes=real_read
    path=next(iter(DEPENDENCIES));original=DEPENDENCIES[path]
    try:DEPENDENCIES[path]='0'*64;rejected(verify_dependencies,9)
    finally:DEPENDENCIES[path]=original
    print('PASS physical warm/change/missing/unreadable/afterfirstread and own delivery',len(DEPENDENCIES),count)
def fixture_artifacts():
    traces=[]
    for row in cases():
        for outcome in ('repeat','finish'):
            t=trace(row,outcome);p=proof(t['source'],*t['args'],t['ledger'])
            traces.append(dict(owner=row['owner'],firstSeal=row['firstSeal'],outcome=outcome,sourceCanonicalUtf8=t['source'].decode('ascii'),continuationLedger=t['ledger'],controlCanonicalUtf8=[x.decode('ascii') for x in t['frames']],proofCanonicalUtf8=raw(p,'Proof').decode('ascii'),sourceHash=sha(t['source']),proofHash=sha(raw(p,'Proof')),eventHashes=[sha(x) for x in t['events']]))
    return dict(contractVersion=1,dependencyManifestHash='sha256:'+identity('dependency-manifest',dict(sorted(DEPENDENCIES.items()))),traces=traces)
FIXTURE=ROOT/'fixtures/combat-actual-post-assault-continuation-v1.json'
def fixture_tests():
    expected=json.loads(FIXTURE.read_bytes());derived=fixture_artifacts();assert expected==derived;print('PASS retained literal8owner/seal/outcome traces,40cuts,32events; no regeneration')
def normal_gate():
    semantic_tests();timestamp_variant_tests();policy_probe_tests();grammar_tests();capacity_tests();dependency_tests()
    for owner in ('axis','commonwealth'):preservation_tests(owner);recovery_tests(owner);timing_tests(owner);adversarial_tests(owner)
    fixture_tests();print('PASS complete actual post-assault continuation normal gate; no native/public/parent completion')
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--group',choices=['semantic','preservation','recovery','timing','adversarial','timestamp','policy','grammar','capacity','dependencies']);parser.add_argument('--owner',choices=['axis','commonwealth']);parser.add_argument('--write-fixture',action='store_true');args=parser.parse_args()
    if args.write_fixture:require(INVENTORY['closureFrozen'],7,'/provisionalClosure');FIXTURE.write_text(json.dumps(fixture_artifacts(),indent=2)+'\n')
    elif args.group=='semantic':semantic_tests()
    elif args.group=='preservation':preservation_tests(args.owner)
    elif args.group=='recovery':recovery_tests(args.owner)
    elif args.group=='timing':timing_tests(args.owner or 'axis')
    elif args.group=='adversarial':adversarial_tests(args.owner or 'axis')
    elif args.group=='timestamp':timestamp_variant_tests()
    elif args.group=='policy':policy_probe_tests()
    elif args.group=='grammar':grammar_tests()
    elif args.group=='capacity':capacity_tests()
    elif args.group=='dependencies':dependency_tests()
    else:normal_gate()

if __name__=='__main__':main()
