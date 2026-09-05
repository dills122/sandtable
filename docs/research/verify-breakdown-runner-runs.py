#!/usr/bin/env python3
"""Run all declared BRK successors from a clean checkout, or compare two evidence roots.

Usage: python3 verify-breakdown-runner-runs.py run REPO ARTIFACT_ROOT
       python3 verify-breakdown-runner-runs.py compare REPO ROOT_A ROOT_B COMMIT OUTPUT_JSON

Public CLI supplies strict semantic readback; comparison checks its successful outputs,
source cleanliness, artifact hashes, both proofs, nine canonical files and report identities.
"""
import hashlib, json, subprocess, sys
from pathlib import Path

FILES = ['exercise-manifest.json','seed-ledger.json','accepted-actions.jsonl','canonical-events.jsonl','initial-snapshot.json','final-snapshot.json','step-evidence.jsonl','reconstruction-proof.json','readjudication-proof.json']
def sha(b): return 'sha256:'+hashlib.sha256(b).hexdigest()
def inputs(repo):
    inventory=json.loads((repo/'docs/specs/breakdown-fixture-migration.v1.json').read_text())
    extra=json.loads((repo/'docs/research/breakdown-runner-migration-results.json').read_text())
    return [r['successorPath'] for r in inventory['scenarioFiles']]+[r['path'] for r in extra['supplementalScenarioFiles']]
def expected(repo):
    result=set()
    for path in inputs(repo):
        d=json.loads((repo/path).read_text())
        children=[e for p in d['pairs'] for e in [p['baseline'],p['candidate']]] if 'pairs' in d else d.get('exercises',[d])
        for e in children: result.add((Path(path).stem,e['exerciseId']))
    return result

def scan(repo,root,commit):
    matrix={}; reports={}; proofs=0
    for manifest_path in root.rglob('exercise-manifest.json'):
        folder=manifest_path.parent
        manifest=json.loads(manifest_path.read_text())
        group=folder.relative_to(root).parts[0]
        key=(group,manifest['exerciseId'])
        assert key not in matrix, ('duplicate',key)
        assert json.loads((folder/'run-result.json').read_text())['status']=='succeeded',key
        build=json.loads((folder/'build-identity.json').read_text())
        assert build['dirty'] is False and build['headCommit']==commit,(key,build)
        for proof in ['reconstruction-proof.json','readjudication-proof.json']:
            assert json.loads((folder/proof).read_text())['status']=='verified',(key,proof)
            proofs+=1
        artifact=json.loads((folder/'artifact-manifest.json').read_text())
        for entry in artifact['files']:
            b=(folder/entry['path']).read_bytes()
            assert len(b)==entry['sizeBytes'] and sha(b)==entry['sha256'],(key,entry)
        matrix[key]={name:sha((folder/name).read_bytes()) for name in FILES}
    assert set(matrix)==expected(repo),(set(matrix)^expected(repo))
    for p in root.rglob('*report.json'):
        report=json.loads(p.read_text())
        if 'deterministic' not in report: continue
        group=p.relative_to(root).parts[0]
        assert group not in reports
        reports[group]={'fingerprint':report['reportFingerprint'],'deterministic':report['deterministic']}
    assert len(reports)==9,len(reports)
    return matrix,reports,proofs

mode=sys.argv[1]; repo=Path(sys.argv[2]).resolve()
if mode=='run':
    root=Path(sys.argv[3]).resolve(); assert not root.exists(),root
    assert not subprocess.check_output(['git','status','--porcelain'],cwd=repo).strip()
    root.mkdir(parents=True)
    dll=repo/'artifacts/bin/Cna.ExerciseRunner/debug/Cna.ExerciseRunner.dll'
    for path in inputs(repo):
        group=Path(path).stem
        cmd=['dotnet',str(dll),'maneuver' if '/maneuvers/'in path else 'exercise','run','--manifest',path,'--artifact-root',str(root/group)]
        completed=subprocess.run(cmd,cwd=repo,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
        (root/(group+'.log')).write_text(completed.stdout)
        print(group,completed.returncode,flush=True)
        if completed.returncode: raise SystemExit(completed.stdout)
elif mode=='compare':
    a,b=Path(sys.argv[3]),Path(sys.argv[4]);commit=sys.argv[5]
    left,reports,proofs=scan(repo,a,commit);right,other,otherproofs=scan(repo,b,commit)
    assert left==right,'canonical artifacts differ'
    assert reports==other,'deterministic reports differ'
    rows=[{'manifest':key[0],'exerciseId':key[1],'files':left[key]}for key in sorted(left)]
    encoded=json.dumps(rows,sort_keys=True,separators=(',',':')).encode()
    result={'candidateCommit':commit,'manifestCount':len(inputs(repo)),'campaignsPerRun':len(left),'runs':2,'verifiedProofs':proofs+otherproofs,'canonicalFileComparisons':len(left)*len(FILES),'canonicalMatrixHash':sha(encoded),'reportFingerprints':{k:v['fingerprint']for k,v in sorted(reports.items())},'canonicalFiles':FILES,'artifactMatrix':rows}
    out=Path(sys.argv[6]);out.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items()if k!='artifactMatrix'},indent=2))

else:
    raise SystemExit('Expected run or compare; see module docstring for arguments.')
