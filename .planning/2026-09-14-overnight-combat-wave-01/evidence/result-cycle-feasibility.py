from pathlib import Path
import importlib.util,copy,json,hashlib
root=Path('docs/specs').resolve()
def load(name,stem):
 s=importlib.util.spec_from_file_location(name,root/stem);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
r2=load('probe_result2','verify-combat-result-settlement-v2.py');cyc=load('probe_cycle','verify-combat-cycle-control-v1.py');rel=cyc.rel
results=[]
for branch in ('defender-capture-guard','attacker-capture-escape'):
 for side in ('axis','commonwealth'):
  case=copy.deepcopy(next(c for c in r2.literal_cases() if c['name']==branch));case.update(attackerSide=side,seals=['attacker','defender'] if side=='axis' else ['defender','attacker'])
  ctx,final,ri,re=r2.trace_case(case);r2.read_state(r2.raw(final,'ResultState'),ctx,ri,re)
  boundary=ctx['base']['boundary'];cycle=copy.deepcopy(boundary['cycle']);w=copy.deepcopy(final['world']);before=r2.raw(w,'World');rng=copy.deepcopy(final['randomState']);future=copy.deepcopy(w['futureObligations']);edge=next(e for e in rel.steps.seq.expected_catalog()['cycles'] if e['operationStage']==cycle['operationStage'] and e['playerPhaseSlot']==cycle['playerPhaseSlot'])
  assert final['closed'] and final['caCompletionReceiptId'] and all(s[k] is not None for s in w['settlements'] for k in ('disposition','losses','retreat','relationships'))
  own=next(e for e in w['elements'] if e['elementId']==ctx['base']['steps']['selection']['attacker']['unit']['elementId']);assert own['reserveStatus']=='none'
  member=dict(unit=rel.world.unit(own,w['creationBinding']),status='none',baseCpa=10,spentCp=copy.deepcopy(own['operationalState']['capabilityPointsExpended']),history=rel.empty_history(cycle))
  rb=dict(contractVersion=1,profile='settled-empty-release',cycle=cycle,firstActingSide=boundary['firstActingSide'],positionId=edge['releasePositionId'],priorVersion=final['stateVersion'],priorPrefix=final['prefix'],combatCompletionReceiptId=final['caCompletionReceiptId'],retainedWorldHash=rel.sha(before),randomState=rng,acceptedHighWater=None,members=[member],attackHistory=copy.deepcopy(ctx['committed']['attackHistory']))
  rel.validate_base(rb);release=rel.initial(rb);rins=[];revents=[];suffix=[]
  for kind in ('open','complete'):
   inp=rel.trusted(rel.command(release,kind),'system',None,True);prior=release;release,event,_=rel.transition(rb,prior,inp);assert event and rel.read_event(event,rb,prior,inp)==release;rins.append(inp);revents.append(json.loads(event));suffix.append(json.loads(event)['eventType'])
  assert release['pending']==[] and release['timing'] is None and release['decisionId'] is None and release['acceptedHighWater'] is None
  projected=rel.project_world(w,rb,release);assert r2.raw(projected,'World')==before
  proof=dict(scope=rel.scope(cycle),ordinal=cycle['ordinal'],completionReceiptId='probe.result2.movement-completed',endLocations=cyc.locations(boundary['world']),excludedBefore=[])
  commits=[json.loads(e) for e in ctx['roundEvents'] if json.loads(e)['effect']['kind']=='attack-committed'];assert len(commits)==1;commit=commits[0]
  cb=dict(contractVersion=1,profile='settled-control',releaseBase=rb,releaseInputs=rins,releaseEvents=revents,world=w,movementEnd=proof,progress=[dict(eventType=commit['eventType'],receiptId=commit['receiptId'],eventHash=rel.sha(next(e for e in ctx['roundEvents'] if json.loads(e)['receiptId']==commit['receiptId'])))],targetUses=copy.deepcopy(ctx['committed']['targetUses']))
  assessment=cyc.assess(cb);mode=cyc.control_mode(assessment);state=cyc.initial(cb)
  for kind,actor,now in [('open','system',3500),('finish',side,3501)]:
   if state['status']=='finished':break
   inp=cyc.trusted(cyc.command(cb,state,kind),actor,now,True);prior=state;state,event,_=cyc.transition(cb,prior,inp);assert event and cyc.read_event(event,cb,prior,inp)==state;suffix.append(json.loads(event)['eventType'])
  assert state['status']=='finished' and state['positionId']==edge['finishPositionId'] and state['activeCycle'] is None
  assert r2.raw(state['world'],'World')==before and state['randomState']==rng and state['world']['futureObligations']==future and state['attackHistory']==ctx['committed']['attackHistory'] and state['targetUses']==ctx['committed']['targetUses']
  assert state['stateVersion']==final['stateVersion']+len(suffix)
  results.append(dict(branch=branch,side=side,seals=case['seals'],mode=mode,witnesses=len(assessment['witnesses']),resultAuditMaximum=final['acceptedHighWater'],cycleOpening=3500,versions=[final['stateVersion'],state['stateVersion']],suffix=suffix,guards=len(w['guards']),entitlements=len(w['replacementEntitlements']),future=[o['kind'] for o in future],worldHash=rel.sha(before),worldBytes=len(before),cycleStateBytes=len(cyc.raw(state,'CycleControlState'))))
print(json.dumps(results,indent=2))
