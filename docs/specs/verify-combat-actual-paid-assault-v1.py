#!/usr/bin/env python3
"""Private actual paid assault reference: semantic unsupported checkpoint for TDD."""
import argparse
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
REPOSITORY = ROOT.parent.parent
INVENTORY = json.loads((ROOT/'combat-actual-paid-assault-v1.schema.json').read_bytes())

class Invalid(ValueError):
    def __init__(self, code, path=''):
        self.code, self.path = f'CMB-APA-{code:03}', path
        super().__init__(self.code+' '+(path or '/'))

def require(ok, code, path=''):
    if not ok: raise Invalid(code,path)

def encode(value): return json.dumps(value,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(data): return 'sha256:'+hashlib.sha256(data).hexdigest()
def digest(kind,value):
    return hashlib.sha256(INVENTORY['domains'][kind].encode('ascii')+b'\0'+encode(value)).hexdigest()

# At this TDD checkpoint only predecessor acquisition is material. Full measured
# successor closure must be frozen before literals/public paid proof acceptance.
aad_path=ROOT/'verify-combat-actual-assault-v1.py'
spec=importlib.util.spec_from_file_location('paid_admission',aad_path)
aad=importlib.util.module_from_spec(spec);spec.loader.exec_module(aad)

def parse_source(data):
    require(type(data) is bytes and 0<len(data)<=INVENTORY['limits']['sourceBytes'],1)
    def pairs(items):
        value={}
        for k,v in items:
            require(k not in value,1,'/'+k);value[k]=v
        return value
    try: value=json.loads(data.decode('ascii'),object_pairs_hook=pairs,parse_constant=lambda _:(_ for _ in ()).throw(ValueError()))
    except (ValueError,UnicodeError,RecursionError) as error:
        if isinstance(error,Invalid):raise
        raise Invalid(1) from error
    require(type(value) is dict and set(value)=={'contractVersion','admissionSourceCanonicalUtf8','paidEventCanonicalUtf8'},1)
    require(type(value['contractVersion']) is int,1)
    require(value['contractVersion']==1,3)
    text=value['admissionSourceCanonicalUtf8']
    require(type(text) is str and text.isascii() and 0<len(text)<=aad.MAX_BYTES,1)
    events=value['paidEventCanonicalUtf8']
    require(type(events) is list and len(events)<=INVENTORY['limits']['acceptedEvents'],1)
    for event in events:require(type(event) is str and event.isascii() and 0<len(event)<=INVENTORY['limits']['eventBytes'],1)
    ordered={k:value[k] for k in ('contractVersion','admissionSourceCanonicalUtf8','paidEventCanonicalUtf8')}
    require(encode(ordered)==data,8)
    return ordered

def admitted(data,selection_ledger,round_ledger,paid_ledger):
    aad.verify_dependencies();source=parse_source(data)
    require(type(paid_ledger) is list and len(paid_ledger)<=INVENTORY['limits']['acceptedEvents'],1,'/paidLedger')
    # Preserve predecessor primitive/union diagnostics before trusted equality.
    try: aad.ledgers(selection_ledger,round_ledger,[])
    except aad.Invalid as error:raise Invalid(int(error.code[-3:]),error.path) from error
    require(len(paid_ledger)==len(source['paidEventCanonicalUtf8']),4,'/paidLedger')
    try: proof=aad.proof(source['admissionSourceCanonicalUtf8'].encode('ascii'),selection_ledger,round_ledger,[])
    except aad.Invalid as error:
        code=int(error.code[-3:]);raise Invalid(9 if code==9 else 4,error.path) from error
    require(not paid_ledger,7,'/paidReplay')
    base=dict(contractVersion=1,admissionProof=proof,resultClockPolicyId=INVENTORY['resultClockPolicyId'])
    return source,base

def read_source(data,selection_ledger,round_ledger,paid_ledger):
    source,_=admitted(data,selection_ledger,round_ledger,paid_ledger)
    return copy.deepcopy(source)

def apply(data,selection_ledger,round_ledger,paid_ledger,new_input):
    source,base=admitted(data,selection_ledger,round_ledger,paid_ledger)
    require(type(new_input) is dict and set(new_input)=={'command','actor','admittedAt','clockAvailable'},1,'/input')
    cmd=new_input['command']
    require(type(cmd) is dict and set(cmd)=={x.split(':')[0] for x in INVENTORY['objects']['PaidCommand'].split()},1,'/command')
    require(type(cmd['contractVersion']) is int,1,'/command/contractVersion')
    require(cmd['contractVersion']==1 and cmd['kind'] in INVENTORY['commands'],3)
    prior=base['admissionProof']['actualRoundProof']['control']
    require(cmd['baseHash']=='sha256:'+digest('base',base) and cmd['roundId']==prior['roundId'],4)
    require(new_input['actor']=='system' and new_input['admittedAt'] is None and new_input['clockAvailable'] is True,4)
    require(cmd['kind']=='commit' and cmd['expectedPriorVersion']==25 and cmd['commitmentId'] is None and cmd['decisionId'] is None and cmd['choice'] is None,4)
    raise Invalid(7,'/paidCommitmentUnsupported')

# These independent expectations intentionally fail at the semantic stub above.
def commit_tests():
    fixture=json.loads((ROOT/'fixtures/combat-actual-assault-v1.json').read_bytes())
    failures=[]
    for case in fixture['cases']:
        selection=[json.loads(x['canonicalUtf8']) for x in case['trustedSelectionInputs']]
        rounds=[json.loads(x['canonicalUtf8']) for x in case['trustedRoundInputs']]
        source=encode(dict(contractVersion=1,admissionSourceCanonicalUtf8=case['source']['canonicalUtf8'],paidEventCanonicalUtf8=[]))
        _,base=admitted(source,selection,rounds,[])
        p=base['admissionProof']['actualRoundProof']['control']
        cmd=dict(contractVersion=1,kind='commit',baseHash='sha256:'+digest('base',base),roundId=p['roundId'],expectedPriorVersion=25,commitmentId=None,decisionId=None,choice=None)
        inp=dict(command=cmd,actor='system',admittedAt=None,clockAvailable=True)
        try:
            after,ledger,receipt=apply(source,selection,rounds,[],inp)
            event=json.loads(parse_source(after)['paidEventCanonicalUtf8'][0])
            assert event['stateVersion']==26 and event['priorVersion']==25
            assert event['priorPrefix']==p['prefix'] and receipt==event['receiptId']
            effect=event['effect'];assert effect['kind']=='attack-committed'
            assert [c['beforeCp'] for c in effect['costs']]==[0,0]
            assert [c['afterCp'] for c in effect['costs']]==[5,3]
            assert [c['beforeAmmo'] for c in effect['costs']]==[10,10]
            assert [c['afterAmmo'] for c in effect['costs']]==[0,0]
            assert effect['preResultRandomState']==p['randomState'] and p['randomState']['nextByteCursor']==2
            assert len(ledger)==1 and ledger[0]==inp
            assert receipt.startswith('cmb.') and effect['commitmentId'].startswith('cmt.')
        except (Invalid,AssertionError) as error:
            failures.append(dict(owner=case['owner'],firstSeal=case['firstSeal'],error=str(error)))
    print(json.dumps(dict(check='genuine-commitment-semantic-expectations',cases=4,failures=failures),indent=2))
    require(not failures,7,'/semanticCommitExpectations')

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--check-commit',action='store_true');parser.parse_args()
    commit_tests()

if __name__=='__main__':main()
