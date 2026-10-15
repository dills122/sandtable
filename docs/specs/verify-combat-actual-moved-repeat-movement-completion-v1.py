#!/usr/bin/env python3
"""R02 authentic R01 Movement stop, System resolution, owner completion."""
import sys,copy,json,hashlib,importlib.util
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent;REPOSITORY=ROOT.parent.parent;NAME="combat-actual-moved-repeat-movement-completion-v1"
I=json.loads((ROOT/(NAME+'.schema.json')).read_bytes());S={k:[f.split(':') for f in v.split()] for k,v in I['objects'].items()};L=I['limits']
spec=importlib.util.spec_from_file_location('mrc_parent',ROOT/'verify-combat-actual-moved-repeat-movement-v1.py');mrm=importlib.util.module_from_spec(spec);spec.loader.exec_module(mrm)
class Invalid(ValueError):
 def __init__(self,n):super().__init__(f'CMB-MRC-{n:03}')
def require(ok,n):
 if not ok:raise Invalid(n)
def encode(v):return json.dumps(v,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(b):return 'sha256:'+hashlib.sha256(b).hexdigest()
def domain(k,b):return hashlib.sha256(I['domains'][k].encode()+b'\0'+b).hexdigest()
# R02 uses the complete compound grammar locally; ancestor reuse is scalar syntax only.
ENUM={'lifecycleKind':tuple(I['commands']),'lifecyclePhase':('movement','breakdown'),'profile':('paid','armed')}
def limit(k):return L['sourceBytes'] if k=='Source' else L['proofBytes'] if k=='Proof' else L['eventBytes'] if k=='Event' else L['inputBytes'] if k in ('Input','Command') else L['controlBytes']
def typed(v,k,depth=0,parent='',field=''):
 require(depth<=L['depth'],1)
 if k in I['unsupportedTypes']:require(False,1)
 elif k in I['arrayAliases']:
  a=I['arrayAliases'][k];require(type(v) is list and len(v)<=a['maxItems'],1)
  for x in v:typed(x,a['elementType'],depth+1)
 elif k.endswith('?'):
  if v is not None:typed(v,k[:-1],depth,parent,field)
 elif k.endswith('[]'):
  require(type(v) is list and len(v)<=I['arrayBounds'].get(parent+'.'+field,512),1)
  for x in v:typed(x,k[:-2],depth+1)
 elif k in I['effectTags']:
  require(type(v) is dict and type(v.get('kind')) is str and v['kind'] in I['effectTags'][k],3);typed(v,I['effectTags'][k][v['kind']],depth)
 elif k in S:
  require(type(v) is dict and set(v)=={f for f,t in S[k]},1)
  for f,t in S[k]:typed(v[f],t,depth+1,k,f)
  if k in ('Source','Base','Command','Event','Control','Proof'):require(v['contractVersion']==1,3)
 elif k in ENUM:require(type(v) is str and v in ENUM[k],3)
 elif k in ('parentCarrier','eventCarrier'):require(type(v) is str and v.isascii() and 0<len(v)<=(L['parentSourceBytes'] if k=='parentCarrier' else L['eventBytes']),1)
 elif k=='signedInt':require(type(v) is int and -2**31<=v<2**31,2)
 else:inherited(lambda:mrm.n3.n2.n1.typed(v,k,'/'))
def canonical(v,k):
 if k in I['arrayAliases']:return [canonical(x,I['arrayAliases'][k]['elementType']) for x in v]
 if k.endswith('?'):return None if v is None else canonical(v,k[:-1])
 if k.endswith('[]'):return [canonical(x,k[:-2]) for x in v]
 if k in I['effectTags']:return canonical(v,I['effectTags'][k][v['kind']])
 if k in S:return {f:canonical(v[f],t) for f,t in S[k]}
 if k in ENUM or k in ('parentCarrier','eventCarrier'):return v
 return inherited(lambda:mrm.n3.canonical(v,k))
def bounds(v,depth=0):
 require(depth<=L['depth'],1)
 if type(v) is dict:
  require(len(v)<=128,1)
  for x in v.values():bounds(x,depth+1)
 elif type(v) is list:
  require(len(v)<=512,1)
  for x in v:bounds(x,depth+1)
def raw(v,k):
 bounds(v);typed(v,k);b=encode(canonical(v,k));require(len(b)<=limit(k),1);return b
def parse(b,k):
 require(type(b) is bytes and 0<len(b)<=limit(k) and b.isascii(),1)
 def pairs(items):
  o={}
  for key,v in items:require(key not in o,1);o[key]=v
  return o
 try:v=json.loads(b.decode('ascii'),object_pairs_hook=pairs,parse_constant=lambda _:(_ for _ in ()).throw(Invalid(1)))
 except (ValueError,UnicodeError,RecursionError) as e:raise Invalid(1) from e
 require(raw(v,k)==b,8);return v
OWN={p:sha(p.read_bytes()) for p in [ROOT/(NAME+'.schema.json'),Path(__file__).resolve()]}
PIN_MANIFEST=encode(dict(sorted(I['runtimeSourceHashes'].items())))
def dependencies():
 require(encode(dict(sorted(I['runtimeSourceHashes'].items())))==PIN_MANIFEST,9)
 for path,h in I['runtimeSourceHashes'].items():
  try:b=(REPOSITORY/path).read_bytes()
  except OSError as e:raise Invalid(9) from e
  require(type(b) is bytes and len(b)<=8388608 and hashlib.sha256(b).hexdigest()==h,9)
 for p,h in OWN.items():
  try:b=p.read_bytes()
  except OSError as e:raise Invalid(9) from e
  require(type(b) is bytes and sha(b)==h,9)
def manifest_hash():return 'sha256:'+domain('dependency-manifest',PIN_MANIFEST)
def inherited(call):
 try:return call()
 except ValueError as e:
  import re
  code=re.search(r'CMB-[A-Z]+-(\d{3})',str(e))
  if code:raise Invalid(int(code[1])) from e
  raise
def source_bytes(packet):
 b=raw(packet,'Source');require(len(packet['lifecycleEventCanonicalUtf8'])<=3,1);return b
REPLAY_COUNT=0
VERBOSE=False
def replay(source,request,prefix,lifecycle,breakdown,selection,round,paid,release,control,movement,completion):
 global REPLAY_COUNT
 require(type(source) is bytes and type(request) is dict,1)
 packet=parse(bytes(source),'Source');source_bytes(packet);request=copy.deepcopy(request);ledgers=copy.deepcopy([prefix,lifecycle,breakdown,selection,round,paid,release,control,movement,completion])
 require(all(type(x) is list and all(type(b) is bytes for b in x) for x in ledgers),1)
 completion=ledgers[9]
 require(len(completion)==len(packet['lifecycleEventCanonicalUtf8'])<=3 and sum(map(len,completion))<=L['lifecycleLedgerBytes'],4);dependencies();REPLAY_COUNT+=1
 pf,p=inherited(lambda:mrm.replay(packet['movementSourceCanonicalUtf8'].encode(),request,*ledgers[:9]));c=p['activeCycle'];count=len(p['actualProgressRefs'])
 require(p['phase']=='movement' and p['publicationReceiptId'] is not None and c['ordinal']==2 and p['originalMovementEnd']['ordinal']==1 and p['closure']['ordinal']==1 and p['closure']['outcome']=='repeated',4)
 require(len(ledgers[8])==len(p['movementReceipts'])==count+1 and 0<=count<=(2 if p['profile']=='paid' else 5) and len(p['tracks'])==int(count>0) and not p['targetUses'],4)
 content=pf['content'];require(all(m['status']=='none' and next(e for e in content['elements'] if e['elementId']==m['unit']['elementId'])['breakdownVehicleCohort'] is None for m in p['members']),3)
 eventCap=3 if count else 1;require(p['stateVersion']<=2**63-1-eventCap,7)
 flow=dict(kind='idle')
 if count:
  track=p['tracks'][0];require(track['unit']==p['members'][0]['unit'] and len(track['route'])==count+1 and track['route'][-1]==next(e['currentLocationId'] for e in p['world']['elements'] if e['elementId']==track['unit']['elementId']),4)
  flow=dict(kind='moving',track=copy.deepcopy(track),lastMoveReceiptId=p['movementReceipts'][-1]['receiptId'])
 base=dict(contractVersion=1,movementSourceHash=sha(packet['movementSourceCanonicalUtf8'].encode()),movementControlHash=sha(mrm.raw(p,'Control')),movementProofHash=sha(mrm.raw(mrm.proof(pf['packet'],p),'Proof')),dependencyManifestHash=manifest_hash(),publicationReceiptId=p['publicationReceiptId'],cycleId=p['cycleId'],configurationHash=c['admittedPolicyBundleDigest'])
 f=dict(packet=packet,parent=p,base=base,baseHash='sha256:'+domain('base',raw(base,'Base')),request=request,ledgers=ledgers,content=content,initialFlow=flow,eventCap=eventCap,moveCount=count)
 require(len(completion)<=eventCap,4);s=initial(f)
 for expected,b in zip(packet['lifecycleEventCanonicalUtf8'],ledgers[9]):
  s,event,_=transition(f,s,parse(b,'Input'),False);require(event is not None and event==expected.encode(),6)
 dependencies();return f,s

def initial(f):
 p=f['parent'];s=copy.deepcopy(p);s.update(movementBaseHash=p['baseHash'],baseHash=f['baseHash'],movementSourceHash=f['base']['movementSourceHash'],flow=copy.deepcopy(f['initialFlow']),interruptContext=None,movementEnd=None,lifecycleReceipts=[],closed=False);return s

def position(f,kind):
 if kind=='movement':return copy.deepcopy(f['parent']['sequencePosition'])
 id='land.position.breakdown-stop' if kind=='stop' else 'land.position.operation-1.first-player.movement-and-combat.breakdown-determination'
 return copy.deepcopy(next(p for p in mrm.n3.n2.n1.seq.expected_catalog()['positions']+mrm.n3.n2.n1.seq.expected_catalog()['interruptPositions'] if p['positionId']==id))
def capability(f,s,kind):return 'sha256:'+domain('capability',encode(dict(baseHash=f['baseHash'],cycleId=s['cycleId'],kind=kind,stateVersion=s['stateVersion'],sequencePosition=canonical(s['sequencePosition'],'Position'),flow=canonical(s['flow'],'Flow'),interruptContext=canonical(s['interruptContext'],'Context?'))))
def command(f,s,kind):return dict(command=dict(contractVersion=1,kind=kind,baseHash=f['baseHash'],cycleId=f['parent']['cycleId'],expectedPositionId=position(f,'stop' if kind=='resolve-breakdown-stop' else 'movement')['positionId'],expectedPriorVersion=s['stateVersion'],capabilityId=capability(f,s,kind)),actor='system' if kind=='resolve-breakdown-stop' else f['parent']['activeCycle']['actingSide'])
def scope(c):return {k:c[k] for k in ['gameTurn','operationStage','playerPhaseSlot','actingSide']}
def unit(f,w,e):return dict(creationBinding=w['creationBinding'],originalSide=next(x['sideId'] for x in f['content']['elements'] if x['elementId']==e['elementId']),elementId=e['elementId'])
def unitkey(u):return tuple(u[k] for k in ['creationBinding','originalSide','elementId'])
def expiry(f,s,rid):
 c=s['activeCycle'];old=s['originalMovementEnd'];w=s['world'];at=sorted([dict(unit=unit(f,w,e),locationId=e['currentLocationId']) for e in w['elements']],key=lambda x:unitkey(x['unit']))
 require(old['scope']==scope(c) and old['ordinal']==c['ordinal']-1 and [unitkey(x['unit']) for x in old['endLocations']]==[unitkey(x['unit']) for x in at] and all(m['status']=='none' and m['history']['scope']==scope(c) for m in s['members']),4)
 def near(a,b):
  found={a};front={a}
  for _ in range(2):
   front={e['secondLocationId'] if e['firstLocationId'] in front else e['firstLocationId'] for e in f['content']['edges'] if e['firstLocationId'] in front or e['secondLocationId'] in front};found|=front
  return b in found
 def excluded(locations,retained):return [copy.deepcopy(x['unit']) for x in locations if x['unit']['originalSide']==c['actingSide'] and (x['unit'] in retained or not any(y['unit']['originalSide']!=c['actingSide'] and near(x['locationId'],y['locationId']) for y in locations))]
 historical=excluded(old['endLocations'],old['excludedBefore']);return dict(scope=scope(c),ordinal=c['ordinal'],completionReceiptId=rid,endLocations=at,excludedBefore=excluded(at,historical))
def certify(f,handlers=None,capacities=None):
 require(set(handlers if handlers is not None else I['supportHandlers'])>=set(I['supportHandlers']) and all((capacities if capacities is not None else L).get(k,-1)>=v for k,v in L.items()),7)
def transition(f,prior,inp,fresh=True,handlers=None,capacities=None,admission=True):
 raw(prior,'Control');raw(inp,'Input');cmd=inp['command'];kind=cmd['kind'];c=f['parent']['activeCycle'];actor=inp['actor'];movement=position(f,'movement');expected=position(f,'stop' if kind=='resolve-breakdown-stop' else 'movement')
 require(cmd['baseHash']==prior['baseHash']==f['baseHash'] and cmd['cycleId']==prior['cycleId']==f['parent']['cycleId'] and prior['activeCycle']==c and prior['movementSourceHash']==f['base']['movementSourceHash'] and cmd['expectedPositionId']==expected['positionId'],4)
 require(actor==('system' if kind=='resolve-breakdown-stop' else c['actingSide']),4);ch=sha(raw(cmd,'Command'));old=next((r for r in prior['lifecycleReceipts'] if r['commandHash']==ch and r['actor']==actor),None)
 if old:return copy.deepcopy(prior),None,old['receiptId']
 require(not prior['closed'] and len(prior['lifecycleReceipts'])<f['eventCap'] and prior['stateVersion']<2**63-1,7)
 require(cmd['expectedPriorVersion']==prior['stateVersion'] and cmd['expectedPositionId']==prior['sequencePosition']['positionId'],6);require(cmd['capabilityId']==capability(f,prior,kind),5)
 s=copy.deepcopy(prior);flow=prior['flow'];context=dict(cycle=copy.deepcopy(c),cycleId=prior['cycleId'],suspendedPosition=movement)
 if kind=='stop-element-movement':
  require(flow['kind']=='moving' and prior['interruptContext'] is None and prior['movementEnd'] is None and prior['sequencePosition']==movement,6)
  require(flow==f['initialFlow'] and flow['track']==prior['tracks'][0] and flow['lastMoveReceiptId']==prior['movementReceipts'][-1]['receiptId'],4)
  descriptor=dict(recordedStateVersion=prior['stateVersion']+1,track=copy.deepcopy(flow['track']),lastMoveReceiptId=flow['lastMoveReceiptId'],reason='deliberate',weatherKind='normal',cohortInputs=[])
  stop=dict(stopId='mrmstop.'+domain('stop',encode(dict(baseHash=f['baseHash'],cycleId=prior['cycleId'],descriptor=descriptor))),**descriptor)
  s['flow']=dict(kind='phasing-stop',stop=stop);s['interruptContext']=context;s['sequencePosition']=position(f,'stop');effect=dict(kind='movement-stopped',stop=copy.deepcopy(stop),interruptContext=copy.deepcopy(context),successorPosition=copy.deepcopy(s['sequencePosition']))
 elif kind=='resolve-breakdown-stop':
  require(flow['kind']=='phasing-stop' and prior['interruptContext']==context and prior['sequencePosition']==position(f,'stop') and prior['movementEnd'] is None and f['initialFlow']['kind']=='moving',6)
  stop=flow['stop'];require(stop['track']==f['initialFlow']['track'] and stop['lastMoveReceiptId']==f['initialFlow']['lastMoveReceiptId'] and not stop['cohortInputs'] and stop['recordedStateVersion']==prior['stateVersion'],4)
  s['flow']=dict(kind='idle');s['interruptContext']=None;s['sequencePosition']=movement;effect=dict(kind='stop-resolved',stopId=stop['stopId'],beforeRandomState=copy.deepcopy(prior['randomState']),afterRandomState=copy.deepcopy(prior['randomState']),checks=[],createdLots=[],sources=['spi-1979-land-rules:21.24-21.26'],successorPosition=copy.deepcopy(movement))
 else:
  require(flow==dict(kind='idle') and prior['interruptContext'] is None and prior['movementEnd'] is None and prior['sequencePosition']==movement,6)
  if f['eventCap']==3:require(len(prior['lifecycleReceipts'])==2,6)
  preview=expiry(f,prior,'pending');s['sequencePosition']=position(f,'breakdown');s['phase']='breakdown';s['closed']=True;effect=dict(kind='movement-completed',endLocations=copy.deepcopy(preview['endLocations']),excludedUnits=copy.deepcopy(preview['excludedBefore']),progress=copy.deepcopy(prior['actualProgressRefs']),successorPosition=copy.deepcopy(s['sequencePosition']))
 if fresh:require(admission,7);certify(f,handlers,capacities)
 event=dict(contractVersion=1,eventType='combat-actual-moved-repeat-'+effect['kind'],author=actor,campaignId=c['campaignId'],rulesetHash=c['rulesetHash'],configurationHash=c['admittedPolicyBundleDigest'],profile=prior['profile'],movementSourceHash=prior['movementSourceHash'],baseHash=f['baseHash'],cycleId=prior['cycleId'],positionId=prior['sequencePosition']['positionId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],input=canonical(inp,'Input'),effect=canonical(effect,'Effect'))
 rid='mrmc.'+domain('receipt',encode(event));event['receiptId']=rid;ev=raw(event,'Event')
 if kind=='complete-movement-segment':s['movementEnd']=expiry(f,prior,rid)
 s['stateVersion']=event['stateVersion'];s['prefix']=mrm.n3.n2.n1.seq.prefix_event(prior['prefix'],ev);receipt=dict(commandHash=ch,eventHash=sha(ev),receiptId=rid,actor=actor,stateVersion=s['stateVersion']);s['receipts'].append(copy.deepcopy(receipt));s['lifecycleReceipts'].append(receipt);raw(s,'Control');return s,ev,rid
def proof(packet,f,s):return dict(contractVersion=1,sourceId='mrmcomp.'+sha(source_bytes(packet))[7:],sourceHash=sha(source_bytes(packet)),base=copy.deepcopy(f['base']),control=copy.deepcopy(s))
def apply(source,args,completion,inp,admission=True,handlers=None,capacities=None):
 owned=bytes(inp);f,s=replay(bytes(source),*copy.deepcopy(args),copy.deepcopy(completion));after,event,rid=transition(f,s,parse(owned,'Input'),True,handlers,capacities,admission)
 if event is None:return source,s,None,rid
 packet=copy.deepcopy(f['packet']);packet['lifecycleEventCanonicalUtf8'].append(event.decode());result=source_bytes(packet);_,rebuilt=replay(result,f['request'],*f['ledgers'][:9],[*f['ledgers'][9],owned]);require(after==rebuilt,6);return result,rebuilt,event,rid

def root(row):
 cases=json.loads((ROOT/'fixtures/combat-actual-moved-repeat-movement-v1.json').read_bytes())['cases'];c=cases[row['rootOrdinal']];require(c['profile']==row['profile'],4)
 parents=json.loads((ROOT/'fixtures/combat-actual-moved-cycle-control-v1.json').read_bytes())['cases'];parent=next(x for x in parents if x['name']==c['parentName']);packet,args=mrm.parent_case(parent);cut=row['movementCut'];packet['movementEventCanonicalUtf8']=c['eventCanonicalUtf8'][:cut];movement=[x.encode() for x in c['inputCanonicalUtf8'][:cut]];p=dict(contractVersion=1,movementSourceCanonicalUtf8=mrm.source_bytes(packet).decode(),lifecycleEventCanonicalUtf8=[]);return p,[*args,movement]
def semantic_rows():
 cases=json.loads((ROOT/'fixtures/combat-actual-moved-repeat-movement-v1.json').read_bytes())['cases'];return [dict(rootOrdinal=i,movementCut=k,profile=c['profile'],group=f"{c['profile']}-{c['name'].split('-')[0]}-m{k-1}",rowKey=f'r{i:03d}-c{k:02d}') for i,c in enumerate(cases) for k in range(1,len(c['inputCanonicalUtf8'])+1)]
def trace(row,retries=True):
 packet,args=root(row);source=source_bytes(packet);f,s=replay(source,*args,[]);ledger=[];events=[];cuts=[];inputs=[];parent=copy.deepcopy(s)
 require(s['profile']==row['profile'] and s['activeCycle']['actingSide']==row['group'].split('-')[1] and len(s['actualProgressRefs'])==row['movementCut']-1,4)
 def retain():cuts.append({k:dict(bytes=len(b),sha256=sha(b)) for k,b in [('source',source),('control',raw(s,'Control')),('proof',raw(proof(parse(source,'Source'),f,s),'Proof'))]})
 retain()
 for kind in (I['commands'] if row['movementCut']>1 else ['complete-movement-segment']):
  b=raw(command(f,s,kind),'Input');source,s,event,rid=apply(source,args,ledger,b);require(event is not None,6);ledger.append(b);inputs.append(b.decode());events.append(event.decode());retain()
  if VERBOSE:print('ACCEPTED',kind,rid,'position',s['sequencePosition']['positionId'],flush=True)
  if retries:
   for old in ledger:
    retry=apply(source,args,ledger,old,False);require(retry[0]==source and retry[1]==s and retry[2] is None,6)
 require(s['closed'] and s['phase']=='breakdown' and s['movementEnd']['ordinal']==2 and s['movementEnd']['completionReceiptId']==rid and s['originalMovementEnd']['ordinal']==1,6)
 for field in ['world','randomState','originalMovementEnd','priorCycleProgress','actualProgressRefs','members','attackHistory','priorCycleTargetUses','targetUses','assessment','pendingRepeatIntent','pendingIntentReceiptId','publicationReceiptId','originalCycle','activeCycle','closure','tracks','movementReceipts']:
  require(s[field]==parent[field],6)
 return dict(**row,inputCanonicalUtf8=inputs,eventCanonicalUtf8=events,cuts=cuts)
def behavior(profile,moves):
 global VERBOSE
 VERBOSE=True
 row=next(r for r in semantic_rows() if r['profile']==profile and r['movementCut']==moves+1);packet,args=root(row);f,s=replay(source_bytes(packet),*args,[]);print('AUTHENTIC R01 ingress',profile,'moves',moves,'ordinal',s['activeCycle']['ordinal'],flush=True);return trace(row)
def read_source(source,args,completion):
 f,s=replay(source,*args,completion);return source_bytes(f['packet'])
def read_control(claim,source,args,completion):
 f,s=replay(source,*args,completion);require(raw(parse(claim,'Control'),'Control')==raw(s,'Control'),6);return copy.deepcopy(s)
def read_proof(claim,source,args,completion):
 f,s=replay(source,*args,completion);p=proof(f['packet'],f,s);require(raw(parse(claim,'Proof'),'Proof')==raw(p,'Proof'),6);return copy.deepcopy(p)
def support_rows():
 out=[];full='Cna.Core.Tests.Campaigns.CombatActualMovedRepeatMovementCompletion';four=[(0,1,'paid'),(0,3,'paid'),(12,1,'armed'),(12,6,'armed')];two=[four[1],four[3]]
 def add(cls,method,args):out.append(dict(className=full+cls,methodName=method,arguments=[dict(name=k,value=v) for k,v in zip(['rootOrdinal','movementCut','profile'],args)]))
 for args in four:add('Tests','AuthenticPublishedMovementCompletesLifecycle',args)
 for method in ['EveryCutContextActorCapabilityAndOrderReject','SourceControlProofReadersRejectForgedClaims','CatalogFullPositionsAndBothProofsStayDistinct']:
  for args in four:add('ContractTests',method,args)
 for method in ['TenIndependentLedgersAndResignedEventsCannotAuthenticate','OwnedBeforeCallbackAndAllReturnedBuffers']:
  for args in two:add('ContractTests',method,args)
 for method in ['AllPhysicalPinsRemainRequiredAfterWarmReplay','EveryHandlerAndCapacityRequired','ClosedGrammarAndSizeFraming','WholeTraceCapacityAndHistoricalExclusionAreBounded','FullSourceFramingCannotForgeParent']:add('ContractTests',method,[])
 require(len(out)==25,6);return out
def support_obligations():return dict(physicalPins=106,physicalFaultModes=5,physicalPinFaults=530,handlers=14,limitEntries=25,handlerCapacityOmissions=156,ledgerFaults=60,resignedEvents=6,claimFaults=24,contextFaults=72,isolatedBoundaries=8,grammarFramingOwnershipChecks=88)
FIXTURE=ROOT/'fixtures'/('combat-actual-moved-repeat-movement-completion-v1.json')
def generate():
 global REPLAY_COUNT
 REPLAY_COUNT=0;cases=[];rows=semantic_rows()
 for i,row in enumerate(rows):
  cases.append(trace(row));print('GENERATED',i+1,'/300',row['rowKey'],'parentReplays',REPLAY_COUNT,flush=True)
 require(len(cases)==300 and sum(len(c['inputCanonicalUtf8']) for c in cases)==788 and sum(len(c['cuts']) for c in cases)==1088 and REPLAY_COUNT==3396,6)
 return dict(contractVersion=1,dependencyManifestHash=manifest_hash(),semanticRows=rows,supportRows=support_rows(),supportObligations=support_obligations(),cases=cases)
def verify(group=None):
 global REPLAY_COUNT
 require(group is None or group in {r['group'] for r in semantic_rows()},3)
 fixture=json.loads(FIXTURE.read_bytes());require(fixture['contractVersion']==1 and fixture['dependencyManifestHash']==manifest_hash() and fixture['semanticRows']==semantic_rows() and fixture['supportRows']==support_rows() and fixture['supportObligations']==support_obligations(),6);REPLAY_COUNT=0;count=events=cuts=0
 for expected,row in zip(fixture['cases'],fixture['semanticRows']):
  if group is not None and row['group']!=group:continue
  actual=trace(row);require(actual==expected,6);count+=1;events+=len(actual['inputCanonicalUtf8']);cuts+=len(actual['cuts']);print('VERIFIED',count,row['rowKey'],'parentReplays',REPLAY_COUNT,flush=True)
 if group is None:require((count,events,cuts,REPLAY_COUNT)==(300,788,1088,3396) and len(fixture['cases'])==300,6)
 print('PASS frozen independent rows/events/cuts/parentReplays',count,events,cuts,REPLAY_COUNT,'all-prior retries',1520 if group is None else 'group',flush=True)
def reject(call,code=None):
 try:call()
 except ValueError as error:
  if code is not None:require(str(error).startswith('CMB-MRC-'+str(code).zfill(3)),6)
  return
 raise AssertionError('invalid source/claim/command accepted')
def example(row):
 case=trace(row,False);packet,args=root(row);return case,packet,args

def closure():
 counters=dict(contextFaults=0,ledgerFaults=0,resignedEvents=0,claimFaults=0,physicalPinFaults=0,handlerCeilings=0,isolatedBoundaries=0)
 rows=semantic_rows();chosen=[next(r for r in rows if r['rootOrdinal']==i and r['movementCut']==k) for i,k in [(0,1),(0,3),(12,1),(12,6)]]
 def carrier(packet,case,cut):return source_bytes(dict(packet,lifecycleEventCanonicalUtf8=case['eventCanonicalUtf8'][:cut]))
 for row in chosen:
  case,packet,args=example(row);inputs=[x.encode() for x in case['inputCanonicalUtf8']];count=len(inputs);source=carrier(packet,case,count);f,done=replay(source,*args,inputs)
  require(done['movementEnd']['completionReceiptId']==done['lifecycleReceipts'][-1]['receiptId'] and done['originalMovementEnd']['ordinal']==1 and done['movementEnd']['ordinal']==2 and done['world']==f['parent']['world'] and done['members']==f['parent']['members'],6)
  require(done['sequencePosition']==position(f,'breakdown') and done['sequencePosition']['actorRole']=='first-acting-side' and done['activeCycle']['ordinal']==2 and done['closure']['ordinal']==1,6)
  for cut,b in enumerate(inputs):
   prior=carrier(packet,case,cut);ff,s=replay(prior,*args,inputs[:cut]);inp=parse(b,'Input')
   for field,value,code in [('baseHash','sha256:'+'0'*64,4),('cycleId','sha256:'+'0'*64,4),('expectedPositionId','foreign',4),('expectedPriorVersion',0,6),('capabilityId','sha256:'+'0'*64,5)]:
    bad=copy.deepcopy(inp);bad['command'][field]=value;reject(lambda:apply(prior,args,inputs[:cut],raw(bad,'Input')),code);counters['contextFaults']+=1
   bad=copy.deepcopy(inp);bad['actor']='system' if inp['actor']!='system' else ff['parent']['activeCycle']['actingSide'];reject(lambda:apply(prior,args,inputs[:cut],raw(bad,'Input')),4);counters['contextFaults']+=1
   reject(lambda:apply(prior,args,inputs[:cut],b,False),7);counters['contextFaults']+=1
   if cut:require(s['sequencePosition']==position(ff,'stop' if cut==1 else 'movement'),6)
   for kind in I['commands']:
    if kind==inp['command']['kind']:continue
    reject(lambda:apply(prior,args,inputs[:cut],raw(command(ff,s,kind),'Input')));counters['contextFaults']+=1
   # Before fresh admission all handler/limit omissions reject; duplicate recovery remains independent.
   if cut==0:
    for handler in I['supportHandlers']:
     reject(lambda:apply(prior,args,[],b,True,[x for x in I['supportHandlers'] if x!=handler],L),7);counters['handlerCeilings']+=1
    for key in L:
     caps=dict(L);caps[key]-=1;reject(lambda:apply(prior,args,[],b,True,I['supportHandlers'],caps),7);counters['handlerCeilings']+=1
  for proofClaim in [False,True]:
   claim=proof(f['packet'],f,done) if proofClaim else done
   for field,value in [('baseHash','sha256:'+'0'*64),('movementEnd',dict(done['movementEnd'],ordinal=1)),('originalMovementEnd',dict(done['originalMovementEnd'],ordinal=2))]:
    bad=copy.deepcopy(claim);target=bad['control'] if proofClaim else bad;target[field]=value;fn=read_proof if proofClaim else read_control;reject(lambda:fn(raw(bad,'Proof' if proofClaim else 'Control'),source,args,inputs),6);counters['claimFaults']+=1
  if row['movementCut']>1:
   for index in range(10):
    combined=copy.deepcopy([*args[1:],inputs])
    for variant in ([combined[index][:-1],[*combined[index],combined[index][0]],[b'{}',*combined[index][1:]]] if combined[index] else [None,[b'{}'],[b'[]']]):
     bad=copy.deepcopy(combined);bad[index]=variant;reject(lambda:replay(source,args[0],*bad));counters['ledgerFaults']+=1
   for index in range(3):
    packetBad=parse(source,'Source');event=json.loads(packetBad['lifecycleEventCanonicalUtf8'][index]);event['stateVersion']+=1;event.pop('receiptId');event['receiptId']='mrmc.'+domain('receipt',encode(event));packetBad['lifecycleEventCanonicalUtf8'][index]=raw(event,'Event').decode();reject(lambda:replay(source_bytes(packetBad),*args,inputs),6);counters['resignedEvents']+=1
  # Pure isolated boundary probes do not claim a new authenticated source.
  boundary=initial(f);boundary['stateVersion']=2**63-1;reject(lambda:transition(f,boundary,command(f,boundary,'complete-movement-segment')),7);counters['isolatedBoundaries']+=1
  retained=copy.deepcopy(initial(f));retained['originalMovementEnd']['excludedBefore']=[copy.deepcopy(retained['members'][0]['unit'])];require(expiry(f,retained,'isolated-union-probe')['excludedBefore']==retained['originalMovementEnd']['excludedBefore'],6);counters['isolatedBoundaries']+=1
 # Warm full-parent authentication cannot replace any of106 physical acquisitions.
 packet,args=root(chosen[0]);source=source_bytes(packet);replay(source,*args,[]);physical=[*(REPOSITORY/p for p in I['runtimeSourceHashes']),*OWN];real=Path.read_bytes
 for target in physical:
  for fault in ['missing','corrupt','null','oversize','suffix']:
   def altered(path):
    if path==target:
     if fault=='missing':raise FileNotFoundError(str(path))
     if fault=='corrupt':return b'changed'
     if fault=='null':return None
     if fault=='oversize':return b' '*(8388608+1)
     return real(path)+b'\n'
    return real(path)
   try:Path.read_bytes=altered;reject(lambda:replay(source,*args,[]),9);counters['physicalPinFaults']+=1
   finally:Path.read_bytes=real
 require(counters['physicalPinFaults']==106*5 and counters['ledgerFaults']==60 and counters['resignedEvents']==6 and counters['claimFaults']==24,6)
 print('PASS focused closure',json.dumps(counters,sort_keys=True),flush=True);return counters

def main():
 import argparse
 p=argparse.ArgumentParser();p.add_argument('--behavior',action='store_true');p.add_argument('--profile',choices=['paid','armed'],default='paid');p.add_argument('--moves',type=int,default=0);p.add_argument('--generate',action='store_true');p.add_argument('--closure',action='store_true');p.add_argument('--group');a=p.parse_args()
 if a.behavior:behavior(a.profile,a.moves);print('PASS genuine lifecycle behavior')
 elif a.closure:closure()
 elif a.generate:FIXTURE.write_text(json.dumps(generate(),indent=2)+'\n');print('PASS explicit fixture generation')
 else:verify(a.group)
if __name__=='__main__':main()
