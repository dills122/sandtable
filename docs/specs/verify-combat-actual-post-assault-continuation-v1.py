#!/usr/bin/env python3
"""Private actual post-assault continuation reference; semantic RED checkpoint."""
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('actual_paid_predecessor',ROOT/'verify-combat-actual-paid-assault-v1.py')
paid=importlib.util.module_from_spec(spec);spec.loader.exec_module(paid)
encode=paid.encode
class Invalid(ValueError):
    def __init__(self,code,path=''):self.code=f'CMB-PAC-{code:03}';self.path=path;super().__init__(self.code+' '+path)
def require(ok,code,path=''):
    if not ok:raise Invalid(code,path)
def digest(domain,value):return hashlib.sha256(domain.encode('ascii')+b'\0'+encode(value)).hexdigest()
def base_for(data,selection,rounds,pay):
    predecessor=paid.proof(data,selection,rounds,pay)
    dependencies=paid.DEPENDENCIES|{str(p.relative_to(ROOT.parent.parent)):hashlib.sha256(p.read_bytes()).hexdigest() for p in paid.OWN_DELIVERY}
    manifest='sha256:'+digest('sandtable.combat.actual-post-assault-continuation.dependency-manifest.v1',dict(sorted(dependencies.items())))
    return dict(contractVersion=1,dependencyManifestHash=manifest,paidProof=predecessor,controlClockPolicyId='sandtable.combat.cycle-control-clock.v1')
def source_for(row):
    return encode(dict(contractVersion=1,paidSourceCanonicalUtf8=row['sourceCanonicalUtf8'],continuationEventCanonicalUtf8=[]))
def apply(data,selection,rounds,pay,continuation,new_input):
    source=json.loads(data)
    paid.replay(source['paidSourceCanonicalUtf8'].encode('ascii'),selection,rounds,pay)
    raise Invalid(7,'/missing-post-assault-consumer')
def semantic_tests():
    cases=json.loads((ROOT/'fixtures/combat-actual-paid-assault-v1.json').read_bytes())['cases'];failures=[]
    for row in cases:
        label=row['owner']+'/'+row['firstSeal'];selection=row['selectionLedger'];rounds=row['roundLedger'];pay=row['paidLedger']
        _,ctx,state,_=paid.replay(row['sourceCanonicalUtf8'].encode('ascii'),selection,rounds,pay)
        assert state['stateVersion']==33 and state['closed'] and state['window'] is None
        assert state['randomState']['seed']==1 and state['randomState']['nextByteCursor']==10
        own=next(e for e in state['world']['elements'] if e['elementId']==ctx['selection']['attacker']['unit']['elementId'])
        assert own['ammunition']['points']==0 and own['components'][0]['currentToe']==10
        assert own['operationalState']['capabilityPointsExpended']==dict(numerator=5,denominator=1)
        assert ctx['boundary']['movementEnd']['completionReceiptId'].startswith('iml.')
        base=base_for(row['sourceCanonicalUtf8'].encode('ascii'),selection,rounds,pay)
        inp=dict(command=dict(contractVersion=1,kind='release-open',baseHash='sha256:'+digest('sandtable.combat.actual-post-assault-continuation.base.v1',base),cycleId=ctx['boundary']['cycleId'],expectedPriorVersion=33,decisionId=None),actor='system',admittedAt=None,clockAvailable=True)
        try:
            after,ledger,rid=apply(source_for(row),selection,rounds,pay,[],inp)
            event=json.loads(json.loads(after)['continuationEventCanonicalUtf8'][0]);assert event['stateVersion']==34 and rid==event['receiptId'] and len(ledger)==1
            print('PASS semantic actual Release open',label)
        except Invalid as error:
            failures.append(label);print('FAIL semantic actual Release publication',label,'expected version34/event/receipt/prefix with unchanged World; actual',error.code)
    assert not failures, str(len(failures))+' genuine successor publications failed: '+','.join(failures)
if __name__=='__main__':semantic_tests()
