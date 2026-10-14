#!/usr/bin/env python3
"""B01 immutable authentic admission probe; no new Breakdown product transition."""
import copy, hashlib, importlib.util, json, platform, subprocess, sys, time, tracemalloc
from pathlib import Path
sys.dont_write_bytecode=True
COMMIT='ed443bdf2dfb5e6d8e3333364f52b49b9d939c83'
REPO='/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable'
SNAPSHOT=Path('/private/tmp/breakdown-extension-ed443bdf-baseline')
OUT=Path('/private/tmp/breakdown-extension-probe.json')
SCHEMA='docs/specs/combat-actual-repeated-movement-completion-v1.schema.json'
ORACLE='docs/specs/verify-combat-actual-repeated-movement-completion-v1.py'
def git_bytes(path):return subprocess.check_output(['git','-C',REPO,'show',COMMIT+':'+path])
inv=json.loads(git_bytes(SCHEMA));paths=sorted(set(inv['runtimeSourceHashes'])|set(inv['testArtifactHashes'])|{SCHEMA,ORACLE})
for path in paths:
    target=SNAPSHOT/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(git_bytes(path))
spec=importlib.util.spec_from_file_location('b01_actual_completion',SNAPSHOT/ORACLE)
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
fixture=json.loads(m.test_bytes('docs/specs/fixtures/combat-actual-repeated-movement-completion-v1.json'))
catalog=m.steps.seq.expected_catalog();positions=catalog['positions']
out=dict(commit=COMMIT,python=sys.version,platform=platform.platform(),snapshot=str(SNAPSHOT),
         exportedFiles=len(paths),externalPins=len(inv['runtimeSourceHashes']),
         method='Public RMC replay from full immutable Source and six independent ledgers; compare retained Control/Proof and inspect canonical sequence edge. No successor Breakdown publication or admission asserted.',rows=[])
start=time.monotonic()
for row,prior,upstream in m.roots():
    for count in range(5):
        saved=next(x for x in fixture['traces'] if (x['owner'],x['firstSeal'],x['moves'])==(row['owner'],row['firstSeal'],count))
        parent,args=m.root_cut(row,prior,upstream,count)
        source=saved['sourceCanonicalUtf8'].encode('ascii');ledger=copy.deepcopy(saved['lifecycleLedger'])
        parsed,ctx,state,owned=m.replay(source,*args,ledger)
        assert m.raw(state,'Control').decode('ascii')==saved['controlCanonicalUtf8'][-1]
        digest=m.identity('source',m.canonical(parsed,'Source'))
        proof=dict(contractVersion=1,sourceId='armcomp.'+digest,sourceHash='sha256:'+digest,base=ctx['base'],control=state)
        assert m.raw(proof,'Proof').decode('ascii')==saved['proofCanonicalUtf8']
        cycle=state['activeCycle'];edge=m.policy.edge(cycle)
        boundary=next(p for p in positions if p['positionId']==edge['breakdownPositionId'])
        successor=next(p for p in positions if p['positionId']==edge['combatPositionIds'][0])
        assert positions.index(successor)==positions.index(boundary)+1
        assert state['closed'] and state['flow']==dict(kind='idle') and state['interruptContext'] is None
        assert state['positionId']==boundary['positionId'] and cycle['ordinal']==2
        assert state['movementEnd']['ordinal']==2 and state['movementEnd']['completionReceiptId']==state['receipts'][-1]['receiptId']
        assert len(owned)==(1 if count==0 else 3) and len(state['nextCycleProgress'])==count
        assert all(e['breakdownVehicleCohort'] is None for e in m.world.PACK['elements'])
        assert all(e['operationalState']['vehicleBreakdownState'] is None for e in state['world']['elements'])
        action=m.sha(m.encode(dict(contractVersion=1,kind='complete-breakdown-segment',operationStage=1)))
        candidate=m.encode(dict(contractVersion=1,repeatedMovementCompletionSourceCanonicalUtf8=source.decode('ascii'),breakdownEventCanonicalUtf8=[]))
        out['rows'].append(dict(owner=row['owner'],firstSeal=row['firstSeal'],moves=count,sourceBytes=len(source),candidateInitialSourceBytes=len(candidate),
            controlBytes=len(m.raw(state,'Control')),proofBytes=len(m.raw(proof,'Proof')),
            stateVersion=state['stateVersion'],candidateSuccessorVersion=state['stateVersion']+1,
            cycle=cycle,scope=state['movementEnd']['scope'],currentProof=state['movementEnd'],
            originalProof=state['priorMovementEnd'],randomState=state['randomState'],members=state['members'],
            progress=state['nextCycleProgress'],targetUses=state['targetUses'],attackHistory=state['attackHistory'],
            relationships=state['world']['relationships'],cohesionCauses=state['world']['cohesionCauses'],
            breakdownPosition=boundary,combatEntryPosition=successor,legacyActionId=action,
            noVehicleFacts=True,noVehicleState=True,worldHash=m.sha(m.encode(state['world']))))
        if row['owner']=='axis' and row['firstSeal']=='attacker' and count in (0,1):
            partial=copy.deepcopy(parsed);partial['lifecycleEventCanonicalUtf8']=partial['lifecycleEventCanonicalUtf8'][:0 if count==0 else 1]
            cutoff=0 if count==0 else 1
            _,_,part,_=m.replay(m.raw(partial,'Source'),*args,ledger[:cutoff])
            out.setdefault('authenticatedNonterminals',[]).append(dict(moves=count,stateVersion=part['stateVersion'],
                positionId=part['positionId'],flow=part['flow'],closed=part['closed'],movementEnd=part['movementEnd'],
                candidateBreakdownBoundary=False))
        OUT.write_text(json.dumps(out,indent=2)+'\n')
out['wallSeconds']=round(time.monotonic()-start,3)
out['candidateBounds']=dict(Source=2*inv['encodedUpperBounds']['Source']+2*32768+512,
    Proof=inv['encodedUpperBounds']['Proof']+inv['capacityAccounting']['world']+131072,
    Control=inv['capacityAccounting']['world']+131072,Input=1024,NewLedger=1026,depth=inv['limits']['depth']+2)
out['note']='candidate successor versions and bounds are research arithmetic only; no new event, receipt, or source is accepted here.'
# Only carrier syntax allocation is measured; this does not measure full admission or native allocation.
tracemalloc.start();parse_start=time.monotonic();syntax=json.loads(candidate.decode('ascii'));current,peak=tracemalloc.get_traced_memory();tracemalloc.stop()
assert syntax['repeatedMovementCompletionSourceCanonicalUtf8']==source.decode('ascii')
out['carrierSyntaxMeasurement']=dict(inputBytes=len(candidate),seconds=round(time.monotonic()-parse_start,6),
    tracedPeakBytes=peak,scope='Python outer-carrier json.loads only, modules warm; excludes full ancestral replay, owned returned values and native allocation.')
OUT.write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps(dict(result=str(OUT),rows=len(out['rows']),wallSeconds=out['wallSeconds'],exportedFiles=len(paths),candidateBounds=out['candidateBounds'])))
