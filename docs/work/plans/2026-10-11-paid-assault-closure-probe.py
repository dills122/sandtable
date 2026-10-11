import sys,json,time,importlib.util
from pathlib import Path
sys.dont_write_bytecode=True
repo=(Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parents[3]).resolve()
seen=set()
def audit(event,args):
    if event=='open' and isinstance(args[0],(str,bytes)):
        try:
            p=Path(args[0]).resolve()
            if p.is_relative_to(repo):seen.add(p.relative_to(repo).as_posix())
        except (ValueError,OSError):pass
sys.addaudithook(audit)
t=time.monotonic()
p=repo/'docs/specs/verify-combat-actual-assault-v1.py'
spec=importlib.util.spec_from_file_location('aad_closure_probe',p)
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
fixture=json.loads(m.FIXTURE.read_bytes())
rows=[]
for case in fixture['cases']:
    source=case['source']['canonicalUtf8'].encode('ascii')
    selection=[json.loads(x['canonicalUtf8']) for x in case['trustedSelectionInputs']]
    round_inputs=[json.loads(x['canonicalUtf8']) for x in case['trustedRoundInputs']]
    proof=m.proof(source,selection,round_inputs,[])
    assert m.raw(proof,'AdmissionProof')==case['proof']['canonicalUtf8'].encode('ascii')
    budget=m.encode(dict(contractVersion=1,admissionSourceCanonicalUtf8=source.decode('ascii'),paidEventsCanonicalUtf8=['\\'*32768]*11))
    rows.append(dict(capacityModelBytes=len(budget),capacityModelOnly=True,owner=case['owner'],firstSeal=case['firstSeal'],sourceBytes=len(source),proofBytes=len(m.raw(proof,'AdmissionProof')),selectionLedgerBytes=len(m.encode(case['trustedSelectionInputs'])),roundLedgerBytes=len(m.encode(case['trustedRoundInputs']))))
result_schema=repo/'docs/specs/combat-result-settlement-v2.schema.json'
rs=json.loads(result_schema.read_bytes())
runtime=set(m.DEPENDENCIES)|{'docs/specs/verify-combat-actual-assault-v1.py','docs/specs/combat-actual-assault-v1.schema.json','docs/specs/combat-result-settlement-v2.schema.json'}
tests={'docs/specs/fixtures/combat-actual-assault-v1.json'}
unknown=seen-runtime-tests
out=dict(scope='predecessor public positive replay plus direct Result2 schema acquisition; not paid closure',elapsedSeconds=round(time.monotonic()-t,3),runtimeCount=len(runtime),testCount=len(tests),readCount=len(seen),unknown=sorted(unknown),notObservedRuntime=sorted(runtime-seen),runtimePaths=sorted(runtime),testPaths=sorted(tests),cases=rows,resultObjects=sorted(rs['objects']))
Path('/private/tmp/paid-assault-closure-probe.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps({k:v for k,v in out.items() if k not in ('runtimePaths','testPaths','resultObjects')},indent=2))
