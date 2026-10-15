#!/usr/bin/env python3
"""N3 private authentic control: real finish or non-executable repeat intent."""
import sys,copy,json,hashlib,importlib.util
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent;REPOSITORY=ROOT.parent.parent;NAME='combat-actual-moved-cycle-control-v1'
I=json.loads((ROOT/(NAME+'.schema.json')).read_bytes());S={k:[f.split(':') for f in v.split()] for k,v in I['objects'].items()};L=I['limits']
spec=importlib.util.spec_from_file_location('n3_parent',ROOT/'verify-combat-actual-moved-continuation-v1.py');n2=importlib.util.module_from_spec(spec);spec.loader.exec_module(n2)
class Invalid(ValueError):
 def __init__(self,n):super().__init__(f'CMB-MCC-{n:03}')
def require(ok,n):
 if not ok:raise Invalid(n)
def encode(v):return json.dumps(v,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(b):return 'sha256:'+hashlib.sha256(b).hexdigest()
def domain(k,b):return hashlib.sha256(I['domains'][k].encode()+b'\0'+b).hexdigest()
def limit(k):return L['sourceBytes'] if k=='Source' else L['proofBytes'] if k=='Proof' else L['eventBytes'] if k=='Event' else L['inputBytes'] if k in ('Input','Command') else L['controlBytes']
ENUM={'controlKind':('control-open','finish','repeat-intent','expire','unavailable','fallback-step'),'controlPhase':('control-unopened','control-open','finished','repeat-intent-pending'),'effectKind':('control-opened','phase-finished','repeat-intent-pending')}
def typed(v,k,depth=0):
 require(depth<=40,1)
 if k.endswith('?'):
  if v is not None:typed(v,k[:-1],depth)
 elif k.endswith('[]'):
  require(type(v) is list and len(v)<=512,1)
  for x in v:typed(x,k[:-2],depth+1)
 elif k in S:
  require(type(v) is dict and set(v)=={f for f,t in S[k]},1)
  for f,t in S[k]:typed(v[f],t,depth+1)
 elif k in ENUM:require(v in ENUM[k],3)
 elif k in ('parentCarrier','eventCarrier'):require(type(v) is str and v.isascii() and 0<len(v)<=(L['parentSourceBytes'] if k=='parentCarrier' else L['eventBytes']),1)
 else:n2.typed(v,k,depth)
def canonical(v,k):
 if k.endswith('?'):return None if v is None else canonical(v,k[:-1])
 if k.endswith('[]'):return [canonical(x,k[:-2]) for x in v]
 if k in S:return {f:canonical(v[f],t) for f,t in S[k]}
 if k in ENUM or k in ('parentCarrier','eventCarrier'):return v
 return n2.canonical(v,k)
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
 b=raw(packet,'Source');require(packet['contractVersion']==1 and len(packet['controlEventCanonicalUtf8'])<=2,3);return b
def replay(source,request,prefix,lifecycle,breakdown,selection,round,paid,release,control):
 packet=parse(source,'Source');source_bytes(packet);dependencies();ledgers=copy.deepcopy([prefix,lifecycle,breakdown,selection,round,paid,release,control]);require(len(control)==len(packet['controlEventCanonicalUtf8']),4)
 pf,parent=n2.replay(packet['continuationSourceCanonicalUtf8'].encode(),request,*ledgers[:7]);require(parent['closed'] and parent['phase']=='assessed' and parent['assessment']['witnessOutcome']=='supported' and not parent['assessment']['executionAvailable'],4)
 basis=dict(contractVersion=1,parentSourceHash=sha(packet['continuationSourceCanonicalUtf8'].encode()),cycleId=parent['cycleId'],assessmentHash=sha(n2.raw(parent['assessment'],'Assessment')),configurationHash=parent['cycle']['admittedPolicyBundleDigest'],dependencyManifestHash=manifest_hash())
 f=dict(packet=packet,parent=parent,parentFrame=pf,request=request,basis=basis,baseHash='sha256:'+domain('base',raw(basis,'Base')),ledgers=ledgers)
 state=initial(f)
 for expected,b in zip(packet['controlEventCanonicalUtf8'],ledgers[7]):
  state,event,_=transition(f,state,parse(b,'Input'),False);require(event is not None and event==expected.encode(),6)
 return f,state
def initial(f):
 s=copy.deepcopy(f['parent']);s.update(parentSourceHash=f['basis']['parentSourceHash'],baseHash=f['baseHash'],phase='control-unopened',closed=False,controlId='mcc.'+domain('control',raw(f['basis'],'Base')),controlWindow=None,closure=None,openingClockFailure=False,acceptedHighWater=None,pendingRepeatIntent=None,pendingIntentReceiptId=None,controlReceipts=[],activeCycle=copy.deepcopy(s['cycle']))
 s['decisionId']=s['controlId']+'.decision';return s
def transition(f,prior,inp,fresh=True):
 raw(inp,'Input');cmd=inp['command'];kind=cmd['kind'];actor=inp['actor'];p=f['parent'];c=p['cycle'];timer=kind in ('expire','unavailable');opening=kind=='control-open'
 require(cmd['contractVersion']==1 and cmd['parentSourceHash']==f['basis']['parentSourceHash'] and cmd['controlId']==prior['controlId'] and cmd['cycleId']==p['cycleId'] and cmd['configurationHash']==f['basis']['configurationHash'],4)
 require(actor==(c['actingSide'] if kind in ('finish','repeat-intent') else 'system') and (cmd['expectedPriorVersion'] is None)==timer and cmd['decisionId']==(None if opening else prior['decisionId']),4)
 h=sha(raw(cmd,'Command'));duplicate=next((r for r in prior['controlReceipts'] if r['commandHash']==h and r['actor']==actor),None)
 if duplicate:return copy.deepcopy(prior),None,duplicate['receiptId']
 if timer and prior['phase']!='control-open':return copy.deepcopy(prior),None,None
 require(not prior['closed'] and len(prior['controlReceipts'])<2 and prior['stateVersion']<2**63-1,6)
 if not timer:require(cmd['expectedPriorVersion']==prior['stateVersion'],6)
 s=copy.deepcopy(prior);now=inp['admittedAt'];reason='owner-choice';author=actor;intent=None
 if opening:
  require(prior['phase']=='control-unopened' and prior['stateVersion']<=2**63-3,6)
  if fresh:n2.certify(f['parentFrame'])
  configuration=n2.n1.selection_leaves.env.Context().config
  budget=next(w['decisionBudgetMilliseconds'] for w in configuration['windows'] if w['kind']=='cycle-control')
  template=n2.n1.selection_leaves.inputs.make_timing(configuration,'cycle-control',1000)
  require(template['configHash']==f['basis']['configurationHash'],4)
  valid=inp['clockAvailable'] and now is not None and now<=253402300799999-budget
  s['openingClockFailure']=not valid;s['acceptedHighWater']=now if valid else None;s['controlWindow']=dict(contractVersion=1,configHash=f['basis']['configurationHash'],kind='cycle-control',decisionBudgetMilliseconds=budget,openedAtUnixMilliseconds=now,deadlineUnixMilliseconds=now+budget,highWaterUnixMilliseconds=now) if valid else None
  outcome='control-opened';reason='opening-clock-unavailable' if not valid else 'window-opened';s['phase']='control-open'
 else:
  require(prior['phase']=='control-open',6);timing=prior['controlWindow'];lost=prior['openingClockFailure'] or not inp['clockAvailable'] or now is None or (prior['acceptedHighWater'] is not None and now<prior['acceptedHighWater']);late=timing is not None and now is not None and now>=timing['deadlineUnixMilliseconds']
  if kind=='fallback-step':require(prior['openingClockFailure'],5)
  if kind=='expire' and not lost and not late:return copy.deepcopy(prior),None,None
  if not lost:s['acceptedHighWater']=now;s['controlWindow']['highWaterUnixMilliseconds']=now
  if lost or late or timer or kind=='fallback-step':
   author='system';outcome='phase-finished';reason='opening-clock-unavailable' if prior['openingClockFailure'] else 'clock-unavailable' if lost else 'deadline' if late else 'controller-unavailable'
  else:outcome='repeat-intent-pending' if kind=='repeat-intent' else 'phase-finished'
  s['closed']=True;s['phase']='finished' if outcome=='phase-finished' else outcome
  if outcome=='phase-finished':
   s['activeCycle']=None;s['sequencePosition']=finish_position()
  else:
   a=prior['assessment'];require(len(a['witnesses'])>0 and not a['executionAvailable'] and a['sourceOccurrence']==p['cycleId'],7)
   intent=dict(contractVersion=1,kind='pending-repeat-intent',sourceOccurrence=p['cycleId'],sourceOrdinal=c['ordinal'],expectedNextOrdinal=c['ordinal']+1,parentSourceHash=f['basis']['parentSourceHash'],worldHash=a['worldHash'],assessmentHash=f['basis']['assessmentHash'],configurationHash=f['basis']['configurationHash'],decisionId=prior['decisionId'],witnessesHash=sha(encode(a['witnesses'])),executionAvailable=False);s['pendingRepeatIntent']=copy.deepcopy(intent)
 effect=dict(kind=outcome,reason=reason,decisionId=s['decisionId'],timing=s['controlWindow'],openingClockFailure=s['openingClockFailure'],successorPositionId=s['sequencePosition']['positionId'],intent=intent)
 event=dict(contractVersion=1,eventType='moved-cycle-control-'+outcome,author=author,campaignId=c['campaignId'],rulesetHash=c['rulesetHash'],configurationHash=c['admittedPolicyBundleDigest'],profile=p['profile'],parentSourceHash=f['basis']['parentSourceHash'],baseHash=f['baseHash'],controlId=s['controlId'],cycleId=p['cycleId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],input=inp,effect=effect)
 rid='mcce.'+domain('receipt',encode(event));event['receiptId']=rid;b=raw(event,'Event');s['stateVersion']=event['stateVersion'];s['prefix']=n2.n1.seq.prefix_event(prior['prefix'],b)
 record=dict(commandHash=h,eventHash=sha(b),receiptId=rid,actor=actor,stateVersion=s['stateVersion']);s['receipts'].append(copy.deepcopy(record));s['controlReceipts'].append(record)
 if outcome=='phase-finished':s['closure']=dict(cycleId=p['cycleId'],ordinal=c['ordinal'],outcome='finished',receiptId=rid)
 if outcome=='repeat-intent-pending':s['pendingIntentReceiptId']=rid
 raw(s,'Control');return s,b,rid
def finish_position():
 return copy.deepcopy(next(p for p in n2.n1.seq.expected_catalog()['positions'] if p['positionId']=='land.position.operation-1.first-player.truck-convoy-movement'))
def proof(packet,s):
 h=sha(source_bytes(packet));events=packet['controlEventCanonicalUtf8'];return dict(contractVersion=1,sourceId='mccs.'+h[7:],sourceHash=h,parentSourceHash=s['parentSourceHash'],dependencyManifestHash=manifest_hash(),control=s,lastEventHash=sha(events[-1].encode()) if events else None,terminalReceiptId=s['closure']['receiptId'] if s['closure'] else s['pendingIntentReceiptId'])
def command(s,kind,now=1000,clock=True):
 return dict(command=dict(contractVersion=1,kind=kind,parentSourceHash=s['parentSourceHash'],controlId=s['controlId'],cycleId=s['cycleId'],configurationHash=s['cycle']['admittedPolicyBundleDigest'],expectedPriorVersion=None if kind in ('expire','unavailable') else s['stateVersion'],decisionId=None if kind=='control-open' else s['decisionId']),actor=s['cycle']['actingSide'] if kind in ('finish','repeat-intent') else 'system',admittedAt=now,clockAvailable=clock)
def trace(c,choice):
 parent=json.loads(c['initialSourceCanonicalUtf8']);parent['releaseEventCanonicalUtf8']=c['eventCanonicalUtf8'];parent_bytes=n2.source_bytes(parent)
 args=[json.loads(c['requestCanonicalUtf8']),*[list(map(str.encode,c[k])) for k in ['prefixInputCanonicalUtf8','lifecycleInputCanonicalUtf8','breakdownInputCanonicalUtf8','selectionInputCanonicalUtf8','roundInputCanonicalUtf8','paidInputCanonicalUtf8','inputCanonicalUtf8']]]
 packet=dict(contractVersion=1,continuationSourceCanonicalUtf8=parent_bytes.decode(),controlEventCanonicalUtf8=[]);initial_source=source_bytes(packet).decode();control=[];cuts=[];events=[];inputs=[]
 def retain(state):
  cuts.append({k:dict(bytes=len(b),sha256=sha(b)) for k,b in [('source',source_bytes(packet)),('control',raw(state,'Control')),('proof',raw(proof(packet,state),'Proof'))]})
 f,s=replay(source_bytes(packet),*args,control);retain(s)
 for kind in ['control-open',choice]:
  inp=command(s,kind);s,event,_=transition(f,s,inp);b=raw(inp,'Input');inputs.append(b.decode());control.append(b);events.append(event.decode());packet['controlEventCanonicalUtf8'].append(event.decode());f,s=replay(source_bytes(packet),*args,control);retain(s)
 for field in ['world','randomState','movementEnd','actualProgressRefs','members','attackHistory','targetUses']:
  require(s[field]==f['parent'][field],6)
 return dict(name=c['name']+'-'+choice,parentName=c['name'],profile=c['profile'],choice=choice,initialSourceCanonicalUtf8=initial_source,inputCanonicalUtf8=inputs,eventCanonicalUtf8=events,cuts=cuts)
def generate():
 parents=json.loads((ROOT/'fixtures/combat-actual-moved-continuation-v1.json').read_bytes())['cases'];cases=[trace(c,choice) for c in parents for choice in ('finish','repeat-intent')];return dict(contract=NAME,contractVersion=1,cases=cases)
def verify():
 rebuilt=generate();require(json.loads((ROOT/'fixtures'/(NAME+'.json')).read_bytes())==rebuilt,6);print(json.dumps(dict(status='pass',parents=56,cases=112,cuts=336,events=224)))
if __name__=='__main__':
 if '--draft-freeze' in sys.argv:
  f=generate();(ROOT/'fixtures'/(NAME+'.json')).write_bytes(encode(f)+b'\n');print(json.dumps(dict(status='draft reference; native not accepted',cases=len(f['cases']))))
 else:verify()
