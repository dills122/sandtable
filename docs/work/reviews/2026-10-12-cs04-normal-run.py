import subprocess,time,json,hashlib,os,signal,sys
from pathlib import Path
fixture=Path('docs/specs/fixtures/combat-actual-repeated-movement-v1.json');identities=json.loads(Path('/private/tmp/cs04-frozen-identities.json').read_bytes());before=hashlib.sha256(fixture.read_bytes()).hexdigest();cmd=['python3','-B','-u','docs/specs/verify-combat-actual-repeated-movement-v1.py'];start=time.monotonic();timed=False;log=Path('/private/tmp/cs04-final-normal.log')
with log.open('wb') as output:
 p=subprocess.Popen(cmd,stdout=output,stderr=subprocess.STDOUT,start_new_session=True)
 try:code=p.wait(timeout=900)
 except subprocess.TimeoutExpired:os.killpg(p.pid,signal.SIGKILL);p.wait();code=p.returncode;timed=True
out=dict(command=cmd,budgetSeconds=900,exitCode=code,timedOut=timed,elapsedSeconds=round(time.monotonic()-start,3),logSha256=hashlib.sha256(log.read_bytes()).hexdigest(),fixtureBeforeSha256=before,fixtureAfterSha256=hashlib.sha256(fixture.read_bytes()).hexdigest(),frozenIdentities=identities,frozenIdentitiesUnchanged=all(hashlib.sha256(Path(k).read_bytes()).hexdigest()==v['sha256'] for k,v in identities.items()));Path('/private/tmp/cs04-final-normal.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(out),flush=True);print(log.read_text(),flush=True);sys.exit(1 if timed or not out['frozenIdentitiesUnchanged'] else code)
