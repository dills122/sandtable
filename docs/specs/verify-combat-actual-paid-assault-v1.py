#!/usr/bin/env python3
"""Private actual paid assault reference: full authenticated replay and settlement."""
import argparse
import copy
import hashlib
import importlib.util
import json
import re
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
REPOSITORY = ROOT.parent.parent
INVENTORY = json.loads((ROOT/'combat-actual-paid-assault-v1.schema.json').read_bytes())

class Invalid(ValueError):
    def __init__(self, code, path=''):
        self.code, self.path = f'CMB-APA-{code:03}', path
        super().__init__(self.code+' '+(path or '/'))

def require(ok, code, path=''):
    if not ok: raise Invalid(code,path)

def encode(value): return json.dumps(value,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(data): return 'sha256:'+hashlib.sha256(data).hexdigest()
def digest(kind,value):
    return hashlib.sha256(INVENTORY['domains'][kind].encode('ascii')+b'\0'+encode(value)).hexdigest()

SCHEMA={k:[tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['objects'].items()}
LIMITS=INVENTORY['limits']
DEPENDENCIES=dict(INVENTORY['runtimeSourceHashes'])
PACKAGE_MANIFEST_BYTES=encode(dict(sorted(DEPENDENCIES.items())))
MAX_UTC=253402300799999

def consume(path):
    require(path in DEPENDENCIES,9,path)
    try: data=(REPOSITORY/path).read_bytes()
    except OSError as error:raise Invalid(9,path) from error
    require(hashlib.sha256(data).hexdigest()==DEPENDENCIES[path],9,path)
    return data

def verify_dependencies():
    require(encode(dict(sorted(DEPENDENCIES.items())))==PACKAGE_MANIFEST_BYTES,9,'/packagedManifest')
    for path,expected in DEPENDENCIES.items():
        require(type(path) is str and path.isascii() and not path.startswith('/') and '\\' not in path and all(part not in ('','.','..') for part in path.split('/')) and re.fullmatch(r'[A-Za-z0-9._/-]+',path) is not None and type(expected) is str and re.fullmatch(r'[0-9a-f]{64}',expected) is not None,9,'/manifestGrammar')
        consume(path)

def dependency_manifest_hash():
    verify_dependencies()
    return 'sha256:'+hashlib.sha256(INVENTORY['domains']['dependency-manifest'].encode('ascii')+b'\0'+PACKAGE_MANIFEST_BYTES).hexdigest()

# Owned schema is a delivery artifact, not a self-referential external pin.
OWN_DELIVERY={p:sha(p.read_bytes()) for p in (ROOT/'combat-actual-paid-assault-v1.schema.json',ROOT/'verify-combat-actual-paid-assault-v1.py')}
def verify_delivery():
    for path,expected in OWN_DELIVERY.items():
        try:data=path.read_bytes()
        except OSError as error:raise Invalid(9,'/ownDelivery') from error
        require(sha(data)==expected,9,'/ownDelivery')


verify_dependencies()
aad_path=ROOT/'verify-combat-actual-assault-v1.py'
spec=importlib.util.spec_from_file_location('paid_admission',aad_path)
aad=importlib.util.module_from_spec(spec);spec.loader.exec_module(aad)
result_inventory=json.loads(consume('docs/specs/combat-result-settlement-v2.schema.json'))
require(INVENTORY['embeddedDomains']=={k:result_inventory['domains'][k] for k in ('result','settlement')},9,'/embeddedDomains')
for kind in ('Draw','AssaultResult'):
    require([k for k,_ in SCHEMA[kind]]==[x.split(':')[0] for x in result_inventory['objects'][kind].split()],9,'/embeddedGrammar')
verify_dependencies()

def typed(v,kind,path='',depth=0,parent='',field=''):
    require(depth<=LIMITS['depth'],1,path)
    if kind.endswith('?'):
        if v is not None:typed(v,kind[:-1],path,depth,parent,field)
    elif kind=='PaidEffect':
        require(type(v) is dict and type(v.get('kind')) is str,1,path)
        require(v['kind'] in INVENTORY['effectTags'],3,path)
        typed(v,INVENTORY['effectTags'][v['kind']],path,depth)
    elif kind in SCHEMA:
        fields=SCHEMA[kind];require(type(v) is dict and set(v)=={k for k,_ in fields},1,path)
        for k,t in fields:typed(v[k],t,path+'/'+k,depth+1,kind,k)
        for k,n in INVENTORY['versionRules'].get(kind,{}).items():require(v[k]==n,3,path+'/'+k)
    elif kind.endswith('[]') or kind=='Route':
        cap=LIMITS['routes'] if kind=='Route' else INVENTORY['arrayBounds'].get(parent+'.'+field,LIMITS['ordinaryArrayItems'])
        require(type(v) is list and len(v)<=cap,1,path)
        for i,x in enumerate(v):typed(x,'id' if kind=='Route' else kind[:-2],f'{path}/{i}',depth+1)
    elif kind=='byte':require(type(v) is int,1,path);require(0<=v<=255,2,path)
    elif kind=='signedInt':require(type(v) is int,1,path);require(-2**31<=v<2**31,2,path)
    elif kind=='utf8':
        require(type(v) is str and v.isascii() and 0<len(v)<=LIMITS['sourceBytes'],1,path)
    elif kind in ('string','text'):
        require(type(v) is str,1,path);require(len(v)<=LIMITS['stringCharacters'],2,path)
    else:
        try:aad.typed(v,kind,path,0)
        except aad.Invalid as error:raise Invalid(int(error.code[-3:]),error.path) from error


def canonical(v,kind):
    if kind.endswith('?'):return None if v is None else canonical(v,kind[:-1])
    if kind=='PaidEffect':return canonical(v,INVENTORY['effectTags'][v['kind']])
    if kind in SCHEMA:return {k:canonical(v[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'):return [canonical(x,kind[:-2]) for x in v]
    if kind=='Route':return list(v)
    if kind in ('byte','signedInt','utf8','string','text'):return v
    return aad.canonical(v,kind)


def byte_limit(kind):
    return {'PaidSource':LIMITS['sourceBytes'],'PaidProof':LIMITS['proofBytes'],'PaidControl':LIMITS['controlBytes'],'PaidEvent':LIMITS['eventBytes'],'PaidInput':LIMITS['inputBytes'],'PaidCommand':LIMITS['inputBytes'],'AssaultResult':LIMITS['eventBytes'],'World':LIMITS['controlBytes']}.get(kind,LIMITS['proofBytes'])

def raw(v,kind):
    typed(v,kind);data=encode(canonical(v,kind));require(len(data)<=byte_limit(kind),1,'/'+kind)
    return data

def preparse(data):
    # Root depth is zero, matching inherited typed validation. Quoted embedded
    # carriers are atomic here and retain their own downstream parser bounds.
    stack=[];quoted=False;escaped=False
    for c in data:
        if quoted:
            if escaped:escaped=False
            elif c==92:escaped=True
            elif c==34:quoted=False
            continue
        if c in (9,10,13,32):continue
        if c==34:
            if stack:stack[-1][2]=True
            quoted=True;continue
        if c in (123,91):
            if stack:stack[-1][2]=True
            stack.append([c,0,False]);require(len(stack)<=LIMITS['depth']+1,1,'/depth');continue
        if c in (125,93):
            require(stack and (stack[-1][0],c) in ((123,125),(91,93)),1,'/json')
            start,commas,occupied=stack.pop();items=commas+int(occupied)
            require(items<=(LIMITS['ordinaryArrayItems'] if start==91 else 128),1,'/arrayOrFields');continue
        if c==44 and stack:
            stack[-1][1]+=1
            require(stack[-1][1]<(LIMITS['ordinaryArrayItems'] if stack[-1][0]==91 else 128),1,'/arrayOrFields')
        elif stack:stack[-1][2]=True
    require(not quoted and not stack,1,'/json')


def parse(data,kind):
    require(type(data) is bytes and 0<len(data)<=byte_limit(kind),1,'/'+kind)
    preparse(data)
    def pairs(items):
        value={}
        for key,child in items:require(key not in value,1,'/'+key);value[key]=child
        return value
    try:value=json.loads(data.decode('ascii'),object_pairs_hook=pairs,parse_constant=lambda _:(_ for _ in ()).throw(ValueError()))
    except (ValueError,UnicodeError,RecursionError) as error:
        if isinstance(error,Invalid):raise
        raise Invalid(1,'/'+kind) from error
    require(raw(value,kind)==data,8,'/'+kind);return value

def parse_source(data):return parse(data,'PaidSource')

def context(base):
    a=base['admissionProof']['actualRoundProof'];b=a['base']['actualSelectionProof']['boundary']
    selection=a['base']['actualSelectionProof']['control']['selection']
    consume('docs/specs/fixtures/combat-content-v7.canonical.json')
    registry=aad.are.selection.env.Context()
    rules=aad.normalized_rules()
    return dict(base=base,prior=a['control'],boundary=b,selection=selection,cycle=b['cycle'],rules=rules,config=registry.config,graph=copy.deepcopy(aad.are.selection.env.world.GRAPH),anchors=copy.deepcopy(aad.are.selection.env.world.ANCHORS))

def initial(ctx):
    p=ctx['prior']
    require(p['randomState']['seed']==1 and p['randomState']['nextByteCursor']==2,4,'/randomState')
    return dict(contractVersion=1,baseHash='sha256:'+digest('base',ctx['base']),roundClockConfigurationHash=p['clockConfigurationHash'],resultClockPolicyId=INVENTORY['resultClockPolicyId'],stateVersion=25,prefix=p['prefix'],status='prepared',commitmentId=None,result=None,settlementId=None,window=None,acceptedHighWater=p['timing']['openingFloorUnixMilliseconds'],world=copy.deepcopy(p['world']),randomState=copy.deepcopy(p['randomState']),attackHistory=[],targetUses=[],roundClosureReceiptId=None,caCompletionReceiptId=None,receipts=[],closed=False)


def ingress(data,selection_ledger,round_ledger,paid_ledger):
    verify_delivery();verify_dependencies();source=parse_source(data)
    require(len(source['admissionSourceCanonicalUtf8'])<=aad.MAX_BYTES,1,'/admissionSourceCanonicalUtf8')
    try:aad.ledgers(selection_ledger,round_ledger,[])
    except aad.Invalid as error:raise Invalid(int(error.code[-3:]),error.path) from error
    require(type(paid_ledger) is list and len(paid_ledger)<=LIMITS['acceptedEvents'],1,'/paidLedger')
    owned=[]
    for i,x in enumerate(paid_ledger):typed(x,'PaidInput',f'/paidLedger/{i}');owned.append(parse(raw(x,'PaidInput'),'PaidInput'))
    require(len(encode(owned))<=LIMITS['paidLedgerBytes'],1,'/paidLedger')
    require(len(owned)==len(source['paidEventCanonicalUtf8']),4,'/paidLedger')
    try:aad.parse(source['admissionSourceCanonicalUtf8'].encode('ascii'),'AdmissionSource')
    except aad.Invalid as error:raise Invalid(int(error.code[-3:]),'/admissionSourceCanonicalUtf8'+error.path) from error
    try:p=aad.proof(source['admissionSourceCanonicalUtf8'].encode('ascii'),selection_ledger,round_ledger,[])
    except aad.Invalid as error:
        code=int(error.code[-3:]);raise Invalid(9 if code==9 else 4,error.path) from error
    base=dict(contractVersion=1,dependencyManifestHash=dependency_manifest_hash(),admissionProof=p,resultClockPolicyId=INVENTORY['resultClockPolicyId'])
    return source,base,owned


def admitted(data,selection_ledger,round_ledger,paid_ledger):
    source,base,owned=ingress(data,selection_ledger,round_ledger,paid_ledger)
    return source,base


def component(e,ctx):
    side=next(ctx['selection'][r]['unit']['originalSide'] for r in ('attacker','defender') if ctx['selection'][r]['unit']['elementId']==e['elementId'])
    return dict(unit=dict(creationBinding=ctx['prior']['world']['creationBinding'],originalSide=side,elementId=e['elementId']),componentId=e['components'][0]['componentId'])

def route(ctx,origin,destination):
    queue=[(origin,[origin])];seen={origin}
    for node,path in queue:
        if node==destination:return path
        for other in sorted(ctx['graph'][node]):
            if other not in seen:seen.add(other);queue.append((other,path+[other]))
    raise Invalid(7,'/route')

def result_facts(rules,diff,ac,dc,die):
    lookup={(x['role'],x['differential'],x['coordinate']):x['lossPercent'] for x in rules['losses']}
    effect=next(x for x in rules['effects'] if x['differential']==diff)
    asum,dsum=ac//10+ac%10,dc//10+dc%10
    ca,cd=asum in effect['attackerCaptureSums'],dsum in effect['defenderCaptureSums']
    require(not(ca and cd),7,'/captureRole')
    role='attacker' if ca else 'defender' if cd else None
    require((die is not None)==(role is not None),6,'/captureDie')
    return dict(differential=diff,attackerCoordinate=ac,defenderCoordinate=dc,captureDie=die,attackerPercent=lookup[('attacker',diff,ac)],defenderPercent=lookup[('defender',diff,dc)],rawEngaged=asum in effect['attackerEngagedSums'],requiredRetreat=int(dsum in effect['defenderRetreatOneHexSums']),capturedRole=role,captureShare=next(x['percent'] for x in rules['captureShares'] if x['die']==die) if die else 0)


def cp(n):return dict(numerator=n,denominator=1)

def project(ctx,paid,result,cut,retreat_choice=None,custody_choice=None):
    consume('docs/specs/fixtures/combat-content-v7.canonical.json')
    w=copy.deepcopy(paid);sid='set.'+hashlib.sha256(INVENTORY['embeddedDomains']['settlement'].encode()+b'\0'+encode(dict(commitmentId=result['commitmentId'],resultId=result['resultId']))).hexdigest()
    facts=result['facts'];cycle=ctx['cycle'];scope=dict(gameTurn=cycle['gameTurn'],operationStage=cycle['operationStage'])
    elements={e['elementId']:e for e in w['elements']}
    attacker=elements[ctx['selection']['attacker']['unit']['elementId']];defender=elements[ctx['selection']['defender']['unit']['elementId']]
    frozen=copy.deepcopy(w['elements']);frozen_by_id={e['elementId']:e for e in frozen}
    st=dict(settlementId=sid,commitmentId=result['commitmentId'],resultId=result['resultId'],**scope,attacker=copy.deepcopy(ctx['selection']['attacker']['unit']),defender=copy.deepcopy(ctx['selection']['defender']['unit']),preLossElements=frozen,result=copy.deepcopy(facts),disposition=None,losses=None,retreat=None,custody=None,relationships=None)
    require(not w['settlements'],7,'/preLossSettlements');w['settlements'].append(st)
    if cut=='resolved':return w
    required=facts['requiredRetreat'];choice=retreat_choice or 'not-required'
    require(choice in (('retreat','refuse-retreat') if required else ('not-required',)),6,'/retreatChoice')
    origin=defender['currentLocationId'];planned=int(choice=='retreat')
    path=route(ctx,origin,ctx['anchors'][st['defender']['originalSide']])[:2] if planned else [origin]
    require(not planned or len(path)==2,7,'/retreatRoute')
    st['disposition']=dict(receiptId=sid+'.disposition',predecessorReceiptId=result['resultId'],kind=choice,requiredDistance=required,plannedDistance=planned,unfulfilledDistance=required-planned,route=path)
    if cut=='disposition':return w
    def cause(e,receipt,kind,points):
        if not points:return
        before=e['operationalState']['cohesionLevel'];after=min(10,before+points) if kind=='assault-victory-rp' else before-points
        require(-2**31<=after<2**31,7,'/cohesionOverflow')
        e['operationalState']['cohesionLevel']=after
        side=component(e,ctx)['unit']['originalSide']
        w['cohesionCauses'].append(dict(causeId=sid+'.'+kind+'.'+side,ordinal=len(w['cohesionCauses'])+1,receiptId=receipt,elementId=e['elementId'],**scope,kind=kind,points=points,before=before,after=after))
    roles=[]
    for r,e in (('attacker',attacker),('defender',defender)):
        toe=frozen_by_id[e['elementId']]['components'][0]['currentToe'];require(toe==10,7,'/preLossToe')
        percent=facts[r+'Percent'];refusal=10*(required-planned) if r=='defender' else 0
        loss=(toe*(percent+refusal)+99)//100 if r=='attacker' else toe*(percent+refusal)//100
        captured=(loss*facts['captureShare']+99)//100 if facts['capturedRole']==r else 0
        require(0<=captured<=loss<=toe,7,'/conservation')
        roles.append(dict(role=r,component=component(e,ctx),committedToe=toe,tablePercent=percent,refusalPercent=refusal,lossToe=loss,capturedToe=captured,otherLossToe=loss-captured,remainingToe=toe-loss,lossDp=3 if loss>=3 else 0))
        e['components'][0]['currentToe']=toe-loss;cause(e,sid+'.losses','loss-dp',3 if loss>=3 else 0)
    st['losses']=dict(receiptId=sid+'.losses',predecessorReceiptId=sid+'.disposition',roles=roles)
    captured=next((r for r in roles if r['capturedToe']),None)
    if captured:
        victim=attacker if captured['role']=='attacker' else defender;captor=defender if victim is attacker else attacker
        lot=dict(lotId=sid+'.captives',lossReceiptId=sid+'.losses',originalComponent=component(victim,ctx),captor=component(captor,ctx)['unit'],quantity=captured['capturedToe'],originLocationId=frozen_by_id[victim['elementId']]['currentLocationId'],currentLocationId=frozen_by_id[victim['elementId']]['currentLocationId'],status='pending',guardId=None,escapeReceiptId=None)
        w['custodyLots'].append(lot)
    if cut=='losses':return w
    before=defender['operationalState']['capabilityPointsExpended'];require(before['denominator']==1,7,'/retreatCp')
    n=before['numerator'];after=n+planned;require(after<2**63,7,'/retreatCp')
    excess=max(0,after-10)-max(0,n-10)
    defender['operationalState']['capabilityPointsExpended']=cp(after);defender['currentLocationId']=path[-1]
    for rep in w['representations']:
        if rep['boundElementIds']==[defender['elementId']]:rep['currentLocationId']=path[-1]
    cause(defender,sid+'.retreat','retreat-excess-dp',excess);cause(attacker,sid+'.retreat','assault-victory-rp',3 if planned else 0)
    st['retreat']=dict(receiptId=sid+'.retreat',predecessorReceiptId=sid+'.losses',kind=choice,route=path,completedDistance=planned,beforeCp=cp(n),afterCp=cp(after),excessCpDp=excess,attackerVictoryRp=3 if planned else 0)
    if cut=='retreat':return w
    if captured:
        require(custody_choice in ('relocate-and-guard','leave-unguarded'),6,'/custodyChoice')
        guarded=custody_choice=='relocate-and-guard';destination=captor['currentLocationId'] if guarded else victim['currentLocationId']
        custody_route=route(ctx,lot['originLocationId'],destination)
        require(len(custody_route)-1<=(3 if guarded else 8),7,'/custodyRoute')
        donor=captor['components'][0]['currentToe']
        if guarded:
            require(donor>=1 and victim['currentLocationId'] not in custody_route[1:],7,'/guardDonorRoute')
            captor['components'][0]['currentToe']-=1
            guard=dict(guardId=sid+'.guard',formationReceiptId=sid+'.custody',lotId=lot['lotId'],originComponent=component(captor,ctx),currentLocationId=destination,toe=1,baseCapabilityPointAllowance=10,offensiveCloseAssaultRating=0,defensiveCloseAssaultRating=1,operationalState=copy.deepcopy(captor['operationalState']),ammunition=copy.deepcopy(captor['ammunition']),readiness=copy.deepcopy(captor['readiness']))
            w['guards'].append(guard);lot.update(status='guarded',currentLocationId=destination,guardId=guard['guardId'])
            w['futureObligations'].append(dict(obligationId=sid+'.upkeep',receiptId=sid+'.custody',kind='guard-priority-upkeep',subjectId=guard['guardId'],earnedScope=scope,eligibleScope=None,activationGate='before-prisoner-upkeep-or-guard-action',status='retained-unimplemented'))
        else:
            ordinal=(scope['gameTurn']-1)*3+scope['operationStage']-1+12
            eligible=dict(gameTurn=ordinal//3+1,operationStage=ordinal%3+1)
            entitlement=dict(entitlementId=sid+'.replacement',escapeReceiptId=sid+'.custody',lotId=lot['lotId'],originalComponent=component(victim,ctx),quantity=lot['quantity'],reunionLocationId=destination,earnedScope=scope,delayOperationStages=12,eligibleScope=eligible,status='awaiting-eligibility-and-training')
            w['replacementEntitlements'].append(entitlement);lot.update(status='escaped',currentLocationId=None,escapeReceiptId=sid+'.custody')
            w['futureObligations'].append(dict(obligationId=sid+'.training',receiptId=sid+'.custody',kind='replacement-training-gate',subjectId=entitlement['entitlementId'],earnedScope=scope,eligibleScope=eligible,activationGate='before-replacement-eligibility-training',status='retained-unimplemented'))
        st['custody']=dict(receiptId=sid+'.custody',predecessorReceiptId=sid+'.retreat',lotId=lot['lotId'],kind=custody_choice,route=custody_route,guardId=sid+'.guard' if guarded else None,entitlementId=None if guarded else sid+'.replacement',donorToeBefore=donor,donorToeAfter=captor['components'][0]['currentToe'])
    else:require(custody_choice is None and cut!='custody',6,'/custody')
    if cut=='custody':return w
    adjacent=defender['currentLocationId'] in ctx['graph'][attacker['currentLocationId']]
    relation=('engaged' if facts['rawEngaged'] and not required else 'contact') if adjacent and all(e['components'][0]['currentToe']>0 for e in (attacker,defender)) else None
    if relation:w['relationships'].append(dict(relationId=sid+'.relation',creationReceiptId=sid+'.relationships',kind=relation,attacker=st['attacker'],defender=st['defender'],**scope,active=True,endedByReceiptId=None,endingCause=None))
    st['relationships']=dict(receiptId=sid+'.relationships',predecessorReceiptId=sid+('.custody' if captured else '.retreat'),relationshipId=sid+'.relation' if relation else None,kind=relation)
    return w

SUPPORTED_HANDLERS=set(INVENTORY['supportHandlers'])
FRESH_ENABLED=True

def paid_world(ctx):
    p=ctx['prior'];w=copy.deepcopy(p['world']);costs=[]
    require(not p['attackHistory'] and not p['targetUses'] and not w['settlements'] and not w['custodyLots'],7,'/paidBoundary')
    for r,cost in (('attacker',5),('defender',3)):
        allocation=next(x['allocation'] for x in p['slots'] if x['role']==r)
        unit=ctx['selection'][r]['unit'];require(allocation['unit']==unit and allocation['committedToe']==10,7,'/allocation')
        e=next(x for x in w['elements'] if x['elementId']==unit['elementId']);before=e['operationalState']['capabilityPointsExpended']
        require(before['denominator']==1 and before['numerator']==0 and e['ammunition']['points']==10 and e['components'][0]['currentToe']==10,7,'/resources')
        e['operationalState']['capabilityPointsExpended']=cp(before['numerator']+cost);e['ammunition']['points']=0
        costs.append(dict(unit=copy.deepcopy(unit),beforeCp=before['numerator'],afterCp=before['numerator']+cost,beforeAmmo=10,afterAmmo=0))
    return w,costs

def make_window(ctx,kind,owner,sid,now,available):
    if not available or now is None:return None
    budget=next(x['decisionBudgetMilliseconds'] for x in ctx['config']['windows'] if x['kind']==kind)
    if now>MAX_UTC-budget:return None
    timing=dict(contractVersion=1,configHash=ctx['cycle']['admittedPolicyBundleDigest'],kind=kind,decisionBudgetMilliseconds=budget,openedAtUnixMilliseconds=now,deadlineUnixMilliseconds=now+budget,highWaterUnixMilliseconds=now)
    value=dict(decisionId=sid+'.choice.'+kind,kind=kind,owner=owner,timing=timing);typed(value,'ChoiceWindow');return value


def certify_support(ctx):
    require(SUPPORTED_HANDLERS==set(INVENTORY['supportHandlers']),7,'/supportHandlers')
    for kind,key in (('PaidSource','sourceBytes'),('PaidProof','proofBytes'),('PaidControl','controlBytes'),('PaidEvent','eventBytes'),('PaidInput','inputBytes'),('PaidLedger','paidLedgerBytes')):
        require(LIMITS[key]>=INVENTORY['encodedUpperBounds'][kind],7,'/supportCapacity/'+kind)
    require(LIMITS['acceptedEvents']>=11 and LIMITS['acceptedReceipts']>=11 and LIMITS['draws']>=9 and LIMITS['consumedBytesPerDraw']>=512 and LIMITS['routes']>=9,7,'/supportCollectionCapacity')
    w,_=paid_world(ctx);summaries={};counts={'morale':0,'assault-cell':0,'capture-refusal':0};coverage={k:set() for k in counts}
    morale={x['coordinate']:x['adjustment'] for x in ctx['rules']['morale']}
    for ordinal,row in enumerate(aad.catalogue_records(ctx['rules'])):
        require(type(row) is dict and row.get('kind') in counts and row.get('ordinal')==ordinal,7,'/supportIndex')
        try:aad.typed(row,aad.KINDS[row['kind']])
        except aad.Invalid as error:raise Invalid(7,'/supportShape') from error
        counts[row['kind']]+=1
        key=(row['attackerMoraleCoordinate'],row['defenderMoraleCoordinate']) if row['kind']=='morale' else (row['differential'],row['attackerCoordinate'],row['defenderCoordinate']) if row['kind']=='assault-cell' else tuple(row['facts'][k] for k in ('differential','attackerCoordinate','defenderCoordinate','captureDie'))+(row['refusalHexes'],)
        require(key not in coverage[row['kind']],7,'/supportDuplicate');coverage[row['kind']].add(key)
        if row['kind']=='morale':
            ac,dc=row['attackerMoraleCoordinate'],row['defenderMoraleCoordinate']
            require(ac in morale and dc in morale and row['attackerAdjustment']==morale[ac] and row['defenderAdjustment']==morale[dc] and row['differential']==morale[ac]-morale[dc],7,'/supportMorale')
            continue
        if row['kind']=='assault-cell':
            effect=next(x for x in ctx['rules']['effects'] if x['differential']==row['differential'])
            ac,dc=row['attackerCoordinate'],row['defenderCoordinate']
            captured=ac//10+ac%10 in effect['attackerCaptureSums'] or dc//10+dc%10 in effect['defenderCaptureSums']
            expected=result_facts(ctx['rules'],row['differential'],ac,dc,1 if captured else None)
            require(all(row[k]==expected[k] for k in ('differential','attackerCoordinate','defenderCoordinate','attackerPercent','defenderPercent','rawEngaged','requiredRetreat','capturedRole')) and row['captureTriggered']==captured,7,'/supportAssaultCell')
            continue
        facts=row['facts']
        require(result_facts(ctx['rules'],facts['differential'],facts['attackerCoordinate'],facts['defenderCoordinate'],facts['captureDie'])==facts,7,'/supportFacts')
        require(0<=row['refusalHexes']<=facts['requiredRetreat'],7,'/supportRefusal')
        a=(10*facts['attackerPercent']+99)//100;d=10*(facts['defenderPercent']+10*row['refusalHexes'])//100
        ca=(a*facts['captureShare']+99)//100 if facts['capturedRole']=='attacker' else 0
        cd=(d*facts['captureShare']+99)//100 if facts['capturedRole']=='defender' else 0
        expected=dict(attackerLossToe=a,defenderLossToe=d,attackerCapturedToe=ca,defenderCapturedToe=cd,attackerOtherLossToe=a-ca,defenderOtherLossToe=d-cd,attackerRemainingToe=10-a,defenderRemainingToe=10-d,attackerLossDp=3 if a>=3 else 0,defenderLossDp=3 if d>=3 else 0)
        require(all(row[k]==v for k,v in expected.items()),7,'/supportLossArithmetic')
        key=tuple(facts[x] for x in ('attackerPercent','defenderPercent','rawEngaged','requiredRetreat','capturedRole','captureShare'))+(row['refusalHexes'],)
        summaries[key]=row
    require(counts=={'morale':1296,'assault-cell':6480,'capture-refusal':8840},7,'/supportCoverage')
    coordinates=set(morale)
    require(coverage['morale']=={(a,d) for a in coordinates for d in coordinates},7,'/supportMoraleCoverage')
    expected_cells={(e['differential'],a,d) for e in ctx['rules']['effects'] for a in coordinates for d in coordinates}
    require(coverage['assault-cell']==expected_cells,7,'/supportCellCoverage')
    expected_variants=set()
    for diff,a,d in expected_cells:
        effect=next(e for e in ctx['rules']['effects'] if e['differential']==diff)
        captured=a//10+a%10 in effect['attackerCaptureSums'] or d//10+d%10 in effect['defenderCaptureSums']
        for die in range(1,7) if captured else (None,):
            facts=result_facts(ctx['rules'],diff,a,d,die)
            for refusal in range(facts['requiredRetreat']+1):expected_variants.add((diff,a,d,die,refusal))
    require(coverage['capture-refusal']==expected_variants,7,'/supportVariantCoverage')
    for row in summaries.values():
        facts=row['facts'];choice='not-required' if not facts['requiredRetreat'] else 'refuse-retreat' if row['refusalHexes'] else 'retreat'
        captured=row['attackerCapturedToe']+row['defenderCapturedToe']>0
        result=dict(resultId='res.'+'f'*64,commitmentId='cmt.'+'f'*64,facts=facts)
        for custody in ('relocate-and-guard','leave-unguarded') if captured else (None,):
            for cut in ('resolved','disposition','losses','retreat','custody','relationships'):
                if cut=='custody' and not captured:continue
                world=project(ctx,w,result,cut,choice,custody);raw(world,'World')
            require(sum(x['components'][0]['currentToe'] for x in world['elements'])+sum(x['toe'] for x in world['guards'])+row['attackerLossToe']+row['defenderLossToe']==20,7,'/supportConservation')
    for kind in ('retreat','custody'):
        fallback=next(x['fallback'] for x in ctx['config']['windows'] if x['kind']==kind)
        require(fallback==('refuse-retreat' if kind=='retreat' else 'leave-unguarded'),7,'/fallback')
        for owner in ('axis','commonwealth'):
            require(make_window(ctx,kind,owner,'set.'+'f'*64,10000,True) is not None,7,'/windowSupport')
            require(make_window(ctx,kind,owner,'set.'+'f'*64,None,True) is None and make_window(ctx,kind,owner,'set.'+'f'*64,MAX_UTC,True) is None,7,'/windowFallbackSupport')
    require(ctx['prior']['randomState']['seed']==1 and ctx['prior']['randomState']['nextByteCursor']==2,7,'/rngCapacity')
    return counts


def resolve_result(ctx,s):
    consume('docs/research/verify-combat-rng.py')
    spec=importlib.util.spec_from_file_location('paid_rng',REPOSITORY/'docs/research/verify-combat-rng.py');rng=importlib.util.module_from_spec(spec);spec.loader.exec_module(rng)
    before=copy.deepcopy(s['randomState']);cursor=before['nextByteCursor'];draws=[];dice=[]
    for label in ctx['rules']['procedure']['orderedPurposes']:
        try:die,end,consumed=rng.roll(before['seed'],cursor)
        except (ValueError,OverflowError) as error:raise Invalid(7,'/randomState') from error
        require(len(consumed)<=LIMITS['consumedBytesPerDraw'],7,'/drawCapacity')
        draws.append(dict(purpose=label,beforeCursor=cursor,afterCursor=end,die=die,consumed=list(consumed)));dice.append(die);cursor=end
    require(len(dice)==8,7,'/procedure')
    ac,dc=10*dice[0]+dice[1],10*dice[2]+dice[3]
    morale={x['coordinate']:x['adjustment'] for x in ctx['rules']['morale']};am,dm=morale[ac],morale[dc];diff=am-dm
    effect=next(x for x in ctx['rules']['effects'] if x['differential']==diff)
    a,d=10*dice[4]+dice[5],10*dice[6]+dice[7]
    role='attacker' if a//10+a%10 in effect['attackerCaptureSums'] else 'defender' if d//10+d%10 in effect['defenderCaptureSums'] else None
    capture=None
    if role:
        try:capture,end,consumed=rng.roll(before['seed'],cursor)
        except (ValueError,OverflowError) as error:raise Invalid(7,'/randomState') from error
        require(len(consumed)<=LIMITS['consumedBytesPerDraw'],7,'/drawCapacity')
        draws.append(dict(purpose=role+'.capture.share',beforeCursor=cursor,afterCursor=end,die=capture,consumed=list(consumed)));cursor=end
    facts=result_facts(ctx['rules'],diff,a,d,capture)
    v=dict(commitmentId=s['commitmentId'],rulesInputHash=ctx['base']['admissionProof']['arithmeticAssessment']['rulesInputHash'],procedureId=ctx['rules']['procedure']['procedureId'],beforeRandomState=before,afterRandomState=before|dict(nextByteCursor=cursor),draws=draws,attackerMoraleCoordinate=ac,defenderMoraleCoordinate=dc,attackerMorale=am,defenderMorale=dm,basicDifferential=0,facts=facts)
    result=dict(resultId='res.'+hashlib.sha256(INVENTORY['embeddedDomains']['result'].encode()+b'\0'+encode(v)).hexdigest(),**v);raw(result,'AssaultResult');return result


def projected(ctx,s,cut,retreat_choice=None,custody_choice=None):
    st=s['world']['settlements'][0] if s['world']['settlements'] else None
    retreat_choice=retreat_choice or (st['disposition']['kind'] if st and st['disposition'] else None)
    custody_choice=custody_choice or (st['custody']['kind'] if st and st['custody'] else None)
    paid,_=paid_world(ctx)
    return project(ctx,paid,s['result'],cut,retreat_choice,custody_choice)


def transition(ctx,prior,inp,fresh=True):
    verify_delivery();verify_dependencies();typed(prior,'PaidControl');typed(inp,'PaidInput');cmd=inp['command'];kind=cmd['kind'];actor=inp['actor'];now=inp['admittedAt']
    require(kind in INVENTORY['commands'],3,'/kind')
    require(cmd['contractVersion']==1 and cmd['baseHash']==prior['baseHash'] and cmd['roundId']==ctx['prior']['roundId'],4,'/context')
    structural=kind in ('commit','resolve','advance')
    require((cmd['expectedPriorVersion'] is not None)==structural and (cmd['choice'] is not None)==(kind=='choose'),3,'/command')
    require(actor in ('axis','commonwealth') if kind=='choose' else actor=='system',4,'/actor')
    require(cmd['commitmentId']==(None if kind=='commit' else prior['commitmentId']),4,'/commitmentId')
    require((cmd['decisionId'] is None)==structural,3,'/decisionId')
    ch=sha(raw(cmd,'PaidCommand'));retained=next((r for r in prior['receipts'] if r['commandHash']==ch and r['actor']==actor),None)
    if retained:return copy.deepcopy(prior),None,retained['receiptId']
    window=prior['window']
    if kind in ('expire','unavailable') and (window is None or cmd['decisionId']!=window['decisionId']):return copy.deepcopy(prior),None,None
    require(not prior['closed'] and len(prior['receipts'])<LIMITS['acceptedReceipts'] and prior['stateVersion']<2**63-1,7,'/transitionCapacity')
    require(cmd['expectedPriorVersion']==prior['stateVersion'] if structural else window is not None and cmd['decisionId']==window['decisionId'],6,'/priorVersionOrDecision')
    s=copy.deepcopy(prior);author=actor;effect=None
    def choice_record(choice,reason,timing):
        nonlocal effect
        wk=window['kind'] if window else ('retreat' if s['status']=='resolved' else 'custody')
        cut='disposition' if wk=='retreat' else 'custody'
        s['world']=projected(ctx,s,cut,choice if wk=='retreat' else None,choice if wk=='custody' else None)
        effect=dict(kind='disposition-recorded' if wk=='retreat' else 'custody-settled',reason=reason,timing=timing,payload=s['world']['settlements'][0][cut]);s['status']=cut;s['window']=None
    if kind=='commit':
        require(prior['status']=='prepared' and now is None and inp['clockAvailable'],6,'/commit')
        if fresh:require(FRESH_ENABLED,7,'/freshAdmission');certify_support(ctx)
        paid,costs=paid_world(ctx);allocations=[copy.deepcopy(x['allocation']) for x in ctx['prior']['slots']]
        preimage=dict(baseHash=prior['baseHash'],priorVersion=prior['stateVersion'],priorPrefix=prior['prefix'],roundId=ctx['prior']['roundId'],allocations=allocations,costs=costs,preResultRandomState=copy.deepcopy(prior['randomState']))
        raw(preimage,'CommitmentPreimage');commitment='cmt.'+digest('commitment',preimage)
        effect=dict(kind='attack-committed',commitmentId=commitment,allocations=allocations,costs=costs,preResultRandomState=copy.deepcopy(prior['randomState']))
        s['world']=paid;s['commitmentId']=commitment;s['status']='committed'
        s['attackHistory']=[dict(commitmentId=commitment,cycleId=ctx['boundary']['cycleId'],segmentId=ctx['prior']['segmentId'],attacker=copy.deepcopy(ctx['selection']['attacker']['unit']),defender=copy.deepcopy(ctx['selection']['defender']['unit']),targetLocationId=ctx['selection']['targetLocationId'],gameTurn=ctx['cycle']['gameTurn'],operationStage=ctx['cycle']['operationStage'])]
        s['targetUses']=[dict(commitmentId=commitment,segmentId=ctx['prior']['segmentId'],targetLocationId=ctx['selection']['targetLocationId'])]
    elif kind in ('choose','expire','unavailable'):
        require(window is not None,6,'/window');choices=('retreat','refuse-retreat') if window['kind']=='retreat' else ('relocate-and-guard','leave-unguarded')
        if kind=='choose':require(actor==window['owner'] and cmd['choice'] in choices,4,'/choiceOwner')
        timing=copy.deepcopy(window['timing']);lost=not inp['clockAvailable'] or now is None or now<timing['highWaterUnixMilliseconds'];late=now is not None and now>=timing['deadlineUnixMilliseconds']
        if kind=='expire' and not lost and not late:return copy.deepcopy(prior),None,None
        if kind=='choose' and not lost:require(not late,5,'/deadline')
        fallback=kind!='choose' or lost
        if not lost:s['acceptedHighWater']=max(s['acceptedHighWater'],now);timing['highWaterUnixMilliseconds']=max(timing['highWaterUnixMilliseconds'],now)
        reason=('clock-unavailable' if lost else 'controller-unavailable' if kind=='unavailable' else 'deadline') if fallback else 'owner-choice'
        if fallback:author='system'
        choice_record(choices[-1] if fallback else cmd['choice'],reason,timing)
    elif kind=='resolve':
        require(prior['status']=='committed' and now is None and inp['clockAvailable'],6,'/resolve')
        result=resolve_result(ctx,s);s['result']=result;s['randomState']=copy.deepcopy(result['afterRandomState']);s['world']=projected(ctx,s,'resolved');s['settlementId']=s['world']['settlements'][0]['settlementId'];s['status']='resolved'
        effect=dict(kind='assault-resolved',result=result,settlementId=s['settlementId'])
    else:
        require(window is None,6,'/windowBypass');status=s['status'];needs_retreat=status=='resolved' and s['result']['facts']['requiredRetreat']>0;needs_custody=status=='retreat' and any(x['status']=='pending' for x in s['world']['custodyLots'])
        if needs_retreat or needs_custody:
            wk='retreat' if needs_retreat else 'custody';owner=ctx['selection']['defender']['unit']['originalSide'] if needs_retreat else s['world']['custodyLots'][0]['captor']['originalSide']
            opened=make_window(ctx,wk,owner,s['settlementId'],now,inp['clockAvailable'])
            if opened is None:choice_record('refuse-retreat' if wk=='retreat' else 'leave-unguarded','opening-clock-unavailable',None)
            else:s['acceptedHighWater']=max(s['acceptedHighWater'],now);s['window']=opened;s['status']='waiting-'+wk;effect=dict(kind='choice-opened',window=copy.deepcopy(opened))
        else:
            require(now is None and inp['clockAvailable'],3,'/structuralTiming')
            if status=='resolved':choice_record('not-required','not-required',None)
            elif status in ('disposition','losses','retreat','custody'):
                cut={'disposition':'losses','losses':'retreat','retreat':'relationships','custody':'relationships'}[status]
                s['world']=projected(ctx,s,cut);s['status']=cut;effect=dict(kind={'losses':'losses-settled','retreat':'retreat-settled','relationships':'relationships-settled'}[cut],payload=s['world']['settlements'][0][cut])
            elif status=='relationships':
                st=s['world']['settlements'][0];proof_receipts=[st[k]['receiptId'] for k in ('disposition','losses','retreat','custody','relationships') if st[k]]
                require(all(st[k] for k in ('disposition','losses','retreat','relationships')) and not any(x['status']=='pending' for x in s['world']['custodyLots']),7,'/pendingClosure')
                s['status']='round-closed';effect=dict(kind='round-closed',settlementId=s['settlementId'],proofReceipts=proof_receipts)
            elif status=='round-closed':
                edge=aad.are.selection.edge(ctx['boundary']);require(s['roundClosureReceiptId'] is not None and len(ctx['prior']['stepReceipts'])==5,7,'/caClosure')
                s['status']='closed';s['closed']=True;effect=dict(kind='ca-completed',fromPositionId=edge['combatPositionIds'][5],toPositionId=edge['releasePositionId'],previousStepReceiptId=ctx['prior']['stepReceipts'][-1],roundClosureReceiptId=s['roundClosureReceiptId'])
            else:raise Invalid(6,'/status')
    cycle=ctx['cycle']
    event=dict(contractVersion=1,eventType='combat-actual-paid-'+effect['kind'],author=author,campaignId=cycle['campaignId'],rulesetHash=cycle['rulesetHash'],configurationHash=cycle['admittedPolicyBundleDigest'],roundClockConfigurationHash=prior['roundClockConfigurationHash'],resultClockPolicyId=INVENTORY['resultClockPolicyId'],admissionSourceHash=ctx['base']['admissionProof']['sourceHash'],baseHash=prior['baseHash'],cycleId=ctx['boundary']['cycleId'],segmentId=ctx['prior']['segmentId'],roundId=ctx['prior']['roundId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],input=canonical(inp,'PaidInput'),effect=canonical(effect,'PaidEffect'))
    receipt='cmb.'+digest('receipt',event);event['receiptId']=receipt;data=raw(event,'PaidEvent')
    if effect['kind']=='round-closed':s['roundClosureReceiptId']=receipt
    if effect['kind']=='ca-completed':s['caCompletionReceiptId']=receipt
    s['stateVersion']=event['stateVersion'];s['prefix']=aad.are.selection.st.seq.prefix_event(prior['prefix'],data)
    s['receipts'].append(dict(commandHash=ch,eventHash=sha(data),receiptId=receipt,actor=actor,stateVersion=s['stateVersion']))
    raw(s,'PaidControl');verify_dependencies();verify_delivery();return s,data,receipt


def replay(data,selection_ledger,round_ledger,paid_ledger):
    source,base,owned=ingress(data,selection_ledger,round_ledger,paid_ledger);ctx=context(base);state=initial(ctx)
    for i,(text,inp) in enumerate(zip(source['paidEventCanonicalUtf8'],owned)):
        data_event=text.encode('ascii');parse(data_event,'PaidEvent');state,expected,receipt=transition(ctx,state,inp,fresh=False)
        require(expected is not None and data_event==expected,6,f'/paidEvent/{i}')
    verify_dependencies();verify_delivery();return source,ctx,state,owned

def apply(data,selection_ledger,round_ledger,paid_ledger,new_input):
    source,ctx,state,owned=replay(data,selection_ledger,round_ledger,paid_ledger)
    after,event,receipt=transition(ctx,state,new_input)
    if event is None:return raw(source,'PaidSource'),copy.deepcopy(owned),receipt
    successor=copy.deepcopy(source);successor['paidEventCanonicalUtf8'].append(event.decode('ascii'));ledger=owned+[parse(raw(new_input,'PaidInput'),'PaidInput')]
    require(len(encode(ledger))<=LIMITS['paidLedgerBytes'],1,'/paidLedger')
    result=raw(successor,'PaidSource');verify_dependencies();verify_delivery();return result,ledger,receipt

def read_source(data,selection_ledger,round_ledger,paid_ledger):
    source,_,_,_=replay(data,selection_ledger,round_ledger,paid_ledger)
    require(not paid_ledger or INVENTORY.get('closureFrozen') is True,7,'/provisionalClosure')
    return copy.deepcopy(source)

def proof(data,selection_ledger,round_ledger,paid_ledger):
    source,ctx,state,_=replay(data,selection_ledger,round_ledger,paid_ledger)
    require(not paid_ledger or INVENTORY.get('closureFrozen') is True,7,'/provisionalClosure')
    h=digest('source',canonical(source,'PaidSource'))
    return parse(raw(dict(contractVersion=1,sourceId='apass.'+h,sourceHash='sha256:'+h,base=ctx['base'],control=state),'PaidProof'),'PaidProof')

def read_control(claim,data,selection_ledger,round_ledger,paid_ledger):
    _,_,state,_=replay(data,selection_ledger,round_ledger,paid_ledger)
    require(not paid_ledger or INVENTORY.get('closureFrozen') is True,7,'/provisionalClosure')
    value=parse(claim,'PaidControl');require(claim==raw(state,'PaidControl'),6,'/control');return value

def read_proof(claim,data,selection_ledger,round_ledger,paid_ledger):
    replay(data,selection_ledger,round_ledger,paid_ledger)
    require(not paid_ledger or INVENTORY.get('closureFrozen') is True,7,'/provisionalClosure')
    value=parse(claim,'PaidProof');expected=proof(data,selection_ledger,round_ledger,paid_ledger);require(claim==raw(expected,'PaidProof'),6,'/proof');return value

def read_cases():
    path='docs/specs/fixtures/combat-actual-assault-v1.json'
    try:data=(REPOSITORY/path).read_bytes()
    except OSError as error:raise Invalid(9,path) from error
    require(hashlib.sha256(data).hexdigest()==INVENTORY['testSourceHashes'][path],9,path)
    cases=json.loads(data)['cases']
    require(len(cases)==4 and {(c['owner'],c['firstSeal']) for c in cases}=={(a,b) for a in ('axis','commonwealth') for b in ('attacker','defender')},6,'/originalCases')
    return {'cases':cases}


# Independent genuine-history expectations retained from the semantic RED checkpoint.
def commit_tests():
    fixture=read_cases()
    failures=[]
    for case in fixture['cases']:
        selection=[json.loads(x['canonicalUtf8']) for x in case['trustedSelectionInputs']]
        rounds=[json.loads(x['canonicalUtf8']) for x in case['trustedRoundInputs']]
        source=encode(dict(contractVersion=1,admissionSourceCanonicalUtf8=case['source']['canonicalUtf8'],paidEventCanonicalUtf8=[]))
        _,base=admitted(source,selection,rounds,[])
        p=base['admissionProof']['actualRoundProof']['control']
        cmd=dict(contractVersion=1,kind='commit',baseHash='sha256:'+digest('base',base),roundId=p['roundId'],expectedPriorVersion=25,commitmentId=None,decisionId=None,choice=None)
        inp=dict(command=cmd,actor='system',admittedAt=None,clockAvailable=True)
        try:
            after,ledger,receipt=apply(source,selection,rounds,[],inp)
            event=json.loads(parse_source(after)['paidEventCanonicalUtf8'][0])
            assert event['stateVersion']==26 and event['priorVersion']==25
            assert event['priorPrefix']==p['prefix'] and receipt==event['receiptId']
            effect=event['effect'];assert effect['kind']=='attack-committed'
            assert [c['beforeCp'] for c in effect['costs']]==[0,0]
            assert [c['afterCp'] for c in effect['costs']]==[5,3]
            assert [c['beforeAmmo'] for c in effect['costs']]==[10,10]
            assert [c['afterAmmo'] for c in effect['costs']]==[0,0]
            assert effect['preResultRandomState']==p['randomState'] and p['randomState']['nextByteCursor']==2
            assert len(ledger)==1 and ledger[0]==inp
            assert receipt.startswith('cmb.') and effect['commitmentId'].startswith('cmt.')
        except (Invalid,AssertionError) as error:
            failures.append(dict(owner=case['owner'],firstSeal=case['firstSeal'],error=str(error)))
    print(json.dumps(dict(check='genuine-commitment-semantic-expectations',cases=4,failures=failures),indent=2))
    require(not failures,7,'/semanticCommitExpectations')

def full_path_tests(owner=None,first_seal=None):
    fixture=read_cases()
    cases=[c for c in fixture['cases'] if (owner is None or c['owner']==owner) and (first_seal is None or c['firstSeal']==first_seal)]
    for case in cases:
        selection=[json.loads(x['canonicalUtf8']) for x in case['trustedSelectionInputs']]
        rounds=[json.loads(x['canonicalUtf8']) for x in case['trustedRoundInputs']]
        source=encode(dict(contractVersion=1,admissionSourceCanonicalUtf8=case['source']['canonicalUtf8'],paidEventCanonicalUtf8=[]));ledger=[]
        _,base=admitted(source,selection,rounds,[]);p=base['admissionProof']['actualRoundProof']['control']
        commitment=None
        for index,kind in enumerate(['commit','resolve']+['advance']*6):
            cmd=dict(contractVersion=1,kind=kind,baseHash='sha256:'+digest('base',base),roundId=p['roundId'],expectedPriorVersion=25+index,commitmentId=commitment,decisionId=None,choice=None)
            inp=dict(command=cmd,actor='system',admittedAt=None,clockAvailable=True)
            source,ledger,receipt=apply(source,selection,rounds,ledger,inp)
            event=json.loads(parse_source(source)['paidEventCanonicalUtf8'][-1]);assert event['stateVersion']==26+index
            if index==0:commitment=event['effect']['commitmentId']
        _,ctx,state,_=replay(source,selection,rounds,ledger)
        assert state['status']=='closed' and state['closed'] and state['stateVersion']==33
        assert state['randomState']['nextByteCursor']==10 and len(state['result']['draws'])==8
        assert [x['die'] for x in state['result']['draws']]==[5,2,6,1,6,2,2,2]
        assert bytes(b for d in state['result']['draws'] for b in d['consumed']).hex()=='10375f90a18525c7'
        st=state['world']['settlements'][0]
        assert [r['lossToe'] for r in st['losses']['roles']]==[0,1]
        assert st['relationships']['kind']=='contact' and state['window'] is None
        assert not state['world']['custodyLots'] and not state['world']['guards'] and not state['world']['replacementEntitlements']
        assert len(ledger)==len(state['receipts'])==8
        assert state['result']['facts']==dict(differential=0,attackerCoordinate=62,defenderCoordinate=22,captureDie=None,attackerPercent=0,defenderPercent=15,rawEngaged=False,requiredRetreat=0,capturedRole=None,captureShare=0)
        original=copy.deepcopy(p['world']);masked=copy.deepcopy(state['world'])
        for role,spent,toe in (('attacker',5,10),('defender',3,9)):
            unit=ctx['selection'][role]['unit'];element=next(e for e in masked['elements'] if e['elementId']==unit['elementId'])
            assert element['ammunition']['points']==0 and element['operationalState']['capabilityPointsExpended']==dict(numerator=spent,denominator=1) and element['components'][0]['currentToe']==toe
            element['ammunition']['points']=10;element['operationalState']['capabilityPointsExpended']=dict(numerator=0,denominator=1);element['components'][0]['currentToe']=10
        masked['settlements']=[];masked['relationships']=[];assert masked==original
        assert all(e['components'][0]['currentToe']==10 and e['ammunition']['points']==0 for e in st['preLossElements'])
        result_preimage={k:state['result'][k] for k in ('commitmentId','rulesInputHash','procedureId','beforeRandomState','afterRandomState','draws','attackerMoraleCoordinate','defenderMoraleCoordinate','attackerMorale','defenderMorale','basicDifferential','facts')}
        assert state['result']['resultId']=='res.'+hashlib.sha256(b'sandtable.combat.assault-result.v2\0'+encode(result_preimage)).hexdigest()
        assert st['settlementId']=='set.'+hashlib.sha256(b'sandtable.combat.settlement.v2\0'+encode(dict(commitmentId=commitment,resultId=state['result']['resultId']))).hexdigest()
        assert event['effect']['toPositionId']==aad.are.selection.edge(ctx['boundary'])['releasePositionId']
        assert event['effect']['previousStepReceiptId']==p['stepReceipts'][-1]
        print('PASS: genuine complete internal paid path',case['owner'],case['firstSeal'],len(source))


def original_arguments(case):
    return (encode(dict(contractVersion=1,admissionSourceCanonicalUtf8=case['source']['canonicalUtf8'],paidEventCanonicalUtf8=[])),[json.loads(x['canonicalUtf8']) for x in case['trustedSelectionInputs']],[json.loads(x['canonicalUtf8']) for x in case['trustedRoundInputs']],[])


def command(ctx,s,kind,choice=None):
    structural=kind in ('commit','resolve','advance')
    return dict(contractVersion=1,kind=kind,baseHash=s['baseHash'],roundId=ctx['prior']['roundId'],expectedPriorVersion=s['stateVersion'] if structural else None,commitmentId=None if kind=='commit' else s['commitmentId'],decisionId=None if structural else s['window']['decisionId'] if s['window'] else 'stale.decision',choice=choice)


def trusted(cmd,actor='system',now=None,available=True):
    return dict(command=cmd,actor=actor,admittedAt=now,clockAvailable=available)


def rejected(call,code):
    try:call()
    except Invalid as error:
        assert error.code==f'CMB-APA-{code:03}',(error.code,code,error.path)
        return
    raise AssertionError('rejection required '+str(code))


def isolated_context(owner='axis'):
    fixture=read_cases()
    case=next(c for c in fixture['cases'] if c['owner']==owner and c['firstSeal']=='attacker')
    args=original_arguments(case);_,base=admitted(*args);ctx=context(base);state=initial(ctx)
    # This is a private mechanics probe. Never append the hypothetical states to
    # an actual source or call them a genuine paid/public proof.
    paid,_=paid_world(ctx);state['world']=paid;state['commitmentId']='cmt.'+'f'*64;state['status']='committed'
    return ctx,state


def isolated_resolved(ctx,state,facts):
    s=copy.deepcopy(state);r=resolve_result(ctx,s);r['facts']=copy.deepcopy(facts)
    preimage={k:v for k,v in r.items() if k!='resultId'}
    r['resultId']='res.'+hashlib.sha256(INVENTORY['embeddedDomains']['result'].encode()+b'\0'+encode(preimage)).hexdigest()
    s['result']=r;s['world']=project(ctx,state['world'],r,'resolved');s['settlementId']=s['world']['settlements'][0]['settlementId'];s['status']='resolved';s['stateVersion']=27
    return s


def window_tests(owner='axis'):
    ctx,paid=isolated_context(owner);records=list(aad.catalogue_records(ctx['rules']))
    retreat=next(x['facts'] for x in records if x['kind']=='capture-refusal' and x['facts']['requiredRetreat'] and not x['facts']['capturedRole'])
    captures=[next(x['facts'] for x in records if x['kind']=='capture-refusal' and x['facts']['capturedRole']==role and x['attackerCapturedToe']+x['defenderCapturedToe']>0) for role in ('attacker','defender')]
    for facts in [retreat,*captures]:
        s=isolated_resolved(ctx,paid,facts)
        while s['status'] not in ('resolved','retreat'):s,_,_=transition(ctx,s,trusted(command(ctx,s,'advance')))
        if s['status']=='resolved' and not facts['requiredRetreat']:
            for _ in range(3):s,_,_=transition(ctx,s,trusted(command(ctx,s,'advance')))
        elif s['status']=='resolved' and facts['requiredRetreat']:
            if facts is not retreat:
                s,_,_=transition(ctx,s,trusted(command(ctx,s,'advance'),now=None))
                for _ in range(2):s,_,_=transition(ctx,s,trusted(command(ctx,s,'advance')))
        kind='retreat' if s['status']=='resolved' else 'custody'
        waiting,event,_=transition(ctx,s,trusted(command(ctx,s,'advance'),now=10000))
        assert waiting['window']['kind']==kind and waiting['window']['timing']['highWaterUnixMilliseconds']==10000
        w=waiting['window'];choice='retreat' if kind=='retreat' else 'relocate-and-guard'
        owner_input=trusted(command(ctx,waiting,'choose',choice),w['owner'],10001)
        accepted,event,rid=transition(ctx,waiting,owner_input);assert accepted['window'] is None and json.loads(event)['author']==w['owner']
        retry,again,rid2=transition(ctx,accepted,owner_input);assert retry==accepted and again is None and rid2==rid
        for alternative in (('retreat','refuse-retreat') if kind=='retreat' else ('relocate-and-guard','leave-unguarded')):
            completed,chosen,_=transition(ctx,waiting,trusted(command(ctx,waiting,'choose',alternative),w['owner'],10001))
            assert json.loads(chosen)['effect']['reason']=='owner-choice'
            while not completed['closed']:
                completed,_,_=transition(ctx,completed,trusted(command(ctx,completed,'advance')))
            assert completed['world']['settlements'][0]['relationships'] and completed['caCompletionReceiptId']
        unavailable,unavailable_event,_=transition(ctx,waiting,trusted(command(ctx,waiting,'unavailable'),now=10001))
        assert unavailable['window'] is None and json.loads(unavailable_event)['effect']['reason']=='controller-unavailable'
        late=trusted(command(ctx,waiting,'choose',choice),w['owner'],w['timing']['deadlineUnixMilliseconds']);rejected(lambda:transition(ctx,waiting,late),5)
        foreign=trusted(command(ctx,waiting,'choose',choice),'axis' if w['owner']=='commonwealth' else 'commonwealth',None,False);rejected(lambda:transition(ctx,waiting,foreign),4)
        for now,available in ((None,True),(10001,False),(9999,True)):
            fallback,event,_=transition(ctx,waiting,trusted(command(ctx,waiting,'choose',choice),w['owner'],now,available));assert fallback['window'] is None and json.loads(event)['author']=='system' and json.loads(event)['input']['actor']==w['owner']
        early,event,rid=transition(ctx,waiting,trusted(command(ctx,waiting,'expire'),now=10001));assert early==waiting and event is None and rid is None
        expired,event,_=transition(ctx,waiting,trusted(command(ctx,waiting,'expire'),now=w['timing']['deadlineUnixMilliseconds']));assert expired['window'] is None
        missing=trusted(command(ctx,s,'advance'),now=None);fallback,event,_=transition(ctx,s,missing);assert fallback['window'] is None and json.loads(event)['effect']['timing'] is None
        overflow=trusted(command(ctx,s,'advance'),now=MAX_UTC);fallback,event,_=transition(ctx,s,overflow);assert fallback['window'] is None and json.loads(event)['effect']['timing'] is None
        invalid=trusted(command(ctx,s,'advance'),now=MAX_UTC+1);rejected(lambda:transition(ctx,s,invalid),2)
        rejected(lambda:transition(ctx,waiting,trusted(command(ctx,waiting,'advance'))),6)
    print('PASS: isolated retreat/custody mandatory window matrix',owner)


def support_tests(owner='axis'):
    ctx,_=isolated_context(owner);prior=initial(ctx);before=copy.deepcopy(prior);inp=trusted(command(ctx,prior,'commit'))
    missing=next(iter(SUPPORTED_HANDLERS));SUPPORTED_HANDLERS.remove(missing)
    try:rejected(lambda:transition(ctx,prior,inp),7);assert prior==before
    finally:SUPPORTED_HANDLERS.add(missing)
    for kind,key in (('PaidSource','sourceBytes'),('PaidProof','proofBytes'),('PaidControl','controlBytes'),('PaidEvent','eventBytes'),('PaidInput','inputBytes'),('PaidLedger','paidLedgerBytes')):
        original=LIMITS[key];LIMITS[key]=INVENTORY['encodedUpperBounds'][kind]-1
        try:rejected(lambda:transition(ctx,prior,inp),7);assert prior==before
        finally:LIMITS[key]=original
    original_records=aad.catalogue_records
    def changed_records(rules):
        for row in original_records(rules):
            if row['kind']=='morale' and row['ordinal']==0:
                row=copy.deepcopy(row);row['attackerAdjustment']=0
            yield row
    aad.catalogue_records=changed_records
    try:rejected(lambda:transition(ctx,prior,inp),7);assert prior==before
    finally:aad.catalogue_records=original_records
    def duplicate_records(rules):
        first=None
        for row in original_records(rules):
            if row['ordinal']==0:first=copy.deepcopy(row)
            if row['ordinal']==1:
                row=copy.deepcopy(first);row['ordinal']=1
            yield row
    aad.catalogue_records=duplicate_records
    try:rejected(lambda:transition(ctx,prior,inp),7);assert prior==before
    finally:aad.catalogue_records=original_records
    print('PASS: missing handler/carrier capacity and poisoned/duplicate support reject before mutation',owner)


def grammar_tests(owner='axis'):
    fixture=read_cases();case=next(c for c in fixture['cases'] if c['owner']==owner)
    args=original_arguments(case);source=parse_source(args[0]);claim=proof(*args)
    assert read_proof(raw(claim,'PaidProof'),*args)==claim
    assert read_control(raw(claim['control'],'PaidControl'),*args)==claim['control']
    for v,code in ((True,1),(1.0,1),(-1,2),(2,3)):
        changed=copy.deepcopy(source);changed['contractVersion']=v;rejected(lambda:read_source(encode(changed),*args[1:]),code)
    changed=copy.deepcopy(source);changed['extra']=None;rejected(lambda:read_source(encode(changed),*args[1:]),1)
    rejected(lambda:read_source(json.dumps(source,indent=2).encode(),*args[1:]),8)
    rejected(lambda:read_source(b'\xef\xbb\xbf'+args[0],*args[1:]),1)
    rejected(lambda:read_source(b' '* (LIMITS['sourceBytes']+1),*args[1:]),1)
    rejected(lambda:parse(b'{"contractVersion":1,"contractVersion":1}','PaidSource'),1)
    for key,lo,hi in (('stateVersion',0,2**63-1),):
        for value,code in ((True,1),(1.5,1),(lo-1,2),(hi+1,2)):
            control=copy.deepcopy(claim['control']);control[key]=value;rejected(lambda:read_control(encode(control),*args),code)
    control=copy.deepcopy(claim['control']);control['world']['elements'][0]['ammunition']['initialAmmunitionOrigin']['references'][0]['locator']+='-foreign';rejected(lambda:read_control(raw(control,'PaidControl'),*args),6)
    control=copy.deepcopy(claim['control']);control['world']['contractVersion']=6;rejected(lambda:read_control(encode(control),*args),3)
    bad=copy.deepcopy(claim);bad['sourceHash']='sha256:'+'0'*64;rejected(lambda:read_proof(raw(bad,'PaidProof'),*args),6)
    claim['control']['world']['elements'][0]['components'][0]['currentToe']=1;assert proof(*args)['control']['world']['elements'][0]['components'][0]['currentToe']==10
    cause=dict(causeId='c',ordinal=1,receiptId='r',elementId='e',gameTurn=1,operationStage=1,kind='loss-dp',points=3,before=0,after=-3)
    assert parse(raw(cause,'Cause'),'Cause')['after']==-3
    for value,code in ((True,1),(1.5,1),(-2**31-1,2),(2**31,2)):
        changed=copy.deepcopy(cause);changed['after']=value;rejected(lambda:raw(changed,'Cause'),code)
    for value in (-2**31,2**31-1):
        changed=copy.deepcopy(cause);changed['after']=value;assert parse(raw(changed,'Cause'),'Cause')['after']==value
    # Declared immediate admission-carrier primitives keep their diagnostic code
    # before trusted actual-history equality.
    for value,code in ((True,1),(1.5,1),(-1,2),(2,3)):
        carrier=json.loads(source['admissionSourceCanonicalUtf8']);carrier['contractVersion']=value
        changed=copy.deepcopy(source);changed['admissionSourceCanonicalUtf8']=encode(carrier).decode()
        rejected(lambda:read_source(encode(changed),*args[1:]),code)
    print('PASS: closed grammar, signed Cohesion, ownership and exact codes',owner)


def mechanics_tests(owner='axis'):
    ctx,paid=isolated_context(owner);count=0;capture_counts={'attacker':0,'defender':0};zero_engaged=0;refusal_dp=0
    for row in aad.catalogue_records(ctx['rules']):
        if row['kind']!='capture-refusal':continue
        facts=row['facts'];choice='not-required' if not facts['requiredRetreat'] else 'refuse-retreat' if row['refusalHexes'] else 'retreat'
        capture=row['attackerCapturedToe']+row['defenderCapturedToe']>0
        result=dict(resultId='res.'+'f'*64,commitmentId='cmt.'+'f'*64,facts=facts)
        for custody in ('relocate-and-guard','leave-unguarded') if capture else (None,):
            w=project(ctx,paid['world'],result,'relationships',choice,custody);st=w['settlements'][0]
            assert [r['lossToe'] for r in st['losses']['roles']]==[row['attackerLossToe'],row['defenderLossToe']]
            assert [r['capturedToe'] for r in st['losses']['roles']]==[row['attackerCapturedToe'],row['defenderCapturedToe']]
            assert [r['lossDp'] for r in st['losses']['roles']]==[row['attackerLossDp'],row['defenderLossDp']]
            assert sum(e['components'][0]['currentToe'] for e in w['elements'])+sum(g['toe'] for g in w['guards'])+sum(r['lossToe'] for r in st['losses']['roles'])==20
            if facts['requiredRetreat']:assert st['relationships']['kind']!='engaged'
            if not facts['requiredRetreat'] and facts['rawEngaged'] and row['attackerLossToe']+row['defenderLossToe']==0:assert st['relationships']['kind']=='engaged';zero_engaged+=1
            if capture:
                capture_counts[facts['capturedRole']]+=1;assert len(w['futureObligations'])==1
                if custody=='relocate-and-guard':assert len(w['guards'])==1 and w['guards'][0]['toe']==1 and w['custodyLots'][0]['status']=='guarded'
                else:assert not w['guards'] and w['replacementEntitlements'][0]['eligibleScope']==dict(gameTurn=5,operationStage=1) and w['custodyLots'][0]['status']=='escaped'
            if row['refusalHexes'] and row['defenderLossDp']:refusal_dp+=1
            raw(w,'World');count+=1
    assert all(capture_counts.values()) and zero_engaged and refusal_dp
    # Explicit isolated spending/RP boundaries; never authentic source authority.
    facts=next(r['facts'] for r in aad.catalogue_records(ctx['rules']) if r['kind']=='capture-refusal' and r['facts']['requiredRetreat'] and not r['facts']['capturedRole'] and r['attackerLossDp']==0)
    w=copy.deepcopy(paid['world']);attacker=next(e for e in w['elements'] if e['elementId']==ctx['selection']['attacker']['unit']['elementId']);defender=next(e for e in w['elements'] if e['elementId']==ctx['selection']['defender']['unit']['elementId']);attacker['operationalState']['cohesionLevel']=9;defender['operationalState']['capabilityPointsExpended']=cp(10)
    final=project(ctx,w,dict(resultId='res.'+'f'*64,commitmentId='cmt.'+'f'*64,facts=facts),'relationships','retreat',None)
    st=final['settlements'][0];assert st['retreat']['afterCp']==cp(11) and st['retreat']['excessCpDp']==1
    attacker=next(e for e in final['elements'] if e['elementId']==ctx['selection']['attacker']['unit']['elementId']);assert attacker['operationalState']['cohesionLevel']==10
    print('PASS: isolated exhaustive selected settlement mechanics',owner,count,capture_counts,zero_engaged,refusal_dp)


def audit_isolation_tests(owner='axis'):
    ctx,paid=isolated_context(owner)
    facts=next(r['facts'] for r in aad.catalogue_records(ctx['rules']) if r['kind']=='capture-refusal' and r['facts']['requiredRetreat'] and r['facts']['capturedRole']=='attacker' and r['attackerCapturedToe']>0)
    resolved=isolated_resolved(ctx,paid,facts);priors=[]
    for accepted in (10001,11000):
        s,_,_=transition(ctx,resolved,trusted(command(ctx,resolved,'advance'),now=10000))
        s,_,_=transition(ctx,s,trusted(command(ctx,s,'choose','retreat'),s['window']['owner'],accepted))
        for _ in range(2):s,_,_=transition(ctx,s,trusted(command(ctx,s,'advance')))
        assert s['status']=='retreat' and s['world']['custodyLots'];priors.append(s)
    assert priors[0]['world']==priors[1]['world']
    pair=[transition(ctx,s,trusted(command(ctx,s,'advance'),now=10500))[0] for s in priors]
    assert pair[0]['window']==pair[1]['window'] and pair[1]['acceptedHighWater']==11000 and pair[1]['window']['timing']['highWaterUnixMilliseconds']==10500
    outcomes=[transition(ctx,s,trusted(command(ctx,s,'choose','relocate-and-guard'),s['window']['owner'],10600))[0] for s in pair]
    assert outcomes[0]['world']==outcomes[1]['world'] and all(s['window'] is None for s in outcomes)
    print('PASS: isolated same-owner audit versus live-window independence',owner)


def preparse_tests():
    original=json.loads;calls=[]
    def observed(*a,**kw):calls.append(True);return original(*a,**kw)
    json.loads=observed
    try:
        rejected(lambda:parse(b'['*(LIMITS['depth']+2)+b'0'+b']'*(LIMITS['depth']+2),'PaidSource'),1)
        assert not calls,'depth guard ran after JSON allocation'
        rejected(lambda:parse(b'['+b','.join([b'0']*(LIMITS['ordinaryArrayItems']+1))+b']','PaidSource'),1)
        assert not calls,'array guard ran after JSON allocation'
    finally:json.loads=original
    print('PASS: raw depth and generic array guards precede JSON allocation')


def genuine_trace(owner='axis',first_seal='attacker'):
    fixture=read_cases();case=next(c for c in fixture['cases'] if c['owner']==owner and c['firstSeal']==first_seal)
    args=original_arguments(case);source,ctx,state,ledger=replay(*args);cuts=[(args[0],[],copy.deepcopy(state))];inputs=[]
    for kind in ['commit','resolve']+['advance']*6:
        inp=trusted(command(ctx,state,kind));inputs.append(inp);state,event,rid=transition(ctx,state,inp)
        source['paidEventCanonicalUtf8'].append(event.decode());ledger.append(copy.deepcopy(inp));data=raw(source,'PaidSource');cuts.append((data,copy.deepcopy(ledger),copy.deepcopy(state)))
    return args,ctx,cuts,inputs


def recovery_tests(owner='axis',first_seal='attacker'):
    global FRESH_ENABLED
    args,ctx,cuts,inputs=genuine_trace(owner,first_seal)
    for i,(data,ledger,expected) in enumerate(cuts):
        _,_,restored,_=replay(data,args[1],args[2],ledger);assert raw(restored,'PaidControl')==raw(expected,'PaidControl')
        if i:
            receipt=expected['receipts'][-1]['receiptId']
            same,owned,rid=apply(data,args[1],args[2],ledger,inputs[i-1]);assert same==data and owned==ledger and rid==receipt
            changed=parse_source(data);event=json.loads(changed['paidEventCanonicalUtf8'][-1]);event['campaignId']+='-forged'
            # Locally resigning a coherent event is not predecessor authority.
            unsigned={k:v for k,v in event.items() if k!='receiptId'};event['receiptId']='cmb.'+digest('receipt',unsigned)
            changed['paidEventCanonicalUtf8'][-1]=raw(event,'PaidEvent').decode();rejected(lambda:replay(raw(changed,'PaidSource'),args[1],args[2],ledger),6)
            rejected(lambda:replay(data,args[1],args[2],ledger[:-1]),4)
            bad=copy.deepcopy(ledger);bad[-1]['actor']='axis';rejected(lambda:replay(data,args[1],args[2],bad),4)
        # Restored objects are owned and do not poison subsequent readback.
        restored['world']['elements'][0]['components'][0]['currentToe']=1
        assert replay(data,args[1],args[2],ledger)[2]['world']['elements'][0]['components'][0]['currentToe']==expected['world']['elements'][0]['components'][0]['currentToe']
    final,ledger,state=cuts[-1]
    timer=trusted(command(ctx,state,'expire'),now=MAX_UTC);same,owned,rid=apply(final,args[1],args[2],ledger,timer);assert same==final and owned==ledger and rid is None
    malformed=copy.deepcopy(inputs[0]);malformed['command']['expectedPriorVersion']=True;rejected(lambda:apply(final,args[1],args[2],ledger,malformed),1)
    original=FRESH_ENABLED;FRESH_ENABLED=False
    try:
        rejected(lambda:apply(args[0],args[1],args[2],[],inputs[0]),7)
        committed,paid_ledger,_=cuts[1];resolved,owned,rid=apply(committed,args[1],args[2],paid_ledger,inputs[1]);assert resolved==cuts[2][0] and owned==cuts[2][1]
    finally:FRESH_ENABLED=original
    print('PASS: genuine prefix/retry/forgery/ownership and disabled-fresh recovery',owner,first_seal,len(cuts))


def rng_tests():
    ctx,paid=isolated_context('axis');consume('docs/research/verify-combat-rng.py')
    spec=importlib.util.spec_from_file_location('paid_rng_tests',REPOSITORY/'docs/research/verify-combat-rng.py');rng=importlib.util.module_from_spec(spec);spec.loader.exec_module(rng)
    roles=set();rejected_bytes=False;crossing=False
    for vector in rng.VECTORS:
        seed,cursor,dice,expected_bytes=vector[:4];s=copy.deepcopy(paid);s['randomState']['seed']=seed;s['randomState']['nextByteCursor']=cursor;before=copy.deepcopy(s)
        result=resolve_result(ctx,s)
        assert ''.join(str(d['die']) for d in result['draws'])==dice
        assert bytes(b for d in result['draws'] for b in d['consumed']).hex()==expected_bytes
        assert result['facts']['differential']==vector[4] and result['facts']['attackerPercent']==vector[5] and result['facts']['defenderPercent']==vector[6]
        assert result['facts']['rawEngaged']==vector[7] and result['facts']['requiredRetreat']==vector[8] and result['facts']['captureShare']==vector[10]
        assert result['afterRandomState']['nextByteCursor']==cursor+len(bytes.fromhex(expected_bytes)) and s==before
        if len(dice)==9:roles.add(result['facts']['capturedRole']);assert result['draws'][-1]['purpose']==result['facts']['capturedRole']+'.capture.share'
        else:assert len(result['draws'])==8
        rejected_bytes|=any(len(d['consumed'])>1 for d in result['draws']);crossing|=cursor//32!=result['afterRandomState']['nextByteCursor']//32
    assert roles=={'attacker','defender'} and rejected_bytes and crossing
    for cursor in (2**64-1,2**64-7):
        s=copy.deepcopy(paid);s['randomState']['nextByteCursor']=cursor;before=copy.deepcopy(s);rejected(lambda:resolve_result(ctx,s),7);assert s==before
    print('PASS: isolated pinned RNG vectors, both conditional capture roles, rejection/block/U64 boundaries',len(rng.VECTORS))


def dependency_tests():
    fixture=read_cases();args=original_arguments(fixture['cases'][0]);proof(*args)
    original=Path.read_bytes
    def altered(target,mode='changed',after=0):
        calls=[0]
        def read(path):
            if path.resolve()==(REPOSITORY/target).resolve():
                calls[0]+=1
                if calls[0]>after:
                    if mode=='unreadable':raise OSError('isolated unreadable dependency')
                    if mode=='missing':raise FileNotFoundError('isolated missing dependency')
                    return original(path)+b'\n# isolated changed bytes'
            return original(path)
        return read
    for path in DEPENDENCIES:
        for mode in ('changed','missing','unreadable'):
            Path.read_bytes=altered(path,mode)
            try:rejected(lambda:read_source(*args),9)
            finally:Path.read_bytes=original
        Path.read_bytes=altered(path)
        try:
            spec=importlib.util.spec_from_file_location('isolated_paid_cold',ROOT/'verify-combat-actual-paid-assault-v1.py');module=importlib.util.module_from_spec(spec)
            try:spec.loader.exec_module(module)
            except ValueError as error:assert getattr(error,'code',None)=='CMB-APA-009'
            else:raise AssertionError('cold dependency bypass '+path)
        finally:Path.read_bytes=original
        Path.read_bytes=altered(path,after=1)
        try:rejected(lambda:read_source(*args),9)
        finally:Path.read_bytes=original
    # Owned delivery must also resist post-load edits before readback.
    for path in ('docs/specs/verify-combat-actual-paid-assault-v1.py','docs/specs/combat-actual-paid-assault-v1.schema.json'):
        Path.read_bytes=altered(path)
        try:rejected(lambda:read_source(*args),9)
        finally:Path.read_bytes=original
    Path.read_bytes=altered('docs/specs/fixtures/combat-actual-assault-v1.json')
    try:
        rejected(read_cases,9)
        assert read_source(*args)==parse_source(args[0])
    finally:Path.read_bytes=original
    print('PASS: all runtime cold/warm/change/missing/unreadable/at-use dependencies and owned delivery',len(DEPENDENCIES))



def manifest_tests():
    args=original_arguments(read_cases()['cases'][0]);claim=proof(*args)
    expected=dependency_manifest_hash()
    assert claim['base']['dependencyManifestHash']==expected
    predecessor=claim['base']['admissionProof']['arithmeticAssessment']['dependencyManifestHash']
    assert predecessor!=expected
    for missing in (False,True):
        bad=copy.deepcopy(claim)
        if missing:del bad['base']['dependencyManifestHash']
        else:bad['base']['dependencyManifestHash']='sha256:'+'0'*64
        rejected(lambda:read_proof(encode(bad),*args),1 if missing else 6)
    bad=copy.deepcopy(claim);bad['base']['dependencyManifestHash']='sha256:'+'0'*64
    bad['control']['baseHash']='sha256:'+digest('base',bad['base'])
    rejected(lambda:read_proof(raw(bad,'PaidProof'),*args),6)
    path=next(iter(DEPENDENCIES));original=DEPENDENCIES[path]
    try:
        DEPENDENCIES[path]='0'*64
        rejected(lambda:read_proof(b'not-json',*args),9)
        rejected(dependency_manifest_hash,9)
    finally:DEPENDENCIES[path]=original
    assert dependency_manifest_hash()==expected
    print('PASS: derived successor manifest, omitted/wrong/coherently resigned claims and 009 precedence',expected)


def grammar_matrix_tests():
    args,ctx,cuts,_=genuine_trace();data,ledger,state=cuts[-1]
    values=[(parse_source(data),'PaidSource'),(state,'PaidControl'),(proof(data,args[1],args[2],ledger),'PaidProof')]
    values += [(json.loads(x),'PaidEvent') for x in parse_source(data)['paidEventCanonicalUtf8']]
    count=0
    def paths(value,prefix=()):
        yield prefix,value
        if type(value) is dict:
            for key,child in value.items():yield from paths(child,prefix+(key,))
        elif type(value) is list:
            for index,child in enumerate(value):yield from paths(child,prefix+(index,))
    for value,kind in values:
        for path,leaf in paths(value):
            if not path:continue
            changed=copy.deepcopy(value);parent=changed
            for key in path[:-1]:parent=parent[key]
            parent[path[-1]]=False if type(leaf) in (str,int,float) else 1 if type(leaf) is bool else {} if leaf is None or type(leaf) is list else []
            rejected(lambda:parse(encode(changed),kind),1);count+=1
    print('PASS: every retained carrier/control/proof/event typed field rejects wrong shape',count)


FIXTURE=ROOT/'fixtures/combat-actual-paid-assault-v1.json'

def fixture_artifacts():
    require(INVENTORY.get('closureFrozen') is True,7,'/provisionalClosure')
    rows=[]
    for owner in ('axis','commonwealth'):
        for seal in ('attacker','defender'):
            args,ctx,cuts,_=genuine_trace(owner,seal);data,ledger,state=cuts[-1]
            derived=proof(data,args[1],args[2],ledger)
            rows.append(dict(owner=owner,firstSeal=seal,sourceCanonicalUtf8=data.decode('ascii'),selectionLedger=args[1],roundLedger=args[2],paidLedger=ledger,proofCanonicalUtf8=raw(derived,'PaidProof').decode('ascii'),sourceBytes=len(data),proofBytes=len(raw(derived,'PaidProof'))))
    return dict(contractVersion=1,dependencyManifestHash=dependency_manifest_hash(),cases=rows)


def fixture_tests():
    retained=json.loads(FIXTURE.read_bytes())
    expected=fixture_artifacts()
    assert retained==expected,'retained actual paid literals differ; normal gate never regenerates'
    assert len(retained['cases'])==4
    for case in retained['cases']:
        args=(case['sourceCanonicalUtf8'].encode('ascii'),case['selectionLedger'],case['roundLedger'],case['paidLedger'])
        claim=case['proofCanonicalUtf8'].encode('ascii');derived=read_proof(claim,*args)
        assert read_control(raw(derived['control'],'PaidControl'),*args)==derived['control']
        assert read_source(*args)==parse_source(args[0])
        forged=copy.deepcopy(derived);forged['control']['world']['elements'][0]['components'][0]['currentToe']-=1
        rejected(lambda:read_proof(raw(forged,'PaidProof'),*args),6)
        print('PASS: retained actual paid literal and public readback',case['owner'],case['firstSeal'],derived['sourceHash'])


def normal_gate():
    commit_tests();preparse_tests();manifest_tests();rng_tests();dependency_tests()
    for owner in ('axis','commonwealth'):
        window_tests(owner);support_tests(owner);grammar_tests(owner);mechanics_tests(owner);audit_isolation_tests(owner)
        for seal in ('attacker','defender'):
            full_path_tests(owner,seal);recovery_tests(owner,seal)
    grammar_matrix_tests();fixture_tests()
    print('PASS: complete actual paid assault normal gate; retained literals were verified without regeneration')


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--write-fixture',action='store_true');parser.add_argument('--check-manifest',action='store_true');parser.add_argument('--check-matrix',action='store_true');parser.add_argument('--check-commit',action='store_true');parser.add_argument('--check-full',action='store_true');parser.add_argument('--check-windows',action='store_true');parser.add_argument('--check-support',action='store_true');parser.add_argument('--check-grammar',action='store_true');parser.add_argument('--check-mechanics',action='store_true');parser.add_argument('--check-audit',action='store_true');parser.add_argument('--check-preparse',action='store_true');parser.add_argument('--check-recovery',action='store_true');parser.add_argument('--check-rng',action='store_true');parser.add_argument('--check-dependencies',action='store_true');parser.add_argument('--owner',choices=['axis','commonwealth']);parser.add_argument('--first-seal',choices=['attacker','defender']);args=parser.parse_args()
    if args.write_fixture:FIXTURE.write_text(json.dumps(fixture_artifacts(),indent=2)+'\n')
    elif args.check_manifest:manifest_tests()
    elif args.check_matrix:grammar_matrix_tests()
    elif args.check_full:full_path_tests(args.owner,args.first_seal)
    elif args.check_windows:window_tests(args.owner or 'axis')
    elif args.check_support:support_tests(args.owner or 'axis')
    elif args.check_grammar:grammar_tests(args.owner or 'axis')
    elif args.check_mechanics:mechanics_tests(args.owner or 'axis')
    elif args.check_audit:audit_isolation_tests(args.owner or 'axis')
    elif args.check_preparse:preparse_tests()
    elif args.check_recovery:recovery_tests(args.owner or 'axis',args.first_seal or 'attacker')
    elif args.check_rng:rng_tests()
    elif args.check_dependencies:dependency_tests()
    elif args.check_commit:commit_tests()
    else:normal_gate()

if __name__=='__main__':main()
