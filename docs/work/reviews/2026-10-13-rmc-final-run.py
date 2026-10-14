"""Bounded final verifier receipt; never writes executable artifacts."""
import hashlib,json,os,signal,subprocess,sys,time
from pathlib import Path
repo=Path(__file__).resolve().parents[3]
paths=['docs/specs/verify-combat-actual-repeated-movement-completion-v1.py','docs/specs/combat-actual-repeated-movement-completion-v1.schema.json','docs/specs/fixtures/combat-actual-repeated-movement-completion-v1.json']
def hashes():return {p:hashlib.sha256((repo/p).read_bytes()).hexdigest() for p in paths}
command=['python3','-B','-u',paths[0]];budget=1200;before=hashes();start=time.monotonic();log=Path('/private/tmp/rmc-final-normal.log');timed=False
with log.open('wb') as stream:
 process=subprocess.Popen(command,cwd=repo,stdout=stream,stderr=subprocess.STDOUT,start_new_session=True)
 try:code=process.wait(timeout=budget)
 except subprocess.TimeoutExpired:os.killpg(process.pid,signal.SIGKILL);process.wait();code=process.returncode;timed=True
after=hashes();receipt=dict(command=command,budgetSeconds=budget,exitCode=code,timedOut=timed,elapsedSeconds=round(time.monotonic()-start,3),preHashes=before,postHashes=after,artifactsUnchanged=before==after,logSha256=hashlib.sha256(log.read_bytes()).hexdigest())
Path('/private/tmp/rmc-final-normal.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt));print(log.read_text()[-6000:]);sys.exit(1 if timed or before!=after else code)
