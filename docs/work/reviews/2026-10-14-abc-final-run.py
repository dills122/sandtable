import subprocess,time,json,hashlib,sys,os,signal
from pathlib import Path
oracle='docs/specs/verify-combat-actual-repeated-breakdown-completion-v1.py';schema='docs/specs/combat-actual-repeated-breakdown-completion-v1.schema.json';fixture='docs/specs/fixtures/combat-actual-repeated-breakdown-completion-v1.json';inv=json.loads(Path(schema).read_bytes());paths=sorted(set(inv['runtimeSourceHashes'])|set(inv['testArtifactHashes'])|{oracle,schema,fixture})
def fingerprints():return {p:hashlib.sha256(Path(p).read_bytes()).hexdigest() for p in paths}
before=fingerprints();log=Path('/private/tmp/b02-final.log');start=time.monotonic();timed=False;command=['python3','-B',oracle]
with log.open('wb') as f:
    proc=subprocess.Popen(command,stdout=f,stderr=subprocess.STDOUT,start_new_session=True,env=dict(os.environ,PYTHONUNBUFFERED='1'))
    try:code=proc.wait(timeout=1200)
    except subprocess.TimeoutExpired:os.killpg(proc.pid,signal.SIGKILL);proc.wait();code=proc.returncode;timed=True
out=dict(command=command,budgetSeconds=1200,exitCode=code,timedOut=timed,elapsedSeconds=round(time.monotonic()-start,3),logSha256=hashlib.sha256(log.read_bytes()).hexdigest(),sourceHashesBefore=before,sourceHashesAfter=fingerprints());out['inputsUnchanged']=out['sourceHashesBefore']==out['sourceHashesAfter'];Path('/private/tmp/b02-final.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps({k:v for k,v in out.items() if not k.startswith('sourceHashes')}));print(log.read_text()[-3000:]);sys.exit(1 if timed or not out['inputsUnchanged'] else code)
