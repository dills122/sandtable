#!/usr/bin/env python3
"""Sequential author closure gate; no runtime authority or fixture rewriting."""
import copy,gc,hashlib,importlib.util,json,sys
from pathlib import Path
sys.dont_write_bytecode=True
REPOSITORY=Path(__file__).resolve().parents[3];ROOT=REPOSITORY/'docs/specs';NAME='combat-actual-moved-repeat-movement-v1'
sp=importlib.util.spec_from_file_location('r01_closure',ROOT/('verify-'+NAME+'.py'));m=importlib.util.module_from_spec(sp);sp.loader.exec_module(m)
checks={}
def reject(action,code=None):
 try:action()
 except ValueError as e:
  if code is not None:assert str(e)==code,(str(e),code)
  return
 raise AssertionError('expected rejection')
def verify():
 parent=json.loads((ROOT/'combat-actual-moved-cycle-control-v1.schema.json').read_bytes())
 inherited=set(m.S)&set(parent['objects'])-{'Source','Base','Command','Input','Event','Control','Proof'}
 assert all(m.I['objects'][k]==parent['objects'][k] for k in inherited)
 assert all(m.I['runtimeSourceHashes'][p]==h for p,h in parent['runtimeSourceHashes'].items())
 m.dependencies();checks.update(inheritedCompoundShapes=len(inherited),unchangedParentPins=len(parent['runtimeSourceHashes']),externalPins=len(m.I['runtimeSourceHashes']))
 # Framing only: adversarial carriers are not authenticated N3 roots.
 packet=dict(contractVersion=1,controlSourceCanonicalUtf8='"'*parent['encodedUpperBounds']['Source'],movementEventCanonicalUtf8=['"'*m.L['eventBytes']]*6)
 wire=m.source_bytes(packet);assert len(wire)<=m.I['encodedUpperBounds']['Source']<m.L['sourceBytes'];assert m.parse(wire,'Source')==packet
 checks['framingBytes']=len(wire);del packet,wire;gc.collect()
 reject(lambda:m.parse(b' '*(m.L['sourceBytes']+1),'Source'),'CMB-MRM-001');reject(lambda:m.parse(b' '*(m.L['inputBytes']+1),'Input'),'CMB-MRM-001')
 checks['oversizeRejections']=2
 fixture=ROOT/'fixtures'/(NAME+'.json');data=fixture.read_bytes();assert hashlib.sha256(data).hexdigest()=='481f93d483d15dd7425f2d025c8d823cfe680ebc36acd3b408ae32bb64e20b8d';cases=json.loads(data)['cases'];parents=json.loads((ROOT/'fixtures/combat-actual-moved-cycle-control-v1.json').read_bytes())['cases'];by_name={c['name']:c for c in parents}
 ceilings=0;bindings=0;resigned=0;ancestor_faults=0
 for c in cases:
  initial,args=m.parent_case(by_name[c['parentName']]);packet=copy.deepcopy(initial);packet['movementEventCanonicalUtf8']=c['eventCanonicalUtf8'];source=m.source_bytes(packet);inputs=list(map(str.encode,c['inputCanonicalUtf8']));f,s=m.replay(source,*args,inputs)
  expected_position=next(p for p in m.n3.n2.n1.seq.expected_catalog()['positions'] if p['positionId']=='land.position.operation-1.first-player.movement-and-combat.movement');assert s['sequencePosition']==expected_position
  assert all(s['activeCycle'][k]==s['originalCycle'][k] for k in s['originalCycle'] if k not in ('ordinal','openedAuthorityVersion','openingPrefix'))
  # Reverse last safe move: remains geometrically valid, but CP17/16 exceeds15.
  destination=json.loads(c['eventCanonicalUtf8'][-1])['effect']['originLocationId'];command=m.raw(m.command(s,'move',destination),'Input');before=m.raw(s,'Control');reject(lambda:m.apply(source,args,inputs,command),'CMB-MRM-005');assert m.raw(s,'Control')==before;ceilings+=1
  retry=m.apply(source,args,inputs,inputs[0],False);assert retry[0]==source and retry[1]==s and retry[2] is None and retry[3]==json.loads(c['eventCanonicalUtf8'][0])['receiptId']
  # Re-sign a causally false effect; an independent input must still defeat it.
  forged=copy.deepcopy(packet);event=json.loads(forged['movementEventCanonicalUtf8'][-1]);event['effect']['costs']['afterCp']+=1;event.pop('receiptId');event['receiptId']='mrm.'+m.domain('receipt',m.encode(event));forged['movementEventCanonicalUtf8'][-1]=m.raw(event,'Event').decode();reject(lambda:m.replay(m.source_bytes(forged),*args,inputs),'CMB-MRM-006');resigned+=1
  if c is next(x for x in cases if x['profile']==c['profile']):
   # Framing only: owned World grammar must grow beyond predecessor native512KiB.
   world=copy.deepcopy(s['world']);element=world['elements'][0];references=[dict(sourceId='s'*128,locator='a'*128)]*512
   for origin in [element['readiness']['initialReadinessOrigin'],element['ammunition']['initialAmmunitionOrigin'],element['operationalState']['initialLedgerOrigin'],element['components'][0]['initialToeOrigin']]:origin['references']=references
   world_wire=m.raw(world,'World');assert 524288<len(world_wire)<m.L['worldBytes'];checks['worldFramingBytes']=max(checks.get('worldFramingBytes',0),len(world_wire))
   # Every independent ledger is mandatory, including the ninth successor ledger.
   for i in range(9):
    ledgers=copy.deepcopy(args[1:]+[inputs]);ledgers[i]=ledgers[i][:-1] if ledgers[i] else [b'{}'];reject(lambda:m.replay(source,args[0],*ledgers));ancestor_faults+=1
   foreign=copy.deepcopy(args);foreign[0]['campaignId']='foreign-campaign';reject(lambda:m.replay(source,*foreign,inputs));bindings+=1
   first_move=copy.deepcopy(initial);first_move['movementEventCanonicalUtf8']=c['eventCanonicalUtf8'][:2];early_source=m.source_bytes(first_move);ef,es=m.replay(early_source,*args,inputs[:2]);origin=json.loads(c['eventCanonicalUtf8'][1])['effect']['originLocationId'];reaction=m.raw(m.command(es,'move',origin),'Input');reject(lambda:m.apply(early_source,args,inputs[:2],reaction),'CMB-MRM-003');bindings+=1
   reject(lambda:m.apply(m.source_bytes(initial),args,[],inputs[0],False),'CMB-MRM-007');bindings+=1
   finish=next(p for p in parents if p['parentName']==by_name[c['parentName']]['parentName'] and p['choice']=='finish');fp,fa=m.parent_case(finish);reject(lambda:m.replay(m.source_bytes(fp),*fa,[]),'CMB-MRM-004');bindings+=1
  print(f'closure parent {ceilings}/{len(cases)}',file=sys.stderr,flush=True)
 # Observe the real isolated support kernel and continue each projection route to its CP ceiling.
 # No observed policy event is appended to an admitted Source, ledger, Proof or RNG history.
 paid_case=next(c for c in cases if c['profile']=='paid');ip,ia=m.parent_case(by_name[paid_case['parentName']]);support_frame,_=m.replay(m.source_bytes(ip),*ia,[]);original=m.transition;support_routes=0;support_steps=0;max_causes=0
 def observe(frame,state,inp,fresh=True,admission=True):
  nonlocal support_routes,support_steps,max_causes
  first=original(frame,state,inp,fresh,admission)
  if not fresh and state['publicationReceiptId']=='mrm.'+'f'*64:
   support_routes+=1;current=first[0];support_steps+=1
   while True:
    max_causes=max(max_causes,len(current['world']['cohesionCauses']));safe=m.live_witnesses(frame,current)['witnesses']
    if not safe:break
    following,event,_=original(frame,current,m.command(current,'move',safe[0]['destinationLocationId']),False);assert event is not None and len(following['actualProgressRefs'])==len(current['actualProgressRefs'])+1<=5
    for field in ['settlements','custodyLots','guards','replacementEntitlements','futureObligations','brokenVehicleLots']:assert following['world'][field]==current['world'][field]
    for field in ['randomState','originalMovementEnd','priorCycleProgress','priorCycleTargetUses','attackHistory','assessment','pendingRepeatIntent','pendingIntentReceiptId']:assert following[field]==current[field]
    current=following;support_steps+=1
  return first
 try:m.transition=observe;m.certify(support_frame)
 finally:m.transition=original
 checks.update(isolatedSupportRoutes=support_routes,isolatedMovementSteps=support_steps,maxIsolatedWorldCauseCount=max_causes)
 checks.update(ceilingFailures=ceilings,resignedEffectForgeries=resigned,independentLedgerFaults=ancestor_faults,modeBoundaryNegatives=bindings,recoveryAfterEveryTerminalCut=ceilings)
 first=m.parse(cases[0]['inputCanonicalUtf8'][0].encode(),'Input');b=m.raw(first,'Input');reject(lambda:m.parse(b+b' ','Input'),'CMB-MRM-008');duplicate=b'{"command":'+m.raw(first['command'],'Command')+b',"command":'+m.raw(first['command'],'Command')+b',"actor":"system"}';reject(lambda:m.parse(duplicate,'Input'),'CMB-MRM-001');first['command']['contractVersion']=True;reject(lambda:m.parse(m.encode(first),'Input'));checks['grammarNegatives']=3
 print(json.dumps(dict(status='pass',checks=checks)))
if __name__=='__main__':verify()
