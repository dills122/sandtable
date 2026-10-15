#!/usr/bin/env python3
"""R01 authentic N3 pending intent to actual ordinal2 ordinary Movement."""
import sys,copy,json,hashlib,importlib.util
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent;REPOSITORY=ROOT.parent.parent;NAME='combat-actual-moved-repeat-movement-v1'
I=json.loads((ROOT/(NAME+'.schema.json')).read_bytes());S={k:[f.split(':') for f in v.split()] for k,v in I['objects'].items()};L=I['limits']
spec=importlib.util.spec_from_file_location('mrm_parent',ROOT/'verify-combat-actual-moved-cycle-control-v1.py');n3=importlib.util.module_from_spec(spec);spec.loader.exec_module(n3)
class Invalid(ValueError):
 def __init__(self,n):super().__init__(f'CMB-MRM-{n:03}')
def require(ok,n):
 if not ok:raise Invalid(n)
def encode(v):return json.dumps(v,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(b):return 'sha256:'+hashlib.sha256(b).hexdigest()
def domain(k,b):return hashlib.sha256(I['domains'][k].encode()+b'\0'+b).hexdigest()
def limit(k):return L['sourceBytes'] if k=='Source' else L['proofBytes'] if k=='Proof' else L['eventBytes'] if k=='Event' else L['inputBytes'] if k in ('Input','Command') else L['controlBytes']
ENUM={'movementKind':('repeat-publish','move'),'movementPhase':('repeat-pending','movement')}
def typed(v,k,depth=0):
 require(depth<=40,1)
 if k in I['unsupportedTypes']:require(False,1)
 elif k in I['arrayAliases']:
  a=I['arrayAliases'][k];require(type(v) is list and len(v)<=a['maxItems'],1)
  for x in v:typed(x,a['elementType'],depth+1)
 elif k.endswith('?'):
  if v is not None:typed(v,k[:-1],depth)
 elif k.endswith('[]'):
  require(type(v) is list and len(v)<=512,1)
  for x in v:typed(x,k[:-2],depth+1)
 elif k in I['effectTags']:
  require(type(v) is dict and v.get('kind') in I['effectTags'][k],3);typed(v,I['effectTags'][k][v['kind']],depth)
 elif k in S:
  require(type(v) is dict and set(v)=={f for f,t in S[k]},1)
  for f,t in S[k]:typed(v[f],t,depth+1)
 elif k in ENUM:require(type(v) is str and v in ENUM[k],3)
 elif k in ('parentCarrier','eventCarrier'):require(type(v) is str and v.isascii() and 0<len(v)<=(L['parentSourceBytes'] if k=='parentCarrier' else L['eventBytes']),1)
 else:n3.typed(v,k,depth)
def canonical(v,k):
 if k in I['arrayAliases']:return [canonical(x,I['arrayAliases'][k]['elementType']) for x in v]
 if k.endswith('?'):return None if v is None else canonical(v,k[:-1])
 if k.endswith('[]'):return [canonical(x,k[:-2]) for x in v]
 if k in I['effectTags']:return canonical(v,I['effectTags'][k][v['kind']])
 if k in S:return {f:canonical(v[f],t) for f,t in S[k]}
 if k in ENUM or k in ('parentCarrier','eventCarrier'):return v
 return n3.canonical(v,k)
def raw(v,k):
 typed(v,k);b=encode(canonical(v,k));require(len(b)<=limit(k),1);return b
def parse(b,k):
 require(type(b) is bytes and 0<len(b)<=limit(k),1)
 def pairs(items):
  o={}
  for key,v in items:require(key not in o,1);o[key]=v
  return o
 try:v=json.loads(b.decode('ascii'),object_pairs_hook=pairs,parse_constant=lambda _:(_ for _ in ()).throw(Invalid(1)))
 except (ValueError,UnicodeError,RecursionError) as e:raise Invalid(1) from e
 require(raw(v,k)==b,8);return v
OWN={p:sha(p.read_bytes()) for p in [ROOT/(NAME+'.schema.json'),Path(__file__).resolve()]}
def dependencies():
 for path,h in I['runtimeSourceHashes'].items():require(hashlib.sha256((REPOSITORY/path).read_bytes()).hexdigest()==h,9)
 for p,h in OWN.items():require(sha(p.read_bytes())==h,9)
def manifest_hash():return 'sha256:'+domain('dependency-manifest',encode(dict(sorted(I['runtimeSourceHashes'].items()))))
def source_bytes(packet):
 b=raw(packet,'Source');require(packet['contractVersion']==1 and len(packet['movementEventCanonicalUtf8'])<=6,3);return b
def replay(source,request,prefix,lifecycle,breakdown,selection,round,paid,release,control,movement):
 packet=parse(source,'Source');source_bytes(packet);ledgers=copy.deepcopy([prefix,lifecycle,breakdown,selection,round,paid,release,control,movement]);dependencies()
 pf,p=n3.replay(packet['controlSourceCanonicalUtf8'].encode(),request,*ledgers[:8]);require(p['closed'] and p['phase']=='repeat-intent-pending' and p['pendingRepeatIntent'] is not None and p['closure'] is None and p['activeCycle']==p['cycle'] and p['cycle']['ordinal']==1,4)
 a=p['assessment'];intent=p['pendingRepeatIntent'];require(a['worldHash']==sha(n3.n2.raw(p['world'],'World')) and a['sourceOccurrence']==p['cycleId'] and a['witnesses'] and not a['executionAvailable'],4)
 require(intent==dict(contractVersion=1,kind='pending-repeat-intent',sourceOccurrence=p['cycleId'],sourceOrdinal=1,expectedNextOrdinal=2,parentSourceHash=p['parentSourceHash'],worldHash=a['worldHash'],assessmentHash=sha(n3.n2.raw(a,'Assessment')),configurationHash=p['cycle']['admittedPolicyBundleDigest'],decisionId=p['decisionId'],witnessesHash=sha(encode(a['witnesses'])),executionAvailable=False),4)
 basis=dict(contractVersion=1,parentSourceHash=sha(packet['controlSourceCanonicalUtf8'].encode()),sourceOccurrence=p['cycleId'],intentReceiptId=p['pendingIntentReceiptId'],worldHash=a['worldHash'],assessmentHash=intent['assessmentHash'],configurationHash=intent['configurationHash'],decisionId=intent['decisionId'],witnessesHash=intent['witnessesHash'],dependencyManifestHash=manifest_hash())
 f=dict(packet=packet,parent=p,parentFrame=pf,request=request,basis=basis,baseHash='sha256:'+domain('base',raw(basis,'Base')),ledgers=ledgers,content=pf['parentFrame']['content'])
 require(len(movement)==len(packet['movementEventCanonicalUtf8'])<=6 and sum(map(len,movement))<=24576,4)
 s=initial(f)
 for expected,b in zip(packet['movementEventCanonicalUtf8'],ledgers[8]):
  s,event,_=transition(f,s,parse(b,'Input'),False);require(event is not None and event==expected.encode(),6)
 return f,s
def initial(f):
 p=f['parent'];return dict(contractVersion=1,profile=p['profile'],parentSourceHash=f['basis']['parentSourceHash'],baseHash=f['baseHash'],phase='repeat-pending',stateVersion=p['stateVersion'],prefix=p['prefix'],sequencePosition=copy.deepcopy(p['sequencePosition']),originalCycle=copy.deepcopy(p['cycle']),activeCycle=copy.deepcopy(p['activeCycle']),cycleId=p['cycleId'],world=copy.deepcopy(p['world']),randomState=copy.deepcopy(p['randomState']),originalMovementEnd=copy.deepcopy(p['movementEnd']),priorCycleProgress=copy.deepcopy(p['actualProgressRefs']),actualProgressRefs=copy.deepcopy(p['actualProgressRefs']),members=copy.deepcopy(p['members']),attackHistory=copy.deepcopy(p['attackHistory']),priorCycleTargetUses=copy.deepcopy(p['targetUses']),targetUses=copy.deepcopy(p['targetUses']),assessment=copy.deepcopy(p['assessment']),pendingRepeatIntent=copy.deepcopy(p['pendingRepeatIntent']),pendingIntentReceiptId=p['pendingIntentReceiptId'],publicationReceiptId=None,closure=None,tracks=[],receipts=copy.deepcopy(p['receipts']),movementReceipts=[])
def next_cycle(f):
 c=copy.deepcopy(f['parent']['cycle']);c.update(ordinal=2,openedAuthorityVersion=f['parent']['stateVersion']+1,openingPrefix=f['parent']['prefix']);return c
def cycle_id(c):return sha(n3.n2.n1.seq.identity(c,'Authority',c['actingSide']))
def movement_position(f):
 p=copy.deepcopy(f['parent']['sequencePosition']);require(p['contractVersion']==5 and p['positionId']=='land.position.operation-1.first-player.movement-and-combat.reserve-release' and p['segmentId']=='land.segment.reserve-release' and p['stepId'] is None,4)
 p.update(positionId='land.position.operation-1.first-player.movement-and-combat.movement',segmentId='land.segment.movement');return p
def transition(f,prior,inp,fresh=True,admission=True):
 raw(inp,'Input');cmd=inp['command'];publish=cmd['kind']=='repeat-publish';actor=inp['actor'];basis=f['basis'];nc=next_cycle(f);nid=cycle_id(nc);p=f['parent'];unit=p['members'][0]['unit']
 require(cmd['contractVersion']==1 and all(cmd[k]==basis[k] for k in ['parentSourceHash','sourceOccurrence','intentReceiptId','configurationHash','decisionId']) and cmd['baseHash']==f['baseHash'],4)
 require(actor==('system' if publish else nc['actingSide']) and cmd['cycleId']==(p['cycleId'] if publish else nid) and cmd['positionId']==(p['sequencePosition']['positionId'] if publish else movement_position(f)['positionId']),4)
 require((cmd['unit'] is None and cmd['originLocationId'] is None and cmd['destinationLocationId'] is None) if publish else cmd['unit']==unit and cmd['originLocationId'] is not None and cmd['destinationLocationId'] is not None,4)
 ch=sha(raw(cmd,'Command'));duplicate=next((r for r in prior['movementReceipts'] if r['commandHash']==ch and r['actor']==actor),None)
 if duplicate:return copy.deepcopy(prior),None,duplicate['receiptId']
 require(prior['stateVersion']<2**63-1,7);require(cmd['expectedPriorVersion']==prior['stateVersion'],6)
 if fresh:require(admission and I['closureFrozen'],7)
 s=copy.deepcopy(prior)
 if publish:
  require(prior['phase']=='repeat-pending' and prior['publicationReceiptId'] is None and prior['activeCycle']==p['cycle'] and prior['world']==p['world'],6)
  if fresh:certify(f)
  effect=dict(kind='repeat-published',intentReceiptId=basis['intentReceiptId'],priorCycleId=p['cycleId'],nextCycleId=nid,nextCycle=nc,successorPositionId=movement_position(f)['positionId'],worldHash=basis['worldHash'],assessmentHash=basis['assessmentHash'],witnessesHash=basis['witnessesHash'])
  s.update(phase='movement',activeCycle=nc,cycleId=nid,sequencePosition=movement_position(f),targetUses=[],actualProgressRefs=[])
 else:
  require(prior['phase']=='movement' and prior['activeCycle']==nc and prior['publicationReceiptId'] is not None,6)
  w=s['world'];e=next(x for x in w['elements'] if x['elementId']==unit['elementId']);op=e['operationalState'];m=s['members'][0];origin=cmd['originLocationId'];dest=cmd['destinationLocationId'];nf=f['parentFrame']['parentFrame']
  require(e['currentLocationId']==origin and dest in n3.n2.neighbors(nf,origin),5)
  require(m['status']=='none' and e['reserveStatus']=='none' and m['baseCpa']==10 and m['history']==p['members'][0]['history'] and m['spentCp']==op['capabilityPointsExpended'] and m['spentCp']['denominator']==1,3)
  facts=next(x for x in f['content']['elements'] if x['elementId']==unit['elementId']);require(facts['baseCapabilityPointAllowance']==10 and facts['breakdownVehicleCohort'] is None and op['vehicleBreakdownState'] is None and op['movementEnded'] is None and (op['ledgerGameTurn'],op['ledgerOperationStage'])==(nc['gameTurn'],nc['operationStage']),3)
  require(all(r['currentLocationId'] not in n3.n2.neighbors(nf,dest) for r in w['representations'] if any(next(x for x in f['content']['elements'] if x['elementId']==id)['sideId']!=actor for id in r['boundElementIds'])),3)
  edge=next(x for x in f['content']['edges'] if {x['firstLocationId'],x['secondLocationId']}=={origin,dest});require(not edge['features'] and next(x for x in f['content']['locations'] if x['locationId']==dest)['terrainId']=='land.terrain.clear',3)
  witnesses=live_witnesses(f,prior)['witnesses'];wit=next((x for x in witnesses if x['unit']==unit and x['destinationLocationId']==dest),None);require(wit is not None,5)
  reps=[r for r in w['representations'] if r['bindingKind']=='independent-element' and r['boundElementIds']==[unit['elementId']]];require(len(reps)==1 and reps[0]['currentLocationId']==origin,4)
  affected=sorted([r for r in w['relationships'] if r['active'] and (r['attacker']==unit or r['defender']==unit)],key=lambda r:r['relationId']);before=op['capabilityPointsExpended']['numerator'];after=wit['afterCp'];dp=wit['excessCpDp'];cohesion=op['cohesionLevel'];require(len(prior['actualProgressRefs'])<5 and cohesion-dp>=-2**31,7)
  ends=[{k:copy.deepcopy(r[k]) for k in ['relationId','creationReceiptId','kind','attacker','defender']}|dict(endingCause='ordinary-break-off') for r in affected]
  costs=dict(terrainCost=2,breakOffCost=wit['breakOffCost'],totalCost=after-before,baseCpa=10,voluntaryCeiling=15,beforeCp=before,afterCp=after,excessCpDp=dp,beforeCohesion=cohesion,afterCohesion=cohesion-dp)
  effect=dict(kind='ordinary-move',unit=unit,representationId=reps[0]['representationId'],originLocationId=origin,destinationLocationId=dest,costs=costs,endedMemberships=ends)
 require(len(prior['movementReceipts'])<6,7)
 event=dict(contractVersion=1,eventType='combat-actual-moved-'+effect['kind'],author=actor,campaignId=nc['campaignId'],rulesetHash=nc['rulesetHash'],configurationHash=nc['admittedPolicyBundleDigest'],profile=p['profile'],parentSourceHash=basis['parentSourceHash'],baseHash=f['baseHash'],cycleId=cmd['cycleId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],input=inp,effect=effect)
 rid='mrm.'+domain('receipt',encode(event));event['receiptId']=rid;ev=raw(event,'Event');s['stateVersion']=event['stateVersion'];s['prefix']=n3.n2.n1.seq.prefix_event(prior['prefix'],ev)
 record=dict(commandHash=ch,eventHash=sha(ev),receiptId=rid,actor=actor,stateVersion=s['stateVersion']);s['receipts'].append(copy.deepcopy(record));s['movementReceipts'].append(record)
 if publish:s['publicationReceiptId']=rid;s['closure']=dict(cycleId=p['cycleId'],ordinal=1,outcome='repeated',receiptId=rid)
 else:
  e['currentLocationId']=dest;op['capabilityPointsExpended']=dict(numerator=after,denominator=1);op['cohesionLevel']=cohesion-dp;m['spentCp']=dict(numerator=after,denominator=1);reps[0]['currentLocationId']=dest
  for r in affected:r.update(active=False,endedByReceiptId=rid,endingCause='ordinary-break-off')
  if dp:
   causes=w['cohesionCauses'];require(len(causes)<512 and len(rid+'.dp')<=128,7);causes.append(dict(causeId=rid+'.dp',ordinal=len(causes)+1,receiptId=rid,elementId=unit['elementId'],gameTurn=nc['gameTurn'],operationStage=nc['operationStage'],kind='ordinary-movement-excess-cp-dp',points=dp,before=cohesion,after=cohesion-dp))
  if s['tracks']:require(s['tracks'][0]['route'][-1]==origin and len(s['tracks'][0]['route'])<6,7);s['tracks'][0]['route'].append(dest)
  else:s['tracks'].append(dict(unit=copy.deepcopy(unit),route=[origin,dest]))
  s['actualProgressRefs'].append(dict(eventType=event['eventType'],receiptId=rid,eventHash=sha(ev)))
 raw(s,'Control');return s,ev,rid
def certify(f,handlers=None,capacities=None):
 require(set(I['supportHandlers'])==set(I['supportHandlers'] if handlers is None else handlers),7)
 caps=L if capacities is None else capacities
 require(all(type(caps.get(k)) is int and caps[k]>=v for k,v in L.items()),7)
 nf=f['parentFrame']['parentFrame'];n3.n2.certify(nf)
 ctx=n3.n2.support_context(nf);paid,_=n3.n2.n1.paid_paid_world(ctx);summaries={}
 for row in n3.n2.CATALOGUE:
  if row['kind']=='capture-refusal':
   fact=row['facts'];key=tuple(fact[k] for k in ('attackerPercent','defenderPercent','rawEngaged','requiredRetreat','capturedRole','captureShare'))+(row['refusalHexes'],);summaries[key]=row
 observed=set()
 for row in summaries.values():
  fact=row['facts'];retreat='not-required' if fact['requiredRetreat']==0 else 'refuse-retreat' if row['refusalHexes'] else 'retreat'
  for custody in ('relocate-and-guard','leave-unguarded') if row['attackerCapturedToe']+row['defenderCapturedToe']>0 else (None,):
   result=dict(resultId='res.'+'f'*64,commitmentId='cmt.'+'f'*64,facts=fact);w=n3.n2.n1.project(ctx,paid,result,'relationships',retreat,custody)
   # Independent policy facts: never an admitted Source/event/Proof or invented RNG ancestry.
   isolated=dict(f,parent=dict(f['parent'],profile='paid'),parentFrame=dict(f['parentFrame'],parentFrame=dict(nf,profile='paid')))
   state=initial(isolated);state.update(world=copy.deepcopy(w),phase='movement',activeCycle=next_cycle(f),cycleId=cycle_id(next_cycle(f)),sequencePosition=movement_position(f),actualProgressRefs=[],targetUses=[],publicationReceiptId='mrm.'+'f'*64)
   for m in state['members']:m['spentCp']=copy.deepcopy(next(e for e in w['elements'] if e['elementId']==m['unit']['elementId'])['operationalState']['capabilityPointsExpended'])
   witnesses=live_witnesses(isolated,state)['witnesses'];require(witnesses,7)
   for witness in witnesses:
    after,event,_=transition(isolated,state,command(state,'move',witness['destinationLocationId']),False)
    require(event is not None and len(after['actualProgressRefs'])==1,7)
    for field in ['settlements','custodyLots','guards','replacementEntitlements','futureObligations','brokenVehicleLots']:require(after['world'][field]==state['world'][field],7)
    for field in ['randomState','originalMovementEnd','priorCycleProgress','priorCycleTargetUses','attackHistory','assessment','pendingRepeatIntent','pendingIntentReceiptId']:require(after[field]==state[field],7)
   observed.add((len(w['guards']),len(w['replacementEntitlements']),tuple(x['kind'] for x in w['relationships']),fact['capturedRole']))
 require(observed,7)
def command(s,kind,destination=None):
 publish=kind=='repeat-publish';u=None if publish else s['members'][0]['unit'];e=None if publish else next(x for x in s['world']['elements'] if x['elementId']==u['elementId'])
 return dict(command=dict(contractVersion=1,kind=kind,parentSourceHash=s['parentSourceHash'],baseHash=s['baseHash'],sourceOccurrence=s['pendingRepeatIntent']['sourceOccurrence'],intentReceiptId=s['pendingIntentReceiptId'],configurationHash=s['pendingRepeatIntent']['configurationHash'],decisionId=s['pendingRepeatIntent']['decisionId'],expectedPriorVersion=s['stateVersion'],cycleId=s['cycleId'],positionId=s['sequencePosition']['positionId'],unit=copy.deepcopy(u),originLocationId=None if publish else e['currentLocationId'],destinationLocationId=destination),actor='system' if publish else s['activeCycle']['actingSide'])
def parent_case(c):
 p=json.loads(c['initialSourceCanonicalUtf8']);p['controlEventCanonicalUtf8']=c['eventCanonicalUtf8'];source=n3.source_bytes(p)
 n2case=next(x for x in json.loads((ROOT/'fixtures/combat-actual-moved-continuation-v1.json').read_bytes())['cases'] if x['name']==c['parentName'])
 args=[json.loads(n2case['requestCanonicalUtf8']),*[list(map(str.encode,n2case[k])) for k in ['prefixInputCanonicalUtf8','lifecycleInputCanonicalUtf8','breakdownInputCanonicalUtf8','selectionInputCanonicalUtf8','roundInputCanonicalUtf8','paidInputCanonicalUtf8','inputCanonicalUtf8']],list(map(str.encode,c['inputCanonicalUtf8']))]
 packet=dict(contractVersion=1,controlSourceCanonicalUtf8=source.decode(),movementEventCanonicalUtf8=[]);return packet,args

def red_gate():
 cases=json.loads((ROOT/'fixtures/combat-actual-moved-cycle-control-v1.json').read_bytes())['cases']
 failed=[]
 for profile in ('paid','armed'):
  c=next(c for c in cases if c['profile']==profile and c['choice']=='repeat-intent');packet,args=parent_case(c);f,s=replay(source_bytes(packet),*args,[])
  require(s['activeCycle']['ordinal']==1 and s['actualProgressRefs']==s['priorCycleProgress'],6)
  try:
   after,event,_=transition(f,s,command(s,'repeat-publish'));require(event is not None and after['activeCycle']['ordinal']==2 and after['phase']=='movement' and after['actualProgressRefs']==[],6)
  except Invalid as e:failed.append(dict(profile=profile,parent=c['name'],diagnostic=str(e)))
 print(json.dumps(dict(status='RED' if failed else 'GREEN',publication=failed)))
 require(not failed,6)
def proof(packet,s):
 h=sha(source_bytes(packet));events=packet['movementEventCanonicalUtf8'];return dict(contractVersion=1,sourceId='mrms.'+h[7:],sourceHash=h,parentSourceHash=s['parentSourceHash'],dependencyManifestHash=manifest_hash(),control=s,lastEventHash=sha(events[-1].encode()) if events else None,publicationReceiptId=s['publicationReceiptId'])
def apply(source,args,movement,inp,admission=True):
 owned=bytes(inp);f,s=replay(bytes(source),*copy.deepcopy(args),copy.deepcopy(movement));after,event,rid=transition(f,s,parse(owned,'Input'),True,admission)
 if event is None:return source,s,None,rid
 packet=copy.deepcopy(f['packet']);packet['movementEventCanonicalUtf8'].append(event.decode());result=source_bytes(packet);_,rebuilt=replay(result,*args,[*movement,owned]);require(after==rebuilt,6);return result,rebuilt,event,rid

def live_witnesses(f,s):
 return n3.n2.assessment(f['parentFrame']['parentFrame'],f['parent'],s['world'])
def rejected(action):
 try:action()
 except (Invalid,n3.Invalid,n3.n2.Invalid):return
 raise AssertionError('expected contract rejection')
def trace(c,negatives=False):
 packet,args=parent_case(c);source=source_bytes(packet);initial_source=source.decode();movement=[];inputs=[];events=[];cuts=[];f,s=replay(source,*args,movement);parent=copy.deepcopy(s)
 def retain():
  cuts.append({k:dict(bytes=len(b),sha256=sha(b)) for k,b in [('source',source),('control',raw(s,'Control')),('proof',raw(proof(parse(source,'Source'),s),'Proof'))]})
 retain()
 while True:
  f,s=replay(source,*args,movement)
  if not movement:inp=command(s,'repeat-publish')
  else:
   witnesses=live_witnesses(f,s)['witnesses']
   if not witnesses:break
   inp=command(s,'move',witnesses[0]['destinationLocationId'])
  b=raw(inp,'Input');before=copy.deepcopy(s);source,s,event,rid=apply(source,args,movement,b);require(event is not None,6);movement.append(b);inputs.append(b.decode());events.append(event.decode());retain()
  if len(movement)==1:
   require(s['activeCycle']['ordinal']==2 and s['activeCycle']['openedAuthorityVersion']==s['stateVersion'] and s['activeCycle']['openingPrefix']==before['prefix'] and s['closure']['ordinal']==1 and s['closure']['outcome']=='repeated' and s['publicationReceiptId']==rid,6)
   require(s['actualProgressRefs']==[] and s['targetUses']==[] and s['world']==before['world'],6)
  for field in ['randomState','originalMovementEnd','priorCycleProgress','priorCycleTargetUses','attackHistory','assessment','pendingRepeatIntent','pendingIntentReceiptId']:
   require(s[field]==parent[field],6)
  if negatives:
   for old in movement:
    retry=apply(source,args,movement,old,False);require(retry[0]==source and retry[1]==s and retry[2] is None,6)
   changed=copy.deepcopy(inp);changed['actor']='commonwealth' if inp['actor']=='axis' else 'axis';rejected(lambda:apply(source,args,movement,raw(changed,'Input')))
   changed=copy.deepcopy(inp);changed['command']['expectedPriorVersion']+=1;rejected(lambda:apply(source,args,movement,raw(changed,'Input')))
   changed=copy.deepcopy(inp);changed['command']['decisionId']='forged';rejected(lambda:apply(source,args,movement,raw(changed,'Input')))
   changed=parse(source,'Source');ev=json.loads(changed['movementEventCanonicalUtf8'][-1]);ev['stateVersion']+=1;changed['movementEventCanonicalUtf8'][-1]=raw(ev,'Event').decode();rejected(lambda:replay(source_bytes(changed),*args,movement))
   rejected(lambda:replay(source,*args,movement[:-1]))
 require(len(movement)==(3 if c['profile']=='paid' else 6),6)
 require(len(s['actualProgressRefs'])==len(movement)-1 and next(e for e in s['world']['elements'] if e['elementId']==s['members'][0]['unit']['elementId'])['operationalState']['capabilityPointsExpended']!=next(e for e in parent['world']['elements'] if e['elementId']==s['members'][0]['unit']['elementId'])['operationalState']['capabilityPointsExpended'],6)
 require(all(s['world'][field]==parent['world'][field] for field in ['settlements','custodyLots','guards','replacementEntitlements','futureObligations','brokenVehicleLots']),6)
 return dict(name=c['name'],parentName=c['name'],profile=c['profile'],initialSourceCanonicalUtf8=initial_source,inputCanonicalUtf8=inputs,eventCanonicalUtf8=events,cuts=cuts)
def generate(negatives=False):
 cases=[c for c in json.loads((ROOT/'fixtures/combat-actual-moved-cycle-control-v1.json').read_bytes())['cases'] if c['choice']=='repeat-intent'];result=[]
 for index,c in enumerate(cases,1):
  result.append(trace(c,negatives));print(f'reference parent {index}/{len(cases)}: {c["name"]}',file=sys.stderr,flush=True)
 return dict(contract=NAME,contractVersion=1,cases=result)
def verify():
 rebuilt=generate(True);require(json.loads((ROOT/'fixtures'/(NAME+'.json')).read_bytes())==rebuilt,6);print(json.dumps(dict(status='pass',parents=len(rebuilt['cases']),cuts=sum(len(c['cuts']) for c in rebuilt['cases']),events=sum(len(c['eventCanonicalUtf8']) for c in rebuilt['cases']))))
if __name__=='__main__':
 if '--red-gate' in sys.argv:red_gate()
 elif '--write-fixture' in sys.argv:
  value=generate();(ROOT/'fixtures'/(NAME+'.json')).write_bytes(encode(value)+b'\n');print(json.dumps(dict(status='fixture written',parents=len(value['cases']))))
 else:verify()
