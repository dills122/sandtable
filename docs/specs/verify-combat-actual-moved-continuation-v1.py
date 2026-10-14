#!/usr/bin/env python3
"""Private seven-ledger moved continuation reference; no control offer or execution."""
import sys,copy,json,hashlib,importlib.util,re,itertools
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent;REPOSITORY=ROOT.parent.parent;NAME='combat-actual-moved-continuation-v1'
I=json.loads((ROOT/(NAME+'.schema.json')).read_bytes());S={k:[f.split(':') for f in v.split()] for k,v in I['objects'].items()};L=I['limits']
def load(name,file):
 s=importlib.util.spec_from_file_location(name,ROOT/file);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
n1=load('n2_parent','verify-combat-actual-moved-assault-v1.py');pac=load('n2_pac','verify-combat-actual-post-assault-continuation-v1.py')
class Invalid(ValueError):
 def __init__(self,n,path=''):self.code=f'CMB-MCT-{n:03}';super().__init__(self.code+' '+path)
def require(ok,n,path=''):
 if not ok:raise Invalid(n,path)
def encode(v):return json.dumps(v,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(b):return 'sha256:'+hashlib.sha256(b).hexdigest()
def domain(k,b):return hashlib.sha256(I['domains'][k].encode()+b'\0'+b).hexdigest()
def limit(k):return L['sourceBytes'] if k=='Source' else L['proofBytes'] if k=='Proof' else L['eventBytes'] if k=='Event' else L['inputBytes'] if k in ('Input','Command') else L['controlBytes']
def typed(v,k,depth=0):
 require(depth<=L['depth'],1)
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
 elif k=='profile':require(v in ('paid','armed'),2)
 elif k=='commandKind':require(v in ('open-empty-release','complete-empty-release'),3)
 elif k=='continuationPhase':require(v in ('release-unopened','release-open','assessed'),2)
 elif k=='null':require(v is None,1)
 elif k=='utf8':require(type(v) is str and v.isascii() and 0<len(v)<=L['parentSourceBytes'],1)
 else:n1.typed(v,k,depth)
def canonical(v,k):
 if k.endswith('?'):return None if v is None else canonical(v,k[:-1])
 if k.endswith('[]'):return [canonical(x,k[:-2]) for x in v]
 if k in I['effectTags']:return canonical(v,I['effectTags'][k][v['kind']])
 if k in S:return {f:canonical(v[f],t) for f,t in S[k]}
 if k in ('profile','commandKind','continuationPhase','null','utf8'):return v
 return n1.canonical(v,k)
def raw(v,k):
 typed(v,k);b=encode(canonical(v,k));require(len(b)<=limit(k),1,k);return b
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
 for path,h in I['runtimeSourceHashes'].items():require(hashlib.sha256((REPOSITORY/path).read_bytes()).hexdigest()==h,9,path)
 for p,h in OWN.items():require(sha(p.read_bytes())==h,9,'own-delivery')
def manifest_hash():return 'sha256:'+domain('dependency-manifest',encode(dict(sorted(I['runtimeSourceHashes'].items()))))
def source_bytes(packet):
 b=raw(packet,'Source');require(packet['contractVersion']==1 and len(packet['releaseEventCanonicalUtf8'])<=2,3)
 require(all(len(x)<=L['eventBytes'] for x in packet['releaseEventCanonicalUtf8']),1);return b
def unit_key(u):return (u['creationBinding'],u['originalSide'],u['elementId'])
def derive(packet,request,ledgers):
 parent_packet=n1.parse(packet['movedAssaultSourceCanonicalUtf8'].encode(),'Source')
 parent_frame=n1.replay(parent_packet,request,*ledgers);parent=n1.control(parent_frame)
 require(parent['closed'] and parent['phase']=='reserve-entry',4)
 profile=packet['profile'];require((parent['paid'] is not None)==(profile=='paid'),4)
 cycle=parent['cycle'];require(cycle['ordinal']==1 and cycle['gameTurn']==1 and cycle['operationStage']==1 and cycle['playerPhaseSlot']=='first-acting-side',3)
 members=copy.deepcopy(parent_frame['entry']['members']);w=parent['world'];content=json.loads((ROOT/'fixtures/combat-content-v7.canonical.json').read_bytes())
 originals=[dict(creationBinding=w['creationBinding'],originalSide=e['sideId'],elementId=e['elementId']) for e in content['elements'] if e['sideId']==cycle['actingSide']]
 require(len(members)==1 and [unit_key(m['unit']) for m in members]==sorted(map(unit_key,originals)),4)
 for m in members:
  e=next(x for x in w['elements'] if x['elementId']==m['unit']['elementId']);op=e['operationalState']
  require(m['status']=='none' and e['reserveStatus']=='none' and m['baseCpa']==10 and all(v is None for k,v in m['history'].items() if k!='scope'),3)
  require(op['capabilityPointsExpended']==dict(numerator=9 if profile=='paid' else 4,denominator=1) and e['ammunition']['points']==(0 if profile=='paid' else 10),3)
  m['spentCp']=copy.deepcopy(op['capabilityPointsExpended']);raw(m,'ReserveMember')
 require(len(parent['actualProgressRefs'])==(3 if profile=='paid' else 2),4)
 require(parent['movementEnd']==parent_frame['entry']['movementEnd'] and parent['movementEnd']['ordinal']==1,4)
 if profile=='paid':require(parent['paid']['window'] is None and not any(x['status']=='pending' for x in w['custodyLots']),4)
 else:
  require(not any(w[k] for k in ['settlements','custodyLots','guards','relationships','futureObligations','replacementEntitlements']),3)
  require(parent['randomState']==dict(contractVersion=1,algorithmId='sandtable.sha256-counter.v1',seed=1,nextByteCursor=2),3)
 basis=dict(contractVersion=1,profile=profile,parentSourceHash=sha(packet['movedAssaultSourceCanonicalUtf8'].encode()),cycleId=parent['cycleId'],configurationHash=cycle['admittedPolicyBundleDigest'])
 return dict(packet=packet,parent=parent,parentFrame=parent_frame,profile=profile,members=members,content=content,base=basis,baseHash='sha256:'+domain('base',raw(basis,'Base')))
def initial(f):
 p=f['parent'];release='mrel.'+domain('release',encode(dict(baseHash=f['baseHash'],cycleId=p['cycleId'],positionId=p['sequencePosition']['positionId'])))
 return dict(contractVersion=1,profile=f['profile'],parentSourceHash=f['base']['parentSourceHash'],baseHash=f['baseHash'],releaseId=release,phase='release-unopened',stateVersion=p['stateVersion'],prefix=p['prefix'],sequencePosition=copy.deepcopy(p['sequencePosition']),cycle=copy.deepcopy(p['cycle']),cycleId=p['cycleId'],world=copy.deepcopy(p['world']),randomState=copy.deepcopy(p['randomState']),movementEnd=copy.deepcopy(p['movementEnd']),actualProgressRefs=copy.deepcopy(p['actualProgressRefs']),members=copy.deepcopy(f['members']),attackHistory=copy.deepcopy(p['paid']['attackHistory'] if p['paid'] else []),targetUses=copy.deepcopy(p['paid']['targetUses'] if p['paid'] else []),releaseOpeningReceiptId=None,releaseCompletionReceiptId=None,assessment=None,controlWindow=None,receipts=copy.deepcopy(p['receipts']),releaseReceipts=[],closed=False)
def neighbors(f,location):return sorted(e['secondLocationId'] if e['firstLocationId']==location else e['firstLocationId'] for e in f['content']['edges'] if location in (e['firstLocationId'],e['secondLocationId']))
def distance(f,start,end):
 pending=[(start,0)];seen={start}
 for current,d in pending:
  if current==end:return d
  for other in neighbors(f,current):
   if other not in seen:seen.add(other);pending.append((other,d+1))
 raise Invalid(3,'unreachable-content')
def assessment(f,s,world=None):
 w=s['world'] if world is None else world;require(s['releaseCompletionReceiptId'] is not None,4);profile=f['profile'];cycle=f['parent']['cycle'];witnesses=[];unsupported=[]
 require(all(x['relationships'] is not None and all(x[k] is not None for k in ('disposition','losses','retreat')) for x in w['settlements']) and not w['brokenVehicleLots'],3)
 for m0 in f['members']:
  m=copy.deepcopy(m0);u=m['unit'];e=next(x for x in w['elements'] if x['elementId']==u['elementId']);op=e['operationalState'];m['spentCp']=copy.deepcopy(op['capabilityPointsExpended'])
  require(e['ammunition']['points']==(0 if profile=='paid' else 10) and e['components'][0]['currentToe']>0,3)
  reps=[x for x in w['representations'] if x['bindingKind']=='independent-element' and x['boundElementIds']==[u['elementId']]];require(len(reps)==1 and reps[0]['currentLocationId']==e['currentLocationId'],4)
  if u in f['parent']['movementEnd']['excludedBefore']:continue
  original=next(x for x in f['parent']['movementEnd']['endLocations'] if x['unit']==u)
  enemies_at_end=[x['locationId'] for x in f['parent']['movementEnd']['endLocations'] if x['unit']['originalSide']!=cycle['actingSide']]
  if not any(distance(f,original['locationId'],x)<=2 for x in enemies_at_end):continue
  relationships=[x for x in w['relationships'] if x['active'] and (x['attacker']==u or x['defender']==u)]
  require(all((x['gameTurn'],x['operationStage'])==(cycle['gameTurn'],cycle['operationStage']) for x in w['relationships'] if x['active']),4)
  kinds={x['kind'] for x in relationships};require(kinds<=set(['contact','engaged']),3);bo=4 if 'engaged' in kinds else 2 if 'contact' in kinds else 0
  for dest in neighbors(f,e['currentLocationId']):
   if any(x['currentLocationId']==dest for x in w['elements']) or any(x['currentLocationId']==dest for x in w['guards']):continue
   spent=op['capabilityPointsExpended']['numerator'];after=spent+2+bo
   if after>15:continue
   dp=max(0,after-10)-max(0,spent-10);require(op['cohesionLevel']-dp>=-2**31,7)
   witness=dict(kind='movement',unit=copy.deepcopy(u),destinationLocationId=dest,terrainCost=2,breakOffCost=bo,afterCp=after,excessCpDp=dp,usesReleaseException=False)
   enemies=[x for x in w['elements'] if x['elementId']!=u['elementId']]
   if any(x['currentLocationId'] in neighbors(f,dest) for x in enemies):unsupported.append(dict(witness=witness,reason='requires-reaction'))
   else:witnesses.append(witness)
 require(len(witnesses)<=2 and len(unsupported)<=2,7)
 cert=dict(contractVersion=1,profile=profile,sourceOccurrence=f['parent']['cycleId'],sourceOrdinal=1,nextOrdinal=2,worldHash=sha(raw(w,'World')),configurationHash=cycle['admittedPolicyBundleDigest'],selectedRulesHash=RULES_HASH,catalogueHash=CATALOGUE_HASH,moralePairs=1296,assaultPairs=6480,captureRefusalVariants=8840,supportHandlersHash='sha256:'+domain('handlers',encode(I['supportHandlers'])),capacitiesHash='sha256:'+domain('capacities',encode(L)))
 value=dict(contractVersion=1,mode=profile,sourceOccurrence=f['parent']['cycleId'],sourceOrdinal=1,nextOrdinal=2,releaseCompletionReceiptId=s['releaseCompletionReceiptId'],movementCompletionReceiptId=f['parent']['movementEnd']['completionReceiptId'],worldHash=sha(raw(w,'World')),membersHash=sha(raw(f['members'],'ReserveMember[]')),progress=copy.deepcopy(f['parent']['actualProgressRefs']),witnesses=witnesses,unsupportedWitnesses=unsupported,combatAssessment='no-own-ammunition' if profile=='paid' else 'armed-ordinary-prospective',witnessOutcome='supported',unsupportedReason=None,executionAvailable=False,supportCertificate=cert)
 raw(value,'Assessment');return value
CATALOGUE=tuple(n1.aad.catalogue_records(n1.aad.normalized_rules()))
CATALOGUE_HASH='sha256:'+domain('catalogue',encode(CATALOGUE))
RULES_HASH=sha(encode(n1.aad.normalized_rules()))
def support_context(f):
 entry=f['parentFrame']['entry'];candidate=f['parentFrame']['parentProof']['candidate']
 prior=dict(world=copy.deepcopy(entry['world']),randomState=copy.deepcopy(entry['randomState']),attackHistory=[],targetUses=[],slots=[dict(role=r,allocation=dict(unit=copy.deepcopy(candidate[r]['unit']),committedToe=10)) for r in ('attacker','defender')])
 return dict(base={},prior=prior,boundary=f['parentFrame']['boundary'],selection=dict(attacker=copy.deepcopy(candidate['attacker']),defender=copy.deepcopy(candidate['defender'])),cycle=f['parent']['cycle'],rules=n1.aad.normalized_rules(),config=n1.paid_leaves.aad.are.selection.env.Context().config,graph=copy.deepcopy(n1.paid_leaves.aad.are.selection.env.world.GRAPH),anchors=copy.deepcopy(n1.paid_leaves.aad.are.selection.env.world.ANCHORS))
def certify(f,handlers=None,capacities=None,records=None):
 require(set(I['supportHandlers'])==set(I['supportHandlers'] if handlers is None else handlers),7,'handlers')
 caps=L if capacities is None else capacities
 for key in ['sourceBytes','parentSourceBytes','controlBytes','proofBytes','eventBytes','inputBytes','depth','members','progress','witnesses','unsupportedWitnesses','releaseEvents','releaseReceipts']:
  require(key in caps and caps[key]>=L[key],7,'capacity/'+key)
 supplied=CATALOGUE if records is None else records
 require(tuple(supplied)==CATALOGUE,7,'catalogue');ctx=support_context(f)
 n1.paid_certify_support(ctx)
 paid,_=n1.paid_paid_world(ctx);summaries={}
 for row in CATALOGUE:
  if row['kind']=='capture-refusal':
   fact=row['facts'];key=tuple(fact[k] for k in ('attackerPercent','defenderPercent','rawEngaged','requiredRetreat','capturedRole','captureShare'))+(row['refusalHexes'],);summaries[key]=row
 observed=set()
 for row in summaries.values():
  fact=row['facts'];retreat='not-required' if fact['requiredRetreat']==0 else 'refuse-retreat' if row['refusalHexes'] else 'retreat'
  for custody in ('relocate-and-guard','leave-unguarded') if row['attackerCapturedToe']+row['defenderCapturedToe']>0 else (None,):
   result=dict(resultId='res.'+'f'*64,commitmentId='cmt.'+'f'*64,facts=fact)
   w=n1.project(ctx,paid,result,'relationships',retreat,custody);raw(w,'World')
   # Explicit isolated policy facts: never a source/proof/event nor a claimed RNG history.
   isolated=copy.deepcopy(f);isolated['profile']='paid';isolated['members']=copy.deepcopy(f['members'])
   for m in isolated['members']:m['spentCp']=copy.deepcopy(next(e for e in w['elements'] if e['elementId']==m['unit']['elementId'])['operationalState']['capabilityPointsExpended'])
   state=dict(world=w,releaseCompletionReceiptId='mct.'+'f'*64)
   a=assessment(isolated,state,w);require(a['witnesses'],7,'unsupported-class');observed.add((len(w['guards']),len(w['replacementEntitlements']),tuple(x['kind'] for x in w['relationships']),fact['capturedRole']))
 if f['profile']=='armed':
  a=assessment(f,dict(world=f['parent']['world'],releaseCompletionReceiptId='mct.'+'f'*64));require(a['witnesses'] and all(x['afterCp']==6 and x['excessCpDp']==0 for x in a['witnesses']),7,'armed-profile')
 require(observed,7,'empty-support')
def transition(f,prior,inp,fresh=True):
 raw(inp,'Input');cmd=inp['command'];require(inp['actor']=='system',4)
 require(cmd['contractVersion']==1 and cmd['parentSourceHash']==f['base']['parentSourceHash'] and cmd['releaseId']==prior['releaseId'],4)
 ch=sha(raw(cmd,'Command'));old=next((x for x in prior['releaseReceipts'] if x['commandHash']==ch and x['actor']==inp['actor']),None)
 if old:return copy.deepcopy(prior),None,old['receiptId']
 require(not prior['closed'] and len(prior['releaseReceipts'])<2 and cmd['expectedPriorVersion']==prior['stateVersion'],6)
 s=copy.deepcopy(prior);kind=cmd['kind']
 if kind=='open-empty-release':
  require(prior['phase']=='release-unopened',6)
  if fresh:certify(f)
  effect=dict(kind='release-opened',releaseId=s['releaseId'],pending=[]);s['phase']='release-open'
 else:
  require(prior['phase']=='release-open' and prior['releaseOpeningReceiptId'] is not None,6)
  effect=dict(kind='release-completed',releaseId=s['releaseId'],openingReceiptId=s['releaseOpeningReceiptId'],retainedUnits=[],dispositionReceipts=[]);s['phase']='assessed';s['closed']=True
 c=s['cycle'];ev=dict(contractVersion=1,eventType='moved-continuation-'+effect['kind'],author='system',campaignId=c['campaignId'],rulesetHash=c['rulesetHash'],configurationHash=c['admittedPolicyBundleDigest'],profile=f['profile'],parentSourceHash=s['parentSourceHash'],baseHash=f['baseHash'],cycleId=s['cycleId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],input=copy.deepcopy(inp),effect=effect)
 rid='mct.'+domain('receipt',encode(ev));ev['receiptId']=rid;b=raw(ev,'Event')
 s['releaseOpeningReceiptId' if kind=='open-empty-release' else 'releaseCompletionReceiptId']=rid;s['stateVersion']=ev['stateVersion'];s['prefix']=n1.seq.prefix_event(prior['prefix'],b)
 receipt=dict(commandHash=ch,eventHash=sha(b),receiptId=rid,actor='system',stateVersion=s['stateVersion']);s['receipts'].append(copy.deepcopy(receipt));s['releaseReceipts'].append(receipt)
 if kind=='complete-empty-release':s['assessment']=assessment(f,s)
 raw(s,'Control');return s,b,rid
def replay(source,request,prefix,lifecycle,breakdown,selection,round,paid,release):
 packet=parse(bytes(source),'Source');source_bytes(packet);ancestors=[list(map(bytes,l)) for l in [prefix,lifecycle,breakdown,selection,round,paid]];owned=[bytes(x) for x in release];require(len(owned)==len(packet['releaseEventCanonicalUtf8']),4)
 inputs=[parse(x,'Input') for x in owned];dependencies();f=derive(packet,request,ancestors);s=initial(f)
 for expected,inp in zip(packet['releaseEventCanonicalUtf8'],inputs):
  s,event,rid=transition(f,s,inp,fresh=False);require(event is not None and event==expected.encode(),6)
 return f,s
def proof(packet,s):
 h=sha(source_bytes(packet));last=packet['releaseEventCanonicalUtf8'][-1] if packet['releaseEventCanonicalUtf8'] else None
 return dict(contractVersion=1,sourceId='mcts.'+h[7:],sourceHash=h,parentSourceHash=s['parentSourceHash'],dependencyManifestHash=manifest_hash(),control=s,lastEventHash=None if last is None else sha(last.encode()),terminalReceiptId=s['releaseCompletionReceiptId'])
def command(s,kind):return dict(command=dict(contractVersion=1,kind=kind,parentSourceHash=s['parentSourceHash'],releaseId=s['releaseId'],expectedPriorVersion=s['stateVersion']),actor='system')
def parent_case(c):
 packet=json.loads(c['initialSourceCanonicalUtf8']);led={k:[] for k in ('selection','round','paid')}
 for inp,ev in zip(c['inputCanonicalUtf8'],c['eventCanonicalUtf8']):
  phase=json.loads(inp)['phase'];led[phase].append(inp.encode());packet[phase+'EventCanonicalUtf8'].append(ev)
 args=[json.loads(c['requestCanonicalUtf8']),*[list(map(str.encode,c[k])) for k in ['prefixInputCanonicalUtf8','lifecycleInputCanonicalUtf8','breakdownInputCanonicalUtf8']],led['selection'],led['round'],led['paid']]
 return packet,args
def trace(c):
 parent,args=parent_case(c);profile='paid' if parent['paidEventCanonicalUtf8'] else 'armed';packet=dict(contractVersion=1,profile=profile,movedAssaultSourceCanonicalUtf8=n1.source_bytes(parent).decode(),releaseEventCanonicalUtf8=[]);initial_source=source_bytes(packet).decode();release=[];cuts=[];commands=[];events=[];maxima=dict(sourceBytes=0,controlBytes=0,proofBytes=0,eventBytes=0,inputBytes=0)
 def retain(f,state):
  cut={}
  for kind,data in [('source',source_bytes(packet)),('control',raw(state,'Control')),('proof',raw(proof(packet,state),'Proof'))]:
   cut[kind]=dict(bytes=len(data),sha256=sha(data));maxima[kind+'Bytes']=max(maxima[kind+'Bytes'],len(data))
  cuts.append(cut)
 f,state=replay(source_bytes(packet),*args,release);retain(f,state)
 for kind in ('open-empty-release','complete-empty-release'):
  inp=command(state,kind);state,event,rid=transition(f,state,inp);b=raw(inp,'Input');release.append(b);commands.append(b.decode());events.append(event.decode());packet['releaseEventCanonicalUtf8'].append(event.decode());maxima['eventBytes']=max(maxima['eventBytes'],len(event));maxima['inputBytes']=max(maxima['inputBytes'],len(b));f,state=replay(source_bytes(packet),*args,release);retain(f,state)
 require(state['world']==f['parent']['world'] and state['randomState']==f['parent']['randomState'] and state['actualProgressRefs']==f['parent']['actualProgressRefs'],6,'preservation')
 return dict(name=c['name'],profile=profile,requestCanonicalUtf8=c['requestCanonicalUtf8'],prefixInputCanonicalUtf8=c['prefixInputCanonicalUtf8'],lifecycleInputCanonicalUtf8=c['lifecycleInputCanonicalUtf8'],breakdownInputCanonicalUtf8=c['breakdownInputCanonicalUtf8'],selectionInputCanonicalUtf8=[x.decode() for x in args[4]],roundInputCanonicalUtf8=[x.decode() for x in args[5]],paidInputCanonicalUtf8=[x.decode() for x in args[6]],initialSourceCanonicalUtf8=initial_source,inputCanonicalUtf8=commands,eventCanonicalUtf8=events,cuts=cuts),maxima
def generate():
 cases=json.loads((ROOT/'fixtures/combat-actual-moved-assault-v1.json').read_bytes())['cases'];result=[];maxima=dict(sourceBytes=0,controlBytes=0,proofBytes=0,eventBytes=0,inputBytes=0)
 for case in cases:
  item,measured=trace(case);result.append(item)
  for k,v in measured.items():maxima[k]=max(maxima[k],v)
 return dict(contract=NAME,contractVersion=1,cases=result),maxima
def verify():
 fixture=json.loads((ROOT/'fixtures'/(NAME+'.json')).read_bytes());rebuilt,measured=generate();require(fixture==rebuilt,6,'fixture')
 require(len(fixture['cases'])==56 and sum(c['profile']=='paid' for c in fixture['cases'])==12,6,'inventory')
 print(json.dumps(dict(status='pass',cases=56,paid=12,armed=44,cuts=168,events=112,maxima=measured)))
if __name__=='__main__':
 if '--draft-freeze' in sys.argv:
  fixture,maxima=generate();(ROOT/'fixtures'/(NAME+'.json')).write_bytes(encode(fixture)+b'\n');print(json.dumps(dict(status='draft reference measurement; native not accepted',cases=56,cuts=168,events=112,maxima=maxima)))
 else:verify()
