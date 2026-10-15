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
 if k.endswith('?'):
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
def transition(f,prior,inp,fresh=True,admission=True):
 # RED checkpoint: authentic pending input currently has no executable consumer.
 raise Invalid(7)
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
 require(len(s['actualProgressRefs'])==len(movement)-1 and s['world']['elements'][0]['operationalState']['capabilityPointsExpended']!=parent['world']['elements'][0]['operationalState']['capabilityPointsExpended'],6)
 require(all(s['world'][field]==parent['world'][field] for field in ['settlements','custodyLots','guards','replacementEntitlements','futureObligations','brokenVehicleLots']),6)
 return dict(name=c['name'],parentName=c['name'],profile=c['profile'],initialSourceCanonicalUtf8=initial_source,inputCanonicalUtf8=inputs,eventCanonicalUtf8=events,cuts=cuts)
def generate(negatives=False):
 cases=[c for c in json.loads((ROOT/'fixtures/combat-actual-moved-cycle-control-v1.json').read_bytes())['cases'] if c['choice']=='repeat-intent'];return dict(contract=NAME,contractVersion=1,cases=[trace(c,negatives) for c in cases])
def verify():
 rebuilt=generate(True);require(json.loads((ROOT/'fixtures'/(NAME+'.json')).read_bytes())==rebuilt,6);print(json.dumps(dict(status='pass',parents=len(rebuilt['cases']),cuts=sum(len(c['cuts']) for c in rebuilt['cases']),events=sum(len(c['eventCanonicalUtf8']) for c in rebuilt['cases']))))
if __name__=='__main__':
 if '--red-gate' in sys.argv:red_gate()
 elif '--write-fixture' in sys.argv:
  value=generate();(ROOT/'fixtures'/(NAME+'.json')).write_bytes(encode(value)+b'\n');print(json.dumps(dict(status='fixture written',parents=len(value['cases']))))
 else:verify()
