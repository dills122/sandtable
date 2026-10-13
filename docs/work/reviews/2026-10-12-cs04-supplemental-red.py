#!/usr/bin/env python3
"""Actual repeated Movement reference: compile-valid semantic RED boundary."""
import copy,hashlib,importlib.util,json,sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]/'specs'
spec=importlib.util.spec_from_file_location('actual_pac_predecessor',ROOT/'verify-combat-actual-post-assault-continuation-v1.py');pac=importlib.util.module_from_spec(spec);spec.loader.exec_module(pac)
encode=pac.encode
class Invalid(ValueError):
    def __init__(self,code,path=''):self.code=f'CMB-RMV-{code:03}';self.path=path;super().__init__(self.code+' '+path)
def apply(data,selection,rounds,paid,continuation,movement,new_input):
    source=json.loads(data);pac.replay(source['continuationSourceCanonicalUtf8'].encode('ascii'),selection,rounds,paid,continuation)
    if type(movement) is not list or movement or source['movementEventCanonicalUtf8']:raise Invalid(1,'/empty-root-movement-ledger')
    raise Invalid(7,'/missing-actual-move-consumer')
def roots():
    rows=json.loads((ROOT/'fixtures/combat-actual-post-assault-continuation-v1.json').read_bytes())['traces'];paid=json.loads((ROOT/'fixtures/combat-actual-paid-assault-v1.json').read_bytes())['cases']
    return [(r,next(p for p in paid if (p['owner'],p['firstSeal'])==(r['owner'],r['firstSeal']))) for r in rows if r['outcome']=='repeat']
def semantic_tests():
    failures=[]
    for row,upstream in roots():
        args=[upstream['selectionLedger'],upstream['roundLedger'],upstream['paidLedger'],row['continuationLedger']]
        prior_source,ctx,terminal,_=pac.replay(row['sourceCanonicalUtf8'].encode('ascii'),*args)
        assert terminal['stateVersion']==37 and terminal['status']=='repeated' and terminal['activeCycle']['ordinal']==2
        assert terminal['members'][0]['spentCp']==dict(numerator=5,denominator=1) and not terminal['nextCycleProgress'] and not terminal['targetUses']
        assert terminal['randomState']['seed']==1 and terminal['randomState']['nextByteCursor']==10 and terminal['movementEnd']['completionReceiptId'].startswith('iml.')
        pins=pac.DEPENDENCIES|{str(p.relative_to(ROOT.parent.parent)):hashlib.sha256(p.read_bytes()).hexdigest() for p in pac.OWN_DELIVERY}
        h=pac.identity('source',pac.canonical(prior_source,'Source'));prior_proof=dict(contractVersion=1,sourceId='apcont.'+h,sourceHash='sha256:'+h,base=ctx['base'],control=terminal)
        manifest='sha256:'+pac.digest('sandtable.combat.actual-repeated-movement.dependency-manifest.v1',dict(sorted(pins.items())))
        basis=dict(contractVersion=1,dependencyManifestHash=manifest,continuationProof=prior_proof)
        base_hash='sha256:'+pac.digest('sandtable.combat.actual-repeated-movement.base.v1',basis)
        source=encode(dict(contractVersion=1,continuationSourceCanonicalUtf8=row['sourceCanonicalUtf8'],movementEventCanonicalUtf8=[]))
        inp=dict(command=dict(contractVersion=1,kind='move',baseHash=base_hash,cycleId=pac.policy.cycle_id(terminal['activeCycle'],ctx['boundary']['firstActingSide']),positionId=terminal['positionId'],expectedPriorVersion=37,unit=copy.deepcopy(terminal['members'][0]['unit']),originLocationId=next(e['currentLocationId'] for e in terminal['world']['elements'] if e['elementId']==terminal['members'][0]['unit']['elementId']),destinationLocationId=row['owner']+'-rear'),actor=row['owner'])
        try:
            successor,ledger,receipt=apply(source,*args,[],inp);event=json.loads(json.loads(successor)['movementEventCanonicalUtf8'][0]);assert event['stateVersion']==38 and receipt==event['receiptId'] and len(ledger)==1
            assert event['effect']['costs']['afterCp']==9 and event['effect']['costs']['excessCpDp']==0 and len(event['effect']['endedMemberships'])==1
            print('PASS actual move publication',row['owner'],row['firstSeal'])
        except Invalid as error:
            failures.append((row['owner'],row['firstSeal']));print('FAIL actual move38/rear/CP9/member/progress/event/receipt expected; actual',error.code,row['owner'],row['firstSeal'])
    assert not failures,str(len(failures))+' genuine move publications failed'
if __name__=='__main__':semantic_tests()
