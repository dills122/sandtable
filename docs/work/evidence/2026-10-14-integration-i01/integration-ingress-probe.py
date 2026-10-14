import importlib.util,json,sys,hashlib,copy,platform,subprocess,time
from pathlib import Path
sys.dont_write_bytecode=True
repo=Path('/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable')
def load(name,filename):
 s=importlib.util.spec_from_file_location(name,repo/'docs/specs'/filename);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
pen=load('pen','verify-combat-positive-entry-v1.py');cl=load('cl','verify-combat-inherited-reaction-closure-v1.py')
rows=[]; begun=time.time()
for side in ['axis','commonwealth']:
 case=next(c for c in json.loads(cl.FIXTURE.read_text())['cases'] if c['name']==side+'-player-decline')
 result=cl.trace(case); assert cl.goldens(result)==case['goldens']; args,inp,ev,before,state=result
 q,created,pre,w,stage,res,moves,triggers=args
 facts=dict(cycle=state['cycle'],world=state['world'],creationBinding=state['creationBinding'],weather=dict(attackerKind=state['operationStageWeather'][0]['kind'],defenderKind=state['operationStageWeather'][0]['kind']))
 candidate=pen.steps.candidate(facts);assert candidate is not None
 packet=dict(contractVersion=1,requestCanonicalUtf8=pen.encode(q).decode('ascii'),createdCanonicalUtf8=created.decode('ascii'),preambleEventCanonicalUtf8=[b.decode('ascii') for b in pre],weatherEventCanonicalUtf8=[b.decode('ascii') for b in w],stageEventCanonicalUtf8=[b.decode('ascii') for b in stage],reserveEventCanonicalUtf8=[b.decode('ascii') for b in res],entryEventCanonicalUtf8=[])
 def outcome(fn):
  try:fn();return 'accepted'
  except Exception as e:return type(e).__name__+':'+str(e)
 extra=copy.deepcopy(packet);extra['movementEventCanonicalUtf8']=[e.decode('ascii') for e in moves+triggers];extra['reactionClosureEventCanonicalUtf8']=[ev.decode('ascii')]
 tail=copy.deepcopy(packet);tail['entryEventCanonicalUtf8']=[e.decode('ascii') for e in moves+triggers]+[ev.decode('ascii')]
 # Never bypass production admissions. Mechanical successors below are explicitly NOT authenticated ingress.
 lifecycle=copy.deepcopy(state);lifecycle.pop('currentPosition',None);lifecycle.update(interruptContext=None,movementEnd=None)
 projected=copy.deepcopy(lifecycle);future=[]
 kernel_error=None
 try:
  for kind in pen.lc.KINDS:
   projected,event=pen.lc._emit(projected,pen.lc.command(projected,kind));future.append(event)
  bdstate=copy.deepcopy(projected);bdstate['breakdownCompletionReceiptId']=None
  projected,event=pen.bd._emit(bdstate,pen.bd.command(bdstate));future.append(event)
 except Exception as e:kernel_error=type(e).__name__+':'+str(e)
 row=dict(side=side,sourceCase=case['name'],authenticatedThrough='reaction-closure',stateVersion=state['stateVersion'],originalRandom=q['randomState'],afterRandom=state['randomState'],world=[dict(element=x['elementId'],location=x['currentLocationId'],cp=x['operationalState']['capabilityPointsExpended']) for x in state['world']['elements']],candidate=candidate,flow=state['breakdownFlow']['kind'],historyEventCount=sum(len(x) for x in args[2:])+1,moveReceiptIds=[json.loads(e)['receiptId'] for e in moves+triggers],closureReceipt=json.loads(ev)['receiptId'],openingHash=pen.sha(pen.packet_bytes(packet)),pinnedOpeningHash=pen.INVENTORY['openingHashes'][side],unchangedOpeningAdmission=outcome(lambda:pen.initial(packet)),withMovementFields=outcome(lambda:pen.initial(extra)),movementEventsInEntry=outcome(lambda:pen.replay(tail)),ordinaryLifecycleReaderWithTrigger=outcome(lambda:pen.lc.replay(q,created,pre,w,stage,res,moves+triggers,[])),pureKernelOnly=dict(authenticated=False,error=kernel_error,endVersion=projected['stateVersion'],position=projected['sequencePosition']['positionId'],futureEvents=len(future)))
 assert row['openingHash']==row['pinnedOpeningHash'] and row['unchangedOpeningAdmission']=='accepted'
 assert row['withMovementFields']=='Invalid:CMB-PEN-001' and row['movementEventsInEntry']=='Invalid:CMB-PEN-007' and row['ordinaryLifecycleReaderWithTrigger']=='Invalid:CMB-IML-004'
 assert row['pureKernelOnly']['endVersion']==18 and row['pureKernelOnly']['error'] is None
 rows.append(row)
 retained=Path('/private/tmp/integration-ingress-probe');retained.mkdir(exist_ok=True)
 (retained/(side+'-evidence.json')).write_text(json.dumps(dict(request=q,created=created.decode('ascii'),events=[e.decode('ascii') for group in args[2:] for e in group]+[ev.decode('ascii')],closureState=state,unauthenticatedKernelSuccessor=projected),sort_keys=True))
sourceHashes={str(f.relative_to(repo)):hashlib.sha256(f.read_bytes()).hexdigest() for f in [repo/'docs/specs'/n for n in ['verify-combat-positive-entry-v1.py','verify-combat-inherited-reaction-closure-v1.py','verify-combat-inherited-reaction-trigger-v1.py','verify-combat-inherited-movement-v1.py','verify-combat-inherited-movement-lifecycle-v1.py','verify-combat-inherited-breakdown-completion-v1.py','fixtures/combat-inherited-reaction-closure-v1.json','fixtures/combat-positive-entry-v1.json']]}
receipt=dict(sourceHashes=sourceHashes,status='research-only-not-RED-credit',head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo,text=True).strip(),environment=dict(python=sys.version,platform=platform.platform()),elapsedSeconds=time.time()-begun,rows=rows,scriptSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
Path('/private/tmp/integration-ingress-probe-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt,indent=2))
