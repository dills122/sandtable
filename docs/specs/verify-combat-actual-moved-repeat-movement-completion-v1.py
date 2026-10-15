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
 else:mrm.n3.n2.n1.typed(v,k,'/')
def canonical(v,k):
 if k in I['arrayAliases']:return [canonical(x,I['arrayAliases'][k]['elementType']) for x in v]
 if k.endswith('?'):return None if v is None else canonical(v,k[:-1])
 if k.endswith('[]'):return [canonical(x,k[:-2]) for x in v]
 if k in I['effectTags']:return canonical(v,I['effectTags'][k][v['kind']])
 if k in S:return {f:canonical(v[f],t) for f,t in S[k]}
 if k in ENUM or k in ('parentCarrier','eventCarrier'):return v
 return mrm.n3.canonical(v,k)
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
 packet=parse(bytes(source),'Source');source_bytes(packet);request=copy.deepcopy(request);ledgers=copy.deepcopy([prefix,lifecycle,breakdown,selection,round,paid,release,control,movement,completion])
 require(all(type(x) is list and all(type(b) is bytes for b in x) for x in ledgers),1)
 require(len(completion)==len(packet['lifecycleEventCanonicalUtf8'])<=3 and sum(map(len,completion))<=L['lifecycleLedgerBytes'],4);dependencies();REPLAY_COUNT+=1
 pf,p=inherited(lambda:mrm.replay(packet['movementSourceCanonicalUtf8'].encode(),request,*ledgers[:9]));c=p['activeCycle'];count=len(p['actualProgressRefs'])
 require(p['phase']=='movement' and p['publicationReceiptId'] is not None and c['ordinal']==2 and p['originalMovementEnd']['ordinal']==1 and p['closure']['ordinal']==1 and p['closure']['outcome']=='repeated',4)
 require(len(movement)==len(p['movementReceipts'])==count+1 and 0<=count<=(2 if p['profile']=='paid' else 5) and len(p['tracks'])==int(count>0) and not p['targetUses'],4)
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
def transition(f,prior,inp,fresh=True,handlers=None,capacities=None):
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
  # Completion is deliberately unimplemented for the next genuine RED checkpoint.
  require(False,7)
 if fresh:certify(f,handlers,capacities)
 event=dict(contractVersion=1,eventType='combat-actual-moved-repeat-'+effect['kind'],author=actor,campaignId=c['campaignId'],rulesetHash=c['rulesetHash'],configurationHash=c['admittedPolicyBundleDigest'],profile=prior['profile'],movementSourceHash=prior['movementSourceHash'],baseHash=f['baseHash'],cycleId=prior['cycleId'],positionId=prior['sequencePosition']['positionId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],input=canonical(inp,'Input'),effect=canonical(effect,'Effect'))
 rid='mrmc.'+domain('receipt',encode(event));event['receiptId']=rid;ev=raw(event,'Event')
 if kind=='complete-movement-segment':s['movementEnd']=expiry(f,prior,rid)
 s['stateVersion']=event['stateVersion'];s['prefix']=mrm.n3.n2.n1.seq.prefix_event(prior['prefix'],ev);receipt=dict(commandHash=ch,eventHash=sha(ev),receiptId=rid,actor=actor,stateVersion=s['stateVersion']);s['receipts'].append(copy.deepcopy(receipt));s['lifecycleReceipts'].append(receipt);raw(s,'Control');return s,ev,rid
def proof(packet,f,s):return dict(contractVersion=1,sourceId='mrmcomp.'+sha(source_bytes(packet))[7:],sourceHash=sha(source_bytes(packet)),base=copy.deepcopy(f['base']),control=copy.deepcopy(s))
def apply(source,args,completion,inp,fresh=True,handlers=None,capacities=None):
 owned=bytes(inp);f,s=replay(bytes(source),*copy.deepcopy(args),copy.deepcopy(completion));after,event,rid=transition(f,s,parse(owned,'Input'),fresh,handlers,capacities)
 if event is None:return source,s,None,rid
 packet=copy.deepcopy(f['packet']);packet['lifecycleEventCanonicalUtf8'].append(event.decode());result=source_bytes(packet);_,rebuilt=replay(result,*args,[*completion,owned]);require(after==rebuilt,6);return result,rebuilt,event,rid

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
def main():
 import argparse
 p=argparse.ArgumentParser();p.add_argument('--behavior',action='store_true');p.add_argument('--profile',choices=['paid','armed'],default='paid');p.add_argument('--moves',type=int,default=0);a=p.parse_args()
 if a.behavior:behavior(a.profile,a.moves);print('PASS genuine lifecycle behavior')
 else:raise SystemExit('normal verifier not yet implemented')
if __name__=='__main__':main()
